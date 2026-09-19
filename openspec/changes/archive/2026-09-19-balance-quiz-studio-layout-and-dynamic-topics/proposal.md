# Proposal: Balance Quiz Studio Layout, Dynamic Topics, and Versatile Question Pacing

## Why

Following the initial Bento restructuring of `/quiz`, actual usage on standard desktop displays reveals three key ergonomic and UX shortcomings:
1. **Excessive Outer Gutters & Height Asymmetry**: The `/quiz` page is constrained to `max-w-5xl` (~1024px), leaving wide, awkward black margins on modern desktop displays (1440px - 1920px). Inside the grid, the asymmetric 7/5 column split creates a vertical height mismatch: the right column (~530px with 4 vertically stacked cards) is significantly taller than the left column (~390px), leaving an unbalanced empty void beneath the left card.
2. **Static Hardcoded Topics**: The suggested topic chips are currently hardcoded to a static array of 7 generic topics, disconnected from the user's uploaded/active documentation books in `/library` and their career target role in `/profile`.
3. **Rigid Question Count Options**: The question count is restricted to a binary choice (5 vs 10 questions), omitting a low-friction "Quick Drill" option (3 questions ~3 mins) ideal for quick concept checks on mobile or busy schedules.

Resolving these issues balances the layout symmetrically, provides contextually relevant study suggestions, and offers flexible question pacing while strictly respecting design invariants.

## What Changes

- **Standard Studio Container Width (`max-w-6xl`)**: Upgrades the root `/quiz` container from `max-w-5xl` to `max-w-6xl` (matching `/library`, `/profile`, and `/roadmap`), effectively eliminating excess side margins and providing comfortable horizontal breathing room.
- **Symmetric 50/50 Bento Grid (`lg:grid-cols-2`)**: Rebalances the generation studio into an equal 2-column grid (`grid grid-cols-1 lg:grid-cols-2 gap-6 items-stretch`) where both cards have equal visual weight.
- **$2 \times 2$ Typographic Seniority Matrix**: Reorganizes the 4 seniority level cards (`Fresher`, `Junior`, `Mid-Level`, `Senior`) into a clean $2 \times 2$ grid (`grid grid-cols-1 sm:grid-cols-2 gap-3`) in the right column, reducing right-column vertical height to perfectly balance with the left column (~440px each).
- **Context-Aware Dynamic Topic Suggestions**: Replaces the static hardcoded string array with reactive, prioritized suggestion chips:
  - **Source 1 (User's Reading Library)**: Prioritizes topics from active books in `libraryStore.books` (e.g. book titles and technical focus).
  - **Source 2 (Target Role Alignment)**: Adapts suggestions based on `profileStore.profile.targetRole` (e.g. tailoring for Senior Backend, Frontend, Cloud/DevOps).
  - **Source 3 (Architectural Fallback)**: Curates foundational senior engineering topics across core pillars (Runtime Internals, Concurrency, Database MVCC, Distributed Systems).
- **3-Tier Question Count Segmented Controller (`3` | `5` | `10`)**: Expands the segmented pill bar from 2 options to 3 distinct learning tempos:
  - `3 Questions`: Quick concept check (~3 minutes).
  - `5 Questions`: Standard daily practice (~5 minutes).
  - `10 Questions`: Comprehensive interview simulation (~10-12 minutes).
  - Full compatibility with backend validator `Math.Clamp(count, 1, 10)`.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `quiz`: Updates the visual layout and interaction requirements of `/quiz` to mandate the `max-w-6xl` container width, equal 50/50 Bento grid with $2 \times 2$ seniority level arrangement, context-aware dynamic topic chips, and 3-tier question count pacing (3, 5, 10).

## Impact

- **Frontend**:
  - `frontend/pages/quiz.vue`: Update container classes, 50/50 grid, $2 \times 2$ seniority layout, dynamic `computedQuickTopics`, and 3-tier count selector.
  - `frontend/i18n/locales/en.json` & `frontend/i18n/locales/vi.json`: Add locale key `quiz.count_3` ("3 Questions" / "3 Câu").
- **Tests**:
  - `frontend/tests/pages/quiz.spec.ts`: Update tests to assert `quiz.count_3`, 50/50 layout classes, and dynamic topic rendering.
- **Backend & Database**: Zero changes. Backend already supports counts 1-10.
