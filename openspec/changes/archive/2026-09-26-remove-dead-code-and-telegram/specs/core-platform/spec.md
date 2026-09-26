# Spec Delta

## MODIFIED Requirements

### Requirement: Notification Dispatch Multi-Channel Support
The system SHALL dispatch study reminders and streak-preservation notifications exclusively via native browser Web Push (VAPID). Notification dispatches SHALL strictly respect the user's localized timezone and preferred time slots rather than firing at hardcoded server hours.

#### Scenario: User receives reminder via Web Push
- **WHEN** the background scheduler triggers a study reminder for a user with active web push subscriptions
- **THEN** the system sends a VAPID-encrypted Web Push notification to all active devices registered by that user, delivering the message directly to the operating system notification center.

#### Scenario: Dispatch respects user timezone and preferred slots
- **WHEN** the background scheduler evaluates users for reminder dispatch
- **THEN** it sends notifications aligned to each user's stored IANA timezone and preferred time slots, not fixed server-local hours.

## REMOVED Requirements

### Requirement: Telegram Push Notifications
**Reason**: The Telegram Bot channel was scaffolded but never wired to any dispatch trigger (zero runtime callers). Morning and streak reminder dispatch is fully served by Web Push (see `Notification Dispatch Multi-Channel Support` and `Web Push Subscription & VAPID Infrastructure`).
**Migration**: None required — no user-facing Telegram configuration ever existed. The `Users.TelegramChatId` column, `Telegram` config section, and `ITelegramNotifier` service are removed by this change.
