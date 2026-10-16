# Spec Delta

## MODIFIED Requirements

### Requirement: Reviewed publication lifecycle
Catalog entries SHALL support Draft, Published and Hidden visibility. Publication SHALL require prepared readable slices and basic attribution. Publication SHALL NOT require a separate prerequisite administrator review record (`ReviewedByUserId`) or permission compliance proofs; an administrator's direct publish action SHALL constitute explicit review and authorization. Missing requirements SHALL block publication with actionable errors. Publication, hiding and explicit review SHALL use current account/session authority and catalog permission at final mutation admission; a prior request-level permission check SHALL NOT authorize a write after a competing revocation has already won admission.
1. **Published Document Immutability & UI Controls**:
- Documents in `Published` visibility SHALL be treated as immutable in the administration interface.
- When `visibility === 'Published'`, the document metadata fields (title, category assignment, language, attribution, source URL) and the Save Changes action SHALL be disabled/read-only, with an explanation that the document must be hidden before editing metadata.
- When `visibility === 'Published'`, the action bar SHALL present Hide from Discover and SHALL NOT display an active Publish to Discover button.
- When `visibility === 'Draft'` or `visibility === 'Hidden'`, the action bar SHALL display Publish to Discover, enabled only when publication requirements are satisfied.
- A shared category rename/order/status change SHALL NOT constitute an edit to a published document, alter its revision, or hide it. Existing admitted drafts SHALL retain preparation/publication eligibility when their category is deactivated; new adoption remains governed separately.

#### Scenario: Viewing a published document in catalog administration
- **WHEN** an administrator inspects a document with `visibility === 'Published'`
- **THEN** metadata input fields and Save Changes are disabled
- **AND** the action bar displays only Hide from Discover without a duplicate Publish to Discover button.

#### Scenario: Publish a prepared draft
- **WHEN** an administrator confirms review and publishes a ready draft with complete metadata
- **THEN** it becomes discoverable and the system records the publishing administrator and time.

#### Scenario: Publish an incomplete draft
- **WHEN** an administrator publishes an entry with incomplete initial slices
- **THEN** the system rejects publication and identifies the unmet requirements.

#### Scenario: Hide or edit a published entry
- **WHEN** an administrator hides a published entry
- **THEN** it disappears from learner listing, preview and new adoption
- **AND** existing personal copies remain usable.
- **WHEN** the administrator needs to change its source, content or assigned category
- **THEN** the entry must be hidden first and reviewed again before republication.

#### Scenario: Admin publishes document directly after slices are prepared
- **WHEN** a catalog document has its initial slices formatted and the administrator clicks Publish
- **THEN** the system immediately transitions it to Published without requiring a prior manual review sign-off step.

#### Scenario: Global category metadata changes after publication
- **WHEN** a published document's category is renamed or deactivated
- **THEN** its published content, attribution, revision, and visibility remain unchanged
- **AND** the current category name/status is shown without invoking AI or requiring republication.

#### Scenario: Revocation wins before hiding or explicit review
- **GIVEN** a hide or explicit review request has passed its initial authentication and catalog permission checks
- **WHEN** a competing role, suspension or session-revocation command commits before the request obtains final actor admission
- **THEN** hiding or explicit review is rejected without changing visibility, revision, review attribution or document state
- **AND** no successful catalog mutation event or new processing work is committed under the revoked authority.

#### Scenario: Catalog mutation wins before actor revocation
- **WHEN** a permitted hide or explicit review command obtains final actor admission before a competing revocation
- **THEN** that admitted mutation and its successful audit event can commit before revocation completes
- **AND** the later revocation still invalidates the actor's old credentials without undoing the earlier committed document action.
