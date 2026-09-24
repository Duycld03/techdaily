# Tasks: Native Studio Time Input Implementation

## 1. Component Refactoring

- [x] 1.1 In `frontend/components/common/AppTimePicker.vue`, replace the popover and teleport elements with an in-place native `<input type="time">` styled with Shadcn and TechDaily standards
- [x] 1.2 Implement value normalization in computed property (`formattedTime`) and bidirectional `update:modelValue` / `change` event handling
- [x] 1.3 Add inset Lucide `Clock` icon and dark mode indicator styling (`dark:[&::-webkit-calendar-picker-indicator]:invert`)

## 2. Testing & Verification

- [x] 2.1 Update unit tests in `frontend/tests/components/AppTimePicker.spec.ts` covering native time input rendering, value formatting, event emission, and disabled state
- [x] 2.2 Run full frontend test suite (`npm test`) and production build (`npm run build`)
- [x] 2.3 Capture visual test screenshots of the new Native Time Input in Settings (Desktop & Mobile, Dark & Light modes)
