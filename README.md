# TechDaily — Daily Senior Engineering & Interview Drill Platform

An AI-powered, daily bite-sized learning and technical interview preparation platform for Senior Fullstack & Backend (.NET) Engineers.

---

## 🎯 Project Overview

TechDaily is built to solve two critical developer challenges:
1. **Daily Micro-Learning from Real Docs:** Extracting curated, 3–5 minute reading slices from authoritative technical documentation (Microsoft Learn, PostgreSQL 17 Manual, Vue 3 / Nuxt 4 Docs, DDIA, CLR via C#) with structured takeaways, inline AI explanations, and IDE-grade Shiki syntax highlighting.
2. **Active Recall & Mock Interview Drills:** Providing daily Senior-level engineering challenges with instant AI evaluation (scoring, technical gap analysis, model answers) and voice response capability.

---

## 🏗️ Architecture & Technology Stack

| Layer | Technology | Key Responsibilities |
|---|---|---|
| **Backend API** | **ASP.NET Core (.NET 10)** | Clean Architecture, C# 13, Plain Use-Case Handlers (Pure DI), Rich Domain Model, RFC 7807 problem details, 100% English codebase |
| **Data Persistence** | **EF Core 10 + Npgsql** | PostgreSQL 17 (`pgvector`), JSONB (`ToJson()`) for takeaways/quizzes/reviews, Local Volume Audio Storage |
| **AI Evaluation Engine** | **Gemini 2.5 / 3.5 Flash API** | 1-Pass Multimodal voice/text evaluation, Structured Output (JSON Schema), Semantic Term Cache, AI Slicing |
| **Document Ingestion** | **PdfPig + ReverseMarkdown** | Zero-LOH streaming for PDFs up to 200MB (800 pages), Geometric Baseline line-grouping, HTML-to-Markdown Web Crawler with syntax preservation |
| **Frontend Web** | **Nuxt 4 + Vue 3** | Dual-Pane SSR/PWA app, Tailwind CSS + `@tailwindcss/typography`, Pinia, `@nuxtjs/i18n` (en/vi), `@nuxtjs/color-mode` (Dark/Light), Shiki TextMate Syntax Highlighter |
| **Notifications** | **Telegram Bot API** | Lightweight morning alerts and streak retention reminders with direct deep links to Web |

---

## 🌟 Core Features

### 1. 🏠 Daily Focus Hub (`/today`)
- **Daily Doc Slice:** Curated 3–5 minute excerpt from official docs preserving original source language with structured takeaways and quick check quiz.
- **Inline AI Explainer:** Highlight any complex technical term to get instant popover explanation localized to your language (backed by `TermExplanationCaches`).
- **Senior Interview Challenge:** Daily scenario question with dual-mode answer submission: **Markdown Editor** & **HTML5 Voice Recording Mode**.
- **AI Reviewer:** Instant scoring (1–10), strengths, missed internal mechanisms, and model answer from a Principal Engineer persona in 2-4s.

### 2. 📚 Technical Library & Document Ingestion (`/library`)
- **3-Tab Modern Ingestion Modal:**
  - **Markdown Tab:** Paste raw technical notes and auto-slice by markdown headings (`#`, `##`).
  - **PDF Drag & Drop Tab:** Upload large technical books, cheatsheets, and slides up to **200 MB (800 pages)** with zero-LOH streaming, automatic code detection, and baseline line-grouping.
  - **URL Article Crawler Tab:** Crawl any Microsoft Learn, Dev.to, Medium, or GitHub raw documentation link with live markdown preview and syntax tag preservation.
- **Document Management:** Delete documents with soft delete protection and cascading chunk cleanup.

### 3. 📖 Reader Experience (`/read/[bookId]`)
- **IDE-Grade Shiki Syntax Highlighting:** Multi-language syntax highlighting for C#, TypeScript, JavaScript, SQL, Python, Go, JSON, Bash, YAML, Dockerfile with top-right language badges.
- **1-Click Code Copying:** Copy any code snippet directly to clipboard with visual checkmark feedback.
- **Compact Bottom Navigation Bar:** Clean, balanced navigation footer with progress indicator, previous/next slice shortcuts, and document completion badges.

### 5. 🗺️ 30-Day Senior Fullstack Roadmap (`/roadmap`)
- **4 Core Technical Pillars:** Frontend & Browser Internals, .NET 10 & C# 13 Runtime Internals, PostgreSQL 17 Storage Engine, and Distributed Systems Architecture.
- **Skill Tree & Milestone Progression:** Daily unlockable modules with instant drill scores, completed days counter, and sprint progress tracking.
- **Strict Authentication Guard:** Requires verified JWT session with seamless auto-redirect to `/login` upon logout.

### 6. ✨ Infinite Senior Tech Insights Feed (`/insights`)
- **Bite-Sized Architectural Lessons:** Curated feed of Senior Anti-Patterns vs Idiomatic Solutions across C#, Rust, Go, Python, TypeScript, Vue 3, and PostgreSQL.
- **Deep Under-The-Hood Breakdown:** Detailed mechanics explaining memory layout, lock contention, compiler lowerings, and OS syscalls with benchmark statistics.
- **Multimodal AI Synthesis:** Generate on-demand, deep-dive insights on any custom technology or topic using Gemini 3.6 Flash.
- **IDE-Grade Shiki Highlighting & Navigation:** Dynamic multi-language theme (`one-dark-pro`), 1-click code copying, category filtering, and `[Space]`/`[→]` keyboard navigation.

### 7. 🌙 Dark/Light Mode & 🌐 Internationalization (i18n)
- **Responsive Dual Theming:** Full dark/light theme switching with `@nuxtjs/color-mode` featuring balanced header banners and syntax highlighting.
- **Multilingual UI:** Full i18n support for English (`en-US`) and Vietnamese (`vi-VN`) via `@nuxtjs/i18n`.

---

## 🚀 Quick Start

Run the fullstack development environment with a single command:
```bash
./run-dev.sh
```

- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:5000`
- Swagger / OpenAPI: `http://localhost:5000/openapi/v1.json`
