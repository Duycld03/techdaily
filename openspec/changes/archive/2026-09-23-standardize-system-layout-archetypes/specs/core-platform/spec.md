# Spec Delta: core-platform

## ADDED Requirements

### Requirement: Accessible Custom Select Dropdown Invariant
All dropdown selection controls across the frontend application and component showcases SHALL use custom accessible dropdown components (`AppSelect.vue`) rather than unstyled native HTML `<select><option>` elements.
1. **Styling & Theme Integrity**:
   - The dropdown trigger button and floating options listbox SHALL adhere to the Dev-Learning Studio theme (`dark:bg-canvas-elevated`, `dark:border-white/[0.08]`, `dark:text-slate-200`).
   - The dropdown listbox SHALL NOT display native operating system selection highlights (such as default blue Windows highlight `#0078d7` or unstyled browser option boxes).
2. **Keyboard Accessibility**:
   - The custom select component SHALL support standard WAI-ARIA combobox/listbox navigation: `Enter` or `Space` to toggle, `Up` / `Down` arrow keys to highlight options, `Escape` to dismiss, and `Enter` to commit selection.

#### Scenario: Interacting with Select Dropdowns on Windows
- **WHEN** an engineer opens a select dropdown on Windows 11
- **THEN** the options menu displays as a themed dark obsidian floating panel with brand-tinted active/hover states, with zero native OS unstyled option rendering.

---

### Requirement: Living Design System Layout Archetypes Showcase
The interactive design system showcase at `/showcase` (`frontend/pages/showcase.vue`) SHALL include Section 09: **"System Layout Archetypes"** (`LayoutArchetypesShowcase.vue`).
1. **Interactive Layout Demos**:
   - The showcase section SHALL provide interactive tabs to demonstrate each of the three layout archetypes:
     - **Tab 1: Flashcards Studio Demo**: Demonstrates `StudioLayout` with sample flashcard, telemetry dock, and hotkey cheatsheet.
     - **Tab 2: Settings Master-Detail Demo**: Demonstrates `MasterDetailLayout` with left sub-nav and right configuration panels using custom `AppSelect` controls.
     - **Tab 3: Notes Board Demo**: Demonstrates `BoardLayout` with sticky search/filter toolbar and responsive card grid.
2. **Full Primitive Parity**:
   - The showcase Primitives section (`PrimitivesShowcase.vue`) SHALL use `AppSelect.vue` for all dropdown controls, confirming elimination of raw `<select>` elements.

#### Scenario: Viewing Layout Archetypes in Showcase
- **WHEN** a developer navigates to `/showcase` and selects the "09. System Layout Archetypes" section
- **THEN** interactive previews of `StudioLayout`, `MasterDetailLayout`, and `BoardLayout` are rendered with realistic mock data and responsive layout controls.
