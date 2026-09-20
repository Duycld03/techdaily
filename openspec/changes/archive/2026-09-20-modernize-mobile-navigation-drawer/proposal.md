# Proposal: Modernize Mobile Navigation Drawer

## Why

The mobile navigation drawer in `frontend/components/layout/AppHeader.vue` has fallen out of sync with the modernized desktop sidebar (`AppSidebar.vue`) and TechDaily's refreshed design token system. 

Specifically:
1. **Outdated Visual Tokens**: The mobile drawer uses legacy hardcoded styles (`bg-slate-900`, `border-slate-800`, `emerald-400` gradient) instead of the unified canvas tokens (`dark:bg-canvas-subtle/95`, `dark:border-white/[0.08]`, `from-brand-600 via-brand-500 to-indigo-400`).
2. **Missing Navigation Items & Route Fault**: The mobile drawer is missing the Dashboard (`/`) route entirely and contains a legacy `isLinkActive` check that falsely highlights "Today / Luyện Tập Hôm Nay" when the user visits `/`.
3. **Inconsistent Active State**: Nav links on mobile use an outdated boxy background highlight without the desktop sidebar's refined `border-l-2 border-brand-500` active indicator.
4. **Hardcoded Strings**: The drawer header contains hardcoded `"TechDaily Menu"` and the footer user card displays unlocalized `"Log Out"` instead of `$t('nav.logout')`.

Modernizing the mobile navigation drawer brings 100% visual and functional parity between desktop and mobile navigation across all screen sizes.

## What Changes

- **Navigation Model Alignment**: Add the missing Dashboard link (`/` with `LayoutGrid` icon) to the mobile navigation groups and synchronize route matching logic (`isLinkActive`) with `AppSidebar.vue`.
- **Design Token & Surface Modernization**: Upgrade the drawer backdrop and container to `bg-white/95 dark:bg-canvas-subtle/95 backdrop-blur-md` with refined borders (`border-slate-200/80 dark:border-white/[0.08]`) and smooth slide-in animations.
- **Brand Header Alignment**: Replace the outdated emerald gradient with the canonical TechDaily brand mark (`from-brand-600 via-brand-500 to-indigo-400`), replace hardcoded "TechDaily Menu" with a polished localized brand presentation, and modernize the close button.
- **Modern Active Link Styling**: Apply the desktop sidebar's signature `border-l-2 border-brand-500` left-accent indicator, `bg-brand-500/10 dark:bg-white/[0.06]`, and matching icon colors for active items.
- **Localized Profile Footer Card**: Modernize the pinned bottom user card with unified canvas tokens, proper avatar typography, and localized logout trigger (`$t('nav.logout')`).
- **E2E Test Assertion Stability**: Update existing navigation tests (`test-navigation-locales.mjs`) to assert against the updated drawer title and verify navigation to both Dashboard and Today on small viewports.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Update navigation drawer specifications to mandate visual parity, unified design tokens, full route coverage (including Dashboard), and complete localization for mobile viewports.

## Impact

- **Frontend Code**: `frontend/components/layout/AppHeader.vue`, `frontend/components/layout/AppSidebar.vue` (potential composable or shared nav definition if extracted).
- **Localization**: Verifies `nav.dashboard`, `nav.logout` keys exist in both `locales/en.json` and `locales/vi.json`.
- **Automated Verification**: End-to-end navigation suite `frontend/e2e/test-navigation-locales.mjs` and component unit tests.
