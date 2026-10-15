# Proposal

## Why

On the `/admin/login` page, the administrator sign-in form (`#main`) and the workspace overview context dock (`#dock`) exhibit severe visual misalignment on desktop viewports. While the left form is vertically centered across a large ~520px content block, the right dock contains a brief ~180px text block configured with `justify-center`, pushing it down to the lower half of the screen (starting ~260px lower than the form). Furthermore, the 3 contextual overview items in the dock lack structured card framing, appearing as raw unstyled text.

This change aligns the baseline between the main stage and context dock, packages the telemetry/context items into cohesive hairline-bordered feature cards with clear visual hierarchy, and refines spacing to ensure a neat, balanced desktop and mobile presentation strictly adhering to the 68/32 `StudioLayout` archetype and DeepPace design system.

## What Changes

- **Synchronized Stage & Dock Baseline**:
  - Replace `justify-center` in the `#dock` slot with top-aligned baseline structure (`justify-start pt-4 sm:pt-6 lg:pt-8`), horizontally synchronizing the "Administrator sign in" heading and the "Your system overview" dock heading.
  - Align container top padding between `#main` and `#dock` so elements sit on an identical horizontal grid plane.

- **Structured Context Feature Cards in `#dock`**:
  - Wrap the 3 admin workspace overview items (System Overview & Accounts, Curated Catalog Management, Server-verified Access) into individual elevated hairline cards (`rounded-xl border border-slate-200 dark:border-white/[0.08] bg-slate-50/50 dark:bg-canvas-subtle p-3.5`).
  - Add icon badges with distinct background containers (`bg-brand-500/10 text-brand-600 dark:text-brand-400 rounded-lg p-2`) and concise titles and descriptions to create visual depth and structure.

- **Form Polish & Visual Balance in `#main`**:
  - Polish vertical rhythm inside `#main` around the alert banner and form inputs to eliminate awkward empty gaps.
  - Maintain the form width boundaries (`max-w-md`) and responsive typography standards ($\ge 14\text{px}$ mobile, $\ge 16\text{px}$ desktop).

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `admin-workspace`: Refine the `DeepPace design system parity` requirement and scenario to explicitly mandate synchronized vertical baseline alignment and structured telemetry/context cards for the admin login `StudioLayout` stage and dock on desktop viewports.

## Impact

- **Affected Files**:
  - `frontend/app/pages/admin/login.vue`
  - `frontend/tests/pages/admin-login.spec.ts` (if assertions touch rendered structure)
- **Zero Impact on Auth/State**: Form submission, API validation, Pinia stores (`useAuthStore`, `useAdminAccessStore`), and route guards remain completely untouched.
