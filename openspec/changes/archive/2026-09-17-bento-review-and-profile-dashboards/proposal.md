# Proposal: Bento Review and Profile Dashboards

## Why

TechDaily's `/review` and `/profile` views suffer from rigid layouts that degrade engineer experience:
1. Rigid Tables & Text Cramping: Vietnamese text is 30-40% longer and diacritic-heavy. The current table-like row layout in `/review` cramps Front prompts and Back explanations, causing awkward vertical line-wrapping and poor readability.
2. Generic Stat Boxes: The row of four flat counter boxes displays raw numbers without motivational progression, visual hierarchy, or 1-click study actions.
3. Filter Clutter: Linear filter chips wrap awkwardly across smaller viewports as options increase.
4. Linear Profile Page: The `/profile` view lacks visual identity, milestone tracking, and domain mastery tracking.

Modern E-Learning Bento Dashboards resolve this with responsive multi-column knowledge cards, a dynamic mastery gauge, 7-day review forecast, two-tier modal filtering, and an asymmetric engineer portfolio.

## What Changes

This proposal modernizes TechDaily's spaced repetition review hub (`/review`) and engineer portfolio (`/profile`) with high-density, responsive Bento dashboard architectures:

### 1. Spaced Repetition & Flashcard Hub (`/review`)
- **Bento Overview**: Replaces the four plain stat boxes with an asymmetric overview:
  - **Hero Action Card**: Displays the count of cards due today, estimated review time (~5 min), and a primary 1-click CTA button to launch the 3D interactive review session.
  - **Mastery Gauge**: Semi-circular radial SVG gauge displaying Mastery Rate = $(Mastered / Total) \times 100\%$ alongside motivational tier badges (e.g. "Trí nhớ xuất sắc" / "Đang xây dựng phản xạ").
  - **7-Day Review Forecast**: Mini bar chart illustrating projected cards due across the upcoming 7 days (Monday to Sunday).
- **Two-Tier Search & Advanced Filter Sheet**:
  - **Quick Bar**: Search input with keyboard shortcut (`⌘K` / `Ctrl+K`), core quick chips (`All`, `Due Today`, `Mastered`), and an `[ ⚙️ Advanced Filter ]` button with an active filter badge.
  - **Advanced Filter Modal**: Multi-section popover/sheet for Knowledge Source (`Topic`, `Highlight`, `QuizMistake`), Mastery Stage (`Learning`, `Reviewing`, `Mastered`), Due Date Urgency, and Sorting (`Next review date`, `Difficulty Ease Factor`, `Recently created`), with Reset and Apply actions, completely immune to text-wrapping bugs.
- **Knowledge Card Grid (Bento Cards)**:
  - Replaces the cramped table with a 2-3 column responsive bento card grid.
  - Prominent Front question prompt, source badge, and status badge.
  - Interactive accordion toggle ("Xem đáp án" / "Show Answer") smoothly expanding the Back markdown answer with code syntax highlighting.
  - Footer displaying SM-2 metrics (Repetitions, Interval Days, Ease Factor, Next Review Date) and action buttons (Edit modal, Reset progression confirmation, Soft Delete confirmation).

### 2. Engineer Portfolio & Learning Dashboard (`/profile`)
- **Asymmetric 2-Column Layout**: Desktop 2/3 - 1/3 layout, stacking seamlessly on mobile viewports:
  - **Left Column (Settings & Goals)**: Personal Info tab (Name, Target Role selector, interactive Daily Goal Pace chips 5m/10m/15m/30m) and Security tab (Current password, New password with dynamic strength bar, Confirm password).
  - **Right Column (Identity & Milestones Widget)**: Large avatar with status indicator, display name, target role badge, account type (Google linked / Standard email). Stacked milestone card: Active Streak with fire icon, longest streak, freeze credits remaining; Total Drills completed; Quiz Accuracy rate.
  - **Domain Mastery Progress Bar (Goal Tracker)**: Visual progress bars tracking curriculum domain coverage (.NET, PostgreSQL, System Design, Frontend) with learning completion metrics.

## Capabilities

### Modified Capabilities
- `review`: Replaces the legacy table and flat stat boxes with a Bento Overview (Hero Action Card, semi-circular Mastery Gauge, 7-day Review Forecast), a Two-Tier Search with Advanced Filter Modal, and a responsive Knowledge Card Grid.
- `core-platform`: Restructures the user profile interface into an asymmetric 2-column engineer portfolio dashboard featuring personal settings, identity/milestones bento widgets, and curriculum domain mastery progress bars.

## Impact

- **Frontend Components**:
  - Introduces reusable Bento components: `FlashcardHeroCard.vue`, `MasteryGaugeCard.vue`, `ReviewForecastChart.vue`, `AdvancedFilterModal.vue`, `FlashcardBentoCard.vue`, `EngineerProfileHero.vue`, and `DomainGoalTracker.vue`.
  - Refactors `frontend/pages/review.vue` and `frontend/pages/profile.vue` to adopt CSS Grid Bento layouts.
- **State & Composables**:
  - Leverages existing `useReviewStore`, `useProfileStore`, and `useInterviewQuizStore` without requiring any breaking API contract modifications.
- **Internationalization (i18n)**:
  - Adds localized strings in `en.json` and `vi.json` for mastery gauge tiers, forecast labels, filter modal categories, and domain goal tracking.
- **Database & Backend APIs**:
  - Zero database schema migrations; fully compatible with existing EF Core query filters and SM-2 endpoints.

## Verification / Testing & Quality Assurance

- **End-to-End Visual & Interaction Verification via MCP Browser Tools**:
  - Direct live browser interaction using MCP browser tools (no throwaway scripts or disposable mock runners).
  - Target routes: Both `/review` (Bento Flashcard Deck Dashboard) and `/profile` (Engineer Portfolio Dashboard).
  - Viewport coverage:
    - Desktop large screen ($\ge 1280\text{px}$, e.g. $1280 \times 800$ or $1440 \times 900$).
    - Mobile screen ($\approx 375\text{px} - 390\text{px}$, e.g. $390 \times 844$).
  - Bilingual coverage:
    - English (`en`) and Vietnamese (`vi`) toggled via application locale switcher.
  - Acceptance checks:
    - Zero text overflow and clean line wrapping for longer Vietnamese technical labels and diacritics.
    - No badge clipping or truncated pill counters across all cards and widgets.
    - Smooth, jitter-free accordion expansion on Bento knowledge cards without shifting adjacent columns.
    - Seamless responsive Bento stacking from multi-column grids down to single-column mobile viewports.
