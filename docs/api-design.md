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
- **Request Body:**
  ```json
  {
    "currentPassword": "OldPassword123!",
    "newPassword": "NewPassword456!"
  }
  ```

---

## 3. Daily Focus Hub (`/api/v1/daily`)

### `GET /api/v1/daily/today`
- **Auth:** Optional / Recommended
- **Response:** Daily topic, reading chunk, scenario question, active streak.

### `POST /api/v1/daily/submit`
- **Auth:** Optional / Recommended
- **Request Body (Multipart or JSON):** `answerText` (string), `audioFile` (multipart WebM/WAV), `locale` (en/vi).
- **Response (200 OK):** Gemini 3.5 Flash scorecard review (score, strengths, missing points, improved answer).

### `POST /api/v1/daily/explain-term`
- **Auth:** Public
- **Request Body:** `{ "term": "Optimistic Locking", "category": "Database", "context": "...", "locale": "en" }`
- **Response (200 OK):** `{ "term": "...", "explanation": "...", "isCached": true }`

---

## 4. Spaced Repetition (`/api/v1/review`)

### `GET /api/v1/review/deck`
- **Auth:** Optional / Recommended
- **Response:** Due flashcard items calculated via SM-2 interval.

### `POST /api/v1/review/cards/{cardId}/grade`
- **Auth:** Optional / Recommended
- **Request Body:** `{ "qualityGrade": 5 }` (0 to 5)
- **Response:** Updated ease factor, next review date, interval days.

---

## 5. Technical Library & Notes (`/api/v1/library`, `/api/v1/notes`)

### `GET /api/v1/library/books` | `POST /api/v1/library/import`
### `GET /api/v1/notes/highlights` | `POST /api/v1/notes/highlights` | `DELETE /api/v1/notes/highlights/{id}`
