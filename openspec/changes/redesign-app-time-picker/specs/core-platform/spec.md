# Spec Delta: core-platform

## ADDED Requirements

### Requirement: Dev-Learning Studio Conformance for Time Selection Primitives
All time selection inputs across the platform SHALL use the custom accessible `AppTimePicker.vue` component, conforming to the **Dev-Learning Studio** visual language, replacing unstyled or disconnected scrollboxes with an integrated obsidian glassmorphic interface.

1. **Trigger Control Standards**:
   - The trigger button SHALL match Density 8/10 desktop dimensions (`px-3.5 py-2 rounded-xl text-xs sm:text-sm font-semibold`).
   - The selected time display SHALL use monospace tabular figures (`font-mono tabular-nums tracking-wide`) to prevent layout jitter during time changes.
   - The trailing chevron icon SHALL smoothly rotate 180 degrees when the popover opens.

2. **Popover Geometry & Aesthetics**:
   - The popover surface SHALL render as a `.glass-panel` on an elevated obsidian canvas (`dark:bg-canvas-elevated`, `border border-slate-200/90 dark:border-white/[0.08]`, `shadow-2xl rounded-2xl p-3 backdrop-blur-md`).
   - Column headers for Hours, Minutes, and Period SHALL use uppercase micro-typography (`text-[10px] font-bold tracking-wider text-slate-400 dark:text-slate-500 text-center`).
   - The Hours list (01 to 12) and Minutes list (00 to 55 in 5-minute steps) SHALL provide clean, custom scrollable tracks with subtle borders and zero browser scrollbar clutter.
   - The Period selector (AM / PM) SHALL render as a balanced vertical segmented control matching the height and curvature of the adjacent number columns.

3. **Active State & Color Hierarchy**:
   - Selected options (active hour, active minute, active period) SHALL display the Dev-Learning Studio brand active pill styling (`bg-brand-500/15 text-brand-600 dark:text-brand-400 font-bold border border-brand-500/30 shadow-xs`).
   - Unselected options SHALL provide smooth hover affordance (`hover:bg-slate-100 dark:hover:bg-white/[0.06] text-slate-700 dark:text-slate-300`).

4. **Action Footer & Accessibility**:
   - The popover footer SHALL include a compact confirmation button (`h-8 text-xs font-semibold rounded-xl bg-brand-600 hover:bg-brand-500 text-white shadow-sm transition-all active:scale-[0.98]`).
   - The popover SHALL support outside-click closing via `onClickOutside`, `Escape` key dismissal, and WAI-ARIA dialog accessibility (`role="dialog"`, `aria-label`).
   - All labels and action texts SHALL be fully localized via `@nuxtjs/i18n` in English and Vietnamese without truncation or visual wrapping.

#### Scenario: User selects study time in Settings
- **WHEN** the user clicks the time picker trigger on `/settings`
- **THEN** the glassmorphic popover opens beneath the trigger with smooth scaling animation
- **AND** the current hour, minute, and period are highlighted with subtle brand active pills
- **WHEN** the user selects "09", "30", and "AM", then clicks "Done"
- **THEN** the model updates to "09:30", the popover closes, and the trigger displays "09:30 AM" in monospace tabular numerals.

#### Scenario: Bilingual responsive layout for time picker popover
- **WHEN** the user switches between English and Vietnamese locales
- **THEN** column headers display "HOUR" / "GIỜ", "MIN" / "PHÚT", and "PERIOD" / "BUỔI"
- **AND** the footer button displays "Done" / "Xong" with balanced padding and zero text clipping.
