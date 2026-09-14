# Delta Specification: Roadmap (Sequential Module Grouping & Clean Cards)

## Purpose
Specifies requirements for grouping document slices into sequential, coherent modules and displaying sanitized preview summaries on the Roadmap page.

## Delta Requirements

### Requirement: Sequential Module Grouping
The Roadmap SHALL group slices by their parent module and chronological chunk order, preventing identical topic names (such as "Overview") across different parts of a book from collapsing into a single distorted milestone.

#### Scenario: Document contains multiple "Overview" slices across different modules
- **WHEN** a document has multiple chapters that contain an "Overview" slice (e.g. `Fundamentals: Overview` at slice #2, `Blazor: Overview` at slice #23)
- **THEN** Roadmap maintains chronological linear progression, grouping slices into distinct, sequential milestones according to their module boundary, with non-overlapping chunk orders (`#1, #2, #3...`).

---

### Requirement: Clean Summary Presentation
Roadmap milestone cards SHALL sanitize raw markdown heading tokens (`#`, `##`, `###`) and trailing boilerplate, presenting clean, legible technical descriptions.

#### Scenario: Slice summary markdown starts with heading tokens
- **WHEN** a slice's `summaryMarkdown` begins with `# Chapter Title` or `### Date`
- **THEN** Roadmap card renders a cleaned text description with leading markdown formatting symbols stripped.
