# Design

## Context

See `proposal.md` for motivation and the seven capability deltas for observable requirements. This design is required because the repairs cross authentication, PostgreSQL command admission, audit transactions, host startup and the Nuxt protected outlet.

Current integration points are already present:

- `CatalogEndpoints` and `AdminDocumentOperationsEndpoints` create application commands from validated principals; three command records currently default an omitted presented session version to 1.
- `AdministrativeUnitOfWork` provides account-control, category and privileged-mutation boundaries. Account commands use the DPACCTRL advisory lock and ordered `Users ... FOR UPDATE` locks; privileged mutations use actor-row admission compatible with competing account updates. Category rejection handling already separates transaction execution from independent rejection persistence.
- `AuthorizationReadinessService` distinguishes required schema readiness from initialization, but receipt exceptions become an absent receipt and Admin integrity uses a row count. The current installed grant set includes the category migration, not only the initial six grants.
- `Program` prepares schema, dispatches the explicit initialization command and then starts the web/worker host. `PdfIngestionWorker` can currently return permanently on failed readiness while the web process continues.
- `app.vue` owns the effective admin shell/outlet boundary; there is no separate admin layout to modify. `AdminAuthorizationBoundary`, the access store and Catalog currently use global administrative authorization in places that need destination permission. The API client already fences responses on session/account/admin-generation changes.
- The role option producer and generated frontend schema already expose `permissionKeys`. The handwritten consumer expects another field. The users store also couples role-loading cleanup to an unrelated directory generation.

The main workspace spec names `/admin` as the default dashboard and an older landing order; the current resolver uses `/admin/dashboard` and Dashboard, Catalog, Categories, Accounts, Documents, Audit. Both dashboard entry paths exist. This pre-existing discrepancy is recorded rather than silently resolved by a return-URL fix. Neither aliases nor landing precedence are changed here.

## Goals / Non-Goals

**Goals:**

- Place each final security decision inside the existing authoritative boundary and preserve the distinction between credential failure, permission denial, availability failure and authorized business rejection.
- Make rejection persistence independent of failed mutation tracking without weakening successful mutation/audit atomicity.
- Fix frontend admission at the outlet boundary, where a capability-state transition can otherwise render a forbidden page before a later redirect.
- Preserve public HTTP request/response shapes, JWT claims, error codes, current category grants and one-time initialization semantics.

**Non-Goals:**

- No role/grant editor, automatic privilege repair, repeat bootstrap, role migration, new accounts, password changes or fallback users.
- No new refresh-family protocol, automatic ingestion retry loop, health telemetry project, dependency, route redesign or generalized policy/transaction framework.
- No changes to `harden-frontend-quality-gates`, its toolchain, quality baseline or CI scope. Existing gates are consumed, not redesigned.
- Planning does not execute migrations, mutate running accounts, deploy, or implement these decisions.

## Decisions

### 1. Require the trusted presented version at internal command boundaries

For publication, ingestion retry and catalog source replacement, remove the initial-version default from the affected internal command records and migrate every constructor call and fixture. Hide and explicit review receive the same trusted presented-version input when moved under final admission. Endpoint construction uses only the session version already verified on the authenticated principal, with explicit named arguments where needed to avoid positional ambiguity.

Do not add a version to public request bodies, hardcode version 2, or fetch the actor's newer database counter as a substitute. The latter would allow an already-stale command to borrow authority it never presented. Keep HTTP success contracts and request-level 401 handling unchanged; a revocation that wins after request authentication must still prevent final mutation.

**Alternative rejected:** retaining `= 1` and patching only the three endpoint calls leaves future omissions silently insecure or unusable. Required internal arguments make omissions visible without adding a new authorization mechanism.

### 2. Reuse final actor admission for hide and explicit review

Wrap only the transactional hide/review mutation and its successful event in the existing privileged boundary, using current actor status, presented version and catalog grant. Preserve its actor-row locking relationship with account control: if revocation obtains admission first, the catalog action cannot commit; if the catalog mutation obtains admission first, its commit precedes revocation. Avoid unrelated document lifecycle changes or a second check that does not hold admission through commit.

Keep external provider work outside security locks. Direct publication remains explicit review; a separate `ReviewedByUserId` is not introduced as a publication prerequisite. Existing visibility, revision and eligibility checks remain authoritative within the mutation.

**Alternative rejected:** another middleware check only narrows the race window. It does not serialize the mutation with role/status/session changes.

### 3. Admit role-command actors before classifying attributable rejections

Move role-command business validation and lookup rejection out of unaudited handler exits into the existing account-control execution lifecycle. Add only the narrow pre-target validation callback needed by that boundary: it runs after locked actor/status/session/grant admission and before target existence/version comparison. Migrate affected callers and test implementations rather than retaining an obsolete overload.

This ordering preserves a nonpositive expected version as input validation, not a stale-target conflict. It also permits safe rejection history for an invalid reason or destination-role ID without claiming that a denied or stale actor was an admitted administrator. Apply existing reason bounds and sensitive-input rules before constructing/staging a successful event; invalid, excessive or sensitive reasons are omitted from rejected metadata, never truncated into history.

Keep target existence, expected-version and self-action safeguards authoritative in the locked boundary. Resolve the destination role and unchanged assignment within the locked role callback. Unknown persistence failures remain unavailable, not fabricated validation/concurrency results. Preserve distinct existing safe codes for missing target/role, self-change, stale version, unchanged assignment and protection failures.

**Alternative rejected:** adding an audit write to each early handler return duplicates policy and misses current-session admission. A separate preflight capability query is not proof of current actor eligibility at the transaction boundary.

### 4. Dispose failed transactions before clean rejection persistence

Reuse the category command's inner-execution/outer-outcome structure for account control. The inner execution owns the transaction; unsuccessful outcomes roll back, then leaving that scope disposes it. Only afterward may the outer outcome independently append an eligible Rejected event. Clear failed tracking and use a separate scope/context through the existing clean audit boundary. Remove the affected same-context fallback; no failed transaction, connection state or tracked target mutation may be reused.

For publication, stop saving rejection events inside `PublishInternalAsync` while the privileged wrapper is still active. Capture an eligible safe rejection only after the mutation callback was entered under actor admission, let the wrapper finish rollback/disposal, then append through the same clean boundary. An actor-admission failure must not be relabeled an authorized publication rejection.

Successful/accepted events remain in their mutation transaction. A clean rejection-write failure returns the existing safe unavailable result with correlation and no durable-history claim; exception/provider messages are not returned or copied into events. Persist exactly one eligible rejection, not both an inner and outer event.

**Alternative rejected:** saving before returning a failed result loses the event when the wrapper rolls back. Saving immediately after rollback while the transaction is still in scope retains the failed persistence boundary and can attempt nested work or accidentally resave mutations.

### 5. Protect actual administrator-removal transitions

Keep the DPACCTRL serialization and ordered actor/target locks. Apply the active-admin minimum only when an Active, non-deleted protected Admin would cease contributing to the minimum: suspension, or assignment to a different non-Admin role. Assignment-specific protection belongs in the locked callback where the destination role is known; suspension retains its corresponding shared safeguard.

Evaluate unchanged assignment before removal protection. Preserve self-role protection and stale-version admission. A Suspended protected Admin may be assigned Learner or another existing eligible role while remaining Suspended with unchanged suspension metadata; the actual role change still advances control/authentication versions and revokes old refresh families. The separate bootstrap advisory lock is not claimed to be the same lock as DPACCTRL.

**Alternative rejected:** counting active administrators whenever the target merely holds the Admin role overblocks Suspended demotion and unchanged assignments without preserving an additional recovery anchor.

### 6. Project the persisted target role without changing locking SQL

Load/project the target role by its persisted `RoleId` within the admitted account transaction before constructing suspension, reactivation or all-device-sign-out responses. Do not compose `Include(Role)` onto the raw `Users ... FOR UPDATE` query: a joined nullable navigation can change PostgreSQL locking behavior. A small separate role query retains clear row-lock semantics and works in fresh contexts without relationship fixup.

Remove the fabricated Learner descriptor fallback rather than hiding missing authority. A missing/deleted role cannot yield a successful misleading account response. Review every existing `ToDescriptor` producer, including authentication, refresh, initial Google provisioning and profile responses, and ensure each has authoritative role data without changing its wire shape. No lazy loading, global AutoInclude or role-name authorization is added.

**Alternative rejected:** keeping the fallback and loading roles only in tests reproduces the same production mismatch. Global eager loading expands unrelated reads and still obscures which producer requires role authority.

### 7. Validate required authority exactly, then gate startup and HTTP at the right phases

Read actual required role identities, stable keys/protection and exact-case grant keys. Readiness and initialization use the same explicit current protected-grant definition, including `ManageCategories` alongside the original six. Verify required-set inclusion, not row count or exactly-six equality. Additional supported grants are not substitutes for required ones; no future enum value is automatically granted or considered installed. Learner retains its empty grant set. A readable empty administrator-managed category table remains valid.

A successfully executed receipt query returning false means valid schema awaiting explicit initialization. A missing/unreadable receipt table or query failure means required readiness failed. Use safe readiness diagnostics and server-side exception logging rather than provider details in public Problem Details.

Perform the mandatory host readiness decision after schema preparation and explicit initialization-command dispatch, before starting HTTP and hosted workers. Operator initialization still performs its own schema/seed preflight and reaches controlled repeated-initialization refusal; it does not require an existing receipt or start workers. Ordinary host startup requires readable valid schema/seeds, not a completed receipt. Remove the worker's optional-readiness/quiet-return path: if required startup admission fails, it must not leave a running host advertising an absent consumer. A failure between preflight and worker startup must stop the host with an unsuccessful process outcome, not silently complete the service. Cancellation during normal shutdown is not a startup failure.

During HTTP operation, retain authentication first, then authenticated administrative availability admission, then grant authorization. No-store headers must be installed early enough for all administrative/capability 401/403/503 responses. Cover `/api/v1/admin/*` and existing catalog capability discovery consistently. Invalid/absent credentials follow the existing 401 path on a valid uninitialized deployment; valid credentials receive 503 for pending initialization before insufficient-grant evaluation. After initialization, insufficient grants still return 403. Runtime required-readiness failure remains unavailable and admits no affected work.

**Alternative rejected:** a worker startup retry loop adds a second operational state machine and can mask broken schema. Fail-fast gives a single restart boundary and prevents the known healthy-API/dead-consumer state. Requiring the completion receipt at host startup would incorrectly block fresh Learners and explicit initialization.

### 8. Admit the current destination reactively, not after an awaited global success

Keep capability discovery (`verify`) distinct from route authorization. In `app.vue`, derive protected-outlet admission from current-session confirmed capabilities plus the committed current destination. Global `isAuthorized` alone cannot mount an outlet. Preserve the mounted admin shell during same-session background checks while withholding a new unadmitted outlet.

`AdminAuthorizationBoundary` handles renewal/retry against the still-current route and actor generation. Definitive destination denial navigates through the existing safe admin-login flow; transient failure retains credentials and retry state. Check destination admission in the reactive outlet guard as well as the continuation, so publishing global authorization cannot create a forbidden intermediate render. An authorized-but-forbidden route must navigate, not remain in a permanent checking boundary. Catalog's own render/initial-load guards use its confirmed catalog capability.

**Alternative rejected:** checking the destination only after `await verify()` redirects too late if Vue has already scheduled the outlet from global state. Clearing all valid capabilities on every route change also causes shell flashes and breaks restricted roles.

### 9. Use one destination mapping for safe contextual return URLs

Reuse the registered destination mapping in `adminRoutes` and the access-store consumer instead of maintaining separate capability predicates. Validate the local return URL with existing safety rules, authorize its pathname, and navigate using the complete sanitized pathname/query/fragment. Use this consistently for post-sign-in and already-authenticated middleware entry.

Unknown routes remain denied. Query/fragment text never selects a grant. Preserve rejection of external/protocol-relative URLs, backslashes, control characters, traversal, ambiguous encoded pathnames and login destinations. Keep independent Categories access, both dashboard entry paths and current landing precedence unchanged.

**Alternative rejected:** dropping query/hash before navigation avoids the comparison bug by losing user context. Comparing the full return string to a route literal wrongly rejects a permitted destination.

### 10. Treat role discovery and conflict reload as owned asynchronous operations

Use only the producer's canonical `permissionKeys` in the handwritten role option type, modal and fixtures; remove consumer-only fields not present on the response instead of adding wire aliases. The existing localized heading key is presentation text and need not be renamed. Permission explanations are informational, never evidence authorizing assignment.

Role requests own a role generation and loading cleanup independent of directory pagination/search/filter/refresh. Only the latest role request may publish options/errors or settle loading; account/session/admin reset still invalidates outstanding results. A failed role request exposes localized retry rather than an unexplained empty selector.

A conflict reload clears the gate only after a non-null accepted detail for the same target and actor/session/admin generation has become the form's current inspected version. Include the current dialog interaction so closing/reopening or retargeting cannot be unlocked by an old completion. Preserve selected role/reason on failure or discarded reload. Successful matching reload updates the version but requires explicit confirmation; it never resubmits automatically. Existing unchanged-role blocking remains when the reloaded role now matches the selection.

**Alternative rejected:** treating promise resolution as reload success ignores the store's null/discard contract. Sharing the directory generation strands role cleanup when an unrelated account-list request supersedes it.

### 11. Verify consumer behavior, real interleavings and the actual rendered surface

Extend existing suites rather than build parallel wiring tests. Use public outcomes, persisted state and actual producer-shaped DTOs. Keep failing-before/passing-after regressions for the uncertain boundaries; replace the existing test that incorrectly expects Suspended-Admin demotion to fail. Never re-pin source text, Tailwind classes or incidental wrapper markup.

PostgreSQL tests use isolated databases, independent contexts and deterministic barriers to exercise both actor-admission orders, concurrent active-admin removal, rollback/disposal and clean rejection persistence. Do not silently return a passing test when the PostgreSQL fixture is absent. Fresh contexts must avoid navigation fixup masking role projection. HTTP tests authenticate fresh sessions with versions greater than 1, and use the real host pipeline for initialization precedence.

Runtime proof during apply must observe the actual host failing startup with invalid required authority, a later successful restart dispatching a pre-existing queued identity, and authenticated publication/retry/replacement behavior with renewed credentials. Use isolated QA identities and persisted fixtures, never production mutations, fabricated browser tokens or configuration allowlist bypasses. For transient authorization UI failures, browser interception may control the response while retaining authentic sessions; report intercepted cases separately from real server transitions.

Frontend evidence includes full `npm test`, applicable existing build/type gates and Chromium inspection in English and Vietnamese at 1440x900 and 390x844. Capture and present screenshots for changed role-confirmation and authorization-boundary states; exercise refresh-only denial, retry, contextual return, failed/matching reload and directory/role-load interleaving. Show no forbidden render or request as well as permitted recovery. Unit tests alone do not prove layout or runtime startup.

The backend coverage gate is at least 80% aggregate executable-line coverage across the non-generated backend production `.cs` files modified by this change. Use the existing Coverlet collector's Cobertura data: sum covered executable lines and divide by summed valid executable lines for that file set, excluding tests, generated code and unmodified files; do not average per-file percentages. Run the full backend suite without adding a tooling project. This code-coverage metric is separate from the required F01–F15 behavioral and runtime evidence.

The review's 113 scoped backend and 83 scoped frontend passing tests are historical baseline evidence only. They neither reproduce these fixes nor replace the implementation verification above.

### Finding-to-acceptance traceability

| Finding | Owning delta / decisive acceptance | Existing implementation or regression surface |
|---|---|---|
| F01: omitted presented session version | `auth`: noninitial valid versions succeed; stale commands cannot borrow a newer version | Catalog/document-operations endpoints and command records; API/session suites |
| F02: hide/review race | `curated-catalog`: both admission orders with no post-revocation mutation | Hide/review handlers; privileged unit of work; PostgreSQL interleavings |
| F03: dead consumer under a healthy host | `document-ingestion`, `role-authorization`: unsuccessful startup, then restart dispatches existing queued identities | `Program`, PDF worker; real host startup/recovery smoke |
| F04: unaudited early role rejection | `admin-audit`: validation/lookup/protection codes retain one eligible Rejected event | Role handler/account command boundary; account and role suites |
| F05: publication rejection rolled back | `admin-audit`: eligible Rejected event survives independently; no published change survives | Publish handler/clean audit boundary; catalog mutation integration |
| F06: fabricated role descriptor | `admin-accounts`: suspend/reactivate/sign-out response matches persisted role in fresh contexts | Account handlers/DTO projection; PostgreSQL and API producers |
| F07: Suspended Admin overprotected | `admin-accounts`: demotion succeeds, remains Suspended; Active minimum and unchanged code preserved | Role assignment/account boundary; existing incorrect regression replaced |
| F08: receipt exception hidden | `role-authorization`: unreadable schema differs from successfully absent receipt | Readiness service; receipt schema/lookup fault cases |
| F09: grant count substitutes for integrity | `role-authorization`: exact required current keys including Categories | Readiness/initialization and protected-grant definition |
| F10: policy denial hides initialization | `role-authorization`, `auth`: valid Learner gets 503 before grants; guest/stale 401; initialized denied 403 | `Program` pipeline; real HTTP integration |
| F11: renewal/retry admits another destination | `admin-workspace`: forbidden outlet never mounts/fetches, even transiently | `app.vue`, authorization boundary, Catalog; integrated component/browser cases |
| F12: failed reload clears conflict | `admin-accounts`: only accepted matching reload clears gate; no automatic resubmit | Role modal/users store; failure, identity, target and dialog replacement cases |
| F13: wrong role option field | `admin-accounts`: real `permissionKeys` explains selected scope | Role modal/users-store fixtures and existing generated schema |
| F14: role loading never settles | `admin-accounts`: directory changes cannot strand latest role request | Users-store role ownership; success/failure/supersession cases |
| F15: contextual URLs denied | `admin-workspace`: safe query/hash preserved; pathname governs grants | Route utilities, access store, login and auth middleware |

## Risks / Trade-offs

- **Startup becomes intentionally fail-fast** -> Establish database readiness before restarting, inspect process outcome rather than expecting a failed host's HTTP health endpoint, and use the existing supervisor/operator restart boundary. No new retry or restart-policy configuration is introduced.
- **Admission-order changes can alter error precedence** -> Preserve credential/policy denial versus authorized business rejection, validate malformed command fields before target-version comparison, and assert public codes with no state changes.
- **Clean audit extraction can accidentally reuse failed tracking or duplicate events** -> Follow the existing category pattern, dispose before append, require a separate context and verify persisted counts plus unchanged membership/session/document state on real PostgreSQL.
- **Additional role reads extend an admitted transaction slightly** -> Use a bounded lookup by persisted role ID, keep provider I/O outside locks and avoid joined locking SQL or global eager loading.
- **Vue may render between capability publication and redirect** -> Make the reactive outlet predicate authoritative; post-await checks are only navigation completion, not the security fence.
- **Asynchronous completions can target a replacement dialog or session** -> Retain existing API-client/session fences and give role loading and conflict reload the narrow ownership they need; do not build a second request framework.
- **Older workspace text differs from current landing aliases/order** -> Preserve that established routing behavior explicitly and keep its reconciliation outside this repair; authorization and contextual returns must not depend on an unconditional dashboard landing.
- **Concurrent frontend quality-gate work may land first** -> Keep application changes independent, run the gates present at apply time and do not change that change's baseline or CI artifacts to make these fixes pass.

## Migration Plan

1. Implement and verify against an isolated migrated QA database using the existing role/category schema and authentic accounts. This change adds no database migration and does not rerun initialization on an initialized database.
2. Update `docs/admin-workspace.md` and the relevant startup/verification section of `docs/operations/vps-1gb-deployment.md` after smoke proof. Correct the obsolete six-grant summary and allowlist-based QA instruction, explain absent receipt versus unreadable schema, and document unsuccessful startup/restart plus the observed verification rather than inheriting historical counts.
3. Release matched backend/frontend binaries with current schema. Confirm required database authority before host start, inspect successful startup and queued-worker operation, then verify administrative destination/role behavior. If containers are rebuilt during an explicitly requested deployment, restart Nginx according to existing deployment guidance; this planning change performs no deployment.
4. No role/grant data rollback is needed because this repair does not migrate data or reset authority. For application rollback, use the previous matched binaries against unchanged schema only as an explicit operator decision; that reintroduces the reviewed defects and is not an authority repair. Never remove receipts, restore legacy allowlist authority or reset account versions as a workaround.
