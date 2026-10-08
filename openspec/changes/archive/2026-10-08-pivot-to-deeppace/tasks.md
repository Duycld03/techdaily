# Tasks

## 1. Domain Component

- [x] 1.1 Update `DomainEnums.cs` to support mastery depth labels for `QuizLevel` (Foundation, Applied, Advanced, Mastery) and extend `Category` to include MentalModels and Habits while preserving integer backwards compatibility.
- [x] 1.2 Review `InterviewQuestion` and `SpacedRepetitionCard` domain invariants to ensure full support for both technical and behavioral decision scenarios.

## 2. Infrastructure & AI Component

- [x] 2.1 Update `GeminiAiService.cs` slice formatting prompt to adaptively support both Technical Architecture books (code snippets, syntax highlighting, runtime systems) and Behavioral Mindset books (habit mechanics, cognitive focus, leadership trade-offs).
- [x] 2.2 Update `GeminiAiService.cs` insight generation prompt to adapt between Software Craftsmanship (problem/solution code snippets, runtime internals, benchmarks) and Mental Craft (habit traps vs optimal routines, cognitive neuroscience).
- [x] 2.3 Update `GeminiAiService.cs` quiz question prompt to synthesize scenario questions reflecting universal Decision Drills across the 4 mastery depth tiers.

## 3. Application & API Component

- [x] 3.1 Update DTOs in `DailyFocusDtos.cs`, `QuizDtos.cs`, and `InsightDtos.cs` to expose normalized mastery levels and de-jargonized field descriptions.
- [x] 3.2 Update `GenerateQuizHandler.cs` and `CurateSliceHandler.cs` to route category-aware prompts to the AI service.
- [x] 3.3 Update API documentation, Scalar endpoint summaries, and Swagger descriptions from TechDaily to DeepPace.

## 4. Frontend Component

- [x] 4.1 Update `frontend/locales/en.json` and `frontend/locales/vi.json` to replace interview-specific jargon with DeepPace terminology (Decision Drills, Reflex Arena, Daily Practice Workspace, Blindspot Queue).
- [x] 4.2 Update branding metadata, site title, favicon, and navbar logos in `nuxt.config.ts`, `app.vue`, and layout components to DeepPace.
- [x] 4.3 Update `/today` (`InterviewChallengePane.vue`), `/quiz` (`quiz.vue`), and `/insights` (`insights.vue`) header badges, card labels, and level selectors to reflect DeepPace design tokens.

## 5. Verification & Testing

- [x] 5.1 Execute backend test suite via `dotnet test` to verify all domain invariants, enum mappings, and handlers pass.
- [x] 5.2 Execute frontend test suite via `npm test` to verify data contracts, stores, and route guards.
- [x] 5.3 Conduct automated visual verification on Desktop (1440x900) and Mobile (390x844) viewports using headless browser to inspect layout integrity and typography across English and Vietnamese locales.
