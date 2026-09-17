# pdf-extraction Specification

## Purpose
TBD - created by archiving change markdown-newline-unescaping-and-prose-extraction. Update Purpose after archive.

## Requirements

### Requirement: Technical Prose Header Isolation in Raw Markdown Extraction
`PdfPigExtractor.FormatAsMarkdown` MUST recognize standard technical documentation and breaking-change section headings as obvious prose and MUST NOT trap them inside code fences (` ``` `).

#### Scenario 1.1: Breaking change headings
- **GIVEN** raw text extracted from a PDF containing code snippets followed by `"New behavior"`, `"Previous behavior"`, `"Type of breaking change"`, `"Reason for change"`, or `"Recommended action"`
- **WHEN** `FormatAsMarkdown` formats the text
- **THEN** any open code block MUST be closed before the heading line
- **AND** the heading line MUST be formatted as normal narrative prose or Markdown subheading, NOT inside code fences.

### Requirement: Closing Brace Termination Heuristic
`PdfPigExtractor.FormatAsMarkdown` MUST recognize standalone closing delimiters (`}`, `};`, `});`) as strong signals to terminate an active code block when followed by empty lines or prose.

#### Scenario 2.1: Method or object declaration conclusion
- **GIVEN** a code block in `FormatAsMarkdown` that outputs `});` or `};` or `}` on its own line followed by an empty line
- **WHEN** the next line is encountered
- **THEN** the code block MUST be closed with ` ``` ` so subsequent text remains outside the code block.

### Requirement: PDF Paragraph and Heading Segmentation Heuristics
The PDF extraction engine (`PdfPigExtractor`) SHALL apply geometry-based line segmentation and typography heuristics during text extraction to prevent prose lines from squashing into a monolithic block of text. Extracted text SHALL preserve paragraph boundaries with double newlines (`\n\n`), isolate section titles with Markdown headings (`\n\n## {Title}\n\n`), and preserve line breaks for spoken dialogue and quotations.

The extraction pipeline SHALL implement the following structural heuristics:
1. **Line Spacing Gap Detection:**
   - The extractor SHALL calculate the vertical distance $\Delta Y_i = Y_{i} - Y_{i+1}$ between consecutive lines on a page and determine the median vertical line spacing $\Delta Y_{\text{median}}$.
   - When the vertical gap between line $i$ and line $i+1$ satisfies $\Delta Y_{\text{gap}} \ge 1.35 \times \Delta Y_{\text{median}}$, the extractor SHALL recognize a paragraph boundary and emit double newlines (`\n\n`).
2. **First-Line Indentation Detection:**
   - The extractor SHALL determine the primary left column margin ($X_{\min}$) across body lines on the page.
   - When a line begins with an indent $\Delta X = X_{\text{start}} - X_{\min} \ge 12\text{pt}$, the extractor SHALL identify a first-line paragraph indent and emit double newlines (`\n\n`) before the indented line.
3. **Heading and Section Title Detection:**
   - Lines that are short ($< 60$ characters), horizontally centered within the text block, or matching uppercase or chapter/preface regex patterns (e.g. `^LỜI NÓI ĐẦU`, `^Phần GIỚI THIỆU`, `^Câu chuyện của chính tôi\.?`, `^Chương\s+\d+`, `^Chapter\s+\d+`, `^PHẦN\s+[IVXLCDM\d]+`) SHALL be formatted as Markdown subheadings (`\n\n## {Title}\n\n`) rather than body paragraph prose.
4. **Dialogue and Quotation Detection:**
   - Lines beginning with quotation marks (`"`, `“`, `”`) or dialogue em-dashes / en-dashes (`—`, `–`, `-`) SHALL preserve independent line breaks without being merged into preceding narrative sentences.
5. **Continuous Sentence Soft Breaks:**
   - Lines belonging to the same continuous paragraph with vertical spacing $< 1.35 \times \Delta Y_{\text{median}}$ and without first-line indentation SHALL be joined with a single space or soft break, allowing the Markdown renderer to wrap sentences naturally.

#### Scenario: Vertical line spacing gap triggers paragraph boundary
- **GIVEN** a PDF page with body text lines having a median line spacing $\Delta Y_{\text{median}} = 14\text{pt}$
- **WHEN** line $i$ and line $i+1$ have a vertical spacing of $21\text{pt}$ ($\ge 1.35 \times 14\text{pt} = 18.9\text{pt}$)
- **THEN** `PdfPigExtractor` emits double newlines (`\n\n`) between line $i$ and line $i+1$
- **AND** the Markdown parser renders line $i$ and line $i+1$ in two distinct semantic `<p>` tags.

#### Scenario: First-line indentation triggers paragraph boundary
- **GIVEN** a PDF page with standard text column left margin $X_{\min} = 54\text{pt}$
- **WHEN** a new line of text starts at $X_{\text{start}} = 70\text{pt}$ with an indentation $\Delta X = 16\text{pt} \ge 12\text{pt}$
- **THEN** `PdfPigExtractor` recognizes a paragraph indentation
- **AND** emits double newlines (`\n\n`) before the line to begin a new paragraph.

#### Scenario: Standalone chapter and section titles are formatted as Markdown headings
- **GIVEN** a PDF page containing a standalone line `"LỜI NÓI ĐẦU"` or `"Câu chuyện của chính tôi."`
- **WHEN** `PdfPigExtractor` processes the line during markdown formatting
- **THEN** the line is recognized as a section heading
- **AND** the extractor wraps the text with double newlines and a level-2 Markdown heading (`\n\n## {Title}\n\n`).

#### Scenario: Spoken dialogue and quotations preserve distinct line breaks
- **GIVEN** a narrative passage in a PDF containing spoken dialogue starting with `"- "` or `“`
- **WHEN** `PdfPigExtractor` extracts consecutive dialogue lines
- **THEN** each spoken dialogue line is output with its own distinct line break
- **AND** dialogue lines are not merged into a single run-on paragraph sentence.

#### Scenario: Continuous sentence lines within normal vertical spacing remain in single paragraph
- **GIVEN** two consecutive lines forming part of the same sentence with vertical spacing $\Delta Y = 14\text{pt}$ and $X_{\text{start}} \approx X_{\min}$
- **WHEN** `PdfPigExtractor` processes the two lines
- **THEN** the lines are joined with a single space or soft newline
- **AND** the Markdown renderer displays them within a single `<p>` paragraph block without premature double-newline fragmentation.
