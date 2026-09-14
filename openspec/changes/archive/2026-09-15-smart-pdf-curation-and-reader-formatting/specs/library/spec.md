# Delta Specification: Library (PDF Smart Curation & Resilient Markdown)

## Purpose
Specifies requirements for hierarchical bookmark curation, resilient code block detection, and metadata sanitization during technical PDF ingestion.

## Delta Requirements

### Requirement: Hierarchical Bookmark Curation & Depth Filtering
The PDF extractor SHALL analyze outline bookmark trees and curate slices at the Topic/Article level (Level 1 or 2), aggregating leaf sub-sections (Level 3+) into coherent, self-contained chapters rather than creating thousands of micro-slices.

#### Scenario: Large PDF with multi-level bookmark hierarchy
- **WHEN** a PDF contains multi-level bookmarks (e.g. Volume $\rightarrow$ Module $\rightarrow$ Topic $\rightarrow$ Sub-section)
- **THEN** extractor curates slices at the Topic level, aggregating child sub-sections into their parent topic content, and names the slice with its contextual module prefix (e.g. `Fundamentals: Dependency Injection`).

#### Scenario: Standalone long article without sub-bookmarks
- **WHEN** a single curated article exceeds 4,000 words without sub-bookmarks
- **THEN** extractor splits only at natural `##` or `###` headings outside code blocks, preserving complete code blocks and sentences without arbitrary 700-word partition cuts.

---

### Requirement: Resilient Code Block Delimitation & Prose Protection
The Markdown formatter SHALL NOT trap prose explanations inside code blocks. Code fences MUST only be emitted for multi-line programming constructs with unmistakable code syntax, and MUST immediately terminate upon encountering normal prose sentences, blank line breaks, or section headings.

#### Scenario: Narrative paragraph following code snippet
- **WHEN** a code snippet is followed by narrative prose (e.g. instructions, component descriptions, or explanation sentences)
- **THEN** formatter closes the code fence (```` ``` ````) immediately prior to the prose text, rendering the explanation in standard body typography.

#### Scenario: Single-line code fragment vs explanatory heading
- **WHEN** a line contains a filename or instructional label (e.g. `Components/Pages/Counter.razor :` or `Change the app`)
- **THEN** formatter classifies it as a heading or prose label, never trapping it inside an active code block.

---

### Requirement: Metadata Sanitization & Boilerplate Stripping
The extractor SHALL strip print-layout artifacts, copyright notices, and pre-release disclaimers from the beginning of chapters, ensuring `KeyTakeaways` and `SummaryMarkdown` capture core architectural insights.

#### Scenario: Chapter begins with pre-release disclaimer
- **WHEN** PDF text begins with publication dates (`### 07/30/2025`) or pre-release disclaimers (*"Important: This information relates to a pre-release product..."*)
- **THEN** extractor strips the disclaimer boilerplate from the slice content, and excludes it from `KeyTakeaways` and `SummaryMarkdown`.
