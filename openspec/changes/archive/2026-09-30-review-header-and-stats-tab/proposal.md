# Proposal

## Why

The `/review` page currently has three user experience defects:
1. **Empty, Non-Standard Header**: The top tab bar sits on an isolated divider with an awkward empty void to its right, lacking the standardized page header archetype (Icon badge + Title + Subtitle on the left, Tab Switcher on the right) established by `/quiz` and `/insights`.
2. **Obscured Flashcard Inventory**: Six large bento stat cards (2 rows × 3 cards) currently monopolize the entire initial screen height of "Kho thẻ của tôi" (Deck Management), forcing users to scroll past half a screen of analytics before seeing any flashcards.
3. **Ambiguous Terminology Collision**: Both the "Thẻ Cần Chú Ý" (At-Risk) card and "Phân Bố Độ Khó" (Ease Factor) card use the identical Vietnamese label "Khó nhớ", confusing cards that fail repeatedly (Leech: $EF \le 1.70$) with low-ease concepts ($EF \in [1.30, 1.70]$).

## What Changes

- **Standardized Header Archetype**: Replace the bare tab switcher with the platform layout archetype matching `/quiz`:
  - **Left**: Icon badge (`<Layers>` in `bg-brand-500/10`), primary title (`$t('review.title')` / "Thẻ Ôn Tập"), and subtitle (`$t('review.subtitle')` / "Hệ Thống Lặp Lại Ngắt Quãng & Quản Lý Kho Thẻ").
  - **Right**: Tab buttons switcher enclosed in a refined rounded glass pill container.
  - **Layout**: Bound within `max-w-7xl mx-auto space-y-6 px-4 sm:px-6 py-6`, eliminating the container width mismatch between header and content.
- **Three-Tab Functional Separation**:
  1. **Tab 1: `session` ("Ôn tập hôm nay" / Review Session)**: Dedicated to active practice — 3D flip card player, SM-2 grading controls, companion telemetry dock, and celebratory completion state.
  2. **Tab 2: `management` ("Kho thẻ của tôi" / Deck Management)**: Dedicated to card browsing — search input (⌘K), quick filter chips, responsive flashcard bento card grid, and pagination controls rendered immediately at the top of the viewport.
  3. **Tab 3: `stats` ("Thống kê" / Analytics)**: Dedicated analytics dashboard hosting the two rows of 6 Bento cards:
     - Row 1: `FlashcardHeroCard` (with CTA to jump to Tab 1), `MasteryGaugeCard`, `ReviewForecastChart`.
     - Row 2: `AtRiskLeechCard` (with CTA to jump to Tab 2), `SourceChannelRetentionCard`, `EaseFactorDistributionCard`.
- **Disambiguated Retention Terminology**:
  - `review.atrisk_leech`: "Khó nhớ" $\rightarrow$ **"Hay quên"** (reflecting repeated lapses / Leech cards).
  - `review.ease_struggling`: "Khó nhớ" $\rightarrow$ **"Chưa vững"** (forming a coherent 3-tier progression: Chưa vững $\rightarrow$ Đang củng cố $\rightarrow$ Vững vàng).
  - Add `review.tab_stats`: **"Thống kê"** (vi) / **"Analytics"** (en).
- **Three-State URL Synchronization**: Expand URL query persistence and restoration to handle `?tab=session`, `?tab=management`, and `?tab=stats`.

## Capabilities

### Modified Capabilities
- `review`: Expand navigation to a three-tab architecture (`session`, `management`, `stats`), integrate the standard header banner with icon/title/subtitle, isolate the deck management view from analytics, and disambiguate Vietnamese memory retention terms.

## Impact

- **Frontend Code**: `frontend/pages/review.vue`, `frontend/i18n/locales/vi.json`, `frontend/i18n/locales/en.json`.
- **Frontend Tests**: `frontend/tests/pages/review.spec.ts` (updated to assert three tabs and correct tab switching).
- **Backend / Database**: Zero impact. No endpoints, DTOs, or database schemas are altered.
