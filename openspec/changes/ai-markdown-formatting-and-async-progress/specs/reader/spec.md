# Delta Specification: Reader (Standardized TechInsight Markdown & On-Demand JIT Formatting)

## ADDED Requirements

### Requirement: Standardized TechInsight Markdown Schema
The AI markdown formatting pipeline SHALL transform raw technical document text into a standardized TechInsight reading structure conforming to the following layout:
1. **Document Heading:** Level-1 `# Title` matching the slice chapter name.
2. **Context Callout:** Immediate `> [!NOTE]` blockquote containing a 2–3 sentence executive summary of the architectural context.
3. **Clean Narrative Prose:** Flowing paragraphs with merged sentence fragments and zero unformatted line-breaks. Explanatory sentences MUST never be trapped inside monospace code fences.
4. **Universal Syntax-Tagged Code Blocks:** Every code snippet MUST be enclosed in fenced blocks with its correct language identifier (e.g. `csharp`, `python`, `typescript`, `sql`, `go`, `rust`, `bash`, `yaml`, `dockerfile`).
5. **Architectural Callouts:** Dedicated `> [!TIP]` or `> [!IMPORTANT]` alert boxes for caveats and best practices.
6. **Key Takeaways:** Exactly three bullet points summarizing actionable takeaways at the end of the slice.

#### Scenario: Raw slice converted by AI formatter
- **WHEN** raw extracted text contains code snippets mixed with explanatory prose instructions
- **THEN** AI formatter emits standard markdown with code cleanly segregated into language-tagged fences, prose formatted as body text, and extraneous publication boilerplate removed.

---

### Requirement: On-Demand Just-In-Time (JIT) Slice Formatting
When a user navigates to a slice in `/read/[bookId]` or `/today` that has not yet been processed by the Tier 2 background queue, the reading service SHALL perform on-demand JIT AI formatting in real time, persist the formatted Markdown to the database, and return the curated content seamlessly.

#### Scenario: User navigates ahead to an unformatted slice
- **WHEN** user opens a slice whose `IsAiFormatted` flag is `false`
- **THEN** reader endpoint transparently invokes `IAiMarkdownFormatter.FormatSliceAsync`, saves the resulting Markdown to `DocumentChunk.OriginalTextMarkdown` and sets `IsAiFormatted = true`, and returns the formatted content to the client within ~1.5s.

#### Scenario: User revisits an already formatted slice
- **WHEN** user opens a slice whose `IsAiFormatted` flag is `true`
- **THEN** reader endpoint immediately serves the persisted Markdown from the database without invoking the AI model.
