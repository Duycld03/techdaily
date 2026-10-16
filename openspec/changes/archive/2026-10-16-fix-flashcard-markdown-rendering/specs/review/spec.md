# Spec Delta

## MODIFIED Requirements

### Requirement: Spaced Repetition Studio Visual Layout & Modal Glass Surfaces
The `/review` route SHALL implement the **Dev-Learning Studio** visual language across all interactive sessions, deck management views, and modal dialogs:

1. **Obsidian Canvas & Tab Bar:**
   - The root `/review` page SHALL render with `dark:bg-canvas` (`#09090b` obsidian base) with hairline divider borders (`border-white/[0.06]`).
   - The dual-mode tab switcher (`Review Session` / `Deck Management`) SHALL use refined glass styling consistent with the global studio standard.

2. **Front/Back Card Content Isolation & Markdown Rendering:**
   - The interactive flashcard container (`FlashcardDeck.vue`) SHALL enforce strict front/back content isolation and rich Markdown rendering:
     - **Front Face (Prompt)**: SHALL render the question prompt or challenge (`card.frontMarkdown`), category pill, repetition counter (`Repetition #X`), difficulty level, and SM-2 telemetry indicators. The front face SHALL NOT display the answer or explanation. Both question text and list options (such as multiple-choice items `- **A.** ...`) SHALL be parsed and rendered as rich HTML via `useMarkdownRenderer` inside a styled prose container (`prose dark:prose-invert max-w-none`) rather than raw unparsed text.
     - **Back Face (Answer)**: When flipped (`isFlipped === true`), the back face SHALL reveal the core technical explanation and solution (`card.backMarkdown`), parsed and rendered as rich HTML via `useMarkdownRenderer` with code syntax highlighting and prose formatting, preserving paragraph breaks, bold headings (`**Correct Option: ...**`), and code blocks.
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
- **THEN** the card displays the topic question, category badge, and repetition count
- **AND** the answer summary and deep-dive technical explanations are completely hidden from the viewport.

#### Scenario: Multiple-choice flashcard renders structured options list on front face
- **GIVEN** an active flashcard created from a quiz mistake with front markdown containing a question and bulleted options `- **A.** ...`, `- **B.** ...`
- **WHEN** the card is rendered in `FlashcardDeck.vue`
- **THEN** the markdown renderer parses the content into structured HTML (`<ul>` and `<li>` elements) with bold option indicators
- **AND** the options render across separate list items rather than collapsing into a single unformatted plain text line.

#### Scenario: Back face renders markdown solution with bold callout and paragraph breaks
- **GIVEN** an active flashcard with back markdown containing `**Correct Option: B. ...**` and an explanation paragraph
- **WHEN** the user reveals the back face
- **THEN** the explanation is parsed with `useMarkdownRenderer`
- **AND** the correct option displays in bold (`<strong>`) followed by a distinct paragraph break separating the analytical explanation.

#### Scenario: User flips card using keyboard shortcut
- **GIVEN** an active flashcard is face-up
- **WHEN** the user presses the `Space` key or clicks the "Show Answer" button
- **THEN** the card animates to reveal the back face with the complete technical explanation and Shiki code blocks
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
