# Tasks: Modern Segmented TimePicker Refactoring

## 1. Component Refactoring

- [x] 1.1 In `frontend/components/common/AppTimePicker.vue`, remove the redundant time badge from the top header
- [x] 1.2 In `frontend/components/common/AppTimePicker.vue`, remove the quick preset buttons row
- [x] 1.3 In `frontend/components/common/AppTimePicker.vue`, replace the bomb-game chevrons with clean Shadcn-style segmented inputs (`w-16 h-12 text-xl font-bold font-mono rounded-xl`)
- [x] 1.4 In `frontend/components/common/AppTimePicker.vue`, replace the vertical oval period toggle with a horizontal segmented control (`[ AM ] [ PM ]`)
- [x] 1.5 Implement auto-focus advancement (Hour -> Minute on 2 digits), ArrowLeft/Right focus shifting, and mouse-wheel stepping

## 2. Testing & Verification

- [x] 2.1 Update unit tests in `frontend/tests/components/AppTimePicker.spec.ts` covering segmented input typing, focus shifting, AM/PM toggle, and keyboard arrows
- [x] 2.2 Run full frontend test suite (`npm test`) and production build (`npm run build`)
- [x] 2.3 Capture visual test screenshots of the new Shadcn Segmented TimePicker in Settings (Desktop & Mobile, Dark & Light modes)
