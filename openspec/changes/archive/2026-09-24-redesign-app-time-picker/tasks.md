# Tasks: Dev-Learning Studio AppTimePicker Redesign

## 1. Frontend - AppTimePicker Component Redesign

- [x] 1.1 Update popover container in `frontend/components/common/AppTimePicker.vue` with `.glass-panel` elevation, `dark:bg-canvas-elevated`, hairline borders (`dark:border-white/[0.08]`), concentric `rounded-2xl` curvature, and backdrop blur
- [x] 1.2 Redesign column headers and number wells with micro-typography (`text-[10px] font-bold tracking-wider uppercase`) and subtle well backgrounds (`bg-slate-100/40 dark:bg-white/[0.02] border-slate-200/60 dark:border-white/[0.05]`)
- [x] 1.3 Upgrade AM/PM period selector into a balanced vertical segmented control eliminating asymmetrical dead space
- [x] 1.4 Refactor selection active and hover states to use Dev-Learning Studio brand highlight pills (`bg-brand-500/15 text-brand-600 dark:text-brand-400 font-bold border-brand-500/30`)
- [x] 1.5 Modernize footer action button to a compact Density 8/10 confirmation button (`h-8 text-xs font-semibold rounded-xl bg-brand-600`) with smooth active scale transitions

## 2. Frontend - Settings Integration & Showcase

- [x] 2.1 Verify time picker trigger ergonomics, focus rings, and tabular figures in `frontend/pages/settings.vue` (study schedule and streak alert time pickers)
- [x] 2.2 Add interactive `AppTimePicker` demo to UI Primitives section in `frontend/pages/showcase.vue` for ongoing design regression testing

## 3. Testing & Verification

- [x] 3.1 Update unit tests in `frontend/tests/components/AppTimePicker.spec.ts` covering time selection, AM/PM switching, outside-click dismissal, and keyboard accessibility
- [x] 3.2 Execute frontend test suite (`npm test`) and production build (`npm run build`) to ensure zero regressions
- [x] 3.3 Capture visual test screenshots in English and Vietnamese across Desktop and Mobile viewports verifying zero truncation or layout shift
