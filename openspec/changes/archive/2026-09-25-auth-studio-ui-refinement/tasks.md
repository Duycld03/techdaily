# Tasks: Refine Studio Auth Layout to Authentic Design Specification

## 1. Top Header & Sub-Header Telemetry Removal
- [x] 1.1 In `frontend/pages/login.vue`, remove the `PING 18ms` badge (Image #4) and `IDE v2.5.0-sys` pill (Image #5), keeping top header branding clean with `[BookOpen] TechDaily` and right-side controls (`LocaleSelector`, `ThemeToggle`)
- [x] 1.2 In `frontend/pages/login.vue`, completely remove the sub-header status line (`• TẤT CẢ DỊCH VỤ HOẠT ĐỘNG • ĐỘ TRỄ 14MS ... ⚡ QUY TẮC BẤT BIẾN ...`, Image #3)

## 2. OAuth Provider Streamlining & Footer Removal
- [x] 2.1 In `frontend/pages/login.vue`, remove the GitHub OAuth button from the developer stack
- [x] 2.2 In `frontend/pages/login.vue`, center the Google Sign-In button as the dedicated 1-click OAuth option
- [x] 2.3 In `frontend/pages/login.vue`, completely remove the bottom telemetry footer bar (`TECHDAILY PHÂN TÁN // NÚT_XÁC_THỰC_AN_TOÀN ... ĐỘNG CƠ TECHDAILY // ... TLS 1.3 ...`, Image #2) and redundant compliance line
## 3. TechDaily Domain Content & Branding Alignment
- [x] 3.1 Replace mock Staff+ / IDE copy with authentic TechDaily documentation reading and SM-2 spaced repetition copy in `en.json` and `vi.json`
- [x] 3.2 Update left showcase card with TechDaily practice snippet (`techDaily.getDailySlice`), reading goals, and SM-2 active recall indicators
- [x] 3.3 Fix tab switching border flicker by eliminating `backdrop-blur-2xl` GPU redraw artifacts, adding `focus:outline-none focus:ring-0`, and switching button transitions to `transition-colors`
- [x] 3.4 Ensure Google Sign-In button is always reliably rendered with official GSI auto-injection and persistent styled fallback

## 4. Testing & Visual Verification
- [x] 4.1 Update unit tests in `frontend/tests/pages/login.spec.ts` for clean header branding, TechDaily content assertions, and Google OAuth
- [x] 4.2 Run full frontend test suite (`npm test`) and production build (`npm run build`) with 100% pass rate
- [x] 4.3 Visually verify seamless tab transitions and persistent Google button in browser via headless evaluation
