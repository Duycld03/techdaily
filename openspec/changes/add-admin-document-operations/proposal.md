# Proposal

## Why

DeepPace administrators see document counts but cannot investigate failed personal imports or reliably retry source processing. PDF inputs are deleted even after failure, web/Markdown imports lack durable attempt history, and restarting the current in-memory worker loses accepted work.

## What Changes

- Add `/admin/documents` for paginated metadata inspection across personal documents and catalog entries, with scope, owner, source type, category, language and processing filters. Show attempt history, observation times, sanitized errors and actionable retry eligibility without private content.
- Persist initial PDF, web and Markdown ingestion attempts and source availability. Use durable dispatch, worker claims, leases and generation checks so restarts, concurrent requests and delayed workers cannot create duplicate or stale output.
- Allow explicit, reasoned retries of failed or interrupted source ingestion against the same retained input. Preserve ownership, document identity, source language and learning data; block retries of completed imports, published catalog entries and unsafe targets with learning activity.
- Keep validated failed sources backend-only for at most seven days, with configurable global and per-account storage quotas. Delete sources immediately after successful processing; remove invalid, expired and deleted-target inputs through bounded cleanup.
- Provide authenticated source replacement and retry for the owner of a failed personal import when its input is missing or expired. Catalog replacement remains a catalog administrator action. No consent-based private-content support viewer is introduced.
- Extend verified admin capabilities, navigation, dashboard drill-downs and append-only audit recording. Accepted retries and their eventual outcomes remain distinct.
- Keep existing personal readiness, catalog preparation/publication, source validation and on-demand learning behavior. This change does not add standalone AI, embedding or audio retry controls for readable documents.
- **BREAKING:** Catalog preparation must no longer accept a client-supplied `ReplacementTempFilePath`. Source replacement moves to validated upload/import endpoints with opaque server-managed source references.

## Capabilities

### New Capabilities

- `admin-document-operations`: Authorized, privacy-preserving document inspection, attempt history and controlled administrative source-ingestion retries.
- `document-ingestion`: Durable initial-ingestion attempts, bounded retained-source storage, safe dispatch/recovery and generation-fenced processing.

### Modified Capabilities

- `library`: Durable PDF admission and status contracts, owner-only failed-source replacement, preservation of import language and safe recovery of personal documents.
- `curated-catalog`: Retained-source recovery, secure replacement and shared ingestion concurrency without changing publication or personal-copy independence.
- `auth`: Add a server-verified document-operations capability while preserving existing destination capabilities and learner ownership restrictions.
- `admin-workspace`: Distinguish Catalog editing from Documents operations and extend admission, session cleanup and responsive layout coverage.
- `admin-dashboard`: Link persisted processing counts to authorized, filtered document operations without presenting them as live queue telemetry.

## Impact

- **Prerequisite:** Implement `add-admin-accounts-audit` first. Reuse its active-account permission resolver, privileged session boundaries, typed audit writer and transactional mutation coordinator; those are planned dependencies, not existing implemented services. Preserve its Accounts and Administration log destinations when composing overlapping auth/workspace deltas.
- Domain/Application: ingestion attempt and source lifecycle invariants, bounded query projections, validators, source replacement and retry handlers, explicit eligibility and safe error codes.
- Infrastructure: PostgreSQL migrations and indexes, atomic admission/audit/dispatch persistence, backend-only persistent storage with quota accounting, lease-based workers, cleanup and startup reconciliation. Existing in-memory channels may wake workers but cannot remain the delivery authority.
- Api/Frontend: protected document-operations endpoints, additive status fields, owner/catalog replacement endpoints, the operations workspace, confirmation forms and EN/VI feedback integrated with existing auth generations.
- Verification: real authorization and ownership tests; PostgreSQL concurrency, rollback, restart and quota tests; existing ingestion/catalog regressions; frontend data-contract tests and authenticated desktop/mobile Chromium inspection in both languages and themes.
- Excluded: private-content support access, impersonation, administrator deletion of personal documents, bulk retry/cancel, automatic failure retry, catalog Markdown creation, billing/cost analytics, infrastructure telemetry and new learning-content regeneration controls.
