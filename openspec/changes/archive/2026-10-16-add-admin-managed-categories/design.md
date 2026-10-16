# Design

## Context

See `proposal.md` for motivation and `specs/document-categories/spec.md` for the shared contract. The target is the existing .NET 10 backend and Nuxt 4 application, not a new service.

Prerequisite: `add-database-backed-roles` must be implemented and accepted before this change. Its database authority, effective capabilities, destination registry, presented-session mutation guard and independently clean rejection writer are the baseline. These artifacts preserve its role-assignment and restricted-role behavior when extending shared specs.

Observed integration points:

| Area | Current evidence | Consequence |
| --- | --- | --- |
| Document persistence | `DocumentBook.Category` and `CatalogDocument.Category` use a varchar converter in `Infrastructure/Persistence/Configurations/EntityConfigurations.cs`; it recognizes BackendDotNet/DotNet and defaults unknown values to BackendRuntime | Read raw stored values for migration; never use that lossy converter for preflight |
| Learning persistence | `QuizQuestion.Category` and `TechInsight.Category` are integer columns; `Data/tech-insights.json` uses ordinals | Use explicit integer and string mappings, not one cast or one database type assumption |
| Ingestion queue | `IPdfIngestionQueue.PdfIngestJob` carries the enum | Drain writers/workers before changing contracts; do not deserialize old in-memory work into the new model |
| Term explanations | `TermExplanationCache.Category` is free text, including Software Architecture/Postgres; `ExplainTermRequest.Category` is a context string | This is not a taxonomy foreign key and must not be treated as an unknown enum |
| Administration | Current source uses IAdminPermissionResolver, three configured allowlist consumers and account-specific audited commands; the RBAC predecessor replaces those authority paths | Extend the accepted database permission/transaction contracts; do not reintroduce the historical allowlist |
| Frontend | Numeric/string category maps in Library/catalog/review/quiz/insights; category-name selectors in both graph renderers | Cut over every active consumer; a database-only category table would leave the feature hardcoded |
| Profiles | `/profile` redirects to Settings; no active `DomainGoalTracker` or category statistics API exists | Retire obsolete four-pillar constraints in specs; do not build a new widget/API |
| API generation | `frontend/app/types/api.generated.ts` exists, while `gen:api` writes `types/api.generated.ts` | Correct the generator destination and regenerate the canonical file; do not leave two conflicting generated contracts |

A current specification conflict needs an explicit resolution: Library requires exact raw Markdown bytes for Engineering Craft and forbids replacing the source field, while AI-curation requires persisted, structurally restored verbatim Markdown across all categories. `CurateSliceHandler` follows the latter, but several worker destinations still branch on EngineeringCraft. This design adopts the existing common structural-restoration contract for future admitted ingestion/explicit curation, preserving author words and language and keeping auxiliary synthesis separate. It does not bulk-reformat historical documents.

## Goals / Non-Goals

**Goals:**
- One category identity across database, API, URL state and visualizations, with metadata edits independent of content identity.
- Database-enforced references plus serialized assignment/deactivation/deletion decisions, not UI-only validation.
- A reversible, rehearsed coordinated cutover with no period of mixed enum/UUID writers.
- Reuse current auth, audited mutation, layout, error and localization conventions.

**Non-Goals:**
- No category hierarchy, merge/reassignment wizard, multiple categories per document, per-user category ownership, or tag-management feature.
- No user-editable prompts, category-specific AI persona settings, automatic category inference, new background jobs, live-push metadata protocol, or profile telemetry feature.
- No changes to source-type, difficulty or retention enums, SM-2 rules, published content snapshots, role-definition management, unrelated security findings or broader hardcoding cleanup.

## Decisions

### 1. Persist a first-class flat category aggregate

Add `DocumentCategory` in Domain and `DocumentCategories` in PostgreSQL. Use the existing base entity timestamps/soft-delete convention. The aggregate contains:

| Domain field | Wire field | Constraint |
| --- | --- | --- |
| Id | id | UUID; stable and never reused |
| Slug | slug | Immutable; trimmed lowercase ASCII kebab case, length 1–100; unique while not deleted |
| NameEn / NameVi | nameEn / nameVi | Both required, trimmed, 1–100 Unicode characters; plain text |
| SortOrder | sortOrder | Integer 0–10000, default 0; ties broken by slug |
| IsActive | isActive | Explicit boolean; new categories default active |
| ConcurrencyVersion | version | Positive integer, starts at 1, incremented on every committed mutation |

Use a partial unique slug index where `IsDeleted = false`, check constraints for required bounds, and an active/order index as useful for reads. Duplicate display names are permitted; identity and slug, not translated names, disambiguate categories. A deleted slug may be reused by a new UUID; an inactive slug remains reserved. Historical event identifiers are never repointed.

Replace Category on `DocumentBook`, `CatalogDocument`, `QuizQuestion` and `TechInsight` with non-null UUID `CategoryId` and restricted foreign keys. Replace existing category-leading filter indexes with equivalent CategoryId indexes, including question category/level and insight category/publication indexes. Cards/highlights/chunks derive their category through the existing source relationship; do not add duplicated category columns merely to render badges.

Alternative rejected: store only a mutable category string. It makes renames rewrite relationships and cannot distinguish history after reuse. An enum plus database extension table is also rejected: it creates two competing taxonomies.

### 2. One descriptor, explicit assignment, no legacy runtime aliases

Use `CategoryDescriptorDto { id, slug, nameEn, nameVi, sortOrder, isActive, version }`. Category-bearing content DTOs expose `categoryId` plus a `category` descriptor; no parallel numeric category field remains. Nullable category fields are allowed only for an artifact whose source provenance genuinely cannot establish a category, such as an orphaned review card.

New import/draft forms start unselected and send `categoryId` for Markdown, PDF multipart, URL, remote PDF, catalog metadata, filters, standalone quiz and insight generation. Invalid/missing UUIDs return validation errors before admitting work. Crawlers return extracted source metadata, not a category guess; preview never overwrites the user's selection.

All assignment paths use the same rules:
- New document/draft, new catalog adoption, changed book/draft assignment, and standalone quiz/insight generation require an active category.
- Existing documents retain their inactive category for display/filtering, reading, source retry, explicit curation and source-grounded practice.
- An unchanged inactive assignment is valid when editing title/language/attribution. Comparing IDs, not whether a selector emitted a field, determines whether reassignment occurred.
- A catalog entry already admitted before deactivation can finish preparation and publication. Published entries remain discoverable/previewable; new adoption is blocked while inactive. Returning an already-owned copy remains idempotent. Re-adoption after deleting the personal copy is a new assignment and requires active status.
- Specific-book quiz generation derives CategoryId from the authorized book. A contradictory explicit CategoryId is a validation error. Topic-grounded standalone generation requires an explicit active CategoryId; no topic keyword inference remains.
- Reclassifying a book updates source-derived graph/card metadata through the book relationship. It does not bulk-reclassify unrelated questions/insights merely because they share the old category. `QuizQuestion` currently has no BookId provenance FK; historical standalone questions retain their assigned UUID rather than inventing one during migration.

Keep existing routes: `/api/v1/library/books`, `/api/v1/library/upload-pdf`, `/api/v1/library/import-document`, `/api/v1/library/import-remote-pdf`, existing `/api/v1/catalog` and `/api/v1/catalog/admin` routes, `/api/v1/quiz`, `/api/v1/insights`, `/api/v1/review`, `/api/v1/graph` and the admin document inventory. The change concerns category payload/query fields, not a route redesign. Update endpoint OpenAPI schemas, manual consumer interfaces, worker payloads, samples and active URL parsers together. Obsolete numeric/enum filter state is cleared, not maintained through aliases. Do not clear unrelated reader/preferences/progress storage.

### 3. Serialize assignment against status/deletion with PostgreSQL row locks

A foreign key prevents physical orphaning but does not enforce active status or soft-delete semantics. Introduce a narrow Application category-assignment guard implemented in Infrastructure with the same scoped DbContext and current write transaction. Follow the existing document mutation-guard callback style instead of creating a generic transaction framework.

For a new standalone assignment, acquire `SELECT ... FOR SHARE` on the target category row, validate existence/nondeletion/active status, perform the authoritative content mutation, and hold the lock until that transaction commits. Do not use only `FOR KEY SHARE`: it does not serialize a non-key IsActive update. Administration obtains `FOR UPDATE` before version/usage checks and mutation. Book-derived practice instead revalidates the authorized source book and its category and can retain an inactive assignment. This exception is never a caller-controlled flag or inferred merely from the presence of BookId.

Use category IDs in sorted order before content aggregate database row locks. For privileged writes, the RBAC predecessor's fresh shared actor lock and presented-session/grant check precede category locks. Any existing outer document mutation lease remains first; then actor when privileged, category, and document rows. Category administration takes no document lease. Refactor CatalogAdoptionService's current document-first transaction: read candidate CategoryId, lock it, then freshly lock/read the catalog source and verify unchanged CategoryId/publication eligibility. Changed assignment returns conflict instead of locking a new category in reverse order. Apply equivalent final source revalidation to book-grounded generation; never authorize using tracked/cached descriptors.

For long AI/network work, validate first without a database lock, generate outside the transaction, then repeat authoritative checks in the final short transaction. Standalone generation requires the category still active; if deactivation wins, return `Category.Inactive` and persist nothing. Authorized book-derived practice rechecks the owned nondeleted source, unchanged authoritative assignment and eligibility and may proceed when that category was inactive initially or became inactive during generation. Changed sources return a conflict; arbitrary BookId values cannot bypass admission. This allows rare wasted standalone generation rather than provider-duration locks or reservation jobs. Admitted ingestion retains its committed assignment after deactivation.

Deletion takes the exclusive category row lock and checks all four referencing tables with query filters bypassed, including hidden and soft-deleted records. Any reference returns `Category.InUse`; there is no cascade or bulk move. New assignments cannot pass their shared guard until deletion commits, then see the category as deleted. Retained audit identifiers do not count as live foreign-key references and remain readable after an unused category is deleted. Free-text term context is not a category reference.

### 4. Extend administrative permission, audited commands and API conventions

Add AdminPermission.ManageCategories, canManageCategories and a CategoryAdmin policy/handler to the predecessor's database authority. A controlled migration explicitly grants the new key to protected Admin; other roles receive no automatic grant. Preserve all six existing capability fields and their meanings, including independent dashboard admission and conjunctive role assignment. Do not infer category permission from role names, catalog authority or browser flags. Reuse the current actor/session checks and 401/403/503 distinction.

Add category use cases/validators and Minimal APIs. Extend audited execution with category-focused commands rather than forcing them into the account callback. Success, category version and event commit together; failure rolls everything back. Reuse the predecessor's independently clean rejection writer only after rollback and failed transaction disposal. Staged mutations must not flush again, rejection-audit outages return unavailable, and no same-context nested transaction or swallowed failure is accepted. Category tests cover this integration; do not reimplement the predecessor's account/role commands or rewrite historical audit JSON.

| Endpoint | Contract |
| --- | --- |
| GET `/api/v1/categories` | Authenticated; all nondeleted descriptors, including inactive; sort by sortOrder then slug; no usage/owner/audit information |
| GET `/api/v1/admin/categories` | CategoryAdmin; validated search up to 200 characters, optional active status, page default 1, pageSize default 20/max 100; deterministic order and total count |
| GET `/api/v1/admin/categories/{id}` | CategoryAdmin; descriptor plus aggregate usage counts/canDelete, including retained soft-deleted references; no private document titles/content |
| POST `/api/v1/admin/categories` | CategoryAdmin; slug, both names, sortOrder, isActive; HTTP 201 and Location plus descriptor |
| PATCH `/api/v1/admin/categories/{id}` | CategoryAdmin; both names, sortOrder, isActive and required positive expectedVersion; HTTP 200 descriptor; id/slug immutable |
| DELETE `/api/v1/admin/categories/{id}?expectedVersion=N` | CategoryAdmin; required current version, server-enforced reference check, HTTP 204 on soft deletion |

Keep validation failures HTTP 400, missing/deleted admin targets 404, conflicts 409, unauthenticated 401, denied 403, unavailable persistence/audit 503. Include stable RFC 7807 codes `Category.ValidationFailed`, `Category.NotFound`, `Category.SlugConflict`, `Category.Inactive`, `Category.InUse`, `Category.ConcurrencyConflict` and `Category.Unavailable`. An assignment to an unknown/deleted UUID is a field validation error; administration lookup of that UUID is 404. Unique-index and EF concurrency exceptions must be mapped, not leaked. Do not implement automatic mutation retries.

Apply these category mappings to existing import, book-edit, adoption, quiz and insight routes as well as the new module. Their generic HTTP 400 branches must not misreport category conflicts. Extend the shared mapper/OpenAPI declarations only for the new category errors and assert actual statuses for initial rejection and post-AI races.

Apply named per-authenticated-user limits to the new endpoints using existing ASP.NET rate-limiter infrastructure: 120 reads/minute and 30 writes/minute, with 429/Retry-After. Do not refactor unrelated endpoint limiting. Administrative and shared authenticated category responses use private/no-store policy; no public/CDN storage of identity-bearing responses. Existing bearer/session/CSRF boundaries remain unchanged.

Audit actions are Category.Create, Category.UpdateMetadata, Category.Reorder, Category.Activate, Category.Deactivate and Category.Delete. A request changing only order or status uses the specific action; a multi-field PATCH uses one UpdateMetadata event listing approved fields and resulting state/version. No-op PATCH returns the unchanged descriptor without version increment or mutation event. Audit views add localized actions/target type and filters but retain ReadAudit permission. Payloads exclude raw forms, private usage lists and source content.

### 5. Separate free-text explanation context from taxonomy

Rename `ExplainTermRequest.Category` and the corresponding term service/entity field to `ContextLabel` (`contextLabel` on the wire); rename the cache column rather than maintaining an alias. This is contextual free text, not a category FK, with no seed-value mapping. Validate the 255-character bound before service calls and preserve accepted 201–255-character labels rather than silently applying the current 200-character context truncation. Missing context does not invent Software Architecture; use available source/chapter context or an empty optional label.

Preserve valid historical explanations, embeddings, hit counts and labels byte-for-byte through the rename and first startup. Existing documented hygiene deletion of known poisoned boilerplate entries remains unchanged; this change does not preserve records that current core-platform invariants require purging. Test both valid retained rows and existing poison predicates. Keep the current cache lookup algorithm/failure contracts, not a contextual-cache redesign. Reader category metadata remains separate, and category renames trigger no cache rewrite, embedding or AI request. Do not add a guessed optional cache CategoryId.

Alternative rejected: map arbitrary cached labels such as Postgres into the seven seeded categories. That conflates contextual text with identity, fails legitimate data, and changes historical vector semantics.

### 6. Shared Nuxt metadata and accessible administration

Add `useCategoryStore` with normalized immutable descriptor records, ordered categories and active assignment choices, a UUID lookup and current-locale name resolver. Fetch on authenticated entry to dependent surfaces; deduplicate concurrent loads but surface failures. Do not persist a bundled category fallback or long-lived enum map. Content DTO descriptors support rendering; the freshest shared store entry wins for an ID. Successful admin mutations refresh shared metadata and affected views in the same session; another browser sees changes on its next refresh/navigation, not through a new push channel. Clear privileged category drafts/requests on logout or account replacement using existing request-generation safeguards.

Add `/admin/categories` inside AdminShell with MasterDetailLayout, bounded list/detail/forms and custom lifecycle confirmation. Reuse AppSelect, AppModal, useToast and destination admission. AppModal alone does not currently prove focus containment: reuse/extend the caller-owned AdminShell pattern. Focus enters a named dialog, Tab/Shift-Tab stay within it including teleported listbox interaction, and background controls cannot receive focus. First Escape closes an open listbox only; subsequent Escape cancels the dialog. Cancel restores the trigger; successful removal focuses a surviving list item or list heading instead of a removed trigger.

The actual focusable combobox exposes its localized label, value, expanded/required/invalid states and associated help/error text; wrapper attributes alone are insufficient. Category chips expose selection programmatically, such as aria-pressed. Announce inactive status without disabling historical filters. Use visible focus, polite announcements, text restrictions, minimum 24px targets and measured 4.5:1 text/3:1 essential-UI contrast. Verify effective 320 CSS-pixel reflow. Actions, badges and footers keep whitespace-nowrap/shrink-0 with responsive gaps. Place long names in wrapping nonbadge text or bounded badges with full keyboard/click disclosure, never hover-only clipping; use required body typography.

Preserve failed drafts. Announce version conflicts and block resubmission until explicit refresh succeeds; show current server values without silently replacing inputs, then require deliberate submission using the refreshed version. Failed refresh preserves the recoverable draft. If an assignment becomes inactive, retain source/form inputs, explain the rejection, refresh choices and require an explicit active replacement. Disabled submission has an adjacent explanation and operable retry. Usage counts are advisory/admin-only; deletion rechecks server-side. Duplicate localized names expose immutable slugs as visible and accessible secondary identification.

Library/Discover quick filters use All plus the first four active categories by sortOrder/slug as a compact subset, not four named curriculum pillars; advanced filters expose all nondeleted categories and retain selected inactive filters. Import forms show only active categories; edit forms additionally include the current inactive category. If zero active categories or metadata is unavailable, show an actionable localized state and disable only new-assignment submissions, not existing reading/review. Admin document inventory filters and badges must use the same metadata, without adding private preview actions.

Update all live manual TypeScript contracts and category-dependent helpers. `shikiHighlighter.detectCodeLanguage` must use code fences/content or a neutral plaintext fallback, never a category slug/UUID mapping to an assumed programming language. Correct `gen:api` to write `app/types/api.generated.ts` and regenerate from the running backend OpenAPI document. Retire stale generated enum definitions and old fixed category translation keys only after their active consumers migrate. Preserve actual insight/highlight tag strings; this is not tag cleanup.

### 7. Dynamic graphs and category-independent curation

Graph hubs/count keys use UUIDs and only authorized surviving artifacts, including inactive categories. One existing-theme palette hashes UUIDs consistently across engines/minimap/drawer/legend, independent of label/order. Repeated colors are allowed; duplicate names require slug disambiguation. Existing DOM controls/detail drawer expose represented category name/slug, inactive status and existing counts with keyboard filtering/detail access in either engine; canvas labels alone are not screen-reader semantics. This does not claim whole-graph AA remediation. Add no admin CSS/color/icon customization or palette-based count ceiling. Omit absent subtitles rather than invent descriptions; preserve physics, idle rendering, edges and ownership.

Remove category checks in PersonalBookDestination, CatalogDocumentDestination, AdminImportCatalogUrlHandler, GetTodayFocusHandler, Gemini formatting/insight prompts and inference helpers. Preserve existing explicit curation gates, provider configuration, cancellation propagation and failure behavior. Source language and source content determine appropriate narrative/code/scenario treatment. All paths persist faithful restored source Markdown consistently, with no author-prose condensation, no auxiliary summary substituted for the source body and no category-change-triggered regeneration.

Every normal quiz candidate reuse/fill path and provider-failure recovery respects the authoritative CategoryId plus existing ownership/mastery boundaries. Test identical topic/level across different categories; never return or relabel the other category's questions. Preserve core-platform's prohibition on canned/template question persistence. Genuine matching stored questions can be reused; otherwise report failure, not a fabricated fallback.

## Risks / Trade-offs

- **Breaking clients/bookmarked numeric filters** -> Coordinated backend/frontend release; clear obsolete category-only filter state, document the intentional incompatibility and do not retain numeric aliases.
- **Corrupt legacy strings or out-of-range ordinals** -> Raw-SQL preflight including soft-deleted rows stops before destructive changes; fix the data explicitly, never guess a category.
- **Check-then-insert race and soft deletion** -> Shared/exclusive row-lock protocol in every assignment path, fresh state checks, restricted FKs and real PostgreSQL concurrency tests.
- **Stale forms or cross-browser metadata** -> Version conflicts and fresh server validation; local refresh after mutations, next-navigation refresh elsewhere. No automatic retry or optimistic success notification.
- **Curation policy disagreement in existing specs** -> Apply both Library and AI-curation deltas together, prove source-word/language preservation on narrative/code fixtures, and do not reformat existing documents as part of migration.
- **Palette collisions with many subjects** -> Stable readable labels and geometry; no promise of a unique color for every category.
- **Large or busy database** -> Measure inventory/counts and migration duration on a restored copy before scheduling the bounded maintenance window. Do not assume a backfill size or online migration requirement.
- **Rollback after new categories receive writes** -> Do not reverse-map new categories into old enums; restoring the old database snapshot would discard post-cutover writes. Keep writes closed until acceptance, then use forward repair or a separately approved recovery plan.

## Migration Plan

### Explicit seed mapping

Use fixed category UUIDs in migrations and example insight category references; do not replace insight IDs. Insert categories once; startup must not overwrite their metadata or recreate intentionally deleted unused categories.

Existing insight seeds have random UUIDs and are identified by slug. Keep that seed-slug provisioning identity: query all matching rows including soft-deleted ones, insert only if none exist, and never overwrite category/content/publication/deletion/timestamps on a match. Preserve every existing insight UUID and bookmark reference, even with multiple historical slug matches. Verify first-startup preservation on legacy random-ID fixtures; valid term-cache rows survive and documented known-poison hygiene remains unchanged.

| Legacy ordinal | Stored enum / recognized aliases | UUID | Slug | Initial English / Vietnamese name | SortOrder |
| --- | --- | --- | --- | --- | --- |
| 0 | FrontendWeb | 10000000-0000-4000-8000-000000000000 | frontend-web | Frontend & Web / Frontend & Web | 0 |
| 1 | BackendRuntime, BackendDotNet, DotNet | 10000000-0000-4000-8000-000000000001 | backend-runtime | Backend & Runtime / Backend & Runtime | 1 |
| 2 | DatabaseStorage | 10000000-0000-4000-8000-000000000002 | database-storage | Database & Storage / Cơ Sở Dữ Liệu & Lưu Trữ | 2 |
| 3 | SystemDesign | 10000000-0000-4000-8000-000000000003 | system-design | Distributed Systems / Hệ Thống Phân Tán | 3 |
| 4 | EngineeringCraft | 10000000-0000-4000-8000-000000000004 | engineering-craft | Clean Code & Software Design / Mã Sạch & Thiết Kế Phần Mềm | 4 |
| 5 | MentalModels | 10000000-0000-4000-8000-000000000005 | mental-models | Mental Models & Decisions / Mô Hình Tư Duy & Quyết Định | 5 |
| 6 | HabitsProductivity | 10000000-0000-4000-8000-000000000006 | habits-productivity | Habits & Deep Work / Thói Quen & Tập Trung Sâu | 6 |

Initial labels are taken from the current Library locale data. Recognize only these names/aliases, compared case-insensitively after trimming; ordinals apply to integer columns only. Unknown, empty and null values in the four taxonomy columns abort preflight. The unrelated Cloud & DevOps translation entry is not evidence of an eighth persisted enum category and does not create an extra migration row.

### Deployment and rollback sequence

First verify the RBAC prerequisite and compose its accepted shared specs. Extend required-schema readiness checks to categories outside the current caught migration-warning block: a failed category preflight/migration or missing required category schema prevents API and worker admission. There is no warning-and-continue route to running UUID consumers on enum storage.

1. Produce a raw SQL inventory of distinct category values and row counts in DocumentBooks/CatalogDocuments/QuizQuestions/TechInsights, including soft-deleted rows. Separately count term-cache rows; do not pass their free text through enum validation. Record all row counts, UUID mappings, content/progress invariants and existing FK/index definitions in rehearsal output.
2. Rehearse on a PostgreSQL backup copy containing string aliases, all ordinals, hidden/soft-deleted references and free-text term-cache entries. Prove unknown-value failure leaves the schema/data unchanged. Measure the window before selecting production timing.
3. Build matching backend/frontend artifacts in advance. During the approved window, stop request writers and ingestion admission, drain in-flight extraction/AI/queue work and stop workers. Take and verify a restorable database backup; preserve pending-source/job state needed for restart. Do not rely on old in-memory enum jobs surviving process replacement.
4. Apply transactional EF schema/data migrations: create/seed DocumentCategories, add nullable CategoryId columns, backfill by the explicit mapping, validate zero unmapped/null references and unchanged counts, install restricted FKs/indexes, set NOT NULL, rename term context metadata, then remove obsolete Category columns/converters. Expand/backfill/contract are phases of one maintenance cutover, not separately deployed dual-write versions. Do not edit historical migrations or audit JSON.
5. Start the matching backend only after required schema checks succeed, fetch actual OpenAPI, regenerate/verify the canonical client contract and deploy matching frontend/PWA assets. Restart nginx after rebuilt containers. Resume only new UUID-capable workers after database/readiness checks; old and new versions must not share the writable schema. Verify startup preserves existing insights, valid term-cache rows and category metadata while retaining known-poison hygiene.
6. While general writes remain closed, exercise the authenticated lifecycle against a controlled smoke account/database path: create a novel category, import/prepare representative source content, publish/adopt, read/review/inspect graph, rename, deactivate, retain existing access, reject new assignments and referenced deletion, activate again, and delete a separate unused category. Prove unauthorized access/audit rollback/concurrency and both locale/viewport gates described in tasks.md. Keep any smoke data isolated and account for it before reopening writes.
7. Before reopening public writes, rollback means stop new services, restore the verified pre-cutover database backup and previous matched backend/frontend assets, restart the proxy when applicable and verify old-version health. Rehearsal must demonstrate this restore. Once public UUID-category writes are admitted, rollback by an enum Down migration is unsupported; use forward repair or separately approved backup recovery acknowledging write loss.

No runtime execution, source edits, migration application or deployment is authorized by this planning workflow.
