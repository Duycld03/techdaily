# Design

## Context

See proposal.md — Why. All targets are confirmed dead by reference tracing: `ITelegramNotifier.SendDailyDispatchAsync`/`SendStreakWarningAsync` have zero callers; `TelegramChatId` and `LikesCount` have no read/write/filter path; the frontend has zero Telegram templates. The only live notification path is `DailyPushNotificationWorker` → `IWebPushService`. Two of the removals touch the database schema and therefore require a forward EF Core migration; the rest are compile-time-only deletions.

## Goals / Non-Goals

**Goals:**
- Clean cutover: delete dead symbols and their now-obsolete tests/i18n keys with no shims, aliases, or deprecated re-exports left behind.
- Keep the build and both test suites green; keep the app booting (DI container validates).
- Shrink API contracts (`user/profile`, insights feed) and regenerate frontend types to match.

**Non-Goals:**
- Removing `pages/playground/*`, `pages/showcase.vue`, or `components/showcase/*`. These are intentional dev-only design-system / prototyping infrastructure mandated by AGENTS.md Pillar 2.4 and pruned from production builds via the `nuxt.config.ts` `pages:extend` hook — not dead code.
- Removing the `pages/profile.vue` backward-compat redirect or simplifying the defensive guest-path branches in `middleware/auth.global.ts` (harmless, low-value, separate concern).
- Changing Web Push behavior. Web Push stays exactly as-is; only the never-wired Telegram channel is removed.

## Decisions

- **One forward migration for both dead columns.** Add a single migration (e.g. `DropDeadColumns`) issuing `DropColumn("TelegramChatId","Users")` and `DropColumn("LikesCount","TechInsights")`. Rationale: both drops are part of the same cleanup; one migration keeps history minimal. Existing historical migration files are immutable and left untouched.
- **Consolidate `OtpMessageResponse` into `MessageResponse`.** The two records are shape-identical (`string Message`). Replace all `OtpMessageResponse` usages in `AuthEndpoints.cs` with the shared `MessageResponse` contract and delete the duplicate. Alternative (keep both) rejected: duplicate DTOs drift and bloat the OpenAPI schema.
- **Remove obsolete `Category.BackendDotNet` enum member, keep the string-compat mapping.** `EntityConfigurations` maps the literal string `"BackendDotNet"` → `Category.BackendRuntime` during deserialization; that mapping is a string comparison and does not reference the enum identifier, so dropping the member is safe and the defensive mapping continues to compile. Rationale: preserves resilience against any legacy stored string while removing the dead enum value.
- **Regenerate `types/api.generated.ts` from the updated OpenAPI document** rather than hand-editing. Rationale: it is generated; hand-edits drift from the backend contract.
- **Spec deltas over silent deletion.** Telegram and the dead columns are documented in `core-platform` and `cleanup` specs, so their removal is recorded as REMOVED/MODIFIED requirements to keep specs the source of truth.

## Risks / Trade-offs

- [Dropping DB columns is irreversible] → Both columns are unused (no reads/writes/filters); production data in them is meaningless. Migration is forward-only; rollback would re-add empty nullable/default columns if ever needed.
- [OpenAPI type regeneration could shift unrelated lines] → Regenerate then diff; confirm only `telegramChatId`/`likesCount` fields disappear and no unrelated contract changes leak in.
- [Removing test files/stubs could mask a real regression] → Only tests that exclusively cover deleted code are removed; the full backend (`dotnet test`) and frontend (`npm test`) suites must stay green as the acceptance gate.
- [MODIFIED requirement header retained as "Multi-Channel Support" despite single channel] → Header kept verbatim so the archive merge matches by exact header; body text unambiguously states Web Push is the sole channel.

## Migration Plan

1. Land all compile-time deletions (backend symbols, frontend files, i18n keys) plus the entity property removals.
2. Generate the `DropDeadColumns` EF migration against the updated entities; verify the generated `Up` drops exactly the two columns.
3. Apply migration locally, run both test suites, boot the API, and smoke-test the affected endpoints.
4. Deploy applies the migration on startup as usual. Forward-only; no rollback migration authored.
