# Spec Delta: core-platform

## ADDED Requirements

### Requirement: Engineering Cockpit Visual Density Standards
The web frontend SHALL standardize on an Engineering Cockpit visual density baseline (Density 8/10) across all application pages, ensuring compact, high-efficiency information layout on desktop viewports:
1. **Interactive Control Heights**: Buttons, inputs, search boxes, and dropdown selects SHALL standardize to `h-9` (36px height) with `px-3.5 text-sm`, eliminating oversized touch targets (`py-3.5`, `rounded-2xl`) on desktop screens.
2. **Concentric Border Radii**: Cards and containers SHALL maintain concentric radius geometry ($R_{\text{outer}} = R_{\text{inner}} + \text{padding}$). Outer cards SHALL use `rounded-xl` (12px), while nested controls and option buttons SHALL use `rounded-lg` (8px) or `rounded-md` (6px). Large `rounded-3xl` radii that force excessive internal padding are prohibited on data cards.
3. **Card & Container Spacing Scale**: Base card padding (`.glass-card`) SHALL standardize to `p-3.5` (14px) to `p-4.5` (18px). Page outer containers SHALL standardize to `py-4 sm:py-5 px-4 sm:px-6`, eliminating `p-8` to `p-10` dead margins.
4. **Desktop Fold Fit Constraint**: Standard desktop pages and cards SHALL fit key data and actions within an available vertical viewport height of $700\text{px}–850\text{px}$ (simulating 1080p desktop with 125% DPI scaling, taskbar, and browser chrome) without unintentional vertical clipping.

#### Scenario: User navigates pages on 1080p desktop with 125% DPI
- **WHEN** user views `/settings`, `/profile`, or `/insights` on a 1080p display with 125% DPI display scaling
- **THEN** page containers render with compact padding (`py-4 sm:py-5 px-4 sm:px-6`)
- **AND** cards render with compact padding (`p-3.5` to `p-4.5`)
- **AND** interactive buttons and inputs render at `h-9` (36px) height.

### Requirement: 3-Tier Production Modal Shell
All modal dialogs across the platform SHALL adhere to a 3-tier layout architecture capped at `max-h-[85vh]`:
1. **Fixed Header**: Pinned at the top (`shrink-0`) containing the modal title and close button.
2. **Scrollable Body**: Constrained to `max-h-[60vh]` with `overflow-y-auto` and `scrollbar-gutter: stable`, allowing long forms to scroll independently.
3. **Sticky Footer**: Pinned at the bottom (`shrink-0`) containing submission and cancellation action buttons. Action buttons SHALL remain permanently visible above the screen fold at all times regardless of content height.

#### Scenario: User opens modal with lengthy form content
- **WHEN** user opens a modal dialog containing multiple inputs, file upload zones, or explanatory guidelines
- **THEN** the modal header and modal footer remain fixed in place
- **AND** the submit and cancel buttons in the footer are immediately visible without requiring internal scrolling
- **AND** only the central form body scrolls when content exceeds `60vh`.

### Requirement: Interactive Design System Showcase
The platform SHALL maintain an interactive living design system showcase at `/showcase` displaying:
1. Base UI Primitives (Buttons, inputs with ⌘K badge, tags, segmented switcher)
2. Interactive Multiple-Choice Option Cards with active/correct/incorrect states
3. Production Modal Shell with sticky actions
4. Bento Metric Cards with tabular numerals
5. Code Snippet Block with clipboard copy feedback
6. Skeleton Loading Shimmers
7. Empty and Error State Cards
8. Reader Floating Selection Toolbar

#### Scenario: Developer or Agent inspects design system showcase
- **WHEN** user navigates to `/showcase`
- **THEN** the page renders all 8 component showcases in both light and dark modes
- **AND** interactive demo states (selection, modal open, copy feedback) function seamlessly.
