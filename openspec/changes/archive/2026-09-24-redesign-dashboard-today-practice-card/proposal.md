# Proposal

## Why

On the Home Bento Dashboard (`/`, `frontend/components/dashboard/HomeBentoDashboard.vue`), Card A (top) displays the user's active reading progress ("Bài Đọc Đang Diễn Ra" / "Continue Reading") with a direct CTA to resume reading in the reader mode (`/read/${bookId}?slice=${order}`). Beneath it, Card B currently renders static placeholder copy for an architectural scenario challenge ("Quyết định kiến trúc Senior", "+10 Điểm", "Giải Tình Huống ->"), but clicking it actually navigates to `/today`.

This causes cognitive friction and visual redundancy because `/today` is the comprehensive **Daily Focus Session** ("Luyện Tập Hôm Nay"), which combines today's curriculum reading slice, architectural scenario drill, micro-quiz, and daily streak tracking. Redesigning Card B into a dedicated **"Today's Practice Session" / "Luyện Tập Hôm Nay" Cockpit Card** establishes a clear and intuitive mental model for developers:
- **Card A (Top)**: Dedicated reader gateway to continue long-form document reading in the Library Reader.
- **Card B (Bottom)**: Dedicated daily practice launchpad for today's structured curriculum session (`/today`), displaying dynamic session metadata (curriculum day order, daily focus topic, practice breakdown, and drill status).

## What Changes

- **Redesign Dashboard Card B into "Today's Practice Session" Cockpit Card**:
  - Replace the static scenario teaser with authentic data from `useDailyFocusStore`:
    - **Header Badge & Status**: Displays "LUYỆN TẬP HÔM NAY" / "TODAY'S PRACTICE" alongside a curriculum day badge ("Lộ trình Ngày {dayOrder} / 30" or "Curriculum Day {dayOrder}") and drill status indicator (Pending / Completed with score).
    - **Focus Title & Itinerary**: Displays the current day's primary topic (`topic?.title`) and a concise session itinerary highlighting the day's dual practice items (1 Deep-Dive Reading + 1 Architecture Drill).
    - **Primary Action CTA**: Prominent action button labeled "Vào Luyện Tập →" / "Start Today's Practice →" navigating directly to `/today`.
- **Clear Action Boundary Between Card A and Card B**:
  - Card A remains the focused gateway for continuous slice reading (`/read/[bookId]?slice=...`).
  - Card B becomes the holistic command center for the daily learning ritual (`/today`).
- **Complete Multi-Language Localization**:
  - Add localized strings in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` for all labels, badges, itineraries, and button states, adhering to the Bilingual Responsive Layout Invariant.

## Capabilities

### Modified Capabilities
- `core-platform`: Update Home Bento Dashboard specifications in `core-platform` to define the architecture, data binding, and responsive visual layout of the redesigned Today's Practice Card (Card B) within the Bento Dashboard `#action-stage`.

## Impact

- **Frontend**:
  - `frontend/components/dashboard/HomeBentoDashboard.vue`: Update Card B template structure, computed properties, and navigation handler.
  - `frontend/i18n/locales/en.json` & `frontend/i18n/locales/vi.json`: Add translations for Today's Practice card headers, itineraries, status tags, and action buttons.
  - `frontend/tests/components/HomeBentoDashboard.spec.ts`: Add and update unit tests verifying data rendering, status badges, and route navigation to `/today`.
- **Backend / Database**:
  - Zero changes required. `GET /api/v1/daily-focus` already returns the complete `TodayFocusResponse` envelope (`topic`, `drill`, `question`, `pacer`).
