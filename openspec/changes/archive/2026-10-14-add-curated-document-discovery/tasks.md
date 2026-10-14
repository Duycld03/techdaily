# Tasks

## 1. Domain

- [x] 1.1 Add CatalogDocument/CatalogSlice, visibility and preparation states, revision/review tracking, provenance fields and publication transitions; verify domain tests reject empty content, missing initial formatting, missing permission metadata and stale review.
- [x] 1.2 Add personal-book catalog provenance and snapshot construction rules; verify new IDs, language/attribution preservation and exclusion of personal learning records in domain tests.

## 2. Infrastructure and Persistence

- [x] 2.1 Extend DbContext interfaces/configuration and generate additive migrations for catalog tables, processing attempts, soft-delete filters and partial unique adoption/slice indexes; verify migration applies to an existing PostgreSQL database without modifying personal content.
- [x] 2.2 Add explicit catalog destinations to ingestion persistence while reusing extraction/crawling/slicing/formatting services; verify existing personal PDF/URL ingestion tests pass and catalog jobs never create personal books.
- [x] 2.3 Implement processing generation/lease checks, progress, cancellation, interrupted-work recovery and explicit retries; verify duplicate retries, stale workers, missing temporary files and initial AI failure cannot publish incomplete content or duplicate slices.
- [x] 2.4 Implement PostgreSQL transaction support for snapshot adoption and publication row locking; verify concurrent adoptions, rollback on mid-copy failure, hide/adopt ordering and re-add after soft deletion against PostgreSQL.
- [x] 2.5 Document worker recovery, additive migration and rollback procedures; verify the documented retry flow works with an interrupted permitted PDF fixture.

## 3. Application

- [x] 3.1 Add dedicated DI handlers and validators for catalog PDF upload, URL preview/confirmation, metadata update, prepare/retry, admin listing/details and slice review; verify payload validation, source language, SSRF reuse and preparation boundaries with handler tests.
- [x] 3.2 Add reviewed publish/hide handlers with revision checks and actor/time records; verify incomplete drafts, published edits, stale versions and metadata/source changes invalidate publication eligibility as specified.
- [x] 3.3 Add published-only discovery and detail queries with conjunctive filters, bounded pagination, first-slice preview, stable ordering and ExistingBookId; verify private metadata, hidden entries and later slice text are absent from learner DTOs.
- [x] 3.4 Add atomic adoption handler without AI/fetch calls, returning BookId and AlreadyAdded; verify copied text/embeddings/provenance, fresh personal state and retry behavior, including separate-user independence.
- [x] 3.5 Propagate authenticated owner identity through affected book/slice/status/export/curation operations and learning entry points; verify cross-user identifiers are rejected in reader, audio, quiz grounding, pacer and graph tests while owned adopted copies remain usable after hiding.

## 4. API and Administrator Authorization

- [x] 4.1 Implement CatalogAdministration user-ID configuration, server authorization policy and authenticated capability endpoint; verify default-deny, active-user checks, configuration reload/restart revocation and spoofed client/registration/profile fields cannot grant access.
- [x] 4.2 Map learner and admin routes from design.md with trusted identity extraction, cancellation, rate limits, validation and RFC 7807 errors; verify 401/403/404/409, 202 preparation, 201 initial adoption and 200 duplicate adoption in endpoint tests.
- [x] 4.3 Document endpoint contracts in OpenAPI and administrator provisioning using placeholder .env-native keys; verify Scalar exposes the contracts without real account IDs/secrets and no public role-management endpoint exists.

## 5. Frontend

UI tasks in this group MUST remain unchecked until both the full npm test suite and relevant desktop/mobile screenshots have passed, been visually inspected and been presented. Read the canonical Vue, Vue best practices, Nuxt, Pinia, VueUse, Vitest, Vue testing and Antfu Design skills before frontend edits; apply only conventions appropriate to the existing Tailwind stack.

- [x] 5.1 Add typed catalog/admin API state with separate results, request race protection, pagination, status polling, capability refresh and logout/403 cleanup; verify store tests cover exact payloads, no duplicate submits, failed adoption retry and stale responses.
- [x] 5.2 Add My Library/Discover tabs, filters, catalog cards, source preview and Add/Open actions within the existing BoardLayout; verify URL/history state and adoption behavior in component/page tests and inspect English/Vietnamese desktop/mobile screenshots.
- [x] 5.3 Add discovery entry points to library/today/roadmap empty states while retaining personal import actions; verify new accounts receive no automatic books and inspect screenshots of the modified empty states in both locales and viewports.
- [x] 5.4 Build `/admin/catalog` using MasterDetailLayout with PDF/URL ingestion, metadata, processing/retry, slice review, publish and hide controls using AppSelect/AppModal/toasts; verify valid and invalid payloads, preserved input on errors, publication gating and authorized navigation in tests and bilingual desktop/mobile screenshots.
- [x] 5.5 Add capability-based navigation and admin route protection; verify anonymous redirect destinations, regular-user denial and permission-revocation cleanup in behavioral tests and screenshots without fake authentication or production bypasses.
- [x] 5.6 Complete English/Vietnamese messages and accessible focus/keyboard handling; run the full `npm test` suite and inspect Chromium screenshots at 1440x900 and 390x844 for discovery, preview, admin edit/review/error and empty states, presenting the captured images before checking any UI task complete. Use authentic sessions and current cookie names, including legacy migration where relevant; do not assert CSS classes in Vitest.

## 6. Integration Verification

- [x] 6.1 Run `dotnet test` and the full frontend `npm test`; exercise admin import/preparation/publication followed by a real learner's adoption and immediate reading with a permitted fixture, documenting results without claiming unexecuted checks.
- [x] 6.2 Exercise two learners adopting the same source, independent notes/review activity, hide/republication, delete/re-add, restart recovery and legacy personal import regression; verify no cross-user data, no partial copies and no automatic pacer switch.
- [x] 6.3 Measure adoption transaction duration/storage on a representative large document and verify cancellation rollback; confirm no external generation occurs during adoption and retain measurements with the implementation verification report.
- [x] 6.4 Run strict OpenSpec validation, compare completed behavior to every scenario and confirm both UI verification gates have evidence before declaring implementation complete.
