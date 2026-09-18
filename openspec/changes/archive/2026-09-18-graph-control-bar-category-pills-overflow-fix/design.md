## Context

On `/graph`, `GraphControlBar.vue` provides interactive multi-dimensional filtering for the knowledge graph. The second filter row displays the Category Pillars ("TRỤ CỘT KỸ THUẬT:").

Currently, the container uses:
```html
<div class="flex items-center gap-1.5 overflow-x-auto pb-0.5 no-scrollbar">
```
In English, the labels fit on a standard 896px (`max-w-4xl`) container. However, in Vietnamese (`vi-VN`), text expansions such as "TRỤ CỘT KỸ THUẬT:", "Tất Cả Trụ Cột", and "Hệ Thống Backend & Runtime" cause the total width of the row to exceed the container width. Because `no-scrollbar` completely suppresses scrollbar visibility, the rightmost pill ("Engineering Craft") is clipped and rendered as `Engineeri...`.

Additionally, inspecting `categoryPills` reveals that four out of five engineering pillars have empty translation keys (`key: ''`), causing untranslated fallback English strings to be displayed in Vietnamese mode.

## Goals / Non-Goals

**Goals:**
- Eliminate horizontal text clipping and truncation on all category pills across both English and Vietnamese locales.
- Implement responsive wrapping (`flex-wrap gap-1.5`) so filter pills flow gracefully when horizontal space is constrained.
- Complete the translation keys in `categoryPills` and localized dictionaries (`en.json`, `vi.json`) for all 5 canonical pillars.
- Streamline Vietnamese copy to be concise, professional, and consistent with the platform's senior developer terminology.

**Non-Goals:**
- Altering the graph filtering logic or state management in `useKnowledgeGraphStore.ts`.
- Changing the top row search input, 2D/3D switcher, or action buttons.

## Decisions

### 1. `flex-wrap` Over `overflow-x-auto no-scrollbar`
- **Decision**: Replace `flex items-center gap-1.5 overflow-x-auto pb-0.5 no-scrollbar` with `flex flex-wrap items-center gap-1.5` across all filter rows in `GraphControlBar.vue`.
- **Rationale**: 
  - `overflow-x-auto no-scrollbar` is an anti-pattern on desktop because users have no visual indication that content overflows or can be scrolled with a wheel.
  - Using `flex-wrap` with `whitespace-nowrap shrink-0` on individual buttons ensures that button labels never wrap internally (no multi-line button text), but the pills as whole units wrap naturally into clean subsequent rows when needed.
- **Alternatives Considered**:
  - *Show visible scrollbar*: Rejected. Scrollbars inside a floating glassmorphic control bar look clunky and distract from the graph visualization.

### 2. Complete Translation Keys in `categoryPills`
- **Decision**: Assign explicit keys to `categoryPills`:
  ```typescript
  const categoryPills = [
    { id: 'all', key: 'graph.filters.allPillars', defaultLabel: 'All Pillars' },
    { id: 'BackendRuntime', key: 'graph.filters.backendRuntime', defaultLabel: 'Backend & Runtime' },
    { id: 'DatabaseStorage', key: 'graph.filters.databaseStorage', defaultLabel: 'Database & Storage' },
    { id: 'SystemDesign', key: 'graph.filters.systemDesign', defaultLabel: 'Distributed Systems' },
    { id: 'FrontendWeb', key: 'graph.filters.frontendWeb', defaultLabel: 'Frontend & Web' },
    { id: 'EngineeringCraft', key: 'graph.filters.engineeringCraft', defaultLabel: 'Engineering Craft' }
  ]
  ```
- **Rationale**: Eliminates hardcoded English defaults when the user selects Vietnamese mode.

### 3. Concise Vietnamese Localization
- **Decision**: Refine translations in `vi.json`:
  - `"allPillars"`: `"Tất Cả"` (down from `"Tất Cả Trụ Cột"`, saving 8 characters).
  - `"backendRuntime"`: `"Backend & Runtime"` (down from `"Hệ Thống Backend & Runtime"`, saving 9 characters).
  - `"databaseStorage"`: `"Database & Storage"` (or `"Cơ Sở Dữ Liệu"`).
  - `"systemDesign"`: `"Distributed Systems"` (or `"Hệ Thống Phân Tán"`).
  - `"frontendWeb"`: `"Frontend & Web"`.
  - `"engineeringCraft"`: `"Engineering Craft"` (or `"Kỹ Nghệ Phần Mềm"`).
- **Rationale**: Adheres to the TechDaily convention where authoritative technical terms retain clean industry-standard wording, saving significant horizontal space while remaining 100% natural to senior engineers.

## Risks / Trade-offs

- **[Risk] Control bar height increases slightly when pills wrap**:
  → **Mitigation**: The control bar is floating with `backdrop-blur-md` and `max-w-4xl`. A 2-line wrap on narrow viewports adds ~28px, which comfortably floats above the graph canvas without obscuring central graph nodes.

## Migration Plan

1. Update `frontend/components/graph/GraphControlBar.vue`:
   - Replace `overflow-x-auto no-scrollbar` with `flex-wrap` on lines 186, 206, and 228.
   - Populate `key` fields in `categoryPills`.
2. Update `frontend/i18n/locales/en.json` and `vi.json` with the new translation keys.
3. Update unit tests in `frontend/tests/components/graph/GraphControlBar.spec.ts` to assert that all category pills render with `whitespace-nowrap shrink-0` inside a `flex-wrap` container and translate correctly.
