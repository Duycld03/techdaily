# Proposal

## Why

When users navigate between the dashboard and other routes using browser forward/back buttons (or mouse macro forward/back keys) via bfcache or popstate navigation, two issues degrade the user experience:
1. In Card B (`HomeBentoDashboard.vue`), the itinerary strip pills (`1 Core Reading`, `1 Architecture Drill` / `1 Bài đọc cốt lõi`, `1 Thử thách tình huống`) employ fragile semi-transparent classes (`bg-slate-100/80 dark:bg-canvas-elevated/60`) that, during route transitions, theme synchronization delays, or browser history restoration, render as solid light-gray `#c4c7ca` blocks with matching light-gray text, creating an unreadable zero-contrast box.
2. The Home Dashboard (`frontend/pages/index.vue`) conditionally skips data fetching if `focusStore.data` already exists in memory (`if (!focusStore.data)`), leaving daily drill completion status, scenario content, and momentum metrics stale when users complete actions on other pages and return via browser history or mouse macro buttons.

## What Changes

- **Resilient Itinerary Pill Contrast Tokens:** Update Card B itinerary strip pills in `frontend/components/dashboard/HomeBentoDashboard.vue` to use high-contrast, theme-resilient styling (`dark:bg-white/[0.04] dark:border-white/[0.06] dark:text-slate-200` in dark mode, and `bg-slate-100 text-slate-700 border-slate-200/80` in light mode). This eliminates low-contrast washed-out light-gray boxes across all browser states and hydration lifecycles.
- **History & bfcache Revalidation:** Update `frontend/pages/index.vue` and `HomeBentoDashboard.vue` to revalidate today's focus data (`fetchTodayFocus`) on route activation and browser history popstate / `pageshow` events, ensuring drill completion status, points badges, and active scenario text remain in sync after returning from other views.
- **Optimistic Background Revalidation:** When cached store data exists, revalidation occurs in the background without clearing current state or showing full-page loading spinners, preventing layout shifts.
- **Unit and Visual Verification:** Update unit tests to verify resilient pill styling classes, test history revalidation logic, and execute headless Chromium visual checks across Desktop and Mobile viewports.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Update Home Command Center Dashboard requirements to mandate theme-resilient itinerary pill contrast tokens and history navigation revalidation for daily practice state.
