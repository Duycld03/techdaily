## MODIFIED Requirements

### Requirement: Spaced Repetition Dual-Mode Navigation
The `/review` page SHALL provide a top-level dual-mode tab switcher allowing software engineers to toggle seamlessly between the active review session ("Review Session" / "Ôn tập hôm nay") and the complete flashcard deck library ("Deck Management" / "Kho thẻ của tôi").

When the active review session is complete or when no cards are currently due for review (`reviewStore.cards.length === 0`), the `/review` page SHALL render a rich, balanced **Daily Review Completion Hub** in place of an isolated empty card:
1. **Celebratory Hero Banner**: Features a celebration icon, triumphant completion heading (`review.no_cards`), motivational description (`review.no_cards_desc`), and triggers a celebratory confetti burst upon completing the final due card of the session.
2. **Embedded Spaced Mastery Gauge**: Embeds the `MasteryGaugeCard` component directly on the completion view, displaying the total flashcard deck mastery rate ($(Mastered / Total) \times 100\%$) and active proficiency tier badge (e.g. "Trí nhớ xuất sắc" / "Đang xây dựng phản xạ") without requiring the user to switch tabs.
3. **Embedded 7-Day Review Forecast Chart**: Embeds the `ReviewForecastChart` component directly on the completion view, displaying projected card review volumes across the upcoming 7 days so users can anticipate tomorrow's review workload at a glance.
4. **Dual Action Controls**:
   - Primary CTA: "Browse Full Deck (N cards)" (`review.browse_deck_btn`), smoothly transitioning the active tab to "Deck Management" (`activeTab = 'management'`) with the total card count prominently displayed.
   - Secondary CTA: "Cram / Extended Practice" (`review.continue_drill`), routing to `/today` or practice challenges for extended study.

#### Scenario: User navigates between review session and deck management
- **GIVEN** an authenticated user who visits `/review`
- **WHEN** user loads `/review`
- **THEN** page defaults to the "Review Session" tab if due cards exist, or allows switching to "Deck Management"
- **AND** when user clicks the "Deck Management" tab, client transitions to the deck management view and loads card statistics and the paginated card library without page reload.

#### Scenario: User completes review session
- **GIVEN** an active review session with due cards remaining
- **WHEN** user finishes grading all due cards in the "Review Session" tab
- **THEN** player displays a completion state with confetti celebration and provides a 1-click shortcut to inspect the full card deck in the "Deck Management" tab.

#### Scenario: User completes daily review session and views rich completion hub
- **GIVEN** an authenticated user who has graded the final due card in `/review` or has zero cards due today (`reviewStore.cards.length === 0`)
- **WHEN** the user views the active review session tab
- **THEN** the client renders the Daily Review Completion Hub featuring the celebratory banner, embedded Mastery Gauge Card, and embedded 7-Day Review Forecast Chart
- **AND** triggers a celebratory confetti burst.

#### Scenario: User inspects 7-day review forecast on completion hub without switching tabs
- **GIVEN** the user has completed all cards due today and is viewing the Daily Review Completion Hub
- **WHEN** the user inspects the embedded Review Forecast Chart
- **THEN** the chart displays upcoming review card volumes for the next 7 days (Monday through Sunday)
- **AND** clearly reveals how many cards will become due tomorrow without requiring navigation to the Deck Management tab.

#### Scenario: User switches to deck management via completion hub CTA
- **GIVEN** the user is viewing the Daily Review Completion Hub with a total deck size of 42 cards
- **WHEN** the user clicks the "Browse Full Deck (42 cards)" action button
- **THEN** the client immediately transitions to the Deck Management tab (`activeTab = 'management'`)
- **AND** displays the full paginated card library and advanced search filters without reloading the page.

#### Scenario: User launches extended practice from completion hub
- **GIVEN** the user is viewing the Daily Review Completion Hub
- **WHEN** the user clicks the "Cram / Extended Practice" action button
- **THEN** the application routes to `/today` to practice daily scenario drills and reading challenges.

#### Scenario: Responsive bento layout of completion hub on mobile and desktop viewports
- **GIVEN** the user is viewing the Daily Review Completion Hub
- **WHEN** viewed on a desktop viewport ($\ge 1280\text{px}$)
- **THEN** the completion hub displays the celebratory banner followed by a balanced 2-column or 3-column layout embedding the Mastery Gauge and 7-day Review Forecast side-by-side
- **WHEN** viewed on a mobile viewport ($375\text{px}\text{--}390\text{px}$)
- **THEN** the completion hub smoothly stacks the banner, Mastery Gauge, Review Forecast, and action buttons into a single vertical column with zero horizontal scrolling.
