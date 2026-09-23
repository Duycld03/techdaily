# Design

## Context

See `proposal.md` for motivation. The Spaced Repetition Review Studio (`pages/review.vue`) and Quiz Arena (`pages/quiz.vue`) provide daily retention loops. Under `antfu/skills`, SM-2 grading buttons must be optimized for 320px screens, forecast charts scaled responsively, and quiz options isolated without state bleed across attempts.

## Goals / Non-Goals

**Goals:**
- Enforce `grid-cols-2 sm:grid-cols-4 gap-2.5` on `Sm2GradingButtons.vue` with minimum touch targets $\ge 44\text{px}$.
- Scale SVG / flex bars in `ReviewForecastChart.vue` to prevent label truncation on narrow screens.
- Standardize keyboard listeners (`1`, `2`, `3`, `4`, `Space`) using VueUse `useEventListener`.
- Enforce clean option badge layout (`whitespace-nowrap shrink-0`) in `pages/quiz.vue`.

**Non-Goals:**
- Altering the SM-2 mathematical progression formula ($I_1 = 1$, $I_2 = 6$, $I_n = I_{n-1} \times \text{EF}$).
- Changing backend quiz attempt submission or grading APIs.

## Decisions

### 1. 2-Column Responsive Grading on Narrow Screens
- *Rationale*: A 4-column inline row on screens $< 375\text{px}$ squeezes buttons to $< 60\text{px}$ width, causing text truncation and frequent misclicks. A 2x2 grid provides $130\text{px}+$ width per button.

### 2. VueUse `useEventListener` for Keyboard Shortcuts
- *Rationale*: Number key triggers (`1` through `4`) provide rapid review flow on desktop. Using VueUse guarantees listeners detach when navigating away from the page.

### 3. Option Badge Isolation
- *Rationale*: Rule 15 in `AGENTS.md` mandates that new quiz questions never leak historical attempt selections into active choices.

## Risks / Trade-offs

- **Risk**: Card flip animation performance on low-end mobile devices.
  - **Mitigation**: Use hardware-accelerated CSS `transform: rotateY` with `will-change: transform` and `backface-visibility: hidden`.
