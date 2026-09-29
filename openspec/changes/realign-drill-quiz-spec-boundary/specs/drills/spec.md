# Spec Delta

## ADDED Requirements

### Requirement: Daily Drill Status Resiliency & State Deserialization
The daily interview challenge components (`InterviewChallengePane.vue` and `today.vue`) SHALL handle both string (`"Reviewed"`, `"reviewed"`, `"Submitted"`) and integer (`2`, `1`) representations of `DrillStatus`. Upon page refresh, when a previously completed drill is retrieved from `GET /api/v1/daily/today`, the interface SHALL preserve the reviewed state, restore the user's selected option, and display the score, architectural explanation, and header completed badge without reverting to an unsubmitted state.

#### Scenario: User refreshes page after completing daily interview challenge
- **WHEN** an authenticated user completes the daily scenario challenge and later refreshes `/today`
- **THEN** the API returns the persisted drill entity with `status = "Reviewed"`
- **AND** the scenario challenge pane recognizes the drill as reviewed (`isReviewed = true`)
- **AND** the header completed badge remains visible
- **AND** the submit button remains hidden, rendering the architectural explanation card and feedback banner.

#### Scenario: Preserving selected option index and feedback across sessions
- **WHEN** user reloads the today view for an already reviewed drill with `selectedOptionIndex = 2`
- **THEN** option 2 is visually selected and styled with appropriate correctness highlighting (emerald for correct, rose for incorrect)
- **AND** the user cannot re-submit or alter the completed drill.

### Requirement: Natural Vertical Layout for Scenario Challenge Interface
The scenario multiple-choice challenge container in `InterviewChallengePane.vue` SHALL utilize natural vertical content stacking (`flex flex-col justify-start gap-5`) rather than artificial edge stretching (`justify-between`). Multiple-choice options, contextual alert banners, submit controls, and post-submission explanations SHALL follow in continuous vertical sequence without arbitrary 300–400px empty gaps on widescreen viewports.

#### Scenario: Natural vertical spacing without layout gaps
- **WHEN** viewing the scenario challenge on a wide desktop screen (>1280px) with extended vertical height
- **THEN** the question header, options list, submit button, and explanation card maintain consistent vertical spacing (`space-y-5 sm:space-y-6`) without pushing controls to the extreme bottom of the viewport.

#### Scenario: Responsive layout during post-submission feedback
- **WHEN** the drill transitions to reviewed state
- **THEN** the result banner and deep-dive explanation card render directly beneath the options list with natural spacing.
