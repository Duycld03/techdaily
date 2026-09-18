## Context

See `proposal.md` for motivation. The codebase currently maintains two parallel documentation layers:
1. A monolithic static `docs/` folder (`features.md`, `domain-rules.md`, `api-design.md`, `database-design.md`) generated during early manual prompting.
2. An active spec-driven OpenSpec repository in `openspec/specs/` (19 modular capability specs) and change workflows in `openspec/changes/`.

Furthermore, `AGENTS.md` instructs AI agents to read `docs/` before executing tasks, leading to token exhaustion and outdated knowledge ingestion.

## Goals / Non-Goals

**Goals:**
- Eliminate the split-brain documentation problem by permanently retiring `docs/`.
- Establish `openspec/specs/` as the single source of behavioral truth across the entire platform.
- Update `AGENTS.md` to guide AI agents directly to `openspec/specs/`, `AGENTS.md` invariants, and executable code artifacts.
- Synchronize `README.md` to remove dead markdown links to `docs/`.

**Non-Goals:**
- Modifying backend C# Minimal API endpoints, domain entities, or database migrations.
- Modifying frontend Vue/Nuxt components or application logic.
- Rewriting existing capability specifications in `openspec/specs/`.

## Decisions

### 1. Pure OpenSpec as Exclusive Spec System
- **Decision**: Retire `docs/` and rely 100% on `openspec/specs/<capability>/spec.md` for requirements and `openspec/changes/` for proposed/delta features.
- **Rationale**: OpenSpec provides modular, domain-isolated, testable specifications with explicit Given/When/Then scenarios. Keeping a secondary static directory causes immediate drift.
- **Alternatives Considered**: 
  - *Keep `docs/` and auto-sync with OpenSpec*: Rejected. Maintaining synchronization scripts between 19 modular specs and 4 monolithic markdown files adds friction, token cost, and continuous drift.

### 2. Code and Type System as Executable Contract
- **Decision**: Treat C# Minimal API endpoint definitions, RFC 7807 problem details, EF Core entity models, and TypeScript composable interfaces as self-documenting executable contracts instead of maintaining static JSON payload tables in `api-design.md` and `database-design.md`.
- **Rationale**: Code compiles and tests run automatically. Static markdown JSON mocks in `docs/` drift within weeks of feature development.
- **Alternatives Considered**: 
  - *Keep `docs/api-design.md` manually updated*: Rejected. High maintenance overhead with zero automated compiler verification.

### 3. Streamlined `AGENTS.md` Source-of-Truth Table
- **Decision**: Replace the top table in `AGENTS.md` with direct references to `openspec/specs/` for capabilities, `AGENTS.md` (Sections 2 & 3) for domain invariants, and code files for schemas/endpoints.
- **Rationale**: Reduces agent token prompt size and guarantees agents use the latest behavioral specifications.

## Risks / Trade-offs

- **[Risk]** Broken hyperlinks in developer tools or git commit messages referencing `docs/` paths.
  → **Mitigation**: Update all active repository documentation (`README.md`, `AGENTS.md`, and `openspec/config.yaml`). Historical change archives remain preserved in `openspec/changes/archive/`.
- **[Risk]** Developers looking for a single giant list of all endpoints.
  → **Mitigation**: Point developers to the interactive Swagger / OpenAPI surface at `/swagger` when running the backend in development, or inspect `TechDaily.Api/Endpoints/`.

## Migration Plan

1. Remove the entire `docs/` directory (`docs/features.md`, `docs/domain-rules.md`, `docs/api-design.md`, `docs/database-design.md`).
2. Update `AGENTS.md` source of truth table and document navigation references.
3. Update `README.md` architecture and documentation tables, pruning all links to `docs/`.
4. Validate OpenSpec integrity with `openspec validate pure-openspec-architecture-transition --type change`.
