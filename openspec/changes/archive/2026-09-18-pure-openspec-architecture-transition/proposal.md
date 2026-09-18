## Why

Historically, the project accumulated a monolithic `docs/` folder containing static documentation files (`features.md`, `domain-rules.md`, `api-design.md`, and `database-design.md`) generated during early ad-hoc AI interactions. This has introduced a severe split-brain architecture where documentation diverged from the actual codebase and from the 19 modular OpenSpec specifications in `openspec/specs/`. 

Furthermore, `AGENTS.md` and `README.md` continue instructing AI agents and developers to read `docs/` as the single source of truth, causing excessive token waste, context window pollution, and hallucinations based on obsolete designs (such as references to Quartz.NET, CodeMirror, and legacy endpoints). Retiring `docs/` and establishing Pure OpenSpec standardizes all feature requirements, invariants, and workflows into a clean, spec-driven developer experience.

## What Changes

- **Retire `docs/` Monolith**: Completely delete the redundant `docs/` directory (`docs/features.md`, `docs/domain-rules.md`, `docs/api-design.md`, `docs/database-design.md`).
- **Establish Pure OpenSpec as Single Source of Truth**: Clarify in platform specifications that all behavioral requirements and acceptance criteria reside exclusively in `openspec/specs/` and active changes in `openspec/changes/`.
- **Align `AGENTS.md` Developer & AI Conventions**: Update `AGENTS.md` to remove all references and mapping tables pointing to `docs/`. Direct agents to `openspec/specs/` for capabilities, `AGENTS.md` (Sections 2 & 3) for core invariants, and strongly-typed code (C# Minimal APIs, EF Core Entities, TypeScript types) for executable contracts.
- **Synchronize `README.md`**: Remove links and sections pointing to `docs/features.md` and other deleted documentation files.
- **Update OpenSpec Configuration**: Ensure `openspec/config.yaml` context reflects the verified tech stack and pure OpenSpec workflow conventions.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Add `Requirement: Pure OpenSpec Single Source of Truth Architecture` establishing spec-driven requirements in `openspec/specs/` and eliminating split-brain static documentation directories.

## Impact

- **Documentation & Spec Footprint**: Deletion of 4 static markdown files (~1,450 lines of duplicate documentation).
- **AI Agent Context & Efficiency**: Significantly reduced token consumption and eliminated hallucinations caused by reading outdated monolithic files.
- **Codebase & Runtime**: Zero runtime impact. No backend or frontend application code or database schema is altered.
