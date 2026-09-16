# Proposal: Fix Term Explainer i18n, Layout, and Context

## Why
The in-reader AI Term Explainer (`TermExplainerModal.vue`), floating selection toolbar, and backend semantic caching service suffer from several visual, functional, and data-integrity defects:

1. **Modal Header Layout Overflow & Collision:**
   - In `TermExplainerModal.vue`, passing long category titles (such as full book titles like *"Designing Data-Intensive Applications: The Big Ideas Behind Reliable, Scalable, and Maintainable Systems"*) causes the `⚡ Instant Cache` badge to wrap onto two lines.
   - The flex row lacks flex boundary containment (`min-w-0 flex-1`), causing the badge and title container to push out and collide with the modal close button (`X`), breaking modal header alignment especially on mobile and compact viewports.

2. **Hardcoded English Copy (Missing i18n):**
   - Copy in `TermExplainerModal.vue` is 100% hardcoded English: `"⚡ Instant Cache"`, `"Analyzing term with Google Gemini..."`, `"Powered by Google Gemini"`, `"Copy Explanation"`, and `"Copied"`.
   - While the user interface supports Vietnamese and English, Vietnamese readers see an untranslated modal experience, violating the platform's multi-lingual requirements.

3. **Impoverished Selection Context Extraction:**
   - When a reader highlights a term in `frontend/pages/read/[bookId].vue`, `handleExplainSelection` only populates `currentContext` with `currentChunk.value?.chapterTitle || ""` rather than the surrounding text paragraph.
   - The Gemini model is starved of the specific paragraph context where the term occurred, resulting in generic definition responses rather than contextual explanations grounded in the author's technical discussion.

4. **Cache Poisoning via Local Fallback Persistence:**
   - In `TermExplanationService.cs`, when Gemini API fails, times out, or when an API key is unconfigured, the service executes `GetFallbackExplanation(term, category, locale)` to generate a generic placeholder response.
   - The service subsequently saves this placeholder text into `TermExplanationCaches` alongside a generated vector embedding.
   - Once persisted, future queries for the term (or semantically similar terms with cosine distance $\le 0.08$) result in Tier 1 or Tier 2 cache hits, permanently returning generic placeholder copy marked with `⚡ Instant Cache`, permanently poisoning the pgvector semantic cache.

## What Changes

1. **Robust Responsive Layout in `TermExplainerModal.vue`:**
   - Add `min-w-0 flex-1` to the header title container so text truncation rules apply correctly.
   - Add `truncate max-w-[180px] sm:max-w-xs` to the category title element.
   - Add `whitespace-nowrap shrink-0` to the `⚡ Instant Cache` badge so it never wraps across lines.
   - Ensure the close button has `shrink-0` to guarantee click target integrity.

2. **Comprehensive i18n Localization:**
   - Extract all modal strings to i18n keys under the `reader` namespace in `frontend/i18n/locales/en.json` and `vi.json`:
     - `reader.instant_cache`: "Instant Cache" / "Bộ nhớ tức thì"
     - `reader.term_explainer_loading`: "Analyzing term with Google Gemini..." / "Đang phân tích thuật ngữ với Google Gemini..."
     - `reader.term_explainer_powered_by`: "Powered by Google Gemini" / "Được hỗ trợ bởi Google Gemini"
     - `reader.term_explainer_copy`: "Copy Explanation" / "Sao chép giải thích"
     - `reader.term_explainer_copied`: "Copied" / "Đã sao chép"
   - Update `TermExplainerModal.vue` to use `$t(...)` bindings for all user-facing copy.

3. **Surrounding Document Context Extraction in Reader:**
   - Update text selection handling in `frontend/pages/read/[bookId].vue` to capture the surrounding DOM paragraph or block element text (up to 400–500 characters enclosing the selected term) when invoking `handleExplainSelection`.
   - Fall back to chapter title only when surrounding DOM node text is unavailable.
   - Deliver rich contextual grounding to the backend `/explain-term` endpoint.

4. **Backend Semantic Cache Hygiene & Poisoned Cache Purge:**
   - Refactor `TermExplanationService.cs` to guarantee that local fallback explanations (`GetFallbackExplanation`) are **never** persisted to `TermExplanationCaches`.
   - Only cache responses when the LLM successfully returns a verified, non-empty explanation.
   - Provide a database cleanup mechanism / SQL migration to purge legacy poisoned cache rows containing generic fallback text patterns (`LIKE '%represents a core runtime or architectural mechanism%'` and `LIKE '%Khái niệm kỹ thuật quan trọng mô tả cơ chế hoạt động nội tại%'`).

## Capabilities

### Modified Capabilities
- `reader`: Responsive flex header layout, full i18n localization in `TermExplainerModal.vue`, and surrounding paragraph context extraction from active text selections in `read/[bookId].vue`.
- `vector-embeddings`: Strict cache hygiene in `TermExplanationService` preventing unverified local fallbacks from entering `TermExplanationCaches`, and purging existing poisoned cache entries.

## Impact
- **Frontend:**
  - `frontend/components/today/TermExplainerModal.vue`: Flex layout fixes, CSS truncation and shrink boundaries, i18n template replacements.
  - `frontend/pages/read/[bookId].vue`: DOM context extraction logic in `handleExplainSelection`.
  - `frontend/i18n/locales/en.json` & `vi.json`: New translation keys for the term explainer modal.
- **Backend:**
  - `backend/src/TechDaily.Infrastructure/Services/TermExplanationService.cs`: Conditional caching gate restricting persistence strictly to verified Gemini responses.
  - Database: Removal of poisoned fallback records from `TermExplanationCaches`.
