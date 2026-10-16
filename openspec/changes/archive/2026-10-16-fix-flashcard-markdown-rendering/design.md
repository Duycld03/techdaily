# Design: Flashcard Markdown Rendering in Review Player

## Context

See `proposal.md` - Why.

The review session in `frontend/app/pages/review.vue` mounts `FlashcardDeck.vue` to present due flashcards. Currently:
- `FlashcardDeck.vue` imports `useMarkdownRenderer`, but never invokes it on the card's content fields.
- `frontMarkdown` is bound via `{{ questionText }}` inside an `<h2>` element.
- `backMarkdown` is bound via `{{ answerText }}` inside a `<p>` element.
- Markdown lists (`- **A.** ...`), bold text, code blocks, and paragraph breaks are treated as plain text strings, collapsing newlines and exposing raw syntax.
- In contrast, the Edit Card modal preview in `review.vue` parses content through `renderMarkdown()` and renders into Tailwind `prose` containers.

## Goals / Non-Goals

**Goals:**
- Provide rich, parsed Markdown rendering for both the front face (prompt/question) and back face (solution/explanation) in `FlashcardDeck.vue`.
- Properly display multiple-choice option lists (`<ul>` and `<li>`), bold answer callouts (`<strong>`), inline code (`<code>`), and code fences (`<pre>`).
- Automatically re-render code blocks with Shiki syntax highlighting when `isHighlighterReady` resolves.
- Replace invalid block-in-heading nesting (`<h2>` and `<p>`) with accessible semantic `<div>` containers styled with Tailwind Typography (`prose dark:prose-invert max-w-none`).
- Provide graceful vertical scroll constraints (`overflow-y-auto`) for lengthy scenario questions or code blocks on mobile viewports.

**Non-Goals:**
- No database migrations, entity changes, or API contract alterations.
- No changes to card generation logic in backend use cases (`CreateCardFromQuizMistakeHandler`, `SubmitDailyDrillHandler`, `CreateCardFromHighlightHandler`).
- No changes to `FlashcardBentoCard.vue` dashboard list snippet behavior.

## Decisions

### Decision 1: Leverage Unified `useMarkdownRenderer` Composable
- **Choice**: Call `renderMarkdown(content)` provided by `useMarkdownRenderer()` in computed properties `renderedQuestion` and `renderedAnswer`.
- **Rationale**: Reuses the platform standard markdown pipeline with Shiki syntax highlighting, escape sequence normalization (`\n`, `\r\n`), LaTeX symbol cleaning, and obsidian code block headers.
- **Alternatives Considered**: Direct instantiation of `new MarkdownIt()`. Rejected to avoid duplicate Shiki setups and inconsistent code block theming.

### Decision 2: Semantic Prose Containers Replacing `<h2>` and `<p>`
- **Choice**:
  - Replace `<h2 class="...">{{ questionText }}</h2>` with:
    ```vue
    <div
      class="text-base sm:text-lg font-bold text-slate-900 dark:text-white leading-relaxed prose dark:prose-invert max-w-none prose-p:my-1 prose-ul:my-2 prose-li:my-0.5"
      v-html="renderedQuestion"
    ></div>
    ```
  - Replace `<p class="...">{{ answerText }}</p>` with:
    ```vue
    <div
      class="text-xs sm:text-sm font-semibold text-slate-800 dark:text-slate-200 leading-relaxed prose dark:prose-invert max-w-none prose-p:my-1.5 prose-ul:my-2"
      v-html="renderedAnswer"
    ></div>
    ```
- **Rationale**: Markdown rendering produces block-level tags (`<p>`, `<ul>`, `<li>`, `<pre>`). Placing block elements inside `<h2>` or `<p>` violates HTML5 specifications and breaks DOM layout trees.
- **Alternatives Considered**: Using CSS `white-space: pre-line` on `<p>`. Rejected because it only fixes line breaks and leaves markdown asterisks, dashes, and backticks unparsed.

### Decision 3: Re-render Reactivity via `isHighlighterReady`
- **Choice**: Touch `isHighlighterReady.value` inside computed properties:
  ```ts
  const renderedQuestion = computed(() => {
    const _ = isHighlighterReady.value
    return renderMarkdown(props.card.frontMarkdown || '')
  })
  ```
- **Rationale**: Guarantees that code snippets inside flashcards transition seamlessly from fallback text to syntax-colored tokens once the asynchronous Shiki highlighter finishes loading.

## Risks / Trade-offs

- **Risk: Content Height and Vertical Collisions**:
  - Lengthy quiz questions with four long options could increase card height on mobile ($390\text{px}$).
  - *Mitigation*: Ensure the front and back card bodies allow natural flex expansion or constrained scrolling (`max-h-[380px] sm:max-h-[460px] overflow-y-auto`) so action buttons (`Show Answer`, SM-2 grading controls) remain within the visible viewport.
- **Risk: XSS / HTML Injection**:
  - Flashcard markdown is rendered via `v-html`.
  - *Mitigation*: TechDaily's `useMarkdownRenderer` initializes `markdown-it` with HTML disabled or sanitized, ensuring external HTML tags cannot execute scripts.
