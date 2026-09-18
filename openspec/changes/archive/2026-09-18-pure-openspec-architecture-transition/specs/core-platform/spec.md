## ADDED Requirements

### Requirement: Pure OpenSpec Single Source of Truth Architecture
The platform SHALL maintain specifications, business constraints, and acceptance criteria exclusively within modular domain capability files in `openspec/specs/<capability>/spec.md` and active lifecycle changes in `openspec/changes/`. Monolithic legacy documentation directories (`docs/`) SHALL be completely retired, preventing desynchronization, dual sources of truth, and outdated developer context.

#### Scenario: Developer or AI agent inspects platform specifications
- **WHEN** a developer or AI agent queries platform capabilities, business rules, or acceptance criteria
- **THEN** specifications are resolved exclusively from modular domain files in `openspec/specs/` and active changes in `openspec/changes/`
- **AND** static legacy documentation directories such as `docs/` are absent from the repository.

#### Scenario: Developer or AI agent consults architecture invariants
- **WHEN** an AI agent or developer inspects non-negotiable architectural rules and anti-patterns
- **THEN** invariants are resolved from `AGENTS.md` (Sections 2 and 3) without requiring or loading monolithic markdown files (`docs/features.md`, `docs/api-design.md`, `docs/database-design.md`).

#### Scenario: Executable contracts serve as database and API truth
- **WHEN** an engineer or agent inspects endpoint signatures or data models
- **THEN** strongly-typed code in `TechDaily.Api/Endpoints/`, OpenAPI/Swagger specifications, EF Core Entity models, and database migrations serve as the definitive, executable implementation contracts.
