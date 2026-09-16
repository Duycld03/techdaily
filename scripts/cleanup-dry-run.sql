-- TechDaily Database Maintenance: Dry-Run Analysis Script
-- Safe for execution in any environment: Transaction is wrapped in BEGIN ... ROLLBACK

BEGIN;

-- 1. Analyze TermExplanationCaches
SELECT 'TermExplanationCaches' AS table_name,
       COUNT(*) AS tainted_rows
FROM "TermExplanationCaches"
WHERE "ExplanationText" LIKE '%represents a core runtime or architectural mechanism%'
   OR "ExplanationText" LIKE '%Khái niệm kỹ thuật quan trọng%'
   OR "Embedding" IS NULL;

-- 2. Analyze TechInsights
SELECT 'TechInsights' AS table_name,
       COUNT(*) AS tainted_rows
FROM "TechInsights"
WHERE "Slug" ~ '-[0-9a-f]{6}$'
   OR "Title" LIKE 'Tối ưu hóa Memory Allocation trong ASP.NET Core với ArrayPool%'
   OR "Title" LIKE 'Architecting Non-Blocking Request Ingestion%';

-- 3. Analyze QuizQuestions
SELECT 'QuizQuestions' AS table_name,
       COUNT(*) AS tainted_rows
FROM "QuizQuestions"
WHERE "QuestionText" LIKE '[Senior] Question #%'
   OR "QuestionText" LIKE '[Senior] Câu hỏi #%'
   OR "ExplanationMarkdown" LIKE '%### Technical Deep Dive%';

-- 4. Analyze SpacedRepetitionCards
SELECT 'SpacedRepetitionCards' AS table_name,
       COUNT(*) AS tainted_rows
FROM "SpacedRepetitionCards"
WHERE "SourceType" = 'Highlight'
  AND (
      "FrontMarkdown" LIKE 'What is the core architectural principle behind:%'
      OR "FrontMarkdown" LIKE 'Nguyên lý kiến trúc cốt lõi đằng sau trích dẫn trong%'
  );

-- 5. Analyze DocumentChunks (Unvectorized)
SELECT 'DocumentChunks (Unvectorized)' AS table_name,
       COUNT(*) AS tainted_rows
FROM "DocumentChunks"
WHERE "Embedding" IS NULL;

-- Consolidated diagnostic summary table
SELECT 'TermExplanationCaches' AS table_name, COUNT(*) AS tainted_rows
FROM "TermExplanationCaches"
WHERE "ExplanationText" LIKE '%represents a core runtime or architectural mechanism%'
   OR "ExplanationText" LIKE '%Khái niệm kỹ thuật quan trọng%'
   OR "Embedding" IS NULL
UNION ALL
SELECT 'TechInsights' AS table_name, COUNT(*) AS tainted_rows
FROM "TechInsights"
WHERE "Slug" ~ '-[0-9a-f]{6}$'
   OR "Title" LIKE 'Tối ưu hóa Memory Allocation trong ASP.NET Core với ArrayPool%'
   OR "Title" LIKE 'Architecting Non-Blocking Request Ingestion%'
UNION ALL
SELECT 'QuizQuestions' AS table_name, COUNT(*) AS tainted_rows
FROM "QuizQuestions"
WHERE "QuestionText" LIKE '[Senior] Question #%'
   OR "QuestionText" LIKE '[Senior] Câu hỏi #%'
   OR "ExplanationMarkdown" LIKE '%### Technical Deep Dive%'
UNION ALL
SELECT 'SpacedRepetitionCards' AS table_name, COUNT(*) AS tainted_rows
FROM "SpacedRepetitionCards"
WHERE "SourceType" = 'Highlight'
  AND (
      "FrontMarkdown" LIKE 'What is the core architectural principle behind:%'
      OR "FrontMarkdown" LIKE 'Nguyên lý kiến trúc cốt lõi đằng sau trích dẫn trong%'
  )
UNION ALL
SELECT 'DocumentChunks (Unvectorized)' AS table_name, COUNT(*) AS tainted_rows
FROM "DocumentChunks"
WHERE "Embedding" IS NULL;

ROLLBACK;
