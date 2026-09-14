# Tasks: Smart PDF Slicing, Fallback Invariant & Mobile Refinement

## 1. Smart PDF Slicing Engine
- [x] 1.1 Refactor `PdfPigExtractor.cs` to split large chapters into <= 2,500 words per slice using page boundaries and natural paragraph breaks.
- [x] 1.2 Add unit test in `PdfPigExtractorTests.cs` verifying that even with monolithic bookmarks, no slice exceeds 2,500 words.

## 2. AI Fallback Invariant Enforcement
- [x] 2.1 Update `AiFormattedSliceResult` to include `bool IsSuccess = true`.
- [x] 2.2 Set `IsSuccess = false` in `GeminiAiService.FallbackFormatSlice`.
- [x] 2.3 Update `CurateSliceHandler.cs` and `GetTodayFocusHandler.cs` to only set `IsAiFormatted = true` and persist interview questions when `aiResult.IsSuccess == true`.

## 3. Mobile UI & Layout Fixes
- [x] 3.1 Update `frontend/pages/today.vue` loading state with `p-6 sm:p-8`, `max-w-sm sm:max-w-md`, and centered typography.
- [x] 3.2 Update `frontend/pages/today.vue` root container to `h-full` to eliminate mobile address bar overflow.

## 4. Local-First Automated Verification
- [x] 4.1 Run `dotnet test backend/tests/TechDaily.Tests` to verify all slicing and curation invariants pass.
- [x] 4.2 Run `npm test` to verify frontend tests pass.
- [x] 4.3 Re-extract and verify the ASP.NET Core 10 PDF slices in the local database and verify Slice 4 in Playwright.
