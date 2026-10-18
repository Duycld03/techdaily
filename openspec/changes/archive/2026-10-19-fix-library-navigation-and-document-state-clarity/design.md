# Design

## Context

See `proposal.md` for motivation and the four delta specs for behavioral requirements. This design is required because the fix crosses Nuxt routing, Pinia query state, generated API contracts, application queries, and two administrator projections.

Observed integration points:

- `frontend/app/composables/useNavigationMenu.ts` derives learner links from catalog capability state; `AppSidebar.vue` and `AppHeader.vue` consume it. Discover currently calls catalog capabilities on mount, which can reveal the administrative link for a privileged account.
- `frontend/app/pages/library.vue` has separate local search/filter refs, partial route hydration, and handlers that both navigate and fetch. `app.vue` keys the page by `route.fullPath`, so query changes can recreate those local refs.
- `useCatalogStore.fetchPublishedDocuments` treats omitted parameters as retained state. The page sends `undefined` when clearing search, language or source type, so old criteria survive. Discovery pagination does not currently update the URL.
- Both library toolbars render all shared categories beside a shrinkable input. The metadata store already orders categories by `sortOrder` and slug, and both existing filter modals expose all nondeleted category descriptors.
- `LibraryFilterModal.vue` actually offers category and language, not source format. `useLibraryStore.fetchBooks`, `LibraryEndpoints`, and `GetBooksRequest` do not forward/accept language. `GetBooksHandler` currently derives its response language from slices; `DocumentBook.DeclaredLanguage` is already available for incomplete imports.
- `CatalogDocument` already owns independent `Visibility` and `ProcessingStatus`. `Hide()` does not change processing status. Operations inventory/detail load the same catalog record but omit visibility; they also send the internal English owner label `System Curated`.
- Operations frontend interfaces incorrectly declare `category: string`, unlike the backend descriptor and generated OpenAPI schema. Tests in `frontend/tests/pages/admin-documents.spec.ts` and `frontend/tests/stores/admin-documents.spec.ts` repeat that invalid fixture shape.
- The workspace spec explicitly requires `Catalog` in both locales. It must change with the dictionary. The existing navigation spec excludes learner chrome from the admin shell but does not state the reverse isolation rule.

The exploration executed current page/store function bodies with stand-in reactive/router/API dependencies. This demonstrated query serialization failures, not a live Nuxt or authenticated Chromium result. Those observations do not satisfy implementation verification.

## Goals / Non-Goals

**Goals:**

- Make committed route state authoritative without changing global shell admission or page-key policy.
- Complete the offered personal-language filter and add catalog publication metadata through existing endpoints and generated types.
- Reuse existing category descriptors, status formatting patterns, layout archetypes, and accessible controls.
- Give every implementation task an explicit, resolvable prerequisite skill set and an observable completion condition.

**Non-Goals:**

- A new curriculum, search engine, debounce policy, per-tab persistent preferences, or a generalized filtering framework.
- New personal source-format controls, publication-state filters, realtime synchronization, or background refresh infrastructure.
- Changes to permissions, source retry eligibility/error codes, AI generation, publication transitions, adoption, or personal-content access.
- A database migration, language backfill, new UI framework, or whole-page refactor unrelated to these defects.

## Decisions

### 1. Remove administrative link construction at the learner navigation source

Remove the catalog-capability-dependent insertion from `useNavigationMenu`, its obsolete parameter/import/icon/active-route branch where no longer used, and migrate its consumers and tests. Remove Discover's capability request if reference analysis confirms it served only that navigation exposure. Do not remove the capability endpoint or store action while authorized admin consumers still need it.

Both desktop and mobile inherit the correction from the shared menu. Keep `AdminSidebar`, admin destination registration, admission, and shared-session switching unchanged. Removing menu links is information-architecture separation, not authorization enforcement.

Alternative rejected: hide the link only on Discover or with a CSS condition. That leaves the same defect on other learner pages and devices.

### 2. Use the active URL as committed query state

Use the existing query names: `tab`, `search`, `categoryId`, `language`, `sourceType` for Discover, and `page`. Keep My Library as the default tab. Preserve unrelated query keys and the route fragment when committing list changes; keep obsolete `category` cleanup limited to that obsolete key.

Extract only the library-specific parse/serialize and route-to-request orchestration needed by this page into a focused composable, proposed as `frontend/app/composables/useLibraryQueryState.ts`. It owns normalized committed criteria and user transitions; it does not own imports, book editing, preview or adoption. The page remains the surface composing the existing stores, toolbar, dialogs and content.

Search text can be a local editing draft until Enter; after commitment or navigation, the draft must match the URL. Use `router.push()` for committed user changes so Back/Forward restores prior criteria; reserve `router.replace()` for canonical URL cleanup without creating another history entry. Commit one route change per user action and load from the resulting normalized route. Avoid a handler fetch followed by a second independent route-watcher fetch. Account for the existing full-path remount rather than changing `app.vue` for the entire application. Preserve discovery's stale-response sequence protection and prevent an older personal-list completion from replacing a newer active query result.

Transition rules:

| Action | Committed query effect |
|---|---|
| Enter or search clear | Set/remove trimmed search; reset page; keep other supported criteria. |
| Select category or All | Set/remove category only; reset page; preserve search and other supported criteria. |
| Apply advanced filters | Set supported selected criteria; preserve committed search; reset page. |
| Reset | Remove search and all supported criteria; reset page; retain active tab and unrelated route context. |
| Change page | Retain criteria and put the selected page in the URL. |
| Switch tab | Retain common URL search/category/language, reset page, and remove source type on entering My Library. |
| Reload/Back/Forward | Recreate controls and requests from that destination's complete URL, not cached defaults. |

Retaining common criteria on tab switch follows the current setter's query-preserving convention. It is not a separate saved search per tab. Browser history restores the earlier complete route. This avoids new state persistence and namespace compatibility rules.

Reset and All have intentionally different meanings. The old library spec has contradictory descriptions: one says All clears category, while another groups All with full reset. Preserve the existing category-only All behavior and correct that contradictory scenario. Likewise, clarify that source format belongs to Discover; implementing a new personal source-format feature is not part of fixing the offered controls.

### 3. Make clearing explicit at the store boundary

Give the library list loader a complete normalized query snapshot on every route-driven load. Use empty search and explicit null/absence semantics for no category, language and source type, so a cleared criterion overwrites previous store state before query serialization. Do not merge the new active query with previously retained filters.

Update every affected discovery list call site after LSP reference inspection; remove obsolete competing local query state rather than keeping a compatibility adapter. Preserve existing response envelopes, published-only discovery, authenticated personal ownership, page sizes and error presentation. A late response must not overwrite the newer active query.

Alternative rejected: fix only the clear button to pass an empty string. That would not repair advanced reset, deep-link hydration, pagination, or full-path remounts.

### 4. Share a bounded toolbar presentation without changing the design system

Extract the duplicated search/quick-filter surface into a small library feature component, proposed as `frontend/app/components/library/LibraryToolbar.vue`, retaining `BoardLayout` and the existing filter dialogs. Props supply search draft, localized categories, selected category and filter count; emits describe search commit/clear, category selection and opening filters. The parent/composable owns navigation and requests.

Render a protected search/action row and a bounded quick-category region that can reflow at narrower widths. Select quick categories by existing metadata order with a finite presentation limit, not subject names. Keep every category in advanced filters. Choose the limit using actual bilingual desktop/mobile inspection, without making its numeric value a product contract or a unit-test assertion.

Use existing Tailwind tokens and components, not UnoCSS installation or a new theme. Preserve readable body/input sizes, localized control names, keyboard access, visible focus, and no-wrap action labels. Long category labels must not force horizontal page overflow or hide search/filter actions.

Alternative rejected: keeping all categories on one nonwrapping desktop row and suppressing overflow. That conceals controls instead of resolving their competing space requirements.

### 5. Complete personal-language filtering before paging

Add optional `Language` to `GetBooksRequest` and bind `language` in the existing authenticated books endpoint. Normalize supported nonempty values to `en` or `vi`; reject unsupported values with existing Result/problem-details conventions. Omitted or empty language means no criterion.

Use one effective-language expression for matching and `BookDto.Language`: preserve the first nondeleted slice language in slice order when slices exist, otherwise use recorded `DeclaredLanguage`, otherwise preserve the existing legacy `en` display default. `UpdateBookHandler` updates slice language without rewriting `DeclaredLanguage`, so preferring the declaration would regress a learner's existing language edit. The fallback still handles declared Vietnamese imports before slices exist and retained adopted copies without changing stored content or backfilling data. Apply the filter before `CountAsync`, `Skip` and `Take`; keep ownership and soft-delete predicates intact. Project only language metadata, not source text.

The no-slice declaration fallback applies only when a declaration is recorded. A record with neither slices nor a declaration keeps its existing legacy `en` label until language metadata becomes available; do not infer language from its title or source. Populating missing declarations in ingestion admission producers is outside this read-query fix.

Pass language through `useLibraryStore`, regular list loads and background polling so an active processing refresh cannot erase a visible language filter. Regenerate OpenAPI types using the existing `npm run gen:api` script against the updated local API, not manually copied interfaces.

Alternative rejected: filtering one downloaded page in the client. That would produce incorrect counts, pagination, and potentially misleading empty results.

### 6. Add visibility as metadata, not a second lifecycle

Add nullable `CatalogVisibility? Visibility` to operations inventory/detail DTOs. The API already uses `JsonStringEnumConverter`, and generated `CatalogVisibility` is `Draft | Published | Hidden`; keep that established wire representation. Project `c.Visibility` from catalog rows and null for both imported and adopted personal rows. Keep `status` as processing status.

Do not add a stored visibility field, migration, synchronization job, or mutation endpoint. Entering or explicitly refreshing operations must fetch the current producer projection; both selected detail and inventory must be refreshed without leaving a successful refresh showing conflicting observations. No realtime cross-tab guarantee is introduced.

Derive operations frontend inventory/detail types from `components['schemas']` instead of the current handwritten category-as-string contracts. Render scope, publication and processing with separately understandable localized labels in inventory and detail. Personal records have no publication pill. Keep processing filters explicitly technical; do not insert Hidden into them.

Keep retry codes and precedence unchanged. `AlreadyReady` copy should say processing is complete and source retry is unnecessary/unavailable, not that publication or new adoption is possible. Published retry denial remains its existing distinct reason. Operations-only permission does not acquire catalog editing or publication controls.

Alternative rejected: replacing Ready with Hidden or copying publication state into processing. Either would corrupt the worker lifecycle and retry interpretation.

### 7. Resolve display metadata from semantic scope and authoritative descriptors

Use `useCategoryStore.getCategoryName(descriptor, locale)` and retained current metadata to display category names, including inactive categories. Do not stringify descriptors, introduce category translation maps, or infer a default category when metadata is missing. Reuse the descriptor already present in each DTO; no category fetch per row.

Resolve system-curated ownership from `scope === 'Catalog'` in the frontend, not by comparing a magic English owner string. Actual personal owner names remain data; unowned personal records keep the unknown-owner meaning rather than being labeled system-curated. The English backend system label can remain non-authoritative metadata; display no longer depends on it. This avoids an unrelated owner wire-contract change.

Use `Tuyển chọn` for Vietnamese curated scope, `Tài liệu tuyển chọn` for expanded labels, and `Chuyên mục` for taxonomy labels in operations. Use a localized system label and source-format labels keyed by actual generated source enum values, including `WebDocUrl`, rather than fixtures' invented `Web`/`Pdf` aliases. Preserve source titles, author names and excerpts.

### 8. Protect behavior with realistic regressions and real-surface verification

Update existing backend `GetBooksHandlerTests`, operations handler/API tests, and relevant catalog hiding/snapshot coverage only where the new consumer-visible boundaries need protection. Include combined owner/language/category/search filtering with filtered counts, declared-language books without slices, retained slice language, nullable personal visibility, and catalog Hidden/Ready projection.

Frontend coverage belongs in existing library, discovery, catalog-store, navigation and operations suites. Use a reactive router or realistic route transitions so URL changes and remounts are exercised, rather than only asserting that a mocked `router.replace` was called. Replace string-category operations fixtures with generated-contract-shaped descriptors. Cover clearing after search, resetting language/source type, history/deep links, common-filter tab switches, pagination, newest-query results, privileged learner navigation, and localized status/category meaning. Remove obsolete behavior expectations rather than preserving the old admin-link branch.

Unit tests must not assert Tailwind classes, exact chip count, source text, or incidental copy to claim layout protection. After integrated changes, run the project test/type/build gates and exercise the real application in Chromium with authentic local test sessions. Inspect and present screenshots for both library tabs and the operations Hidden/Ready case in EN and VI at 1440x900 and 390x844. Include a long-label/category-overflow case and mobile navigation. No production mutation or fake production auth bypass is part of this change.

## Risks / Trade-offs

- Full-path remounts can erase draft-only search or duplicate requests -> keep committed criteria in the URL and make a single route-driven loading path; verify Enter followed by category/page changes.
- Shared URL criteria carry between tabs -> document this existing convention, reset page, strip Discover-only source type on My Library, and verify Back/Forward. Do not add per-tab preference storage.
- A stored declaration may lag a learner's language edit -> preserve slice-language precedence for existing books, use declared language only when slices are absent, and cover changed-language/incomplete/legacy/adopted cases without mutating source metadata.
- EF translation can differ between the existing SQLite test fixture and PostgreSQL -> exercise combined language filtering against the real local API/database as well as handler tests.
- A stale operations detail can contradict a refreshed rail -> refresh both affected observations from the same authoritative catalog identity and retain explicit stale/error feedback on failures.
- Existing tests encode invalid DTO shapes and obsolete navigation -> migrate realistic fixtures and consumer-visible expectations, not casts or new compatibility aliases.
- Skill documentation may describe newer major versions than this repository -> follow `frontend/package.json` and lockfile versions; do not upgrade Pinia, Vitest, Tailwind or other packages as part of reading a skill.

## Migration Plan

1. Implement the optional language query and nullable visibility response, update producer contracts, and regenerate frontend types before integrating the corresponding consumer changes. No database migration or historical data rewrite is required.
2. Complete learner navigation, route-driven loaders, shared toolbar and operations presentation; preserve the existing permission and personal-content boundaries.
3. Pass backend and frontend behavioral checks, typecheck/build, real API/browser smoke paths, and the bilingual desktop/mobile visual gate. Update the relevant README and admin-workspace documentation with query semantics, separate status axes and terminology.
4. Deploy the compatible backend before or together with its frontend consumer. Existing clients can ignore the added response field and omit the new query parameter. Do not deploy the new frontend alone against an older backend and claim the language/visibility behavior works.
5. Roll back the frontend first, then backend if necessary. No content, publication, ownership or learning data needs restoration because these changes add read-query/projection behavior rather than lifecycle mutations. Deployment is not authorized by this proposal.
