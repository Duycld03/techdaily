# Spec Delta: quiz

## ADDED Requirements

### Requirement: Quiz Arena Question & Option Layout Density
The Quiz Arena interface on `/quiz` SHALL style question cards, option buttons, and answer submission controls according to the standardized Cockpit Density standards:
1. **Compact Option Cards**: The multiple-choice choices in the Arena tab SHALL utilize `OptionCard.vue` (`px-3 py-2.5 rounded-lg border text-sm`), eliminating legacy `p-4 sm:p-5 rounded-2xl` option buttons.
2. **Question Card Padding**: The primary question card padding SHALL standardize to `p-4 sm:p-5` with `space-y-4` (eliminating `p-5 sm:p-7 space-y-6`).
3. **Desktop Fold Visibility Invariant**: On standard 1080p desktop viewports, all four multiple-choice options (A, B, C, D) and the "Submit Choice" action button SHALL render completely above the screen fold without requiring vertical scrolling.

#### Scenario: User takes quiz on standard desktop screen
- **WHEN** user answers a quiz question in the Arena tab on `/quiz` on a 1080p desktop display
- **THEN** Question text, options A, B, C, D, and the "Submit Choice" button are all visible within the viewport
- **AND** Option D and the Submit button are not clipped or pushed below the screen fold.
