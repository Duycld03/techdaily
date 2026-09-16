# Core Platform Capability Delta Specification

## Purpose
Defines delta requirements for auto-syncing the user's timezone during browser Web Push subscription and providing actionable, localized user guidance when Brave browser or privacy-hardened Chromium configurations block push messaging services.

---

## MODIFIED Requirements

### Requirement: Web Push Subscription & VAPID Infrastructure
The system SHALL implement modern browser Web Push using Voluntary Application Server Identification (VAPID) across standard browser push endpoints (FCM, Apple Web Push, Mozilla autopush). The `POST /api/v1/notifications/push/subscribe` endpoint SHALL accept an optional `timeZone` string parameter alongside the endpoint, subscription keys, and user agent. When provided, the backend SHALL validate and persist this timezone to `User.TimeZone`, updating `User.UpdatedAt` and `User.IsPushEnabled = true` within the same transaction. The frontend Web Push composable (`useWebPush`) SHALL automatically detect the client browser's IANA timezone and include it in the subscription payload.

#### Scenario: Client retrieves VAPID public key
- **WHEN** an authenticated client calls `GET /api/v1/notifications/push/vapid-public-key`
- **THEN** the server returns the base64url-encoded VAPID public key configured in `WebPush:VapidPublicKey`.

#### Scenario: Client subscribes browser device to push notifications
- **WHEN** an authenticated user enables push and sends `POST /api/v1/notifications/push/subscribe` with `endpoint`, `keys.p256dh`, `keys.auth`, and optional `userAgent`
- **THEN** the system upserts the record into `UserPushSubscriptions`, sets `User.IsPushEnabled = true`, and returns `HTTP 200 OK`.

#### Scenario: Client unsubscribes browser device
- **WHEN** a client sends `POST /api/v1/notifications/push/unsubscribe` with `endpoint`
- **THEN** the system removes the subscription record, and sets `User.IsPushEnabled = false` if no remaining active subscriptions exist for that user.

#### Scenario: Client subscribes to push notifications with detected timezone
- **WHEN** an authenticated user calls `POST /api/v1/notifications/push/subscribe` with valid push keys and `timeZone: "Asia/Ho_Chi_Minh"`
- **THEN** the system upserts the `UserPushSubscription` record, updates `User.TimeZone = "Asia/Ho_Chi_Minh"`, sets `User.IsPushEnabled = true`, and returns `HTTP 200 OK`.

#### Scenario: Client subscribes without timezone or with empty timezone
- **WHEN** a client calls `POST /api/v1/notifications/push/subscribe` with null or whitespace `timeZone`
- **THEN** the system registers the subscription, sets `User.IsPushEnabled = true`, and preserves the existing `User.TimeZone` value without modification.

#### Scenario: Client subscribes with unrecognized timezone string
- **WHEN** a client calls `POST /api/v1/notifications/push/subscribe` with an unrecognized or malformed timezone identifier (e.g. `"Invalid/Timezone"`)
- **THEN** the system safely falls back to `"UTC"`, persisting `"UTC"` to `User.TimeZone` without failing the subscription request.

---

## ADDED Requirements
### Requirement: Brave Browser Push Service Restriction Handling & Actionable Guidance
The client-side Web Push composable (`useWebPush`) and Settings view (`settings.vue`) SHALL detect when push subscription fails due to browser-level push service restrictions (such as Brave browser disabling Google services for push messaging by default), classify the restriction, and present clear, localized, actionable instructions directing the user to `brave://settings/privacy`.

#### Scenario: Push subscription on Brave browser with Google push services disabled
- **WHEN** a user on Brave browser attempts to enable Web Push notifications while "Use Google services for push messaging" is disabled in the browser settings
- **THEN** `useWebPush` catches the resulting `DOMException` (`"Registration failed - push service error"`), identifies the environment or error pattern, and classifies it as a Brave push service restriction
- **AND** the settings view displays an actionable localized alert/toast explaining that Brave requires enabling Google push services at `brave://settings/privacy` to receive notifications, suppressing confusing raw browser error strings.

#### Scenario: Localization parity for Brave push service instructions
- **WHEN** the Brave push service blocker is encountered under English (`en`) or Vietnamese (`vi`) locale
- **THEN** the actionable guidance is presented in the user's active locale using defined i18n translation keys without untranslated raw strings.
