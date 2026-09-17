# Design: Bento Review and Profile Dashboards

## Context

TechDaily's `/review` and `/profile` interfaces currently employ rigid layouts that cause layout wrapping and aesthetic degradation, especially when rendering Vietnamese technical content (which is 30-40% longer and diacritic-heavy compared to English). Specifically:
- In `/review`, flashcards are displayed in a table-like list row that vertically stretches and wraps Front prompts and Back explanations awkwardly. The four summary statistics (Total, Learning, Reviewing, Mastered) are flat, dark boxes that lack motivational hierarchy or actionable triggers.
- In `/profile`, personal info, security, and metrics are displayed in a narrow, stacked single-column layout that underutilizes desktop screens and fails to highlight the user's engineering milestones or curriculum progress.

This design introduces modern, high-density E-Learning Bento Dashboard architectures for both views, breaking the monoliths into isolated, modular Vue components with pure CSS/SVG visualizations, robust two-tier filtering, and responsive grid layouts.

## Goals / Non-Goals

**Goals:**
- Deliver a modern EdTech Bento dashboard on `/review` featuring a Hero Action Card, semi-circular SVG Mastery Gauge, and 7-Day Review Forecast bar chart.
- Replace rigid tables with a responsive 2-3 column Knowledge Card Grid with interactive smooth-accordion answer toggles and code syntax highlighting.
- Solve text wrapping and overflow issues for Vietnamese by implementing a Two-Tier search system: a compact quick search bar (`⌘K` + quick chips) and an Advanced Filter Modal dialog.
- Restructure `/profile` into an asymmetric 2-column engineer portfolio dashboard (Desktop 2/3 - 1/3, stacking on mobile) featuring personal settings, identity/milestones widgets, and a curriculum domain mastery goal tracker.
- Achieve 100% i18n localization parity across English and Vietnamese with zero untranslated strings.
- Maintain zero breaking changes to existing backend API contracts and database schemas.

**Non-Goals:**
- Introducing heavy client-side charting libraries (such as Chart.js, ECharts, or D3). All gauges and forecast bars must be built with lightweight, accessible SVG and Tailwind CSS.
- Modifying the core SM-2 interval or ease factor algorithm in the backend.
- Re-introducing notification scheduling or timezone preferences to `/profile` (those strictly belong to `/settings` per the Core Platform spec).

---

## Decisions

### 1. Component Architecture & Modularization

To ensure separation of concerns and maintainability, the new dashboard interfaces are decomposed into isolated components:

```
frontend/
├── components/
│   ├── review/
│   │   ├── FlashcardHeroCard.vue        # Hero action card with due counter & 1-click CTA
│   │   ├── MasteryGaugeCard.vue         # Semi-circular SVG mastery radial gauge
│   │   ├── ReviewForecastChart.vue      # 7-day CSS/SVG mini bar chart forecast
│   │   ├── AdvancedFilterModal.vue      # Clean multi-section filter popover/sheet
│   │   └── FlashcardBentoCard.vue       # 2-3 column grid card with accordion markdown toggle
│   └── profile/
│       ├── EngineerProfileHero.vue      # Right-column identity widget & stacked milestones
│       └── DomainGoalTracker.vue        # Curriculum domain progress tracker (.NET, Postgres, etc.)
└── pages/
    ├── review.vue                       # Assembles review bento overview, quick bar & card grid
    └── profile.vue                      # Assembles asymmetric 2-column portfolio dashboard
```

#### Detailed Component Specifications:

1. **`FlashcardHeroCard.vue`**:
   - Displays the number of cards due today (`reviewStore.totalCardsDue`).
   - Calculates estimated study time dynamically: $\approx \max(1, \lceil \text{dueCards} \times 0.5 \rceil)$ minutes.
   - Houses the primary action button (`1-click CTA`) to instantly transition to the interactive 3D flip card review session (`activeTab = 'session'`).
   - Styled with subtle gradient glow (`bg-gradient-to-br from-brand-600 to-indigo-700 text-white`).

2. **`MasteryGaugeCard.vue`**:
   - Calculates Mastery Rate:
     $$\text{MasteryRate} = \begin{cases} \frac{\text{MasteredCount}}{\text{TotalCards}} \times 100\% & \text{if } \text{TotalCards} > 0 \\ 0\% & \text{otherwise} \end{cases}$$
   - Renders a lightweight SVG semi-circle gauge ($180^\circ$ arc) using `stroke-dasharray` and `stroke-dashoffset`.
   - Displays a dynamic proficiency level badge:
     - $0\% - 25\%$: "Đang khởi động" / "Getting Started" (slate badge)
     - $26\% - 50\%$: "Đang xây dựng phản xạ" / "Building Reflexes" (amber badge)
     - $51\% - 75\%$: "Tiến bộ vững chắc" / "Solid Progress" (blue badge)
     - $76\% - 100\%$: "Trí nhớ xuất sắc" / "Mastery Excellence" (emerald badge)

3. **`ReviewForecastChart.vue`**:
   - Visualizes cards scheduled for review over the next 7 days ($T_0$ to $T_{+6}$, Mon–Sun).
   - Generates normalized bar heights relative to the peak day in the window.
   - Highlights today ($T_0$) with a distinct active indicator, providing tooltips on hover with exact card counts.

4. **`AdvancedFilterModal.vue`**:
   - Triggered by `[ ⚙️ Advanced Filter ]` button in the quick bar.
   - Multi-section layout:
     - **Knowledge Source**: `All`, `Curriculum Topic` (0), `Reading Highlight` (1), `Quiz Mistake` (2).
     - **Mastery Stage**: `All`, `Learning` (0), `Reviewing` (1), `Mastered` (2).
     - **Due Date Urgency**: `All`, `Due Today` ($\le \text{Today}$), `Overdue`, `Upcoming (7 Days)`.
     - **Sort Options**: `Next Review Date` (asc/desc), `Ease Factor / Difficulty`, `Recently Created`.
   - Actions: `Reset to Default` and `Apply Filters`.
   - Modal design prevents text wrapping issues common in horizontal chip strips when displaying Vietnamese text.

5. **`FlashcardBentoCard.vue`**:
   - Individual card rendered inside a `grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4` container.
   - Top row: Source type badge (e.g. `Reading Highlight`, `Quiz Mistake`), Status badge (`Learning`, `Reviewing`, `Mastered`), and Due indicator.
   - Middle body: Front prompt / question prominently rendered.
   - Accordion section: "Xem đáp án" ("Show Answer") toggle button with smooth height/opacity transition expanding the Back markdown answer with syntax-highlighted code.
   - Footer: Pill badges for SM-2 parameters ($Repetitions$, $Interval$, $Ease Factor$, $Next Review Date$) and action icons (Edit markdown modal, Reset progress confirm, Delete confirm).

6. **`EngineerProfileHero.vue`**:
   - Occupies the right column (1/3 width on desktop).
   - Identity Header: Large avatar (fallback to gradient monogram with user initial), display name, role badge, account type badge (Google account / Email account).
   - Stacked Milestones:
     - **Streak Card**: Current streak with animated fire icon, longest streak, and freeze credits remaining.
     - **Drill Counter**: Total drills completed.
     - **Quiz Accuracy**: Percentage accuracy from `useInterviewQuizStore`.

7. **`DomainGoalTracker.vue`**:
   - Positioned in the left column beneath personal settings.
   - Tracks mastery progression across TechDaily's 4 core technical curriculum categories:
     - `.NET Backend` (`Category.BackendDotNet`)
     - `PostgreSQL & Storage` (`Category.DatabaseStorage`)
     - `System Design & Architecture` (`Category.SystemDesign`)
     - `Frontend Web` (`Category.FrontendWeb`)
   - Visualizes percentage completion bars with distinct theme accents (emerald, violet, amber, sky).

---

### 2. State Management & API Integration

- **`useReviewStore` Integration**:
  - The deck overview consumes `deckStatistics` (`totalCards`, `learningCount`, `reviewingCount`, `masteredCount`) and `totalCardsDue`.
  - The 7-day forecast aggregates `nextReviewDate` across retrieved cards, computing daily bins client-side while preserving existing pagination endpoints.
  - Advanced filters map cleanly to `fetchDeckCards({ search, status, sourceType, page, pageSize })`.
- **`useProfileStore` & `useInterviewQuizStore` Integration**:
  - `fetchProfile()` provides `user` (name, email, role, daily goal minutes, account type) and `stats` (streaks, drills completed, freeze credits).
  - `fetchStats()` from `useInterviewQuizStore` provides overall `accuracyRate` and `topicBreakdown` used to feed domain mastery calculations.

---

### 3. Responsive Layout & Spacing Tokens

| Viewport | `/review` Layout | `/profile` Layout |
| :--- | :--- | :--- |
| **Mobile (< 768px)** | 1-column stacked overview; 1-column card grid; full-screen filter sheet | 1-column vertical stack (Identity widget on top, Settings below, Domain tracker at bottom) |
| **Tablet (768px - 1024px)** | 2-column bento overview; 2-column card grid; modal popover | 1-column stacked layout with 2-column metric cards |
| **Desktop (>= 1024px)** | 3-column asymmetric overview (Hero 50%, Gauge 25%, Forecast 25%); 3-column card grid | Asymmetric 2-column layout: Left Column (2/3 width), Right Column (1/3 width) |

### 4. Styling & Dark Mode Palette Tokens

Adheres strictly to TechDaily's existing design system:
- **Backgrounds**:
  - Light mode: `bg-slate-50`, cards `bg-white`, accents `bg-slate-100`.
  - Dark mode: `bg-slate-950`, cards `bg-slate-900`, accents `bg-slate-800`.
- **Borders**:
  - Light: `border-slate-200/80`
  - Dark: `border-slate-800`
- **Typography & Brand Accents**:
  - Primary text: `text-slate-900 dark:text-white`
  - Muted text: `text-slate-500 dark:text-slate-400`
  - Brand Primary: `brand-600` (`#2563eb`), hover `brand-500`
  - Accent Gradients: `from-brand-600 to-indigo-700` (Hero Action Card)

---

### 5. Internationalization (i18n) Key Additions

New translation keys added to both `en.json` and `vi.json`:
- `review.hero_title`: "Ready to Review?" / "Sẵn sàng ôn tập?"
- `review.hero_desc`: "{count} cards due today • ~{minutes} min study session" / "{count} thẻ cần ôn hôm nay • ~{minutes} phút ôn tập"
- `review.start_review_btn`: "Start Review Session" / "Bắt đầu ôn tập ngay"
- `review.mastery_rate`: "Deck Mastery" / "Tỷ lệ thành thạo"
- `review.tier_starting`: "Getting Started" / "Đang khởi động"
- `review.tier_building`: "Building Reflexes" / "Đang xây dựng phản xạ"
- `review.tier_solid`: "Solid Progress" / "Tiến bộ vững chắc"
- `review.tier_mastered`: "Mastery Excellence" / "Trí nhớ xuất sắc"
- `review.forecast_title`: "7-Day Forecast" / "Dự báo 7 ngày tới"
- `review.quick_filter_all`: "All Cards" / "Tất cả thẻ"
- `review.quick_filter_due`: "Due Today" / "Cần ôn hôm nay"
- `review.quick_filter_mastered`: "Mastered" / "Đã thành thạo"
- `review.advanced_filter_btn`: "Advanced Filter" / "Bộ lọc nâng cao"
- `review.filter_modal_title`: "Filter & Sort Deck" / "Bộ lọc & Sắp xếp kho thẻ"
- `review.filter_urgency`: "Urgency" / "Mức độ ưu tiên"
- `review.filter_sort`: "Sort By" / "Sắp xếp theo"
- `profile.domain_mastery`: "Curriculum Domain Coverage" / "Độ phủ kiến thức chuyên môn"
- `profile.domain_dotnet`: ".NET & C# Architecture" / "Kiến trúc .NET & C#"
- `profile.domain_postgres`: "PostgreSQL & Database Systems" / "Hệ cơ sở dữ liệu PostgreSQL"
- `profile.domain_system_design`: "Distributed System Design" / "Thiết kế hệ thống phân tán"
- `profile.domain_frontend`: "Frontend Architecture & Web" / "Kiến trúc Frontend & Web"

---

## Risks / Trade-offs

- **[Risk] Card Height Jitter in Multi-Column Grid on Accordion Expand**: Expanding a card's answer could alter the height of sibling items if layout containers are misconfigured.
  - *Mitigation:* Apply `items-start` to the grid container and encapsulate card expansion inside an isolated CSS transition (`transition-all duration-200 ease-in-out`), preventing layout reflow across adjacent columns.
- **[Risk] Vietnamese Diacritic Text Truncation in Compact Gauges/Charts**: Long localized labels in the gauge or forecast might clip on narrow screens.
  - *Mitigation:* Use responsive font scaling (`text-xs sm:text-sm`) with tooltip triggers on hover/tap, ensuring full accessibility without text overflow.
- **[Risk] Forecast Distribution when Cards Span Long Intervals**: If a user has few cards due in the next 7 days, the bar chart could render empty columns.
  - *Mitigation:* Render empty placeholder tick marks with subtle dashed outlines and a celebratory "0 cards due" indicator for free days.

---

## Verification Strategy

To guarantee responsive layout fidelity and linguistic parity across all target viewports without creating disposable test code or flaky throwaway scripts, verification relies strictly on live end-to-end browser inspection using MCP browser tools.

### MCP Browser Verification Protocol

1. **Direct Live Browser Inspection (No Throwaway Scripts)**:
   - Connect directly to the active application runtime using MCP browser tools (`browser.open`, `tab.observe`, `tab.run`, `tab.screenshot`).
   - Exercise live user journeys directly via real DOM interactions and visual assertions, completely eschewing disposable test scripts or mock harness code.

2. **Viewport Switching Matrix**:
   - **Desktop Large Screen ($1280 \times 800$ or $1440 \times 900$)**:
     - `/review`: Verify 3-column asymmetric Bento Overview (Hero Action Card 50%, Mastery Gauge 25%, 7-Day Forecast 25%) and 3-column Knowledge Card Grid (`grid-cols-1 md:grid-cols-2 xl:grid-cols-3`).
     - `/profile`: Verify asymmetric 2-column layout (Settings & Goals left column occupying 2/3 width, Identity & Milestones right column occupying 1/3 width).
   - **Mobile Screen ($390 \times 844$, standard ~375px - 390px mobile viewport)**:
     - `/review`: Verify single-column stacked overview cards and single-column card grid with touch-friendly hit areas ($\ge 44\text{px}$).
     - `/review`: Verify that the Advanced Filter dialog transitions into a mobile-friendly full-screen/bottom sheet modal with clean touch targets.
     - `/profile`: Verify single-column vertical stacking with the Identity widget placed at the top, followed by settings tabs and the domain goal tracker.
     - Verify zero horizontal scrolling ($overflow-x = hidden$) and no clipped container boundaries across both pages.

3. **Bilingual Locale Switching Protocol**:
   - For each target viewport (Desktop and Mobile), inspect the interface under both English (`en`) and Vietnamese (`vi`) locales.
   - Toggle languages dynamically using the top navigation language switcher.
   - Verify that Vietnamese technical vocabulary and diacritic-heavy labels (which average 30–40% longer than English counterparts) wrap smoothly without causing text truncation, pill badge clipping, or broken grid containers.

4. **Component-by-Component Inspection Checklist**:
   - **Hero Action Card (`/review`)**:
     - Cards due today counter and dynamic study duration estimation are clearly legible.
     - 1-click CTA button successfully triggers transition to active review session mode (`activeTab = 'session'`).
   - **Mastery Gauge Card (`/review`)**:
     - Semi-circular SVG radial arc renders smoothly across 0%, 50%, and 100% states without clipping.
     - Motivational tier badge displays appropriate color tokens and accurate localized text.
   - **7-Day Review Forecast (`/review`)**:
     - Relative bar heights scale accurately against peak day volumes.
     - Day labels (Mon–Sun / T2–CN) display without overlap, and empty day indicators render cleanly.
   - **Quick Search & Advanced Filter Modal (`/review`)**:
     - Quick search input gains focus upon `⌘K` / `Ctrl+K` keypress.
     - `[ ⚙️ Advanced Filter ]` trigger button opens the modal popover, and active filter badge updates dynamically.
     - Filter sections (Source Type, Mastery Stage, Urgency, Sort Options) toggle properly, and Reset clears all active criteria.
   - **Bento Knowledge Cards (`/review`)**:
     - Prominently styled Front prompt, source badge, and mastery status badge.
     - Accordion toggle ("Xem đáp án" / "Show Answer") smoothly expands the Back markdown answer without column height reflow or adjacent card jitter.
     - Embedded code blocks maintain syntax highlighting and bounded horizontal scrolling.
     - SM-2 metrics footer (Repetitions, Interval, Ease Factor, Next Review Date) and action buttons (Edit, Reset, Delete) remain intact.
   - **Engineer Identity Hero Card (`/profile`)**:
     - Large avatar monogram/image, display name, target role badge, and account connection badge render without overflow.
     - Stacked milestone metrics (active streak with fire icon, drills completed, quiz accuracy) display crisp typography.
   - **Domain Goal Tracker (`/profile`)**:
     - Visual progress bars for the 4 curriculum domains (.NET, PostgreSQL, System Design, Frontend) render with respective accent colors and accurate percentage widths.
     - Vietnamese domain titles render without badge clipping or percentage alignment distortion.
