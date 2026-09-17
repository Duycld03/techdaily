## MODIFIED Requirements

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
