# Design

## Context
See `proposal.md` for motivation. Currently, `AGENTS.md` Pillar 3 prohibits CSS class assertions in Vitest and names a "Visual Verification Gate", but does not provide an actionable, programmatic execution protocol for agents to follow. Consequently, agents stop after running `npm test`, conflating passing unit test assertions with visual layout correctness.

Because Happy-DOM does not render CSS or compute geometry, severe layout shifts, text collisions, and awkward dead spaces go undetected until manually identified by users.

## Goals / Non-Goals

**Goals:**
- **Actionable Execution Protocol**: Codify the exact programmatic commands and steps for executing headless browser visual inspection using `browser` in `eval`.
- **Dual-Gate Verification Invariant**: Establish a strict rule in both `AGENTS.md` (Pillars 3 & 5) and `specs/core-platform/spec.md` requiring both Gate 1 (Vitest unit tests) and Gate 2 (Headless browser screenshots) before any UI task can be marked complete.
- **Session Authentication Standard**: Document the exact authentication injection method (`techdaily_token` and `techdaily_user` cookies/localStorage) so agents can inspect protected views without manual login hoops.

**Non-Goals:**
- Modifying backend APIs, database schemas, or frontend application features. This change is strictly focused on testing standards, agent governance, and verification protocols.

## Decisions

### 1. Codify Automated Browser Screenshot Protocol in Pillar 3
Update `AGENTS.md` Section 3 (Pillar 3) to outline the exact execution standard:
```typescript
// 1. Launch browser tab
const tab = await browser.open({
  name: "ui-verify",
  url: "http://localhost:3000/login",
  viewport: { width: 1440, height: 900 }
});

// 2. Inject authentic user session for protected routes
await tab.evaluate(`(() => {
  document.cookie = 'techdaily_token=<jwt>; path=/; max-age=86400';
  document.cookie = 'techdaily_user=' + encodeURIComponent(JSON.stringify(authUser)) + '; path=/; max-age=86400';
  localStorage.setItem('techdaily_token', '<jwt>');
  localStorage.setItem('techdaily_user', JSON.stringify(authUser));
})()`);

// 3. Navigate & Capture Desktop and Mobile screenshots
await tab.goto("http://localhost:3000/<route>", { wait_until: "networkidle" });
await tab.screenshot(); // Desktop (1440x900)
// Emulate mobile (390x844) & capture screenshot
await tab.screenshot();
await tab.close();
```

### 2. Dual-Gate Verification Invariant in Pillar 5
Update `AGENTS.md` Section 3 (Pillar 5) to state:
- **Gate 1 (Behavioral Data Contract):** `npm test` passes 100% of Vitest test suites.
- **Gate 2 (Visual Integrity Gate):** Headless Chromium captures screenshots for Desktop and Mobile viewports, showing zero layout shifts, proper text wrapping, and clean spacing across English and Vietnamese.
- **Sign-Off Prohibition:** Agents MUST NEVER mark a UI task complete (`- [x]`) in `tasks.md` or yield "Implementation Complete" to the user based solely on Gate 1. If visual inspection was not executed and presented, the task is incomplete.

### 3. Synchronize `specs/core-platform/spec.md`
Update `Requirement: Frontend Testing Boundaries & Visual Inspection Standard` in `specs/core-platform/spec.md` to enshrine these exact rules into durable capability specifications.

## Risks / Trade-offs

| Risk / Trade-off | Mitigation |
| :--- | :--- |
| **Execution Time for Browser Launch**: Taking screenshots takes ~1-2 seconds per breakpoint. | Running headless Chromium via `browser` in `eval` is fast (<2s total) and completely eliminates the risk of deploying broken layouts. |
| **Authentication Dependency**: Some routes require a logged-in user. | Documenting the token injection pattern (`techdaily_token`) allows instant authentication in headless browser without brittle UI form automation. |
