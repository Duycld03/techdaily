# Design: Stabilize Quiz Generator Layout and Fix Topic Fallback

## Context

In `frontend/pages/quiz.vue`, the generator tab uses a 2-column Bento grid on desktop screens (`lg:grid-cols-2 gap-6 items-stretch`).
- The left column houses the Topic & Context Hub with an expandable "Luyện đề theo sách" card containing `AppSelect`.
- The right column houses the Seniority & Action Hub with the level matrix, question count controller, and primary "Tạo Bộ Đề AI" button, laid out with `flex flex-col justify-between`.

Because of `items-stretch` combined with `justify-between`, when `isGrounded` is toggled true, the left card's height expansion forces the right card to stretch vertically as well, pushing the generate button downward. Furthermore, `handleGenerateQuiz` still accesses an obsolete `quickTopics` reference on fallback.

## Goals / Non-Goals

**Goals:**
- Eliminate layout shift (CLS) and button displacement when toggling "Luyện đề theo sách".
- Ensure the right Bento card preserves a stable, independent height and fixed button position.
- Fix topic fallback resolution in `handleGenerateQuiz` to prevent `ReferenceError`.
- Maintain clean, consistent aesthetics across desktop and mobile viewports.

**Non-Goals:**
- Restructuring other tabs in `/quiz` (`arena`, `review`, `stats`).
- Modifying backend quiz generation handlers, prompts, or API payloads.

## Decisions

### 1. Grid Alignment via `items-start`

- **Decision**: Update the grid container class from `items-stretch` to `items-start`.
- **Rationale**: Grid items in CSS default to `items-stretch`, which locks sibling heights together. Switching to `items-start` decouples column heights while maintaining clean top alignment. When the left card expands to reveal the book selection menu, the right card remains completely stationary.

```html
<!-- Before -->
<div v-if="quizStore.activeTab === 'generate'" class="grid grid-cols-1 lg:grid-cols-2 gap-6 items-stretch">

<!-- After -->
<div v-if="quizStore.activeTab === 'generate'" class="grid grid-cols-1 lg:grid-cols-2 gap-6 items-start">
```

### 2. Predictable Vertical Flow in Seniority & Action Hub

- **Decision**: Simplify the right Bento card container from `flex flex-col justify-between` to a standard vertical flow container with consistent spacing (`space-y-6`).
- **Rationale**: Removing `justify-between` prevents the container from injecting dynamic empty space above the action button. The button stays at a predictable, comfortable offset directly below the question count controller.

```html
<!-- Right Bento Container -->
<div class="glass-card p-5 sm:p-7 space-y-6">
  <!-- 2x2 Seniority Matrix -->
  ...
  <!-- 3-Tier Question Count -->
  ...
  <!-- Primary Action Button -->
  <div class="pt-2">
    <button data-testid="generate-quiz-btn" ...>
  </div>
</div>
```

### 3. Safe Topic Fallback in `handleGenerateQuiz`

- **Decision**: Update line 249 to read from `computedQuickTopics.value?.[0]` with a sensible fallback `'Technical Architecture'`.
- **Rationale**: `quickTopics` was removed in favor of `computedQuickTopics`. Safely referencing `.value?.[0]` protects against runtime exceptions when generating quizzes without manual text input.

```typescript
const chosenTopic = topic || customTopicInput.value || computedQuickTopics.value?.[0] || 'Technical Architecture'
```

## Risks / Trade-offs

- **Column Height Asymmetry**: When the book dropdown is open, the left column will be slightly taller (~455px) than the right column (~420px). This is standard for modern Bento dashboards (e.g. Linear, Vercel) and visually preferable to interactive buttons shifting or jumping under the user's cursor.
