# Design: 5 Core Engineering Pillars & Strict Testing Boundaries

## Context

`AGENTS.md` serves as the authoritative context file loaded into AI agent system instructions at the start of every turn. Over multiple development cycles, `AGENTS.md` accumulated 21 flat rules that intermingled high-level architectural mandates with transient bug fixes. Furthermore:
1. `AGENTS.md` line 11 explicitly anchored the platform's starter curriculum to `backend/src/TechDaily.Infrastructure/Data/curriculum-30-days.json`, causing agents to treat the demo track as an architectural boundary rather than an onboarding seed file.
2. In recent frontend changes, agents repeatedly authored unit tests in Vitest that merely asserted CSS class names (e.g. `classes().toContain('max-w-5xl')`). Because Happy-DOM does not execute a CSS layout engine, these tests passed 100% while offering zero defense against visual regressions (text overflow, element collision, padding collapse), wasting developer time, maintenance overhead, and token budgets.

## Goals / Non-Goals

**Goals:**
- Consolidate all 21 rules into 5 structured, memorable **Core Engineering Pillars**:
  - Pillar 1: Production Security & Auth Boundaries
  - Pillar 2: UI Design System & Component Governance
  - Pillar 3: Frontend Testing Boundaries & Visual Inspection
  - Pillar 4: AI Engine & External Ingestion
  - Pillar 5: Verification, DevOps & Skills Protocol
- Remove the `curriculum-30-days.json` entry from `AGENTS.md`'s Source of Truth table, clarifying that TechDaily is a technology-agnostic engine for user-ingested documentation.
- Establish an explicit prohibition against CSS class assertions in Vitest, mandating visual screenshot inspection for layout and styling verification.
- Sync capability specification `openspec/specs/core-platform/spec.md` with the new 5-Pillar structure.

**Non-Goals:**
- Deleting or altering `backend/src/TechDaily.Infrastructure/Data/curriculum-30-days.json` on disk (the seeder remains intact for initial dev database initialization; decoupling seed data is out of scope).
- Rewriting existing passing Vitest test files in bulk (new tests must follow the rule; existing tests will be refactored organically during future feature iterations).

## Decisions

### Decision 1: 5 Pillars Grouping Taxonomy
Rather than maintaining an ever-expanding numbered list (Rule 1 through Rule 22+), rules are grouped into 5 distinct domains:
- **Pillar 1 (Security & Auth)** combines Rule 1 (Fake users), Rule 2 (Dev bypass auth), Rule 6 (No local-dev workarounds in prod).
- **Pillar 2 (UI Governance)** combines Rule 7 (No browser dialogs), Rule 19 (Layout archetypes), Rule 20 (No native `<select>`), Rule 21 (Vue Playground protocol), and typography/bilingual standards.
- **Pillar 3 (Testing & Visual Inspection)** establishes the new testing boundary (Data Contracts vs Visual Geometry).
- **Pillar 4 (AI & Ingestion)** combines Rule 10 (Markdown prose), Rule 11 (User-triggered generation), Rule 12 (Flash-Lite model selection), Rule 14 (Balanced bracket JSON), Rule 16 (Canonical links), Rule 17 (Crawler moniker filtering).
- **Pillar 5 (DevOps & Protocol)** combines Rule 5/13 (Local-first verification), Rule 8 (Nginx cache restart), Rule 9 (ED25519 SSH), Rule 18 (Mandatory skills reading).

### Decision 2: Clear Boundary Between Vitest and Visual Inspection
Vitest + Happy-DOM is designated strictly as a **Data Contract & Behavioral State Validator**:
- **Allowed in Vitest**: Form submission payload checking (`expect(spy).toHaveBeenCalledWith(...)`), validation messages, store state transitions, route middleware redirects, error toast triggers.
- **Prohibited in Vitest**: Asserting CSS utility classes (`expect(el.classes()).toContain(...)`) under the false guise of UI testing.
- **Visual Inspection**: Visual layout, responsive scaling (Mobile 390px vs Desktop 1080p), and bilingual text wrapping are validated through real browser rendering and screenshot previews.

### Decision 3: Remove Curriculum Starter Pack from Source of Truth Table
The entry in `AGENTS.md` linking "Starter Pack curriculum" to `curriculum-30-days.json` is removed. TechDaily's documentation and source of truth table will direct developers to `openspec/specs/` for capabilities and `backend/src/TechDaily.Domain/Entities/` for data models, keeping the core platform technology-agnostic.

## Risks / Trade-offs

- **Risk**: An agent might misinterpret "prohibit CSS class assertions" as prohibiting all component testing.
  - **Mitigation**: The rule explicitly enumerates what Vitest MUST test: form input binding, data payload serialization, validation barriers, disabled states, and error handling.
