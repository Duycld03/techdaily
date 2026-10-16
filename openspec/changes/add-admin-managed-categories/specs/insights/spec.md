# Spec Delta

## MODIFIED Requirements

### Requirement: Tech Insights Feed Data Model & Query API
The system SHALL retain the standalone insight catalog, paginated/random browsing and `GET /api/v1/insights/meta`, with the existing Studio presentation, syntax-highlighted problematic/idiomatic code, mechanics and benchmark chips for technical content and appropriate behavioral comparisons for non-code topics. Category identity SHALL use UUIDs and shared bilingual descriptors, not a closed enum or fixed topic dictionary.

#### Scenario: User requests next technical insight card
- **WHEN** an authenticated user requests `GET /api/v1/insights/feed` with optional `categoryId` UUID or tag filters
- **THEN** matching insights are returned with their authoritative category descriptors, problem context, analysis and applicable code/benchmark content.

#### Scenario: User navigates insights on frontend card reader
- **WHEN** the user clicks Next Insight or presses Space/ArrowRight
- **THEN** the reader transitions to the next card with code highlighting and a stored localized category name
- **AND** pure-text All/category filter chips remain separate from the view-mode switcher.

#### Scenario: Structured benchmark telemetry metric chips
- **WHEN** benchmark statistics contain one or multiple pipe-delimited metrics
- **THEN** each metric renders as a compact brand-violet chip with one Zap icon
- **AND** leading raw emoji markers are stripped to avoid duplicate icons.

#### Scenario: Responsive card header layout and telemetry placement
- **WHEN** a card renders on mobile, tablet or desktop
- **THEN** taxonomy badges and bookmark action remain separated
- **AND** telemetry wraps without displacing the bookmark button or compressing the title.

#### Scenario: Client requests insights metadata and dynamic topic suggestions
- **WHEN** an authenticated client requests insights metadata
- **THEN** categories are resolved from the shared catalog with UUIDs, slugs, both stored names, order/status, and published insight counts
- **AND** suggested topics use only the authenticated user's nondeleted books and chapter titles grouped by category UUID
- **AND** each group contains at most eight distinct cleaned titles without raw chapter prefixes
- **AND** groups lacking source topics remain empty rather than receiving a fixed curriculum fallback
- **AND** HTTP 200 returns `{ categories, suggestedTopics }` without hardcoded category arrays or the removed Topics table.

#### Scenario: User toggles between Explore and Saved view modes
- **WHEN** a user with five bookmarks selects Saved (5)
- **THEN** Saved is active and only their bookmarks are fetched
- **WHEN** Explore is selected
- **THEN** the general feed is restored without losing the selected category UUID.

#### Scenario: User filters saved bookmarks by category
- **WHEN** the user selects a category while in Saved mode
- **THEN** the client requests `GET /api/v1/insights/feed?categoryId=<selected-uuid>&onlyBookmarked=true&page=1&pageSize=50`
- **AND** only matching bookmarks display and the selected category remains active.

#### Scenario: User views tailored empty state in Saved mode with zero bookmarks
- **WHEN** a user with no bookmarks enters Saved mode
- **THEN** the localized empty state displays a visible BookmarkCheck icon, explains how to save cards, and offers the Explore Knowledge action.

#### Scenario: User clicks CTA in Saved empty state to return to Explore mode
- **WHEN** the user clicks the Explore Knowledge CTA in the Saved empty state
- **THEN** Explore activates and loads the feed for the selected category.

#### Scenario: User bookmarks an insight card and views updated bookmark count in switcher
- **WHEN** a user with three saved cards bookmarks another through `POST /api/v1/insights/{id}/bookmark`
- **THEN** the bookmark becomes active and the Saved counter immediately changes to four.

#### Scenario: Responsive AI generation modal height containment
- **WHEN** the user opens AI generation for an active category
- **THEN** the modal remains within 85dvh with a scrollable body and bounded suggestion area
- **AND** topic count cannot push the dialog beyond the viewport.

#### Scenario: Dynamic category-aware modal placeholders and guidance
- **WHEN** the user opens the generation modal for any category, including a new subject
- **THEN** its stored name and available source-derived suggestions provide context
- **AND** a generic localized topic prompt works when suggestions are empty, without subject-name switches or invented examples.

#### Scenario: Empty state visual integrity with contextual icons
- **WHEN** Explore contains no insights for the selected category
- **THEN** the empty state displays a visible Lightbulb icon rather than an unstyled empty box.

#### Scenario: Localized category badge and sanitized tag display
- **WHEN** a card is viewed in Vietnamese
- **THEN** its category badge displays stored `nameVi` rather than an enum name or English fallback
- **AND** tags remain trimmed without leading/internal spaces.

#### Scenario: Seed data availability across all practice domains
- **WHEN** a fresh database provisions the original example insights
- **THEN** their category references resolve to the migration's seven stable seed UUIDs
- **AND** subsequent startup does not rename, reorder, reactivate or duplicate administrator-managed categories
- **AND** newly created categories are allowed to have zero insights and do not trigger automatic generation.

#### Scenario: Restart preserves existing example insight state
- **GIVEN** a legacy random-ID example insight was reclassified, edited, hidden or soft-deleted, with existing bookmark references
- **WHEN** first post-migration startup and subsequent restarts provision examples
- **THEN** its UUID, bookmarks, category, content/source, publication/deletion state and timestamps remain unchanged
- **AND** provisioning inserts only when no matching seed slug exists across all rows, including soft-deleted matches; fixed category UUIDs do not replace insight identities.

#### Scenario: Inactive category history remains browsable
- **WHEN** a category with published or bookmarked insights is deactivated
- **THEN** its existing feed and saved cards remain filterable under the same UUID and current label
- **AND** it is excluded from new standalone generation choices.

### Requirement: On-Demand AI Insight Synthesizer
The system SHALL synthesize insights only after an explicit user action with a valid active `categoryId` and topic. Technical topics SHALL use problematic/idiomatic code and performance analysis; behavioral topics SHALL compare antipatterns, useful models and cognitive mechanisms. Source/topic/language SHALL guide this distinction, never a category ordinal, name, or slug switch. The existing modal SHALL preserve flush focus styling and bilingual cancellation.

#### Scenario: User opens AI topic synthesis modal and inspects input focus
- **WHEN** an authenticated user opens the modal and the topic input receives autofocus
- **THEN** its focus ring conforms to rounded input geometry without a detached outline or double border.

#### Scenario: User inspects modal dismiss button localization
- **WHEN** the modal is viewed in Vietnamese
- **THEN** its dismiss action reads "Hủy Bỏ"
- **WHEN** the locale switches to English
- **THEN** the dismiss action reads "Cancel".

#### Scenario: User triggers AI insight generation
- **WHEN** the user explicitly submits a technical topic and active category UUID
- **THEN** Gemini generates the applicable code/benchmark insight and persists it under the admitted category UUID.

#### Scenario: User triggers mental model AI insight generation
- **WHEN** the user explicitly submits a cognitive/behavioral topic and active category UUID
- **THEN** Gemini generates the behavioral comparison and persists it under that UUID without requiring a predefined mental-model category.

#### Scenario: Category changed or deactivated during generation
- **WHEN** deactivation wins the final assignment-admission check
- **THEN** generation returns HTTP 409 `Category.Inactive` without persisting an insight under the inactive category
- **AND** a category filter or topic-chip click alone never invokes AI.
