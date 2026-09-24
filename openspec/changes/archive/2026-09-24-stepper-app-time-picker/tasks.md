# Tasks: Stepper & Direct-Input Architecture for AppTimePicker

## 1. Component Refactoring

- [x] 1.1 In `frontend/components/common/AppTimePicker.vue`, replace the 3-column scroll tracks with 3 digital stepper segments (Hour, Minute, Period) with Up/Down buttons
- [x] 1.2 Implement direct numeric input binding for Hour (1-12) and Minute (0-59) with auto-formatting and ArrowUp/Down key listeners
- [x] 1.3 Add quick preset chips (`08:00 AM`, `12:00 PM`, `08:00 PM`, `10:00 PM`) for instant 1-click time setting
- [x] 1.4 Refine container spacing and eliminate all scrollbars, achieving $\le 180\text{px}$ popover height

## 2. Testing & Verification

- [x] 2.1 Update unit tests in `frontend/tests/components/AppTimePicker.spec.ts` covering direct input, stepper increment/decrement, preset clicks, and boundary wrapping
- [x] 2.2 Run full frontend test suite (`npm test`) and production build (`npm run build`) to ensure zero regressions
- [x] 2.3 Capture visual test screenshots of the new Stepper TimePicker in Settings (Desktop & Mobile, Dark & Light modes)
