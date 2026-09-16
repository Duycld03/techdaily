# Core Platform Capability Delta Specification

## Purpose
Establishes native browser Web Push notification infrastructure (RFC 8291 / RFC 8292 / VAPID), user-configurable study routine and streak preservation schedules, and a 15-minute timezone-aware background dispatch engine.

---

## MODIFIED Requirements

### Requirement: User Profile Management, Route Guards & Security
The user profile endpoints (`GET /api/v1/user/profile`, `PUT /api/v1/user/profile`, `PUT /api/v1/user/change-password`) SHALL support managing user study schedules, streak preservation alert preferences, IANA timezones, and browser push status alongside existing profile properties, protected with strict JWT Bearer authentication, reject unauthenticated requests with `HTTP 401 Unauthorized`, and enforce route middleware guards on protected frontend pages.

#### Scenario: Unauthenticated request to user profile
- **WHEN** unauthenticated client calls `GET /api/v1/user/profile`
- **THEN** system returns `401 Unauthorized`.

#### Scenario: User updates profile settings
- **WHEN** authenticated user sends `PUT /api/v1/user/profile` with target level and learning goals
- **THEN** system updates user profile and returns updated profile DTO.

#### Scenario: User configures personal study schedule and timezone
- **WHEN** an authenticated user submits `PUT /api/v1/user/profile` with `preferredStudyTime: "07:30"`, `streakAlertTime: "21:00"`, `timeZone: "Asia/Ho_Chi_Minh"`, and `isPushEnabled: true`
- **THEN** the system validates the IANA timezone string, persists the preferences on the `User` entity, and returns the updated profile DTO.

#### Scenario: Validation of invalid timezone identifier
- **WHEN** a client submits an invalid or unrecognized timezone string (e.g., `"Invalid/Zone"`)
- **THEN** the system falls back safely to `"UTC"` or responds with `HTTP 400 Bad Request` with code `VALIDATION_FAILED`.

---

## ADDED Requirements

### Requirement: Notification Dispatch Multi-Channel Support
The system SHALL support multi-channel notifications, giving precedence to native browser Web Push while maintaining optional Telegram integration for users who explicitly configure a `TelegramChatId`. Notification dispatches SHALL strictly respect the user's localized timezone and preferred time slots rather than firing at hardcoded server hours.

#### Scenario: User receives reminder via Web Push
- **WHEN** the background scheduler triggers a study reminder for a user with active web push subscriptions
- **THEN** the system sends a VAPID-encrypted Web Push notification to all active devices registered by that user, delivering the message directly to the operating system notification center.

---
### Requirement: Web Push Subscription & VAPID Infrastructure
The system SHALL implement modern browser Web Push using Voluntary Application Server Identification (VAPID) across standard browser push endpoints (FCM, Apple Web Push, Mozilla autopush).

#### Scenario: Client retrieves VAPID public key
- **WHEN** an authenticated client calls `GET /api/v1/notifications/push/vapid-public-key`
- **THEN** the server returns the base64url-encoded VAPID public key configured in `WebPush:VapidPublicKey`.

#### Scenario: Client subscribes browser device to push notifications
- **WHEN** an authenticated user enables push and sends `POST /api/v1/notifications/push/subscribe` with `endpoint`, `keys.p256dh`, `keys.auth`, and optional `userAgent`
- **THEN** the system upserts the record into `UserPushSubscriptions`, sets `User.IsPushEnabled = true`, and returns `HTTP 200 OK`.

#### Scenario: Client unsubscribes browser device
- **WHEN** a client sends `POST /api/v1/notifications/push/unsubscribe` with `endpoint`
- **THEN** the system removes the subscription record, and sets `User.IsPushEnabled = false` if no remaining active subscriptions exist for that user.

---

### Requirement: Timezone-Aware Background Push Dispatch Worker
The system SHALL run a background service (`DailyPushNotificationWorker`) executing every 15 minutes to evaluate study reminder and streak preservation alert windows against each user's local timezone.

#### Scenario: Morning study reminder dispatch within user's local window
- **WHEN** the worker evaluates a user whose local time in their configured `TimeZone` matches their `PreferredStudyTime` (within the current 15-minute slot), and the user has not completed today's reading or quiz
- **THEN** the worker dispatches a push notification with title `"TechDaily Study Time 📚"` and deep link to `/today`, recording the dispatch to prevent duplicate messages on subsequent ticks.

#### Scenario: Evening streak preservation alert
- **WHEN** the worker evaluates a user whose local time matches their `StreakAlertTime`, and the user has an active streak (`CurrentStreak > 0`) but has not yet studied today
- **THEN** the worker dispatches an urgent push alert `"Keep your {streak}-day streak alive! 🔥"` warning that the streak will expire at local midnight.

#### Scenario: Skip notification when daily activity is already completed
- **WHEN** the evaluation window matches but the user has already completed today's reading slice and challenge
- **THEN** the worker suppresses the notification, preventing unnecessary notification spam.

---

### Requirement: Stale Push Subscription Auto-Pruning
The Web Push dispatch pipeline SHALL automatically detect and delete revoked or expired push subscription endpoints.

#### Scenario: Push service returns HTTP 404 or 410 Gone
- **WHEN** sending a push notification to an endpoint and the push service responds with HTTP 404 (Not Found) or HTTP 410 (Gone) indicating the user unsubscribed or reset browser state
- **THEN** the worker catches the response and immediately deletes that `UserPushSubscription` row from PostgreSQL, preserving database hygiene.
