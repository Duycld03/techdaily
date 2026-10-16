# Proposal

## Why

Document categories are a closed enum duplicated in persistence, APIs, filters, AI prompts, and learning visualizations. Administrators cannot introduce a new subject without a deployment, which conflicts with DeepPace's user-supplied, technology-agnostic learning model.

## What Changes

- Introduce a global, flat category catalog managed by authorized administrators: stable IDs, immutable unique slugs, required English and Vietnamese names, display order, and active/inactive status.
- Add a dedicated category administration workspace with validated create/edit, activation/deactivation, safe deletion, optimistic concurrency, and atomic audit history. Depend on `add-database-backed-roles`: add an explicit ManageCategories grant to protected Admin and resolve current database authority, never a runtime user-ID allowlist.
- Keep exactly one primary category per document. Inactive categories remain visible on existing documents and learning history, but cannot receive new assignments. Referenced categories cannot be deleted.
- **BREAKING**: Replace category enum names/ordinals in persistence, requests, responses, filters, graph identities, and client state with a single stable-ID contract. Migrate all seven existing categories and known legacy aliases without rewriting document content or learning progress; remove the obsolete enum and category dictionaries after cutover.
- Load category labels and choices from shared metadata across personal Library, Discover/catalog administration, quizzes, insights, reviews, and knowledge graphs. Preserve English/Vietnamese rendering and existing ownership boundaries; do not recreate the obsolete profile mastery widget.
- Remove fixed keyword classification, fixed pillar counts, and category-name-dependent curation policy. Require explicit category selection for new documents; ground AI enrichment in the source and its language rather than treating a category as a curriculum or formatting mode.
- Reconcile the conflicting Library and AI-curation preservation requirements: faithful structural Markdown restoration may occur only during existing explicit ingestion/curation flows, never as a side effect of category administration or renaming; author prose must not be condensed or rewritten.
- Deliver a coordinated database/API/frontend cutover and migration rehearsal, not compatibility aliases or a second taxonomy. No category hierarchy, multi-category documents, free-form tag system, automatic classifier, unrelated security remediation, or broader hardcoding cleanup is included.

## Capabilities

### New Capabilities

- `document-categories`: Shared category identity and metadata, administrator lifecycle, assignment rules, migration guarantees, and the category management workspace.

### Modified Capabilities

- `library`: Data-driven import/edit/filter choices and badges, stable category assignments, and category-independent source fidelity.
- `curated-catalog`: Dynamic draft metadata, discovery filters, adoption assignments, and shared category labels without changing published content snapshots.
- `knowledge-graph`: Stable-ID category hubs, dynamic filtering and legends, and consistent 2D/3D presentation without a fixed number of pillars.
- `quiz`: Stable category references for generation, source-grounded questions, mistake filtering, and category analytics.
- `insights`: Category-ID queries and generation, dynamic metadata, and source-derived suggestions instead of a fixed category topic table.
- `review`: Authoritative category metadata across card loading and mutations, including retained inactive assignments and explicitly unclassified orphaned cards.
- `ai-curation`: Source- and language-grounded formatting independent of category names, with one consistent verbatim structural-restoration policy.
- `core-platform`: Remove fixed-four-pillar constraints from obsolete profile mastery clauses without adding a new tracker/statistics API; distinguish free-text term context from category identity and correct the canonical generated OpenAPI type destination. Existing settings, schedule, branding and authentication behavior remains unchanged.
- `admin-workspace`: Capability-gated Categories navigation, route admission, and design-system integration.
- `admin-audit`: Category mutation events committed atomically with their changes and rendered through the existing bounded, read-only audit workspace.

## Impact

Prerequisite: implement and accept `add-database-backed-roles` first, including its authority initialization, shared capability/navigation contracts and clean rejection-audit path. Compose shared deltas on that accepted baseline; category planning alone does not authorize either implementation.

- Backend: .NET 10 Domain entities and enums, Application handlers/DTOs/validators and permission contracts, EF Core/PostgreSQL mappings and migrations, category-aware caches/seeds, Minimal API endpoints, and OpenAPI schemas.
- Frontend: Nuxt 4 category metadata/state, existing import and administration components, filters and badges, learning surfaces, graph rendering, and English/Vietnamese locale resources. Regenerate or remove stale generated category contracts according to actual use; no numeric compatibility map remains in production clients. The current profile-to-settings redirect remains unchanged.
- Operations: Confirm the RBAC prerequisite, back up and inventory category values, stop writers/workers for a bounded cutover, migrate and verify references, deploy matching backend/frontend versions, and use a tested backup-based rollback before reopening writes. Required category schema failure must prevent API/worker readiness.
- Verification during implementation: backend behavioral and PostgreSQL migration/concurrency tests, the complete frontend test suite, an authenticated end-to-end category lifecycle smoke, and directly inspected English/Vietnamese desktop/mobile browser screenshots. This change currently creates planning artifacts only.
