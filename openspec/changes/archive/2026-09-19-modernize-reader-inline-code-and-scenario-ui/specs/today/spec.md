# Spec Delta: Today Focus Studio

## ADDED Requirements

### Requirement: Scenario Drill Multiple-Choice Interface & Evaluation Feedback
The Scenario Challenge multiple-choice interface (`InterviewChallengePane.vue`) and auxiliary panels (`DocReaderPane.vue`) on `/today` SHALL style option cards, score badges, and evaluation feedback according to Dev-Learning Studio design tokens:
1. **Unselected & Inactive Options**: SHALL use studio subtle background (`dark:bg-canvas-subtle`), elevated surface (`dark:bg-canvas-elevated`), and hairline borders (`dark:border-white/[0.04]` to `dark:border-white/[0.08]`), eliminating all legacy dark slate classes (`dark:bg-slate-800`, `dark:border-slate-800/60`, `dark:bg-slate-950/20`).
2. **Semantic Success & Optimal Choice**: When reviewed, the optimal choice option card SHALL render with studio-grade translucent emerald styling (`border-emerald-500 dark:border-emerald-500/40 bg-emerald-50/80 dark:bg-emerald-500/10 text-emerald-950 dark:text-emerald-100 ring-2 ring-emerald-500/20`), preserving clear semantic success distinction without neon over-saturation.
3. **Score & Status Telemetry Badges**: Score telemetry badge (`+10 Pts`) and status pill badges SHALL render with translucent emerald pill styling (`dark:bg-emerald-950/40 dark:border-emerald-500/30 dark:text-emerald-300`).
4. **Authoritative Source Excerpt**: In `DocReaderPane.vue`, the source context box SHALL render as a `.glass-panel` container with `dark:bg-canvas-subtle/80`, `dark:border-white/[0.08]`, and brand telemetry header (`text-brand-600 dark:text-brand-400`), eliminating legacy green background tint (`bg-emerald-500/5 dark:bg-emerald-950/20`).

#### Scenario: Reviewed optimal choice rendering in dark mode
- **WHEN** user submits an answer to the Senior Scenario Drill on `/today` in dark mode
- **THEN** the optimal choice card displays with translucent emerald border (`dark:border-emerald-500/40`) and subtle tint (`dark:bg-emerald-500/10`)
- **AND** the letter badge displays with solid emerald fill (`bg-emerald-600 text-white`)
- **AND** the optimal choice pill badge renders with high-contrast text and crisp checkmark icon
- **AND** other unselected options display with neutral studio slate/white tokens (`dark:bg-slate-950/20` replaced with `dark:bg-canvas-subtle/30` and `dark:border-white/[0.04]`).

#### Scenario: Authoritative source excerpt rendering in dark mode
- **WHEN** user views a reading slice on `/today` that contains an authoritative source excerpt
- **THEN** the excerpt box renders with `.glass-panel` container styling with `dark:bg-canvas-subtle/80` and `dark:border-white/[0.08]`
- **AND** the header label renders with Deep Iris Violet telemetry color (`text-brand-600 dark:text-brand-400`) rather than emerald green.
