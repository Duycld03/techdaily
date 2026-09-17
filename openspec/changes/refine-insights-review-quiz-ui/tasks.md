# Tasks: Refine Insights, Review Session, Advanced Filter, and Quiz Bento UI

## 1. Insights Dedicated View Mode Switcher & Tailored Empty State (`frontend/pages/insights.vue`)

- [x] 1.1 In `frontend/pages/insights.vue`, declare reactive state `viewMode = ref<'explore' | 'saved'>('explore')` and a computed property `bookmarkedCount` reflecting `insightsStore.bookmarkedInsights.length`.
- [x] 1.2 In `frontend/pages/insights.vue`, extract the "Đã Lưu" button out of the horizontal category chip bar and implement the dedicated View Mode Switcher directly beneath the Header Banner and above the Category Filter Bar:
  - Option 1: `[ 🌐 Khám Phá ]` (`$t('insights.view_explore')`), active when `viewMode === 'explore'`.
  - Option 2: `[ 🔖 Đã Lưu (N) ]` (`$t('insights.view_saved')`), active when `viewMode === 'saved'` and displaying the total count of bookmarked cards.
- [x] 1.3 In `frontend/pages/insights.vue`, ensure category filtering remains active and independent for BOTH modes:
  - In Explore mode, selecting a category filters the general feed by category.
  - In Saved mode, selecting a category calls `fetchFeed(catId, null, true)` to filter bookmarked insights by that category.
- [x] 1.4 In `frontend/pages/insights.vue`, implement the tailored empty state for Saved mode when `!insightsStore.currentInsight && viewMode === 'saved'`:
  - Icon: `BookmarkCheck` in an indigo badge container.
  - Title: "Bạn chưa lưu mẫu kiến thức nào" (`$t('insights.saved_empty_title')`).
  - Description: "Hãy bấm biểu tượng Bookmark trên các thẻ kiến thức khi khám phá để lưu lại xem sau." (`$t('insights.saved_empty_desc')`).
  - CTA Button: `[ 🌐 Khám Phá Kiến Thức Ngay ]` (`$t('insights.saved_empty_cta')`) which calls `switchViewMode('explore')`.
- [x] 1.5 In `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, add localized keys:
  - `insights.view_explore`: "Khám Phá" / "Explore"
  - `insights.view_saved`: "Đã Lưu ({count})" / "Saved ({count})"
  - `insights.saved_empty_title`: "Bạn chưa lưu mẫu kiến thức nào" / "No saved insights yet"
  - `insights.saved_empty_desc`: "Hãy bấm biểu tượng Bookmark trên các thẻ kiến thức khi khám phá để lưu lại xem sau." / "Click the Bookmark icon on insight cards while exploring to save them for later."
  - `insights.saved_empty_cta`: "Khám Phá Kiến Thức Ngay" / "Explore Insights Now"

---

## 2. Review Session Completion State Deduplication (`frontend/pages/review.vue`)

- [x] 2.1 In `frontend/pages/review.vue`, locate the review session completion branch rendered when `activeTab === 'session' && (!currentCard || reviewStore.cards.length === 0)`.
- [x] 2.2 Remove the duplicate `<MasteryGaugeCard>` and `<ReviewForecastChart>` components rendered at the bottom of the session completion state (preserving them exclusively in Tab 2 Deck Management).
- [x] 2.3 Refactor the completion container to be cleanly centered and well-spaced (`w-full max-w-2xl mx-auto space-y-6 text-center`), keeping the celebratory hero card, confetti trigger, and the two primary CTA buttons (`[ 📚 Khám phá kho thẻ (N thẻ) ]` and `[ ⚡ Luyện tập mở rộng ]`).

---

## 3. Normalize Advanced Filter Modal Button Styling (`frontend/components/review/AdvancedFilterModal.vue`)

- [x] 3.1 In `frontend/components/review/AdvancedFilterModal.vue`, remove all `<Check class="w-3.5 h-3.5" />` icon components from filter option buttons across all 4 sections (Knowledge Source, Mastery Stage, Due Urgency, Sort Options).
- [x] 3.2 Remove the unused `Check` import from `lucide-vue-next`.
- [x] 3.3 Replace disparate rainbow active color classes (`bg-sky-600`, `bg-amber-600`, `bg-rose-600`, `bg-purple-600`, `bg-emerald-600`, `bg-amber-500`) with a uniform brand active styling: `bg-brand-600 text-white font-bold border-transparent shadow-sm`.
- [x] 3.4 Add `whitespace-nowrap` to all filter option button class bindings to prevent label breaking and awkward multi-line wrapping.

---

## 4. Symmetric Bento Grid Alignment for Quiz Stats Tab (`frontend/pages/quiz.vue`)

- [x] 4.1 In `frontend/pages/quiz.vue`, update the 4-card Bento Grid Dashboard container under `activeTab === 'stats'` from `grid-cols-1 md:grid-cols-2 lg:grid-cols-12` to a balanced 2-column grid: `grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6`.
- [x] 4.2 Remove asymmetric column span classes (`lg:col-span-7`, `lg:col-span-5`, and `lg:col-span-6`) from all 4 Bento cards so that each card takes an equal 50% width on large screens (`lg:`).
- [x] 4.3 Verify that Row 1 renders Card 1 (Hero Performance Card) and Card 2 (Spaced Mastery Gauge Card), and Row 2 renders Card 3 (Seniority Matrix Card) and Card 4 (Topic Strengths & Weaknesses Radar Card), forming a continuous, 100% straight vertical dividing seam between Column 1 and Column 2.

---

## 5. Verification & Automated Tests

- [x] 5.1 Run frontend unit and component tests (`npm run test`) to ensure zero regressions in existing test suites.
- [x] 5.2 Verify responsive layout behavior across desktop ($\ge 1280\text{px}$) and mobile ($375\text{px}\text{--}390\text{px}$) viewports for all four modified surfaces.

---

## 6. Production Deployment & Live Verification via MCP Tools

- [ ] 6.1 Commit all planning and implementation changes and push to remote `main` branch: `git push origin main`.
- [ ] 6.2 Monitor GitHub Actions CI/CD workflow and wait 4–5 minutes for the multi-arch Docker image build, GHCR push, VPS container restart (`techdaily-frontend` and `techdaily-backend`), health checks, and nginx reload to complete.
- [ ] 6.3 Conduct live end-to-end verification on production `https://techdaily.duckdns.org` using MCP tools:
  - **Insights Page (`/insights`)**:
    - Verify prominent View Mode Switcher directly below Header Banner with `[ 🌐 Khám Phá ]` and `[ 🔖 Đã Lưu (N) ]`.
    - Verify independent category filtering in both Explore and Saved modes.
    - Test empty state in Saved mode with `BookmarkCheck` icon and "Khám Phá Kiến Thức Ngay" CTA button.
  - **Review Page (`/review`)**:
    - Verify session completion state renders cleanly centered celebratory hero card without duplicate Mastery Gauge or Review Forecast cards.
    - Open Advanced Filter Modal and verify all filter option buttons display uniform brand-600 active styling, no checkmarks, and zero text wrapping.
  - **Quiz Page (`/quiz`)**:
    - Select Stats tab and verify symmetric 2-column Bento Grid on desktop viewport with straight vertical alignment seam from top to bottom.
