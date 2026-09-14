# Proposal: All-in-One Quiz Generation, Lazy Curation & Lookahead Prefetch

## 1. Problem Statement
1. **Background Over-processing & Rate Limits:** Processing all slices of a document (e.g. 50+ slices) eagerly in the background burns excessive Gemini API quota, risking 429 Too Many Requests errors when multiple users upload documents. Most users never finish reading full books, making full eager ingestion wasteful.
2. **Two-Pass Inefficiency & Topical Drift:** Generating quizzes in a separate subsequent pass doubles API roundtrips and risks semantic divergence between the left reading pane and the right challenge pane.
3. **UI Noise:** A persistent progress bar on book cards expecting 100% background curation is misleading and unnecessary for a lazy curation architecture.
4. **Brittle Error Handling:** When AI generation fails, users need a graceful fallback that allows immediate reading of raw text while ensuring subsequent page reloads re-attempt high-quality AI curation.

## 2. Proposed Solution
1. **All-in-One 1-Shot Generation Prompt:**
   - Extend `IAiMarkdownFormatter.FormatSliceAsync` to generate formatted Markdown, summary, 3 key takeaways, and a Senior Scenario Drill in a single LLM invocation.
   - Saves both `DocumentChunk` and `InterviewQuestion` atomically.
2. **Lean Ingestion (3 Slices Only):**
   - Background ingestion parses bookmarks and extracts all slices, but only curates Slices 1..3 via AI.
   - Slices 1..3 provide an immediate Day 1..3 reading runway. The book is marked `Ready` in ~8–10 seconds. Eager background processing stops.
3. **1-Step Lookahead Prefetching:**
   - When reading Slice $N$ (on `/read/[bookId]` or `/today`), the client triggers an async background prefetch for Slice $N+1$ if uncurated.
   - By the time the user finishes reading Slice $N$, Slice $N+1$ is already curated in the database, yielding 0-latency navigation.
4. **JIT Loading & Resilient Ephemeral Raw Text Fallback:**
   - Navigating to an uncurated slice shows an unobtrusive loading spinner ("AI is curating this chapter...").
   - If AI fails, render a friendly error card offering "Retry with AI" and "View raw text temporarily".
   - Viewing raw text sets an in-memory client flag without modifying `IsAiFormatted = false` in the database, guaranteeing that page refreshes or re-visits automatically re-invoke AI curation.
5. **Clean Library Card UI:**
   - Replace the background progress bar with a clean `Ready` badge and the user's reading milestone (e.g. `Resumes at Slice 4`).

## 3. Key Invariants Preserved
- 100% English codebase.
- Standardized TechInsight structure (H1 Title, `> [!NOTE]` context, universal code fences, `> [!TIP]`, 3 Key Takeaways).
- Zero prose trapped inside code blocks.
- Database soft deletes and partial unique indexes preserved.
- Local-first verification with Playwright before deployment.
