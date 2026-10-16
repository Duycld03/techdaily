# Spec Delta

## MODIFIED Requirements

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
