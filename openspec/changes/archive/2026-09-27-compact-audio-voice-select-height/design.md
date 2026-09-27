# Design

## Context

In `ReaderAudioPlayer.vue`, the player bar arranges its primary controls in a horizontal flex layout with `items-center gap-2 sm:gap-3`:
- **"Listen" Action Button**: `flex items-center gap-1.5 shrink-0 whitespace-nowrap rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-sm font-semibold px-3 py-1.5` (~32px rendered height).
- **Engine Toggle (Cloud vs Device)**: Segmented control with `p-0.5` outer container and `px-2.5 py-1 text-xs` buttons (~28-30px height).
- **Voice Selector (`AppSelect.vue`)**: Rendered when `engineMode === 'cloud'`.

Currently, `AppSelect.vue` hardcodes trigger button classes with generous form-input padding:
```vue
w-full flex items-center justify-between gap-2.5 px-3.5 py-2.5 rounded-xl text-left text-xs sm:text-sm font-semibold ...
```
Because of `py-2.5`, `AppSelect` renders at ~42px height. When switching to Cloud mode, this taller component forces the parent container to expand vertically, causing visual layout shifting and height jumping.

## Goals / Non-Goals

**Goals:**
- Provide a standardized `size?: 'sm' | 'md'` prop on `AppSelect.vue` (defaulting to `'md'` for complete backward compatibility across existing forms, modals, and settings).
- Configure `size="sm"` on `AppSelect.vue` to use compact vertical padding (`py-1.5`), compact horizontal padding (`px-3`), and balanced gaps (`gap-2`), producing a ~32px rendered height that matches the "Listen" button.
- Apply `size="sm"` in `ReaderAudioPlayer.vue` to ensure the audio player control bar maintains a uniform height across both Device and Cloud modes.

**Non-Goals:**
- Modifying standard form select heights in settings, library upload modals, or quiz pages.
- Altering the backend API, speech synthesis pipelines, or voice quota management.

## Decisions

### Decision 1: First-class `size` prop on `AppSelect.vue` vs arbitrary class overrides
- **Rationale**: Adding `size?: 'sm' | 'md'` provides a clean, reusable design-system primitive. When `size === 'sm'`, the trigger button applies:
  - Padding: `px-3 py-1.5` (vs `px-3.5 py-2.5` for `'md'`)
  - Typography: `text-xs sm:text-sm font-semibold`
  - Chevron / Icon: `w-3.5 h-3.5` (vs `w-4 h-4` for `'md'`)
- **Alternative considered**: Passing raw utility classes through an extra prop like `triggerClass`. Rejected because component consumers would have to duplicate focus rings, states, and icon alignment classes, risking design system drift.

### Decision 2: Alignment in `ReaderAudioPlayer.vue`
- Update `<AppSelect>` in `ReaderAudioPlayer.vue` with `size="sm"`.
- Ensure the wrapper `div` (`w-36 sm:w-44 shrink-0`) and the trigger button align flush with the Listen button and segmented switch without vertical offset or baseline misalignment.

## Risks / Trade-offs

- **Risk**: Potential label truncation on small mobile screens.
- **Mitigation**: The wrapper is constrained to `w-36 sm:w-44`, and the inner text span already employs `truncate`. The voice options use concise labels ("Nữ (Neural2-A)", "Female (Neural2-F)"), verified to fit comfortably on 390px mobile viewports without text collision.
