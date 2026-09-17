# Core Platform Capability Delta Specification

## MODIFIED Requirements

### Requirement: Stale Push Subscription Auto-Pruning
The Web Push dispatch pipeline SHALL automatically detect and delete revoked or expired push subscription endpoints during both background scheduled dispatches (`DailyPushNotificationWorker`) and user-initiated test dispatches (`POST /api/v1/notifications/push/test`). When all registered push subscriptions for a user are purged as stale, the system SHALL update `User.IsPushEnabled = false` and refresh `User.UpdatedAt`.

#### Scenario: Push service returns HTTP 404 or 410 Gone
- **WHEN** sending a push notification to an endpoint and the push service responds with HTTP 404 (Not Found) or HTTP 410 (Gone) indicating the user unsubscribed or reset browser state
- **THEN** the worker catches the response and immediately deletes that `UserPushSubscription` row from PostgreSQL, preserving database hygiene.

#### Scenario: Push service returns HTTP 404 or 410 Gone during test push dispatch
- **WHEN** an authenticated user invokes `POST /api/v1/notifications/push/test`
- **AND** the upstream push service returns HTTP 410 (Gone) or HTTP 404 (Not Found) throwing `WebPushSubscriptionExpiredException`
- **THEN** the endpoint catches the exception per subscription without propagating an unhandled HTTP 500 error
- **AND** removes the expired subscription rows from `UserPushSubscriptions`
- **AND** sets `User.IsPushEnabled = false` if no active subscriptions remain for that user
- **AND** returns `HTTP 400 Bad Request` with code `PUSH_SUBSCRIPTION_EXPIRED`.

---

### Requirement: Client-Side Dynamic Error Code Localization
The web client (`useApiError` composable) SHALL resolve API error codes against the active i18n locale (`api_errors.<CODE>`). When an error code is present in the locale dictionary, the translated text SHALL take precedence over raw developer-facing English error messages (`responseData.error`). If no match is found, the system SHALL display the provided localized fallback message or generic localized error message.

#### Scenario: API returns AUTH_INVALID_CREDENTIALS with Vietnamese locale
- **WHEN** API responds with `{ "code": "AUTH_INVALID_CREDENTIALS", "error": "Invalid email or password." }` and client locale is `vi`
- **THEN** client renders toast: "Email hoặc mật khẩu không chính xác."

#### Scenario: API returns AUTH_INVALID_CREDENTIALS with English locale
- **WHEN** API responds with `{ "code": "AUTH_INVALID_CREDENTIALS", "error": "Invalid email or password." }` and client locale is `en`
- **THEN** client renders toast: "Invalid email or password."

#### Scenario: API returns error code with matching translation in locale dictionary
- **WHEN** API responds with `{ "code": "PUSH_SUBSCRIPTION_EXPIRED", "error": "Push subscription has expired." }`
- **AND** the active locale is Vietnamese (`vi`)
- **THEN** `formatError` returns the localized message `"Đăng ký thông báo đẩy đã hết hạn. Vui lòng tắt và bật lại thông báo để làm mới."` (`api_errors.PUSH_SUBSCRIPTION_EXPIRED`) instead of the English `error` string.

#### Scenario: API returns error code without translation in locale dictionary
- **WHEN** API responds with `{ "code": "UNKNOWN_ERROR_CODE", "error": "Something went wrong." }`
- **AND** no matching entry exists in `api_errors`
- **THEN** `formatError` falls back to `responseData.error` or the caller-provided `fallbackKey`.

---

## ADDED Requirements

### Requirement: Resilient Test Push Dispatch & Standardized Error Contracts
The endpoint `POST /api/v1/notifications/push/test` SHALL isolate push delivery errors per subscription, automatically purge expired endpoints, update `User.IsPushEnabled = false` when all endpoints are pruned, and return standardized machine-readable envelopes (`code`, `error`, `details`) with `sent`, `total`, and `stalePurged` metrics.

The endpoint SHALL adhere to the following response contract:
1. **No Registered Subscriptions:** If the user has zero registered push subscriptions, the endpoint SHALL return `HTTP 400 Bad Request` with body:
   ```json
   {
     "code": "PUSH_NO_SUBSCRIPTIONS",
     "error": "No active push subscriptions found for this device.",
     "details": null
   }
   ```
2. **All Subscriptions Expired:** If all registered push subscriptions throw `WebPushSubscriptionExpiredException`, the endpoint SHALL purge the expired subscriptions, update `User.IsPushEnabled = false`, and return `HTTP 400 Bad Request` with body:
   ```json
   {
     "code": "PUSH_SUBSCRIPTION_EXPIRED",
     "error": "All push notification subscriptions for this device have expired. Please toggle notifications off and on to renew your browser subscription.",
     "details": {
       "sent": 0,
       "total": 1,
       "stalePurged": 1
     }
   }
   ```
3. **Successful Dispatch:** When at least one push notification is successfully dispatched, the endpoint SHALL return `HTTP 200 OK` with body:
   ```json
   {
     "success": true,
     "sent": 1,
     "total": 1,
     "stalePurged": 0
   }
   ```
4. **Mixed Dispatch (Partial Expiry):** When some subscriptions succeed and some throw `WebPushSubscriptionExpiredException`, the endpoint SHALL purge the expired subscriptions and return `HTTP 200 OK` with body:
   ```json
   {
     "success": true,
     "sent": 1,
     "total": 2,
     "stalePurged": 1
   }
   ```

#### Scenario: User sends test push with valid active subscription
- **WHEN** an authenticated user calls `POST /api/v1/notifications/push/test` with 1 active, valid subscription
- **THEN** the system dispatches the push payload via `IWebPushService`
- **AND** returns `HTTP 200 OK` with `{ "success": true, "sent": 1, "total": 1, "stalePurged": 0 }`.

#### Scenario: User sends test push with expired browser subscription
- **WHEN** an authenticated user calls `POST /api/v1/notifications/push/test` with 1 subscription that returns HTTP 410/404 from the push service
- **THEN** the endpoint catches `WebPushSubscriptionExpiredException`
- **AND** deletes the stale subscription from `UserPushSubscriptions`
- **AND** sets `user.IsPushEnabled = false`
- **AND** returns `HTTP 400 Bad Request` with code `PUSH_SUBSCRIPTION_EXPIRED`.

#### Scenario: User sends test push with multiple subscriptions where one is expired
- **WHEN** an authenticated user has 2 subscriptions (e.g. mobile and desktop), where mobile is expired (HTTP 410) and desktop is active (HTTP 200)
- **THEN** the endpoint purges the mobile subscription from `UserPushSubscriptions`
- **AND** successfully delivers the push notification to the desktop device
- **AND** preserves `user.IsPushEnabled = true` because 1 subscription remains
- **AND** returns `HTTP 200 OK` with `{ "success": true, "sent": 1, "total": 2, "stalePurged": 1 }`.

#### Scenario: User sends test push without any active subscriptions
- **WHEN** an authenticated user calls `POST /api/v1/notifications/push/test` but has zero subscriptions in `UserPushSubscriptions`
- **THEN** the endpoint returns `HTTP 400 Bad Request` with code `PUSH_NO_SUBSCRIPTIONS`.

---

### Requirement: Localized Settings Error Resolution & Token Expiration Guidance
The Settings interface (`frontend/pages/settings.vue`) SHALL use `useApiError.formatError` to resolve API error codes and server failures against the active locale, suppressing raw unlocalized RFC 7807 ProblemDetails strings.

When a push subscription is detected as expired (`PUSH_SUBSCRIPTION_EXPIRED`) or when the backend returns zero delivered notifications (`res.sent === 0`), the UI SHALL display clear, localized guidance prompting the user to toggle notifications off and on to renew their browser push token.

#### Scenario: User clicks test notification and receives successful delivery
- **WHEN** user clicks "Gửi Thông Báo Thử" and `sendTestPush` returns `{ success: true, sent: 1, ... }`
- **THEN** UI displays a success toast using `settings.web_push_test_success`.

#### Scenario: User clicks test notification with expired browser token
- **WHEN** user clicks "Gửi Thông Báo Thử" and the backend returns `PUSH_SUBSCRIPTION_EXPIRED`
- **THEN** UI displays an error or warning toast using `settings.web_push_test_expired`
- **AND** the toast text instructs the user to toggle the notification switch off and on to renew their browser push token.

#### Scenario: User clicks test notification during unhandled server failure
- **WHEN** user clicks "Gửi Thông Báo Thử" and the backend returns HTTP 500
- **THEN** `useApiError.formatError` resolves the error to `settings.web_push_test_error` or `api_errors.SERVER_ERROR`
- **AND** does NOT display the raw English title `"An unexpected error occurred while processing your request."` to a Vietnamese user.

#### Scenario: Test push response indicates zero sent notifications
- **WHEN** `sendTestPush` completes successfully without throwing but returns `sent: 0`
- **THEN** UI displays a warning toast using `settings.web_push_test_zero_sent`
- **AND** does NOT display a misleading success notification.
