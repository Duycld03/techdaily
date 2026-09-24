# Tasks: Consolidate AGENTS.md Governance & Establish Strict Testing Boundaries

## 1. Documentation - AGENTS.md Restructuring

- [x] 1.1 In `AGENTS.md`, remove the `curriculum-30-days.json` row from the Source of Truth table, and update the Technology-Agnostic Platform invariant to emphasize user-ingested documentation without hardcoded curriculum boundaries
- [x] 1.2 In `AGENTS.md`, replace the flat 21-rule list in Section 3 with the **5 Core Engineering Pillars** (Pillar 1: Production Security & Auth Boundaries, Pillar 2: UI Design System & Component Governance, Pillar 3: Frontend Testing Boundaries & Visual Inspection, Pillar 4: AI Engine & External Ingestion, Pillar 5: Verification, DevOps & Skills Protocol)
- [x] 1.3 In `AGENTS.md` Pillar 3, codify the strict prohibition of CSS/Tailwind class assertions in Vitest (`expect(el.classes()).toContain(...)`), specify what Vitest MUST test (data contracts, validation barriers, auth state, error handling), and mandate visual screenshot inspection for layout and responsive geometry

## 2. Specification - Core Platform Spec Sync

- [x] 2.1 In `openspec/specs/core-platform/spec.md`, update the `Developer & Agent UI Design Governance Protocol` requirement to formally reference the 5 Core Engineering Pillars
- [x] 2.2 In `openspec/specs/core-platform/spec.md`, add the `Frontend Testing Boundaries & Visual Inspection Standard` requirement specifying the separation between behavioral data contracts (Vitest) and visual geometry verification (screenshot inspection)

## 3. Verification

- [x] 3.1 Verify Markdown formatting, typography, and section headers in `AGENTS.md`
- [x] 3.2 Run `openspec validate consolidate-agents-governance-and-testing-pillars --json` to verify that all change artifacts adhere to schema constraints
