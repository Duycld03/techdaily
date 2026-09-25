# Spec Delta: core-platform

## MODIFIED Requirements

### Requirement: Frontend Testing Boundaries & Visual Inspection Standard
The web frontend automated test suite (`Vitest` + `Happy-DOM`) and verification workflow SHALL enforce a strict separation between behavioral data contracts and visual layout verification, actively purging test theater assertions and mandating direct headless browser screenshot inspection:

1. **Prohibition of CSS Class Assertions in Vitest**:
   - Unit tests SHALL NOT assert the presence, absence, or modification of Tailwind CSS utility classes (e.g. `classes().toContain('max-w-5xl')`, `classes().toContain('hidden')`, `classes().toContain('lg:grid-cols-12')`, `classes().toContain('flex')`, `classes().toContain('shrink-0')`, `classes().toContain('whitespace-nowrap')`) under the premise of verifying visual presentation or layout integrity.
   - All tests in the frontend test suite asserting styling classes under the pretense of UI layout verification SHALL be pruned or replaced with behavioral state assertions.

2. **Permitted Scope for Automated Unit Testing (Vitest)**:
   - **Form Serialization & Data Contracts**: Verify that user input (names, emails, passwords, numerical values) is correctly parsed and dispatched in the exact required payload schema to backend endpoints or stores.
   - **Form Validation & Constraint Enforcement**: Verify that invalid inputs (missing required fields, passwords < 8 characters, mismatched passwords) produce validation errors and prevent submission.
   - **Authentication State & Route Middleware**: Verify that unauthenticated requests redirect to `/login?redirect=...`, and that token expiration triggers proper cleanup.
   - **Asynchronous Lifecycle & Error Feedback**: Verify loading indicators (`isSubmitting`), disabled button states during async requests, and RFC 7807 error problem details.

3. **Mandatory Automated Browser Visual Verification Protocol**:
   - Visual correctness (alignment, padding, typography, element collisions, and text wrapping across English and Vietnamese) SHALL be verified exclusively through direct headless browser rendering and screenshot previews.
   - For all frontend UI modifications (pages, layouts, components), the agent or developer SHALL execute automated browser inspection before marking tasks complete:
     - Open headless Chromium via the `browser` device.
     - Inject authentic user session cookies/tokens (`techdaily_token`, `techdaily_user`) when navigating authenticated views.
     - Capture visual screenshots at both **Desktop (1440x900 or 1920x1080)** and **Mobile (390x844)** viewports.
     - Present the captured screenshots directly in the verification output.

4. **Dual-Gate Verification Invariant**:
   - Marking a frontend UI task complete (`- [x]`) in planning tasks or declaring "Implementation Complete" SHALL strictly require passing both verification gates:
     - **Gate 1 (Behavioral Data Contract):** Vitest test suite (`npm test`) passes with zero regression.
     - **Gate 2 (Visual Integrity Gate):** Headless browser screenshots captured and verified across both desktop and mobile viewports.
   - Declaring a UI task complete based solely on Gate 1 without Gate 2 is strictly prohibited.

#### Scenario: Dual-Gate verification requirement for frontend UI changes
- **WHEN** an agent completes coding changes to a Vue page, component, or layout
- **THEN** the agent executes `npm test` in `frontend/` to satisfy Gate 1 (Behavioral Data Contract)
- **AND** the agent executes automated browser screenshot capture for Desktop and Mobile viewports to satisfy Gate 2 (Visual Integrity)
- **AND** the task is only marked complete (`- [x]`) after both gates have succeeded.

#### Scenario: Agent captures automated browser screenshots across Desktop and Mobile viewports
- **WHEN** an agent performs visual verification on an updated route (e.g. `/notes`)
- **THEN** the agent launches headless Chromium, navigates to the route (injecting auth cookies if required), and takes screenshots at 1440x900 (Desktop) and 390x844 (Mobile)
- **AND** inspects the resulting images for text wrapping, layout shift, and element collision across locales before yielding to the user.

#### Scenario: Rejection of premature UI task sign-off without visual inspection
- **WHEN** an agent passes all Vitest unit tests but has not captured browser screenshots for modified UI surfaces
- **THEN** the agent SHALL NOT mark the visual verification task complete
- **AND** SHALL NOT yield "Implementation Complete" until the automated visual inspection is executed and presented.
