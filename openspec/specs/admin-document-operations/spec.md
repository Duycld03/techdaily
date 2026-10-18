# Admin Document Operations Specification

## Purpose

Let authorized administrators investigate document ingestion through bounded metadata and attempt history, then request safe retries without accessing private document content.

## Requirements

### Requirement: System document metadata inventory
Authorized administrators SHALL inspect non-deleted personal books and catalog entries through a dedicated inventory. Each item SHALL identify its scope, document ID, title, owner ID when recorded, source type, category, language, document processing state, source availability and observation time. Catalog inventory and detail SHALL additionally expose authoritative publication `visibility`; personal records SHALL use null because catalog publication does not apply. Personal copies SHALL remain separate from their catalog source.

#### Scenario: Personal and catalog records coexist
- **WHEN** an administrator lists documents without a scope filter
- **THEN** independently imported books, adopted personal copies and catalog entries appear with explicit scope and owner identifiers
- **AND** adoption does not turn a personal copy into another catalog entry.

#### Scenario: Legacy unowned document
- **WHEN** an administrator inspects a non-deleted book without a recorded owner
- **THEN** the inventory labels ownership as unknown and exposes metadata only
- **AND** retry and owner-source replacement are unavailable.

#### Scenario: Deleted target requested
- **WHEN** a document is soft-deleted before its detail request is evaluated
- **THEN** detail returns 404 and the document is excluded from active inventory.

#### Scenario: Project catalog visibility without replacing processing state
- **WHEN** an administrator requests catalog inventory or detail for a document with visibility Hidden and processing state Ready
- **THEN** both responses return `visibility=Hidden` and `status=Ready` for the same document ID
- **AND** a personal imported or adopted book returns null visibility rather than inheriting its catalog source's publication state.

#### Scenario: Refresh operations after catalog hiding
- **WHEN** an authorized catalog administrator hides a published document and operations inventory or detail is subsequently loaded or explicitly refreshed
- **THEN** operations reports the current Hidden visibility while retaining the document's actual processing status
- **AND** the catalog record remains inspectable in operations even though discovery, preview and new adoption exclude it
- **AND** existing personal snapshots and their learning state are unchanged.

### Requirement: Bounded inventory and attempt queries
Inventory SHALL support search, scope, owner ID, source type, category, language and document-state filters with validated paging. Attempt history SHALL support state and UTC time filters. Page size SHALL default to 20 and be limited to 100; search SHALL be limited to 200 characters. Results SHALL include total count, paging and observation time with deterministic timestamp, identifier and scope ordering.

#### Scenario: Filter failures for one owner
- **WHEN** an administrator requests personal Failed documents for a valid owner ID
- **THEN** only matching records appear and changing filters resets the UI page to one
- **AND** filtering occurs server-side rather than after downloading the full inventory.

#### Scenario: Invalid or empty query
- **WHEN** a query contains an unsupported enum, invalid identifier, nonpositive page or excessive page size
- **THEN** the API returns 400 with a safe machine-readable code.
- **WHEN** a valid query has no matches
- **THEN** it returns an empty page and total count zero.

#### Scenario: Time range and stable history
- **WHEN** an administrator filters attempts with UTC from and to timestamps
- **THEN** from is inclusive, to is exclusive and invalid ranges are rejected
- **AND** equal timestamps have a deterministic identifier tie-breaker.

### Requirement: Private-content exclusion
Document-operations responses SHALL contain allowlisted metadata and sanitized status/error codes only. They SHALL exclude source bytes, document or slice text, summaries, previews, notes, answers, learning records, credentials and storage locations. Source URL display SHALL remove credentials, query strings and fragments. Document-operations permission SHALL NOT grant personal reading, export or source-download access.

#### Scenario: Inspect another user's failed PDF
- **WHEN** an administrator opens its operations detail
- **THEN** title, ownership, status, attempt timing and safe error information are available
- **AND** no source download, reader preview or document body is returned.

#### Scenario: Sensitive exception or source reference
- **WHEN** processing fails with an exception containing text, a server path or a credential-bearing URL
- **THEN** the operations response exposes only an approved error code and localized guidance
- **AND** neither the raw exception nor the sensitive reference appears in detail or audit output.

#### Scenario: Existing personal endpoints remain isolated
- **WHEN** an administrator uses their own session to request another learner's personal book, slice or export
- **THEN** the existing ownership boundary rejects the request
- **AND** operations permission does not substitute the document owner's identity.

### Requirement: Truthful processing history
Detail SHALL show paginated persisted attempts with attempt ID, origin, source type, stage, state, recorded progress, queued/started/ended times, safe error and retry eligibility. Document state and attempt state SHALL be distinguished. Records predating attempt tracking SHALL show unavailable historical data rather than invented attempts, durations or queue measurements.

#### Scenario: Failed then successful retry
- **WHEN** an initial attempt fails and its explicit retry later succeeds
- **THEN** both attempts remain visible with distinct identifiers and outcomes
- **AND** current document status is shown separately from historical failure.

#### Scenario: Restart or missing history
- **WHEN** an expired worker claim is recovered
- **THEN** its attempt is shown as Interrupted rather than still actively running.
- **WHEN** an older document has no attempt history
- **THEN** the UI states that history is unavailable.

#### Scenario: Readable document has enrichment warnings
- **WHEN** a readable document lacks an embedding, formatted slice or audio result
- **THEN** the workspace does not present that absence as a retryable source-ingestion failure
- **AND** it offers no standalone learning-content regeneration action.

### Requirement: Controlled source-ingestion retry eligibility
Administrative retry SHALL be limited to Failed or Interrupted initial PDF, web or Markdown source ingestion with usable retained input. The server SHALL recheck permission, target state, ownership, source availability and concurrency. Completed or already readable imports, active attempts, published catalog entries, deleted or unowned personal targets and targets with learning activity SHALL be blocked with an explicit reason.

#### Scenario: Retry retained source
- **WHEN** an authorized administrator confirms an eligible failed ingestion
- **THEN** exactly one new attempt processes the retained accepted input
- **AND** no remote refetch silently substitutes a newer source revision.

#### Scenario: Input missing or expired
- **WHEN** a failed attempt's source is missing or past expiry
- **THEN** retry is rejected with source-unavailable guidance
- **AND** the UI directs the personal owner or catalog administrator to the appropriate source replacement flow.

#### Scenario: Another operation wins the race
- **WHEN** another accepted attempt, source replacement, deletion or publication changes eligibility before retry commits
- **THEN** the retry returns 409 without dispatching duplicate work.

#### Scenario: Unsafe or out-of-scope retry
- **WHEN** the target is readable, has learning activity, is published or needs only AI, embedding or audio regeneration
- **THEN** administrative source retry is unavailable
- **AND** existing catalog preparation actions retain their separate authorization and readiness rules.

#### Scenario: Ready hidden catalog document is not a publication claim
- **WHEN** an administrator inspects a Hidden catalog document with Ready processing and retry ineligibility code `AlreadyReady`
- **THEN** localized guidance explains that processing is complete and source retry is unavailable
- **AND** the message does not claim that the source is published, visible in Discover, or available for new adoption
- **AND** inspecting the explanation initiates no processing, publication or visibility change.

### Requirement: Explicit retry command and replay protection
A retry SHALL require the failed attempt ID, expected control version, a unique request ID and a trimmed operational reason of 1 to 500 characters. Identical authorized replays SHALL identify the original accepted attempt; reuse of a request ID with different command data SHALL return 409. Submission SHALL be single-flight and rate-limited, with authoritative duplicate prevention on the server.

#### Scenario: Empty reason or stale form
- **WHEN** an administrator submits a blank reason, invalid identifier or missing version
- **THEN** submission is rejected without changing processing state.
- **WHEN** the supplied control version is stale
- **THEN** the API returns 409 and the UI reloads current eligibility.

#### Scenario: Response lost after commit
- **WHEN** an administrator repeats the same command and request ID after an ambiguous network result
- **THEN** the response references the original accepted attempt
- **AND** no second attempt, second acceptance event or retention extension is created.

#### Scenario: Rate limit reached
- **WHEN** an administrator exceeds the configured retry rate
- **THEN** the response is 429 with retry guidance and no new work is dispatched.

### Requirement: Administrative retry audit trail
Accepted administrative retries SHALL atomically record actor, target scope/ID, attempt ID, request correlation, reason and Accepted outcome with the command's state change. Their eventual terminal outcome SHALL append a linked completion event exactly once. Authorized identifiable rejections SHALL use safe Rejected events. Audit records SHALL exclude source contents and raw exceptions.

#### Scenario: Acceptance transaction fails
- **WHEN** acceptance or audit persistence fails
- **THEN** neither a runnable retry nor a successful acceptance event is committed
- **AND** failure is returned without claiming durable acceptance.

#### Scenario: Completion after restart
- **WHEN** an administratively initiated retry completes or fails after worker recovery
- **THEN** the attempt outcome and linked Succeeded or Failed completion event agree
- **AND** duplicate delivery cannot append a second completion event.

#### Scenario: Definitive or infrastructure rejection
- **WHEN** an authorized actor submits a stale or ineligible command for an identifiable target
- **THEN** a sanitized rejection is recorded without mutation.
- **WHEN** audit persistence is unavailable
- **THEN** the API returns an unavailable error and does not claim that the event was recorded.

### Requirement: Ownership and learning-state preservation
Source retry SHALL preserve document ID, personal ownership, category, source language and catalog attribution. It SHALL NOT reset reading progress, notes, cards, quizzes, pacers or adopted copies. Unsafe replacement of output referenced by learning activity SHALL be refused. Partial output replacement SHALL commit atomically after a fresh state check, without duplicate active slice orders.

#### Scenario: Partial import without learning activity
- **WHEN** an eligible failed import contains unreferenced partial slices and is retried successfully
- **THEN** the completed output has unique sequential slice orders under the same document ID
- **AND** failed output is not appended again or removed before successful replacement commits.

#### Scenario: Learning activity arrives before commit
- **WHEN** learner activity makes a target unsafe while a retry is running
- **THEN** the retry refuses destructive output replacement
- **AND** existing slices, learning activity and ownership remain intact.

#### Scenario: Catalog retry does not mutate personal snapshots
- **WHEN** a nonpublished catalog source is safely recovered
- **THEN** previously adopted personal copies and their learning state remain unchanged.

### Requirement: Accessible operations workspace and session lifecycle
The workspace SHALL provide localized loading, empty, unavailable, stale, failure and conflict states, explicit refresh and accessible retry confirmation. Viewing, filtering or refreshing SHALL NOT initiate processing or AI calls. Privileged results and pending requests SHALL be cleared or discarded on logout, account replacement or definitive permission denial; transient failures SHALL retain valid credentials.

#### Scenario: Inspect and confirm on mobile
- **WHEN** an administrator opens detail and retry confirmation in English or Vietnamese at 390x844
- **THEN** the target, reason, source expiry and retry consequence remain readable with keyboard/touch-accessible controls
- **AND** canceling the dialog starts no work.

#### Scenario: Detail resolves for an old session
- **WHEN** an operations request resolves after logout, account replacement or permission loss
- **THEN** its result cannot repopulate protected inventory, detail or confirmation state.

#### Scenario: Refresh fails or source expires while open
- **WHEN** refresh fails after a successful observation
- **THEN** the previous data is visibly stale and a retry action is offered.
- **WHEN** source eligibility changes before submission
- **THEN** the server recheck prevails and the UI shows the current blocked reason.

### Requirement: Independent publication and processing presentation
Operations inventory and detail SHALL distinguish catalog publication visibility from processing readiness through independently named, localized status information. Processing filters SHALL identify their technical meaning and SHALL NOT treat Hidden as a processing state. Personal documents SHALL show no catalog publication badge. Editing and publication actions SHALL remain in their independently authorized catalog workspace.

#### Scenario: Hidden and Ready are shown together
- **WHEN** a Hidden and Ready catalog document is selected in English or Vietnamese
- **THEN** the inventory item and detail identify both its hidden publication state and completed processing state without contradictory readiness wording
- **AND** scope, publication and processing information remain distinguishable at 1440x900 and 390x844.

#### Scenario: Draft processing and published readiness remain distinct
- **WHEN** operations inspects a Processing draft or a Ready published entry
- **THEN** the displayed publication and processing values independently match the authoritative catalog record
- **AND** technical readiness does not automatically publish a draft or republish a hidden entry.

#### Scenario: Operations permission does not grant publication authority
- **WHEN** an account can inspect document operations but lacks catalog-management permission
- **THEN** it can inspect permitted publication metadata without receiving publication/editing actions or access to personal content
- **AND** existing destination and API permission checks remain independent.

### Requirement: Localized document identity metadata
Operations SHALL render category descriptors as localized stored names associated with their UUIDs, including inactive categories, never as serialized objects. System-curated ownership, document scope and source type SHALL use UI translations rather than raw internal English labels. Document titles, source attribution and author names SHALL retain their source language. Missing category metadata SHALL be identified without inventing a category.

#### Scenario: Real category descriptor in either locale
- **WHEN** a personal or catalog document returns a descriptor containing category UUID, English name and Vietnamese name
- **THEN** operations displays the stored name corresponding to the selected locale rather than descriptor JSON
- **AND** retaining an inactive category preserves its identity and translated display name.

#### Scenario: Curated system ownership is localized by scope
- **WHEN** a catalog record is shown in Vietnamese
- **THEN** scope uses the curated-document term and system ownership uses a Vietnamese system label even when no personal owner exists
- **AND** actual personal owner names are not replaced by a catalog label.

#### Scenario: Source formats use human-readable translations
- **WHEN** operations shows a supported web, PDF or Markdown source type in English or Vietnamese
- **THEN** its metadata contains the corresponding human-readable source-format label rather than an internal enum name
- **AND** switching interface language does not translate the document title or source text.

#### Scenario: Unavailable category metadata is not fabricated
- **WHEN** category metadata needed for a labeled reference is unavailable
- **THEN** the interface displays a localized unavailable indication while retaining the document's recorded category identity
- **AND** no raw object or guessed default category is displayed.
