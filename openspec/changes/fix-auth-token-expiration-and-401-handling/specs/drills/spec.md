# Delta Spec: Resilient Scenario Drill Submission Error Handling

## MODIFIED Requirements

### Requirement: Frontend Interactive Scenario Drill UI
The web frontend SHALL render the Senior Scenario challenge with 4 interactive option cards (A, B, C, D), hover effects, selection highlights, confetti celebrations on correct answers, and rich markdown explanation breakdown. When a submission fails due to network error or authorization failure, the UI SHALL preserve the reading pane and question choices intact, presenting feedback via notifications rather than unmounting the entire view.

#### Scenario: Drill submission encounters error
- **WHEN** user clicks "Submit Answer" and the backend returns an error (such as 401 or network failure)
- **THEN** the application DOES NOT replace the dual-pane content with a full-screen `HTTP Error` container.
- **THEN** an error notification appears alerting the user to the failure.
- **THEN** the question, selected option, and document reader remain visible and intact.
