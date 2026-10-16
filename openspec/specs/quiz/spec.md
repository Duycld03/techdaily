# Quiz Specification

## Purpose
Provides high-speed AI multiple-choice interview quiz generation via Gemini, real-time timer arena, mastery tracking, and mistake review queue.

## Requirements

### Requirement: Mandatory Authentication on All Quiz Capabilities
The system SHALL require authenticated user identity for all quiz generation, submission, review queue, and analytics endpoints.

#### Scenario: Unauthenticated visitor navigates to /quiz
- **WHEN** an unauthenticated visitor navigates to `/quiz`
- **THEN** the system immediately redirects to `/login?redirect=/quiz` without exposing quiz data.

#### Scenario: Unauthenticated API request to quiz endpoints
- **WHEN** a client sends a request to any `/api/v1/quiz/*` endpoint without a valid JWT token
- **THEN** the system responds with `HTTP 401 Unauthorized`.

---

### Requirement: Structured AI Question Generation with Gemini
The system SHALL generate batches of 5 to 10 multiple-choice questions tailored by topic and mastery depth level (Foundation, Applied, Advanced, Mastery) via Gemini. Standalone generation SHALL require an explicitly selected active `categoryId` UUID; source-grounded generation SHALL retain its authoritative document category. Responses SHALL include stable category references and descriptors rather than enum ordinals.

#### Scenario: User requests a new quiz batch
- **WHEN** an authenticated user submits `POST /api/v1/quiz/generate` with a valid topic (2–100 characters), active `categoryId`, level, and count (5 or 10)
- **THEN** Gemini generates fresh questions with four options and one correct answer, persists their category UUIDs, and returns the questions excluding previously mastered questions.

#### Scenario: User requests additional questions for the same topic ("Generate More")
- **WHEN** the user clicks Generate More Questions after completing a quiz
- **THEN** existing question titles are passed to the generation prompt to prevent duplication and the new batch retains the admitted category UUID.

#### Scenario: Gemini API rate limit or network failure
- **WHEN** Gemini returns HTTP 429, timeout, or invalid JSON
- **THEN** recovery may return genuine existing unmastered questions only from the authoritative selected category under existing ownership/mastery rules
- **AND** if suitable stored questions are unavailable the request reports failure; it never persists canned/mock/template questions or substitutes another category.

#### Scenario: New subject without a numeric category mapping
- **WHEN** a learner explicitly generates questions for an administrator-created active category
- **THEN** the category is accepted through the same UUID contract without adding an enum, prompt mapping, or topic-keyword classifier.

#### Scenario: Standalone category deactivated before admission
- **WHEN** the selected category is already inactive at initial standalone generation validation
- **THEN** HTTP 409 `Category.Inactive` is returned without invoking AI or persisting questions.

#### Scenario: Deactivation races with generation persistence
- **WHEN** standalone AI generation has started but deactivation commits before the final assignment-admission check
- **THEN** HTTP 409 `Category.Inactive` is returned with no persisted new questions
- **AND** no database category lock is held while waiting for the AI provider.

#### Scenario: Existing question reuse respects category isolation
- **WHEN** two categories contain questions with the same topic and level and a batch is requested for one category
- **THEN** normal reuse, fill and failure-recovery paths return questions only from the authoritative selected category
- **AND** they never relabel or return the other category's historical questions.

### Requirement: Answer Submission & Mastery Tracking
The system SHALL persist user answer attempts, mark correctly answered questions as Mastered (`IsMastered = true`), and route incorrect questions into a Mistake Review Queue.

#### Scenario: User answers a quiz question correctly
- **WHEN** user submits the correct option index via `POST /api/v1/quiz/submit`
- **THEN** the system sets `IsMastered = true`, increments `CorrectCount`, and excludes this question from future generation batches for this user.

#### Scenario: User answers a quiz question incorrectly
- **WHEN** user submits an incorrect option index via `POST /api/v1/quiz/submit`
- **THEN** the system sets `IsMastered = false`, increments `IncorrectCount`, and retains the question in the user's Review Queue.

#### Scenario: Idempotent Submission Handling
- **WHEN** user double-clicks submit or submits concurrently for the same question
- **THEN** the system executes an idempotent upsert against `UserQuizProgress` using the unique `(UserId, QuestionId)` constraint without duplicating records.

---

### Requirement: Mistake Review Queue & Mastery Analytics
The system SHALL provide review of unmastered questions and mastery analytics, with a one-click action on failed questions to add them to the daily SM-2 deck through `POST /api/v1/review/cards/from-quiz-mistake`.

`GET /api/v1/quiz/review-queue` SHALL accept `page` (default/minimum 1), `pageSize` (default 10, range 1–100), optional `categoryId` UUID, optional integer `level`, and optional string `topic`. Results SHALL contain `questions` ordered by `LastAttemptedAt` descending, `totalCount`, `page`, `pageSize`, and `totalPages` equal to the ceiling of count/page size or zero for no results. Category filtering SHALL retain inactive referenced categories and reject malformed references without numeric compatibility aliases.

The Review Queue SHALL retain numbered pagination, boundary-disabled previous/next, Practice Current Batch and Practice All Mistakes actions, URL synchronization using `?tab=review&page=N&categoryId=UUID&level=L&topic=T`, and resetting page to 1 after category/level/topic filter changes.

The Stats tab SHALL retain its responsive four-card Bento Dashboard:
1. Hero Performance: accuracy, answered count, review-queue count, and direct review CTA.
2. Spaced Mastery Gauge: mastered/answered percentage and localized proficiency badges.
3. Seniority Matrix: distinct string/integer mappings for Fresher, Junior, Middle and Senior, individual progress/counters, never a universal Senior fallback.
4. Topic Strengths & Weaknesses: topic titles, answered/mastered counts and accuracy badges (emerald at least 80%, amber 50–79%, rose below 50%).

Category breakdowns SHALL be grouped by stable category UUID with current bilingual descriptors, never fixed category slots. Existing question mastery, SM-2 bridge behavior and ownership SHALL remain unchanged.

#### Scenario: User opens the Mistake Review Queue
- **WHEN** the user requests page 1 with page size 10
- **THEN** up to ten of their unmastered questions are returned with total count/pages
- **AND** cards, pagination and total mistake counter render.

#### Scenario: User masters a previously failed question during review
- **WHEN** a queued question is answered correctly
- **THEN** it is marked mastered, removed from active review and the queue count decreases by one.

#### Scenario: User requests quiz mastery statistics
- **WHEN** the user requests `GET /api/v1/quiz/stats`
- **THEN** answered/mastered/unmastered counts and mastery rates are returned with dynamic category-UUID breakdowns.

#### Scenario: User promotes an incorrect question from quiz results
- **WHEN** a completed batch includes mistakes
- **THEN** each mistake explanation offers Push to SM-2 Deck.

#### Scenario: User clicks Push to SM-2 Deck button
- **WHEN** the action is clicked
- **THEN** the client submits the question ID, disables the action as Added to SM-2 Deck, and shows a localized scheduling toast.

#### Scenario: User navigates mistake review queue with numbered pagination controls
- **WHEN** a user with 35 queued mistakes and page size 10 clicks page 2
- **THEN** the client fetches page 2, updates `?tab=review&page=2`, and shows questions 11–20 with page 2 active.

#### Scenario: User synchronizes review queue page with URL query parameters
- **WHEN** the user directly opens `/quiz?tab=review&page=3`
- **THEN** the review tab activates and page 3 is fetched/displayed.

#### Scenario: User launches practice session for current review queue batch
- **WHEN** the user viewing questions 11–20 clicks Practice Current Batch (10)
- **THEN** only those ten questions enter the arena starting at question one.

#### Scenario: User launches practice session for all unmastered questions in review queue
- **WHEN** a user with 35 mistakes across four pages clicks Practice All Mistakes (35)
- **THEN** all 35 are eagerly retrieved and enqueued for arena remediation.

#### Scenario: User views quiz stats tab with bento dashboard layout
- **WHEN** an authenticated user selects Stats
- **THEN** all four dashboard cards render with reactive answered/mastered/accuracy metrics.

#### Scenario: User views quiz stats tab with symmetric 2-column bento grid
- **WHEN** Stats is viewed on a desktop at least 1024px wide
- **THEN** Hero/Gauge occupy equal halves of row one and Seniority/Topic cards equal halves of row two.

#### Scenario: Straight vertical divider seam between column 1 and column 2 across both bento rows
- **WHEN** the large-screen dashboard is inspected
- **THEN** the dividing boundary aligns continuously across both rows without staggered columns or span asymmetry.

#### Scenario: User inspects seniority breakdown with string-serialized levels
- **WHEN** string levels Fresher, Junior, Middle and Senior are returned
- **THEN** each maps to its distinct localized label/progress row, without defaulting all rows to Senior.

#### Scenario: User inspects topic strengths and weaknesses card
- **WHEN** topic accuracy metrics render
- **THEN** accuracy at least 80% uses emerald, 50–79% amber, and below 50% rose.

#### Scenario: User initiates review session directly from hero performance card
- **WHEN** a user with three mistakes clicks Review Mistakes (3)
- **THEN** the interface activates the review workflow and loads the unmastered questions without full-page navigation.

#### Scenario: Bilingual visual verification of quiz stats bento dashboard
- **WHEN** the Stats dashboard switches between English and Vietnamese
- **THEN** titles, levels, proficiency badges and actions update reactively with readable diacritics and no overflow/truncation.

#### Scenario: Renamed and inactive categories remain in mistake analytics
- **WHEN** a category with historical mistakes is renamed or deactivated
- **THEN** filtering and category totals retain the same UUID and current descriptor
- **AND** neither mastery nor historical attempts are reset or regrouped into a default subject.

### Requirement: Book & Curriculum Grounded Quiz Generation
Grounded quiz generation SHALL resolve authoritative context uniformly from the authenticated user's library document chunks through existing similarity search, without separate curriculum/book slice models. A specifically selected book SHALL supply the authoritative category UUID, including an inactive existing assignment. Contradictory caller-supplied category IDs SHALL be rejected, not silently override the book or be guessed from keywords.

#### Scenario: User requests quiz based on a specific book
- **WHEN** the user initiates generation with an owned `BookId`
- **THEN** its chunks supply source excerpts and its category UUID is retained on the generated questions
- **AND** questions are grounded in those excerpts even if the book's existing category is inactive.

#### Scenario: User requests quiz grounded in topic curriculum
- **WHEN** the user requests a broad topic with grounding enabled and an active category UUID
- **THEN** similarity search retrieves matching authorized library chunks in the selected category and provides their excerpts for scenario generation.

#### Scenario: Excerpt citation in quiz question explanations
- **WHEN** an answer to a grounded question is submitted
- **THEN** the explanation includes a Source Reference badge with book title, chapter slice and source excerpt.

#### Scenario: Book selection conflicts with category selection
- **WHEN** a generation request specifies an owned book and a different category UUID
- **THEN** HTTP 400 explains the conflicting selection without AI invocation or persisted questions.

#### Scenario: Book category is inactive before or during grounded generation
- **WHEN** the server resolves an authorized owned book whose retained category is inactive initially or becomes inactive during generation
- **THEN** final persistence rechecks the source and unchanged authoritative assignment and retains that category UUID without the standalone active-category restriction
- **AND** arbitrary BookId values or caller-controlled flags do not authorize another user's source or an unrelated inactive category.

#### Scenario: Source is reclassified during grounded generation
- **WHEN** the source book's authoritative category changes before final persistence
- **THEN** the request returns a conflict without persisting questions under an unchecked category or acquiring source/category locks in reverse order.

### Requirement: 1-Click Spaced Repetition Bridge for Quiz Mistakes
The backend SHALL expose `POST /api/v1/review/cards/from-quiz-mistake` allowing authenticated users to transform any `QuizQuestion` into an active `SpacedRepetitionCard`.

#### Scenario: Creating a spaced repetition card from a quiz mistake
- **WHEN** an authenticated user invokes `POST /api/v1/review/cards/from-quiz-mistake` with a valid `questionId`
- **THEN** the system constructs a new `SpacedRepetitionCard` where `SourceType = CardSourceType.QuizMistake`, `SourceQuizQuestionId = questionId`, `FrontMarkdown` contains the question text and formatted answer choices, `BackMarkdown` contains the correct answer and authoritative explanation, and `NextReviewDate` is set to the current UTC date.

#### Scenario: Idempotent card creation for previously converted questions
- **WHEN** a user invokes the endpoint for a question that was previously converted to an SM-2 card
- **THEN** the system resets the existing card's `NextReviewDate` to today, resets `RepetitionCount = 0` and `Status = CardStatus.Learning` to restart the spaced repetition cycle, and returns `HTTP 200 OK` without creating duplicate records.

---

### Requirement: Bidirectional Mastery Synchronization via Spaced Repetition Grading
When a user grades a spaced repetition card derived from a quiz mistake in `/review`, the system SHALL synchronize recall performance back to `UserQuizProgress`.

#### Scenario: Successful recall in SM-2 deck masters quiz question
- **WHEN** user grades a card whose `SourceType == CardSourceType.QuizMistake` with `qualityGrade >= 3` (3 = Pass, 4 = Good, 5 = Excellent)
- **THEN** `GradeReviewCardHandler` locates the corresponding `UserQuizProgress` record and sets `IsMastered = true`, incrementing `CorrectCount` and removing the question from the pending `/quiz/review-queue`.

#### Scenario: Failed recall in SM-2 deck maintains unmastered status
- **WHEN** user grades a quiz-derived card with `qualityGrade < 3` (0 = Blackout, 1 = Incorrect, 2 = Hard)
- **THEN** `UserQuizProgress.IsMastered` remains `false`, incrementing `IncorrectCount`, and the card is scheduled for immediate repetition in tomorrow's deck.

### Requirement: Flashcard Generation Idempotency from Multiple Sources
The spaced repetition bridge SHALL guarantee idempotency for card creation across all source channels (highlight conversions via `POST /api/v1/review/cards/from-highlight` and quiz mistakes via `POST /api/v1/review/cards/from-quiz-mistake`). Calling card generation endpoints repeatedly for the same source entity SHALL NOT create duplicate records in `SpacedRepetitionCards`.

#### Scenario: Idempotent card generation from reading highlight
- **WHEN** user invokes card generation for a reading highlight that was already transformed into a flashcard
- **THEN** the backend identifies the existing card by `SourceHighlightId` and returns the existing card details with `HTTP 200 OK` without creating redundant database records.

#### Scenario: Idempotent card generation from quiz mistake
- **WHEN** user invokes `POST /api/v1/review/cards/from-quiz-mistake` for a question already in the user's review deck
- **THEN** the backend resets the existing card's review schedule to today and returns `HTTP 200 OK` without duplicating records.

### Requirement: Interview Quiz Studio Visual Layout & Interactive Tokens
The `/quiz` route SHALL implement the **DeepPace Studio** visual language and 100% bilingual localization across all internal tabs (`generate`, `arena`, `summary`, `review`, `stats`):

1. **Canvas & Container Consistency:**
   - The root `/quiz` container SHALL render over `dark:bg-canvas` (`#09090b` obsidian base) instead of legacy slate, utilizing a standard studio container width (`max-w-6xl mx-auto px-4 sm:px-6 py-6`) to prevent wide empty margins on desktop displays and align with `/library` and `/profile`.
   - All tab content containers, topic generation cards, and Bento statistics cards SHALL utilize `.glass-card` styling with subtle hairline borders (`border-white/[0.06]`).

2. **Clean Studio Header Banner**:
   - The page header SHALL feature a clean, minimalist studio presentation with an icon tile (`p-2.5 rounded-2xl bg-brand-500/10 border border-brand-500/20 text-brand-600 dark:text-brand-400`), high-contrast title (`quiz.title`), and localized subtitle.
   - The header SHALL strictly omit decorative pulse halos, glowing badge rings, or distracting gradient background overlays.

3. **Stable Dynamic-Aligned 50/50 Bento Generation Studio with Initial Equal Height**:
   - When the `generate` tab is active, the generator interface SHALL render as an equal 50/50 2-column Bento grid on desktop viewports (`grid grid-cols-1 lg:grid-cols-2 gap-6`):
     - **Topic & Context Hub (Column 1 - 50% width)**: A `.glass-card` container organized with flex column distribution (`flex flex-col justify-between`), housing the custom topic text input and context-aware quick topic chips at the top, and anchoring the grounded-in-book toggle card with `AppSelect` book dropdown at the bottom.
     - **Seniority & Action Hub (Column 2 - 50% width)**: A `.glass-card` container organizing the $2 \times 2$ seniority level cards, 3-tier question count segmented pills, and primary generation trigger button.
   - **Initial Equal Height Baseline**: When "Luyện Đề Theo Sách" is in its default unselected state (`!isGrounded`), the grid SHALL apply `items-stretch` so both Bento cards share the exact same top and bottom baseline (`ngang nhau`).
   - **Single-Side Downward Expansion**: Toggling the book-grounded mode (`isGrounded`) SHALL switch grid alignment to `items-start`, expanding only the Topic & Context Hub vertically and never forcing vertical stretching or downward displacement of the primary generation button in the sibling Action Hub.
   - On mobile and tablet viewports (<1024px), the layout SHALL gracefully stack into a single column with consistent vertical rhythm.
4. **Context-Aware Dynamic Topic Suggestions & Safe Fallback**:
   - The suggested topic chips SHALL dynamically prioritize user learning context and technology-agnostic engineering fundamentals:
     - Prioritize active/uploaded book topics from `libraryStore.books` when books exist in the user's library.
     - Fall back to foundational, technology-agnostic senior architectural pillars (System Design, Database Indexing, Distributed Consensus, Caching Strategies, API Design, Concurrency Models, Containers & Cloud Infrastructure), strictly avoiding version-pinned or single-framework dependencies.
   - When triggering quiz generation with an empty topic input, the generator SHALL safely fall back to the first available computed quick topic suggestion or a reliable default engineering topic, without throwing runtime reference errors.
   - Topic chips SHALL enforce `whitespace-nowrap shrink-0` with horizontal wrapping to maintain neat presentation.
5. **Minimalist Typographic Seniority Level Matrix ($2 \times 2$ Grid)**:
   - The 4 mastery depth tiers (`Foundation`, `Applied`, `Advanced`, `Mastery`) SHALL render in an ergonomic $2 \times 2$ matrix (`grid grid-cols-1 sm:grid-cols-2 gap-3`) to equalize vertical height between the two studio columns.
   - The level cards SHALL strictly prohibit decorative emojis, icon badges, and graphical embellishments to maintain a calm, professional engineering aesthetic.
   - Selected level cards SHALL indicate active state via subtle brand borders (`border-brand-500`), translucent background fill (`bg-brand-500/10`), a soft focus ring (`ring-1 ring-brand-500/30`), and a discrete circular accent dot (`w-2 h-2 rounded-full bg-brand-500`).
   - Unselected cards SHALL render neutral translucent surfaces (`border-slate-200/80 dark:border-white/[0.06] bg-white/60 dark:bg-white/[0.02] hover:border-slate-300 dark:hover:border-white/[0.15]`).
   - Role descriptions within level cards SHALL enforce responsive typography standards (`text-xs sm:text-sm font-normal text-slate-500 dark:text-slate-400`), preventing illegible micro-text across viewport sizes.

6. **Versatile 3-Tier Question Count Segmented Controller (3 / 5 / 10)**:
   - The question count selector SHALL offer 3 distinct learning tempos in a compact segmented pill container (`bg-slate-100 dark:bg-canvas-subtle p-1 rounded-xl border border-slate-200/80 dark:border-white/[0.08]`):
     - `3 Questions` (Quick Drill ~3 mins).
     - `5 Questions` (Standard Practice ~5 mins).
     - `10 Questions` (Full Interview Mock ~10 mins).
   - Active count options SHALL elevate with a solid brand pill (`bg-brand-600 text-white shadow-sm font-bold`), while inactive options remain unobtrusive text pills (`text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white`).

7. **Interactive Tab Switcher & Navigation:**
   - The top tab bar (`generate`, `arena`, `review`, `stats`) SHALL display a glass container (`bg-slate-100 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06] p-1 rounded-2xl`) with active tabs elevated via subtle neutral glass (`bg-white dark:bg-white/[0.08] text-brand-600 dark:text-white font-bold shadow-sm`).
   - The review queue navigation button SHALL bind to `quiz.tab_review_queue` ("Review Queue" / "Ôn Tập"), resolving missing key errors and never displaying raw untranslated key strings.

8. **Arena Question & Option Cards:**
   - Multiple-choice option cards (A, B, C, D) SHALL present neutral dark glass surfaces in unselected states (`border-white/[0.06] bg-white/[0.03] text-slate-200 hover:border-white/[0.15] hover:bg-white/[0.06]`).
   - Selected options SHALL highlight cleanly with Deep Iris Violet (`border-brand-500 bg-brand-500/10 text-white ring-1 ring-brand-500/30`).
   - Answered states SHALL use calibrated primary brand violet (`border-brand-500/80 bg-brand-500/10 text-brand-700 dark:text-brand-300`) for correct options and rose (`border-rose-500/80 bg-rose-500/10 text-rose-300`) for incorrect choices without heavy opaque backgrounds.
   - Mastered questions, correct choice banners, and review queue completed empty states SHALL display primary brand styling (`text-brand-600 dark:text-brand-400`, `bg-brand-500/10`, `border-brand-500/30`), completely replacing disparate emerald green accents.

9. **Explanation & Markdown Presentation:**
   - The post-answer explanation container SHALL use `.glass-card` with clean typography and Shiki code block integration.

10. **Bilingual Localization Parity:**
   - All status badges, attempt counters, and completion scores SHALL bind to localized dictionary keys:
     - Session score percentage: `{percentage}% {accuracy}` via `quiz.stats_accuracy`.
     - Mistake badge: `quiz.incorrect_badge` ("Incorrect" / "Chưa chính xác").
     - Mistake attempt counter: `quiz.incorrect_attempts` ("{count} incorrect attempts" / "{count} lần làm sai").
     - SM-2 push failure alert: `quiz.toast_push_sm2_failed` ("Failed to push to SM-2 Deck." / "Không thể đưa vào bộ ôn tập SM-2.").

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

### Requirement: Quiz Arena Question & Option Layout Density
The Quiz Arena interface on `/quiz` SHALL style question cards, option buttons, and answer submission controls according to the standardized Cockpit Density standards:
1. **Compact Option Cards**: The multiple-choice choices in the Arena tab SHALL utilize `OptionCard.vue` (`px-3 py-2.5 rounded-lg border text-sm`), eliminating legacy `p-4 sm:p-5 rounded-2xl` option buttons.
2. **Question Card Padding**: The primary question card padding SHALL standardize to `p-4 sm:p-5` with `space-y-4` (eliminating `p-5 sm:p-7 space-y-6`).
3. **Desktop Fold Visibility Invariant**: On standard 1080p desktop viewports, all four multiple-choice options (A, B, C, D) and the "Submit Choice" action button SHALL render completely above the screen fold without requiring vertical scrolling.

#### Scenario: User takes quiz on standard desktop screen
- **WHEN** user answers a quiz question in the Arena tab on `/quiz` on a 1080p desktop display
- **THEN** Question text, options A, B, C, D, and the "Submit Choice" button are all visible within the viewport
- **AND** Option D and the Submit button are not clipped or pushed below the screen fold.

### Requirement: OptionCard Zero-Layout-Shift Tactile Geometry
Option choices in `quiz.vue` and `frontend/components/ui/OptionCard.vue` SHALL maintain strict tactile geometry invariants across all interactive states:
1. **Dimensions & Padding**: Padding (`p-4 sm:p-5`), border widths (`border`), and line-heights MUST remain identical across `default`, `hover`, `selected`, `correct`, and `incorrect` states to prevent layout shifts.
2. **Tactile Option Badges**: Option index badges (`A`, `B`, `C`, `D`) SHALL use `w-7 h-7 sm:w-8 sm:h-8 shrink-0 rounded-lg text-xs font-mono font-bold flex items-center justify-center` with high-contrast active states.
3. **Typography Standard**: Option text MUST render at `text-sm sm:text-base` with `leading-relaxed` for maximum readability across English and Vietnamese questions.
4. **Hairline Border Styling**: Unselected cards SHALL use `border-slate-200/80 dark:border-white/[0.08]` over `bg-white dark:bg-canvas-subtle/50`. Selected cards SHALL highlight with brand accent borders (`border-brand-500`) without changing border thickness.

#### Scenario: User selects an option card in quiz arena
- **WHEN** an engineer selects Option B during a timed quiz question
- **THEN** Option B highlights with active brand colors and the option badge fills with brand contrast, without shifting the vertical position of Option C or Option D.

### Requirement: Arena Mode Telemetry Dock & Hotkey Integration
The Arena Mode dock in `quiz.vue` SHALL display live session metrics and support instant keyboard shortcuts:
1. **Live Countdown & Streak**: Display remaining time with urgent color shifts ($< 10\text{s}$ turns rose-500) and current streak multiplier badge.
2. **Question Navigation Map**: Display compact numeric pill indicators for all questions in the current batch, showing unvisited, active, answered, and mistake statuses.
3. **Keyboard Quick Select**: Pressing number keys `1`, `2`, `3`, `4` or letter keys `A`, `B`, `C`, `D` MUST trigger option selection instantly.

#### Scenario: Engineer answers using keyboard hotkey
- **WHEN** an engineer presses key `2` while viewing a quiz question
- **THEN** Option B is selected immediately without requiring mouse interaction.
