# Design: System Layout Archetypes and Custom Select Standard

## Context

TechDaily's UI primitive layer (buttons, form inputs, option cards, modals, code blocks) is established, but page-level views lack cohesive layout shells. On standard 1080p desktop viewports (1920x1080, available width ~1680px), pages such as `/review`, `/settings`, and `/notes` suffer from excessive empty black margins, unbalanced heights, or awkward single-column vertical stretching. Additionally, native unstyled HTML `<select>` elements render OS-native dropdowns with default blue highlights that break dark obsidian theme immersion.

The base implementations of the three layout components (`StudioLayout.vue`, `MasterDetailLayout.vue`, `BoardLayout.vue`) and their interactive showcase (`LayoutArchetypesShowcase.vue`) were authored in commit `a800956` on branch `v0/design-system-showcase`. This design integrates and adapts them into the core repository.

## Goals / Non-Goals

**Goals:**
- Provide 3 reusable system layout components in `frontend/components/layout/`:
  - `StudioLayout.vue`: Two-column (68/32) layout for practice and focus sessions.
  - `MasterDetailLayout.vue`: Sidebar rail (256px) + expansive content panel for settings and configuration.
  - `BoardLayout.vue`: Header search/tag filter bar + 2-to-3 column auto-flowing grid for content archives.
- Replace all unstyled native `<select><option>` elements in `PrimitivesShowcase.vue` with accessible `AppSelect.vue` dropdowns.
- Mount Section 09 ("System Layout Archetypes") in `frontend/pages/showcase.vue`.
- Modernize `/review` using `StudioLayout` to pair the active review card with a companion telemetry dock (session progress, SM-2 metrics, keyboard cheatsheet).
- Maintain 100% responsive behavior down to 320px mobile viewports with zero horizontal overflow and no nested double-scrollbars.

**Non-Goals:**
- Modifying backend API endpoints, DTO contracts, or database schemas.
- Altering core SM-2 spaced repetition algorithms or card scheduling formulas.
- Overhauling primary application navigation shells (`AppSidebar.vue`, `AppHeader.vue`).

## Decisions

### 1. Viewport Bounding and Column Split Strategy
- **Decision**: In `StudioLayout.vue`, use `min-h-[calc(100vh-3.5rem)] sm:min-h-[calc(100vh-3.75rem)]` matching the application header offset.
- **Split Ratio**: Main action stage takes `w-full lg:w-[68%]` and Telemetry Dock takes `hidden lg:flex lg:w-[32%] flex-col gap-4`.
- **Mobile Graceful Degradation**: On screens $< 1024\text{px}$, the dock collapses into a stacked section below the main action stage or remains accessible via tab toggle, ensuring cards and buttons receive 100% width.
- *Alternatives Considered*: A 50/50 split was rejected because the primary flashcard and code snippet require greater horizontal reading width than auxiliary telemetry indicators.

### 2. Custom Select Component Mandate
- **Decision**: All dropdown controls must strictly use `frontend/components/common/AppSelect.vue`.
- **Technical Rationale**: Native `<select>` elements delegate `<option>` rendering to the host operating system (Windows/macOS), ignoring CSS theme overrides and displaying default OS highlights. `AppSelect.vue` uses `<Teleport to="body">`, custom styling with `#18181b` elevation, Lucide checkmarks, and full keyboard arrow navigation.

### 3. Review Page Refactoring Architecture
- **Decision**: Restructure `frontend/pages/review.vue` under `activeTab === 'session'` into:
  - `#main`: Encapsulates `FlashcardDeck.vue` with max width `max-w-3xl`.
  - `#dock`:
    - `ReviewProgressCard`: Shows `doneCount / totalCount` with radial or gradient progress bar.
    - `Sm2TelemetryCard`: Displays active card's Ease Factor ($EF$), interval days, repetition number, and status tag.
    - `ReviewHotkeysCard`: Shows key bindings (`[Space]`, `[1-4]`, `[E]`).
    - `SourceContextCard`: Displays original document excerpt origin and link back to chapter.
- *Alternatives Considered*: Keeping the centered single card was rejected because it wastes over 60% of available screen real estate on desktop 1080p.

## Risks / Trade-offs

- **Risk: Content Height Exceeding Viewport Fold**:
  - *Mitigation*: Ensure the main card and dock widgets maintain compact padding (`p-4 sm:p-5`) and font sizing (`text-sm` body, `text-base` headings) to stay above 850px vertical fold on 1080p screens with Windows 125% DPI scale.
- **Risk: Teleport Z-Index Overlap in Showcases**:
  - *Mitigation*: `AppSelect.vue` coordinates with z-index `z-[100]` and uses click-outside detection from VueUse to guarantee clean dismissal without clipping.
