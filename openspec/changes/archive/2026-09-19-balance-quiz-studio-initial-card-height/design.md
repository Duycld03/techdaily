# Design: Balance Quiz Studio Initial Card Height and Single-Side Expansion

## Context

In `frontend/pages/quiz.vue`, when `quizStore.activeTab === 'generate'`, the desktop interface renders a 2-column Bento Grid (`grid grid-cols-1 lg:grid-cols-2 gap-6`).
- **Left Bento (Topic & Context Hub)**: Contains custom topic input, quick topic chips, and the "Luyện Đề Theo Sách" toggle card. In the unselected state, its natural height is ~382px.
- **Right Bento (Seniority & Generation Controls)**: Contains the 2x2 level matrix, 3-tier question count pills, and primary action button. Its natural height is ~414px.

When grid alignment is statically set to `items-start`, the left card ends ~32px higher than the right card, resulting in an uneven bottom edge. Conversely, if grid alignment is statically set to `items-stretch`, expanding the left card forces the right card to stretch as well, pushing its buttons.

## Goals / Non-Goals

**Goals:**
- Perfectly align the bottom edges of both Bento cards in their default unselected state (`!isGrounded`) so they are equal in height (`ngang nhau`).
- Anchor the "Luyện Đề Theo Sách" toggle card to the bottom of the left Bento card.
- Ensure that toggling "Luyện Đề Theo Sách" ON expands ONLY the left Bento card downwards to reveal the book selector, while the right Bento card remains completely stationary.
- Pure CSS solution: 0 JavaScript observers, 0 resize listeners, 0 DOM refs, 100% SSR safe and zero hydration lag.

**Non-Goals:**
- Modifying the internal contents or layout of the right Bento card.
- Changing API contracts, question generation logic, or backend services.

## Decisions

### 1. Conditional Grid Alignment (`items-stretch` vs `items-start`)

- **Decision**: Dynamically switch the grid container's alignment class based on `isGrounded`:
  ```html
  <div
    v-if="quizStore.activeTab === 'generate'"
    :class="[
      'grid grid-cols-1 lg:grid-cols-2 gap-6',
      isGrounded ? 'items-start' : 'items-stretch'
    ]"
  >
  ```
- **Rationale**:
  - **When `!isGrounded` (unselected state)**: The grid applies `items-stretch`. Both columns are stretched to the height of the taller column (the right card, ~414px). Both cards have mathematically identical heights and perfectly aligned bottom edges.
  - **When `isGrounded` (toggled ON)**: The grid instantly switches to `items-start`. The left card expands downward to ~490px to show the book selector. Because the grid is now top-aligned, the right card does NOT stretch and remains at its natural height (~414px). The generate button does not move.

### 2. Left Bento Flex Distribution (`flex flex-col justify-between`)

- **Decision**: Structure the left Bento card with `flex flex-col justify-between`:
  ```html
  <div class="glass-card p-5 sm:p-7 space-y-6 flex flex-col justify-between">
    <div class="space-y-6">
      <!-- Topic Input & Quick Topic Chips -->
    </div>

    <!-- Grounded in Book Toggle Card (Anchored to Bottom) -->
    <div class="p-4 sm:p-5 rounded-2xl ... mt-4">
      ...
    </div>
  </div>
  ```
- **Rationale**: When stretched to match the right card's height, `justify-between` places the extra ~32px gap between the quick topic chips and the book toggle card, pushing the toggle card cleanly to the bottom edge in harmony with the right card.

## Risks / Trade-offs

- **Zero JavaScript Overhead**: Completely eliminates the need for `useElementSize`, `ResizeObserver`, template refs, or client-only lifecycle hooks.
- **SSR/Hydration Invariant**: Since `isGrounded` defaults to `false` on both server and client, initial SSR HTML renders with `items-stretch` and hydration is 100% identical without layout shift.
