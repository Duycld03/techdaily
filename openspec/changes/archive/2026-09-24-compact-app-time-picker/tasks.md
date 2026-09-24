# Tasks: Compact AppTimePicker Popover Dimensions

## 1. Frontend - AppTimePicker Component Compaction

- [x] 1.1 In `frontend/components/common/AppTimePicker.vue`, replace `h-40 flex-1` on Hours and Minutes scroll tracks with fixed `h-28 max-h-28 overflow-y-auto scrollbar-none`
- [x] 1.2 In `frontend/components/common/AppTimePicker.vue`, balance the Period column container to `h-28` with symmetrical pill buttons (`h-[50px]` each)
- [x] 1.3 In `frontend/components/common/AppTimePicker.vue`, tighten popover shell padding to `p-3 space-y-2.5`, header to `pb-1.5`, and footer confirmation button to `h-7.5`
- [x] 1.4 In `frontend/components/common/AppTimePicker.vue`, update `updateFloatingPosition()` collision detection threshold to `spaceBelow < 230`

## 2. Testing & Verification

- [x] 2.1 Update unit tests in `frontend/tests/components/AppTimePicker.spec.ts` to assert compact height classes (`h-28`, `max-h-28`) and button ergonomics
- [x] 2.2 Execute frontend test suite (`npm test`) and production build (`npm run build`) to ensure zero regressions
- [x] 2.3 Capture visual test screenshots in Settings popover (both Desktop and Mobile) confirming total height $\le 220\text{px}$ and zero viewport overflow
