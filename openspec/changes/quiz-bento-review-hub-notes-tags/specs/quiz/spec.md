## MODIFIED Requirements

### Requirement: Mistake Review Queue & Mastery Analytics
The system SHALL provide a dedicated review mode to practice unmastered questions and view overall mastery analytics. The quiz summary interface and Mistake Review Queue (`/quiz`) SHALL feature a 1-click action allowing users to promote any failed question directly into their daily SM-2 spaced repetition deck (`POST /api/v1/review/cards/from-quiz-mistake`), rather than confining mistake remediation strictly to manual re-quizzing.

The `/quiz` Stats tab interface SHALL render a responsive 4-card Bento Grid Dashboard replacing flat counter rows:
1. **Hero Performance Card**: Visualizes the overall quiz accuracy rate percentage (`quizStore.stats.accuracyRate`), total questions answered, and unmastered review queue count (`quizStore.stats.reviewQueueCount`), alongside a primary 1-click CTA button allowing users to immediately launch the mistake review queue (`activeTab = 'review'`).
2. **Spaced Mastery Gauge Card**: Features a semi-circular radial SVG gauge displaying Mastery Rate = $(Mastered / TotalAnswered) \times 100\%$ accompanied by motivational proficiency tier badges (e.g. "Trí nhớ xuất sắc" / "Đang rèn luyện" / "Bắt đầu hành trình") matching the spaced repetition design system.
3. **Seniority Matrix Card**: Accurately maps both string enum values (`"Fresher"`, `"Junior"`, `"Middle"`, `"Senior"`) and integer indices (`0`, `1`, `2`, `3`) via the `formatSeniorityLevel` helper, completely resolving the legacy bug where string indexing against array `seniorityLevels` caused all four rows to default to 'Senior'. Displays distinct color-coded progress bars, mastered/answered counters, and accuracy percentages for Fresher, Junior, Mid-Level, and Senior levels.
4. **Topic Strengths & Weaknesses Radar Card**: Renders the `TopicBreakdown` array from `GetQuizStatsResponse` with topic titles, answered count, mastered count, and accuracy badges (Emerald for $\ge 80\%$, Amber for $50\%\text{--}79\%$, and Rose for $< 50\%$), enabling users to identify areas of strength and topics requiring remediation.

#### Scenario: User opens the Mistake Review Queue
- **GIVEN** an authenticated user with unmastered questions in their history
- **WHEN** user requests `GET /api/v1/quiz/review-queue`
- **THEN** the system returns all questions where `IsMastered = false` for the user, allowing targeted re-practice.

#### Scenario: User masters a previously failed question during review
- **GIVEN** a question currently in the user's review queue with `IsMastered = false`
- **WHEN** user re-takes a question from the review queue and answers correctly
- **THEN** `IsMastered` is updated to `true` and the question is removed from active review queues.

#### Scenario: User requests quiz mastery statistics
- **GIVEN** an authenticated user who has answered questions across multiple categories
- **WHEN** user requests `GET /api/v1/quiz/stats`
- **THEN** the system returns total answered, total mastered, unmastered count, and mastery rate percentage by category.

#### Scenario: User promotes an incorrect question from quiz results
- **GIVEN** a completed quiz session containing one or more mistakes
- **WHEN** user finishes a quiz batch with one or more incorrect answers
- **THEN** the results card displays a `🔄 Push to SM-2 Deck` button alongside each mistake explanation.

#### Scenario: User clicks Push to SM-2 Deck button
- **GIVEN** a displayed quiz mistake with the Push to SM-2 Deck button
- **WHEN** user clicks `🔄 Push to SM-2 Deck` for a question
- **THEN** the client sends `POST /api/v1/review/cards/from-quiz-mistake` with `questionId`, the button updates to a disabled checkmark state ("Added to SM-2 Deck"), and a localized toast confirms scheduling for today's review session.

#### Scenario: User views quiz stats tab with bento dashboard layout
- **GIVEN** an authenticated user who has completed quiz sessions and navigated to `/quiz`
- **WHEN** the user selects the "Thống kê" ("Stats") tab
- **THEN** the client displays the 4-card Bento Grid Dashboard including Hero Performance, Spaced Mastery Gauge, Seniority Matrix, and Topic Strengths & Weaknesses cards
- **AND** the overview metrics reactively display total answered count, mastered count, and accuracy rate percentage.

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
