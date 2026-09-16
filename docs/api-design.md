# TechDaily — API Design & Contract Specification

Base URL: `/api/v1`

All responses follow RFC 7807 problem details on error. Protected endpoints require `Authorization: Bearer <jwt-token>`.

---

## 1. Authentication (`/api/v1/auth`)

### `POST /api/v1/auth/register`
- **Auth:** Public
- **Request Body:**
  ```json
  {
    "email": "user@example.com",
    "password": "SecurePassword123!",
    "name": "Alex Mercer",
    "locale": "en"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6...",
    "user": {
      "id": "8f044892-d718-43e0-ab04-503e57bc0972",
      "email": "user@example.com",
      "name": "Alex Mercer",
      "preferredLocale": "en"
    }
  }
  ```

### `POST /api/v1/auth/login`
- **Auth:** Public
- **Request Body:**
  ```json
  {
    "email": "user@example.com",
    "password": "SecurePassword123!"
  }
  ```
- **Response (200 OK):** Same as register.

### `POST /api/v1/auth/google`
- **Auth:** Public
- **Request Body:** `{ "idToken": "google-id-token" }`
- **Response (200 OK):** Same as register.

---

## 2. User Profile & Settings (`/api/v1/user`)

### `GET /api/v1/user/profile`
- **Auth:** Required (`Bearer`)
- **Response (200 OK):**
  ```json
  {
    "user": {
      "id": "8f044892-...",
      "email": "user@example.com",
      "name": "Alex Mercer",
      "avatarUrl": null,
      "preferredLocale": "en",
      "targetRole": "Senior Engineer",
      "dailyGoalMinutes": 10,
      "telegramChatId": null,
      "hasPassword": true,
      "isGoogleLinked": false
    },
    "stats": {
      "currentStreak": 5,
      "longestStreak": 12,
      "freezeCreditsRemaining": 2,
      "totalDrillsCompleted": 5,
      "averageScore": 8.6,
      "totalCardsInDeck": 14,
      "totalHighlightsSaved": 3,
      "memberSince": "2026-08-31T00:00:00Z"
    }
  }
  ```
- **Response (401 Unauthorized):** Missing or invalid JWT.

### `PUT /api/v1/user/profile`
- **Auth:** Required (`Bearer`)
- **Request Body:**
  ```json
  {
    "name": "Alex Mercer, Principal",
    "targetRole": "Principal Architect",
    "dailyGoalMinutes": 15,
    "preferredLocale": "vi",
    "telegramChatId": 123456789
  }
  ```

### `PUT /api/v1/user/change-password`
- **Auth:** Required (`Bearer`)
- **Description:** Sets or changes user password. For Google OAuth users without an existing password (`hasPassword: false`), `currentPassword` can be omitted or null.
- **Request Body (Standard Update):**
  ```json
  {
    "currentPassword": "OldPassword123!",
    "newPassword": "NewPassword456!"
  }
  ```
- **Request Body (Google Account Initial Setup):**
  ```json
  {
    "newPassword": "NewPassword456!"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "message": "Password updated successfully."
  }
  ```

---

## 3. Daily Focus Hub (`/api/v1/daily` & `/api/daily-focus`)

### `GET /api/daily-focus/today`
- **Auth:** Optional / Recommended (`Bearer`)
- **Query Params:** `bookId` (optional Guid), `chunkOrder` (optional int), `dayOrder` (optional int: 1–30), `date` (optional string), `locale` (en/vi)
- **Response (200 OK):**
  - `topic`: `TopicDto` (id, slug, title, category, difficulty, dayOrder, summary, deepDiveMarkdown)
  - `question`: `InterviewQuestionDto` (id, questionText, options: `string[]`, difficulty, expectedKeyPoints; `correctOptionIndex` and `explanationMarkdown` masked until answered)
  - `documentChunk`: `DocumentChunkDto` (id, chunkOrder, chapterTitle, originalTextMarkdown, summaryMarkdown, keyTakeaways, microQuiz, startPage, endPage)
  - `pacer`: `PacerDto` (`bookId`, `bookTitle`, `currentChunkOrder`, `totalChunks`, `progressPercent`, `totalPages`, `startPage`, `endPage`, `availableBooks: AvailableBookDto[]`)
  - `drill`: `DailyDrillDto` (id, status, selectedOptionIndex, isCorrect, score)
  - `currentStreak`: int
  - `longestStreak`: int
  - `freezeCreditsRemaining`: int

### `POST /api/daily-focus/switch-book`
- **Auth:** Required (`Bearer`)
- **Request Body (JSON):**
  ```json
  {
    "bookId": "bf7c9a22-c619-4bbf-81ac-b97ed3c482e9"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "bookId": "bf7c9a22-c619-4bbf-81ac-b97ed3c482e9",
    "bookTitle": "Designing Data-Intensive Applications",
    "currentChunkOrder": 1,
    "totalChunks": 36,
    "progressPercent": 2,
    "totalPages": 612,
    "startPage": 1,
    "endPage": 18,
    "availableBooks": [...]
  }
  ```

### `GET /api/daily-focus/chunk-challenge/{chunkId}`
- **Auth:** Public
- **Description:** High-priority scenario challenge lookup or JIT on-demand synthesis for a specific slice.
- **Response (200 OK):** `InterviewQuestionDto` (with options, difficulty, expected key points).

### `POST /api/daily-focus/drills/{id}/submit`
- **Auth:** Required (`Bearer`)
- **Request Body (JSON):**
  ```json
  {
    "selectedOptionIndex": 1,
    "locale": "en"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "isCorrect": true,
    "selectedOptionIndex": 1,
    "correctOptionIndex": 1,
    "score": 10,
    "explanationMarkdown": "Deep architectural breakdown...",
    "currentStreak": 6,
    "longestStreak": 12,
    "totalDrillsCompleted": 6,
    "averageScore": 9.5
  }
  ```

### `POST /api/v1/daily/explain-term`
- **Auth:** Public
- **Request Body:**
  ```json
  {
    "term": "Optimistic Locking",
    "category": "Database", // optional, defaults to "Software Architecture"
    "context": "Surrounding excerpt context...", // optional
    "locale": "vi" // "en" or "vi"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "term": "Optimistic Locking",
    "explanation": "Markdown-formatted 2-sentence explanation...",
    "locale": "vi"
  }
  ```

---

## 4. Curriculum Roadmap (`/api/v1/curriculum`)

### `GET /api/v1/curriculum/roadmap`
- **Auth:** Required (`Bearer`)
- **Response (200 OK):**
  ```json
  {
    "totalDays": 30,
    "completedDaysCount": 4,
    "currentActiveDay": 5,
    "overallProgressPercentage": 13.3,
    "modules": [
      {
        "category": 0,
        "moduleTitle": "Frontend & Browser Internals",
        "description": "Vue 3 Reactivity, Rendering Strategies, Browser Pipeline, Web Vitals, State Management, WebSockets & Bundlers.",
        "startDay": 1,
        "endDay": 7,
        "completedCount": 4,
        "totalCount": 7,
        "days": [
          {
            "dayOrder": 1,
            "slug": "vue3-reactivity-engine",
            "title": "Vue 3 Reactivity Engine Under The Hood",
            "summary": "Deep dive into Proxy, Reflect, track(), trigger()...",
            "difficulty": 1,
            "isCompleted": true,
            "isActiveToday": false,
            "isUnlocked": true,
            "drillScore": 10
          }
        ]
      }
    ]
  }
  ```
- **Response (401 Unauthorized):** Missing or invalid JWT token.

---

## 5. Tech Insights Feed (`/api/v1/insights`)

### `GET /api/v1/insights/feed`
- **Auth:** Public / Optional (`Bearer`)
- **Query Params:**
  - `category` *(optional int)*: `0=FrontendWeb`, `1=BackendDotNet`, `2=DatabaseStorage`, `3=SystemDesign`
  - `tag` *(optional string)*: Filter by keyword tag
  - `page` *(optional int)*: Page index (default: 1)
  - `pageSize` *(optional int)*: Items per page (default: 10, max: 50)
  - `randomize` *(optional bool)*: Randomize feed order
  - `onlyBookmarked` *(optional bool)*: If `true` and authenticated, returns only insights bookmarked by the current user.
- **Response (200 OK):**
  ```json
  {
    "insights": [
      {
        "id": "56314f47-7596-4d30-ae82-b3ca7e9f0a95",
        "slug": "dotnet-channels-backpressure",
        "title": "High-Throughput In-Memory Queues with System.Threading.Channels",
        "category": 1,
        "tags": ["dotnet10", "csharp13", "channels"],
        "summaryMarkdown": "`BlockingCollection<T>` relies on heavy monitor locks...",
        "problemSnippet": "// ❌ BAD: Heavy lock synchronization...",
        "solutionSnippet": "// ✅ SENIOR PATTERN: Bounded Channel...",
        "underTheHoodMarkdown": "### Lock-Free Ring Buffer Mechanics...",
        "benchmarkStats": "⚡ Throughput: 4.2M ops/sec",
        "sourceUrl": "https://learn.microsoft.com/en-us/dotnet/core/extensions/channels",
        "likesCount": 12,
        "bookmarksCount": 4,
        "isBookmarkedByUser": true
      }
    ],
    "totalCount": 8,
    "page": 1,
    "pageSize": 10,
    "hasMore": false
  }
  ```

### `POST /api/v1/insights/generate`
- **Auth:** Public / Authenticated
- **Request Body (JSON):**
  ```json
  {
    "preferredCategory": 1,
    "preferredTopic": "Span<T> vs Memory<T> in high-throughput parsing",
    "locale": "en"
  }
  ```
- **Response (200 OK):** Synthesized `TechInsightDto` saved to the catalog.

### `POST /api/v1/insights/{id}/bookmark`
- **Auth:** Required (`Bearer`)
- **Description:** Toggles bookmark status for the authenticated user. Increments or decrements `BookmarksCount` and persists state in `UserInsightBookmarks`.
- **Response (200 OK):**
  ```json
  {
    "insightId": "56314f47-7596-4d30-ae82-b3ca7e9f0a95",
    "isBookmarked": true,
    "totalBookmarks": 5
  }
  ```
- **Response (401 Unauthorized):** Returned when no valid JWT is present.

---

## 6. Spaced Repetition (`/api/v1/review`)

> ℹ️ **Updated Production Specifications:** See [Section 10: Review & Spaced Repetition](#10-review--spaced-repetition-apiv1review) for complete contracts, idempotency rules, and 1-Click Flashcard synthesis from reading highlights.

### `GET /api/v1/review/deck`
- **Auth:** Optional / Recommended
- **Response:** Due flashcard items calculated via SM-2 interval.

### `POST /api/v1/review/cards/{cardId}/grade`
- **Auth:** Optional / Recommended
- **Request Body:** `{ "qualityGrade": 5 }` (0 to 5)
- **Response:** Updated ease factor, next review date, interval days.

---

## 7. Technical Library (`/api/v1/library`)

### `GET /api/v1/library/books`
- **Auth:** Public
- **Query Params:** `category` (optional int), `search` (optional string)
- **Response (200 OK):** List of published, non-deleted books with metadata and total chunk count.

### `GET /api/v1/library/books/{id}`
- **Auth:** Public
- **Response (200 OK):** Full book details including all ordered `DocumentChunk` slices.

### `POST /api/v1/library/import`
- **Auth:** Required (`Bearer`)
- **Request Body:**
  ```json
  {
    "title": "Clean Architecture in .NET 10",
    "markdownContent": "# Domain Layer\n\n...",
    "category": 0,
    "sourceUrl": "https://github.com/...",
    "language": "en"
  }
  ```
- **Response (201 Created):** Created `BookDto`.

### `GET /api/v1/library/books/{id}/status`
- **Auth:** Public
- **Description:** Real-time ingestion progress polling for background PDF processing.
- **Response (200 OK):**
  ```json
  {
    "bookId": "bf7c9a22-c619-4bbf-81ac-b97ed3c482e9",
    "title": "Clean Architecture in .NET 10",
    "status": "Processing",
    "totalPages": 350,
    "processedPages": 120,
    "progressPercent": 34,
    "errorMessage": null
  }
  ```

### `POST /api/v1/library/upload-pdf`
- **Auth:** Required (`Bearer`)
- **Content-Type:** `multipart/form-data` (Supports up to **300 MB**, 8,000+ pages via disk spooling & Channels queue)
- **Form Fields:**
  - `file`: Binary PDF file stream (`.pdf`)
  - `title` *(optional)*: Custom book title
  - `category` *(optional int)*: Category enum value
  - `language` *(optional string)*: `en` or `vi`
- **Response (202 Accepted):**
  ```json
  {
    "book": {
      "id": "bf7c9a22-c619-4bbf-81ac-b97ed3c482e9",
      "title": "Microsoft.Win32 Namespace (.NET 10.0)",
      "slug": "microsoftwin32-namespace-net-100-aee32b",
      "sourceType": 0,
      "category": 0,
      "authorOrSourceUrl": "microsoft.win32-net-10.0.pdf",
      "totalChunks": 0,
      "isPublished": false,
      "createdAt": "2026-08-31T17:44:31.0257833+00:00"
    },
    "message": "PDF queued for background ingestion. Processing up to 300MB asynchronously."
  }
  ```

### `POST /api/v1/library/crawl-url`
- **Auth:** Required (`Bearer`)
- **Request Body:**
  ```json
  {
    "url": "https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "title": "Dependency injection in ASP.NET Core",
    "sourceUrl": "https://learn.microsoft.com/...",
    "markdownContent": "## Keyed services\n\n```csharp\n...",
    "estimatedWordCount": 3200
  }
  ```

### `DELETE /api/v1/library/books/{id}`
- **Auth:** Required (`Bearer`)
- **Response (204 No Content):** Soft-deletes document and all associated chunks.

---

## 8. Architectural Highlights & Saved Insights Hub (`/api/v1/notes`)

The `/notes` page serves as the centralized knowledge base with a 2-tab management hub:
1. **🔖 Saved Insights:** Powered by `GET /api/v1/insights/feed?onlyBookmarked=true` and toggled via `POST /api/v1/insights/{id}/bookmark`.
2. **🖍️ Reading Highlights:** Powered by the dedicated highlights endpoints below:

### `GET /api/v1/notes/highlights`
- **Auth:** Required (`Bearer`)
- **Response (200 OK):** User's highlighted excerpts with tags and timestamps.

### `POST /api/v1/notes/highlights`
- **Auth:** Required (`Bearer`)
- **Request Body:** `{ "documentChunkId": "...", "selectedText": "...", "note": "...", "tags": ["csharp"] }`
- **Response (201 Created):** Created highlight object.

### `DELETE /api/v1/notes/highlights/{id}`
- **Auth:** Required (`Bearer`)
- **Response (204 No Content):** Deletes specified user highlight.

---

## 9. Senior Technical Interview Quiz & Mastery Arena (`/api/v1/quiz`)

The `/quiz` route powers high-intensity interview scenario drills generated with Gemini 3.1 Flash Lite and tracks question-level spaced repetition mastery in PostgreSQL.

### `POST /api/v1/quiz/generate`
- **Auth:** Required (`Bearer`)
- **Description:** Normalizes the topic string (stripping conversational prefixes such as `"về "`, `"about "`), queries PostgreSQL for unattempted questions for the requesting user, and synthesizes missing questions on-demand via Gemini 3.1 Flash Lite.
- **Session State Guarantee:** In a newly generated quiz batch, `lastSelectedOptionIndex` and `isLastAnswerCorrect` are guaranteed to be returned as `null` to ensure client sessions always start with an unselected state. Cumulative statistics (`isMastered`, `correctCount`, `incorrectCount`) reflect user history.
- **Request Body:**
  ```json
  {
    "topic": "Clean Architecture in .NET 10",
    "level": 3,
    "count": 5,
    "category": null,
    "locale": "vi"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "questions": [
      {
        "id": "439b8361-ed91-4bb2-83ba-7cc2e632418e",
        "topic": "Clean Architecture in .NET 10",
        "category": 1,
        "level": 3,
        "questionText": "Trong Clean Architecture, tại sao Domain Layer không được phụ thuộc vào bất kỳ framework hoặc cơ sở dữ liệu nào?",
        "options": [
          "Để cô lập Core Business Logic khỏi biến động công nghệ bên ngoài",
          "Để tăng tốc độ biên dịch ứng dụng",
          "Bắt buộc bởi compiler của C#",
          "Để giảm dung lượng file DLL"
        ],
        "correctOptionIndex": 0,
        "explanationMarkdown": "### Phân Tích Kỹ Thuật Chuyên Sâu\n...",
        "tags": ["clean-architecture", "senior", "domain-model"],
        "isMastered": false,
        "lastSelectedOptionIndex": null,
        "isLastAnswerCorrect": null,
        "correctCount": 0,
        "incorrectCount": 0
      }
    ],
    "topic": "Clean Architecture in .NET 10",
    "level": 3,
    "totalCount": 5
  }
  ```

### `POST /api/v1/quiz/submit`
- **Auth:** Required (`Bearer`)
- **Description:** Evaluates the selected option, updates user answer history, increments consecutive streaks, and marks question as `isMastered: true` upon 2 consecutive correct answers.
- **Request Body:**
  ```json
  {
    "questionId": "439b8361-ed91-4bb2-83ba-7cc2e632418e",
    "selectedOptionIndex": 0
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "questionId": "439b8361-ed91-4bb2-83ba-7cc2e632418e",
    "isCorrect": true,
    "correctOptionIndex": 0,
    "explanationMarkdown": "### Phân Tích Kỹ Thuật Chuyên Sâu\n...",
    "isMastered": false,
    "correctCount": 1,
    "incorrectCount": 0,
    "consecutiveCorrectCount": 1
  }
  ```

### `GET /api/v1/quiz/review-queue`
- **Auth:** Required (`Bearer`)
- **Description:** Returns pending unmastered questions for targeted review.
- **Review State Guarantee:** Unlike `/generate`, the review queue retains the user's historical `lastSelectedOptionIndex` and `isLastAnswerCorrect` to assist post-mortem inspection of previous mistakes.
- **Query Params:** `page` (default 1), `pageSize` (default 20)
- **Response (200 OK):**
  ```json
  {
    "questions": [ ... ],
    "totalCount": 8,
    "page": 1,
    "pageSize": 20
  }
  ```

### `GET /api/v1/quiz/stats`
- **Auth:** Required (`Bearer`)
- **Response (200 OK):**
  ```json
  {
    "totalAttempted": 42,
    "totalCorrect": 36,
    "totalIncorrect": 6,
    "masteredCount": 15,
    "accuracyRate": 85.7,
    "weakestTopics": [
      { "topic": "PostgreSQL MVCC", "accuracy": 50.0, "totalAttempts": 4 }
    ],
    "strongestTopics": [
      { "topic": ".NET Memory & GC", "accuracy": 100.0, "totalAttempts": 8 }
    ]
  }
  ```

---

## 10. Review & Spaced Repetition (`/api/v1/review`)

The Spaced Repetition system powers active recall drills based on the SuperMemo SM-2 algorithm. Cards originate from curriculum topics (`SourceType = Topic`), highlighted reading passages (`SourceType = Highlight`), or failed interview quiz questions (`SourceType = QuizMistake`).

### `POST /api/v1/review/from-highlight`
- **Route Aliases:** `POST /api/v1/review/from-highlight`, `POST /api/v1/review/cards/from-highlight`
- **Auth:** Required (`Bearer`)
- **Description:** Synthesizes an active recall card (front question and back answer) using Gemini 3.5 Flash Lite from any saved user highlight (`UserHighlight`) in `/notes` or `/read/[bookId]`.
- **Idempotency Guarantee:** Automatically checks `SpacedRepetitionCards` for existing records with `UserId == CurrentUserId` and `SourceHighlightId == HighlightId`. If an existing card is found, it returns the persisted card immediately without re-invoking Gemini or creating duplicates.
- **Request Body:**
  ```json
  {
    "highlightId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "locale": "en"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "cardId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "front": "**Architectural Concept**: What are the trade-offs of optimistic concurrency control?",
    "back": "Optimistic concurrency control assumes conflicts are rare, checking version tokens on commit. It avoids long-lived locks but increases transaction aborts under high write contention."
  }
  ```
- **Response (404 Not Found):** Specified highlight does not exist or does not belong to the user.

### `GET /api/v1/review/due`
- **Route Aliases:** `GET /api/v1/review/due`, `GET /api/v1/review/deck`
- **Auth:** Required (`Bearer`)
- **Description:** Retrieves all flashcards due for review on or before the requested date (`NextReviewDate <= date`).
- **Query Params:**
  - `date` *(optional string)*: Target evaluation date in `YYYY-MM-DD` format (defaults to UTC today).
- **Response (200 OK):**
  ```json
  {
    "cards": [
      {
        "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
        "sourceType": 1,
        "front": "**Architectural Concept**: What are the trade-offs of optimistic concurrency control?",
        "back": "Optimistic concurrency control assumes conflicts are rare...",
        "repetitionCount": 2,
        "easeFactor": 2.50,
        "intervalDays": 6,
        "nextReviewDate": "2026-09-17",
        "status": 0,
        "topicTitle": "Designing Data-Intensive Applications",
        "topicSummary": "Transactions and Concurrency Control"
      }
    ],
    "totalDue": 1
  }
  ```

### `POST /api/v1/review/grade`
- **Route Aliases:** `POST /api/v1/review/grade`, `POST /api/v1/review/cards/{cardId}/grade`
- **Auth:** Required (`Bearer`)
- **Description:** Applies user recall quality grade and recalculates ease factor ($EF$), interval ($I$), and next review date ($NextReviewDate$) using the SuperMemo SM-2 algorithm:
  - $EF' = EF + (0.1 - (5 - q) \times (0.08 + (5 - q) \times 0.02))$, clamped to $[1.30, 2.50]$.
  - If $q < 3$: Repetition count resets to 0, Interval resets to 1 day.
  - If $q \ge 3$: Interval progression follows $I_1 = 1$, $I_2 = 6$, $I_n = I_{n-1} \times EF$.
- **Quality Grades:**
  - `0`: *Again* (complete blackout / reset)
  - `3`: *Hard* (recalled with significant difficulty)
  - `4`: *Good* (successful recall with slight hesitation)
  - `5`: *Easy* (instant, effortless recall)
- **Request Body:**
  ```json
  {
    "cardId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "qualityGrade": 4
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "cardId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "repetitionCount": 3,
    "easeFactor": 2.50,
    "intervalDays": 15,
    "nextReviewDate": "2026-10-02",
    "status": 0
  }
  ```

---

## 11. Web Push Notifications & Schedule Settings (`/api/v1/notifications`)

The notification engine dispatches daily curriculum reminders and streak preservation alerts across browser Web Push (VAPID) and the Telegram Bot API.

### `POST /api/v1/notifications/push/subscribe`
- **Auth:** Required (`Bearer`)
- **Description:** Registers or updates a browser Web Push subscription (`UserPushSubscriptions`) for the authenticated user device. Persists encryption keys (`p256dh`, `auth`), registers user agent, automatically updates user IANA timezone, and activates push notifications (`IsPushEnabled = true`).
- **Brave Browser Guidance:** On Brave, users must enable *"Use Google services for push messaging"* under `brave://settings/privacy` to avoid push service connection rejections.
- **Request Body:**
  ```json
  {
    "endpoint": "https://fcm.googleapis.com/fcm/send/dK9v...",
    "keys": {
      "p256dh": "BNcRdreALRFXTkOOUHK1EtK2wtaz5Ry4YfYCA_0QT9h0rV30...",
      "auth": "tBHItJI5svbpez7KI4CCXg=="
    },
    "userAgent": "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36...",
    "timeZone": "Asia/Ho_Chi_Minh"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "success": true
  }
  ```
- **Response (400 Bad Request):** Returned when endpoint or key parameters are missing or malformed.

### `POST /api/v1/notifications/push/test`
- **Auth:** Required (`Bearer`)
- **Description:** Sends an immediate test Web Push notification to all active devices registered by the current user to verify VAPID signing and Service Worker delivery.
- **Notification Payload:**
  - **Title:** `TechDaily Test Push 🚀`
  - **Body:** `Web Push notifications are successfully configured and active!`
  - **Url:** `/today`
  - **Tag:** `techdaily-test`
- **Response (200 OK):**
  ```json
  {
    "success": true,
    "sent": 1,
    "total": 1
  }
  ```
- **Response (400 Bad Request):** Returned when user has no active push subscriptions.

### `PUT /api/v1/notifications/schedule`
- **Route Aliases:** `PUT /api/v1/notifications/schedule`, `PUT /api/v1/user/profile`
- **Auth:** Required (`Bearer`)
- **Description:** Updates the user's daily study dispatch schedule (`preferredStudyTime`), evening streak alert (`streakAlertTime`), and local IANA timezone (`timeZone`).
  - **Morning Curriculum Push:** Dispatched at `preferredStudyTime` (default: `08:00`).
  - **Streak Retention Push:** Dispatched at `streakAlertTime` (default: `20:00`).
- **Request Body:**
  ```json
  {
    "preferredStudyTime": "08:00",
    "streakAlertTime": "20:00",
    "timeZone": "Asia/Ho_Chi_Minh",
    "isPushEnabled": true
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "message": "Notification schedule updated successfully.",
    "preferredStudyTime": "08:00",
    "streakAlertTime": "20:00",
    "timeZone": "Asia/Ho_Chi_Minh",
    "isPushEnabled": true
  }
  ```

### Supporting Push Endpoints
- **`GET /api/v1/notifications/push/vapid-public-key`**: Returns the server VAPID public key for browser Service Worker push manager subscription.
- **`POST /api/v1/notifications/push/unsubscribe`**: Unregisters a device endpoint. If no remaining subscriptions exist, automatically disables `IsPushEnabled`.

---

## 12. Knowledge Base & Export (`/api/v1/notes` & `/api/v1/library`)

### `GET /api/v1/notes/export/{bookId}`
- **Route Aliases:** `GET /api/v1/notes/export/{bookId}`, `GET /api/v1/library/books/{bookId}/export-markdown`
- **Auth:** Required (`Bearer`)
- **Description:** Streams a complete, structured Markdown document containing book outline chapters, executive summaries, key takeaways, and user highlights with reflections and tags. Formatted with YAML frontmatter ready for second-brain tools (Obsidian, Logseq).
- **Headers:**
  - `Accept: text/markdown`
- **Response (200 OK):**
  - **Content-Type:** `text/markdown; charset=utf-8`
  - **Content-Disposition:** `attachment; filename="{slug}-notes.md"`
  - **Sample Markdown Output:**
    ```markdown
    ---
    book: "Designing Data-Intensive Applications"
    author: "Martin Kleppmann"
    exported_at: 2026-09-17
    total_chapters: 12
    total_highlights: 5
    tags: [techdaily, architecture, notes]
    ---

    # Designing Data-Intensive Applications

    *Exported from TechDaily on 2026-09-17*

    ---

    ## Chapter 1: Reliable, Scalable, and Maintainable Applications

    ### Executive Summary
    Core architectural foundations of distributed data systems focusing on fault tolerance, throughput percentiles, and operational manageability.

    ### Key Takeaways
    - Reliability means making systems work correctly even when faults occur.
    - Scalability describes a system's ability to cope with increased load (evaluated via p95 and p99 latency).

    ### Highlights & Engineering Reflections

    > "Faults are defined as one component deviating from spec, whereas a failure is when the system as a whole stops providing the required service."
    >
    > **Personal Note:** Critical architectural distinction: design systems for fault containment to prevent cascading failures.
    >
    > *Tags: `#reliability` `#distributed-systems`*
    ```
- **Response (404 Not Found):** Specified `bookId` does not exist or has been deleted.


