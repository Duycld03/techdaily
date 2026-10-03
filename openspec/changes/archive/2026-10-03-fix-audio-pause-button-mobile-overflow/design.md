# Design

## Context

See `proposal.md` for motivation.

`ReaderAudioPlayer.vue` renders an audio playback interface inside technical reading views (`/read/[bookId]` and `/today` via `DocReaderPane.vue`). The component organizes controls into two rows:
- **Row 1**: Primary playback controls and utilities arranged in a `flex items-center justify-between gap-2 w-full min-w-0` layout:
  - Left cluster: Play/Pause button and Engine Mode segmented switch (System, Cloud, Device).
  - Right cluster: Auto-advance toggle button, Sleep timer menu trigger, and Playback speed toggle (`{{ speed }}x`).
- **Row 2**: Voice selection dropdown (System/Cloud) or Device label, along with error messages, loading indicators, or the audio scrubber slider.

Currently, both the left and right clusters in Row 1 apply `shrink-0 whitespace-nowrap`. On compact mobile viewports (360px–390px width), the available content width inside the reader card is approximately 320px–336px after accounting for container padding. 

When audio begins playing:
1. Visible button text changes from `reader.audio_listen` ("Nghe" / "Listen") to `reader.audio_pause` ("Tạm dừng giọng đọc" in Vietnamese, 18 characters; "Pause narration" in English, 15 characters).
2. The button width expands from ~75px to ~165px.
3. Total required width for Row 1 expands to ~382px, exceeding the ~320px–336px container width.
4. Because both clusters have `shrink-0` and the parent has `justify-between`, the rightmost element—the speed button (`{{ speed }}x`)—is pushed completely outside the reader card boundary.

## Goals / Non-Goals

**Goals:**
- Maintain stable, compact button dimensions across idle, loading, and playing states in both Vietnamese and English locales.
- Ensure all controls in Row 1 (Play/Pause, Engine Switch, Auto-Advance, Sleep Timer, Speed Selector) remain 100% visible and contained inside the reader card on mobile screens down to 360px width.
- Preserve screen reader accessibility by providing descriptive accessible names (`aria-label`) without expanding visible button dimensions.
- Adhere to AGENTS.md Pillar 2 (responsive typography, bilingual layout invariants) and Dev-Learning Studio design system conventions.

**Non-Goals:**
- Redesigning the underlying synthesis worker, WebGPU/WASM pipeline, or IndexedDB caching logic.
- Altering Row 2 scrubber slider or voice selector logic.
- Wrapping Row 1 controls into multiple stacked rows on mobile (which would degrade vertical reading space).

## Decisions

### Decision 1: Shorten `reader.audio_pause` to concise action verbs in localization catalogs
- **Rationale**: Media players universally use concise action pairs: "Listen" / "Pause" in English and "Nghe" / "Tạm dừng" in Vietnamese. "Tạm dừng" is 8 characters (~55px text width), reducing the button width by over 70px and keeping it symmetrical with "Nghe" (4 characters).
- **Alternatives Considered**:
  - *Responsive text truncation / `hidden sm:inline`*: Hiding "giọng đọc" on mobile via CSS classes splits translation grammar across markup and complicates localization.
  - *Icon-only toggle button*: Removing text entirely degrades primary action discoverability. "Nghe" and "Tạm dừng" provide immediate clarity.

### Decision 2: Separate visible button label from full descriptive accessibility label
- **Rationale**: While visual buttons require compact labels, screen readers benefit from descriptive announcements.
  - Visible button: `{{ playing ? t('reader.audio_pause') : t('reader.audio_listen') }}` using "Tạm dừng" / "Nghe" (vi) and "Pause" / "Listen" (en).
  - Screen reader `aria-label`: `:aria-label="playing ? t('reader.audio_pause_desc') : t('reader.audio_play')"` ("Tạm dừng giọng đọc" / "Phát giọng đọc" in Vietnamese, "Pause narration" / "Play narration" in English).
- **Alternatives Considered**:
  - *Using "Tạm dừng" for both visible text and aria-label*: While acceptable, retaining explicit descriptive strings in `aria-label` provides the highest accessibility standard without visual cost.

### Decision 3: Responsive cluster spacing and compact mobile button padding
- **Rationale**: To guarantee that all controls fit within 320px available width even when sleep timer shows a timer label (e.g. "14:59"), refine Row 1 spacing:
  - Left cluster: `gap-1.5 sm:gap-2`
  - Play/pause button: `px-2.5 sm:px-3 text-xs sm:text-sm`
  - Engine switch buttons: `px-1.5 sm:px-2.5 py-1`
  - Right cluster: `gap-1 sm:gap-1.5`
  - Speed button: `px-1.5 sm:px-2 py-1 text-xs tabular-nums`
- **Alternatives Considered**:
  - *Horizontal scrolling row (`overflow-x-auto`)*: Horizontal scrollbars inside a media player card create poor UX and clumsy touch interactions on mobile.

## Risks / Trade-offs

- **[Risk]** Existing Vitest tests asserting visible button text might fail if they expect the previous "Pause narration" string.
  - **Mitigation**: Inspect and update `ReaderAudioPlayer.spec.ts`. The existing assertion `expect(playBtn.text()).toContain('Pause')` already matches "Pause". Update the test message catalog so mock translations match the updated dictionary.
- **[Risk]** Active sleep timer badge (e.g. `14:59`) adding horizontal width on ultra-narrow viewports (320px).
  - **Mitigation**: With the 70px reduction from "Tạm dừng", total Row 1 width with an active timer is ~305px, comfortably fitting within 320px–336px available content width.
