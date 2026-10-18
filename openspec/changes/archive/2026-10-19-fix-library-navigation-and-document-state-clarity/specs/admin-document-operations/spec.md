# Spec Delta

## MODIFIED Requirements

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

## ADDED Requirements

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
