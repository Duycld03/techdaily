# Spec Delta

## ADDED Requirements

### Requirement: Category Filter Bar Layout Stability

The `/library` catalog page SHALL provide responsive category filter pills to filter technical documents without visual jitter, text expansion, or layout instability:

1. **Jitter-Free Category Pill Transitions**:
   - The category filter row SHALL render filter buttons using a constant font weight (`font-semibold`) on both active and inactive states.
   - Button styling transitions SHALL be strictly restricted to `transition-colors duration-150`, prohibiting `transition-all`.
   - The active category button SHALL apply active background (`bg-slate-100 dark:bg-canvas-elevated`), active border (`border-slate-300 dark:border-white/[0.12]`), and accent text color (`text-brand-600 dark:text-brand-400`) without modifying font weight, padding, or element box dimensions.
   - Inactive category buttons SHALL maintain `border-slate-200/80 dark:border-white/[0.08]` and `bg-white dark:bg-canvas-subtle` with hover color states.

2. **Zero Layout Shift Invariant**:
   - Toggling between category filter buttons (e.g. from "Tất Cả" to "Backend & Phân Tán") SHALL NOT cause button width changes or horizontal position shifts of sibling buttons.

#### Scenario: User switches category filter tab in library
- **WHEN** user clicks a category tab (e.g., "Tất Cả", "Frontend & Web", "Backend & Phân Tán", "Tư Duy Kỹ Sư & Năng Suất")
- **THEN** the active tab updates its background, border, and text colors via `transition-colors`
- **AND** the button maintains its exact text width and baseline geometry without any border jitter, horizontal position jumping, or layout shifts across adjacent tabs.

#### Scenario: User clears or toggles category filter
- **WHEN** user clicks "Tất Cả" (All) after viewing a specific category
- **THEN** the category filter state is cleared
- **AND** the tab switches cleanly with smooth color transitions and zero width shift.
