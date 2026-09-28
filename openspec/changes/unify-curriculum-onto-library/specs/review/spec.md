## MODIFIED Requirements

### Requirement: Flashcard Deck Library Querying & Metrics
The backend SHALL expose `GET /api/v1/review/cards` to retrieve a paginated list of flashcards belonging to the authenticated user, supporting optional keyword search across front and back markdown, status filtering (`Learning`, `Reviewing`, `Mastered`), and source type filtering (`DocumentChunk` / track-drill, `Highlight`, `QuizMistake`). The response SHALL include deck statistics counting cards in each mastery status (`DeckStatisticsDto`), alongside pagination metadata (`cards`, `totalCount`, `page`, `pageSize`, and calculated `totalPages = (int)Math.Ceiling((double)totalCount / pageSize)`).

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

The deck management interface SHALL feature complete, accessible numbered pagination controls (`< 1 2 3 ... 8 >`) replacing bare Previous/Next text links:
1. **Interactive Numbered Buttons**: Direct page navigation buttons for all available pages or truncated windows, highlighting the active page with distinct accent styling.
2. **Ellipsis Compaction**: When total pages exceed 7, the pagination bar displays smart windowing with non-clickable ellipsis dividers (e.g. `[1] 2 3 ... 10`, `1 ... 4 [5] 6 ... 10`, `1 ... 8 9 [10]`).
3. **Previous / Next Controls**: Navigational buttons with SVG chevron icons to decrement or increment the active page, automatically disabled at boundaries.
4. **Filter Reset**: Modifying search keywords, quick chips, or advanced filter criteria SHALL automatically reset the deck page index to 1.
5. **Two-Way URL Synchronization**: The active page index, search query, status filter, and source type SHALL synchronize bidirectionally with URL query parameters (`?page=N&search=S&status=X&sourceType=Y`).
6. **Smooth Viewport Reset**: Switching pages SHALL smoothly scroll the user's viewport to the top of the knowledge card container to preserve visual continuity.

The Bento Deck Dashboard and Advanced Filter sheet SHALL adapt responsively across Desktop (≥ 1280px, 3-column asymmetric overview and 3-column bento card grid) and Mobile (~375px - 390px, single-column vertically stacked cards and full-screen filter sheet) viewports without horizontal scrolling or layout breakage.

All visual elements, buttons, badges, metrics, and filter controls in `/review` SHALL support bilingual rendering (English and Vietnamese), ensuring that longer Vietnamese diacritic strings and technical labels render without text clipping, awkward word wrapping, or breaking card container bounds.

#### Scenario: User views deck statistics
- **WHEN** user views the "Deck Management" tab
- **THEN** UI displays counter summary cards for Total Cards, Learning, Reviewing, and Mastered calculated from the user's active flashcards.

#### Scenario: User searches and filters flashcard deck
- **WHEN** user inputs a search keyword (e.g. "PostgreSQL") and selects status filter "Learning" and source filter "Highlight"
- **THEN** client calls `GET /api/v1/review/cards?search=PostgreSQL&status=Learning&sourceType=Highlight&page=1&pageSize=20`
- **AND** table renders only matching flashcards with SM-2 metrics (Repetitions, Interval Days, Ease Factor, Next Review Date).

#### Scenario: User filters deck by document-chunk drill source
- **WHEN** user selects the `DocumentChunk` / track-drill source type filter
- **THEN** client calls `GET /api/v1/review/cards?sourceType=DocumentChunk&page=1&pageSize=20`
- **AND** the grid renders only cards derived from document-chunk drill failures, with no `Topic` source-type option offered.

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

#### Scenario: User navigates deck using numbered pagination bar and ellipsis
- **GIVEN** a flashcard deck with 140 cards (`totalPages = 7` with `pageSize = 20`)
- **WHEN** the user opens the "Deck Management" tab
- **THEN** the pagination controls render numbered buttons `1`, `2`, `3`, `4`, `5`, `6`, `7` with page `1` highlighted
- **WHEN** the user clicks button `4`
- **THEN** client updates the URL query string to `?page=4`
- **AND** fetches page 4 of the deck and smoothly scrolls viewport to the card grid top.

#### Scenario: User synchronizes deck page number and filter state via URL query parameters
- **WHEN** a user visits `/review?tab=deck&page=3&status=Learning&search=concurrency`
- **THEN** client parses query parameters into `useReviewStore`
- **AND** fetches `GET /api/v1/review/cards?page=3&pageSize=20&status=Learning&search=concurrency`
- **AND** initializes the search input with "concurrency", highlights "Learning" filter, and renders page 3 of matching cards.

#### Scenario: Browser back button restores previous deck page and search state
- **GIVEN** the user navigated from deck page 1 to page 2 and applied a search filter
- **WHEN** the user clicks the browser "Back" button
- **THEN** the route query restores the previous state
- **AND** client fetches and displays the preceding page without reloading the entire application.

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

### Requirement: Spaced Repetition Studio Visual Layout & Modal Glass Surfaces
The `/review` route SHALL implement the **Dev-Learning Studio** visual language across all interactive sessions, deck management views, and modal dialogs:

1. **Obsidian Canvas & Tab Bar:**
   - The root `/review` page SHALL render with `dark:bg-canvas` (`#09090b` obsidian base) with hairline divider borders (`border-white/[0.06]`).
   - The dual-mode tab switcher (`Review Session` / `Deck Management`) SHALL use refined glass styling consistent with the global studio standard.

2. **Front/Back Card Content Isolation & 3D Flip Perspective:**
   - The interactive flashcard container SHALL enforce strict front/back content isolation, rendering card faces from the card's stored `frontMarkdown` / `backMarkdown` (persisted at card creation):
     - **Front Face (Prompt)**: SHALL render solely the question or challenge prompt (`card.frontMarkdown`), category pill (`CategoryBadge`), repetition counter (`Repetition #X`), difficulty level, and SM-2 telemetry indicators (Ease Factor, Interval). The front face SHALL NOT display the answer content (`card.backMarkdown`).
     - **Back Face (Answer)**: When flipped (`isFlipped === true`), the back face SHALL reveal the core technical explanation (`card.backMarkdown`) with Shiki syntax-highlighted code blocks.
   - The card container SHALL support a smooth 3D flip perspective (`perspective: 1000px`, `transform-style: preserve-3d`) or refined animated transition with glassmorphism styling (`.glass-card`, subtle backdrops, and hairline borders `dark:border-white/[0.08]`).

3. **Keyboard Navigation Ergonomics:**
   - The review session SHALL register tactile keyboard event listeners:
     - Pressing `Space` or `Enter` while the card is face-up SHALL flip the card to show the answer.
     - Pressing number keys `1`, `2`, `3`, or `4` while the card is flipped SHALL grade the card with SM-2 scores ($Score = 1$ for Again, $Score = 3$ for Hard, $Score = 4$ for Good, $Score = 5$ for Easy) and advance to the next card.
     - Keyboard event listeners SHALL be ignored when typing inside text inputs, textareas, or search boxes, and SHALL be cleanly unbound upon component unmount.

4. **Bilingual SM-2 Grading Controls:**
   - Ease rating buttons (`Again`, `Hard`, `Good`, `Easy`) in `Sm2GradingButtons.vue` SHALL feature semi-transparent glass borders and calibrated feedback colors without harsh opaque glare: Rose for Again, Amber for Hard, Sky/Indigo for Good, and Primary Brand Violet for Easy (`border-brand-300/80 dark:border-brand-500/40 bg-brand-50/80 dark:bg-brand-500/10 hover:bg-brand-100/90 dark:hover:bg-brand-500/20 text-brand-700 dark:text-brand-300`).
   - Each button SHALL display its respective keyboard shortcut badge (`[1]`, `[2]`, `[3]`, `[4]`).
   - Each button SHALL display localized action labels and localized interval descriptions in both English (`en-US`) and Vietnamese (`vi-VN`), replacing hardcoded English subtext.

5. **Deck Library & Modals Elevation:**
   - The 3-card Bento overview (`FlashcardHeroCard`, `MasteryGaugeCard`, `RetentionForecastCard`) and deck card library items SHALL use `.glass-card`.
   - Modals (Edit Card Modal with `edit`/`preview` tabs, Reset Progress Modal, Delete Card Modal) SHALL render with `.glass-panel` backdrop blur (`backdrop-blur-xl`), hairline borders (`border-white/[0.08]`), and dark inputs.

#### Scenario: User reviews flashcards in session mode
- **WHEN** user engages in an active flashcard review session
- **THEN** the flip card container renders with dark glass aesthetics and hairline borders
- **AND** ease grading buttons display clean, subtle color indicators without visual glare.

#### Scenario: User opens edit card modal in deck management
- **WHEN** user clicks edit on any card in the deck management table
- **THEN** the modal opens with a glass panel container
- **AND** the markdown edit/preview tabs toggle cleanly without opaque slate styling.

#### Scenario: User inspects face-up flashcard without answer leakage
- **GIVEN** an active flashcard is presented to the user
- **WHEN** the card is in the face-up state (`isFlipped === false`)
- **THEN** the card displays the question prompt (`card.frontMarkdown`), category badge, and repetition count
- **AND** the answer content (`card.backMarkdown`) is completely hidden from the viewport.

#### Scenario: User flips card using keyboard shortcut
- **GIVEN** an active flashcard is face-up
- **WHEN** the user presses the `Space` key or clicks the "Show Answer" button
- **THEN** the card animates to reveal the back face rendering `card.backMarkdown` with Shiki code blocks
- **AND** the SM-2 grading button group becomes visible with keyboard shortcut badges `[1]`, `[2]`, `[3]`, `[4]`.

#### Scenario: User grades card using keyboard shortcut
- **GIVEN** an active flashcard is flipped to the back face
- **WHEN** the user presses the `3` key
- **THEN** the system immediately submits a "Good" rating ($Score = 4$) for the active card
- **AND** the deck smoothly transitions to the next due card in the face-up state.

#### Scenario: Bilingual localized copy in SM-2 grading controls
- **WHEN** the user views the SM-2 grading controls in Vietnamese (`vi-VN`)
- **THEN** the buttons render Vietnamese labels and interval descriptions:
  - Button 1: "Chưa nhớ" with subtext "Đặt lại streak"
  - Button 2: "Khó" with subtext "Khoảng cách x1.2"
  - Button 3: "Tốt" with subtext "Khoảng cách x2.5"
  - Button 4: "Rất dễ" with subtext "Thưởng khoảng cách"
- **WHEN** toggled to English (`en-US`)
- **THEN** the buttons render English labels and interval descriptions ("Again" / "Reset streak", "Hard" / "Interval x1.2", "Good" / "Interval x2.5", "Easy" / "Bonus interval").
