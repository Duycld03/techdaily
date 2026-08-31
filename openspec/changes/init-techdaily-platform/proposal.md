# Change Proposal: Initialize TechDaily Platform

## 1. Why (Problem & Motivation)
Software engineers aspiring to reach Senior and Principal levels often struggle with two problems:
1. **Information Overload:** Long, dense documentation and 800-page textbooks are hard to digest in busy daily schedules.
2. **Passive vs Active Learning:** Reading alone leads to rapid knowledge decay. Without active recall, mock interview practice, and spaced repetition, engineers struggle during technical interviews and architectural design reviews.

TechDaily provides a daily 5–10 minute micro-learning loop combining official documentation slices, Senior interview scenario challenges, and instant AI evaluation.

## 2. What (Scope & Deliverables)
Initialize the fullstack TechDaily platform with the following core modules:
- **Daily Focus Hub (`/today`):** Serving daily official doc excerpts with Vietnamese takeaways, inline AI terminology explanations, and interview scenario questions.
- **AI Mock Reviewer:** Structured AI grading (1–10 score, key strengths, missed mechanisms, Principal model answer).
- **Doc Library & AI Chunking (`/library`):** Slicing documentation sources (books, web URLs, markdown) into daily digestible chunks.
- **Spaced Repetition Engine (SM-2) & Notes (`/review`, `/notes`):** Resurfacing challenging concepts at optimal intervals and archiving highlights.
- **Habit & Streak System:** GitHub-style contribution heatmaps with monthly Streak Freeze protection.
- **Telegram Notifier:** Lightweight morning alert (08:00 AM) and streak reminder (20:00 PM) linking directly to the Web app.

## 3. Impact & Non-Goals
- **Impact:** Establishes a complete Clean Architecture backend (.NET 10) and responsive Nuxt 4 frontend with PostgreSQL persistence and Gemini AI integration.
- **Non-Goals:** Global social networks, complex paid subscription billing, or heavyweight server-side code execution engines.
