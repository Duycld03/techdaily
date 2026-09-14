# Capability: PDF Prose & Code Block Separation Heuristics

## MODIFIED REQUIREMENTS

### Requirement 1: Technical Prose Header Isolation in Raw Markdown Extraction
`PdfPigExtractor.FormatAsMarkdown` MUST recognize standard technical documentation and breaking-change section headings as obvious prose and MUST NOT trap them inside code fences (` ``` `).

#### Scenario 1.1: Breaking change headings
- **GIVEN** raw text extracted from a PDF containing code snippets followed by `"New behavior"`, `"Previous behavior"`, `"Type of breaking change"`, `"Reason for change"`, or `"Recommended action"`
- **WHEN** `FormatAsMarkdown` formats the text
- **THEN** any open code block MUST be closed before the heading line
- **AND** the heading line MUST be formatted as normal narrative prose or Markdown subheading, NOT inside code fences.

### Requirement 2: Closing Brace Termination Heuristic
`PdfPigExtractor.FormatAsMarkdown` MUST recognize standalone closing delimiters (`}`, `};`, `});`) as strong signals to terminate an active code block when followed by empty lines or prose.

#### Scenario 2.1: Method or object declaration conclusion
- **GIVEN** a code block in `FormatAsMarkdown` that outputs `});` or `};` or `}` on its own line followed by an empty line
- **WHEN** the next line is encountered
- **THEN** the code block MUST be closed with ` ``` ` so subsequent text remains outside the code block.
