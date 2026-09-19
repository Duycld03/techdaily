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
The system SHALL generate batches of 5 to 10 multiple-choice questions tailored by topic and seniority level (Fresher, Junior, Middle, Senior) via Gemini 3.6 Flash.

#### Scenario: User requests a new quiz batch
- **WHEN** an authenticated user submits `POST /api/v1/quiz/generate` with a valid topic (2-100 characters), category, level, and count (5 or 10)
- **THEN** the system generates fresh questions using Gemini with 4 options and 1 correct answer, saves them into the `QuizQuestions` table, and returns the questions (excluding previously mastered questions).

#### Scenario: User requests additional questions for the same topic ("Generate More")
- **WHEN** user clicks "Generate More Questions" after completing a quiz
- **THEN** the system passes the titles of existing questions to Gemini's prompt to prevent duplication, saves newly generated questions to `QuizQuestions`, and returns the new batch.

#### Scenario: Gemini API rate limit or network failure
- **WHEN** the Gemini API returns HTTP 429, timeout, or invalid JSON
- **THEN** the system falls back gracefully to existing unmastered questions in the database or structured fallback templates without crashing.

---

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
The system SHALL provide a dedicated review mode to practice unmastered questions and view overall mastery analytics. The quiz summary interface and Mistake Review Queue (`/quiz`) SHALL feature a 1-click action allowing users to promote any failed question directly into their daily SM-2 spaced repetition deck (`POST /api/v1/review/cards/from-quiz-mistake`), rather than confining mistake remediation strictly to manual re-quizzing.

The backend endpoint `GET /api/v1/quiz/review-queue` SHALL accept pagination and filtering parameters: `page` (integer, default 1, minimum 1), `pageSize` (integer, default 10, minimum 1, maximum 100), optional `category` (integer), optional `level` (integer), and optional `topic` (string). The response SHALL return a structured envelope containing:
1. `questions`: A list of unmastered `QuizQuestionDto` items for the requested page slice, ordered by `LastAttemptedAt` descending.
2. `totalCount`: Total number of unmastered questions matching filter criteria.
3. `page`: Active page index (1-based).
4. `pageSize`: Page size applied to the query.
5. `totalPages`: Total number of pages calculated as $\lceil \text{totalCount} / \text{pageSize} \rceil$, or 0 if `totalCount` is 0.

The frontend Review Queue interface on `/quiz` SHALL render accessible pagination controls (`< 1 2 3 ... 8 >`) beneath the question cards when `totalPages > 1`:
1. **Numbered Page Buttons**: Direct navigation buttons for each page in the review queue with active page styling.
2. **Previous / Next Controls**: Navigational buttons to decrement or increment the active page, automatically disabled at boundaries (`page === 1` and `page === totalPages`).
3. **Dual Review CTAs**: The review queue header SHALL provide two distinct practice triggers:
   - "Practice Current Batch (N)" (`startReviewSession(quizStore.reviewQueue)`): Immediately loads the questions on the active page into the interactive Arena player for a focused study session.
   - "Practice All Mistakes (Total N)" (`startReviewSession()` with eager fetch): Loads all unmastered questions across the entire queue into the Arena player.
4. **Two-Way URL Query Synchronization**: The active queue page and filter states SHALL synchronize bidirectionally with URL query parameters (`?tab=review&page=N&category=C&level=L&topic=T`).
5. **Filter Reset**: Modifying category, seniority level, or topic filters SHALL reset the review queue page to 1.

The `/quiz` Stats tab interface SHALL render a responsive 4-card Bento Grid Dashboard:
1. **Hero Performance Card**: Visualizes the overall quiz accuracy rate percentage (`quizStore.stats.accuracyRate`), total questions answered, and unmastered review queue count (`quizStore.stats.reviewQueueCount`), alongside a primary 1-click CTA button allowing users to immediately launch the mistake review queue (`activeTab = 'review'`).
2. **Spaced Mastery Gauge Card**: Features a semi-circular radial SVG gauge displaying Mastery Rate = $(Mastered / TotalAnswered) \times 100\%$ accompanied by motivational proficiency tier badges (e.g. "Trí nhớ xuất sắc" / "Đang rèn luyện" / "Bắt đầu hành trình") matching the spaced repetition design system.
3. **Seniority Matrix Card**: Accurately maps both string enum values (`"Fresher"`, `"Junior"`, `"Middle"`, `"Senior"`) and integer indices (`0`, `1`, `2`, `3`) via the `formatSeniorityLevel` helper, completely resolving the legacy bug where string indexing against array `seniorityLevels` caused all four rows to default to 'Senior'. Displays distinct color-coded progress bars, mastered/answered counters, and accuracy percentages for Fresher, Junior, Mid-Level, and Senior levels.
4. **Topic Strengths & Weaknesses Radar Card**: Renders the `TopicBreakdown` array from `GetQuizStatsResponse` with topic titles, answered count, mastered count, and accuracy badges (Emerald for $\ge 80\%$, Amber for $50\%\text{--}79\%$, and Rose for $< 50\%$), enabling users to identify areas of strength and topics requiring remediation.

#### Scenario: User opens the Mistake Review Queue
- **WHEN** user requests `GET /api/v1/quiz/review-queue?page=1&pageSize=10`
- **THEN** the system returns up to 10 questions where `IsMastered = false` for the user, alongside `totalCount` and `totalPages`
- **AND** renders the question cards with pagination controls and a total mistake counter badge.

#### Scenario: User masters a previously failed question during review
- **WHEN** user re-takes a question from the review queue and answers correctly
- **THEN** `IsMastered` is updated to `true` and the question is removed from active review queues
- **AND** the review queue total count decrements by 1.

#### Scenario: User requests quiz mastery statistics
- **WHEN** user requests `GET /api/v1/quiz/stats`
- **THEN** the system returns total answered, total mastered, unmastered count, and mastery rate percentage by category.

#### Scenario: User promotes an incorrect question from quiz results
- **WHEN** user finishes a quiz batch with one or more incorrect answers
- **THEN** the results card displays a `🔄 Push to SM-2 Deck` button alongside each mistake explanation.

#### Scenario: User clicks Push to SM-2 Deck button
- **WHEN** user clicks `🔄 Push to SM-2 Deck` for a question
- **THEN** the client sends `POST /api/v1/review/cards/from-quiz-mistake` with `questionId`, the button updates to a disabled checkmark state ("Added to SM-2 Deck"), and a localized toast confirms scheduling for today's review session.

#### Scenario: User navigates mistake review queue with numbered pagination controls
- **GIVEN** a user has 35 unmastered questions in the review queue (`totalPages = 4` with `pageSize = 10`)
- **WHEN** the user opens the "Review Queue" tab on `/quiz` and clicks page number "2"
- **THEN** client calls `GET /api/v1/quiz/review-queue?page=2&pageSize=10`
- **AND** updates URL query string to `?tab=review&page=2`
- **AND** renders questions 11 through 20 with page button "2" highlighted.

#### Scenario: User synchronizes review queue page with URL query parameters
- **WHEN** a user navigates directly to `/quiz?tab=review&page=3`
- **THEN** client sets `activeTab = 'review'` in `useInterviewQuizStore`
- **AND** fetches `GET /api/v1/quiz/review-queue?page=3&pageSize=10`
- **AND** displays page 3 of the mistake queue with pagination controls centered on page 3.

#### Scenario: User launches practice session for current review queue batch
- **GIVEN** the user is viewing page 2 of the mistake queue (displaying questions 11 to 20)
- **WHEN** the user clicks "Practice Current Batch (10)"
- **THEN** client loads only the 10 questions from page 2 into the Arena question list
- **AND** transitions `activeTab = 'arena'` starting at question 1 of 10.

#### Scenario: User launches practice session for all unmastered questions in review queue
- **GIVEN** the user has 35 total unmastered questions across 4 pages
- **WHEN** the user clicks "Practice All Mistakes (35)"
- **THEN** client eagerly retrieves all 35 unmastered questions from the backend
- **AND** transitions to the Arena player with all 35 questions enqueued for comprehensive remediation.

#### Scenario: User views quiz stats tab with bento dashboard layout
- **GIVEN** an authenticated user who has completed quiz sessions and navigated to `/quiz`
- **WHEN** the user selects the "Thống kê" ("Stats") tab
- **THEN** the client displays the 4-card Bento Grid Dashboard including Hero Performance, Spaced Mastery Gauge, Seniority Matrix, and Topic Strengths & Weaknesses cards
- **AND** the overview metrics reactively display total answered count, mastered count, and accuracy rate percentage.

#### Scenario: User views quiz stats tab with symmetric 2-column bento grid
- **GIVEN** an authenticated user on `/quiz` with answered quiz questions
- **WHEN** user selects the Stats tab (`activeTab === 'stats'`) on a desktop screen ($\ge 1024\text{px}$)
- **THEN** client renders the 4-card Bento Grid Dashboard in a symmetric 2-column grid (`grid-cols-1 lg:grid-cols-2`)
- **AND** Hero Performance Card and Spaced Mastery Gauge Card render on Row 1 with identical 50% widths
- **AND** Seniority Matrix Card and Topic Strengths & Weaknesses Radar Card render on Row 2 with identical 50% widths.

#### Scenario: Straight vertical divider seam between column 1 and column 2 across both bento rows
- **GIVEN** the Bento Grid Dashboard on `/quiz` is rendered on a large screen
- **WHEN** inspecting the vertical dividing space between Column 1 and Column 2
- **THEN** the boundary line separating Card 1 and Card 2 in Row 1 aligns directly and continuously with the boundary line separating Card 3 and Card 4 in Row 2
- **AND** no staggered horizontal offset or column-span asymmetry occurs.

#### Scenario: User inspects seniority breakdown with string-serialized levels
- **GIVEN** `GetQuizStatsResponse` returns `LevelBreakdown` containing string enum values (`"Fresher"`, `"Junior"`, `"Middle"`, `"Senior"`)
- **WHEN** the Seniority Matrix card renders the breakdown rows
- **THEN** the `formatSeniorityLevel` helper maps each string to its distinct label ("Fresher / Entry", "Junior", "Mid-Level", "Senior / Staff")
- **AND** the UI renders four distinct seniority rows with individual progress bars, preventing any row from improperly falling back to 'Senior'.

#### Scenario: User inspects topic strengths and weaknesses card
- **GIVEN** `GetQuizStatsResponse` returns `TopicBreakdown` with various topics and accuracy rates
- **WHEN** the user inspects the Topic Strengths & Weaknesses card in the Bento Dashboard
- **THEN** topics with accuracy $\ge 80\%$ display an emerald high-mastery badge
- **AND** topics with accuracy between $50\%$ and $79\%$ display an amber developing badge
- **AND** topics with accuracy $< 50\%$ display a rose remediation badge.

#### Scenario: User initiates review session directly from hero performance card
- **GIVEN** the user has 3 unmastered questions in the review queue
- **WHEN** the user clicks the "Review Mistakes (3)" button on the Hero Performance Card
- **THEN** the quiz interface immediately transitions to the Review tab (`activeTab = 'review'`)
- **AND** loads the unmastered questions into the review arena without requiring full page navigation.

#### Scenario: Bilingual visual verification of quiz stats bento dashboard
- **GIVEN** the user is viewing the `/quiz` Stats Bento Dashboard
- **WHEN** the user toggles the application locale between English (`en`) and Vietnamese (`vi`)
- **THEN** all Bento card titles, level labels, proficiency tier badges, and action button labels update reactively
- **AND** Vietnamese diacritics and technical labels render cleanly without layout overflow or truncated text.

### Requirement: Book & Curriculum Grounded Quiz Generation
The `GenerateQuizHandler` SHALL support generating scenario-based technical questions grounded in specific document books or curriculum slices using pgvector similarity search.

#### Scenario: User requests quiz based on a specific book
- **WHEN** user initiates quiz generation with `BookId` specified
- **THEN** generator retrieves document slices from that book, extracts key architectural tradeoffs, and prompts Gemini to create scenario questions strictly grounded in those excerpts.

#### Scenario: User requests quiz grounded in topic curriculum
- **WHEN** user requests quiz for a broad topic (e.g., "PostgreSQL Indexing & MVCC") with grounding enabled
- **THEN** generator uses pgvector to find top matching curriculum slices, includes the slice excerpts as authoritative reference context, and synthesizes deep-dive scenario questions referencing those architectural patterns.

#### Scenario: Excerpt citation in quiz question explanations
- **WHEN** user submits an answer to a grounded quiz question
- **THEN** explanation section includes a "Source Reference" badge indicating the book title, chapter slice, and context excerpt from which the question was derived.

---

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

### Requirement: Daily Drill Status Resiliency & State Deserialization
The daily interview challenge components (`InterviewChallengePane.vue` and `today.vue`) SHALL handle both string (`"Reviewed"`, `"reviewed"`, `"Submitted"`) and integer (`2`, `1`) representations of `DrillStatus`. Upon page refresh, when a previously completed drill is retrieved from `GET /api/v1/daily/today`, the interface SHALL preserve the reviewed state, restore the user's selected option, and display the score, architectural explanation, and header completed badge without reverting to an unsubmitted state.

#### Scenario: User refreshes page after completing daily interview challenge
- **WHEN** an authenticated user completes the daily scenario challenge and later refreshes `/today`
- **THEN** the API returns the persisted drill entity with `status = "Reviewed"`
- **AND** the scenario challenge pane recognizes the drill as reviewed (`isReviewed = true`)
- **AND** the header completed badge remains visible
- **AND** the submit button remains hidden, rendering the architectural explanation card and feedback banner.

#### Scenario: Preserving selected option index and feedback across sessions
- **WHEN** user reloads the today view for an already reviewed drill with `selectedOptionIndex = 2`
- **THEN** option 2 is visually selected and styled with appropriate correctness highlighting (emerald for correct, rose for incorrect)
- **AND** the user cannot re-submit or alter the completed drill.

---

### Requirement: Natural Vertical Layout for Scenario Challenge Interface
The scenario multiple-choice challenge container in `InterviewChallengePane.vue` SHALL utilize natural vertical content stacking (`flex flex-col justify-start gap-5`) rather than artificial edge stretching (`justify-between`). Multiple-choice options, contextual alert banners, submit controls, and post-submission explanations SHALL follow in continuous vertical sequence without arbitrary 300–400px empty gaps on widescreen viewports.

#### Scenario: Natural vertical spacing without layout gaps
- **WHEN** viewing the scenario challenge on a wide desktop screen (>1280px) with extended vertical height
- **THEN** the question header, options list, submit button, and explanation card maintain consistent vertical spacing (`space-y-5 sm:space-y-6`) without pushing controls to the extreme bottom of the viewport.

#### Scenario: Responsive layout during post-submission feedback
- **WHEN** the drill transitions to reviewed state
- **THEN** the result banner and deep-dive explanation card render directly beneath the options list with natural spacing.

---

### Requirement: Flashcard Generation Idempotency from Multiple Sources
The spaced repetition bridge SHALL guarantee idempotency for card creation across all source channels (highlight conversions via `POST /api/v1/review/cards/from-highlight` and quiz mistakes via `POST /api/v1/review/cards/from-quiz-mistake`). Calling card generation endpoints repeatedly for the same source entity SHALL NOT create duplicate records in `SpacedRepetitionCards`.

#### Scenario: Idempotent card generation from reading highlight
- **WHEN** user invokes card generation for a reading highlight that was already transformed into a flashcard
- **THEN** the backend identifies the existing card by `SourceHighlightId` and returns the existing card details with `HTTP 200 OK` without creating redundant database records.

#### Scenario: Idempotent card generation from quiz mistake
- **WHEN** user invokes `POST /api/v1/review/cards/from-quiz-mistake` for a question already in the user's review deck
- **THEN** the backend resets the existing card's review schedule to today and returns `HTTP 200 OK` without duplicating records.

### Requirement: Interview Quiz Studio Visual Layout & Interactive Tokens
The `/quiz` route SHALL implement the **Dev-Learning Studio** visual language and 100% bilingual localization across all 5 internal tabs (`generate`, `arena`, `summary`, `review`, `stats`):

1. **Canvas & Card Consistency:**
   - The root `/quiz` container SHALL render over `dark:bg-canvas` (`#09090b` obsidian base) instead of legacy slate.
   - All tab content containers, topic generation cards, and Bento statistics cards SHALL utilize `.glass-card` styling with subtle hairline borders (`border-white/[0.06]`).

2. **Interactive Tab Switcher & Navigation:**
   - The top tab bar (`generate`, `arena`, `review`, `stats`) SHALL display a glass container (`bg-slate-100 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06] p-1 rounded-2xl`) with active tabs elevated via subtle neutral glass (`bg-white dark:bg-white/[0.08] text-brand-600 dark:text-white font-bold shadow-sm`).
   - The review queue navigation button SHALL bind to `quiz.tab_review_queue` ("Review Queue" / "Ôn Tập"), resolving missing key errors and never displaying raw untranslated key strings.

3. **Arena Question & Option Cards:**
   - Multiple-choice option cards (A, B, C, D) SHALL present neutral dark glass surfaces in unselected states (`border-white/[0.06] bg-white/[0.03] text-slate-200 hover:border-white/[0.15] hover:bg-white/[0.06]`).
   - Selected options SHALL highlight cleanly with Deep Iris Violet (`border-brand-500 bg-brand-500/10 text-white ring-1 ring-brand-500/30`).
   - Answered states SHALL use calibrated emerald (`border-emerald-500/80 bg-emerald-500/10 text-emerald-300`) for correct options and rose (`border-rose-500/80 bg-rose-500/10 text-rose-300`) for incorrect choices without heavy opaque backgrounds.

4. **Explanation & Markdown Presentation:**
   - The post-answer explanation container SHALL use `.glass-card` with clean typography and Shiki code block integration.

5. **Bilingual Localization Parity:**
   - All status badges, attempt counters, and completion scores SHALL bind to localized dictionary keys:
     - Session score percentage: `{percentage}% {accuracy}` via `quiz.stats_accuracy`.
     - Mistake badge: `quiz.incorrect_badge` ("Incorrect" / "Chưa chính xác").
     - Mistake attempt counter: `quiz.incorrect_attempts` ("{count} incorrect attempts" / "{count} lần làm sai").
     - SM-2 push failure alert: `quiz.toast_push_sm2_failed` ("Failed to push to SM-2 Deck." / "Không thể đưa vào bộ ôn tập SM-2.").

#### Scenario: User navigates across quiz studio tabs
- **WHEN** user selects any tab in `/quiz` (`generate`, `arena`, `review`, `stats`)
- **THEN** the active tab highlights with a neutral glass elevation and white text
- **AND** the tab content renders on an obsidian base with hairline borders.

#### Scenario: User selects multiple-choice option in arena
- **WHEN** user clicks an option card (A, B, C, or D) before submitting
- **THEN** the option card highlights with Deep Iris Violet borders and subtle violet tint
- **AND** does not display harsh opaque colors or visual noise.

#### Scenario: Bilingual localization of review queue tab and mistake indicators
- **WHEN** user views `/quiz` in Vietnamese mode (`vi`)
- **THEN** the review queue tab displays "Ôn Tập" via `quiz.tab_review_queue` instead of the raw key string `quiz.tab_review_queue`
- **AND** incorrect answer badges in summary and review list render "Chưa chính xác" and "{count} lần làm sai" via `quiz.incorrect_badge` and `quiz.incorrect_attempts`
- **AND** the session summary score renders `{percentage}% Chính Xác` instead of hardcoded English.
