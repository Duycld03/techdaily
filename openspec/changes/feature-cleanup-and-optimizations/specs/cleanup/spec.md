# Spec: Feature Cleanup, Grouped Navigation & Clean ERD

## 1. Grouped Navigation Standard
Both desktop sidebar (`components/layout/AppSidebar.vue`) and mobile drawer (`components/layout/AppHeader.vue`) MUST adhere to the following grouped layout:

```
[Luyện Tập / Practice]
- Luyện Tập Hôm Nay (/today)
- Lộ Trình Senior (/roadmap)
- Đấu Trường Quiz (/quiz)
- Thẻ Ôn Tập SM-2 (/review)

[Tri Thức & Ghi Nhớ / Knowledge]
- Tech Insights (/insights)
- Thư Viện Tài Liệu (/library)
- Ghi Chú & Highlights (/notes)

[Hệ Thống / System]
- Hồ Sơ Cá Nhân (/profile)
- Cài Đặt Hệ Thống (/settings)
```

## 2. Reader-to-Quiz Bridge Standard
- When viewing a book chapter in `/read/[bookId]`, the reader view MUST offer a prominent primary button to practice quizzes on that chapter.
- Navigating to `/quiz?topic=<topic>` MUST automatically populate the Quiz Arena topic input.

## 3. Database & Entity Standard
- `DailyDrills` entity contains only: `UserId`, `QuestionId`, `DocumentChunkId`, `ScheduledDate`, `Status`, `SelectedOptionIndex`, `IsCorrect`, `Score`, `AttemptCount`, `SubmittedAt`.
- No `AiReviews` table or audio storage columns are present in domain or infrastructure models.
