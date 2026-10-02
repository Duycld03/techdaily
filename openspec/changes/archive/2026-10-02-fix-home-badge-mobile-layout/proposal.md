# Proposal

## Why

On mobile viewports (<640px), the "Executive Cockpit" badge in the home dashboard orientation banner (`HomeBentoDashboard.vue`) wraps into two lines ("Executive" and "Cockpit") and becomes vertically distorted with asymmetric padding. This violates the core engineering layout invariant that all action buttons, badges, and card footers MUST declare `whitespace-nowrap shrink-0` with responsive container wrapping across English and Vietnamese locales.

## What Changes

- **Home Orientation Banner Badge Invariant**: Enforce `whitespace-nowrap shrink-0` on the "Executive Cockpit" badge in `HomeBentoDashboard.vue` to guarantee single-line integrity across all viewport widths.
- **Responsive Metadata Row Flex Wrapping**: Update the parent container of the orientation badge and slice metadata in `HomeBentoDashboard.vue` to `flex flex-wrap items-center gap-1.5 sm:gap-2` with `min-w-0`, preventing flex compression from squeezing adjacent role and slice text.
- **Design System Showcase Alignment**: Apply identical `whitespace-nowrap shrink-0` and responsive gap styling to the demo badge in `LayoutArchetypesShowcase.vue` to maintain consistency between production components and design system documentation.
- **Automated Visual & Behavioral Regression Verification**: Verify the fixed layout via Vitest data contract tests and dual-gate headless Chromium screenshots at both Mobile (390x844) and Desktop (1440x900) viewports.

## Capabilities

### Modified Capabilities
- `today`: Update the Home Dashboard responsive bento layout requirements to explicitly mandate single-line non-wrapping pill badge rendering and responsive metadata flex wrapping in the orientation header banner.

## Impact

- **Affected Code**: `frontend/components/dashboard/HomeBentoDashboard.vue`, `frontend/components/showcase/LayoutArchetypesShowcase.vue`.
- **Affected Tests**: `frontend/tests/components/dashboard/HomeBentoDashboard.spec.ts`.
- **API / Database**: Zero backend, API, or database schema changes.
- **Breaking Changes**: None.
