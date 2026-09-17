# Proposal: Refine Insights, Review Session, Advanced Filter, and Quiz Bento UI

## Why

TechDaily aims to provide senior software engineers with an exceptionally fluid, visually balanced, and distraction-free learning ecosystem. Across the core interfaces—Senior Technical Insights (`/insights`), Spaced Repetition Review (`/review`), and Interview Quiz Mastery (`/quiz`)—user feedback and visual design audits have identified four distinct UI friction points and alignment anomalies:

1. **Buried Bookmark Filter & Mixed State Concerns on `/insights`:**
   - In `frontend/pages/insights.vue`, the "Đã Lưu" (Saved) filter is currently appended as the final chip in the horizontally scrollable category chips bar (`overflow-x-auto`). On standard desktop and mobile displays with 4+ categories, "Đã Lưu" is pushed outside the viewport and completely hidden unless the user happens to scroll horizontally.
   - Appending a global view mode ("All feed" vs. "My saved bookmarks") into a category taxonomy list ("Frontend", ".NET", "Postgres", "System Design") fundamentally conflates *view mode* with *category filter*.
   - Because "Đã Lưu" was implemented as a mutually exclusive category chip, users who selected "Đã Lưu" were unable to filter their saved bookmarks by topic (e.g. view only bookmarked ".NET" insights).
   - When users in "Đã Lưu" mode have no saved insights, they are presented with a generic, unhelpful empty state ("Chưa có kiến thức phù hợp" / "Tạo Với AI") that does not explain how to bookmark insights or provide an immediate pathway back to discovery.

2. **Card Redundancy in Review Session Completion State (`/review`):**
   - In `frontend/pages/review.vue`, when a user completes their daily review session (`activeTab === 'session' && (!currentCard || reviewStore.cards.length === 0)`), the page renders a celebratory hero card followed directly by two analytical cards: `MasteryGaugeCard` and `ReviewForecastChart`.
   - These exact two cards are already the centerpiece of Tab 2 ("Kho thẻ của tôi" / Deck Management). Duplicating them in the session completion screen creates visual redundancy, stretches the view vertically, and distracts from the core psychological milestone of finishing the daily deck.
   - The completion state should feature a focused, cleanly centered celebratory hero card with clear, intentional CTAs (`[ 📚 Khám phá kho thẻ (N thẻ) ]` and `[ ⚡ Luyện tập mở rộng ]`).

3. **Visual Inconsistency & Label Wrapping in Advanced Filter Modal (`AdvancedFilterModal.vue`):**
   - In `frontend/components/review/AdvancedFilterModal.vue`, every filter option button renders an embedded checkmark icon (`<Check class="w-3.5 h-3.5" />`) when active. This icon occupies 14px plus flex gap spacing, causing button labels (particularly longer Vietnamese diacritic strings like "Ghi chú đã lưu", "Đang xây dựng", "Thẻ ôn hôm nay") to wrap onto two or three lines, creating awkward button heights and layout distortion.
   - The modal uses a chaotic "rainbow" color scheme across sections (Sky for topics, Amber for highlights, Rose for quiz mistakes, Amber/Purple/Emerald for mastery stages, Rose for overdue). This clashes with TechDaily's clean brand palette.
   - The buttons lack `whitespace-nowrap`, leaving them vulnerable to irregular wrapping on compact screens.

4. **Asymmetric Grid Alignment on Quiz Stats Tab (`/quiz`):**
   - In `frontend/pages/quiz.vue`, the 4-card Bento Grid Dashboard on the Stats tab uses an asymmetric column span on large screens (`lg:`):
     - Row 1: Hero Performance Card (`lg:col-span-7`, ~58.3% width) + Spaced Mastery Gauge Card (`lg:col-span-5`, ~41.7% width).
     - Row 2: Seniority Matrix Card (`lg:col-span-6`, 50% width) + Topic Strengths & Weaknesses Radar Card (`lg:col-span-6`, 50% width).
   - This asymmetry creates an offset vertical divider: the vertical boundary between Card 1 and Card 2 does not align with the boundary between Card 3 and Card 4, producing an uneven visual zigzag.
   - Converting this layout into a symmetric 2-column grid (`grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6`) ensures all 4 Bento cards have equal 50% width on large screens, forming a 100% straight vertical alignment seam from top to bottom.

Resolving these issues elevates the aesthetic quality, usability, and visual rhythm of TechDaily across these three core surfaces.

---

## What Changes

We propose a targeted four-pillar UI refinement and a comprehensive deployment and live verification workflow:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                 REFINE INSIGHTS, REVIEW & QUIZ UI                           │
│                                                                             │
│  Pillar 1: Dedicated View Mode Switcher on /insights                        │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ Directly below Header Banner:                                         │  │
│  │ [ 🌐 Khám Phá ]  [ 🔖 Đã Lưu (N) ]                                    │  │
│  │ • Independent category bar below: works in BOTH Explore & Saved modes │  │
│  │ • Tailored Saved empty state: BookmarkCheck icon, guided copy & CTA   │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  Pillar 2: Deduplicate Cards in Review Completion State (/review)           │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • When activeTab === 'session' && reviewStore.cards.length === 0:     │  │
│  │ • Remove duplicate MasteryGaugeCard & ReviewForecastChart             │  │
│  │ • Keep cleanly centered celebratory Hero Card with dual CTAs          │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  Pillar 3: Normalize Advanced Filter Modal Buttons (AdvancedFilterModal)    │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Remove all <Check> icons to eliminate extra width & line wrapping   │  │
│  │ • Replace rainbow colors with uniform brand-600 active styling        │  │
│  │ • Add whitespace-nowrap across all option button labels               │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  Pillar 4: Symmetric Bento Grid Alignment for Quiz Stats (/quiz)           │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Refactor grid: grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6            │  │
│  │ • All 4 cards have equal 50% width on lg screens                      │  │
│  │ • Row 1: Hero Performance (col 1) + Spaced Mastery Gauge (col 2)      │  │
│  │ • Row 2: Seniority Matrix (col 1) + Topic Strengths Radar (col 2)     │  │
│  │ • 100% straight vertical dividing seam from top to bottom             │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  Pillar 5: Production Deployment & Live MCP Verification                    │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Commit & push to origin main                                        │  │
│  │ • Wait 4-5 mins for GitHub Actions CI/CD to build, push & deploy      │  │
│  │ • Verify live behavior on https://techdaily.duckdns.org via MCP tools │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Pillar 1: Dedicated View Mode Switcher on `/insights` (`frontend/pages/insights.vue`)
- **Prominent View Mode Switcher**:
  - Located directly beneath the Header Banner and above the Category Filter Bar.
  - Implemented as a segmented pill switch:
    - Option 1: `[ 🌐 Khám Phá ]` (`$t('insights.view_explore')`) for browsing general or category-specific insight cards.
    - Option 2: `[ 🔖 Đã Lưu (N) ]` (`$t('insights.view_saved')`) displaying the total count of bookmarked cards.
- **Independent Category Filter Bar**:
  - Remove the "Đã Lưu" button from the horizontal category chip row. The category chip row now contains exclusively topic filters: `[ Tất Cả Chủ Đề ]`, `[ Frontend & Vue ]`, `[ .NET & C# ]`, `[ Postgres & DB ]`, `[ Thiết Kế Hệ Thống ]`.
  - In Explore mode, selecting a category filters the general feed by that category.
  - In Saved mode, selecting a category filters the user's bookmarked insights by that category.
- **Tailored Saved Empty State**:
  - When in Saved mode and no bookmarks match the filter:
    - Icon: `BookmarkCheck` in an indigo badge container.
    - Title: "Bạn chưa lưu mẫu kiến thức nào" (`insights.saved_empty_title`).
    - Description: "Hãy bấm biểu tượng Bookmark trên các thẻ kiến thức khi khám phá để lưu lại xem sau." (`insights.saved_empty_desc`).
    - CTA Button: `[ 🌐 Khám Phá Kiến Thức Ngay ]` (`insights.saved_empty_cta`) which switches `viewMode` back to `'explore'`.

### Pillar 2: Deduplicate Cards in Review Session Completion State (`frontend/pages/review.vue`)
- When `activeTab === 'session' && (!currentCard || reviewStore.cards.length === 0)`:
  - Remove the duplicate `<MasteryGaugeCard>` and `<ReviewForecastChart>` components from the bottom of the session container.
  - Center and expand the Celebratory Completion Hero Card with proper vertical spacing and padding.
  - Retain the two prominent CTA buttons:
    - Primary CTA: `[ 📚 Khám phá kho thẻ (N thẻ) ]` (`review.browse_deck_btn`), seamlessly switching to Tab 2 (`activeTab = 'management'`).
    - Secondary CTA: `[ ⚡ Luyện tập mở rộng ]` (`review.cram_practice_btn`), routing to `/today` for daily drills and reading challenges.

### Pillar 3: Normalize Advanced Filter Modal Button Styling (`frontend/components/review/AdvancedFilterModal.vue`)
- **Remove Checkmark Icons**:
  - Eliminate all `<Check class="w-3.5 h-3.5" />` elements from filter option buttons across all sections: Knowledge Source, Mastery Stage, Due Urgency, and Sort Options.
- **Normalize Active State Styling**:
  - Replace the disparate rainbow color palette (`bg-sky-600`, `bg-amber-600`, `bg-rose-600`, `bg-purple-600`, `bg-emerald-600`, `bg-amber-500`) with a single, uniform brand active class:
    `bg-brand-600 text-white font-bold border-transparent shadow-sm`.
- **Enforce Single-Line Labels**:
  - Add `whitespace-nowrap` to all filter option buttons to guarantee that button labels never wrap into multiple lines on narrow or localized viewports.

### Pillar 4: Symmetric Bento Grid Alignment on Quiz Stats Tab (`frontend/pages/quiz.vue`)
- **Symmetric 2-Column Grid**:
  - Replace `grid grid-cols-1 md:grid-cols-2 lg:grid-cols-12 gap-4 sm:gap-6` with `grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6`.
  - Remove `lg:col-span-7`, `lg:col-span-5`, and `lg:col-span-6` from the cards.
- **Balanced Row Layout**:
  - Row 1: Hero Performance Card (Col 1, 50% width) + Spaced Mastery Gauge Card (Col 2, 50% width).
  - Row 2: Seniority Matrix Card (Col 1, 50% width) + Topic Strengths & Weaknesses Radar Card (Col 2, 50% width).
  - The vertical dividing line between Column 1 and Column 2 aligns 100% straight from top to bottom across both rows on `lg:` viewports.

### Pillar 5: Production Deployment & Live Verification
- Commit and push changes to remote repository `main` branch.
- Wait 4–5 minutes for GitHub Actions CI/CD to build multi-arch Docker images, push to GHCR, deploy to VPS, restart containers, and reload nginx.
- Perform live verification on `https://techdaily.duckdns.org` via MCP tools.

---

## Capabilities

### Modified Capabilities
- `insights`: Extracts "Đã Lưu" from the horizontal category chip row into a prominent, dedicated View Mode Switcher (`[ 🌐 Khám Phá ]` / `[ 🔖 Đã Lưu (N) ]`), enables independent category filtering in both modes, and adds a tailored empty state for Saved mode with `BookmarkCheck` and a discovery CTA.
- `review`: Deduplicates analytical cards in the review session completion state by removing `MasteryGaugeCard` and `ReviewForecastChart` (which remain in Tab 2 Deck Management) and keeps a centered celebratory hero card with dual CTAs. Normalizes `AdvancedFilterModal.vue` option buttons by removing checkmark icons, standardizing on brand-600 active styling, and adding `whitespace-nowrap`.
- `quiz`: Re-architects the Stats tab Bento Grid into a balanced, symmetric 2-column grid (`grid-cols-1 lg:grid-cols-2`), ensuring all 4 cards have equal 50% width on large screens and a continuous, straight vertical dividing seam.

---

## Impact

- **Frontend Pages & Components:**
  - `frontend/pages/insights.vue`: Add view mode state (`viewMode`), render dedicated View Mode Switcher, decouple category filters, and render tailored Saved empty state.
  - `frontend/pages/review.vue`: Remove embedded `MasteryGaugeCard` and `ReviewForecastChart` from session completion state; center the celebratory hero card.
  - `frontend/components/review/AdvancedFilterModal.vue`: Remove `<Check>` icons, replace rainbow active states with `bg-brand-600 text-white font-bold border-transparent shadow-sm`, add `whitespace-nowrap`.
  - `frontend/pages/quiz.vue`: Update grid container to `grid-cols-1 lg:grid-cols-2`, remove asymmetric col-spans.
- **Localization (`frontend/i18n/locales/`):**
  - `en.json` and `vi.json`: Add keys for `insights.view_explore`, `insights.view_saved`, `insights.saved_empty_title`, `insights.saved_empty_desc`, and `insights.saved_empty_cta`.
- **Backend & Database:**
  - Zero database schema migrations required.
  - Zero backend API endpoint changes; existing `GET /api/v1/insights/feed`, `GET /api/v1/review/cards`, and `GET /api/v1/quiz/stats` contracts remain fully compatible.

---

## Verification / Testing & Quality Assurance

- **Local Verification:**
  - Verify template rendering, reactivity, and responsive layout across desktop ($\ge 1280\text{px}$) and mobile ($375\text{px}\text{--}390\text{px}$).
  - Run frontend test suite (`npm run test`) to ensure zero regressions in component tests.
- **Production Deployment & Live Verification via MCP Tools:**
  - Deploy to production VPS via CI/CD push to `main`.
  - Perform live verification using browser tools on `https://techdaily.duckdns.org`:
    1. Navigate to `/insights`: verify View Mode Switcher, switch to "Đã Lưu", filter by category, verify empty state with zero bookmarks.
    2. Navigate to `/review`: verify session completion state contains only the celebratory hero card without duplicate gauge or forecast cards; open Advanced Filter Modal and verify uniform brand styling without checkmarks and without text wrapping.
    3. Navigate to `/quiz`: switch to Stats tab, verify symmetric 2-column Bento grid with straight vertical dividing line on desktop viewports.
