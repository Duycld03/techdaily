# Design: Dev-Learning Studio AppTimePicker Component

## Context

In `frontend/pages/settings.vue`, study schedule preferences (`preferredStudyTime` and `streakAlertTime`) currently utilize native `<input type="time">`. While global `color-scheme: dark;` prevents white-screen flashes, the browser's User-Agent Shadow DOM renders a rigid, non-customizable popup with square edges, generic system fonts, and light blue OS selection highlights.

`frontend/components/common/AppSelect.vue` established the pattern for custom studio inputs:
1. Trigger button with studio hairline border, hover states, and WCAG keyboard focus ring.
2. Floating dropdown popover teleported to `document.body` (`z-[60]`) with fixed viewport coordinates.
3. Viewport auto-flip logic preventing container clipping and modal scrollbar expansion.
4. Glassmorphism elevation tokens (`glass-panel dark:bg-canvas-elevated border-white/[0.08] shadow-2xl backdrop-blur-md`).

`AppTimePicker.vue` adopts this exact pattern for temporal selection.

## Goals / Non-Goals

**Goals:**
- Build `frontend/components/common/AppTimePicker.vue` with 0 external dependencies.
- Support seamless two-way `v-model` binding with standard `HH:mm` 24-hour time strings (e.g. `'08:00'`, `'20:30'`).
- Display localized 12-hour formatted time (`08:00 AM`, `08:30 PM`) in trigger and selector controls.
- Provide a clean 3-column studio interface:
  - Hours column (`01` to `12`) with smooth pill buttons.
  - Minutes column (`00` to `55` in 5-minute increments) with smooth pill buttons.
  - Period segmented toggle (`AM` / `PM`).
- Include quick schedule presets for instant 1-click study planning (`07:00 AM`, `08:00 AM`, `08:00 PM`, `09:00 PM`).
- Guarantee zero viewport overflow using fixed teleportation and auto-flip detection.
- Migrate `frontend/pages/settings.vue` to use `<AppTimePicker>`.

**Non-Goals:**
- Date or calendar picker capabilities (this component focuses strictly on time of day).
- Modifying backend API contracts or profile stores (both already communicate via `HH:mm`).

## Decisions

### 1. Two-Way Contract & Internal Representation

- **Decision**: External contract accepts and emits `HH:mm` (24h format). Internally, time is parsed and converted to 12-hour representation:
  ```ts
  function parseTimeTo12h(timeStr: string) {
    const [hStr, mStr] = (timeStr || '08:00').split(':')
    const h = parseInt(hStr, 10) || 0
    const m = parseInt(mStr, 10) || 0
    const period = h >= 12 ? 'PM' : 'AM'
    const hour12 = h % 12 === 0 ? 12 : h % 12
    return { hour12, minute: m, period }
  }

  function format12hTo24h(hour12: number, minute: number, period: 'AM' | 'PM') {
    let h = hour12 % 12
    if (period === 'PM') h += 12
    return `${String(h).padStart(2, '0')}:${String(minute).padStart(2, '0')}`
  }
  ```
- **Rationale**: 24h `HH:mm` is the universal ISO/HTML standard stored in backend databases and Postgres time types. 12-hour display (`08:00 AM`) is the most human-readable format for study scheduling.

### 2. Floating Popover Architecture

- **Decision**: Render popover via `<Teleport to="body">` with fixed positioning calculated from trigger element coordinates:
  ```ts
  const spaceBelow = window.innerHeight - rect.bottom
  const placeAbove = spaceBelow < 300 && rect.top > spaceBelow
  ```
- **Rationale**: Completely prevents the popover from being clipped by `settings.vue`'s container cards or introducing unwanted container scrollbars.

### 3. Studio Styling & Interaction

- **Trigger**:
  - `p-2.5 rounded-xl border border-slate-200/90 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle text-slate-900 dark:text-white`
  - Features `Clock` icon (1.5px stroke weight) and chevron indicator.
  - Active state: `border-brand-500/50 ring-2 ring-brand-500/20`.
- **Popover**:
  - `w-72 sm:w-80 glass-panel dark:bg-canvas-elevated border border-slate-200/90 dark:border-white/[0.08] shadow-2xl rounded-2xl p-3.5 backdrop-blur-md`
  - Selected pills: `bg-brand-600 text-white font-bold shadow-sm`
  - Unselected pills: `text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06]`
- **Quick Presets**:
  - Bottom bar with 4 chips: `07:00 AM`, `08:00 AM`, `08:00 PM`, `09:00 PM`.

## Risks / Trade-offs

- **5-Minute Increments vs. 1-Minute Precision**: For study reminders and streak warning alerts, 5-minute increments are optimal and eliminate endless scrolling. If a custom time is passed from the database (e.g. `08:17`), the minute column displays and rounds to the nearest slot while displaying the exact time.
