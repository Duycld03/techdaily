# Tasks: Brand Logo & Favicon Redesign with Stitch Developer Emblem

- [x] 1.1 In `frontend/public/favicon.svg`, replace legacy green book SVG with the full Stitch Developer Emblem vector markup from `local://paste-4.md`
- [x] 1.2 Verify `frontend/nuxt.config.ts` favicon link tag (`/favicon.svg`) resolves cleanly in browser tabs
## 2. Reusable Brand Emblem Component
- [x] 2.1 Create `frontend/components/common/AppLogo.vue` supporting flexible sizing props (`'sm'`: 28px, `'md'`: 32px, `'lg'`: 40px, or custom number) and accessibility metadata (`role="img"`, `aria-label="TechDaily"`)
- [x] 2.2 Implement the complete Stitch SVG definitions inside `AppLogo.vue` (squircle canvas, gradient hexagon outer ring, hairline inner hexagon, white code brackets `< >`, radiant nucleus node, and axis balance dots)

## 3. Global Navigation & Header Integration
- [x] 3.1 In `frontend/components/layout/AppHeader.vue`, replace the desktop topbar gradient `BookOpen` box with `<AppLogo size="md" />`
- [x] 3.2 In `frontend/components/layout/AppHeader.vue`, replace the mobile navigation drawer brand header emblem with `<AppLogo size="md" />`
- [x] 3.3 In `frontend/pages/login.vue`, replace the topbar brand emblem with `<AppLogo size="md" />`

## 4. Testing & Verification
- [x] 4.1 Create unit tests in `frontend/tests/components/AppLogo.spec.ts` testing SVG rendering, size prop scaling, and accessibility attributes
- [x] 4.2 Update unit tests in `frontend/tests/components/AppHeader.spec.ts` and `frontend/tests/pages/login.spec.ts` to assert `AppLogo` presence and root navigation
- [x] 4.3 Execute full frontend test suite (`npm test`) and production build (`npm run build`) to ensure zero regressions
- [x] 4.4 Capture browser verification screenshots of the new logo across desktop (1440x900) and mobile (375x812) viewports in dark and light modes
