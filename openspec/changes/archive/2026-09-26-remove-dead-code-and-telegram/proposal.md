# Proposal

## Why

The codebase carries dead weight from abandoned features and superseded scaffolds. The Telegram Bot notification channel was scaffolded during the initial platform build but was never wired to any trigger — when Web Push (VAPID) landed, the dispatch worker was written solely for Web Push, leaving `ITelegramNotifier`, its implementation, config, entity column, DTO fields, and i18n keys as pure residue with zero runtime callers. Several other unused symbols, duplicate DTOs, an obsolete enum member, a dead DB column, and orphaned frontend components/composables accumulated across feature cutovers. Removing them reduces maintenance surface and eliminates misleading signals (e.g. a `Telegram` config section that implies a working integration).

## What Changes

- **BREAKING (API contract):** Remove `telegramChatId` from `GET/PUT /api/v1/user/profile` request/response DTOs; remove `likesCount` from the tech-insights feed DTO. No frontend UI consumes either.
- Remove the Telegram integration end-to-end: `ITelegramNotifier` interface, `TelegramNotifier` service, DI registrations, `Telegram` config section, `User.TelegramChatId` column, profile DTO fields, and dead Telegram i18n keys / store fields.
- Drop dead database columns via a new EF Core migration: `Users.TelegramChatId` and `TechInsights.LikesCount` (no endpoint reads, writes, or filters on either).
- Remove backend dead symbols: unused `Error` static constants (`NullValue`, `ServerError`, `PasswordTooShort`, `Validation`, `Conflict`, `Forbidden`), the redundant `WebArticleCrawler.ValidateSafeUrl` forwarder, the always-null `GraphNodeDto.EmbleUrl`/`EmblemUrl` scaffold, the obsolete `Category.BackendDotNet` enum member, and the duplicate `OtpMessageResponse` record (consolidated into `MessageResponse`).
- Remove frontend dead code: 5 unused `components/ui/*` duplicates, 3 orphaned `components/profile/*` components (+ their unit test), the unused `useTodayViewMode` composable (+ test), the unused `DomainMasteryProgress` interface, an unused `lucide` icon import, dead Telegram + obsolete profile-domain/milestone i18n keys, and dead test stubs.
- Update `README.md` and `openspec/config.yaml` notification lines to drop the Telegram reference.

Out of scope (intentional dev infrastructure / not dead — see design.md Non-Goals): `pages/playground/*`, `pages/showcase.vue` + `components/showcase/*` (governed by AGENTS.md Pillar 2.4 playground protocol, pruned from prod builds), the `pages/profile.vue` backward-compat redirect, and the defensive guest-path branches in `middleware/auth.global.ts`.

## Capabilities

### New Capabilities

- (none)

### Modified Capabilities

- `core-platform`: Remove the `Telegram Push Notifications` requirement (superseded by Web Push); narrow `Notification Dispatch Multi-Channel Support` so Web Push is the sole notification channel while preserving the timezone / preferred-time-slot dispatch guarantee.
- `cleanup`: Extend `Database & Entity Standard` to mandate removal of the dead `Users.TelegramChatId` and `TechInsights.LikesCount` columns from domain and infrastructure models.

## Impact

- **Backend:** `TechDaily.Domain` (`User`, `TechInsight`, `DomainEnums`), `TechDaily.Application` (`Error`, `IServiceInterfaces`, KnowledgeGraph DTO + handler, Insights DTO + handlers), `TechDaily.Infrastructure` (delete `TelegramNotifier`, DI, `EntityConfigurations`, `WebArticleCrawler`, **new EF migration**), `TechDaily.Api` (`appsettings.json`, profile contracts + `UserEndpoints`, `AuthEndpoints`).
- **Frontend:** components (`ui/*`, `profile/*`), composables (`useTodayViewMode`, `useNavigationMenu`), `useProfileStore`, i18n `en.json`/`vi.json`, regenerated `types/api.generated.ts`, and affected Vitest specs.
- **Database:** one forward migration dropping two columns (irreversible data loss for those columns; both are unused).
- **Docs/config:** `README.md`, `openspec/config.yaml`.
- **APIs:** profile and insights response shapes shrink; no client reads the removed fields.
