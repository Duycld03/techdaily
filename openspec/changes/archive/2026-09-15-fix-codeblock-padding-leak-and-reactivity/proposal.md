# Proposal: Fix Codeblock Padding Leak and Shiki Reactivity

## Why

When users navigate between pages or first land on the Gitbook Reader (`/read/[bookId]`) or Today (`/today`), asynchronous chunk prefetching (such as preloading `/insights`) dynamically injects an unscoped CSS rule (`.shiki-container pre.shiki { padding: 0 !important; }`) from `ShikiCodeBlock.vue` into `<head>`. This globally wipes out the horizontal padding of all document code blocks, causing code text to be flush against the outer container border ("sát bên trái"). When the user refreshes the page directly, the route renders prior to the chunk prefetch, creating inconsistent and non-deterministic visual glitches. Furthermore, missing `isHighlighterReady` reactivity in secondary markdown panes prevents code blocks from updating once the Shiki syntax highlighter completes client-side initialization.

## What Changes

- **Scope `ShikiCodeBlock.vue` Styles**: Convert unscoped `<style>` in `frontend/components/common/ShikiCodeBlock.vue` to `<style scoped>` using `:deep(pre.shiki)` so zero-padding rules are strictly confined to standalone insight blocks and can never leak globally.
- **Isolate Markdown Renderer Code Containers**: In `frontend/composables/useMarkdownRenderer.ts`, rename code container class from `shiki-container` to `markdown-code-content` to decouple document markdown rendering from standalone component selectors.
- **Standardize Responsive Code Block Padding**: In `frontend/assets/css/main.css`, define explicit responsive padding rules (`1rem 1.25rem` on mobile, `1.25rem 1.5rem` on desktop) for `.code-block-wrapper pre.shiki` and `.code-content pre.shiki`, perfectly aligning code text with the window header buttons (`px-4 sm:px-5`).
- **Complete Shiki Reactivity Coverage**: Add `isHighlighterReady` reactivity dependency in `InterviewChallengePane.vue` and `TermExplainerModal.vue` so explanation code blocks reliably re-render when Shiki initializes.

## Capabilities

### Modified Capabilities

- `markdown-formatting`: Add requirement for style-isolated code block rendering with guaranteed minimum horizontal padding bounds regardless of route chunk injection order.
- `reader`: Ensure code blocks within reading slices maintain consistent visual indentation matching the code block header controls across all device viewports.

## Impact

- **Frontend Components**:
  - `frontend/components/common/ShikiCodeBlock.vue`
  - `frontend/composables/useMarkdownRenderer.ts`
  - `frontend/assets/css/main.css`
  - `frontend/components/today/InterviewChallengePane.vue`
  - `frontend/components/today/TermExplainerModal.vue`
- **Zero API / Backend Changes**: Pure frontend CSS isolation and Vue reactivity improvement.
- **Zero Breaking Changes**: Existing insights, quiz, and reader functionality are fully preserved.
