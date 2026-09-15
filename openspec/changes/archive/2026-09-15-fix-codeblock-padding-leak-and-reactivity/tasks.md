# Tasks: Fix Codeblock Padding Leak and Shiki Reactivity

## 1. Style Encapsulation & CSS Isolation

- [x] 1.1 Scope styles in `ShikiCodeBlock.vue` with `<style scoped>` and `:deep(pre.shiki)` / `:deep(code)`, and verify with `npm --prefix frontend test tests/components/ShikiCodeBlock.spec.ts`
- [x] 1.2 Update `useMarkdownRenderer.ts` to replace `shiki-container` with `markdown-code-content`, ensuring clean decoupling from standalone component selectors
- [x] 1.3 Standardize responsive padding rules in `main.css` for `.code-block-wrapper pre.shiki` and `.code-content pre.shiki` (`1rem 1.25rem` on mobile, `1.25rem 1.5rem` on desktop)

## 2. Reactivity & Highlighting Coverage

- [x] 2.1 Wire `isHighlighterReady` reactivity dependency into `InterviewChallengePane.vue` `renderedExplanation` computed property and verify with `npm --prefix frontend test tests/components/InterviewChallengePane.spec.ts`
- [x] 2.2 Wire `isHighlighterReady` reactivity dependency into `TermExplainerModal.vue` `renderedExplanation` computed property

## 3. Verification & Visual Testing

- [x] 3.1 Run full frontend test suite (`npm --prefix frontend test`) to ensure all tests pass with zero regressions
- [x] 3.2 Execute Playwright verification script confirming code block padding is strictly $\ge 16\text{px}$ on mobile and $\ge 20\text{px}$ on desktop across cold navigations, route chunk preloads, and page reloads
