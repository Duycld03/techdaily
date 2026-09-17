## MODIFIED Requirements

### Requirement: Flashcard Deck Library Querying & Metrics
The backend SHALL expose `GET /api/v1/review/cards` to retrieve a paginated list of flashcards belonging to the authenticated user, supporting optional keyword search across front and back markdown, status filtering (`Learning`, `Reviewing`, `Mastered`), and source type filtering (`Topic`, `Highlight`, `QuizMistake`). The response SHALL include deck statistics counting cards in each mastery status.

The frontend deck management interface SHALL present an E-Learning Bento Overview replacing plain flat stat boxes, comprising:
1. A Hero Action Card displaying the count of cards due today, an estimated study time (~5 minutes), and a 1-click CTA button to launch the interactive review session.
2. A Semi-Circular Mastery Gauge calculating Mastery Rate = $(Mastered / Total) \times 100\%$ with a motivational level badge (e.g. "Trí nhớ xuất sắc", "Đang xây dựng phản xạ").
3. A 7-Day Review Forecast Mini Bar Chart forecasting upcoming card review volumes across the next 7 days (Monday through Sunday).

The frontend SHALL provide a Two-Tier Search and Advanced Filter Sheet immune to Vietnamese diacritic text wrapping:
1. A Quick Bar featuring a keyword search input with `⌘K` keyboard shortcut, essential quick chips (`All`, `Due Today`, `Mastered`), and an `[ ⚙️ Advanced Filter ]` trigger button with an active filter count badge.
2. An Advanced Filter Modal/Sheet popover providing multi-section selections for Knowledge Source (`Topic`, `Highlight`, `QuizMistake`), Mastery Stage (`Learning`, `Reviewing`, `Mastered`), Due Date Urgency, and Sorting options (`Next review date`, `Difficulty Ease Factor`, `Recently created`), with Reset and Apply actions.

The card listing SHALL render as a 2-3 column responsive Knowledge Card Grid replacing rigid HTML tables, featuring:
1. Prominently rendered Front question prompt with source badge and mastery status badge.
2. An interactive accordion toggle ("Xem đáp án" / "Show Answer") smoothly expanding the Back markdown answer with syntax-highlighted code blocks.
3. An SM-2 metrics footer displaying Repetitions, Interval Days, Ease Factor, and Next Review Date, alongside action buttons for Edit modal, Reset progression confirmation, and Soft Delete confirmation.

The Bento Deck Dashboard and Advanced Filter sheet SHALL adapt responsively across Desktop (≥ 1280px, 3-column asymmetric overview and 3-column bento card grid) and Mobile (~375px - 390px, single-column vertically stacked cards and full-screen filter sheet) viewports without horizontal scrolling or layout breakage.

All visual elements, buttons, badges, metrics, and filter controls in `/review` SHALL support bilingual rendering (English and Vietnamese), ensuring that longer Vietnamese diacritic strings and technical labels render without text clipping, awkward word wrapping, or breaking card container bounds.

#### Scenario: User views deck statistics
- **WHEN** user views the "Deck Management" tab
- **THEN** UI displays counter summary cards for Total Cards, Learning, Reviewing, and Mastered calculated from the user's active flashcards.

#### Scenario: User searches and filters flashcard deck
- **WHEN** user inputs a search keyword (e.g. "PostgreSQL") and selects status filter "Learning" and source filter "Highlight"
- **THEN** client calls `GET /api/v1/review/cards?search=PostgreSQL&status=Learning&sourceType=Highlight&page=1&pageSize=20`
- **AND** table renders only matching flashcards with SM-2 metrics (Repetitions, Interval Days, Ease Factor, Next Review Date).

#### Scenario: User paginates through flashcard deck
- **WHEN** user clicks to advance to page 2 of the deck
- **THEN** client requests page 2 with existing search and filter criteria preserved, smoothly replacing table rows.

#### Scenario: User views bento overview with hero action card and mastery gauge
- **WHEN** user navigates to the flashcard deck management view
- **THEN** the Bento Overview renders the Hero Action Card showing cards due today, estimated review time, and a 1-click CTA to start the review session
- **AND** the semi-circular Mastery Gauge visualizes the percentage of mastered cards with a motivational proficiency tier badge
- **AND** the 7-day Review Forecast chart displays projected card review volumes for each day of the upcoming week.

#### Scenario: User inspects 7-day review forecast
- **WHEN** user views the 7-day forecast chart in the Bento Overview
- **THEN** chart displays 7 distinct vertical day bars (Monday through Sunday) with relative heights reflecting the distribution of upcoming cards due based on their scheduled review dates.

#### Scenario: User opens advanced filter modal and applies multi-criteria filters
- **WHEN** user clicks the `[ ⚙️ Advanced Filter ]` button in the quick search bar
- **THEN** the advanced filter modal opens displaying source type, mastery stage, urgency, and sorting options without layout wrapping
- **WHEN** user selects specific filter criteria and clicks "Apply"
- **THEN** the modal closes, the quick bar reflects the active filter count badge, and the card grid updates to display matching flashcards.

#### Scenario: User toggles knowledge card answer accordion in bento grid
- **WHEN** user views cards in the 2-3 column responsive knowledge card grid
- **AND** clicks the "Xem đáp án" (Show Answer) toggle on a specific card
- **THEN** the card accordion smoothly expands to reveal the back markdown answer with code syntax highlighting without shifting neighboring cards in the grid.

#### Scenario: Desktop and mobile responsive layouts for bento cards and advanced filter sheet
- **WHEN** user accesses `/review` on a desktop viewport ($\ge 1280\text{px}$)
- **THEN** the Bento Overview renders in an asymmetric 3-column layout (Hero 50%, Gauge 25%, Forecast 25%) and the knowledge cards render in a responsive 3-column grid
- **AND** the advanced filter opens as a centered modal popover
- **WHEN** user accesses `/review` on a mobile viewport ($375\text{px} - 390\text{px}$)
- **THEN** the overview cards stack into a single vertical column, the knowledge cards stack into a single column with touch-friendly targets, and the advanced filter renders as a mobile-optimized full-screen or bottom sheet without horizontal scrolling.

#### Scenario: Bilingual visual verification of review dashboard without layout degradation
- **WHEN** user toggles the interface language between English (`en`) and Vietnamese (`vi`) on the `/review` page
- **THEN** all UI strings including hero action copy, mastery tier badges, 7-day forecast labels, filter modal categories, and card action buttons update reactively
- **AND** longer Vietnamese text strings (such as "Đang xây dựng phản xạ", "Bộ lọc & Sắp xếp kho thẻ", and "Xem đáp án") render without text overflow, truncated labels, or clipping badge containers.
