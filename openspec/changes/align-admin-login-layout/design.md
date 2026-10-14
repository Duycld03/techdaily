# Design

## Context

The `/admin/login` page utilizes the `StudioLayout` component (`frontend/components/layout/StudioLayout.vue`), which establishes a 68/32 split between the `#main` practice stage and the `#dock` telemetry sidebar. The form inside `#main` contains header branding, titles, an access-denial alert banner, and credentials inputs, spanning over 500px in vertical height. Conversely, the `#dock` slot currently houses a brief informational section wrapped in `<div class="flex flex-1 flex-col justify-center gap-6 py-3 sm:py-6">`.

Because the dock height is bounded by the stage height in `StudioLayout`, applying `justify-center` to the compact dock content centers it vertically across ~720px, causing the text to start at roughly $Y = 430\text{px}$ while the sign-in form begins at $Y = 260\text{px}$. In addition, the dock items are unstyled text paragraphs without container elevation or visual hierarchy.

See `proposal.md` for motivation.

## Goals / Non-Goals

**Goals:**
- Synchronize the top vertical baseline between the `#main` form stage and the `#dock` context rail on desktop displays ($\ge 1024\text{px}$) with matching top padding (`pt-6 sm:pt-8 lg:pt-10`).
- Encapsulate the 3 context overview items into elevated hairline-bordered cards (`border-slate-200 dark:border-white/[0.08] bg-white/40 dark:bg-canvas-subtle p-3.5 rounded-xl`) with distinct icon badges.
- Maintain full parity with the 68/32 `StudioLayout` archetype without introducing breaking changes or extra props to `StudioLayout.vue`.
- Ensure flawless bilingual responsive rendering down to 390px mobile viewports.

**Non-Goals:**
- Modifying `frontend/components/layout/StudioLayout.vue` itself (preserving stability across `review.vue`, `quiz.vue`, and archetype showcases).
- Altering authentication logic, Pinia stores (`useAuthStore`, `useAdminAccessStore`), form submission handlers, or API contracts.
- Redesigning other admin surfaces (`/admin`, `/admin/catalog`).

## Decisions

### 1. Vertical Rhythm & Baseline Synchronization
- **Decision**: In `frontend/app/pages/admin/login.vue`:
  - On `#main`: Ensure the inner wrapper `<div class="mx-auto w-full max-w-md ...">` adopts unified top-alignment rhythm (`flex flex-col justify-start pt-4 sm:pt-6 lg:pt-8 pb-6 sm:pb-8`) rather than arbitrary internal centering that drifts when the access denial alert renders.
  - On `#dock`: Replace `justify-center` with `justify-start pt-4 sm:pt-6 lg:pt-8 pb-6 sm:pb-8`.
- **Rationale**: Both columns will start from the exact same vertical offset, ensuring the "Administrator sign in" heading and the "Your system overview" heading align horizontally across the desktop viewport.

### 2. Dock Telemetry & Overview Cards Architecture
- **Decision**: Structure the `#dock` content into:
  - Header block: Badge pill (`text-brand-600 dark:text-brand-400 font-semibold text-xs tracking-wider uppercase`) + Section title (`text-xl font-bold text-slate-900 dark:text-white`).
  - 3 Overview Feature Cards:
    - Card 1: Overview & Inventory (Icon: `LayoutDashboard`, title: System Overview, body: View accounts, document inventory and measured service status).
    - Card 2: Curated Catalog (Icon: `LibraryBig`, title: Curated Catalog, body: Prepare, review and publish curated documents).
    - Card 3: Server Verification (Icon: `ShieldCheck`, title: Server Verified, body: Administrator access is verified by the server).
  - Card styling: `rounded-xl border border-slate-200 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle p-3.5 shadow-sm space-y-1.5`.
  - Icon container: `flex h-8 w-8 items-center justify-center rounded-lg bg-brand-500/10 text-brand-600 dark:text-brand-400`.
- **Rationale**: Matches the visual quality of telemetry docks across `quiz.vue` and `LayoutArchetypesShowcase.vue`, transforming plain unstyled text into cockpit-grade UI.

### 3. Responsive Stacking on Mobile Viewports
- **Decision**: On mobile screens ($< 1024\text{px}$), the dock automatically stacks below `#main` via `StudioLayout`. The top padding on mobile will be compact (`pt-4 sm:pt-6`) and the cards will display full width with `space-y-3`.

## Risks / Trade-offs

- **Risk**: Adding structured cards to `#dock` might increase height if strings wrap in Vietnamese.
  - *Mitigation*: Ensure responsive font sizes (`text-sm sm:text-base`) and `whitespace-normal leading-relaxed` with concise titles so cards maintain a compact vertical rhythm.
- **Risk**: Test breakage in `tests/pages/admin-login.spec.ts`.
  - *Mitigation*: `admin-login.spec.ts` stubs `StudioLayout` with simple slots and asserts form field IDs (`#admin-email`, `#admin-password`) and alert roles. Inspect and run Vitest to verify all tests pass without regression.
