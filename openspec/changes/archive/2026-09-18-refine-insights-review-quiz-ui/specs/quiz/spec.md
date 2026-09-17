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

In the `/quiz` Mastery Stats tab (`activeTab === 'stats'`), the 4-card Bento Grid Dashboard SHALL be organized in a balanced, symmetric 2-column grid layout (`grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6`) on large screens (`lg:`):
1. **Equal Width Bento Cards**: All 4 Bento cards (Hero Performance Card, Spaced Mastery Gauge Card, Seniority Matrix Card, Topic Strengths & Weaknesses Radar Card) SHALL have an equal 50% width on large screens (`lg:`), replacing asymmetric column spans (`lg:col-span-7` and `lg:col-span-5`).
2. **Symmetric Row Composition**:
   - Row 1: Bento Card 1 (Hero Performance Card) in Column 1; Bento Card 2 (Spaced Mastery Gauge Card) in Column 2.
   - Row 2: Bento Card 3 (Seniority Matrix Card) in Column 1; Bento Card 4 (Topic Strengths & Weaknesses Radar Card) in Column 2.
3. **Continuous Vertical Alignment Seam**: The vertical dividing gap between Column 1 and Column 2 SHALL align 100% straight from top to bottom between Row 1 and Row 2 on large viewports.

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
