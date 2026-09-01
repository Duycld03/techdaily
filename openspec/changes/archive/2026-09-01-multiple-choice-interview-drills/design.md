# Technical Design: Multiple-Choice Senior Scenario Interview Drills

## 1. Architectural Layers & Boundaries

In accordance with Clean Architecture principles:
- **`TechDaily.Domain`**:
  - `InterviewQuestion`: Add `Options` (`List<string>`), `CorrectOptionIndex` (`int`), and `ExplanationMarkdown` (`string`).
  - `DailyDrill`: Add `SelectedOptionIndex` (`int?`), `IsCorrect` (`bool?`), and `Score` (`int?`). Add domain method `SubmitOption(int selectedIndex, bool isCorrect, int score)`.
- **`TechDaily.Application`**:
  - DTOs:
    - Update `InterviewQuestionDto` with `Options: List<string>` (and optional `ExplanationMarkdown`, `CorrectOptionIndex` only populated post-review).
    - Update `SubmitDailyDrillRequest` with `SelectedOptionIndex: int?`.
    - Update `SubmitDailyDrillResponse` with `IsCorrect: bool`, `CorrectOptionIndex: int`, `Score: int`, and `ExplanationMarkdown: string`.
  - Use-Case Handlers:
    - Update `SubmitDailyDrillHandler` to evaluate multiple-choice submissions deterministically, update `DailyDrill`, update `StreakRecord`, and create `SpacedRepetitionCard` on incorrect answers.
    - Update `GetTodayFocusHandler` to map new `InterviewQuestion` fields and hide `CorrectOptionIndex` / `ExplanationMarkdown` if drill is pending.
- **`TechDaily.Infrastructure`**:
  - EF Core configuration: Map `Options` as PostgreSQL `text[]` or JSONB column.
  - EF Core Migration: `AddMultipleChoiceToInterviewQuestions`.
- **`TechDaily.Api`**:
  - Update `DailyFocusEndpoints.cs` to handle `{ selectedOptionIndex }` payload in `POST /api/v1/daily/drills/{id}/submit`.
- **`frontend`**:
  - `useDailyFocusStore.ts`: Update types and actions to submit selected option index.
  - `InterviewChallengePane.vue`: Redesign interface with interactive choice cards, option letter badges, submit trigger, and detailed trade-off breakdown view.
  - `locales/en.json` & `locales/vi.json`: Add translation strings for scenario options and feedback.

---

## 2. Invariants & Data Integrity
1. **Option Boundaries:** Every multiple-choice interview question must contain at least 2 options (standard: 4 options), and `CorrectOptionIndex` must satisfy $0 \le \text{CorrectOptionIndex} < \text{Options.Count}$.
2. **Cheat Prevention:** `CorrectOptionIndex` and `ExplanationMarkdown` MUST NOT be serialized in `GetTodayFocus` responses until the user has submitted their drill (`Status == Reviewed`).
3. **Deterministic Scoring:** Correct answers yield a score of 10 and keep the user's streak active. Incorrect answers yield a score of 0 and automatically trigger SM-2 flashcard creation for the topic.
4. **100% English Codebase:** All entity properties, DTOs, endpoint routes, database columns, and unit tests strictly in English.

---

## 3. API Contract Modifications

### `GET /api/v1/daily/today`
**Response (Pending Drill):**
```json
{
  "topic": { ... },
  "documentChunk": { ... },
  "question": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "questionText": "When designing an idempotency key filter in ASP.NET Core with Redis distributed cache, which approach avoids race conditions under concurrent burst requests?",
    "options": [
      "Use GET then SET with a 60-second TTL.",
      "Use Redis StringSet with When.NotExists (SETNX) and a 120-second TTL before executing the handler pipeline.",
      "Execute the handler first, then write the response to Redis with SET.",
      "Acquire a PostgreSQL table-level lock on the Orders table."
    ],
    "difficulty": "Hard"
  },
  "drill": {
    "id": "4ab95f64-5717-4562-b3fc-2c963f66afb7",
    "status": 0,
    "selectedOptionIndex": null,
    "isCorrect": null
  }
}
```

### `POST /api/v1/daily/drills/{id}/submit`
**Request Payload:**
```json
{
  "selectedOptionIndex": 1,
  "locale": "en"
}
```

**Response Payload:**
```json
{
  "isCorrect": true,
  "selectedOptionIndex": 1,
  "correctOptionIndex": 1,
  "score": 10,
  "explanationMarkdown": "### Architecture Breakdown\nUsing `SETNX` (`When.NotExists`) provides an atomic lock-and-set primitive in Redis, ensuring that only the first concurrent request proceeds while duplicates are rejected immediately with `409 Conflict` or served cached responses.\n\n- **Option A Pitfall:** A `GET` followed by `SET` is non-atomic and introduces a classic Time-of-Check to Time-of-Use (TOCTOU) race condition.\n- **Option C Pitfall:** Executing before acquiring the lock permits duplicate side-effects (e.g., double charging credit cards).\n- **Option D Pitfall:** Table-level locking creates catastrophic database contention and bottlenecking.",
  "currentStreak": 5,
  "longestStreak": 14,
  "totalDrillsCompleted": 12,
  "averageScore": 9.2
}
```
