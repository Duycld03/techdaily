# Spec Delta

## ADDED Requirements

### Requirement: Executive Bento Profile and Settings Mobile Responsive Standards
The Engineer Portfolio Profile (`pages/profile.vue`), System Settings (`pages/settings.vue`), and Authentication (`pages/login.vue`) surfaces SHALL render with responsive Bento geometry down to $320\text{px}$, responsive 2x2 daily study pace chips, and touch-accessible notification scheduling controls.

#### Scenario: Executive Bento Profile on 320px Viewports
- **WHEN** user views `pages/profile.vue` on a narrow mobile viewport ($320\text{px}$ to $375\text{px}$)
- **THEN** Tier 1 Passport badges (Role, Google Linked, Streak Trophy) SHALL wrap cleanly without clipping
- **AND** Tier 2 Milestones telemetry cells SHALL render in a balanced 2-column grid (`grid-cols-2 lg:grid-cols-4`) without label truncation.

#### Scenario: Responsive Daily Goal Pace Selector
- **WHEN** user selects daily study pace ("5m", "10m", "15m", "30m") in Account Settings
- **THEN** the selection chips SHALL wrap into `grid-cols-2 sm:grid-cols-4 gap-2` on mobile screens to ensure touch targets remain $\ge 44\text{px}$ without horizontal text squishing.

#### Scenario: Mobile Settings Web Push and Timezone Controls
- **WHEN** user configures notification schedules or timezone preferences in `pages/settings.vue` on a mobile device
- **THEN** time picker popovers and timezone dropdowns SHALL clamp within viewport boundaries
- **AND** the Brave push setup guidance card SHALL adapt responsively without table or code block clipping.
