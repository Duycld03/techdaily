# Tasks: Resilient Web Push Test & Localized Error Handling

## Phase 1: Backend API Exception Handling & Stale Cleanup

- [x] 1.1 In `backend/src/TechDaily.Api/Endpoints/NotificationEndpoints.cs`, update `POST /api/v1/notifications/push/test` parameter list to inject `ILoggerFactory` and filter active subscriptions with `!s.IsDeleted`.
- [x] 1.2 Standardize the initial subscription existence check in `POST /api/v1/notifications/push/test` to return `HTTP 400 Bad Request` with machine-readable envelope `{ code = "PUSH_NO_SUBSCRIPTIONS", error = "No active push subscriptions found for this device.", details = (object?)null }`.
- [x] 1.3 Add exception isolation inside the subscription `foreach` dispatch loop in `NotificationEndpoints.cs`:
  - Catch `WebPushSubscriptionExpiredException`: record subscription in `staleSubscriptions` list and log diagnostic warning with endpoint details.
  - Catch general `Exception`: log structured warning for transient dispatch failure and continue loop without aborting other endpoints.
- [x] 1.4 Implement transactional purge of stale subscriptions in `POST /api/v1/notifications/push/test`:
  - Call `db.UserPushSubscriptions.RemoveRange(staleSubscriptions)` and commit via `await db.SaveChangesAsync(ct)`.
  - Count remaining active subscriptions for `userId.Value`. If zero remain, set `user.IsPushEnabled = false`, update `user.UpdatedAt = DateTime.UtcNow`, and save changes.
- [x] 1.5 Standardize response envelopes in `POST /api/v1/notifications/push/test`:
  - If `sentCount == 0 && staleSubscriptions.Count == subscriptions.Count`, return `HTTP 400 Bad Request` with `{ code = "PUSH_SUBSCRIPTION_EXPIRED", error = "...", details = { sent = 0, total, stalePurged } }`.
  - If `sentCount == 0`, return `HTTP 400 Bad Request` with `{ code = "PUSH_DELIVERY_FAILED", error = "...", details = { sent = 0, total, stalePurged } }`.
  - If `sentCount > 0`, return `HTTP 200 OK` with `{ success = true, sent = sentCount, total = subscriptions.Count, stalePurged = staleSubscriptions.Count }`.

---

## Phase 2: Frontend Error Formatting & i18n Localization

- [x] 2.1 Add missing test push and API error keys to `frontend/i18n/locales/vi.json`:
  - `settings.web_push_test_error`: `"Không thể gửi thông báo thử. Vui lòng thử lại sau."`
  - `settings.web_push_test_expired`: `"Đăng ký thông báo trên trình duyệt của bạn đã hết hạn hoặc bị hủy. Vui lòng tắt và bật lại thông báo để làm mới mã đăng ký."`
  - `settings.web_push_test_zero_sent`: `"Không có thông báo nào được gửi đến thiết bị. Vui lòng kiểm tra lại quyền thông báo trên trình duyệt."`
  - `api_errors.PUSH_SUBSCRIPTION_EXPIRED`: `"Đăng ký thông báo đẩy đã hết hạn. Vui lòng tắt và bật lại thông báo để làm mới."`
  - `api_errors.PUSH_NO_SUBSCRIPTIONS`: `"Không tìm thấy thiết bị nào được đăng ký nhận thông báo."`
  - `api_errors.PUSH_DELIVERY_FAILED`: `"Không thể gửi thông báo thử tới trình duyệt của bạn. Vui lòng thử lại."`
- [x] 2.2 Add corresponding English translation keys to `frontend/i18n/locales/en.json` ensuring 100% key parity with `vi.json`.
- [x] 2.3 In `frontend/composables/useApiError.ts`, refactor error code resolution order so that `api_errors.<CODE>` dictionary lookups are evaluated before falling back to `responseData.error` or caller `fallbackKey`.
- [x] 2.4 In `frontend/composables/useWebPush.ts`, update `sendTestPush()` return type to `Promise<{ success: boolean; sent: number; total: number; stalePurged?: number }>`.
- [x] 2.5 In `frontend/pages/settings.vue`, refactor `handleSendTestPush`:
  - Integrate `useApiError.formatError(err, 'settings.web_push_test_error')`.
  - Check `res.sent === 0` on successful HTTP response and display `settings.web_push_test_zero_sent` warning toast.
  - On catch of `PUSH_SUBSCRIPTION_EXPIRED`, display prominent `settings.web_push_test_expired` toast with extended duration (8000ms) guiding the user to toggle notifications off and on.

---

## Phase 3: Automated Unit Testing & Verification

- [x] 3.1 In `backend/tests/TechDaily.Tests/Api/NotificationEndpointsTests.cs`, implement test cases for `POST /api/v1/notifications/push/test`:
  - `TestPushNotification_WithValidSubscription_DispatchesAndReturnsOk` verifying `200 OK` and `sent = 1, stalePurged = 0`.
  - `TestPushNotification_WithExpiredSubscription_PurgesStaleAndReturnsExpired` verifying `400 Bad Request` with `code = "PUSH_SUBSCRIPTION_EXPIRED"`, record deletion from database, and `user.IsPushEnabled = false`.
  - `TestPushNotification_WithMixedSubscriptions_DeliversActiveAndPurgesExpired` verifying `200 OK` with `sent = 1, total = 2, stalePurged = 1`, deletion of expired row, retention of valid row, and `user.IsPushEnabled = true`.
  - `TestPushNotification_WithNoSubscriptions_ReturnsBadRequest` verifying `400 Bad Request` with `code = "PUSH_NO_SUBSCRIPTIONS"`.
- [x] 3.2 In `frontend/tests/composables/useApiError.spec.ts`, add unit test verifying that machine-readable `api_errors.<CODE>` translations take precedence over raw `responseData.error` strings.
- [x] 3.3 In `frontend/tests/composables/useWebPush.spec.ts`, update test for `sendTestPush` to assert receipt of `{ success: true, sent: 1, total: 1, stalePurged: 0 }`.
- [x] 3.4 Execute full backend and frontend test suites (`dotnet test` and `vitest run`) to confirm all new tests pass with zero regressions.
