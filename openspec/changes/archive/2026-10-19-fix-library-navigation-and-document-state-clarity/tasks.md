# Tasks

This is an implementation checklist, not authorization to implement. Start it only after a separate apply request. All boxes remain unchecked until the corresponding behavior and required verification are complete.

## Required skill reads

Before starting a task, use the `read` tool to load every skill named by its **Required reads** entry below. Skill IDs resolve to the exact paths in this catalog. A skill already read in the same apply session need not be reread unless it changes; each delegated agent must read its own required set because it starts with separate context. Reading a skill does not authorize package upgrades, workflow changes, production actions, or work outside this change.

| Skill ID | Exact skill path | Applies to |
|---|---|---|
| APPLY | `skill://openspec-apply-change` | Starting and tracking this change's implementation. |
| IMPACT | `skill://change-impact` | Caller/contract impact and matching documentation. |
| BACKEND | `skill://dotnet-patterns` | C# requests, queries, DTOs, nullability and cancellation. |
| API | `skill://api-design` | Optional query parameters, response schema and errors. |
| CSHARP-TEST | `skill://csharp-testing` | xUnit, handler and real API regressions. |
| TDD | `skill://tdd-workflow` | Failing-before/passing-after bug regressions. |
| ROUTER | `.agents/skills/vue-router-best-practices/SKILL.md` | Route queries, history and component remounts. |
| VUE-TEST | `.agents/skills/vue-testing-best-practices/SKILL.md` | Vue behavioral tests and realistic async/router fixtures. |
| I18N | `skill://i18n-sync` | Paired English/Vietnamese keys and terminology. |
| A11Y | `skill://accessibility` | Search, tabs, filter controls and status semantics. |
| BROWSER | `skill://browser-qa` | Real-surface interaction and screenshot inspection. Also read `xd://eval/browser` before driving the browser device. |
| VERIFY | `skill://verification-loop` | Integrated tests, types, build and lint evidence. |
| SECURITY | `skill://security-review` | Query validation, ownership, privileged metadata and content boundaries. |
| REVIEW | `skill://coding-standards` | Final code-quality review with the required specialist agents. |

**FRONTEND-CORE** means reading every canonical project file below before any `.vue`, composable, store, style, generated frontend type, or frontend test edit:

- `.agents/skills/vue/SKILL.md`
- `.agents/skills/vue-best-practices/SKILL.md`, plus its `references/reactivity.md`, `references/sfc.md`, `references/component-data-flow.md`, and `references/composables.md`
- `.agents/skills/nuxt/SKILL.md`
- `.agents/skills/pinia/SKILL.md`
- `.agents/skills/vueuse-functions/SKILL.md`
- `.agents/skills/vitest/SKILL.md`
- `.agents/skills/antfu-design/SKILL.md`

Use installed versions from `frontend/package.json` and its lockfile if a skill describes a different major version. In particular, do not adopt UnoCSS or upgrade Pinia/Vitest as part of these tasks.

Execution rules:

- Read `proposal.md`, `design.md` and all four change delta specs at apply entry. Before changing exported symbols or constructors, use LSP references and migrate every affected caller; do not keep obsolete signatures or compatibility aliases.
- Write meaningful bug regressions before their fixes. The integration owner records focused RED/GREEN evidence; delegated editing agents do not run build/lint/test/formatter commands mid-flight. Run broad checks after the integrated edits, not redundantly in every agent.
- Preserve authentic authorization and personal-content boundaries. Use local seeded test accounts/sessions, never fake production identities or production mutations.
- Do not mark frontend UI implementation tasks complete until task 2.7's behavioral and visual gates pass. Passing unit tests alone is insufficient.
- Domain entities, worker retry policy, Infrastructure persistence, migrations, source prose and adoption snapshots require no changes in this plan. A newly discovered need to change those boundaries requires a reviewed scope update, not an implicit extra task.

## 1. Application and API contracts

- [x] 1.1 Add failing producer-side regressions for language filtering and publication metadata; verify they expose the missing behavior before implementation changes.
  - **Required reads:** APPLY, IMPACT, BACKEND, API, CSHARP-TEST, TDD, SECURITY.
  - **Targets:** `backend/tests/DeepPace.Tests/Application/GetBooksHandlerTests.cs`, `AdminDocumentOperationsHandlersTests.cs`, and the existing document-operations API tests.
  - Cover combined owner/category/search/language selection and counts, declared language without slices, retained slice language, a learner-updated slice language that differs from the original declaration, Hidden/Ready catalog inventory and detail, and null visibility for both imported and adopted personal records. Keep existing hiding/snapshot/permission coverage rather than duplicating already protected scenarios.
  - Record a focused RED result for the new consumer-visible assertions, without CSS/source-text tests or tests that only echo mocked DTOs.

- [x] 1.2 Complete the authenticated personal-books language query across request, handler and endpoint; verify matching languages, filtered totals and invalid-language responses with task 1.1's regressions.
  - **Required reads:** IMPACT, BACKEND, API, CSHARP-TEST, SECURITY.
  - **Targets:** `Features/Library/GetBooks/GetBooksRequest.cs`, `GetBooksHandler.cs`, and `DeepPace.Api/Endpoints/LibraryEndpoints.cs`.
  - Accept optional normalized `en`/`vi`, treat empty as no filter, reject unsupported nonempty values with HTTP 400 problem details, and filter before count/paging. Use the same effective language rule for filtering and `BookDto.Language`: first nondeleted slice language in slice order, then declaration when no slices exist, then the legacy default. Preserve ownership, soft-delete, response-envelope and cancellation behavior.
  - Migrate constructor/use-case callers found by references rather than silently shifting positional arguments.

- [x] 1.3 Project nullable catalog publication visibility in operations inventory and detail; verify the same catalog ID exposes independent visibility/status and personal records remain publication-inapplicable.
  - **Required reads:** IMPACT, BACKEND, API, CSHARP-TEST, SECURITY.
  - **Targets:** `Features/Admin/Documents/DTOs/AdminDocumentDtos.cs`, `AdminGetDocumentInventoryHandler.cs`, and `AdminGetDocumentDetailHandler.cs`.
  - Add nullable catalog visibility using the existing enum wire representation. Keep personal visibility null, metadata allowlisting, permissions, retry codes/precedence and catalog publication transitions unchanged. No new stored state or synchronization work.

- [x] 1.4 Regenerate the frontend API types from the updated local API; verify generated language-query, category-descriptor and nullable-visibility contracts match the producer schema.
  - **Required reads:** API, IMPACT, FRONTEND-CORE, SECURITY.
  - **Targets:** existing OpenAPI output and `frontend/app/types/api.generated.ts`; use `npm --prefix frontend run gen:api` against the configured local API.
  - Do not hand-edit the generated schema, retain handwritten shadow DTOs, or upgrade generator/runtime dependencies. The generated contract must keep actual enum values such as `WebDocUrl`.

- [x] 1.5 Exercise and document the updated read contracts; verify live local API responses and the backend suite before accepting the producer work.
  - **Required reads:** BACKEND, API, CSHARP-TEST, VERIFY, SECURITY, IMPACT.
  - Run `dotnet test backend/DeepPace.sln` after producer integration. Exercise authenticated local GET requests for combined language filtering and Hidden/Ready operations metadata, including a personal record with null visibility. Confirm 400 for unsupported language and unchanged ownership/permission boundaries.
  - Update the relevant contract/status sections of `docs/admin-workspace.md` with authoritative visibility, processing-only status and no new synchronization job. Document only behavior observed in the local smoke; record no credentials or private source content.

## 2. Frontend navigation, state and presentation

- [x] 2.1 Add realistic failing frontend regressions for the reported interaction paths; verify failures reflect actual request/state or rendered-navigation differences before applying fixes.
  - **Required reads:** FRONTEND-CORE, VUE-TEST, ROUTER, TDD, I18N.
  - **Targets:** existing `frontend/tests/pages/library*.spec.ts`, catalog store suites, navigation suites, and operations page/store suites.
  - Cover search then clear, language/source-type reset, URL hydration, history and pagination, category navigation after search, supported-criteria tab switching, current-query result precedence, and the privileged learner desktop/mobile menus. Use a reactive router/realistic remounts, not just `replace()` call assertions.
  - Replace string category fixtures and invented source enum aliases in operations tests with producer-shaped descriptors and actual enum values. Cover Hidden/Ready vs personal-null visibility and localized metadata meaning; remove obsolete admin-link expectations instead of preserving the old behavior.

- [x] 2.2 Remove administrative destinations from the shared learner menu; verify privileged accounts see no admin link in either learner sidebar or mobile navigation without losing authorized admin access.
  - **Required reads:** FRONTEND-CORE, IMPACT, ROUTER, VUE-TEST.
  - **Targets:** `useNavigationMenu.ts`, its desktop/mobile consumers and the Discover capability request in `pages/library.vue` where obsolete.
  - Migrate affected references and remove the obsolete capability-dependent menu parameter/import/icon/branch. Retain capability APIs/actions still used by authorized administration, the admin shell and shared-session behavior.
  - Verify with task 2.1's navigation regressions and task 2.7's authentic desktop/mobile flow.

- [x] 2.3 Implement one route-driven committed query path and explicit filter clearing; verify URL, inputs, store, result page and requests agree across Enter, clear, reset, page changes, tab switches, remounts and browser history.
  - **Required reads:** FRONTEND-CORE, ROUTER, VUE-TEST, API, IMPACT.
  - **Targets:** proposed `useLibraryQueryState.ts`, `pages/library.vue`, `useCatalogStore.ts`, `useLibraryStore.ts`, and every affected list call site.
  - Preserve common URL criteria on tab switch, reset page, strip Discover-only source type on My Library, preserve unrelated query/fragment context, and distinguish category-only All from full Reset. Push committed user changes into history; use replace only for canonical cleanup. Use complete normalized request snapshots rather than merging hidden cached filters.
  - Forward personal language during both ordinary loads and background processing polls. Preserve existing stale-response protection and stop an older personal-list result from replacing the newer active query. Do not change global `app.vue` admission/page-key behavior.
  - Verify the request sequences from task 2.1, including an empty clear request after a nonempty search and reset without retained language/source type.

- [x] 2.4 Replace duplicated library filter rows with the focused shared toolbar; verify all controls remain operable with long bilingual categories and all omitted categories remain reachable.
  - **Required reads:** FRONTEND-CORE, A11Y, VUE-TEST, BROWSER.
  - **Targets:** proposed `components/library/LibraryToolbar.vue`, both library tab compositions, and existing filter-dialog integration.
  - Protect search and filter-action space; select a finite quick subset by shared metadata ordering, not hardcoded subject names. Retain the existing BoardLayout/custom-control patterns and source-format filtering only where supported.
  - Verify events/selection behavior with task 2.1's tests and geometry through task 2.7's screenshots. Do not assert Tailwind classes, wrappers or an incidental numeric chip limit as layout tests.

- [x] 2.5 Use generated operations contracts and independently labeled publication/processing metadata; verify catalog Hidden/Ready and personal-null cases render correctly in both the rail and selected detail.
  - **Required reads:** FRONTEND-CORE, API, I18N, VUE-TEST, A11Y, SECURITY.
  - **Targets:** `useAdminDocumentsStore.ts`, `pages/admin/documents.vue`, and existing shared category-name resolution.
  - Remove category-as-string shadow types. Render the descriptor's localized name, localized scope-based system ownership and actual source-type labels. Do not relabel unowned personal books as curated or translate external document titles.
  - Refresh inventory and selected detail from authoritative reads so a successful refresh cannot leave stale publication information in one view. Keep processing filters technical and catalog publication/editing actions permission-separated.
  - Verify realistic operations fixtures, category locale changes, no descriptor JSON, and unchanged personal-content exclusion.

- [x] 2.6 Synchronize English/Vietnamese scope, publication, source-format, owner and readiness messages; verify dictionary parity and correct meaning in actual operations views.
  - **Required reads:** FRONTEND-CORE, I18N, A11Y, VUE-TEST, BROWSER.
  - **Targets:** `frontend/app/i18n/locales/en.json`, `vi.json`, and their touched callers.
  - Use the agreed Vietnamese curated-document terminology and distinguish it from taxonomy categories. Replace the `AlreadyReady` readability claim with completed-processing guidance while preserving the machine-readable code and published-retry precedence.
  - Verify parsed locale JSON, matching keys/placeholders and both locale views. Remove raw `System Curated`/source-enum presentation without changing actual personal owner data or source prose.

- [x] 2.7 Pass the frontend behavioral and real-browser visual gates; verify and present EN/VI desktop/mobile proof before marking any affected UI implementation task complete.
  - **Required reads:** FRONTEND-CORE, VUE-TEST, ROUTER, VERIFY, BROWSER, A11Y, SECURITY.
  - After integrated edits run `npm --prefix frontend test`, `npm --prefix frontend run typecheck`, `npm --prefix frontend run lint`, and `npm --prefix frontend run build`; the Vitest suites must pass completely. Report actual coverage if collected, not an assumed percentage.
  - Launch the actual local application and drive Chromium through the browser device using authentic test-session state. Exercise both tabs' search/clear/reset, category/page changes, reload/Back/Forward and mobile navigation; observe requests and resulting content, not mocked-only results.
  - Capture, directly inspect and present My Library, Discover and operations Hidden/Ready at **1440x900 and 390x844 in both English and Vietnamese**. Include long category labels/more categories than the quick subset, reachable search/filter actions, correct category names, independent status meanings and no administrative learner links. A missing screenshot or failed behavioral gate leaves the UI tasks unfinished.

- [x] 2.8 Update learner/admin workflow documentation after the frontend smoke; verify the documented controls, route semantics and terminology match the exercised application.
  - **Required reads:** IMPACT, ROUTER, I18N, BROWSER.
  - **Targets:** the existing library section of `README.md` and relevant navigation/presentation sections of `docs/admin-workspace.md`.
  - Document Enter/clear/reset/All distinctions, common-criteria tab switching, complete personal-language filtering, separate publication/processing interpretation, Vietnamese scope terminology and retained personal-copy readability after catalog hiding. Do not add unrelated guides or repeat already documented producer details.

## 3. Integrated acceptance and review

- [x] 3.1 Exercise the end-to-end catalog/operations/owned-copy boundary; verify hiding changes current publication observations but not processing readiness or existing personal learning state.
  - **Required reads:** APPLY, IMPACT, API, BROWSER, VERIFY, SECURITY.
  - Use only an authorized local test fixture: publish/adopt/hide through existing supported flows, refresh operations, and observe Hidden plus Ready for the catalog source. Confirm discovery/preview/new adoption exclude it while the existing owned copy remains readable and unchanged.
  - Confirm operations-only permission still grants no catalog mutation or personal-content access, and normal learner/admin navigation retains its existing admission rules. Opening/filtering/refreshing must dispatch no AI generation or processing work.
  - Reuse task 1.5 and 2.7's successful suite evidence unless subsequent changes invalidate it; this integration task is not a reason to rerun unchanged checks gratuitously.

- [x] 3.2 Complete required independent reviews and acceptance reconciliation; verify every scenario is covered by observed evidence before requesting archive or delivery approval.
  - **Required reads:** REVIEW, SECURITY, VERIFY, IMPACT, APPLY.
  - Invoke `code-reviewer`, `csharp-reviewer`, `typescript-reviewer`, and `vue-reviewer` for their touched surfaces, plus `security-reviewer` for the input/query and privileged metadata boundaries. Resolve blocking/high findings and rerun only checks affected by corrective changes.
  - Reconcile the implementation against all four delta specs and the named scope boundaries. Validate the change with `openspec validate fix-library-navigation-and-document-state-clarity --strict`, retain actual backend/frontend/browser evidence, and do not claim implementation or UI completion while any required task/gate is unfinished.
