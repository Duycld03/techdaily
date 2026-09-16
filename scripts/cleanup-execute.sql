-- TechDaily Database Maintenance: Atomic Purge Execution Script
-- Transactional & Idempotent: Executed within BEGIN ... COMMIT

BEGIN;

-- 1. Purge Poisoned TermExplanationCaches
WITH deleted AS (
    DELETE FROM "TermExplanationCaches"
    WHERE "ExplanationText" LIKE '%represents a core runtime or architectural mechanism%'
       OR "ExplanationText" LIKE '%Khái niệm kỹ thuật quan trọng%'
       OR "Embedding" IS NULL
    RETURNING 1
)
SELECT 'TermExplanationCaches' AS table_name, COUNT(*) AS deleted_rows FROM deleted;

-- 2. Purge Poisoned TechInsights (Cascades to UserInsightBookmarks)
WITH deleted AS (
    DELETE FROM "TechInsights"
    WHERE "Slug" ~ '-[0-9a-f]{6}$'
       OR "Title" LIKE 'Tối ưu hóa Memory Allocation trong ASP.NET Core với ArrayPool%'
       OR "Title" LIKE 'Architecting Non-Blocking Request Ingestion%'
    RETURNING 1
)
SELECT 'TechInsights' AS table_name, COUNT(*) AS deleted_rows FROM deleted;

-- 3. Purge Poisoned QuizQuestions (Cascades to UserQuizProgresses; sets null on SpacedRepetitionCards)
WITH deleted AS (
    DELETE FROM "QuizQuestions"
    WHERE "QuestionText" LIKE '[Senior] Question #%'
       OR "QuestionText" LIKE '[Senior] Câu hỏi #%'
       OR "ExplanationMarkdown" LIKE '%### Technical Deep Dive%'
    RETURNING 1
)
SELECT 'QuizQuestions' AS table_name, COUNT(*) AS deleted_rows FROM deleted;

-- 4. Purge Poisoned SpacedRepetitionCards (Preserves underlying UserHighlights)
WITH deleted AS (
    DELETE FROM "SpacedRepetitionCards"
    WHERE "SourceType" = 'Highlight'
      AND (
          "FrontMarkdown" LIKE 'What is the core architectural principle behind:%'
          OR "FrontMarkdown" LIKE 'Nguyên lý kiến trúc cốt lõi đằng sau trích dẫn trong%'
      )
    RETURNING 1
)
SELECT 'SpacedRepetitionCards' AS table_name, COUNT(*) AS deleted_rows FROM deleted;

COMMIT;
