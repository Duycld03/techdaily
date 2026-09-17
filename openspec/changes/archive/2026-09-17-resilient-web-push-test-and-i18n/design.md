# Design: Resilient Web Push Test & Localized Error Handling

## Context

TechDaily enables engineers to receive browser Web Push notifications for daily study reminders and streak preservation alerts. The platform implements Web Push via standard Voluntary Application Server Identification (VAPID) protocols communicating with upstream push services (FCM, Apple Web Push, Mozilla Autopush).

In production (`https://techdaily.duckdns.org/settings`), clicking **"Gửi Thông Báo Thử"** triggers an unhandled HTTP 500 error leaking raw English:
`"An unexpected error occurred while processing your request."`

The issue stems from an unhandled `WebPushSubscriptionExpiredException` in `NotificationEndpoints.cs` when an endpoint returns HTTP 410 Gone / 404 Not Found. This exception aborts the test dispatch loop, prevents stale subscription cleanup in PostgreSQL, and bubbles to ASP.NET Core global exception middleware. Concurrently, `frontend/pages/settings.vue` bypasses `useApiError`, leaking raw ProblemDetails strings into the Vietnamese UI, and translation dictionaries lack keys for push error scenarios.

---

## Goals / Non-Goals

### Goals
1. **Zero Unhandled Server Exceptions:** Isolate push delivery exceptions per subscription endpoint within `POST /api/v1/notifications/push/test`.
2. **Self-Healing Subscription Pruning:** Automatically purge expired or revoked endpoints (`WebPushSubscriptionExpiredException`) during test dispatches, matching `DailyPushNotificationWorker` behavior.
3. **Automatic Push State Maintenance:** Set `User.IsPushEnabled = false` when all registered subscriptions for a user are purged as stale.
4. **Standardized API Error Envelopes:** Return machine-readable error codes (`PUSH_NO_SUBSCRIPTIONS`, `PUSH_SUBSCRIPTION_EXPIRED`, `PUSH_DELIVERY_FAILED`) and operational metrics (`sent`, `total`, `stalePurged`).
5. **Zero Raw Error Leaks:** Integrate `useApiError.formatError` in `settings.vue` with caller fallback keys to ensure Vietnamese UI parity.
6. **Prioritize i18n Over Developer Strings:** Enhance `useApiError.ts` so machine-readable `api_errors.<CODE>` lookups take precedence over developer-facing English `responseData.error` strings.
7. **Actionable Expiration Guidance:** Prompt users to toggle push notifications off and on when tokens expire, providing an immediate path to recovery.
8. **100% Bilingual Parity:** Provide complete English and Vietnamese translations for all new error keys.

### Non-Goals
- Changing the cryptographic VAPID signing pipeline or `WebPushService` internals.
- Modifying background worker scheduling or cron triggers in `DailyPushNotificationWorker`.
- Adding third-party push notification channels (e.g. SMS, Email).

---

## Decisions

### 1. Backend Exception Isolation & Stale Subscription Purge Sequence

In `backend/src/TechDaily.Api/Endpoints/NotificationEndpoints.cs`, `POST /api/v1/notifications/push/test` currently iterates through subscriptions without error isolation:

```mermaid
sequenceDiagram
    autonumber
    actor User as Engineer
    participant Client as settings.vue
    participant API as NotificationEndpoints.cs
    participant DB as PostgreSQL (DbContext)
    participant PushSvc as WebPushService
    participant Remote as FCM / Mozilla Push Service

    User->>Client: Click "Gửi Thông Báo Thử"
    Client->>API: POST /api/v1/notifications/push/test
    API->>DB: Query UserPushSubscriptions (UserId == current)
    alt Subscriptions count == 0
        API-->>Client: 400 Bad Request { code: "PUSH_NO_SUBSCRIPTIONS" }
    end

    loop For each subscription
        API->>PushSvc: SendNotificationAsync(endpoint, keys, payload)
        PushSvc->>Remote: Send VAPID encrypted push request
        alt Remote returns 200/201 OK
            Remote-->>PushSvc: Success
            PushSvc-->>API: true (sentCount++)
        else Remote returns 410 Gone / 404 Not Found
            Remote-->>PushSvc: 410 Gone
            PushSvc-->>API: throw WebPushSubscriptionExpiredException
            Note over API: Catch exception; record in staleSubscriptions list
        else Transient Network Failure
            PushSvc-->>API: Exception (log warning, continue loop)
        end
    end

    alt staleSubscriptions.Count > 0
        API->>DB: RemoveRange(staleSubscriptions)
        API->>DB: SaveChangesAsync()
        API->>DB: Count remaining subscriptions
        opt remaining == 0
            API->>DB: user.IsPushEnabled = false; user.UpdatedAt = UtcNow
            API->>DB: SaveChangesAsync()
        end
    end

    alt sentCount > 0
        API-->>Client: 200 OK { success: true, sent, total, stalePurged }
    else sentCount == 0 && stalePurged > 0
        API-->>Client: 400 Bad Request { code: "PUSH_SUBSCRIPTION_EXPIRED" }
    else sentCount == 0
        API-->>Client: 400 Bad Request { code: "PUSH_DELIVERY_FAILED" }
    end
```

#### Detailed Endpoint Handler Implementation
```csharp
group.MapPost("/push/test", async (
    ClaimsPrincipal userClaims,
    TechDailyDbContext db,
    IWebPushService webPushService,
    ILoggerFactory loggerFactory,
    CancellationToken ct) =>
{
    var logger = loggerFactory.CreateLogger("TechDaily.Api.Notifications");
    var userId = GetCurrentUserId(userClaims);
    if (!userId.HasValue) return Results.Unauthorized();

    var subscriptions = await db.UserPushSubscriptions
        .Where(s => s.UserId == userId.Value && !s.IsDeleted)
        .ToListAsync(ct);

    if (subscriptions.Count == 0)
    {
        return Results.BadRequest(new
        {
            code = "PUSH_NO_SUBSCRIPTIONS",
            error = "No active push subscriptions found for this device.",
            details = (object?)null
        });
    }

    var payload = new PushNotificationPayload(
        "TechDaily Test Push 🚀",
        "Web Push notifications are successfully configured and active!",
        "/today",
        "techdaily-test"
    );

    var sentCount = 0;
    var staleSubscriptions = new List<UserPushSubscription>();

    foreach (var sub in subscriptions)
    {
        try
        {
            var ok = await webPushService.SendNotificationAsync(
                sub.Endpoint, sub.P256dh, sub.Auth, payload, ct);
            if (ok) sentCount++;
        }
        catch (WebPushSubscriptionExpiredException ex)
        {
            logger.LogWarning(ex, "Stale push subscription detected during test dispatch: {Endpoint}", sub.Endpoint);
            staleSubscriptions.Add(sub);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send test push notification to {Endpoint}", sub.Endpoint);
        }
    }

    // Purge expired subscriptions atomically
    if (staleSubscriptions.Count > 0)
    {
        db.UserPushSubscriptions.RemoveRange(staleSubscriptions);
        await db.SaveChangesAsync(ct);

        var remainingCount = await db.UserPushSubscriptions
            .CountAsync(s => s.UserId == userId.Value && !s.IsDeleted, ct);

        if (remainingCount == 0)
        {
            var user = await db.Users.FindAsync([userId.Value], ct);
            if (user != null)
            {
                user.IsPushEnabled = false;
                user.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }
    }

    if (sentCount == 0 && staleSubscriptions.Count == subscriptions.Count)
    {
        return Results.BadRequest(new
        {
            code = "PUSH_SUBSCRIPTION_EXPIRED",
            error = "All push notification subscriptions for this device have expired. Please toggle notifications off and on to renew your browser subscription.",
            details = new
            {
                sent = 0,
                total = subscriptions.Count,
                stalePurged = staleSubscriptions.Count
            }
        });
    }

    if (sentCount == 0)
    {
        return Results.BadRequest(new
        {
            code = "PUSH_DELIVERY_FAILED",
            error = "Failed to deliver test push notification to registered devices.",
            details = new
            {
                sent = 0,
                total = subscriptions.Count,
                stalePurged = staleSubscriptions.Count
            }
        });
    }

    return Results.Ok(new
    {
        success = true,
        sent = sentCount,
        total = subscriptions.Count,
        stalePurged = staleSubscriptions.Count
    });
})
.RequireAuthorization()
.WithName("TestPushNotification")
.WithSummary("Sends an immediate test push notification and prunes stale subscriptions.");
```

---

### 2. API Response Contracts & Error Envelopes

| Condition | HTTP Status | Response Payload |
|---|---|---|
| **No subscriptions** | `400 Bad Request` | `{"code": "PUSH_NO_SUBSCRIPTIONS", "error": "No active push subscriptions found for this device.", "details": null}` |
| **All subscriptions expired** | `400 Bad Request` | `{"code": "PUSH_SUBSCRIPTION_EXPIRED", "error": "All push notification subscriptions for this device have expired...", "details": {"sent": 0, "total": 1, "stalePurged": 1}}` |
| **Delivery failed (non-expiry)** | `400 Bad Request` | `{"code": "PUSH_DELIVERY_FAILED", "error": "Failed to deliver test push notification to registered devices.", "details": {"sent": 0, "total": 1, "stalePurged": 0}}` |
| **Successful delivery ($\ge 1$ sent)** | `200 OK` | `{"success": true, "sent": 1, "total": 1, "stalePurged": 0}` |
| **Partial delivery (1 sent, 1 purged)** | `200 OK` | `{"success": true, "sent": 1, "total": 2, "stalePurged": 1}` |

---

### 3. Client Error Resolution Precedence in `useApiError.ts`

Currently, `useApiError.ts` checks `responseData.error` before checking `api_errors.${code}`:
```typescript
// Legacy flow in useApiError.ts:
if (typeof responseData.error === 'string') {
  return responseData.error // <--- Returns raw English before checking translation!
}
const code = responseData.code ...
if (code && te(`api_errors.${code}`)) {
  return t(`api_errors.${code}`)
}
```

#### Refactored Flow:
We invert the evaluation order so that machine-readable error codes are resolved against the active locale dictionary **first**:

```typescript
// 1. Check for machine-readable error code FIRST against i18n
const code =
  (typeof responseData?.code === 'string' ? responseData.code : undefined) ||
  (typeof errorObj?.code === 'string' ? errorObj.code : undefined)

if (code && te(`api_errors.${code}`)) {
  return t(`api_errors.${code}`)
}

// 2. Fall back to RFC 7807 problem details or developer-facing error string
if (responseData && typeof responseData === 'object') {
  if (typeof responseData.title === 'string' && typeof responseData.detail === 'string') {
    return `${responseData.title}: ${responseData.detail}`
  }
  if (typeof responseData.detail === 'string') {
    return responseData.detail
  }
  if (typeof responseData.error === 'string') {
    return responseData.error
  }
}
```

This guarantees:
1. When the backend returns `{ "code": "PUSH_SUBSCRIPTION_EXPIRED", "error": "..." }`, Vietnamese users receive the translated Vietnamese message.
2. English users receive the English translation.
3. Unmapped codes fall back gracefully to `responseData.error` or the caller-provided `fallbackKey`.

---

### 4. Frontend Integration in `settings.vue` & `useWebPush.ts`

#### Composable Type Expansion (`useWebPush.ts`)
```typescript
interface TestPushResponse {
  success: boolean
  sent: number
  total: number
  stalePurged?: number
}

async function sendTestPush(): Promise<TestPushResponse> {
  isLoading.value = true
  error.value = null
  try {
    const res = await api.post<TestPushResponse>('/api/v1/notifications/push/test')
    return res
  } catch (err: unknown) {
    const msg = formatError(err, 'settings.web_push_test_error')
    error.value = msg
    throw err
  } finally {
    isLoading.value = false
  }
}
```

#### Settings Page Action (`settings.vue`)
```typescript
const { formatError } = useApiError()

async function handleSendTestPush() {
  isSendingTest.value = true
  try {
    const res = await sendTestPush()
    if (res.sent === 0) {
      toast.warning(t('settings.web_push_test_zero_sent'))
    } else {
      toast.success(t('settings.web_push_test_success'))
    }
  } catch (err: unknown) {
    // Extract error code if present
    const errObj = typeof err === 'object' && err !== null ? (err as Record<string, unknown>) : null
    const responseData = (errObj?.data || (errObj?.response as Record<string, unknown> | undefined)?._data) as Record<string, unknown> | undefined
    const code = responseData?.code || errObj?.code

    if (code === 'PUSH_SUBSCRIPTION_EXPIRED') {
      toast.error(t('settings.web_push_test_expired'), 8000)
    } else {
      toast.error(formatError(err, 'settings.web_push_test_error'))
    }
  } finally {
    isSendingTest.value = false
  }
}
```

---

### 5. Internationalization (i18n) Dictionary Additions

#### Vietnamese (`frontend/i18n/locales/vi.json`)
```json
{
  "settings": {
    "web_push_test_error": "Không thể gửi thông báo thử. Vui lòng thử lại sau.",
    "web_push_test_expired": "Đăng ký thông báo trên trình duyệt của bạn đã hết hạn hoặc bị hủy. Vui lòng tắt và bật lại thông báo để làm mới mã đăng ký.",
    "web_push_test_zero_sent": "Không có thông báo nào được gửi đến thiết bị. Vui lòng kiểm tra lại quyền thông báo trên trình duyệt."
  },
  "api_errors": {
    "PUSH_SUBSCRIPTION_EXPIRED": "Đăng ký thông báo đẩy đã hết hạn. Vui lòng tắt và bật lại thông báo để làm mới.",
    "PUSH_NO_SUBSCRIPTIONS": "Không tìm thấy thiết bị nào được đăng ký nhận thông báo.",
    "PUSH_DELIVERY_FAILED": "Không thể gửi thông báo thử tới trình duyệt của bạn. Vui lòng thử lại."
  }
}
```

#### English (`frontend/i18n/locales/en.json`)
```json
{
  "settings": {
    "web_push_test_error": "Failed to send test push notification. Please try again later.",
    "web_push_test_expired": "Your browser push subscription has expired or was revoked. Please toggle notifications off and on to renew your subscription.",
    "web_push_test_zero_sent": "No push notifications could be delivered. Please verify your browser notification permissions."
  },
  "api_errors": {
    "PUSH_SUBSCRIPTION_EXPIRED": "Push notification subscription has expired. Please toggle notifications off and on to re-subscribe.",
    "PUSH_NO_SUBSCRIPTIONS": "No active push subscriptions found for this device.",
    "PUSH_DELIVERY_FAILED": "Failed to deliver test push notification to your device. Please try again."
  }
}
```

---

### 6. Automated Unit Testing Strategy

#### Backend Integration Tests (`backend/tests/TechDaily.Tests/Api/NotificationEndpointsTests.cs`)
1. **`TestPushNotification_WithValidSubscription_DispatchesAndReturnsOk`**
   - Seed user with 1 active subscription.
   - Configure `MockWebPushService` to return `true`.
   - Assert `200 OK`, `sent == 1`, `total == 1`, `stalePurged == 0`.
2. **`TestPushNotification_WithExpiredSubscription_PurgesStaleAndReturnsExpired`**
   - Seed user with 1 active subscription.
   - Configure `MockWebPushService` to throw `WebPushSubscriptionExpiredException`.
   - Assert `400 Bad Request`, `code == "PUSH_SUBSCRIPTION_EXPIRED"`.
   - Assert `UserPushSubscriptions` count is 0 in database.
   - Assert `User.IsPushEnabled` is set to `false`.
3. **`TestPushNotification_WithMixedSubscriptions_DeliversActiveAndPurgesExpired`**
   - Seed user with 2 subscriptions (`ep1` active, `ep2` expired).
   - Configure `MockWebPushService` to return `true` for `ep1` and throw for `ep2`.
   - Assert `200 OK`, `sent == 1`, `total == 2`, `stalePurged == 1`.
   - Assert only `ep1` remains in database; `User.IsPushEnabled` remains `true`.
4. **`TestPushNotification_WithNoSubscriptions_ReturnsBadRequest`**
   - Authenticated user with 0 subscriptions.
   - Assert `400 Bad Request`, `code == "PUSH_NO_SUBSCRIPTIONS"`.

#### Frontend Composable Tests (`frontend/tests/composables/useApiError.spec.ts`)
1. **`resolves api_errors.<CODE> before responseData.error`**
   - Provide `{ data: { code: 'PUSH_SUBSCRIPTION_EXPIRED', error: 'English raw' } }`.
   - Assert translated `api_errors.PUSH_SUBSCRIPTION_EXPIRED` is returned, not `'English raw'`.
2. **`falls back to responseData.error if code is unmapped in api_errors`**
   - Provide `{ data: { code: 'UNMAPPED_CODE', error: 'Specific backend error' } }`.
   - Assert `'Specific backend error'` is returned.

#### Frontend Composable Tests (`frontend/tests/composables/useWebPush.spec.ts`)
1. **`sendTestPush returns full response payload`**
   - Mock API post returning `{ success: true, sent: 1, total: 1, stalePurged: 0 }`.
   - Assert returned structure contains `sent`, `total`, and `stalePurged`.

---

## Risks / Trade-offs

| Risk / Trade-off | Evaluation | Mitigation Strategy |
|---|---|---|
| **Purging valid endpoints on transient failure** | High severity if valid users lose subscriptions. | Strict exception classification: only `WebPushSubscriptionExpiredException` (HTTP 410 Gone / 404 Not Found) triggers deletion. Network timeouts and other HTTP errors throw general exceptions, which are logged and skipped without deletion. |
| **Race conditions with background worker** | Low risk; worker and endpoint execute independently. | PostgreSQL row-level locks and EF Core `RemoveRange` are atomic and idempotent. |
| **Breaking existing API contracts** | None; `POST /api/v1/notifications/push/test` is an internal endpoint consumed solely by `settings.vue`. | Retains `success`, `sent`, and `total` properties on successful 200 responses, adding non-breaking `stalePurged`. |
