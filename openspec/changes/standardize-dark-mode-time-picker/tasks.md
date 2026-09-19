# Tasks

## 1. Frontend: Global Dark Mode Color-Scheme & Temporal Inputs

- [x] 1.1 In `frontend/assets/css/main.css`, define `color-scheme: dark;` on `html.dark`, `.dark`, and temporal input elements (`input[type="time"]`, `input[type="date"]`, `input[type="datetime-local"]`), and define `color-scheme: light;` on `html:not(.dark)`.
- [x] 1.2 In `frontend/assets/css/main.css`, configure `::-webkit-calendar-picker-indicator` with invert filter and pointer cursor to guarantee high contrast and clickability on dark backgrounds.
- [x] 1.3 In `frontend/pages/settings.vue`, verify that native study time pickers render with studio dark styles without border or outline distortions.

## 2. Frontend: Daily Focus Studio Loading Indicator Realignment

- [x] 2.1 In `frontend/pages/today.vue`, import `Loader2` from `lucide-vue-next` and replace the spinning `<Sparkles>` icon (lines 13 & 339-341) with `<Loader2 class="animate-spin" :stroke-width="1.5">`.

## 3. Verification & Automated Test Suite Execution

- [x] 3.1 Validate OpenSpec specifications and schema using `openspec validate --changes` and `openspec validate --specs`.
- [x] 3.2 Execute frontend test suite (`npm test` in `frontend/`) to ensure all assertions pass cleanly.
- [x] 3.3 Execute full Nuxt production build (`npm run build` in `frontend/`) to verify clean SSR compilation.
