# Design: Today Reader Refinement & Architectural Trade-off Challenge

## Architectural Overview & Separation of Concerns

```
                  ┌─────────────────────────────────────────────────────────┐
                  │                 Today Page (/today)                     │
                  └───────────────────────────┬─────────────────────────────┘
                                              │
                    ┌─────────────────────────┴─────────────────────────┐
                    ▼                                                   ▼
     ┌─────────────────────────────┐                     ┌─────────────────────────────┐
     │    Left Pane: Deep Reader   │                     │  Right Pane: Architecture   │
     │     (DocReaderPane.vue)     │                     │     Trade-off Challenge     │
     └──────────────┬──────────────┘                     │ (InterviewChallengePane.vue)│
                    │                                    └──────────────┬──────────────┘
        ┌───────────┴───────────┐                                       │
        ▼                       ▼                         ┌─────────────┴─────────────┐
  Authoritative           Key Takeaways,                  ▼                           ▼
 Technical Deep Dive     Source Excerpt &           Production Scenario        Architecture
 (Shiki Code Blocks &   Benchmark Snippet            & Constraints            Proposal Cards
  Floating Explainer)                                                        (Pros/Cons/Failure)
```

## 1. Left Pane: Distraction-Free Deep Reading (`DocReaderPane.vue`)
- **Removal of `MicroQuizCard`:**
  - The `<MicroQuizCard>` component is completely removed from the template.
  - The `.micro-quiz-container` CSS wrapper is deleted.
  - The `handleMouseUp` event handler drops the check for `target.closest('.micro-quiz-container')`, preventing stale selectors.
- **Reading Progression:**
  - Reader flows uninterrupted from the document title and summary through the rendered deep dive, followed optionally by authoritative source excerpt and benchmark context.
  - Text selection toolbar (teleported to body) remains fully active for Gemini term explanations, note highlights, and clipboard copy.

## 2. Right Pane: Architectural Trade-off Challenge (`InterviewChallengePane.vue`)
- **Card Anatomy:**
  - **Scenario & Constraints Header:** Displays system domain (e.g. *Distributed Systems*, *High-Concurrency Storage*), seniority tag (*Senior / Staff*), and clear operational constraints.
  - **Proposal Cards:** Each option represents an architectural design pattern (e.g., Option A: In-Memory Sharded Queue, Option B: Distributed Lock with Redlock, Option C: Optimistic Concurrency Control).
  - **Review State:** After submission, options display status badges (`Optimal Choice`, `Your Choice`) with distinct color treatments (emerald for optimal, rose for suboptimal).
  - **Principal Review Deep Dive:** The explanation section is rendered inside a styled prose container, highlighting why the optimal architecture fits the operational constraints while dissecting failure modes of alternatives under scale.

## 3. Localization & Responsive Typography Invariants
- **Typography:**
  - Mobile body text: at least `text-sm` (14px).
  - Desktop body text: `text-base` (16px) or `text-lg` (18px) for comfortable reading.
  - Micro text (`text-xs` / 12px) strictly restricted to badges, tags, and status chips.
- **Bilingual Layout:**
  - All labels and status badges use `whitespace-nowrap shrink-0` with flex gaps to prevent text clipping across English and Vietnamese translations.
  - All copy is keyed through `$t('today.*')` in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.
