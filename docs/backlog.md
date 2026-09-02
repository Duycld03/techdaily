# 📌 TechDaily — Product Backlog & Upcoming Roadmap

This document outlines prioritized user stories, planned capabilities, and engineering tasks for upcoming sprints.

---

## 🎯 Active Priorities (Next Sprints)

### Sprint 1: Interactive System Design Mock Interviews & Audio Flashcards
- [ ] **AI System Design Voice / Audio Explainer**: Generate natural audio pronunciation & voice summaries of daily doc slices and interview solutions.
- [ ] **Interactive System Design Canvas**: Drag-and-drop architectural diagramming with Gemini feedback on load balancers, caches, and DB partitions.
- [ ] **Custom Quiz Arena Topics**: Allow users to type any specific domain (e.g. *PostgreSQL WAL & MVCC*, *C# 13 Unsafe Memory Layout*) to generate targeted 10-question drills.

### Sprint 2: Collaborative Learning & Peer Leaderboards
- [ ] **Senior Engineer Leaderboard**: Global & team rankings based on continuous streak and quiz accuracy.
- [ ] **Deck Sharing & Community Packs**: Export/Import SM-2 spaced repetition decks via link or JSON.
- [ ] **Telegram Interactive Quiz Bot**: Directly answer quick-check multiple-choice questions within Telegram bot without opening the web browser.

### Sprint 3: Advanced Ingestion & AI Personalization
- [ ] **GitHub Repository Codebase Ingestion**: Connect public/private GitHub repositories to slice and explain real production open-source architectures (e.g. *dotnet/runtime*, *vuejs/core*).
- [ ] **EPUB & MOBI Format Support**: Native streaming parser for `.epub` technical ebooks alongside existing PDF support.
- [ ] **Adaptive Difficulty Engine**: Automatically adjust scenario challenge complexity based on user's recent accuracy rate and target role.

---

## 🛠️ Technical Debt & Engineering Backlog
- [ ] **Vector Search & RAG Acceleration (`pgvector`)**: Pre-compute embeddings for all ingested books and enable semantic natural language book searching.
- [ ] **PWA Offline Mode & ServiceWorker**: Enable offline reading and SM-2 flashcard review with background sync when reconnected.
- [ ] **Automated End-to-End Playwright CI Test Suite**: Integrate headless browser visual regression tests into GitHub Actions workflow.

---

## 📊 Feature Traceability & OpenSpec Changes
| Change Directory | Status | Summary |
|---|---|---|
| `openspec/changes/google-oauth-password-setup` | Merged / Done | Google OAuth 2.0 and Hybrid password setup |
| `openspec/changes/immersive-book-reader` | Merged / Done | Markdown/PDF reader, TOC, and Shiki highlighting |
| `openspec/changes/init-techdaily-platform` | Merged / Done | Core Clean Architecture, PostgreSQL, and Auth |
| `openspec/changes/insights-feed-and-roadmap` | Merged / Done | Senior Insights feed, bookmarks, and 30-day roadmap |
| `openspec/changes/interview-quiz-generator` | Merged / Done | High-speed AI interview scenario quiz generator |
| `openspec/changes/pdf-upload-and-web-crawler` | Merged / Done | PdfPig 200MB streaming and Web URL crawler |
