# Design

## Context

See `proposal.md` for background and motivation. The system operates with an ASP.NET Core backend and a Nuxt 3 frontend. The route `/read/**` is configured with `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless` to support multi-threaded on-device WASM speech synthesis. Transitioning between non-COOP pages (such as `/`) and `/read/**` causes modern browsers (notably Brave Shields and Chromium) to enforce browsing context group process isolation.

## Goals / Non-Goals

**Goals:**
- Eliminate history poisoning and infinite reload loops on browser back navigation from `/read/[bookId]`.
- Prevent accidental authentication session clearance (`clearSession`) when navigating across COOP process boundaries.
- Provide intelligent, contextual return navigation in the Reader header bar.
- Disambiguate dashboard practice actions so Daily Micro-Drill targets `/today?tab=challenge` and Senior Dilemma targets `/quiz`.
- Guarantee exact pixel-level row height alignment between the Action Stage and Telemetry Dock in `HomeBentoDashboard.vue`.

**Non-Goals:**
- Removing or altering the COOP/COEP security headers required for WASM speech synthesis.
- Modifying backend authentication endpoints or database entities.
- Redesigning the interior reading studio or quiz pages.

## Decisions

### 1. Reader Isolation Reload: `window.location.replace` vs `window.location.assign`

- **Decision**: Replace `window.location.assign(to.fullPath)` with `window.location.replace(to.fullPath)` inside `frontend/pages/read/[bookId].vue`.
- **Rationale**: When Nuxt client router pushes a route, browser history contains `[/] -> [/read/xxx]`. If `assign()` is invoked, a third entry is pushed: `[/] -> [/read/xxx (unisolated)] -> [/read/xxx (isolated)]`. Pressing browser back visits the unisolated middle entry, which triggers the reload again in an infinite loop. Using `replace()` overwrites the unisolated entry in-place, leaving a clean 2-step history `[/] -> [/read/xxx (isolated)]`. Pressing browser back returns cleanly to `[/]`.
- **Alternative Considered**: Loading COOP/COEP globally across the whole application. Rejected because COOP breaks `window.opener` needed by Google OAuth popup on the login page.

### 2. Cookie Persistence & Anti-Purge Guard in `useAuthStore` & `useApiClient`

- **Decision**:
  1. Ensure `tokenCookie` in `useAuthStore.ts` always specifies `{ path: '/', sameSite: 'lax', maxAge: 60 * 60 * 24 * 30 }`.
  2. In `useApiClient.ts`, retrieve the token by checking `useCookie('techdaily_token')`, `localStorage`, and `sessionStorage`.
  3. Ensure that failed transient API requests during initial page hydration do not immediately wipe persistent credentials. Only an explicit 401 response from the refresh token endpoint (`/api/v1/auth/refresh`) may purge credentials.
- **Rationale**: When switching processes across COOP boundaries in Brave with Shields enabled, `sessionStorage` is partitioned, and hydration timing may delay cookie synchronization. Checking all persistent stores prevents false-positive logouts.

### 3. Contextual Reader Return Navigation

- **Decision**: Update `ReaderHeaderBar.vue` so the back button navigates back via `router.back()` if the previous history entry belongs to the application, or falls back to `/` (if entered from dashboard) or `/library`.
- **Rationale**: Readers entering from the Home Bento Dashboard expect "Back" to return to the dashboard, whereas readers entering from `/library` expect to return to the library.

### 4. Differentiated Dashboard Practice Action Targets

- **Decision**:
  - Sub-card 1 (Daily Micro-Drill): Emits and navigates to `/today?tab=challenge`. In `today.vue`, when `route.query.tab === 'challenge'`, activate `activeMobileTab = 'challenge'` and scroll/focus the challenge dock.
  - Sub-card 2 (Senior Dilemma): Emits and navigates to `/quiz`, directly opening the Architecture Interview Arena where users can practice scenario-based questions across Fresher to Senior/Staff levels.
- **Rationale**: Resolves duplicate links to `/today` and maps each dashboard card to its authentic domain venue.

### 5. Bento Dashboard 2x2 Flush Grid Alignment (CSS Subgrid)

- **Decision**:
  - In `BentoDashboardLayout.vue`, configure the main grid container with `lg:grid-cols-3 lg:grid-rows-2 gap-3.5 sm:gap-4`.
  - In `action-stage` (`lg:col-span-2`), apply `lg:row-span-2 lg:grid lg:grid-rows-subgrid`.
  - In `telemetry-dock` (`lg:col-span-1`), apply `lg:row-span-2 lg:grid lg:grid-rows-subgrid`.
  - In `HomeBentoDashboard.vue`, style the "Ghi Nhớ & Mục Tiêu" card with `h-full flex flex-col justify-between`.
- **Rationale**: CSS Subgrid forces Row 1 of the action stage (Hero Reading Card) and Row 1 of the telemetry dock (Practice Streak Card) to share the exact same grid track height. The streak card's footer aligns flush with the reading card's action footer, completely eliminating the hollow notch.

```
+-------------------------------------------------------------------------------+
| HEADER: Orientation Banner                                                    |
+-------------------------------------------------------+-----------------------+
| ROW 1 (Shared Grid Track Height via Subgrid):         |                       |
| [ Hero Active Reading Card ] (lg:col-span-2)          | [ Ghi Nhớ & Mục Tiêu ]|
| - Title, Chapter, Summary                             | - Streak Days counter |
| - Badges & Reading progress bar                       | - Retention subhead   |
| - Footer: "Nhấn Enter để đọc"     [ Đọc Tiếp -> ]     | - Goal progress bar   |
+-------------------------------------------------------+-----------------------+
| ROW 2 (Shared Grid Track Height via Subgrid):         |                       |
| [ Tier 2 Split Practice Subgrid ] (lg:col-span-2)     | [ Radar Tri Thức ]    |
| - Micro-Drill (-> /today?tab=challenge)               | - Knowledge nodes     |
| - Senior Dilemma (-> /quiz)                           | - [ Mở Vũ Trụ 3D -> ] |
+-------------------------------------------------------+-----------------------+
```

## Risks / Trade-offs

- **CSS Subgrid Compatibility**: All modern desktop browsers (Chrome 117+, Firefox 71+, Safari 16+) fully support CSS Subgrid. For legacy engines lacking subgrid, the layout degrades gracefully to flex column layout.
- **Router Back on Cold Reload**: If a user enters `/read/[bookId]` directly by copying the URL, `window.history.length` may be 1. The fallback to `/library` prevents breaking navigation.
