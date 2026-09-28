# Spec Delta

## MODIFIED Requirements

### Requirement: Zero-Flicker Tab Navigation Invariant

All navigation rail tab buttons in `MasterDetailLayout`, segmented view switchers, category filter bars, and interactive tab switchers SHALL enforce a constant 1px border baseline (`border border-transparent` in inactive state, `border-brand-500/20` or active border tokens in active state), constant font weight, and scoped `transition-colors` rather than `transition-all`.

1. **Layout Shift Elimination**:
   - Tab switching SHALL NOT cause 1px box-sizing height/width jumps, font-weight advance metric expansions, or border flashing.
   - All interactive tab buttons, segment toggles, and category filter chips across the platform (`RoadmapViewSwitcher.vue`, `library.vue`, `insights.vue`, `notes.vue`, `review.vue`, `quiz.vue`, `profile.vue`) SHALL maintain a constant font weight (e.g. `font-semibold`) on both active and inactive states. Toggling between `font-medium` and `font-bold` is strictly prohibited.

2. **Visual Consistency & Scoped Transitions**:
   - Inactive buttons maintain consistent padding and alignment with active pill buttons.
   - Tab borders and indicator styles SHALL align with the unified `Dev-Learning Studio` design tokens.
   - Interactive button transitions SHALL be strictly restricted to `transition-colors duration-150` (or `transition-colors`), prohibiting unconstrained `transition-all`.

#### Scenario: Switching Tabs in Master-Detail Settings
- **WHEN** a user clicks between navigation rail tabs in `/settings`
- **THEN** the active tab updates smoothly with zero border flashing, zero layout jumping, and immediate visual feedback.

#### Scenario: User switches category filter pills
- **WHEN** user clicks between category filter pills (e.g. from "Tất Cả" to "Backend & Phân Tán" or "Tư Duy Kỹ Sư & Năng Suất")
- **THEN** both the previously selected and newly selected buttons retain identical font weight (`font-semibold`) and button width
- **AND** the border and background colors transition smoothly via `transition-colors` without any horizontal layout jitter or border flashing.

#### Scenario: User switches modal tab views
- **WHEN** user clicks between tab options in a dialog or drawer (such as "Xem chi tiết" / "Chỉnh sửa" in notes modal)
- **THEN** both tabs maintain constant font weight and 1px border baseline without text expansion or layout jumping.
