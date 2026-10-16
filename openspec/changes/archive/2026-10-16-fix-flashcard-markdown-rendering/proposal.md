# Proposal: Fix Flashcard Markdown Rendering in Review Player

## Why

During spaced repetition review sessions (`/review`), the interactive flashcard player (`FlashcardDeck.vue`) currently binds `card.frontMarkdown` and `card.backMarkdown` as raw plain text inside `<h2>{{ questionText }}</h2>` and `<p>{{ answerText }}</p>` instead of parsing Markdown into HTML.

Because cards generated from Quiz mistakes, Daily Drills, or user edits contain structured Markdown (such as multiple-choice options `- **A.** ...`, bold labels `**Correct Option: ...**`, inline code fragments, and paragraph breaks), this causes:
1. Markdown asterisks (`**`), dashes (`-`), and backticks to remain literally visible as raw syntax.
2. Newlines to collapse into a single unformatted line of text on both the front face and back face.
3. Severe inconsistency with the "Edit Flashcard" modal (`review.vue`), which already renders rich Markdown properly via `useMarkdownRenderer`.

Fixing this now ensures active recall sessions present clean, readable multi-line multiple-choice challenges and architectural explanations as intended across all devices.

## What Changes

- **Frontend (`FlashcardDeck.vue`)**:
  - Integrate `useMarkdownRenderer()` to parse `card.frontMarkdown` and `card.backMarkdown` into sanitized, styled HTML.
  - Bind the highlighter readiness state (`isHighlighterReady`) to trigger re-rendering when Shiki finishes initializing.
  - Replace raw text interpolation `<h2>{{ questionText }}</h2>` and `<p>{{ answerText }}</p>` with prose-styled containers (`v-html`, `prose dark:prose-invert max-w-none`) that properly render headings, ordered/unordered lists, bold text, and code snippets.
  - Maintain responsive typography (`text-base sm:text-lg` for question prompts, `text-sm sm:text-base` for explanations) preserving visual hierarchy and preventing layout shifts.
- **Testing**:
  - Add/update Vitest unit tests in `frontend/test/components/FlashcardDeck.spec.ts` asserting that `frontMarkdown` with list items (`- **A.**`) and `backMarkdown` with bold formatting (`**Correct Option: ...**`) are rendered into structured HTML elements (`<ul>`, `<li>`, `<strong>`) rather than raw strings.

## Capabilities

### Modified Capabilities
- `review`: Enforce Markdown rendering via `useMarkdownRenderer` on both the front prompt and back explanation faces in `FlashcardDeck.vue`.

## Impact

- **Affected Files**:
  - `frontend/app/components/review/FlashcardDeck.vue`
  - `frontend/test/components/FlashcardDeck.spec.ts`
- **Zero Breaking Changes**: Database schema, API contracts (`FrontMarkdown`, `BackMarkdown`), and SM-2 progression logic remain 100% unchanged.
