# Spec Delta: quiz (Phase 2 Interactive Practice)

## ADDED Requirements

### Requirement: OptionCard Zero-Layout-Shift Tactile Geometry
Option choices in `quiz.vue` and `frontend/components/ui/OptionCard.vue` SHALL maintain strict tactile geometry invariants across all interactive states:
1. **Dimensions & Padding**: Padding (`p-4 sm:p-5`), border widths (`border`), and line-heights MUST remain identical across `default`, `hover`, `selected`, `correct`, and `incorrect` states to prevent layout shifts.
2. **Tactile Option Badges**: Option index badges (`A`, `B`, `C`, `D`) SHALL use `w-7 h-7 sm:w-8 sm:h-8 shrink-0 rounded-lg text-xs font-mono font-bold flex items-center justify-center` with high-contrast active states.
3. **Typography Standard**: Option text MUST render at `text-sm sm:text-base` with `leading-relaxed` for maximum readability across English and Vietnamese questions.
4. **Hairline Border Styling**: Unselected cards SHALL use `border-slate-200/80 dark:border-white/[0.08]` over `bg-white dark:bg-canvas-subtle/50`. Selected cards SHALL highlight with brand accent borders (`border-brand-500`) without changing border thickness.

#### Scenario: User selects an option card in quiz arena
- **WHEN** an engineer selects Option B during a timed quiz question
- **THEN** Option B highlights with active brand colors and the option badge fills with brand contrast, without shifting the vertical position of Option C or Option D.

### Requirement: Arena Mode Telemetry Dock & Hotkey Integration
The Arena Mode dock in `quiz.vue` SHALL display live session metrics and support instant keyboard shortcuts:
1. **Live Countdown & Streak**: Display remaining time with urgent color shifts ($< 10\text{s}$ turns rose-500) and current streak multiplier badge.
2. **Question Navigation Map**: Display compact numeric pill indicators for all questions in the current batch, showing unvisited, active, answered, and mistake statuses.
3. **Keyboard Quick Select**: Pressing number keys `1`, `2`, `3`, `4` or letter keys `A`, `B`, `C`, `D` MUST trigger option selection instantly.

#### Scenario: Engineer answers using keyboard hotkey
- **WHEN** an engineer presses key `2` while viewing a quiz question
- **THEN** Option B is selected immediately without requiring mouse interaction.
