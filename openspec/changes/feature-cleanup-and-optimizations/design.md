# Design: Feature Cleanup, ERD Standardization & UI Navigation Grouping

## Architecture & Design Decisions

### 1. Navigation Architecture (Desktop & Mobile Parity)
The platform is categorized into 3 functional domains:
- **Practice (`nav.group_practice` / Luyện Tập):** `/today` (Daily Focus), `/roadmap` (Curriculum), `/quiz` (AI Quiz Arena), `/review` (Spaced Repetition).
- **Knowledge (`nav.group_knowledge` / Tri Thức & Ghi Nhớ):** `/insights` (Tech Insights), `/library` (Document Library), `/notes` (Highlights & Bookmarks).
- **System (`nav.group_account` / Hệ Thống):** `/profile` (User Profile & Stats), `/settings` (Preferences & Integrations).

Both `AppSidebar.vue` (desktop) and `AppHeader.vue` (mobile drawer) consume the same structured `navGroups` definition with localized section headers and distinct iconography.

### 2. Cross-Feature Reader-to-Quiz Bridge
In `frontend/pages/read/[bookId].vue`, each chapter concludes with a primary action button **"Luyện Quiz Chương Này"**.
- Action: Navigates to `/quiz?topic=${encodeURIComponent(chapterTitle)}`.
- In `frontend/pages/quiz.vue`, `onMounted()` checks `route.query.topic` and initializes `customTopicInput` without triggering heavy AI generation until user confirmation.

### 3. Database & Clean Architecture Refactoring
- **Removed Entities:** `AiReview.cs`
- **Removed Domain Properties from `DailyDrill`:** `UserAnswerText`, `UserAudioUrl`, `AiReview` navigation property.
- **Removed Services:** `IAudioStorageService`, `LocalAudioStorageService.cs`, `IAiReviewService`.
- **Database Migration:** Generated `20260902142808_RemoveAiReviewsAndPruneDailyDrill` to drop the table `AiReviews` and unused columns.
