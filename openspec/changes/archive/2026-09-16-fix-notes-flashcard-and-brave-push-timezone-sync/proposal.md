# Proposal: Fix Notes Flashcard Creation, Handle Brave Push Service, and Auto-Sync Timezone

## Title
Fix Notes Flashcard Creation, Handle Brave Push Service, and Auto-Sync Timezone

## Context & Problem Statement

TechDaily provides engineers with a unified learning and retention experience, encompassing reading notes, flashcard generation via SuperMemo SM-2, and personalized Web Push notifications for study sessions and streak preservation. However, real-world usage and automated browser testing have surfaced three critical issues spanning Vue lifecycle conformance, browser-specific Web Push restrictions, and timezone preference synchronization:

### 1. Vue Composition API Lifecycle Violation in Notes Flashcard Creation
In `frontend/pages/notes.vue`, users can generate active recall flashcards from reading highlights by clicking the "Flashcard SM-2" button. However, clicking this button immediately throws an unhandled client-side runtime exception before any HTTP network request is dispatched:
```text
[vue-i18n] Not found injection "vue-i18n"
```
**Root Cause:** The async click handler `handleCreateFlashcard(highlightId)` attempts to read the active locale via an inline call:
```typescript
const localeVal = (useI18n().locale.value as string) || 'en'
```
Under Vue 3 and `@nuxtjs/i18n`, `useI18n()` relies on Vue's `inject()` dependency injection mechanism, which requires an active component instance. In Vue's Composition API, `inject()` is strictly bound to synchronous execution during component setup (`setup()` / top-level `<script setup>`). Invoking composables like `useI18n()` inside asynchronous event handlers or after an `await` boundary executes outside Vue's active instance context, causing `inject()` to return `undefined` and throw an unhandled injection error.

### 2. Confusing Push Service Failure on Brave and Privacy Browsers
On Brave browser (and privacy-hardened Chromium variants like Ungoogled Chromium), when a user toggles "Enable Web Push Notifications" in `/settings`, the browser prompts for notification permission (which the user grants), but the subsequent call to `registration.pushManager.subscribe(...)` fails with:
```text
DOMException: Registration failed - push service error
```
**Root Cause:** Brave disables "Use Google services for push messaging" by default to protect user privacy against Firebase Cloud Messaging (FCM) telemetry. Without this setting enabled, the underlying Chromium push service daemon refuses to negotiate push subscriptions with Google FCM endpoints.

**Current Experience Defect:** The frontend `useWebPush.ts` composable catches this error generically and throws `DOMException: Registration failed - push service error` directly to `settings.vue`, which presents a raw, cryptic error toast to the user:
```text
Registration failed - push service error
```
Users have no indication of why push subscription failed or how to fix it. There is no detection of the Brave environment or the specific push service error, and no actionable guidance directing users to `brave://settings/privacy`.

### 3. Friction in Timezone Synchronization
TechDaily's Web Push background worker (`DailyPushNotificationWorker`) evaluates study reminders and streak preservation alerts every 15 minutes based on the user's localized `TimeZone` (e.g. `"Asia/Ho_Chi_Minh"`, `"America/New_York"`).

**Current Limitation:** `Users.TimeZone` can currently only be configured through a manual "Save Schedule" or profile update form (`PUT /api/v1/user/profile`). In practice, software engineers expect modern web applications to automatically synchronize their timezone when granting notification permissions. Requiring a separate manual save step leads to timezone mismatch, where push subscriptions default to `"UTC"`, causing study reminders to fire at inappropriate local times (e.g., in the middle of the night).

---

## Proposed Solution

We propose a targeted, cohesive three-part solution addressing each defect cleanly without introducing architectural overhead:

```
┌───────────────────────────────────────────────────────────────────────────┐
│                      REMEDY ARCHITECTURE & FLOWS                          │
│                                                                           │
│  Part 1: Vue Composition Lifecycle Conformity                            │
│  [notes.vue: <script setup>]                                              │
│     ├── Top-Level: const { t, locale } = useI18n()  (Synchronous context) │
│     └── handleCreateFlashcard: reads locale.value (Zero runtime crash)    │
│                                                                           │
│  Part 2: Brave & Push Service Guard with Actionable Guidance              │
│  [useWebPush.ts: subscribeUser()]                                         │
│     ├── Catch DOMException ("push service error" / Brave detection)       │
│     └── Emit localized guidance to enable "Google services for push"     │
│         at brave://settings/privacy (EN & VI i18n support)                │
│                                                                           │
│  Part 3: Auto-Sync Timezone on Push Subscription (Option A)               │
│  [Browser: useWebPush.ts]                                                 │
│     └── POST /api/v1/notifications/push/subscribe                         │
│         payload: { endpoint, keys, userAgent, timeZone }                  │
│                     │                                                     │
│                     ▼                                                     │
│  [Backend: SubscribePushHandler]                                          │
│     └── Persist user.TimeZone = request.TimeZone.Trim()                   │
│         (Validated & synced immediately with notification opt-in)         │
└───────────────────────────────────────────────────────────────────────────┘
```

### Part 1: Lifecycle-Compliant Composable Usage in Notes
- Refactor `frontend/pages/notes.vue` to destructure `locale` alongside `t` at the top level of `<script setup>`:
  ```typescript
  const { t, locale } = useI18n()
  ```
- In `handleCreateFlashcard`, reference `locale.value` directly rather than invoking `useI18n()` inside the asynchronous click callback.
- Eliminates the injection crash completely, ensuring smooth SM-2 flashcard creation from highlight notes.

### Part 2: Brave Push Service Error Detection & Actionable i18n Guidance
- Enhance `frontend/composables/useWebPush.ts` with error classification heuristics for push service blockers:
  - Detect Brave browser environment via `(navigator as any).brave && typeof (navigator as any).brave.isBrave === 'function'`.
  - Intercept push registration errors containing `"push service error"` or `"Registration failed"`.
  - Classify the error into a structured error state (`isBraveBlocked` / `BRAVE_PUSH_SERVICE_DISABLED`).
- Add localized translation keys in `frontend/i18n/locales/en.json` and `vi.json`:
  - English: `"Brave browser requires enabling 'Use Google services for push messaging' at brave://settings/privacy to receive push notifications."`
  - Vietnamese: `"Trình duyệt Brave yêu cầu bật 'Sử dụng dịch vụ của Google cho tin nhắn đẩy' tại brave://settings/privacy để nhận thông báo đẩy."`
- In `frontend/pages/settings.vue`, display this actionable message prominently in an alert or informative toast when this specific error occurs, guiding users directly to resolve the browser restriction.

### Part 3: Auto-Sync Timezone on Push Subscription
- **Backend Request Expansion:** Update `SubscribePushRequest` in `NotificationEndpoints.cs` to include an optional `string? TimeZone` property.
- **Backend Persistence:** In the `SubscribePush` endpoint handler, when `request.TimeZone` is provided, validate/sanitize it against `DailyPushNotificationWorker.ResolveTimeZone(timeZone)` and persist `user.TimeZone = request.TimeZone.Trim()`, updating `user.UpdatedAt = DateTime.UtcNow`.
- **Frontend Transmission:** In `frontend/composables/useWebPush.ts`, automatically resolve the client browser's IANA timezone using `Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC'` and include it in the `POST /api/v1/notifications/push/subscribe` payload.

---

## Capabilities

### Modified Capabilities
- `reader`: Conformance to Vue 3 Composition API lifecycle rules in `notes.vue`, ensuring that highlight-to-flashcard conversion runs reliably without injection errors.
- `core-platform`:
  - Automated timezone synchronization upon browser Web Push subscription (`POST /api/v1/notifications/push/subscribe`).
  - Intelligent handling and actionable user guidance for Brave and privacy-hardened Chromium push service restrictions.

---

## Value & Impact

| Layer | Changes & Enhancements | Concrete Impact |
|---|---|---|
| **Frontend Notes** | Destructure `locale` at top-level `<script setup>` in `notes.vue`. | Eliminates `[vue-i18n] Not found injection` runtime crash; 1-click flashcard generation functions as designed. |
| **Frontend Push Composable** | Brave browser detection, error classification for `push service error`, and auto-supplying client timezone in `useWebPush.ts`. | Replaces raw `DOMException` error with clear, actionable user instructions; transmits detected timezone automatically. |
| **Frontend i18n** | Add English and Vietnamese locale keys for Brave push service instructions. | 100% i18n coverage conforming to platform multi-language standards. |
| **Backend Notifications** | Add `TimeZone` to `SubscribePushRequest` and persist to `User.TimeZone` in `NotificationEndpoints.cs`. | Zero-friction timezone synchronization; ensures push reminders fire accurately at user's local study hour. |
| **Testing** | Unit tests for composable error handling, Brave heuristics, and backend push subscription with timezone sync. | High regression resistance and verified browser interoperability. |
