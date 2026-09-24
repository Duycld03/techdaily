# Proposal: Consolidate AGENTS.md Governance & Establish Strict Testing Boundaries

## Why

Over dozens of feature iterations, `AGENTS.md` has accumulated 21 flat, fragmented rules that mix supreme architectural invariants with transient bug fixes. This flat enumeration causes cognitive overload for AI agents and developers, resulting in missed rules and "Test Theater" where agents write brittle Vitest assertions checking CSS/Tailwind classes (e.g., `expect(el.classes()).toContain('max-w-5xl')`) under the false assumption that this validates UI layout, while Happy-DOM lacks a CSS layout engine and cannot detect real-world visual regressions. Furthermore, `AGENTS.md` still cites `curriculum-30-days.json` as a platform baseline, contradicting TechDaily's core principle as a technology-agnostic learning engine for user-ingested documentation.

## What Changes

- **Consolidate `AGENTS.md` into 5 Core Engineering Pillars**: Restructure the sprawling 21-rule list into 5 coherent pillars:
  1. *Security & Clean Architecture*: Pure DI, Result Pattern, PBKDF2 security, 401 on unauthorized access, zero fake/default fallback users.
  2. *UI Design System & Component Governance*: 4 mandatory layout archetypes (`StudioLayout`, `BentoDashboardLayout`, `MasterDetailLayout`, `BoardLayout`), strict ban on native `<select>` and browser dialogs (`alert/confirm/prompt`), responsive typography standards ($\ge 14\text{px}$ mobile, $\ge 16\text{px}$ desktop), bilingual `whitespace-nowrap shrink-0` invariants, and mandatory Vue playground prototyping.
  3. *Frontend Testing Boundaries & Visual Inspection Standard*: Strict prohibition of CSS/Tailwind class assertions in Vitest (`expect(el.classes()).toContain(...)`). Vitest tests MUST strictly defend data contracts, form serialization payloads, validation barriers, auth store state, and route guards. Visual layout, spacing, and responsive text wrapping MUST be verified via visual screenshot inspection, not unit tests.
  4. *AI Engine & External Ingestion*: Low-latency models (`gemini-3.5-flash-lite`), depth-balanced bracket scanning for LLM JSON, user-triggered generation, and canonical crawler link resolution.
  5. *Verification, DevOps & Skills Protocol*: Local-first testing prior to git push, Docker Nginx upstream cache restart, and mandatory pre-flight reading of `.agents/skills/`.
- **Remove Hardcoded Starter Pack Reference**: Eliminate the `curriculum-30-days.json` row from the Source of Truth table in `AGENTS.md`, reinforcing that TechDaily is a technology-agnostic documentation platform without a hardcoded curriculum boundary.
- **Synchronize Core Platform Specification**: Update `openspec/specs/core-platform/spec.md` to reflect the 5 consolidated pillars and formally codify the prohibition of CSS class assertions in automated tests.

## Capabilities

### New Capabilities
*None.*

### Modified Capabilities
- `core-platform`: Update Developer & Agent UI Design Governance Protocol to codify the 5 Core Engineering Pillars and define strict testing boundaries between data contracts (Vitest) and visual geometry (screenshot inspection).

## Impact

- **Documentation**: `AGENTS.md`.
- **Specifications**: `openspec/specs/core-platform/spec.md`.
- **Agent Behavior**: AI agents will no longer generate wasteful CSS class assertions in Vitest, focusing unit tests strictly on data payloads and API contracts while relying on visual inspection for layout validation.
- **Breaking Changes**: Zero breaking changes to application runtime or production APIs.
