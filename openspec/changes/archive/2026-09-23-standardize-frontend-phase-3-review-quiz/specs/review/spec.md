# Spec Delta

## ADDED Requirements

### Requirement: Spaced Repetition Grading Geometry and Quiz Option State Isolation
The SM-2 flashcard grading interface (`Sm2GradingButtons.vue`), forecast chart (`ReviewForecastChart.vue`), and Quiz Arena (`pages/quiz.vue`) SHALL enforce mobile responsive grid wrapping, touch target sizing ($\ge 44\text{px}$), and strict option selection isolation across question sessions.

#### Scenario: SM-2 Grading Buttons on Small Mobile Screens
- **WHEN** user grades a flashcard in `Sm2GradingButtons.vue` on a viewport $\le 360\text{px}$
- **THEN** the 4 grading options ("Again", "Hard", "Good", "Easy") SHALL wrap into a 2-column grid (`grid-cols-2 sm:grid-cols-4 gap-2.5`)
- **AND** each button SHALL provide at least $44\text{px}$ height for accurate touch tapping without misclicks.

#### Scenario: Forecast Bar Chart Responsive Scaling
- **WHEN** user views the 7-day retention forecast in `ReviewForecastChart.vue` on mobile screens
- **THEN** chart bars SHALL scale proportionally without text overflow, displaying numeric values formatted with `tabular-nums`.

#### Scenario: Quiz Option Badge Isolation and Keyboard Triggers
- **WHEN** a user answers a question in `pages/quiz.vue`
- **THEN** option badges ("A", "B", "C", "D") SHALL preserve `whitespace-nowrap shrink-0`
- **AND** shortcut keys (`1`, `2`, `3`, `4`) SHALL be registered via VueUse `useEventListener` and properly cleaned up when the quiz completes.
