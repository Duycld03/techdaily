# Spec Delta

## MODIFIED Requirements

### Requirement: Verbatim Text Preservation and Structural Restoration
AI slice curation SHALL restore source structure while preserving the author's complete prose in every category, including administrator-created and retained inactive categories. Category names, slugs and IDs SHALL NOT select a rewrite mode, persona, mandatory code example, or content-replacement policy.

The formatter SHALL enforce:
1. Preserve original narrative prose, examples, anecdotes, voice and source language without summarization, omission, condensation or synthetic rewording.
2. Detect run-in subheadings and restore standard Markdown heading boundaries without changing their words.
3. Remove duplicate running-header echoes and extraction/page artifacts without deleting author content.
4. Restore paragraph separation and dialogue/quotation boundaries without altering words.
5. Keep the executive context/summary, exactly three takeaways, estimated reading time and source-grounded scenario drill separate from the source reading body. Source/topic content SHALL guide the drill; code/pseudocode SHALL appear only when supported by the source, not because a category requires it.

#### Scenario: Curation of imported chapter with run-in headings
- **WHEN** an admitted book slice with merged run-in headings and narrative is explicitly curated or processed by its admitted ingestion workflow
- **THEN** headings and natural paragraph boundaries are restored
- **AND** the author's words and language remain complete and unsummarized regardless of category.

#### Scenario: Elimination of duplicate header echoes
- **WHEN** extracted text contains repeated chapter headers or page artifacts
- **THEN** only duplicate/non-content artifacts are removed
- **AND** restored source prose remains distinct from the auxiliary executive note rather than being replaced by it.

#### Scenario: Preservation of spoken dialogue and quotes
- **WHEN** source text contains dialogue or author quotations
- **THEN** their words and quotation boundaries are preserved on appropriate separate lines, not collapsed into adjacent prose.

#### Scenario: New category is not a formatting configuration
- **WHEN** an administrator creates a new category or renames an existing category
- **THEN** no prompt branch, ordinal mapping or deployment is needed
- **AND** source content and language, not category text, determine appropriate examples and scenario style.

### Requirement: Synchronized Ingestion Pipeline for Clean Markdown Storage
Direct PDF uploads, web/Markdown imports and remote embedded-PDF ingestion SHALL use one source-faithful curation contract. Successful explicit curation SHALL persist restored source Markdown and its formatted state for all reader surfaces, with auxiliary summaries and questions stored separately. Category changes SHALL never curate historical content or queue AI work. Failed curation SHALL not overwrite readable source with partial or synthetic fallback text.

#### Scenario: CurateSliceHandler persists clean AI-restored verbatim text
- **WHEN** on-demand or admitted initial curation returns successful nonempty restored source Markdown
- **THEN** the restored source body and formatted status are persisted under the existing save/concurrency safeguards
- **AND** category-specific overwrite bypasses do not make PDF, web and remote-PDF flows disagree.

#### Scenario: Reader surfaces render AI-restored verbatim markdown
- **WHEN** a user opens an already-curated slice in the reader or Today view
- **THEN** restored headings/paragraphs render with the author's complete unsummarized prose
- **AND** auxiliary notes, summaries or takeaways do not replace the source body.

#### Scenario: Administrative category mutation has no curation side effects
- **WHEN** category metadata, order or active status changes
- **THEN** no source body, formatted status, summary, drill, audio or learning progress is rewritten
- **AND** no AI request, source retry or embedding regeneration starts automatically.
