# Tasks

## 1. Domain

- [x] 1.1 Remove `TelegramChatId` from `TechDaily.Domain/Entities/User.cs` and `LikesCount` from `TechDaily.Domain/Entities/TechInsight.cs`; verify `dotnet build backend/src/TechDaily.Domain` succeeds.
- [x] 1.2 Remove the obsolete `BackendDotNet = 1` member from `Category` in `TechDaily.Domain/Enums/DomainEnums.cs`; verify no source references `Category.BackendDotNet` (grep returns only the removed line) and the project builds.

## 2. Application

- [x] 2.1 Delete the unused static `Error` instances (`NullValue`, `ServerError`, `PasswordTooShort`, `Validation`, `Conflict`, `Forbidden`) from `Common/Error.cs`; verify `dotnet build` of the Application project succeeds (no broken references).
- [x] 2.2 Delete the `ITelegramNotifier` interface (and its `SendDailyDispatchAsync`/`SendStreakWarningAsync` members) from `Interfaces/IServiceInterfaces.cs`; verify build succeeds.
- [x] 2.3 Remove `EmbleUrl` param and `EmblemUrl` alias from `GraphNodeDto` in `Features/KnowledgeGraph/DTOs/KnowledgeGraphDtos.cs` and the 5 `EmbleUrl: null` arguments in `GetKnowledgeGraphQueryHandler.cs`; verify build succeeds.
- [x] 2.4 Remove `LikesCount` from `TechInsightDto` and its assignments in `Features/Insights/GenerateInsightHandler.cs` and `GetInsightsFeedHandler.cs`; verify build succeeds.

## 3. Infrastructure

- [x] 3.1 Delete `Services/TelegramNotifier.cs` and remove `AddHttpClient<TelegramNotifier>()` and `AddScoped<ITelegramNotifier, TelegramNotifier>()` from `DependencyInjection.cs`; verify build succeeds.
- [x] 3.2 Remove the `TelegramChatId` and `LikesCount` property mappings from `Persistence/Configurations/EntityConfigurations.cs`; verify build succeeds.
- [x] 3.3 Remove the redundant `WebArticleCrawler.ValidateSafeUrl` forwarding method; verify callers still use `UrlSecurityValidator.ValidateSafeUrl` and build succeeds.
- [x] 3.4 Generate EF migration `DropDeadColumns` (`dotnet ef migrations add DropDeadColumns`); verify the generated `Up` drops exactly `Users.TelegramChatId` and `TechInsights.LikesCount` and nothing else.

## 4. Api

- [x] 4.1 Remove the `"Telegram"` section from `appsettings.json`; verify the JSON parses and the app boots (`dotnet run` reaches "Application started" with no config error).
- [x] 4.2 Remove `TelegramChatId` from `Contracts/UserProfileResponse.cs`, `Contracts/UpdateUserProfileResponse.cs`, and all references in `Endpoints/UserEndpoints.cs` (GET/PUT projection, update block, `UpdateProfileRequest`); verify build succeeds and `GET /api/v1/user/profile` returns a payload with no `telegramChatId` field.
- [x] 4.3 Replace `OtpMessageResponse` with the shared `Contracts/MessageResponse` at all `AuthEndpoints.cs` usages and delete the duplicate record; verify build succeeds and OTP endpoints still return `{ "message": ... }`.

## 5. Frontend

- [x] 5.1 Delete unused `components/ui/{CodeBlock,BentoStatCard,EmptyState,FloatingSelectionToolbar,SkeletonShimmer}.vue`; verify `grep -r` finds zero remaining references and `npm run build` (or typecheck) passes.
- [x] 5.2 Delete `components/profile/{DomainGoalTracker,EngineerIdentityPassport,EngineerMilestonesCard}.vue` and `tests/components/profile.spec.ts`, and remove their dead stub mounts from `tests/pages/settings.spec.ts`; verify `npm test -- settings` passes.
- [x] 5.3 Delete `composables/useTodayViewMode.ts` and `tests/composables/useTodayViewMode.spec.ts`; verify no file references `useTodayViewMode` and the suite still collects.
- [x] 5.4 Remove `telegramChatId` and the unused `DomainMasteryProgress` interface from `stores/useProfileStore.ts`, and the `telegramChatId` assertions/mock from `tests/stores/profile.spec.ts`; verify `npm test -- profile` passes.
- [x] 5.5 Remove the unused `User` icon import from `composables/useNavigationMenu.ts`; verify typecheck/lint passes.
- [x] 5.6 Remove the dead Telegram i18n keys (`profile.telegram_id`, `profile.telegram_placeholder`, `settings.telegram_title`, `settings.telegram_desc`, `settings.telegram_bot_info`) and the obsolete profile-domain/milestone keys from `i18n/locales/en.json` and `vi.json`, keeping en/vi key parity; verify no template references the removed keys and `npm test` passes.
- [x] 5.7 Regenerate `types/api.generated.ts` from the updated OpenAPI document; verify the diff removes only `telegramChatId` and `likesCount` and introduces no unrelated contract changes.

## 6. Specs & Docs

- [x] 6.1 Update the notification line in `README.md` (drop `+ Telegram`) and `openspec/config.yaml` (remove Telegram from the Notifications stack line); verify neither file mentions Telegram.
- [x] 6.2 Run `openspec validate remove-dead-code-and-telegram --strict`; verify it passes with the `core-platform` and `cleanup` deltas.

## 7. Integration Verification

- [x] 7.1 Run backend `dotnet test tests/TechDaily.Tests` from `backend/`; verify the full suite passes (no orphaned references to removed symbols).
- [x] 7.2 Run frontend `npm test` from `frontend/`; verify all Vitest suites pass.
- [x] 7.3 Apply the migration to a local DB, boot the API, and smoke-test: `GET /api/v1/user/profile` (no `telegramChatId`), insights feed (no `likesCount`), and confirm `\d "Users"` / `\d "TechInsights"` show the columns dropped.
