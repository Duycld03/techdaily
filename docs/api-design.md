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

## 3. Daily Focus Hub (`/api/v1/daily`)

### `GET /api/v1/daily/today`
- **Auth:** Optional / Recommended (`Bearer`)
- **Query Params:** `dayOrder` (optional int: 1–30), `date` (optional string), `locale` (en/vi)
- **Response (200 OK):**
  - `topic`: `TopicDto` (id, slug, title, category, difficulty, dayOrder, summary, deepDiveMarkdown)
  - `question`: `InterviewQuestionDto` (id, questionText, options: `string[]`, difficulty, expectedKeyPoints; `correctOptionIndex` and `explanationMarkdown` masked until reviewed)
  - `documentChunk`: `DocumentChunkDto` (id, chunkOrder, chapterTitle, originalTextMarkdown, summaryMarkdown, keyTakeaways, microQuiz)
  - `drill`: `DailyDrillDto` (id, status, selectedOptionIndex, isCorrect, score)
  - `currentStreak`: int
  - `longestStreak`: int
  - `freezeCreditsRemaining`: int

### `POST /api/v1/daily/drills/{id}/submit`
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

### `POST /api/v1/library/upload-pdf`
- **Auth:** Required (`Bearer`)
- **Content-Type:** `multipart/form-data` (Supports up to 200 MB, max 800 pages)
- **Form Fields:**
  - `file`: Binary PDF file stream (`.pdf`)
  - `title` *(optional)*: Custom book title
  - `category` *(optional int)*: Category enum value
  - `language` *(optional string)*: `en` or `vi`
- **Response (201 Created):**
  ```json
  {
    "book": {
      "id": "bf7c9a22-c619-4bbf-81ac-b97ed3c482e9",
      "title": "Microsoft.Win32 Namespace (.NET 10.0)",
      "slug": "microsoftwin32-namespace-net-100-aee32b",
      "sourceType": 0,
      "category": 0,
      "authorOrSourceUrl": "microsoft.win32-net-10.0.pdf",
      "totalChunks": 67,
      "isPublished": true,
      "createdAt": "2026-08-31T17:44:31.0257833+00:00"
    }
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

