## MODIFIED Requirements

### Requirement: Spaced Repetition Dual-Mode Navigation
The `/review` page SHALL provide a top-level dual-mode tab switcher allowing software engineers to toggle seamlessly between the active review session ("Review Session" / "Ôn tập hôm nay") and the complete flashcard deck library ("Deck Management" / "Kho thẻ của tôi").

When the active review session is complete or when no cards are currently due for review (`activeTab === 'session' && (!currentCard || reviewStore.cards.length === 0)`), the `/review` page SHALL render a cleanly centered, celebratory completion hero card:
1. **Celebratory Hero Banner**: Features a triumphant completion icon (`CheckCircle`), celebratory heading (`review.no_cards`), encouraging retention copy (`review.no_cards_desc`), and triggers a celebratory confetti burst upon completing the final due card of the session.
2. **Dedicated Action Controls**:
   - Primary CTA: "Browse Full Deck (N cards)" (`review.browse_deck_btn`), smoothly transitioning the active tab to "Deck Management" (`activeTab = 'management'`) with the total card count prominently displayed.
   - Secondary CTA: "Cram / Extended Practice" (`review.cram_practice_btn`), routing to `/today` for extended scenario challenge practice.
3. **Card Deduplication**: The completion state SHALL NOT embed duplicate `MasteryGaugeCard` or `ReviewForecastChart` components, preserving these comprehensive retention analytics exclusively in Tab 2 ("Kho thẻ của tôi" / Deck Management) to maintain a focused, decluttered completion celebration.

#### Scenario: User navigates between review session and deck management
- **WHEN** user loads `/review`
- **THEN** page defaults to the "Review Session" tab if due cards exist, or allows switching to "Deck Management"
- **WHEN** user clicks the "Deck Management" tab
- **THEN** client transitions to the deck management view and loads card statistics and the paginated card library without page reload.

#### Scenario: User completes review session
- **WHEN** user finishes grading all due cards in the "Review Session" tab
- **THEN** player displays a completion state with confetti celebration and provides a 1-click shortcut to inspect the full card deck in the "Deck Management" tab.

#### Scenario: User completes daily review session and views focused completion hero card
- **GIVEN** an authenticated user who finishes grading the last due card or has zero cards due today
- **WHEN** user views the Review Session tab (`activeTab === 'session'`)
- **THEN** client renders only the centered Celebratory Completion Hero Card with confetti
- **AND** displays the primary CTA `[ 📚 Khám phá kho thẻ (N thẻ) ]` and secondary CTA `[ ⚡ Luyện tập mở rộng ]`
- **AND** does NOT render the duplicate Mastery Gauge Card or Review Forecast Chart beneath the hero card.

#### Scenario: User transitions from completion hero card to deck management tab
- **GIVEN** user is viewing the Celebratory Completion Hero Card
- **WHEN** user clicks `[ 📚 Khám phá kho thẻ (N thẻ) ]`
- **THEN** client immediately switches `activeTab` to `'management'`
- **AND** renders the 3-card Bento Overview (including Mastery Gauge Card and Review Forecast Chart) alongside the complete paginated flashcard library.

---

### Requirement: Flashcard Deck Library Querying & Metrics
The backend SHALL expose `GET /api/v1/review/cards` to retrieve a paginated list of flashcards belonging to the authenticated user, supporting optional keyword search across front and back markdown, status filtering (`Learning`, `Reviewing`, `Mastered`), and source type filtering (`Topic`, `Highlight`, `QuizMistake`). The response SHALL include deck statistics counting cards in each mastery status.

The frontend deck management interface SHALL present an E-Learning Bento Overview replacing plain flat stat boxes, comprising:
1. A Hero Action Card displaying the count of cards due today, an estimated study time (~5 minutes), and a 1-click CTA button to launch the interactive review session.
2. A Semi-Circular Mastery Gauge calculating Mastery Rate = $(Mastered / Total) \times 100\%$ with a motivational level badge (e.g. "Trí nhớ xuất sắc", "Đang xây dựng phản xạ").
3. A 7-Day Review Forecast Mini Bar Chart forecasting upcoming card review volumes across the next 7 days (Monday through Sunday).

The frontend SHALL provide a Two-Tier Search and Advanced Filter Sheet immune to Vietnamese diacritic text wrapping:
1. A Quick Bar featuring a keyword search input with `⌘K` keyboard shortcut, essential quick chips (`All`, `Due Today`, `Mastered`), and an `[ ⚙️ Advanced Filter ]` trigger button with an active filter count badge.
2. An Advanced Filter Modal popover (`AdvancedFilterModal.vue`) adhering to a standardized visual contract:
   - All checkmark icons (`<Check>`) SHALL be omitted from filter option buttons across all sections to eliminate horizontal width bloat and prevent multi-line button label wrapping.
   - Active filter option buttons SHALL apply a single, uniform brand active state: `bg-brand-600 text-white font-bold border-transparent shadow-sm`, eliminating inconsistent multi-color ("rainbow") themes across filter dimensions.
   - All filter option buttons SHALL apply `whitespace-nowrap` to enforce single-line label rendering across all viewports and modal widths.

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

#### Scenario: User inspects Advanced Filter Modal options with normalized button styling
- **GIVEN** user opens the Advanced Filter Modal on `/review`
- **WHEN** user views option buttons across Knowledge Source, Mastery Stage, Due Urgency, and Sort Options
- **THEN** all option buttons display text-only labels with `whitespace-nowrap`
- **AND** no checkmark icon components are rendered inside any option button.

#### Scenario: User applies filters with uniform brand active states without text wrapping
- **GIVEN** user is selecting filters in the Advanced Filter Modal
- **WHEN** user selects source type "Highlight" and status "Learning"
- **THEN** both selected buttons render with uniform brand active styling (`bg-brand-600 text-white font-bold border-transparent shadow-sm`)
- **AND** button labels "Ghi chú đã lưu" and "Đang học" render completely on a single line without text wrapping or layout distortion.
