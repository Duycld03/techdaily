# Proposal: Balance Quiz Studio Initial Card Height and Single-Side Expansion

## Why

In the `/quiz` generation studio (`frontend/pages/quiz.vue`), when "Luyện Đề Theo Sách" is in its default unselected state (`!isGrounded`), the left Bento card (Topic & Context Hub) is slightly shorter (~382px) than the right Bento card (Seniority & Generation Controls, ~414px). This creates an uneven vertical gap where the bottom edge of the left card does not align with the right card.

Users expect both Bento cards to be visually balanced and equal in height (aligned horizontally at both the top and bottom edges) in their initial state. When the user clicks "Luyện Đề Theo Sách", only the left card should expand downwards to reveal the book selector, leaving the right card completely stationary and untouched.

## What Changes

- **Equal Height Alignment in Default State**:
  - Structure the left Bento card (Topic & Context Hub) as a flex column container (`flex flex-col justify-between`), placing the topic input and quick topic chips at the top and anchoring the "Luyện Đề Theo Sách" toggle card at the bottom.
  - Establish an initial minimum height on the left card matching the right card (via responsive CSS `lg:min-h-[415px]` and client-side `useElementSize` height tracking), ensuring both cards share the exact same top and bottom baseline when unselected.
- **Single-Side Downward Expansion**:
  - When "Luyện Đề Theo Sách" is activated (`isGrounded = true`), the left card expands downward beyond the initial baseline to accommodate the `AppSelect` book selector.
  - The right card remains top-aligned at its natural height, keeping the level picker, question count, and primary "Tạo Bộ Đề AI" button stationary without any layout shift.
- **Zero Breaking Changes**:
  - No backend changes, no database migrations, and no API contract modifications.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `quiz`: Update the Interview Quiz Studio Visual Layout & Interactive Tokens requirement to specify initial equal-height bottom alignment between the two Bento cards and single-side downward expansion for the book selector.

## Impact

- **Frontend**: `frontend/pages/quiz.vue` and `frontend/tests/pages/quiz.spec.ts`.
- **Specs**: `openspec/specs/quiz/spec.md`.
- **User Experience**: Harmonious, balanced 50/50 Bento grid on page load, with smooth localized expansion restricted to the left card upon toggling book selection.
