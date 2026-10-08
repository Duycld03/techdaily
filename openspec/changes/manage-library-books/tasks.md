# Tasks

## 1. Backend Application Layer & Validation

- [x] 1.1 Add `Language` property to `UpdateBookRequest.cs` and validation rule (`"en"` or `"vi"`) to `UpdateBookValidator.cs`.
- [x] 1.2 Update `UpdateBookHandler.cs` to cascade `Language` updates to all surviving `DocumentChunk` records.
- [x] 1.3 Register `UpdateBookHandler` in DI container in `TechDaily.Application/DependencyInjection.cs`.
- [x] 1.4 Update unit tests in `UpdateBookHandlerTests.cs` verifying language validation and chunk cascading.

## 2. Backend API Endpoint

- [x] 2.1 Update `PATCH /api/v1/library/books/{id:guid}` endpoint in `LibraryEndpoints.cs` to map `Language` from request body.

## 3. Frontend Library Management & UI Design System

- [x] 3.1 Add Edit action trigger button (`Pencil` icon) to book cards on `/library` alongside the Delete button.
- [x] 3.2 Update `EditBookModal` in `pages/library.vue` to include an accessible `AppSelect.vue` for Document Language (`en` / `vi`).
- [x] 3.3 Update Pinia store and API update payload to pass `language`.
- [x] 3.4 Add localized strings for edit modal and actions in `en.json` and `vi.json`.
- [x] 3.5 Update unit tests in `library.spec.ts` and `stores/library.spec.ts` verifying language field rendering and payload submission.

## 4. Verification & Dual-Gate Validation

- [x] 4.1 Run backend unit tests via `dotnet test` to verify 100% passing test suites across all library and update handler tests.
- [x] 4.2 Run frontend unit tests via `npm test` to verify 100% passing test suites for library page data contracts and modal actions.
- [x] 4.3 Execute automated headless browser inspection on `/library` for Desktop (1440x900) and Mobile (390x844) viewports, verifying the updated Edit modal with Language selection.
