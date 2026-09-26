# Spec Delta

## MODIFIED Requirements

### Requirement: Database & Entity Standard
The `DailyDrills` entity contains only core drill fields (`UserId`, `QuestionId`, `DocumentChunkId`, `ScheduledDate`, `Status`, `SelectedOptionIndex`, `IsCorrect`, `Score`, `AttemptCount`, `SubmittedAt`). Legacy `AiReviews` table and audio storage columns MUST be removed from domain and infrastructure models. Dead columns with no read, write, or filter usage MUST NOT persist in domain entities or the database schema: the `Users.TelegramChatId` column (unused Telegram integration) and the `TechInsights.LikesCount` column (never incremented, filtered, or rendered) MUST be removed via a forward EF Core migration.

#### Scenario: Developer queries database entities
- **WHEN** inspecting database context and migrations
- **THEN** `AiReviews` table and legacy audio columns are removed and `DailyDrills` contains only required drill attempt fields.

#### Scenario: Dead entity columns are pruned from schema
- **WHEN** inspecting the `Users` and `TechInsights` tables after migrations are applied
- **THEN** neither `Users.TelegramChatId` nor `TechInsights.LikesCount` columns exist, and neither property appears on the `User` or `TechInsight` domain entities or their API response DTOs.
