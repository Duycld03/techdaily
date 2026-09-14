# Design: AI-Powered Technical Interview Quiz & Mastery Arena

## 1. Architecture & Layering

Following TechDaily Clean Architecture principles:
```
TechDaily.Api            → QuizEndpoints.cs (/api/v1/quiz/*), JWT Bearer auth
TechDaily.Application    → GenerateQuiz, SubmitQuizAnswer, GetQuizReviewQueue, GetQuizStats Use Cases, Result pattern, FluentValidation
TechDaily.Domain         → QuizQuestion, UserQuizProgress, QuizLevel enum (No EF, No I/O)
TechDaily.Infrastructure → EF Core configurations, PostgreSQL schema migration, Gemini 3.6 Flash quiz generator
```

---

## 2. Database Schema Design (PostgreSQL 17 + EF Core)

### Entity: `QuizQuestion`
```csharp
public class QuizQuestion : BaseEntity
{
    public string Topic { get; set; } = string.Empty;
    public Category Category { get; set; }
    public QuizLevel Level { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new(); // Exactly 4 items
    public int CorrectOptionIndex { get; set; } = 0; // 0..3
    public string ExplanationMarkdown { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public Guid? CreatedByUserId { get; set; }
}
```

### Entity: `UserQuizProgress`
```csharp
public class UserQuizProgress : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public bool IsMastered { get; set; } = false;
    public int? LastSelectedOptionIndex { get; set; }
    public bool? IsLastAnswerCorrect { get; set; }
    public int CorrectCount { get; set; } = 0;
    public int IncorrectCount { get; set; } = 0;
    public DateTimeOffset? LastAttemptedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public QuizQuestion Question { get; set; } = null!;
}
```

### Enums:
```csharp
public enum QuizLevel
{
    Fresher = 0,
    Junior = 1,
    Middle = 2,
    Senior = 3
}
```

### Indexes & Constraints:
- `QuizQuestions`: Index on `(Topic, Level)` and `(Category, Level)`.
- `UserQuizProgress`: Unique Index on `(UserId, QuestionId)`. Index on `(UserId, IsMastered)`.

---

## 3. Gemini AI Generation Engine

### Interface: `IQuizGeneratorService`
```csharp
public interface IQuizGeneratorService
{
    Task<Result<List<QuizQuestion>>> GenerateQuestionsAsync(
        string topic,
        Category category,
        QuizLevel level,
        int count,
        List<string> existingTitlesToAvoid,
        string locale = "en",
        CancellationToken cancellationToken = default);
}
```

### Prompt Specification:
- **Model:** `gemini-3.6-flash`
- **Output Token Budget:** `maxOutputTokens: 8192` (protects against truncation).
- **Format:** Strict JSON array of objects containing `questionText`, `options` (4 items), `correctOptionIndex` (0..3), `explanationMarkdown`, and `tags`.
- **System Instruction:** Explicit instruction to generate authoritative technical scenario questions for the requested level, explaining runtime mechanics, memory trade-offs, and distractor flaws.

---

## 4. API Contracts (`TechDaily.Api`)

### Endpoints (Base Route: `/api/v1/quiz` — RequireAuthorization):
1. `POST /api/v1/quiz/generate`
   - Request: `{ topic: string, category?: int, level: int, count: int, locale?: string }`
   - Response: `{ questions: QuizQuestionDto[], topic: string, level: int, totalCount: int }`
2. `POST /api/v1/quiz/submit`
   - Request: `{ questionId: Guid, selectedOptionIndex: int }`
   - Response: `{ isCorrect: bool, correctOptionIndex: int, explanationMarkdown: string, isMastered: bool, attemptCount: int }`
3. `GET /api/v1/quiz/review-queue`
   - Query: `category?: int, level?: int, page?: int, pageSize?: int`
   - Response: `{ items: QuizReviewItemDto[], totalCount: int }`
4. `GET /api/v1/quiz/stats`
   - Response: `{ totalAnswered: int, masteredCount: int, reviewQueueCount: int, accuracyRate: decimal, levelBreakdown: LevelStatDto[], topicBreakdown: TopicStatDto[] }`

---

## 5. Frontend Architecture (`Nuxt 3 + Pinia + Tailwind CSS`)

### Store: `useInterviewQuizStore.ts`
- State: `activeQuestions`, `currentIndex`, `selectedAnswers`, `reviewQueue`, `stats`, `isLoading`, `isGenerating`, `isSubmitting`.
- Actions: `generateQuiz(topic, level, count)`, `submitAnswer(questionId, optionIndex)`, `fetchReviewQueue()`, `fetchStats()`, `nextQuestion()`, `resetSession()`.

### View: `pages/quiz.vue`
- Protected by `auth.global.ts` route middleware with SSR cookie integration.
- Responsive layout: Mobile $\ge 14\text{px}$, Desktop $\ge 16\text{px}$.
- 4 interactive option cards with A/B/C/D letter badges.
- Confetti celebratory animation on correct answers and session completion.
