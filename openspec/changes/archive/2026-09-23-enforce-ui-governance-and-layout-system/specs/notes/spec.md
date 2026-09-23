# Spec Delta: notes

## ADDED Requirements

### Requirement: Technical Notes Board Layout Integration
The technical notes archive interface (`frontend/pages/notes.vue`) SHALL implement the `BoardLayout` archetype (`BoardLayout.vue`), replacing the narrow single-column list with an expansive, auto-flowing knowledge card board.
1. **Header Slot (`#header`)**:
   - Houses the Notes Archive title, subtitle, and primary action triggers.
2. **Filters Slot (`#filters`)**:
   - Houses the full-text search input with ⌘K shortcut badge and horizontal scrollable tag filter chips (#All, #Database, #Vue, #Kafka).
3. **Content Grid Slot (`#content`)**:
   - Renders saved technical highlights in a responsive auto-flowing grid: 1 column on mobile, 2 columns on tablets/small laptops (`md:grid-cols-2`), and 3 columns on standard desktop viewports (`xl:grid-cols-3 gap-4`).
   - Eliminates $> 400\text{px}$ dead black margins on both sides of the screen.
4. **Pagination Slot (`#pagination`)**:
   - Houses the paginated navigation controls (`BasePagination.vue`).

#### Scenario: Browsing Technical Highlights on Desktop
- **WHEN** an engineer views their saved highlights on a 1920x1080 display
- **THEN** highlights render as compact, structured cards distributed evenly across a 2-to-3 column grid spanning the available container width, with search and tag filters pinned at the top.
