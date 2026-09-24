# Tasks: Dev-Learning Studio Authentication Surface Redesign

## 1. Frontend - App Shell Isolation

- [x] 1.1 In `frontend/app.vue`, add route detection for authentication routes (`isAuthPage = computed(() => route.path === '/login')`)
- [x] 1.2 Suppress `AppSidebar` rendering on guest authentication routes (`v-if="!isReaderMode && !isAuthPage"`) to eliminate internal navigation clutter for unauthenticated users

## 2. Frontend - Studio Auth Viewport Redesign

- [x] 2.1 Refactor outer container in `frontend/pages/login.vue` into a responsive Studio Auth viewport (`w-full max-w-5xl mx-auto grid lg:grid-cols-12 gap-8 lg:gap-12 items-center`)
- [x] 2.2 Implement left branding and platform value stage (`lg:col-span-5 hidden lg:block`) featuring TechDaily emblem, mission heading, and 3 feature cards (Scenario Drills, SM-2 Spaced Mastery, Daily System Design Slices)
- [x] 2.3 Implement right interactive authentication card (`lg:col-span-7 w-full`) housing the mode switcher tabs, credentials forms, and action controls within a sleek `.glass-panel`
- [x] 2.4 Re-architect Register mode form inputs into an ergonomic 2-column grid on desktop (`sm:grid-cols-2 gap-3.5`) for Full Name + Email and Password + Confirm Password, reducing vertical elongation by over 40%
- [x] 2.5 Refactor the OAuth divider to use an absolute centering architecture (`absolute inset-0 flex items-center`) with a centered badge, preventing flex width blowout and ensuring centered text alignment

## 3. Testing & Verification

- [x] 3.1 Update unit tests in `frontend/tests/pages/login.spec.ts` covering the 2-column register layout, form validation, password visibility toggles, and shell isolation
- [x] 3.2 Execute frontend test suite (`npm test`) and production build (`npm run build`) to ensure zero regressions
- [x] 3.3 Capture visual test screenshots in English and Vietnamese across Desktop (1920x1080) and Mobile viewports verifying balanced layout proportions and zero layout shift
