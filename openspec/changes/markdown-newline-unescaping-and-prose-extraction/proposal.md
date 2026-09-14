# Change: Markdown Newline Unescaping & Raw Prose Code-Block Extraction Refinement

## Why
Two formatting discrepancies were discovered when viewing curated and uncurated document slices in the Reader interface (`/read/[bookId]`):

1. **Slice 15 (`/read/...slice=15`) — Double-Escaped Newlines (`\n`):**
   - **Problem:** Gemini generated AI-formatted Markdown containing literal escape sequences `\n` and `\r\n` instead of actual linebreaks (0x0A).
   - **Visual Impact:** MarkdownIt rendered literal text `\n\n> [!NOTE]\n>`, `\n\n### Overview of Changes\nIn previous versions...` on a single flattened paragraph line, breaking alert callout boxes, headings, and lists.
   - **Root Cause:** The Gemini JSON system instruction contained an interpolated verbatim string `$@"...""formattedMarkdown"": ""# {chapterTitle}\n\n> [!NOTE]...""..."` which prompted the LLM to emit double-escaped `\\n` in JSON values. When `System.Text.Json` unescaped the JSON string, it left literal `\` and `n` characters in C#, which were persisted to PostgreSQL without unescaping or normalization.

2. **Slice 11 (`/read/...slice=11`) — Prose Trapped Inside Code Blocks in Raw Extraction:**
   - **Problem:** When viewing an uncurated slice in raw extraction mode (before AI curation finishes or when reading raw temporarily), explanatory prose sections were trapped inside black code blocks (````csharp`).
   - **Visual Impact:** Lines like `"New behavior"`, `"Starting in ASP.NET Core 11, Blazor.registerCustomEventType throws..."`, and `"Type of breaking change"` were enclosed inside syntax code fences.
   - **Root Cause:** `PdfPigExtractor.FormatAsMarkdown` relied on `IsObviousProse()`, which missed common technical documentation section markers (e.g. `New behavior`, `Previous behavior`, `Type of breaking change`, `Reason for change`, `Recommended action`, `Affected APIs`) and lacked closing-brace token detection (`};`, `});`, `}`).

## What Changes
1. **Gemini AI Markdown Normalization:**
   - Add `NormalizeEscapedNewlines()` in `GeminiAiService.ParseSliceResponse` to convert any literal `\n`, `\r\n`, `\t` into real control characters.
   - Refactor system instruction JSON schema example in `GeminiAiService.cs` to eliminate literal `\n` in prompt templates.
   - Add defense-in-depth normalization in `useMarkdownRenderer.ts` (`sanitizeScraperArtifacts`) to convert any lingering `\n` before rendering with MarkdownIt.
   - Execute a database fixup script to repair existing persisted chunks containing literal `\n`.
2. **Smart Raw Prose vs Code Block Extraction:**
   - Extend `IsObviousProse()` in `PdfPigExtractor.cs` with Microsoft Docs & Breaking Change section keywords.
   - Add token-based code block termination on closing braces (`}`, `};`, `});`) followed by blank lines or obvious prose.
3. **Reader UI JIT Curation State:**
   - Ensure that opening uncurated slices in `/read` displays a clear JIT curation spinner and auto-replaces with AI-curated Markdown upon completion.

## Impact
- **Affected Specs:** `specs/markdown-formatting/spec.md`, `specs/pdf-extraction/spec.md`
- **Affected Components:**
  - `backend/src/TechDaily.Infrastructure/Services/GeminiAiService.cs`
  - `backend/src/TechDaily.Infrastructure/Services/PdfPigExtractor.cs`
  - `frontend/composables/useMarkdownRenderer.ts`
  - `backend/tests/TechDaily.Tests/Infrastructure/PdfPigExtractorTests.cs`
