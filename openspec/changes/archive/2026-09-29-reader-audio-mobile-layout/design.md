# Design: Reader Audio Player Mobile Responsive Layout

## Context

The reader audio player (`ReaderAudioPlayer.vue`) is deployed on all book slice reading screens.
- On desktop viewports (≥640px), the player bar spans between 800px and 1200px, easily accommodating all controls horizontally: Play button, Engine switch, Voice picker, Scrubber slider, Timestamps, Synthesis badge, Device badge, and Speed button in a single row (`min-h-[50px]`).
- On mobile viewports (<640px, e.g. 390px iPhone, 360px Android), the usable horizontal width inside the content container is only ~328px.
- Previously, placing both the voice selector (`flex-1`) and an idle spacer (`flex-1`), or placing both the voice selector and the scrubber slider (`flex-1`) in the same horizontal flex row caused severe layout crushing:
  - In idle state: The voice dropdown was compressed to ~70px, truncating labels into unreadable stubs (`"N.. ⌵"`).
  - In playing state: The voice selector was squeezed down to `0px` or an empty icon pill ("nó mất tiêu luôn"), while the scrubber slider was squished below 40px width.

## Goals / Non-Goals

**Goals:**
- Guarantee that the voice dropdown label remains clearly legible (e.g. `"Neural2-A"`, `"Standard-A"`) on mobile in both idle and active states without truncating into illegible two-letter stubs.
- Prevent the voice selector from collapsing to 0px or vanishing when playback starts.
- Provide a full-width, finger-friendly scrubber slider on mobile screens during active playback.
- Preserve the existing desktop layout intact (single row, `min-h-[50px]`).
- Keep `AppSelect.vue` untouched as a system-wide common component.

**Non-Goals:**
- Modifying audio synthesis, Web Worker, or audio streaming logic.
- Adding new audio features or changing backend APIs.

## Decisions

### Decision 1: Two-Tier Responsive Architecture for Mobile Playback

Instead of forcing all 6 controls into a single row on 328px mobile screens, structure `ReaderAudioPlayer.vue` using responsive layout tiers:

```
Mobile Playing State (< 640px):
+-------------------------------------------------------------+
| [🔊 Tạm dừng] [ ☁ | 💻 ]  [ Neural2-A (Nữ) ⌵ ]         [1x] |  <-- Row 1: Primary Controls
| [ ───●───────────────────────────────── ] 0:04 / 2:33 (2/52)|  <-- Row 2: Touch Scrubber (w-full)
+-------------------------------------------------------------+

Mobile Idle State (< 640px):
+-------------------------------------------------------------+
| [🔊 Nghe]     [ ☁ | 💻 ]  [ Neural2-A (Nữ) ⌵ ]         [1x] |  <-- 1 Row: Clean & Compact (50px)
+-------------------------------------------------------------+

Desktop / Tablet State (≥ 640px):
+-----------------------------------------------------------------------------------------+
| [🔊 Nghe] [ ☁ Cloud | 💻 Device ] [ Neural2-A ⌵ ] [ ───●────── ] 0:04/2:33  [CPU]  [1x] |
+-----------------------------------------------------------------------------------------+
```

1. **Top Row (Controls)**:
   - Contains: Play/Pause button (`shrink-0`), Engine mode switch (`shrink-0`), Voice picker (flex-allocated with `min-w-[130px]`), and Speed button (`shrink-0 ml-auto sm:ml-0`).
   - Does not have competing flex spacers.
2. **Bottom Row (Scrubber & Status - Mobile only)**:
   - Condition: `v-if="loadedId === chunk?.id && duration > 0"` rendered only on `< sm` screens (`sm:hidden`).
   - Spans `w-full` with full-width `<input type="range">`, time display, and synthesis progress badge.
   - Provides an accessible touch track for thumb scrubbing.
3. **Desktop Scrubber**:
   - Retained inline with `hidden sm:flex sm:flex-1 sm:items-center sm:gap-2` so desktop viewports remain a single unified row.

### Decision 2: Elimination of Competing Flex Spacers

In `ReaderAudioPlayer.vue`, remove the redundant `<span v-else class="flex-1" />` spacer in the primary controls row. When the voice picker is active, it cleanly takes the available middle space without being crowded out by an empty spacer.

## Risks / Trade-offs

- **Mobile Player Height during playback**: When playing audio on mobile, the player height will expand to ~78px to accommodate the full-width scrubber row.
  - *Trade-off*: A dedicated 28px bottom scrubber row ensures thumb-friendly seeking and completely avoids squishing controls. When paused or idle, the player stays at a compact 50px single row. This matches the interaction design of mobile podcast and music players (Spotify, Apple Podcasts).
