# Proposal: AI-Powered Technical Interview Quiz & Mastery Arena

## 1. Why (Problem & Motivation)

Software engineers preparing for technical interviews often struggle to find focused, high-yield practice tailored to their specific technology stack and target seniority level. Existing interview practice is either too generic, lacks deep-dive architectural trade-off explanations, or fails to systematically track which concepts have been mastered versus which need reinforcement.

TechDaily currently offers daily curriculum reading and a single daily scenario drill on `/today`. While valuable for daily routine, engineers need:
1. **On-Demand Custom Topic Practice:** The freedom to test their knowledge on any technology stack (.NET 10, PostgreSQL MVCC, React Concurrency, Distributed Consensus, Docker/Kubernetes, Redis, Go, etc.) at their specific seniority tier (**Fresher**, **Junior**, **Middle**, **Senior**).
2. **AI-Generated Interactive Quizzes:** 5 to 10 multiple-choice questions per session with 4 distinct choices, 1 optimal answer, and deep-dive trade-off explanations powered by Gemini 3.6 Flash.
3. **Mastery Progression & Spaced Review:** Automatic database persistence where answered questions are marked as **"Mastered"** (never repeated in future batches), while failed/unanswered questions are collected into a dedicated **Mistake Review Queue** for iterative practice until mastered.
4. **Infinite Continuity ("Generate More"):** The ability to seamlessly generate fresh, non-repeating batches of 5–10 questions on the same topic and level with one click.

---

## 2. What (Scope & Deliverables)

### Capability 1: Technical Interview Quiz Engine & Persistence
- **Domain Modeling & Database Schema:**
  - `QuizQuestions`: Stores topic, level (Fresher, Junior, Middle, Senior), question text, 4 options (A, B, C, D), correct option index (0..3), in-depth explanation markdown, and tags.
  - `UserQuizProgress`: Tracks per-user mastery status (`IsMastered`), last chosen option, correctness, attempt counts, and timestamp.
- **Strict Authentication:**
  - All quiz endpoints (`/api/v1/quiz/*`) enforce `.RequireAuthorization()` and return `401 Unauthorized` for unauthenticated requests.
  - Route middleware guards `/quiz` on both SSR and Client, redirecting guests to `/login?redirect=/quiz`.

### Capability 2: Gemini AI Structured Question Generation
- **Intelligent Prompting & Fallback:**
  - Invokes Gemini 3.6 Flash with structured JSON output (`maxOutputTokens >= 8192` to protect against reasoning token truncation).
  - Input validation (topics 2–100 chars, category mapping, level mapping).
  - Anti-duplication mechanism: Passes existing question titles to Gemini prompt to prevent repetitive questions when generating follow-up batches.
  - Robust offline/mock fallback for network timeouts or missing API keys.

### Capability 3: Interactive Quiz Arena & Review Queue UI
- **Sidebar Integration:** New tab in `AppSidebar.vue` (`/quiz` — *"Trắc Nghiệm Phỏng Vấn"* / *"Interview Quiz"*).
- **Quiz Hub (`pages/quiz.vue`):**
  - **Tab 1: Tạo Đề Mới (AI Quiz Generator):** Quick topic selector chips + custom topic input, 4 seniority level cards (auto-selected from profile `TargetRole`), question count selector (5 or 10 questions).
  - **Tab 2: Ôn Tập Câu Sai (Mistake Review Queue):** Filterable list of unmastered/failed questions for targeted re-practice.
  - **Tab 3: Thống Kê Năng Lực (Mastery Analytics):** Visual summary of mastered questions, accuracy percentage, and topic mastery.
- **Interactive Quiz Player:**
  - Stepper header with progress bar.
  - 4 interactive option cards with A/B/C/D badges and IDE syntax-highlighted code snippets via `CommonShikiCodeBlock`.
  - Immediate feedback with trade-off analysis markdown rendered via `useMarkdownRenderer()`.
  - Confetti and score summary screen upon completion with actions: "Tạo thêm câu hỏi" (Generate more), "Làm lại câu sai" (Retry mistakes), "Chọn chủ đề mới" (New topic).
- **Bilingual Support:** Full English and Vietnamese localization (`en.json`, `vi.json`).

---

## 3. Impact & Non-Goals
- **Impact:** Transforms TechDaily into an active, on-demand interview preparation platform that continuously adapts to user weaknesses and drives engineer mastery.
- **Non-Goals:** Replacing the 30-day curriculum roadmap on `/today` (the quiz arena operates as an independent, complementary practice portal).
