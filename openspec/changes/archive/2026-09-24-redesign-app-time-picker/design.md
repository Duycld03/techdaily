# Technical Design: Dev-Learning Studio AppTimePicker Redesign

## Context

`AppTimePicker.vue` is a foundation UI primitive used in `frontend/pages/settings.vue` for scheduling daily study focus reminders (`preferredStudyTime`) and evening streak preservation alerts (`streakAlertTime`). It receives a 24-hour time string model (`HH:mm`), parses it into 12-hour components (Hours 1–12, Minutes 00–55 in 5m steps, Period AM/PM), and renders a teleported popover.

See `proposal.md` for motivation and background.

## Goals / Non-Goals

**Goals:**
- Upgrade the visual architecture of `AppTimePicker.vue` to match TechDaily's **Dev-Learning Studio** design system (`.glass-panel`, `dark:bg-canvas-elevated`, hairline translucent border `dark:border-white/[0.08]`).
- Replace clunky, sunken separated scroll containers with cohesive, subtle column wells.
- Align the AM/PM selector as a balanced vertical segmented control eliminating awkward empty space.
- Standardize active states with studio brand highlights (`bg-brand-500/15 text-brand-600 dark:text-brand-400 font-bold border border-brand-500/30`), replacing harsh solid purple fills.
- Provide a sleek, compact confirmation button in the popover footer (`h-8 text-xs font-semibold rounded-xl`).
- Maintain 100% functional parity, keyboard accessibility (`Esc`, `Tab`), outside-click dismissal, and unit test coverage.

**Non-Goals:**
- Introducing external date/time picker libraries (e.g. flatpickr or v-calendar) — TechDaily strictly uses zero-overhead custom Vue primitives.
- Modifying backend time storage or scheduling payload formats (remains standard `HH:mm`).

## Decisions

### 1. Popover Container Surface & Geometry
The popover container will inherit standard Dev-Learning Studio elevation tokens:
```html
<div
  class="glass-panel dark:bg-canvas-elevated border border-slate-200/90 dark:border-white/[0.08] shadow-2xl rounded-2xl p-3.5 backdrop-blur-md space-y-3 outline-none z-50 w-64"
>
```
Concentric curvature ensures outer `rounded-2xl` nests inner `rounded-xl` wells, and inner button targets use `rounded-lg` / `rounded-md`.

### 2. Symmetrical 3-Column Architecture
The 3 columns (Hours, Minutes, Period) will be organized into a 3-column grid (`grid grid-cols-3 gap-2`):
- **Headers**: Uppercase micro-typography (`text-[10px] font-bold tracking-wider text-slate-400 dark:text-slate-500 text-center`).
- **Hours & Minutes Wells**: `max-h-40 overflow-y-auto space-y-0.5 p-1 rounded-xl bg-slate-100/40 dark:bg-white/[0.02] border border-slate-200/60 dark:border-white/[0.05] scrollbar-none`.
- **Active Selection**: `bg-brand-500/15 text-brand-600 dark:text-brand-400 font-bold border border-brand-500/30 shadow-xs`.
- **Inactive Hover**: `text-slate-700 dark:text-slate-300 hover:bg-slate-200/60 dark:hover:bg-white/[0.06] border border-transparent`.

### 3. Balanced AM/PM Segmented Control
Rather than leaving the third column half-empty:
- The Period column container spans the same height as the number wells.
- AM and PM buttons will be evenly spaced within the well with symmetric vertical padding (`py-3` or flex-centered) to fill the column gracefully.

### 4. Compact Studio Action Footer
The footer confirmation button will match Density 8/10 ergonomics:
```html
<button
  type="button"
  @click="closeDropdown"
  class="w-full h-8 flex items-center justify-center rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-xs font-semibold shadow-sm transition-all active:scale-[0.98] cursor-pointer"
>
  {{ $t('settings.btn_done') }}
</button>
```

### 5. Density 8/10 Trigger Button
The trigger button (`combobox`) will retain:
- Clock icon, formatted time in `font-mono tabular-nums`, and smooth rotating `ChevronDown`.
- Standardized padding and height matching `AppSelect.vue`.

## Risks / Trade-offs

- **Small Viewport Clipping**: The popover is teleported to `body` and utilizes dynamic floating coordinates with bounds checking against viewport edges.
- **Scrollbar Visual Noise**: Uses `scrollbar-none` utility to prevent thick native scrollbars from cluttering the compact number columns on Windows and Linux browsers.
