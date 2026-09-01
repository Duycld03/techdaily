# Delta Spec: Multiple-Choice Senior Scenario Interview Drills

## Requirements

### REQ-1: Scenario Multiple-Choice Interview Question Domain Model
- The `InterviewQuestion` entity SHALL include:
  - `Options` (`List<string>`): A list of 4 distinct answer choices (A, B, C, D) representing technical solutions, architectural decisions, or debugging actions.
  - `CorrectOptionIndex` (`int`): Zero-based index (0–3) specifying the single best Senior/Staff engineering choice.
  - `ExplanationMarkdown` (`string`): A detailed markdown explanation analyzing why the correct option is optimal and highlighting the architectural pitfalls of each distractor option.
  - `Difficulty` (`Difficulty` enum: Easy, Medium, Hard).
- The `InterviewQuestionDto` returned by `GET /api/v1/daily/today` SHALL include `options` and `difficulty`. When the drill is not yet completed/reviewed, `correctOptionIndex` and `explanationMarkdown` SHALL be omitted or masked to prevent client-side inspection leakage.

### REQ-2: Multiple-Choice Submission & Instant Evaluation
- The `DailyDrill` entity SHALL record:
  - `SelectedOptionIndex` (`int?`): Zero-based index of the option chosen by the user.
  - `IsCorrect` (`bool?`): Boolean indicating whether `SelectedOptionIndex == Question.CorrectOptionIndex`.
  - `Score` (`int` / `decimal`): 10 for a correct response, 0 for an incorrect response.
- The submission endpoint `POST /api/v1/daily/drills/{id}/submit` SHALL accept `{ selectedOptionIndex: number }` in addition to optional free-text notes.
- The `SubmitDailyDrillHandler` SHALL validate that `selectedOptionIndex` is within the valid range `[0, question.Options.Count - 1]`.
- Upon submission, the handler SHALL:
  1. Record the chosen option and set status to `Reviewed`.
  2. Compute and persist the result (correctness and score).
  3. Increment and update the user's active streak record via `StreakRecord.RecordCompletion(today, score)`.
  4. Automatically schedule a `SpacedRepetitionCard` (SM-2) for tomorrow if the question was answered incorrectly.
  5. Return the evaluation result including `isCorrect`, `correctOptionIndex`, `score`, and `explanationMarkdown`.

### REQ-3: Frontend Interactive Scenario Drill UI (`InterviewChallengePane.vue`)
- The web frontend SHALL render the Senior Scenario challenge with:
  - Clear scenario context and problem description.
  - 4 option cards with option labels (`A`, `B`, `C`, `D`), clear typography, hover effects, and distinct selection state borders (`border-brand-500 bg-brand-50/10`).
  - An active "Submit Answer" button, enabled only when an option is selected.
- Upon successful submission:
  - Display a prominent Result Banner: Green `Correct!` with celebration confetti if correct, or Amber `Incorrect` if wrong.
  - Distinctly mark the correct option with a Green badge and checkmark icon (`CheckCircle2`).
  - If the user selected an incorrect option, highlight their choice with a Red badge and cross icon (`XCircle`).
  - Render the deep-dive `ExplanationMarkdown` with Shiki syntax highlighting, explaining the trade-offs in depth.
  - Update user streak in the top navigation bar.

### REQ-4: Internationalization (i18n) & Accessibility
- All new UI strings SHALL be registered in `frontend/locales/en.json` and `frontend/locales/vi.json`.
- Choice option buttons SHALL be fully accessible with keyboard navigation (`Tab`, `Enter`, `Space`) and screen-reader compliant aria attributes.

### REQ-5: Invariants & Development Standards
- All C# domain entities, handlers, DTOs, database migrations, Vue components, TypeScript interfaces, and unit tests SHALL be written in 100% English.
