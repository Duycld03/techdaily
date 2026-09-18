# Tasks: UI Modernization — Dev-Learning Studio Design Tokens & App Shell

## 1. Design Tokens & Styling Foundation

- [x] 1.1 In `frontend/tailwind.config.js`, define semantic color layers: `canvas` (`DEFAULT: '#09080e'`, `subtle: '#12101b'`, `elevated: '#1a1726'`, `border: 'rgba(255, 255, 255, 0.08)'`), update `brand` to Electric Violet (`500: '#8b5cf6'`, `400: '#a78bfa'`, `600: '#7c3aed'`), add `streak` (`amber: '#f59e0b'`), and add `cyber` (`400: '#22d3ee'`, `500: '#06b6d4'`).
- [x] 1.2 In `frontend/assets/css/main.css`, define reusable utility classes: `.glass-card`, `.glass-panel`, `.hairline-border`, and update selection highlight color to `selection:bg-brand-500/30 selection:text-brand-300`.
- [x] 1.3 In `frontend/assets/css/main.css`, update custom scrollbar styling to blend with the OLED canvas palette.

## 2. Global Command Palette Component

- [x] 2.1 In `frontend/components/app/AppCommandPalette.vue`, create the Command Palette teleported modal component with fuzzy filtering across all main platform destinations (Today, Review, Quiz, Graph, Roadmap, Library, Notes, Settings).
- [x] 2.2 In `AppCommandPalette.vue`, implement global keyboard shortcut detection (`Cmd+K` / `Ctrl+K`) with `preventDefault()` to prevent default browser search, plus `Escape` to close and `ArrowDown`/`ArrowUp`/`Enter` keyboard navigation.
- [x] 2.3 In `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, add localization keys for Command Palette search placeholder, section headers, and shortcut badges.

## 3. Application Shell & Navigation Modernization

- [x] 3.1 In `frontend/app.vue` and `frontend/components/layout/AppHeader.vue`, mount `AppCommandPalette` and refactor the top navigation bar:
  - Add the centered `⌘K Quick Jump` search trigger button with keyboard shortcut badge on desktop, collapsing to an icon trigger on mobile.
  - Upgrade the Streak badge to a glowing pill with flame icon and subtle ember amber halo.
  - Modernize the language toggle and user profile avatar ring.
- [x] 3.2 In `frontend/components/layout/AppSidebar.vue`, modernize the sidebar navigation with Electric Violet active route accents (`bg-brand-500/10 text-brand-400 border-l-2 border-brand-500`) and refined icon spacing.

## 4. Automated Tests & Verification

- [x] 4.1 In `frontend/tests/components/app/AppCommandPalette.spec.ts`, write unit tests asserting:
  - Palette opens on `Cmd+K` / `Ctrl+K` keydown event.
  - Search input filters navigation destinations accurately.
  - Pressing `Enter` triggers router push to the selected destination.
  - Pressing `Escape` closes the palette.
- [x] 4.2 Run `npm --prefix frontend test` to verify all frontend unit tests pass with zero regressions.
- [x] 4.3 Run `dotnet test backend/TechDaily.sln` to confirm backend test suite stability.
- [x] 4.4 Run responsive E2E smoke test (`node frontend/e2e/test-graph-roadmap-responsive.mjs`) to verify layout stability across desktop, tablet, and mobile.
- [x] 4.5 Run `openspec validate --strict ui-modernization-design-tokens-and-app-shell` to confirm OpenSpec compliance.
