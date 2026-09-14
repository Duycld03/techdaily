# Design: Linebreak Normalization & Smart Prose Extraction

## Architecture Overview

```
[Raw PDF Stream]
       │
       ▼
[PdfPigExtractor.ExtractPageLines]
       │
       ▼
[PdfPigExtractor.FormatAsMarkdown] ──► Recognizes Technical Headings & Closes on Delimiters
       │                               (No trapped prose in raw mode)
       ▼
[DocumentChunks.OriginalTextMarkdown (Raw)]
       │
       ▼ (JIT AI Curation or Background Curation)
[GeminiAiService.FormatSliceAsync]
       │ (Prompt without misleading literal \n in JSON schema)
       ▼
[GeminiAiService.ParseSliceResponse]
       │
       ├──► NormalizeEscapedNewlines() converts "\n" to real LF (0x0A)
       │
       ▼
[PostgreSQL DocumentChunks] ── (Persisted with true LF 0x0A)
       │
       ▼
[Frontend: useMarkdownRenderer.ts]
       │
       ├──► sanitizeScraperArtifacts() unescapes "\n" as fallback defense
       │
       ▼
[MarkdownIt + Shiki Renderer] ──► Beautiful Prose, Callout Alerts & Highlighted Code
```

## Detailed Technical Design

### 1. Gemini AI Service Normalization
- Create a private static helper:
  ```csharp
  private static string NormalizeEscapedNewlines(string? text)
  {
      if (string.IsNullOrEmpty(text)) return string.Empty;
      if (text.Contains(@"\n"))
      {
          return text.Replace(@"\r\n", "\n").Replace(@"\n", "\n").Replace(@"\t", "\t");
      }
      return text;
  }
  ```
- Apply `NormalizeEscapedNewlines` to `formattedMarkdown`, `summaryMarkdown`, and `scenarioDrill.ExplanationMarkdown`.
- Update the system instruction prompt template to avoid literal `\\n` escaping in JSON examples.

### 2. PdfPigExtractor Prose Detection Enhancements
- Expand regex in `IsObviousProse`:
  ```csharp
  if (Regex.IsMatch(trimmed, @"^(New behavior|Previous behavior|Type of breaking change|Reason for change|Recommended action|Affected APIs|Change the app|Prerequisites|Next steps|See also|Important|Note|Overview|Summary|For more information)\b", RegexOptions.IgnoreCase))
  {
      return true;
  }
  ```
- Track `sawClosingBrace = (trimmed == "}" || trimmed == "};" || trimmed == "});")`. If an empty line follows a closing brace, close the code block immediately.

### 3. Frontend Markdown Renderer Defense
- In `frontend/composables/useMarkdownRenderer.ts`:
  ```typescript
  function sanitizeScraperArtifacts(text: string): string {
    if (!text) return ''
    if (text.includes('\\n')) {
      text = text.replace(/\\r\\n/g, '\n').replace(/\\n/g, '\n')
    }
    ...
  }
  ```
- In PostgreSQL: Run an update command to replace literal `\n` in existing `DocumentChunks`.
