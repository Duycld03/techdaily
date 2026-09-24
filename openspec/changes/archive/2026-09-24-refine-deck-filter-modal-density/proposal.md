# Proposal: Refine Deck Filter Modal Layout & Sort Options Density

## Why

In the Spaced Repetition Deck Management interface (`/review` Tab 2), the Advanced Filter Modal (`AdvancedFilterModal.vue`) currently formats Section 4 ("SẮP XẾP THEO" / "Sort By") as an elongated 2-column grid (`grid grid-cols-1 sm:grid-cols-2 gap-2`).

Inside the modal container (`max-w-2xl`), each sort button unnaturally stretches across half of the entire modal width (~300px wide for only ~15-20 characters of text). This creates a stark visual disparity: while Sections 1, 2, and 3 ("NGUỒN KIẾN THỨC", "GIAI ĐOẠN THÀNH THẠO", and "MỨC ĐỘ ƯU TIÊN") render as clean, compact pill chips that size naturally around their content, Section 4 appears bloated and horizontally over-scaled.

Harmonizing Section 4 to use the same compact chip architecture (`flex flex-wrap items-center gap-2`) resolves the horizontal stretching, creates a cohesive visual rhythm throughout the entire modal, and maintains strict bilingual responsiveness.

## What Changes

- **Rebalance Sort Options Section in `AdvancedFilterModal.vue`**:
  - Replace the rigid 2-column grid (`grid grid-cols-1 sm:grid-cols-2 gap-2`) with `flex flex-wrap items-center gap-2`.
  - Align button dimensions, padding (`px-3.5 py-2 rounded-xl text-xs font-semibold whitespace-nowrap`), and active purple/inactive dark states with the filter chips in Sections 1–3.
  - Ensure all 4 sort options (`Ngày ôn gần nhất` / `Earliest`, `Ngày ôn xa nhất` / `Latest`, `Thẻ khó nhất trước` / `Hardest First`, `Mới tạo gần đây` / `Recently Created`) render with natural content-sized boundaries without excessive horizontal empty space.
- **Maintain Modal Structural Stability & i18n**:
  - Retain the `max-w-2xl` modal boundary and ensure zero text clipping across both English and Vietnamese locales.
  - Preserve all existing filter and sort state bindings (`localSortBy`, `emit('apply')`, `emit('reset')`).

## Capabilities

### Modified Capabilities

- `review`:
  - Update Advanced Filter Modal requirements to mandate cohesive chip layout and balanced visual density across all filter and sorting groups.

## Impact

- **Affected Code**: `frontend/components/review/AdvancedFilterModal.vue`.
- **Affected Tests**: `frontend/tests/components/reviewBentoComponents.spec.ts`.
- **Breaking Changes**: None. Filter logic, emitted payloads, and Pinia store contracts remain unchanged.
