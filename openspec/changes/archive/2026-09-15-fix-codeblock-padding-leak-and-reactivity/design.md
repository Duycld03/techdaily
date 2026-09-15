# Design: Codeblock Padding Leak Isolation & Shiki Reactivity

## Context

See `proposal.md` for background and problem statement. In Nuxt 3, code-split chunks for lazy routes (`/insights`) inject their component stylesheets dynamically into `<head>` upon prefetch. Because `ShikiCodeBlock.vue` was the sole component with an unscoped `<style>` block containing `padding: 0 !important;`, it clobbered `main.css`'s `.shiki-container pre.shiki { padding: 1rem 1.5rem !important; }` rule whenever `/insights` was preloaded. Furthermore, `useMarkdownRenderer` shared the same class name (`shiki-container`), compounding the collision.

## Goals / Non-Goals

**Goals:**

- Eliminate global CSS pollution by scoping `ShikiCodeBlock.vue` with Vue SFC scoped styling (`<style scoped>` + `:deep(...)`).
- Decouple markdown reader code blocks from standalone components by assigning a distinct container class (`markdown-code-content`).
- Standardize responsive padding in `main.css` to guarantee clean visual alignment with the window header buttons (`px-4 sm:px-5`).
- Ensure full Shiki initialization reactivity across all markdown-rendering components (`InterviewChallengePane.vue`, `TermExplainerModal.vue`).

**Non-Goals:**

- Altering the Shiki syntax highlighter engine, themes, or token generation.
- Modifying backend markdown storage, curation handlers, or database schemas.
- Changing the visual design or padding of code blocks within `insights.vue`.

## Decisions

### Decision 1: Scoped Deep Selectors in `ShikiCodeBlock.vue`

- **Choice**: Use `<style scoped>` with `:deep(pre.shiki)` and `:deep(code)`.
- **Rationale**: Vue SFC scoped styles append a unique component hash attribute (e.g., `[data-v-xxxx] pre.shiki`). This ensures that `padding: 0 !important` strictly targets `<pre>` tags rendered within `ShikiCodeBlock.vue`'s own `v-html` and can never bleed into other components.
- **Alternatives Considered**:
  - _Inline CSS styles_: Difficult to apply to elements generated dynamically by Shiki's HTML output.
  - _Global class rename_: Still vulnerable if unscoped; Vue scoped styles are the platform-native encapsulation mechanism.

### Decision 2: Decoupled Markdown Container Class

- **Choice**: Replace `.shiki-container` in `useMarkdownRenderer.ts` with `.markdown-code-content`.
- **Rationale**: Defense-in-depth. By using a distinct class name, markdown code blocks cannot be matched by any third-party or component rule targeting `.shiki-container`.

### Decision 3: Standardized Responsive Padding in `main.css`

- **Choice**:

  ```css
  .code-block-wrapper pre.shiki,
  .code-content pre.shiki {
    background-color: transparent !important;
    margin: 0 !important;
    padding: 1rem 1.25rem !important;
    overflow-x: visible !important;
    font-family: inherit !important;
    font-size: inherit !important;
    line-height: inherit !important;
  }

  @media (min-width: 640px) {
    .code-block-wrapper pre.shiki,
    .code-content pre.shiki {
      padding: 1.25rem 1.5rem !important;
    }
  }
  ```

- **Rationale**: The code card header uses `px-4 sm:px-5` (16px on mobile, 20px on desktop). Setting pre horizontal padding to `1.25rem` (20px) on mobile and `1.5rem` (24px) on desktop ensures the code text aligns naturally below the mac window dot indicators.

### Decision 4: Comprehensive Shiki Reactivity Coverage

- **Choice**: Destructure `isHighlighterReady` and access `const _ = isHighlighterReady.value` in computed properties in `InterviewChallengePane.vue` and `TermExplainerModal.vue`.
- **Rationale**: Matches the pattern established in `read/[bookId].vue`, `DocReaderPane.vue`, and `quiz.vue`. When the highlighter initializes asynchronously on client hydration, these views automatically re-render with syntax highlighting.

## Risks / Trade-offs

- **[Risk] Styling Regression in `insights.vue`**
  → _Mitigation_: Verify with unit tests (`ShikiCodeBlock.spec.ts`) and browser screenshot to ensure `insights.vue` retains its exact visual spacing.
- **[Risk] Fallback Pre Padding Discrepancy**
  → _Mitigation_: Ensure fallback `<pre class="shiki one-dark-pro ...">` shares the same CSS rules as highlighted `<pre class="shiki ...">`.

## Migration Plan

1. Modify `ShikiCodeBlock.vue` to add `scoped` and `:deep(...)`.
2. Update `useMarkdownRenderer.ts` and `main.css`.
3. Update `InterviewChallengePane.vue` and `TermExplainerModal.vue`.
4. Run automated tests (`npm --prefix frontend test`).
5. Run Playwright script to verify code block padding across route prefetch and direct navigation.
