# Design

## Context

See `proposal.md` - Why.

The TechDaily home page (`frontend/pages/index.vue`) hosts `HomeBentoDashboard.vue`. While the page uses `BentoDashboardLayout.vue`, the internal widgets were assembled incrementally, resulting in a hybrid visual hierarchy. Specifically, the Telemetry Dock still depends on `ConcentricMetricCard.vue` (which renders legacy circular SVG rings) and `DomainConstellationCard.vue` (which uses outdated nested padding and styling).

In contrast, `/showcase` (`LayoutArchetypesShowcase.vue` Demo 4) demonstrates the canonical Bento Dashboard archetype: clean glass cards with unified border radius (`rounded-2xl`), uppercase micro-labels (`text-xs font-bold uppercase tracking-wider`), hairline progress bars (`h-2 rounded-full`), and an asymmetric 2:1 column split that perfectly balances vertical heights on 1080p desktop viewports.

## Goals / Non-Goals

**Goals:**
- Rebuild `HomeBentoDashboard.vue` from a clean slate directly modeled on `/showcase` Demo 4.
- Implement an Executive Orientation Banner in `#header` with target role, active practice title, and high-visibility 1-click CTA button (`[ Bắt Đầu Học Ngay → ]` / `Start Daily Focus`).
- Structure the Action Stage into Tier 1 (Hero Reading Card) and Tier 2 (Split Subgrid with Micro-Drill and Senior Dilemma).
- Replace legacy circular SVG widgets in Telemetry Dock with 2 clean, high-contrast Bento cards (Streak Consistency Card and Knowledge Constellation Card).
- Inline telemetry card markup directly into `HomeBentoDashboard.vue` to eliminate component fragmentation and prop-drilling overhead.
- Ensure strict responsive scaling: 2:1 grid on desktop ($\ge 1024\text{px}$) and 1-column stack on mobile ($< 640\text{px}$).

**Non-Goals:**
- Altering backend API contracts (`GET /api/v1/daily/today`, `GET /api/v1/review/deck`) or Pinia store state management (`useDailyFocusStore`, `useReviewStore`, `useKnowledgeGraphStore`).
- Modifying other layout archetypes (`StudioLayout`, `MasterDetailLayout`, `BoardLayout`).

## Decisions

### 1. Orientation Banner Header Architecture (`#header`)
- Placed in `#header` slot of `BentoDashboardLayout`:
  - Top row: Executive Cockpit badge (`bg-brand-500/15 text-brand-600 dark:text-brand-300`) alongside user target role (`targetRole`) and active slice count (`sliceBadgeText`).
  - Title & Subtitle: High-contrast typography with dynamic daily practice headline (`Luyện Tập Kiến Trúc & Hệ Thống Phân Tán Hằng Ngày` / `Senior Architecture & System Design Practice`) and concise motivation subtitle.
  - Primary 1-Click CTA Launchpad: High-contrast button (`bg-brand-600 hover:bg-brand-500 text-white font-bold rounded-xl shadow-md shadow-brand-500/20 active:scale-95`) that invokes `handleStartReading` or `handleStartScenario`.

### 2. Action Stage Two-Tier Structure (`#action-stage`)
- **Tier 1 (Hero Active Reading Card)**:
  - Header: Micro-label `text-[11px] font-mono font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400` with source book title, slice progress, and duration pill (`⏱ X phút đọc`).
  - Body: Slice chapter title (`line-clamp-1 text-base sm:text-lg font-bold`) and summary excerpt (`line-clamp-2 text-xs sm:text-sm`).
  - Progress bar: Gradient hairline fill indicating book chunk completion.
  - Footer: Keyboard hint (`Nhấn Enter để bắt đầu đọc`) and direct `[ Đọc Tiếp → ]` button.
- **Tier 2 (Split Practice Subgrid)**:
  - Rendered as `grid grid-cols-1 sm:grid-cols-2 gap-3.5 sm:gap-4 flex-1`.
  - **Left (Daily Micro-Drill)**: Micro-label `TRẮC NGHIỆM PHẢN XẠ`, status pill (`+10 Điểm`, `Hoàn thành: X/10`, `Cần ôn tập`), curriculum day, drill question excerpt, and `[ Vào Luyện Tập → ]` / `[ Xem Lại Buổi Học → ]` action.
  - **Right (Senior Dilemma)**: Micro-label `TÌNH HUỐNG SENIOR`, target role badge, scenario title, trade-off situation description, and `[ Quyết Định Kiến Trúc → ]` action.

### 3. Telemetry Dock Architecture (`#telemetry-dock`)
- Replaces legacy multi-ring SVG and nested components with 2 unified Bento cards:
  - **Card 1 (Practice Streak & Consistency)**:
    - Header: `TELEMETRY DOCK (1 COL)` micro-label and Flame icon.
    - Big metric: Large tabular numeral (`text-2xl sm:text-3xl font-black tabular-nums`) displaying consecutive streak days.
    - Consistency bar: Hairline progress bar representing weekly momentum, plus freeze credits badge (`X/2 Lượt bảo lưu`).
  - **Card 2 (Knowledge Constellation & Recall)**:
    - Header: `Knowledge Constellation` micro-label and concept summary (`X active technical concept nodes mastered`).
    - Metric summary: Tabular count of connected concepts and associative relations.
    - Action trigger: Interactive button or styled link `[ Mở Bản Đồ Tri Thức → ]` navigating to `/graph`.

### 4. Elimination of Legacy Component Files
- With all telemetry markup consolidated cleanly in `HomeBentoDashboard.vue`, `ConcentricMetricCard.vue` and `DomainConstellationCard.vue` become redundant. They are safely decommissioned from the home dashboard.

## Risks / Trade-offs

- **Risk:** Vitest unit tests relying on legacy child component stubs (`ConcentricMetricCard: true`) could fail.
  - **Mitigation:** Update `HomeBentoDashboard.spec.ts` to test actual DOM text, computed properties, and routing behavior directly, conforming to Pillar 3 testing boundaries.
