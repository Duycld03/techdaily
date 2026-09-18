# Spec Delta: core-platform

## ADDED Requirements

### Requirement: Global Error Experience & Authentication Studio Layout
The global error boundary page (`frontend/error.vue`) and authentication views (`frontend/pages/login.vue`) SHALL adhere to the **Dev-Learning Studio** visual theme:

1. **Global Error Page Refinement (`error.vue`):**
   - The error page SHALL render over `dark:bg-canvas` (`#09090b` obsidian base) instead of legacy slate.
   - The central error card SHALL utilize `.glass-panel` elevation with translucent hairline borders (`border-white/[0.08]`).
   - Legacy Emerald styling (`bg-emerald-600`, `text-emerald-400`) SHALL be replaced with Deep Iris Violet (`brand-600` / `brand-500`) for primary action buttons (`Back to Daily Practice`) and status code badges.

2. **Authentication Studio Modernization (`login.vue`):**
   - The login/register form card SHALL render using `.glass-panel` over `dark:bg-canvas`.
   - The authentication mode switcher (`Sign In` / `Register`) SHALL feature clean glass tab styling.
   - Text input fields SHALL render with subtle dark glass backgrounds (`bg-white/[0.04] dark:bg-canvas-subtle`) and hairline borders (`border-white/[0.08]`).

#### Scenario: User encounters system error or 404
- **WHEN** an unhandled error or missing page occurs
- **THEN** the system displays the error boundary page over a neutral obsidian canvas
- **AND** the primary action button displays Deep Iris Violet without legacy emerald colors.

#### Scenario: User visits login page
- **WHEN** a visitor navigates to `/login`
- **THEN** the authentication card displays glass panel styling with refined brand accents.
