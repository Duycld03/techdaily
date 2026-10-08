# Spec Delta

## MODIFIED Requirements

### Requirement: Structured AI Question Generation with Gemini
The system SHALL generate batches of 5 to 10 multiple-choice questions tailored by topic and mastery depth level (Foundation, Applied, Advanced, Mastery) via Gemini.

#### Scenario: User requests a new quiz batch
- **WHEN** an authenticated user submits `POST /api/v1/quiz/generate` with a valid topic (2-100 characters), category, level, and count (5 or 10)
- **THEN** the system generates fresh questions using Gemini with 4 options and 1 correct answer, saves them into the `QuizQuestions` table, and returns the questions (excluding previously mastered questions).

#### Scenario: User requests additional questions for the same topic ("Generate More")
- **WHEN** user clicks "Generate More Questions" after completing a quiz
- **THEN** the system passes the titles of existing questions to Gemini's prompt to prevent duplication, saves newly generated questions to `QuizQuestions`, and returns the new batch.

#### Scenario: Gemini API rate limit or network failure
- **WHEN** the Gemini API returns HTTP 429, timeout, or invalid JSON
- **THEN** the system falls back gracefully to existing unmastered questions in the database or structured fallback templates without crashing.

### Requirement: Interview Quiz Studio Visual Layout & Interactive Tokens
The `/quiz` route SHALL implement the **DeepPace Studio** visual language and 100% bilingual localization across all internal tabs (`generate`, `arena`, `summary`, `review`, `stats`):
1. **Canvas & Container Consistency**: The root container SHALL render over `dark:bg-canvas` with standard studio container width (`max-w-6xl mx-auto px-4 sm:px-6 py-6`).
2. **Reflex Practice Header**: The header SHALL display the DeepPace practice title, subtitle, and studio icon tile.
3. **Mastery Depth Level Matrix**: The 4 mastery depth tiers (`Foundation`, `Applied`, `Advanced`, `Mastery`) SHALL render in an ergonomic $2 \times 2$ matrix.
4. **Interactive Arena Options**: Multiple-choice cards SHALL adhere to hairline border styling and active violet accents.

#### Scenario: User navigates across quiz studio tabs
- **WHEN** user selects any tab in `/quiz` (`generate`, `arena`, `review`, `stats`)
- **THEN** the active tab highlights with a neutral glass elevation and white text
- **AND** the tab content renders on an obsidian base with hairline borders within a `max-w-6xl` studio container.

#### Scenario: User configures and generates quiz in 2-column bento studio
- **WHEN** user navigates to `/quiz` with the `generate` tab active on a desktop screen (>=1024px)
- **THEN** the interface renders an equal 50/50 2-column Bento Grid separating topic/source configuration on the left from seniority/action controls on the right
- **AND** both columns render with balanced vertical height within a `max-w-6xl` studio container.

#### Scenario: Equal card height in initial unselected state
- **WHEN** user views the quiz generation interface with "Luyện đề theo sách" in its default unselected state
- **THEN** both the Topic & Context card and the Seniority & Action card render with identical top and bottom baselines via `items-stretch`
- **AND** the book toggle card sits anchored at the bottom edge of the left card matching the right card's bottom edge.

#### Scenario: User toggles book-grounded generation mode without button displacement
- **WHEN** user toggles "Luyện đề theo sách" (`isGrounded`) on a desktop screen (>=1024px)
- **THEN** the book selection dropdown renders within the Topic & Context card
- **AND** the primary generation button in the Seniority & Action card maintains its stable vertical position without shifting downward.

#### Scenario: Safe topic fallback when generating quiz without explicit input
- **WHEN** user clicks the generation button without typing a custom topic
- **THEN** the generation handler resolves the topic from the first computed topic suggestion or default topic
- **AND** initiates quiz generation without throwing a `ReferenceError`.

#### Scenario: Context-aware topic suggestions reflect active learning context
- **WHEN** an authenticated user has active technical books in `/library` or a target career role in `/profile`
- **THEN** the suggested topic chips dynamically display relevant topics from the user's library and role
- **AND** clicking any topic chip populates the topic input field without auto-submitting.

#### Scenario: User selects seniority tier on minimalist typographic cards
- **WHEN** user clicks on any of the 4 seniority level cards in the $2 \times 2$ grid (e.g. Senior / Staff)
- **THEN** the selected card highlights with primary brand violet borders and a discrete accent dot
- **AND** the card displays clean typography without any decorative emoji icons or graphical spam.

#### Scenario: Responsive level card descriptions adhere to typography standards
- **WHEN** user views seniority level options on any device
- **THEN** the descriptive text explaining each level's scope renders at least `text-xs` on mobile and scales to `text-sm` on larger viewports
- **AND** does not truncate or wrap awkwardly across English and Vietnamese locales.

#### Scenario: Question count switches via segmented pill control
- **WHEN** user clicks between "3 Questions", "5 Questions", and "10 Questions"
- **THEN** the selected count highlights with an elevated brand pill inside the segmented container
- **AND** updates the generator payload to the corresponding count value.

#### Scenario: User selects multiple-choice option in arena
- **WHEN** user clicks an option card (A, B, C, or D) before submitting
- **THEN** the option card highlights with Deep Iris Violet borders and subtle violet tint
- **AND** does not display harsh opaque colors or visual noise.

#### Scenario: User submits correct option in arena
- **WHEN** user submits the correct answer to a multiple-choice question in the arena
- **THEN** the correct option highlights with primary brand violet styling (`border-brand-500/80 bg-brand-500/10 text-brand-700 dark:text-brand-300`)
- **AND** the result feedback banner renders with primary brand accents instead of emerald green.

#### Scenario: Bilingual localization of review queue tab and mistake indicators
- **WHEN** user views `/quiz` in Vietnamese mode (`vi`)
- **THEN** the review queue tab displays "Ôn Tập" via `quiz.tab_review_queue` instead of the raw key string `quiz.tab_review_queue`
- **AND** incorrect answer badges in summary and review list render "Chưa chính xác" and "{count} lần làm sai" via `quiz.incorrect_badge` and `quiz.incorrect_attempts`
- **AND** the session summary score renders `{percentage}% Chính Xác` instead of hardcoded English.
