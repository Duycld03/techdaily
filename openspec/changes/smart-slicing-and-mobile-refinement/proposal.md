# Proposal: Smart PDF Slicing, Fallback Invariant Enforcement & Mobile UI Refinement

## Context & Motivation
1. **Monster Slices (>15,000 words / 54 minutes read):** Technical documents like `aspnet-core-aspnetcore-10.0.pdf` have monolithic sections without deep bookmarks (e.g. "ASP.NET Core 11: What's new in 11" spanning 23 pages). Because raw PDF text contains no markdown hashes (`##`), the existing splitter failed to subdivide it, dumping 85KB into Slice 4.
2. **AI Overload & False Formatted State:** Gemini cannot format 85KB in a single generation, triggering `FallbackFormatSlice`. The backend mistakenly marked `IsAiFormatted = true` and persisted a generic template scenario drill to `InterviewQuestions`, preventing the frontend from recognizing it as raw text.
3. **Mobile Screen Edge Collision:** On mobile devices in `/today`, the loading synthesis banner lacked horizontal padding and centering, causing text to touch the viewport borders.

## Proposed Solution
1. **Heuristic Smart Slicing in `PdfPigExtractor.cs`:**
   - Detect natural section headings and page transitions in raw text.
   - Enforce a strict **2,500-word ceiling (~8 minutes max read)** per slice. If a section exceeds 2,000 words, cleanly split at natural paragraph breaks (`\n\n`) into `Section 1`, `Section 2`, etc.
2. **Explicit AI Success / Fallback Invariant:**
   - Distinguish genuine AI curation from fallback in `IAiMarkdownFormatter` (`IsSuccess = true/false`).
   - If AI formatting fails or falls back, `CurateSliceHandler` and `GetTodayFocusHandler` MUST NOT set `IsAiFormatted = true` and MUST NOT persist generic fallback questions to `InterviewQuestions`.
3. **Responsive Mobile Loading Banner (`today.vue`):**
   - Use `h-full` to prevent mobile viewport height overflow.
   - Add generous padding (`p-6 sm:p-8`), max-width constraints (`max-w-sm sm:max-w-md`), and centered typography so text never touches mobile screen edges.
