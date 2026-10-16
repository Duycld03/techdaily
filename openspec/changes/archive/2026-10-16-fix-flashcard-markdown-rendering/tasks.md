# Tasks

## 1. Frontend Implementation

- [x] 1.1 In `frontend/app/components/review/FlashcardDeck.vue`, update `renderedQuestion` and `renderedAnswer` computed properties to invoke `renderMarkdown(content)` with reactive dependency on `isHighlighterReady.value`.
- [x] 1.2 In `frontend/app/components/review/FlashcardDeck.vue`, replace `<h2>{{ questionText }}</h2>` with a semantic prose container `<div class="text-base sm:text-lg font-bold text-slate-900 dark:text-white leading-relaxed prose dark:prose-invert max-w-none prose-p:my-1.5 prose-ul:my-2 prose-li:my-0.5" v-html="renderedQuestion"></div>`.
- [x] 1.3 In `frontend/app/components/review/FlashcardDeck.vue`, replace `<p>{{ answerText }}</p>` with a semantic prose container `<div class="text-xs sm:text-sm font-semibold text-slate-800 dark:text-slate-200 leading-relaxed prose dark:prose-invert max-w-none prose-p:my-1.5 prose-ul:my-2 prose-strong:text-brand-600 dark:prose-strong:text-brand-400" v-html="renderedAnswer"></div>`.
- [x] 1.4 In `frontend/app/components/review/FlashcardDeck.vue`, add responsive vertical scroll handling (`max-h-[380px] sm:max-h-[460px] overflow-y-auto`) to front and back face containers to gracefully accommodate multi-option quiz challenges and technical code blocks without layout shifts.

## 2. Frontend Unit & Contract Testing

- [x] 2.1 In `frontend/tests/components/flashcardDeck.spec.ts`, add test cases verifying that markdown front content containing multiple-choice options (`- **A.** ...`) renders as HTML unordered list (`<ul>`, `<li>`) with bold labels (`<strong>A.</strong>`).
- [x] 2.2 In `frontend/tests/components/flashcardDeck.spec.ts`, add test cases verifying that back face markdown content (`**Correct Option: ...**\n\nExplanation`) renders bold answer callout and separated explanation paragraphs.
- [x] 2.3 Run `npm test -- frontend/tests/components/flashcardDeck.spec.ts` and verify all tests pass with 100% green status.

## 3. Visual & End-to-End Verification Gate

- [x] 3.1 Run headless Chromium browser inspection via `browser` device in `eval` across Desktop ($1440\times900$) and Mobile ($390\times844$) viewports on `/review`.
- [x] 3.2 Capture and inspect visual screenshots of both face-up (multiple-choice options list) and flipped (solution with bold callout and explanation) states to verify layout integrity and zero text wrapping collisions.
