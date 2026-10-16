# Spec Delta

## MODIFIED Requirements

### Requirement: Frontend Automated Static Linting & Quality Gates
The frontend codebase SHALL provide automated linting scripts (`npm run lint` and `npm run lint:fix`) integrated with `@nuxt/eslint` and flat configuration (`eslint.config.mjs`) targeting Nuxt 4, Vue 3.5 Composition API, and TypeScript. The linting rules SHALL enforce Vue template safety rules (`vue/no-v-html`, `vue/no-template-target-blank`), check statically lintable component and prop declarations, and prevent unused variables while preserving Nuxt auto-imports without false-positive `no-undef` violations. Full component, prop, and application type validation SHALL be performed by the separate frontend typechecking command rather than inferred from lint or build success.

#### Scenario: Running frontend lint command
- **WHEN** developer or CI executes `npm run lint` in the `frontend/` directory
- **THEN** ESLint executes across authored Vue and TypeScript files, reports violations, and retains its native exit status without changing source files.

#### Scenario: Running frontend lint auto-fix command
- **WHEN** developer executes `npm run lint:fix` in the `frontend/` directory
- **THEN** ESLint automatically corrects fixable style and formatting violations across authored Vue and TypeScript files.

#### Scenario: Lint warnings remain observable
- **WHEN** lint completes without errors but reports warnings
- **THEN** warnings remain visible even when ESLint exits zero
- **AND** the result is not described as warning-free or checked against a warning allowance, count ceiling, or debt ratchet.

#### Scenario: Lint encounters an error or invalid configuration
- **WHEN** ESLint reports an error, parsing failure, or invalid configuration
- **THEN** the lint command retains the native nonzero exit status instead of converting the failure into accepted debt.

#### Scenario: Custom quality commands are retired
- **WHEN** a developer inspects the available frontend validation commands after the cutover
- **THEN** native lint, lint auto-fix, typecheck, test, and build commands remain available
- **AND** `quality:check` and `quality:baseline` are absent without compatibility aliases or replacement accepted-debt commands.

### Requirement: Frontend Testing Boundaries & Visual Inspection Standard
The web frontend automated test suite (`Vitest` + `Happy-DOM`) and verification workflow SHALL enforce a strict separation between behavioral data contracts and visual layout verification, actively purging test theater assertions and mandating direct headless browser screenshot inspection:

1. **Prohibition of CSS Class Assertions in Vitest**:
   - Unit tests SHALL NOT assert the presence, absence, or modification of Tailwind CSS utility classes (e.g. `classes().toContain('max-w-5xl')`, `classes().toContain('hidden')`, `classes().toContain('lg:grid-cols-12')`, `classes().toContain('flex')`, `classes().toContain('shrink-0')`, `classes().toContain('whitespace-nowrap')`) under the premise of verifying visual presentation or layout integrity.
   - All tests in the frontend test suite asserting styling classes under the pretense of UI layout verification SHALL be pruned or replaced with behavioral state assertions.

2. **Permitted Scope for Automated Unit Testing (Vitest)**:
   - **Form Serialization & Data Contracts**: Verify that user input (names, emails, passwords, numerical values) is correctly parsed and dispatched in the exact required payload schema to backend endpoints or stores.
   - **Form Validation & Constraint Enforcement**: Verify that invalid inputs (missing required fields, passwords < 8 characters, mismatched passwords) produce validation errors and prevent submission.
   - **Authentication State & Route Middleware**: Verify that unauthenticated requests redirect to `/login?redirect=...`, and that token expiration triggers proper cleanup.
   - **Asynchronous Lifecycle & Error Feedback**: Verify loading indicators (`isSubmitting`), disabled button states, and RFC 7807 error problem details.

3. **Mandatory Automated Browser Visual Verification Protocol**:
   - Visual correctness (alignment, padding, typography, element collisions, and text wrapping across English and Vietnamese) SHALL be verified exclusively through direct headless browser rendering and screenshot previews.
   - For all frontend UI modifications (pages, layouts, components), the agent or developer SHALL execute automated browser inspection before marking tasks complete:
     - Open headless Chromium via the `browser` device.
     - Use authentic QA sessions and the canonical storage identifiers (`deeppace_token`, `deeppace_user`) with the existing persistence and refresh-cookie policy. Legacy identifiers SHALL remain confined to migration verification as required by `client-storage`.
     - Capture visual screenshots at both **Desktop (1440x900 or 1920x1080)** and **Mobile (390x844)** viewports.
     - Present the captured screenshots directly in the verification output.

4. **Dual-Gate Verification Invariant**:
   - Marking a frontend UI task complete (`- [x]`) in planning tasks or declaring "Implementation Complete" SHALL strictly require passing both verification gates:
     - **Gate 1 (Behavioral Data Contract):** Vitest test suite (`npm test`) passes with zero regression.
     - **Gate 2 (Visual Integrity Gate):** Headless browser screenshots captured and verified across both desktop and mobile viewports.
   - Declaring a UI task complete based solely on Gate 1 without Gate 2 is strictly prohibited.

Tooling-only changes that do not alter rendered UI SHALL NOT require unrelated UI screenshots. This exception SHALL NOT apply when a tooling change also changes page, component, layout, style, or rendered translation behavior.

#### Scenario: Dual-Gate verification requirement for frontend UI changes
- **WHEN** an agent completes coding changes to a Vue page, component, or layout
- **THEN** the agent executes `npm test` in `frontend/` to satisfy Gate 1 (Behavioral Data Contract)
- **AND** the agent executes automated browser screenshot capture for Desktop and Mobile viewports to satisfy Gate 2 (Visual Integrity)
- **AND** the task is only marked complete (`- [x]`) after both gates have succeeded.

#### Scenario: Agent captures automated browser screenshots across Desktop and Mobile viewports
- **WHEN** an agent performs visual verification on an updated route (e.g. `/notes`)
- **THEN** the agent launches headless Chromium, navigates using an authentic session when required, and captures screenshots at 1440x900 (Desktop) and 390x844 (Mobile)
- **AND** inspects the resulting images for text wrapping, layout shift, and element collision across both locales before yielding to the user.

#### Scenario: Rejection of premature UI task sign-off without visual inspection
- **WHEN** an agent passes all Vitest unit tests but has not captured browser screenshots for modified UI surfaces
- **THEN** the agent SHALL NOT mark the visual verification task complete
- **AND** SHALL NOT yield "Implementation Complete" until the automated visual inspection is executed and presented.

#### Scenario: Tooling-only validation does not claim visual proof
- **WHEN** a change modifies only lint, typecheck, CI, or verification documentation without changing rendered UI
- **THEN** validation exercises the affected tooling and its failure paths without requiring unrelated page screenshots
- **AND** its report does not claim UI layout, accessibility, or visual-regression proof.

## ADDED Requirements

### Requirement: Frontend Authored-Code Lint Coverage
Frontend linting SHALL cover authored application, configuration, tests, E2E JavaScript, and service-worker code with appropriate execution globals. Generated output, generated declarations, and non-code assets SHALL remain excluded. Authored declarations SHALL participate in application typechecking.

#### Scenario: Authored browser scripts and service worker are linted
- **WHEN** lint encounters an authored E2E JavaScript file or `public/sw.js` containing a lint violation
- **THEN** that file receives applicable diagnostics rather than an ignored-file result
- **AND** legitimate execution globals do not produce false-positive undefined-global errors.

#### Scenario: Generated output is not treated as authored source
- **WHEN** prepared output, production bundles, generated declarations, or screenshots exist
- **THEN** lint excludes them without blanket exclusions for authored E2E JavaScript or the service worker.

#### Scenario: Authored declarations affect a type contract
- **WHEN** an authored declaration changes an application type contract
- **THEN** typechecking validates its effect independently of declaration-file lint exclusions.

### Requirement: Frontend Static Type Checking
The frontend SHALL expose a standalone `npm run typecheck` for authored Vue SFCs, TypeScript, declarations, and project configuration using the prepared application/server/shared/tooling contexts. It SHALL preserve strictness, aliases, diagnostics, and the compiler's native exit status without an accepted-debt mode. Lint, unit tests, and bundling SHALL NOT substitute for typechecking.

#### Scenario: A component receives an incompatible prop
- **WHEN** an application component receives a prop incompatible with its declared type
- **THEN** typechecking reports the mismatch and returns nonzero even if bundling succeeds.

#### Scenario: Existing semantic type diagnostics are visible
- **WHEN** the project contains existing semantic type errors
- **THEN** the command displays them and retains its nonzero exit status
- **AND** no ledger, suppression added for this cutover, or wrapper reclassifies that execution as passing.

#### Scenario: Invalid project configuration is reported
- **WHEN** the compiler rejects project configuration
- **THEN** its diagnostics and nonzero exit status remain visible rather than becoming accepted type debt.

### Requirement: Frontend CI Native Lint Enforcement
Frontend CI SHALL invoke native lint once before its existing tests and production build, retaining installation and publication dependencies. Any failing stage SHALL block dependent publication/deployment. Standalone typechecking SHALL remain outside the required CI stages in this change; CI SHALL NOT claim that unperformed typechecking passed.

#### Scenario: Native lint fails
- **WHEN** the CI lint command returns nonzero
- **THEN** subsequent frontend tests/build do not execute as successful validation stages
- **AND** dependent image publication and deployment remain blocked.

#### Scenario: Native lint reports warnings without errors
- **WHEN** lint returns zero while reporting warnings
- **THEN** CI retains the warning output and continues to its existing tests and build
- **AND** no baseline comparison or non-growing-debt guarantee is implied.

#### Scenario: A completed frontend CI pass does not duplicate work
- **WHEN** installation and native lint succeed
- **THEN** the existing full test suite and production build each execute once
- **AND** the job requires no quality comparison SHA, baseline bootstrap, or softened typecheck.

#### Scenario: Manual dispatch needs no diagnostic comparison input
- **WHEN** a maintainer manually dispatches CI
- **THEN** frontend validation does not require a `quality-base` input or fetch comparison data for diagnostics.

### Requirement: Scope-Proportional Frontend Verification
Frontend verification SHALL select checks according to affected behavior, preserve the UI dual gate, and distinguish executed evidence from recommendations. README SHALL provide concise command and CI policy guidance; behavioral requirements SHALL remain in OpenSpec. Permanent tests SHALL protect application behavior rather than assert validation-script text or styling.

#### Scenario: Source changes are validated during iteration
- **WHEN** a developer iterates on isolated frontend behavior
- **THEN** lint and existing behavioral tests can target the affected files while type validation uses the prepared project
- **AND** unrelated suites, coverage collection, and full builds are not repeated after every edit.

#### Scenario: UI work reaches completion
- **WHEN** changed UI behavior is ready for sign-off
- **THEN** the full frontend test suite and authentic bilingual desktop/mobile screenshots satisfy the existing dual gate.

#### Scenario: Only documentation changes
- **WHEN** a change modifies documentation without executable configuration or frontend behavior
- **THEN** verification validates the changed documentation without requiring unrelated builds or browser journeys.

#### Scenario: A browser check has no comparison baseline
- **WHEN** screenshots are captured and inspected without approved image-comparison baselines
- **THEN** the report describes current-state visual inspection rather than claiming automated visual-regression comparison.
