# Proposal: Resilient Web Push Test & Localized Error Handling

## Why

On production (`https://techdaily.duckdns.org/settings`), when an authenticated user clicks the **"Gửi Thông Báo Thử"** (Send Test Notification) button, the application frequently displays a raw, unlocalized English error toast:
> *"An unexpected error occurred while processing your request."*

This degrades the engineer user experience and prevents users from verifying their browser notification setup. An in-depth investigation revealed three compounding root causes spanning the backend endpoint, frontend error handling, and translation dictionaries:

### 1. Backend Unhandled Exception & Stale Subscription Accumulation (`NotificationEndpoints.cs`)
- In `POST /api/v1/notifications/push/test` (`backend/src/TechDaily.Api/Endpoints/NotificationEndpoints.cs`, lines 146–180), `webPushService.SendNotificationAsync(...)` is called sequentially inside a `foreach` loop over all user push subscriptions without any `try-catch` block.
- When a user's browser push endpoint expires, is revoked by browser state reset, or becomes invalid (HTTP 410 Gone / HTTP 404 Not Found from upstream push services like Google FCM, Apple Web Push, or Mozilla Autopush), `WebPushService.SendNotificationAsync` re-throws a `WebPushSubscriptionExpiredException`.
- While the background worker (`DailyPushNotificationWorker.cs`) catches `WebPushSubscriptionExpiredException` and prunes stale records, `NotificationEndpoints.cs` lets it escape unhandled.
- The uncaught exception bubbles up to the ASP.NET Core global exception handling middleware, returning an unhandled `HTTP 500 Internal Server Error` formatted as an RFC 7807 ProblemDetails envelope (`{ "title": "An unexpected error occurred while processing your request.", "status": 500 }`).
- Because execution halts immediately upon the first expired subscription, any remaining healthy device subscriptions in the loop are never dispatched.
- Stale subscriptions are never deleted from `db.UserPushSubscriptions`, trapping the user in an infinite failure loop whenever they attempt to test notifications.

### 2. Frontend Missing i18n Error Resolution & Unchecked Delivery (`settings.vue`)
- In `frontend/pages/settings.vue` (lines 110–120), `handleSendTestPush` does not utilize the platform's standardized error handling composable (`useApiError.formatError()`).
- Instead, it directly extracts `err instanceof Error ? err.message : 'Failed to send test push.'`. When the backend returns an HTTP 500 ProblemDetails payload, `ofetch` populates `err.message` with the raw English title string, directly leaking it into the localized Vietnamese UI.
- Furthermore, `settings.vue` does not check if `res.sent === 0` (which occurs when all registered endpoints have expired or failed). It simply displays a generic success toast if the HTTP call completes without throwing, or shows a generic error without actionable recovery steps.
- Users are never informed that their browser push token has expired, nor guided to toggle push notifications off and on to re-register a fresh VAPID token with the browser.

### 3. Missing i18n Translation Dictionary Keys (`vi.json` & `en.json`)
- Neither `frontend/i18n/locales/vi.json` nor `frontend/i18n/locales/en.json` contains dedicated translation keys for test push failure modes:
  - `settings.web_push_test_error`
  - `settings.web_push_test_expired`
  - `settings.web_push_test_zero_sent`
  - `api_errors.PUSH_SUBSCRIPTION_EXPIRED`
- Additionally, `useApiError.ts` currently prioritizes the raw developer-facing English `responseData.error` string before checking whether `api_errors.${code}` exists in the i18n dictionary. As a result, even when the backend provides a machine-readable error code like `PUSH_SUBSCRIPTION_EXPIRED`, Vietnamese users would still see the English error message.

---

## What Changes

We propose a cohesive, end-to-end enhancement that makes the Web Push test flow resilient, self-healing, and fully localized:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                   RESILIENT WEB PUSH ARCHITECTURE & FLOW                    │
│                                                                             │
│  [POST /api/v1/notifications/push/test]                                     │
│     │                                                                       │
│     ├── 1. Check Subscriptions                                              │
│     │      └── Count == 0 ➔ 400 Bad Request { code: PUSH_NO_SUBSCRIPTIONS } │
│     │                                                                       │
│     ├── 2. Resilient Dispatch Loop (try-catch per subscription)             │
│     │      ├── WebPushSubscriptionExpiredException ➔ Collect for purging    │
│     │      ├── Exception (transient) ➔ Log warning, continue loop           │
│     │      └── Success ➔ sentCount++                                        │
│     │                                                                       │
│     ├── 3. Transactional Stale Purge                                        │
│     │      ├── db.UserPushSubscriptions.RemoveRange(staleSubscriptions)     │
│     │      └── If remaining == 0 ➔ user.IsPushEnabled = false               │
│     │                                                                       │
│     └── 4. Standardized Response                                            │
│            ├── sentCount > 0 ➔ 200 OK { sent, total, stalePurged }          │
│            └── sentCount == 0 & stalePurged > 0                             │
│                ➔ 400 Bad Request { code: PUSH_SUBSCRIPTION_EXPIRED }        │
│                                                                             │
│  [frontend/pages/settings.vue]                                              │
│     │                                                                       │
│     ├── handleSendTestPush() integrated with useApiError.formatError()      │
│     ├── Actionable guidance toast on PUSH_SUBSCRIPTION_EXPIRED              │
│     ├── Warning toast on res.sent === 0 (zero delivered)                    │
│     └── 100% bilingual parity across vi.json and en.json                    │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1. Backend: Resilient Push Dispatch & Stale Cleanup (`NotificationEndpoints.cs`)
- Wrap the push dispatch call inside the `foreach` loop with dedicated exception handling:
  - Catch `WebPushSubscriptionExpiredException`: record the subscription in a `staleSubscriptions` list for purging and log diagnostic details.
  - Catch general `Exception`: log a structured warning and continue the loop so other valid devices are not blocked.
- After evaluating all subscriptions:
  - If any stale subscriptions were collected, remove them from `db.UserPushSubscriptions` in a single batch.
  - Check if any subscriptions remain for the user. If zero remain, set `user.IsPushEnabled = false; user.UpdatedAt = DateTime.UtcNow;`.
  - Commit changes transactionally via `await db.SaveChangesAsync(ct)`.
- Return standardized machine-readable responses:
  - **No subscriptions**: `Results.BadRequest(new { code = "PUSH_NO_SUBSCRIPTIONS", error = "No active push subscriptions found for this device.", details = (object?)null })`.
  - **All subscriptions expired & purged**: `Results.BadRequest(new { code = "PUSH_SUBSCRIPTION_EXPIRED", error = "All push notification subscriptions for this device have expired. Please toggle notifications off and on to renew your browser subscription.", details = new { sent = 0, total = subscriptions.Count, stalePurged = staleSubscriptions.Count } })`.
  - **Successful dispatch (at least 1 sent)**: `Results.Ok(new { success = true, sent = sentCount, total = subscriptions.Count, stalePurged = staleSubscriptions.Count })`.

### 2. Frontend: Standardized Error Resolution & Actionable Guidance (`settings.vue`)
- Refactor `handleSendTestPush` to invoke `formatError(err, 'settings.web_push_test_error')` from `useApiError`.
- Handle specific error scenarios:
  - If the server reports `PUSH_SUBSCRIPTION_EXPIRED` (or if all subscriptions were purged), show a prominent localized toast (`settings.web_push_test_expired`) informing the user that their browser token expired and instructing them to toggle notifications off and on.
  - If the request succeeds but `res.sent === 0`, display a warning toast (`settings.web_push_test_zero_sent`) instead of an unearned success message.
- Update `useWebPush.ts` `sendTestPush()` return type signature to include `{ success: boolean; sent: number; total: number; stalePurged?: number }`.

### 3. Client Error Resolution Precedence (`useApiError.ts`)
- Refactor `useApiError.ts` so that `api_errors.${code}` translation resolution is evaluated **before** returning the raw English `responseData.error` string.
- This ensures that whenever the backend provides a machine-readable error code with an existing localized translation (such as `PUSH_SUBSCRIPTION_EXPIRED`), the user receives their preferred language rather than fallback English.

### 4. Complete Bilingual Dictionary Coverage (`vi.json` & `en.json`)
- Add comprehensive translations for test push operations:
  - `settings.web_push_test_error`: Localized generic test push error message.
  - `settings.web_push_test_expired`: Localized message explaining token expiration with toggle re-subscription instructions.
  - `settings.web_push_test_zero_sent`: Localized warning when zero notifications could be dispatched.
  - `api_errors.PUSH_SUBSCRIPTION_EXPIRED`: Machine-readable error code translation for expired push subscriptions.
  - `api_errors.PUSH_NO_SUBSCRIPTIONS`: Machine-readable error code translation for missing subscriptions.
  - `api_errors.PUSH_DELIVERY_FAILED`: Machine-readable error code translation for delivery failures.

---

## Capabilities

### Modified Capabilities
- `core-platform`: Extends push notification infrastructure with resilient test dispatch exception handling, automatic stale subscription purging on test send, standardized machine-readable error contracts, and localized frontend settings error resolution with token expiration recovery guidance.

---

## Impact

| Area | Nature of Change | Details |
|---|---|---|
| **Backend API** | Bug fix & resilience | `NotificationEndpoints.cs` catches `WebPushSubscriptionExpiredException`, purges stale subscriptions from `UserPushSubscriptions`, updates `User.IsPushEnabled`, and returns standardized RFC 7807/envelope responses. |
| **Frontend Settings** | UX & Error Resolution | `frontend/pages/settings.vue` integrates `useApiError`, inspects delivery counts, and displays actionable token renewal guidance. |
| **Frontend Composables** | Type safety & error priority | `frontend/composables/useWebPush.ts` updates `sendTestPush` signature. `frontend/composables/useApiError.ts` prioritizes `api_errors.${code}` over raw `responseData.error`. |
| **Internationalization** | Bilingual completeness | `frontend/i18n/locales/vi.json` and `en.json` receive 6 new keys covering test push errors and API codes. |
| **Database** | Hygiene & consistency | Eliminates stale, invalid endpoints from `UserPushSubscriptions` automatically during test dispatches. |
| **Tests** | Quality assurance | Adds xUnit integration tests in `NotificationEndpointsTests.cs` and Vitest unit tests in `useApiError.spec.ts` and `useWebPush.spec.ts`. |

---

## Risks & Mitigations

1. **Risk:** Upstream push service network glitches (e.g. temporary FCM timeout) mistakenly causing subscription deletion.
   - **Mitigation:** Only `WebPushSubscriptionExpiredException` (triggered strictly by HTTP 404 Not Found or HTTP 410 Gone from upstream push servers) marks a subscription as stale. Transient network errors throw general `HttpRequestException` or `WebPushException` with other status codes, which are logged as warnings and NOT pruned.
2. **Risk:** Concurrency race between `DailyPushNotificationWorker` and `NotificationEndpoints` deleting the same expired subscription.
   - **Mitigation:** EF Core `RemoveRange` generates SQL `DELETE FROM "UserPushSubscriptions" WHERE "Id" IN (...)`. If another process has already deleted the row, EF Core handles it gracefully or the subsequent read simply finds fewer rows.
3. **Risk:** User gets confused when push is automatically disabled after all expired devices are pruned.
   - **Mitigation:** The frontend toast explicitly explains: *"Đăng ký thông báo trên trình duyệt của bạn đã hết hạn. Vui lòng tắt và bật lại thông báo để làm mới."*, providing an immediate, actionable path to re-subscribe.
