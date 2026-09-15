# Proposal: Clean Reader AI Experience & Model Standardization

## Why
The previous in-book RAG feature inadvertently introduced a conversational chatbot side-drawer (`AskBookDrawer.vue`) with chat bubbles, message history, and avatar icons into the reader. This conversational paradigm introduces UI clutter, distracts from deep technical reading, and misrepresents the underlying stateless vector retrieval. Additionally, legacy references to non-existent models ("Gemini 3.6 Flash") persist across UI copy, backend fallbacks, and capability specs, violating project architecture standards.

## What Changes
1. **Remove In-Book Chatbot Drawer:** Completely remove `AskBookDrawer.vue`, the "Hỏi AI" header button, and `Shift + ?` shortcut from both `/read/[bookId]` and `/today` to restore a 100% distraction-free, immersive reading experience.
2. **Remove Ask Book Endpoint & Handlers (BREAKING):** Deprecate and remove `POST /api/v1/library/books/{id}/ask`, `AskBookHandler.cs`, validator, DTOs, `IBookQAService`, and corresponding Nginx rate-limiting route matching.
3. **Standardize Gemini AI Model Naming & Branding:**
   - Eliminate all references to "Gemini 3.6 Flash" across frontend UI, backend fallbacks, and specifications.
   - Standardize backend fallback configurations to `gemini-3.1-flash-lite` in accordance with `AGENTS.md` Rule 12.
   - Update UI copy in `TermExplainerModal.vue` to display clean, professional branding: "Powered by Google Gemini" and "Analyzing term with Google Gemini...".
4. **Preserve Core pgvector Value:** Retain 100% of the pgvector infrastructure: two-tier semantic cache for floating term explanations (`⚡ Instant Cache`), automatic vectorization on book ingestion, and grounded interview quiz generation (`/quiz`).

## Capabilities

### Modified Capabilities
- `reader`: Remove the in-book RAG slide-over drawer and keyboard shortcut; standardize AI term explainer copy and loading feedback to accurate Google Gemini branding.
- `quiz`: Standardize model specifications from "Gemini 3.6 Flash" to Google Gemini Flash Lite.
- `insights`: Standardize model specifications from "Gemini 3.6 Flash" to Google Gemini Flash Lite.

## Impact
- **Frontend:** Deletion of `AskBookDrawer.vue` and `AskBookDrawer.spec.ts`. Simplified `read/[bookId].vue` and `today.vue` headers. Cleaned `useLibraryStore.ts`. Updated `TermExplainerModal.vue` copy.
- **Backend:** Deletion of `AskBookHandler.cs` and unit tests. Removal of `IBookQAService` and `AskAsync` in `GeminiAiService`. Alignment of configuration fallbacks in `GeminiAiService` and `TermExplanationService` to `gemini-3.1-flash-lite`.
- **Infrastructure:** Removal of `/library/books/[^/]+/ask` from `nginx/nginx.conf`.
