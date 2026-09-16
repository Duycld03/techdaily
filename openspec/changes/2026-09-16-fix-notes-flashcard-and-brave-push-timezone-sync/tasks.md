# Tasks: Fix Notes Flashcard Creation, Handle Brave Push Service, and Auto-Sync Timezone

## Phase 1: Fix Notes Flashcard Composable Lifecycle (`frontend/pages/notes.vue`)
- [x] 1.1 Refactor top-level composable imports and destructuring in `frontend/pages/notes.vue`:
  - Update `const { t } = useI18n()` to `const { t, locale } = useI18n()`.
  - Ensure `locale` is captured synchronously within the `<script setup>` context.
- [x] 1.2 Update `handleCreateFlashcard` handler in `frontend/pages/notes.vue`:
  - Replace `const localeVal = (useI18n().locale.value as string) || 'en'` with `const localeVal = (locale.value as string) || 'en'`.
  - Remove all inline `useI18n()` invocations from asynchronous execution blocks.
- [x] 1.3 Manually verify that clicking "Flashcard SM-2" in `/notes` invokes `reviewStore.createCardFromHighlight` without throwing `[vue-i18n] Not found injection "vue-i18n"`.

## Phase 2: Brave Push Service Error Handling & Actionable Guidance
- [x] 2.1 Add localized strings for Brave push service guidance in `frontend/i18n/locales/en.json` under `settings`:
  - Add `brave_push_service_blocked`: `"Brave requires enabling 'Use Google services for push messaging' at brave://settings/privacy to receive notifications."`
  - Add `brave_push_service_instruction`: `"Open a new tab, navigate to brave://settings/privacy, enable 'Use Google services for push messaging', and restart Brave."`
- [x] 2.2 Add localized strings for Brave push service guidance in `frontend/i18n/locales/vi.json` under `settings`:
  - Add `brave_push_service_blocked`: `"Brave yêu cầu bật 'Sử dụng dịch vụ của Google cho tin nhắn đẩy' tại brave://settings/privacy để nhận thông báo."`
  - Add `brave_push_service_instruction`: `"Mở tab mới, truy cập brave://settings/privacy, bật 'Sử dụng dịch vụ của Google cho tin nhắn đẩy' và khởi động lại Brave."`
- [x] 2.3 Implement error classification helper in `frontend/composables/useWebPush.ts`:
  - Define `isBravePushServiceError(err: unknown): boolean` checking for `navigator.brave` and error messages containing `"push service error"` or `"Registration failed"`.
- [x] 2.4 Update `subscribeUser` in `frontend/composables/useWebPush.ts`:
  - Catch `DOMException` during `reg.pushManager.subscribe(...)`.
  - When classified as a Brave push service error, set `error.value = 'settings.brave_push_service_blocked'` (or a dedicated structured error) and throw a descriptive, localized error rather than the raw `DOMException`.
- [x] 2.5 Update error presentation in `frontend/pages/settings.vue`:
  - In `handleTogglePush()`, catch the classified Brave error and render `t('settings.brave_push_service_blocked')` in an error toast or alert banner.

## Phase 3: Auto-Sync Timezone on Push Subscription
- [x] 3.1 Update `SubscribePushRequest` record in `backend/src/TechDaily.Api/Endpoints/NotificationEndpoints.cs`:
  - Add `string? TimeZone = null` to the record definition.
- [x] 3.2 Update `SubscribePush` endpoint handler in `backend/src/TechDaily.Api/Endpoints/NotificationEndpoints.cs`:
  - Check if `request.TimeZone` is provided and non-empty.
  - Resolve and validate timezone using `DailyPushNotificationWorker.ResolveTimeZone(request.TimeZone.Trim())`.
  - Update `user.TimeZone = resolved.Id` and `user.UpdatedAt = DateTime.UtcNow`.
  - Ensure changes are saved transactionally with `UserPushSubscription` record.
- [x] 3.3 Update `subscribeUser` in `frontend/composables/useWebPush.ts`:
  - Accept optional `preferredTimeZone?: string` parameter.
  - Automatically detect client IANA timezone via `preferredTimeZone || Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC'`.
  - Include `timeZone` in the `POST /api/v1/notifications/push/subscribe` request payload.
- [x] 3.4 Update push subscription invocation in `frontend/pages/settings.vue`:
  - Pass `timeZone.value` or allow auto-detection during `subscribeUser()`.
  - Confirm `user.TimeZone` in `profileStore` is updated upon subscription success.

## Phase 4: Automated Testing & Verification
- [x] 4.1 Add or update frontend unit test in `frontend/tests/pages/notes.spec.ts` (or create targeted spec):
  - Verify that clicking "Flashcard SM-2" reads `locale.value` from top-level setup and triggers flashcard creation without runtime injection exceptions.
- [x] 4.2 Update frontend unit tests in `frontend/tests/composables/useWebPush.spec.ts`:
  - Test that `subscribeUser` includes `timeZone` in `api.post('/api/v1/notifications/push/subscribe')`.
  - Test that `subscribeUser` catches push service errors when Brave browser environment is simulated, properly classifying the error.
- [x] 4.3 Add backend integration/unit tests in `tests/TechDaily.Tests`:
  - Test `POST /api/v1/notifications/push/subscribe` with a valid IANA timezone (e.g. `"Asia/Ho_Chi_Minh"`) updates `User.TimeZone`.
  - Test `POST /api/v1/notifications/push/subscribe` with `null` or whitespace timezone preserves the existing `User.TimeZone`.
  - Test `POST /api/v1/notifications/push/subscribe` with an unknown/invalid timezone falls back to `"UTC"`.
- [x] 4.4 Run full automated verification:
  - Run frontend test suite (`npm test`).
  - Run backend test suite (`dotnet test`).
  - Confirm clean builds with zero compiler warnings or lint errors.
