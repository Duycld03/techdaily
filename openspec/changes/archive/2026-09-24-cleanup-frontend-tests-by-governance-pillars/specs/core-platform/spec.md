# Spec Delta: core-platform

## MODIFIED Requirements

### Requirement: Frontend Testing Boundaries & Visual Inspection Standard
The web frontend automated test suite (`Vitest` + `Happy-DOM`) SHALL enforce a strict separation between behavioral data contracts and visual layout verification, actively purging test theater assertions:

1. **Prohibition of CSS Class Assertions in Vitest**:
   - Unit tests SHALL NOT assert the presence, absence, or modification of Tailwind CSS utility classes (e.g. `classes().toContain('max-w-5xl')`, `classes().toContain('hidden')`, `classes().toContain('lg:grid-cols-12')`, `classes().toContain('flex')`, `classes().toContain('shrink-0')`, `classes().toContain('whitespace-nowrap')`) under the premise of verifying visual presentation or layout integrity.
   - All tests in the frontend test suite asserting styling classes under the pretense of UI layout verification SHALL be pruned or replaced with behavioral state assertions.

2. **Permitted Scope for Automated Unit Testing (Vitest)**:
   - **Form Serialization & Data Contracts**: Verify that user input (names, emails, passwords, numerical values) is correctly parsed and dispatched in the exact required payload schema to backend endpoints or stores.
   - **Form Validation & Constraint Enforcement**: Verify that invalid inputs (missing required fields, passwords < 8 characters, mismatched passwords) produce validation errors and prevent submission.
   - **Authentication State & Route Middleware**: Verify that unauthenticated requests redirect to `/login?redirect=...`, and that token expiration triggers proper cleanup.
   - **Asynchronous Lifecycle & Error Feedback**: Verify loading indicators (`isSubmitting`), disabled button states during async requests, and RFC 7807 error problem details.

3. **Visual Verification Protocol**:
   - Visual correctness (alignment, padding, typography, element collisions, and text wrapping across English and Vietnamese) SHALL be verified exclusively through direct browser rendering and screenshot previews (e.g. at 1920x1080 desktop and 390x844 mobile viewports).

#### Scenario: Agent writes unit tests for an interactive form
- **WHEN** an agent writes tests for an authentication or settings form
- **THEN** tests assert input value binding, submission payload data, and validation error messages
- **AND** tests do NOT assert CSS classes like `max-w-*`, `flex`, `grid`, or `p-*`.

#### Scenario: Verifying responsive layout across viewports
- **WHEN** an agent develops or modifies a page component
- **THEN** layout stability and responsive presentation are verified through direct browser rendering or screenshot inspection at desktop and mobile breakpoints
- **AND** no Happy-DOM unit tests are written to verify pixel geometry.

#### Scenario: Auditing and pruning legacy CSS class test assertions
- **WHEN** the test suite executes
- **THEN** unit test assertions focus exclusively on data payloads, DOM element presence, accessible ARIA attributes, and user event reactions
- **AND** zero tests fail due to incidental styling or Tailwind utility class renames.
