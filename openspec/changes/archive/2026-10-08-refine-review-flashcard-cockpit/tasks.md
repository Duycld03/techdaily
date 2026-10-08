# Tasks

## 1. Backend Application Handlers & DTO Projection

- [x] 1.1 Update `GetReviewDeckHandler.cs` to project `Category`, `Difficulty`, and `TopicTitle` from `SourceQuizQuestion`, `SourceDocumentChunk`, or `SourceHighlight.DocumentChunk` instead of hardcoded defaults (`FrontendWeb`, `Senior`).
- [x] 1.2 Update `GetReviewCardsHandler.cs` to project `Category`, `Difficulty`, and `TopicTitle` from related source entities.
- [x] 1.3 Add backend unit tests in `TechDaily.Tests` verifying entity-aware projection for cards originating from quiz mistakes, document chunks, and highlights.

## 2. Frontend Store & Component Refactoring

- [x] 2.1 Update `ReviewCard` interface in `frontend/stores/useReviewStore.ts` to include `topicTitle?: string` and align `sourceType` mapping.
- [x] 2.2 Refactor `frontend/pages/review.vue` to remove the redundant sub-header row, the 3rd `<Layers>` icon badge, `cleanTopicTitle`, `cleanSourceSubtitle`, and the duplicate remaining badge, placing `FlashcardDeck.vue` directly on the canvas as the single hero card.
- [x] 2.3 Update `frontend/components/review/FlashcardDeck.vue` header to render a compact source provenance chip (`From Quiz Challenge` / `Từ Bài Trắc Nghiệm`, `Reading Highlight` / `Từ Trích Đoạn`, `Monograph Monograph` / `Tài Liệu Chuyên Khảo`).
- [x] 2.4 Add localized provenance strings in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.

## 3. Verification & Dual-Gate Testing

- [x] 3.1 Execute backend test suite via `dotnet test` to verify review handlers and DTO mappings pass 100%.
- [x] 3.2 Execute frontend test suite via `npm test` to verify component rendering and store state pass 100%.
- [x] 3.3 Conduct automated visual verification on Desktop (1440x900) and Mobile (390x844) viewports using headless browser to inspect the clean single-hero layout and absence of repeated icons.
