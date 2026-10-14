# Spec Delta

## ADDED Requirements

### Requirement: Relational Soft Delete Partial Unique Index Invariant
The relational persistence layer SHALL configure partial unique indexes filtered by `WHERE "IsDeleted" = false` on all soft-deletable entities declaring unique business constraints. Soft-deleted records SHALL NOT prevent active records from utilizing the same business key.

#### Scenario: Re-registration with a previously soft-deleted email
- **WHEN** a user account with email `engineer@example.com` is marked `IsDeleted = true` and a new registration request is submitted with `engineer@example.com`
- **THEN** the registration succeeds and persists a new active user without database unique constraint violation

#### Scenario: Re-importing a document with an identical slug after soft deletion
- **WHEN** a document book with slug `design-patterns` is soft-deleted and a user imports a new document generating the slug `design-patterns`
- **THEN** the persistence layer allows the import without slug uniqueness conflict
