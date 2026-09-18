# Proposal

## Why

1. **Legacy 30-Day Assumptions**: The Home Dashboard (`HomeBentoDashboard.vue`) and main navigation header (`AppHeader.vue`) currently hardcode `/ 30` in their progress badges (e.g. `Lộ trình Ngày 1 / 30`, `Day 1 / 30`). TechDaily is an open, tech-agnostic technical documentation learning engine where imported books have dynamic chunk counts (e.g. 15, 23, 50 slices). Hardcoding 30 contradicts the core platform invariant and misleads users whose active document has a different slice count (such as the ASP.NET Core guide with 23 slices).
2. **SVG Animation & Hover Jitter in Constellation Card**: In `DomainConstellationCard.vue`, using Tailwind's `animate-ping origin-center` on an SVG `<circle>` element causes expanding bubble distortions because CSS `transform-origin` behaves inconsistently across SVG viewports. Furthermore, applying `group-hover:translate-x-0.5` to the `<NuxtLink>` causes the "Mở Vũ Trụ 3D" header text to jump horizontally whenever the cursor moves over the card body.
3. **Duplicate Navigation Targets**: In the Home Bento Grid, both the "Đọc Tiếp" (Continue Reading) and "Giải Tình Huống" (Solve Challenge) buttons navigate to the exact same URL (`/today`). Reading and problem-solving represent distinct learning modes that require differentiated navigation paths.

## What Changes

- **Dynamic Pacer Slice & Curriculum Badges**:
  - Replace hardcoded `/ 30` in `frontend/components/dashboard/HomeBentoDashboard.vue` with dynamic Pacer telemetry: `Slice {pacer.currentChunkOrder} / {pacer.totalChunks}` (in Vietnamese: `Lát cắt {pacer.currentChunkOrder} / {pacer.totalChunks}`) when an active book exists.
  - Update `frontend/components/layout/AppHeader.vue` to dynamically display the active book slice or topic order without hardcoded 30-day caps.
  - Add bilingual i18n keys for slice badge formatting (`dashboard.active_slice_badge`: "Slice {current} / {total}" / "Lát cắt {current} / {total}").
- **Constellation Card Visual Polish**:
  - In `frontend/components/dashboard/DomainConstellationCard.vue`, eliminate `animate-ping origin-center` on SVG circles, replacing it with a concentric SVG halo and gentle `animate-pulse` (opacity modulation) that stays anchored to the node's `(cx, cy)` coordinate.
  - Remove `group-hover:translate-x-0.5` from the `<NuxtLink>` title, ensuring the "Mở Vũ Trụ 3D" text remains rock-solid when hovering over the card.
- **Differentiated Action Navigation**:
  - Update Card A CTA ("Continue Reading" / "Đọc Tiếp"): Navigate directly to the GitBook reader at the active slice (`/read/${pacer.bookId}?slice=${pacer.currentChunkOrder}`), falling back to `/today` if no book is active.
  - Update Card B CTA ("Solve Challenge" / "Giải Tình Huống"): Navigate to `/today?tab=challenge`, driving the user straight to the Focus Studio interview scenario drill.
- **Regression Verification**:
  - Update unit tests in `HomeBentoDashboard.spec.ts` and `DomainConstellationCard.spec.ts` to assert dynamic slice rendering, stable constellation SVG rendering, and distinct navigation links.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Ensure dynamic slice progress badges reflecting active document bounds, stabilize constellation SVG animation, and provide dedicated navigation targets for reader vs scenario drills.

## Impact

- **Affected Areas**: `frontend/components/dashboard/HomeBentoDashboard.vue`, `frontend/components/dashboard/DomainConstellationCard.vue`, `frontend/components/layout/AppHeader.vue`, `frontend/i18n/locales/en.json`, `frontend/i18n/locales/vi.json`, associated test files.
- **User Experience**: Clear document slice context, smooth visual constellation display without ballooning bubbles or jumpy hover text, and direct one-click access to the GitBook reader at the current reading position.
- **Breaking Changes**: None.
