# Delta Specification: Reader (Code Syntax & Monospace Isolation)

## Purpose
Specifies requirements for ensuring that Gitbook Reader and Today Reader properly isolate code blocks from narrative prose and prevent style leakage.

## Delta Requirements

### Requirement: Code Syntax & Monospace Isolation
The Gitbook Reader and Today Reader SHALL guarantee that explanatory text, instructional steps, and filenames remain rendered in standard typography (`prose`) and are never styled as code within code blocks.

#### Scenario: Reader renders a slice with code snippets and narrative explanations
- **WHEN** user reads a document slice containing mixed code and explanatory prose
- **THEN** code snippets are contained within shaded code block wrappers with syntax highlighting and copy buttons, while all surrounding instructions and explanations render in standard body font.
