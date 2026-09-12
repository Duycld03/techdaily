## Why

The current bottom navigation row in `/read/[bookId]` interpolates long chapter titles directly into an inline button (`Tiếp theo: <Long Chapter Title>`), causing extreme visual asymmetry where the Next button spans up to 70% of the width while the Previous button and progress indicator get compressed and break into awkward two-line fragments (e.g., `Lát Cắt` / `Trước` and `9/141 hoàn` / `thành`). Replacing this clunky row with a modern, symmetrical two-card layout (similar to GitBook, VitePress, and Stripe Docs) provides balanced, elegant navigation that handles arbitrary chapter title lengths without visual distortion.

## What Changes

- Replace the asymmetric bottom navigation row with a balanced two-column card layout (`grid grid-cols-1 sm:grid-cols-2 gap-4`).
- **Previous Card (Left)**: Renders a dedicated card with an uppercase label (`← PREVIOUS` / `← LÁT CẮT TRƯỚC`) and the previous slice's chapter title, styled with smooth hover elevation. Gracefully hidden or preserved as empty space when on slice 1.
- **Next Card (Right)**: Renders a dedicated card with an uppercase label (`NEXT →` / `TIẾP THEO →` or `COMPLETED →` / `HOÀN THÀNH →`) and the next slice's chapter title (or "Return to Library"), styled with subtle brand accent highlights.
- **Progress Meta**: Display reading progress (`reader.done` / `9 / 141 slices (6%)`) as a clean, centered or top-aligned meta badge that never suffers from text wrapping or horizontal clipping.
- **Bilingual & Responsive Ergonomics**: Ensure both English and Vietnamese text labels fit cleanly without text wrapping (`whitespace-nowrap shrink-0` on labels, clean `truncate` on chapter titles), adapting from stacked on mobile to 50/50 side-by-side on desktop.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `reader`: Update bottom navigation requirements to specify balanced two-card navigation (`Previous Slice` and `Next Slice`) with structured uppercase subheadings and truncated chapter titles, eliminating asymmetric button stretching.

## Impact

- Frontend: `frontend/pages/read/[bookId].vue` bottom navigation markup and styling.
- Localization: `frontend/i18n/locales/en.json` and `vi.json` for card subheadings and action labels.
- Specs: Delta spec in `openspec/changes/reader-bottom-nav-cards/specs/reader/spec.md`.
