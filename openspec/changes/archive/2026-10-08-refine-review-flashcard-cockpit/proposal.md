# Proposal: Refine Review Flashcard Cockpit & Eliminate Visual Echoes

## Why

The `/review` spaced repetition player currently renders three identical `<Layers>` icons in vertical succession and displays an awkward 50-character sliced question snippet as an outer title (`cleanTopicTitle`) directly above the flashcard, duplicating the question text and causing visual stutter. Additionally, backend review card projection hardcodes card category and difficulty to `FrontendWeb` and `Senior`, misattributing backend and system design cards (such as ASP.NET Core Kestrel challenges) to frontend.

## What Changes

- **Eliminate Redundant Sub-Header Bar in Review Session**: Remove the outer sub-header row in `pages/review.vue` containing the 3rd `<Layers>` icon badge, the truncated `cleanTopicTitle` (`frontMarkdown.slice(0, 50)`), `cleanSourceSubtitle`, and the duplicate "cards remaining" badge. The flashcard becomes the sole hero focus on the canvas directly beneath the page header tabs.
- **Accurate Category & Difficulty Projection in Backend**: Update `GetReviewDeckHandler.cs` and `GetReviewCardsHandler.cs` to dynamically project `Category`, `Difficulty` (mapped from QuizLevel or default), and `TopicTitle` from the underlying `SourceQuizQuestion` or `SourceDocumentChunk`, replacing the hardcoded `FrontendWeb` and `Senior` assignments.
- **Card Provenance Badges**: Update `FlashcardDeck.vue` header to render a clean, compact source provenance chip (`From Quiz Challenge` / `Từ Trắc Nghiệm`, `Reading Highlight` / `Từ Trích Đoạn`, or `Monograph Monograph`) alongside category and difficulty badges, replacing the need for an external subtitle.

## Capabilities

### Modified Capabilities
- `review`: Update the active review session visual hierarchy requirements to eliminate outer title/icon duplication, establish the unboxed direct-canvas flashcard hero as the single focal point, and mandate accurate entity-projected category/difficulty metadata in the SM-2 review deck.

## Impact

- **Backend**: `backend/src/TechDaily.Application/Features/Review/GetReviewDeck/GetReviewDeckHandler.cs`, `backend/src/TechDaily.Application/Features/Review/GetReviewCards/GetReviewCardsHandler.cs`, `ReviewCardDto.cs`.
- **Frontend**: `frontend/pages/review.vue`, `frontend/components/review/FlashcardDeck.vue`.
- **Tests**: `backend/tests/TechDaily.Tests` (verify projected Category and Difficulty for quiz mistake cards), `frontend/tests/components/reviewBentoComponents.spec.ts`, `frontend/tests/pages/review.spec.ts`.
