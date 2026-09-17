## MODIFIED Requirements

### Requirement: Spaced Repetition Dual-Mode Navigation
The `/review` page SHALL provide a top-level dual-mode tab switcher allowing software engineers to toggle seamlessly between the active review session ("Review Session" / "Ôn tập hôm nay") and the complete flashcard deck library ("Deck Management" / "Kho thẻ của tôi").

When the active review session is complete or when no cards are currently due for review (`activeTab === 'session' && (!currentCard || reviewStore.cards.length === 0)`), the `/review` page SHALL render a cleanly centered, celebratory completion hero card with generous breathing room and balanced visual proportions:
1. **Celebratory Hero Banner**: Features a triumphant completion icon (`CheckCircle`), celebratory heading (`review.no_cards`), encouraging retention copy (`review.no_cards_desc`), and triggers a celebratory confetti burst upon completing the final due card of the session.
2. **Generous Proportions & Padding**: The completion hero card container SHALL enforce an expanded maximum width bound of `max-w-xl` (36rem / 576px) and generous multi-tier responsive padding of at least `p-10 sm:p-12 md:p-14` (or `p-8 sm:p-12 md:p-14`), ensuring that internal content, heading, descriptive copy, and action buttons maintain ample breathing room without crowding or pressing against card borders.
3. **Relaxed Vertical Hierarchy & CTA Spacing**: The card elements SHALL maintain relaxed vertical spacing (`space-y-6 sm:space-y-7`) between the completion icon badge, headline, descriptive copy, and action buttons, with an expanded top padding offset on the action CTA row (`pt-4 sm:pt-6` and `gap-3.5 sm:gap-4`).
4. **Dedicated Action Controls**:
   - Primary CTA: "Browse Full Deck (N cards)" (`review.browse_deck_btn`), smoothly transitioning the active tab to "Deck Management" (`activeTab = 'management'`) with the total card count prominently displayed.
   - Secondary CTA: "Cram / Extended Practice" (`review.cram_practice_btn`), routing to `/today` for extended scenario challenge practice.
5. **Card Deduplication**: The completion state SHALL NOT embed duplicate `MasteryGaugeCard` or `ReviewForecastChart` components, preserving these comprehensive retention analytics exclusively in Tab 2 ("Kho thẻ của tôi" / Deck Management) to maintain a focused, decluttered completion celebration.

#### Scenario: User navigates between review session and deck management
- **WHEN** user loads `/review`
- **THEN** page defaults to the "Review Session" tab if due cards exist, or allows switching to "Deck Management"
- **WHEN** user clicks the "Deck Management" tab
- **THEN** client transitions to the deck management view and loads card statistics and the paginated card library without page reload.

#### Scenario: User completes review session
- **WHEN** user finishes grading all due cards in the "Review Session" tab
- **THEN** player displays a completion state with confetti celebration and provides a 1-click shortcut to inspect the full card deck in the "Deck Management" tab
- **AND** the completion card renders with expanded max-width bounds (`max-w-xl`), generous internal padding (`p-10 sm:p-12 md:p-14`), and relaxed vertical spacing (`space-y-6 sm:space-y-7`, `pt-4 sm:pt-6`), preventing action buttons and content from crowding container borders.

#### Scenario: User completes daily review session and views focused completion hero card
- **GIVEN** an authenticated user who finishes grading the last due card or has zero cards due today
- **WHEN** user views the Review Session tab (`activeTab === 'session'`)
- **THEN** client renders only the centered Celebratory Completion Hero Card with confetti
- **AND** the card container applies generous internal padding (`p-10 sm:p-12 md:p-14`), expanded max-width (`max-w-xl`), and relaxed vertical spacing (`space-y-6 sm:space-y-7`)
- **AND** displays the primary CTA `[ 📚 Khám phá kho thẻ (N thẻ) ]` and secondary CTA `[ ⚡ Luyện tập mở rộng ]` with ample horizontal breathing room and touch-friendly padding
- **AND** does NOT render the duplicate Mastery Gauge Card or Review Forecast Chart beneath the hero card.

#### Scenario: User transitions from completion hero card to deck management tab
- **GIVEN** user is viewing the Celebratory Completion Hero Card
- **WHEN** user clicks `[ 📚 Khám phá kho thẻ (N thẻ) ]`
- **THEN** client immediately switches `activeTab` to `'management'`
- **AND** renders the 3-card Bento Overview (including Mastery Gauge Card and Review Forecast Chart) alongside the complete paginated flashcard library.
