# Design: Redesign Dashboard Today's Practice Card

## Context

The Home Bento Dashboard (`frontend/components/dashboard/HomeBentoDashboard.vue`) structures the primary action area into two stacked cards within `#action-stage`:
- **Card A (Top)**: Focuses on the user's ongoing book reading progress, with a direct CTA to resume reading in Reader mode (`/read/${bookId}?slice=${order}`).
- **Card B (Bottom)**: Was originally prototyped with static teaser copy for a generic scenario challenge ("Quyết định kiến trúc Senior", "+10 Điểm", "Giải Tình Huống ->"), but navigates directly to `/today`.

In the application's domain model, `/today` is the **Daily Focus Session** ("Luyện Tập Hôm Nay") which unifies:
1. Daily curriculum reading slice (`documentChunk`).
2. Architectural interview challenge (`question`).
3. Interactive micro-quiz and daily drill evaluation (`drill`).
4. Daily streak progression (`currentStreak`).

Currently, Card B does not reflect this daily session structure, and `focusStore.data?.scenario` does not exist in `TodayFocusResponse`, causing Card B to always render static fallback copy. Transforming Card B into an authentic **"Today's Practice Session" Cockpit Card** resolves user confusion and provides a clear gateway to today's daily ritual.

## Goals / Non-Goals

**Goals:**
- Redesign Card B in `HomeBentoDashboard.vue` from a static scenario teaser into a dynamic "Today's Practice Session" Cockpit Card.
- Bind Card B to live daily focus state from `useDailyFocusStore` (`topic`, `drill`, `pacer`).
- Clearly differentiate Card A (Reader mode for long-form book slices) and Card B (Daily Focus Cockpit for curriculum practice).
- Support dynamic drill states (Pending vs Completed) with appropriate status indicators and CTA button labels.
- Guarantee strict visual height balance and zero-shift layout alignment between Card A and Card B on 1080p desktop viewports.
- Maintain 100% bilingual responsiveness in English (`en.json`) and Vietnamese (`vi.json`).

**Non-Goals:**
- Modifying backend API endpoints or database entities (`GET /api/v1/daily-focus` already delivers all required data).
- Modifying the right-side Telemetry dock (Concentric metrics, streak, constellation) or welcome banner.
- Altering the internal implementation of `/today` (`today.vue`).

## Decisions

### 1. Information Architecture & Card Structure

Card B will be structured to communicate a complete daily learning itinerary:
- **Card Header**:
  - Left: Emblem icon (`CalendarDays` or `Flame` / `Target`) inside a rounded studio container (`w-8 h-8 rounded-lg bg-brand-500/10 text-brand-500`).
  - Category pill: Uppercase monospace label `TODAY'S PRACTICE` (`dashboard.today_practice_badge` / `LUYỆN TẬP HÔM NAY`).
  - Curriculum Day badge: `Day {dayOrder} / 30` or `Lộ trình Ngày {dayOrder} / 30`.
  - Right: Real-time status pill:
    - *Pending*: `+10 Points Available` (`+10 Điểm Sẵn Sàng`).
    - *Completed*: `Completed • {score}/10` (`Đã Hoàn Thành • {score}/10`).
- **Card Body**:
  - Focus Title: Daily curriculum topic title (`topic?.title || 'Daily Architecture Focus'`).
  - Session Itinerary Strip: A clean horizontal micro-badge or bullet strip summarizing the daily agenda:
    - Item 1: `1 Core Reading` (`1 Bài đọc cốt lõi`).
    - Item 2: `1 Architecture Drill` (`1 Tình huống thực chiến`).
  - Summary / Motivation: Brief description of today's focus (`topic?.summary`).
- **Card Footer**:
  - Left: Keyboard shortcut or quick hint (`Enter to launch practice`).
  - Right: Primary CTA button:
    - When pending: `"Start Today's Practice →"` (`$t('dashboard.start_today_practice')` / `"Vào Luyện Tập →"`).
    - When completed: `"Review Today's Session →"` (`$t('dashboard.review_today_practice')` / `"Xem Lại Buổi Học →"`).
    - Navigation: `navigateTo('/today')`.

### 2. Symmetrical Action Stage Geometry

To maintain aesthetic balance with Card A:
- Both cards adhere to `p-4 sm:p-5 flex flex-col justify-between min-h-0 border border-slate-200/80 dark:border-white/[0.06] rounded-3xl glass-panel`.
- Identical top header row spacing (`flex items-center justify-between gap-3 mb-3`).
- Identical bottom action footer divider (`pt-3 border-t border-slate-200/80 dark:border-white/[0.06] flex items-center justify-between`).

### 3. Clear Navigation Routing Boundaries

- **Card A ("Continue Reading")**:
  - Target: `/read/${bookId}?slice=${currentChunkOrder}` (dedicated Reader mode for active book).
  - Intent: Deep continuous reading of documentation chunks.
- **Card B ("Today's Practice")**:
  - Target: `/today` (Daily Focus Studio Cockpit).
  - Intent: Daily habit loop (reading + scenario drill + micro-quiz + streak check).

## Risks / Trade-offs

- **Risk: Redundant Topic Titles**: When the active reading slice (Card A) is also the primary topic for today's practice (Card B), the titles might match.
  - *Mitigation*: Distinct badges, icons, and CTA phrasing clearly demarcate Card A as "Reading Slice Progress" and Card B as "Daily Curriculum Session".
- **Risk: Layout Shift on Asymmetric Status Badges**: Long status badge text in Vietnamese could cause word wrapping.
  - *Mitigation*: Enforce `whitespace-nowrap shrink-0` and compact badge styling in compliance with the Bilingual Responsive Layout Invariant.
