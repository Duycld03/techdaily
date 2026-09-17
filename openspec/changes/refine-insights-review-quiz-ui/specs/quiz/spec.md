## MODIFIED Requirements

### Requirement: Mistake Review Queue & Mastery Analytics
The system SHALL provide a dedicated review mode to practice unmastered questions and view overall mastery analytics. The quiz summary interface and Mistake Review Queue (`/quiz`) SHALL feature a 1-click action allowing users to promote any failed question directly into their daily SM-2 spaced repetition deck (`POST /api/v1/review/cards/from-quiz-mistake`), rather than confining mistake remediation strictly to manual re-quizzing.

In the `/quiz` Mastery Stats tab (`activeTab === 'stats'`), the 4-card Bento Grid Dashboard SHALL be organized in a balanced, symmetric 2-column grid layout (`grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6`) on large screens (`lg:`):
1. **Equal Width Bento Cards**: All 4 Bento cards (Hero Performance Card, Spaced Mastery Gauge Card, Seniority Matrix Card, Topic Strengths & Weaknesses Radar Card) SHALL have an equal 50% width on large screens (`lg:`), replacing asymmetric column spans (`lg:col-span-7` and `lg:col-span-5`).
2. **Symmetric Row Composition**:
   - Row 1: Bento Card 1 (Hero Performance Card) in Column 1; Bento Card 2 (Spaced Mastery Gauge Card) in Column 2.
   - Row 2: Bento Card 3 (Seniority Matrix Card) in Column 1; Bento Card 4 (Topic Strengths & Weaknesses Radar Card) in Column 2.
3. **Continuous Vertical Alignment Seam**: The vertical dividing gap between Column 1 and Column 2 SHALL align 100% straight from top to bottom between Row 1 and Row 2 on large viewports.

#### Scenario: User opens the Mistake Review Queue
- **WHEN** user requests `GET /api/v1/quiz/review-queue`
- **THEN** the system returns all questions where `IsMastered = false` for the user, allowing targeted re-practice.

#### Scenario: User masters a previously failed question during review
- **WHEN** user re-takes a question from the review queue and answers correctly
- **THEN** `IsMastered` is updated to `true` and the question is removed from active review queues.

#### Scenario: User requests quiz mastery statistics
- **WHEN** user requests `GET /api/v1/quiz/stats`
- **THEN** the system returns total answered, total mastered, unmastered count, and mastery rate percentage by category.

#### Scenario: User promotes an incorrect question from quiz results
- **WHEN** user finishes a quiz batch with one or more incorrect answers
- **THEN** the results card displays a `🔄 Push to SM-2 Deck` button alongside each mistake explanation.

#### Scenario: User clicks Push to SM-2 Deck button
- **WHEN** user clicks `🔄 Push to SM-2 Deck` for a question
- **THEN** the client sends `POST /api/v1/review/cards/from-quiz-mistake` with `questionId`, the button updates to a disabled checkmark state ("Added to SM-2 Deck"), and a localized toast confirms scheduling for today's review session.

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
