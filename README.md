# 🚀 TechDaily — Senior Engineering & Interview Drill Platform

> **Live Production:** [http://techdaily.duckdns.org](http://techdaily.duckdns.org)

An AI-powered, daily bite-sized learning and technical interview preparation platform designed for Senior Fullstack & Backend (.NET) Engineers.

---

## 🎯 Project Overview

TechDaily solves the two most critical challenges for senior engineers preparing for architectural roles:
1. **Daily Micro-Learning from Real Documentation:** Curated 3–5 minute reading slices extracted directly from authoritative sources (*Microsoft Learn, PostgreSQL 17 Internals, Vue 3 / Nuxt 4 Docs, Designing Data-Intensive Applications, CLR via C#*) with key takeaways, inline AI explanations, and IDE-grade Shiki syntax highlighting.
2. **Active Recall & Mock Interview Drills:** Daily Senior-level engineering scenario challenges with dual-mode answer submissions (Markdown or Spoken Voice) and instant Principal Engineer AI reviews (scoring, rubric gap analysis, model answers) powered by **Google Gemini 3.5 Flash**.

---

## 🏗️ Architecture & Technology Stack

```
TechDaily (Clean Architecture)
├── Api            → ASP.NET Core Minimal APIs (.NET 10, C# 13), JWT Bearer Auth, RFC 7807 Problem Details
├── Application    → Pure DI Use-Case Handlers, FluentValidation, Result Pattern, DTOs
├── Domain         → Rich Domain Entities, SM-2 Spaced Repetition Invariants, PBKDF2 Password Security
├── Infrastructure → PostgreSQL 17 (pgvector, EF Core 10), Gemini 3.5 Flash Client, PdfPig, ReverseMarkdown
└── Frontend       → Nuxt 4, Vue 3, Pinia, Tailwind CSS, @nuxtjs/i18n (en/vi), @nuxtjs/color-mode, Shiki
```

| Layer | Technology | Key Responsibilities |
|---|---|---|
| **Backend API** | **ASP.NET Core (.NET 10)** | Clean Architecture, C# 13, Plain Use-Case Handlers (Pure DI), Rich Domain Model, RFC 7807 problem details, 100% English codebase |
| **Data Persistence** | **EF Core 10 + Npgsql** | PostgreSQL 17 (`pgvector`), JSONB (`ToJson()`) for takeaways/quizzes/reviews, Local Volume Audio Storage |
| **AI Evaluation Engine** | **Gemini 3.5 Flash API** | 1-Pass Multimodal voice/text evaluation, Structured Output (JSON Schema), Semantic Term Cache, AI Slicing |
| **Document Ingestion** | **PdfPig + ReverseMarkdown** | Zero-LOH streaming for PDFs up to 200MB (800 pages), Geometric Baseline line-grouping, HTML-to-Markdown Web Crawler |
| **Frontend Web** | **Nuxt 4 + Vue 3** | Dual-Pane SSR/PWA app, Tailwind CSS + `@tailwindcss/typography`, Pinia, `@nuxtjs/i18n` (en/vi), `@nuxtjs/color-mode` (Dark/Light), Shiki TextMate Syntax Highlighter |
| **Notifications** | **Telegram Bot API** | Lightweight morning alerts and streak retention reminders with direct deep links to Web |

---

## 🌟 Comprehensive Feature Set

### 1. 🏠 Daily Focus Hub (`/today`)
- **Daily Doc Slice:** Curated 3–5 minute excerpt from official docs preserving original source language with structured takeaways and quick check quiz.
- **Inline AI Explainer:** Highlight any complex technical term to get instant popover explanation localized to your language (backed by `TermExplanationCaches`).
- **Senior Interview Challenge:** Daily scenario question with dual-mode answer submission: **Markdown Editor** & **HTML5 Voice Recording Mode**.
- **AI Reviewer:** Instant scoring (1–10), strengths, missed internal mechanisms, and model answer from a Principal Engineer persona in 2-4s.
- **Streak & Freeze Retention:** Automatic streak incrementing, longest streak tracking, and monthly streak freeze credits.

### 2. 🗺️ 30-Day Senior Fullstack Roadmap (`/roadmap`)
- **Core Pillars:** Frontend & Browser Internals, .NET 10 & C# 13 Runtime Internals, PostgreSQL 17 Storage Engine, and Distributed Systems Architecture.
- **Skill Tree & Milestone Progression:** Daily unlockable modules with instant drill scores, completed days counter, and sprint progress tracking.
- **Direct Navigation:** Jump directly into any unlocked day's focus topic and challenge.

### 3. 🧠 Spaced Repetition Flashcards (`/review`)
- **SuperMemo SM-2 Engine:** Strict mathematical intervals ($EF \in [1.30, 2.50]$, progression intervals $I_1=1, I_2=6, I_n = I_{n-1} \times EF$).
- **Interactive 3D Cards:** Smooth flip animation between prompt/question and model architecture answer.
- **4 Quality Grades:** *Again (0)*, *Hard (3)*, *Good (4)*, *Easy (5)* with real-time next review queue calculations.

### 4. 📚 Technical Library & Document Ingestion (`/library`)
- **3-Tab Modern Ingestion Modal:**
  - **Markdown Tab:** Paste raw technical notes and auto-slice by markdown headings (`#`, `##`).
  - **PDF Drag & Drop Tab:** Upload large technical books, cheatsheets, and slides up to **200 MB (800 pages)** with zero-LOH streaming, automatic code detection, and baseline line-grouping.
  - **URL Article Crawler Tab:** Crawl any Microsoft Learn, Dev.to, Medium, or GitHub raw documentation link with live markdown preview and syntax tag preservation.
- **Document Management:** Safe soft-deletion with cascading chunk cleanup.

### 5. 📖 Immersive Book Reader (`/read/[bookId]`)
- **Table of Contents Sidebar:** Real-time chapter navigation, active slice indicator, and reading completion status.
- **IDE-Grade Shiki Highlighting:** Multi-language syntax highlighting for C#, TypeScript, JavaScript, SQL, Python, Go, JSON, Bash, YAML, Dockerfile with 1-click clipboard copying.
- **Reading Progress Bar:** Live percentage counter and automatic local bookmark persistence (`localStorage`).
- **Floating Selection Toolbar:** 1-click AI Explanation with Gemini, text highlighting, and clipboard copying.

### 6. ✨ Infinite Senior Tech Insights Feed (`/insights`)
- **Bite-Sized Architectural Lessons:** Curated feed of Senior Anti-Patterns vs Idiomatic Solutions across C#, Rust, Go, Python, TypeScript, Vue 3, and PostgreSQL.
- **Under-The-Hood Mechanics:** Memory layouts, lock contention, compiler lowerings, and OS syscalls with benchmark statistics.
- **Multimodal AI Synthesis:** Generate on-demand, deep-dive insights on any custom technology or topic using Gemini 3.5 Flash.
- **Keyboard Navigation:** Fast card flipping using `[Space]`, `[→]`, and `[←]` keys.

### 7. 🖍️ Architectural Highlights & Notes (`/notes`)
- **Captured Snippets:** Centralized archive of all highlighted quotes categorized by book and chapter.
- **Tagging & Reflection:** Custom notes and multi-tag filtering.
- **Production Confirmation Modal:** Sleek, accessible delete confirmation dialog (Zero native browser popups).

### 8. 🛡️ Hybrid Authentication & User Profile (`/login`, `/profile`)
- **Standard Email/Password:** Secure PBKDF2 hashing with 16-byte random salt and 100,000 SHA-256 iterations.
- **Google OAuth 2.0:** One-Tap & standard Google authentication.
- **Hybrid Password Setup:** Seamlessly set an initial password for Google accounts to enable multi-device / mobile login without OAuth.
- **Password Strength Analyzer:** Real-time entropy & security feedback.
- **Global Toast Notification System:** Non-blocking, glassmorphic top-right toast alerts for all user actions.
- **Bilingual & Dual Theme:** 100% Vietnamese (`vi-VN`) & English (`en-US`) parity with Dark/Light mode support.

---

## ⚡ Quick Start (Local Development)

### Prerequisites
- [Docker & Docker Compose](https://www.docker.com/) (PostgreSQL 17 + `pgvector`)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+ & npm](https://nodejs.org/)

### Single-Command Start
Run the fullstack development environment with all services wired:
```bash
./run-dev.sh
```

### Access URLs
- **Frontend Application:** [http://localhost:3000](http://localhost:3000)
- **Backend API:** [http://localhost:5000](http://localhost:5000)
- **OpenAPI / Swagger Spec:** [http://localhost:5000/openapi/v1.json](http://localhost:5000/openapi/v1.json)

---

## 🧪 Testing & Verification

Run the entire automated test suite:

```bash
# Run Backend Unit & Integration Tests (35 Tests)
dotnet test backend/TechDaily.sln

# Run Frontend Component & Store Tests (36 Tests)
npm --prefix frontend test
```

---

## 📖 Architecture & Developer Documentation

| Document | Description |
|---|---|
| [**`AGENTS.md`**](file:///home/duycld03/workspace/techdaily/AGENTS.md) | Coding conventions, strict architectural rules, and invariants for AI agents and developers. |
| [**`docs/domain-rules.md`**](file:///home/duycld03/workspace/techdaily/docs/domain-rules.md) | Business logic, SM-2 algorithm, PBKDF2 parameters, typography rules, and UI constraints. |
| [**`docs/api-design.md`**](file:///home/duycld03/workspace/techdaily/docs/api-design.md) | Complete REST API endpoint contracts and RFC 7807 problem details specifications. |
| [**`docs/database-design.md`**](file:///home/duycld03/workspace/techdaily/docs/database-design.md) | Database schema, table definitions, entity relationships, and pgvector types. |
| [**`docs/curriculum-30-days.md`**](file:///home/duycld03/workspace/techdaily/docs/curriculum-30-days.md) | 30-day curriculum breakdown across 6 senior engineering domains. |

---

## 📜 License
MIT License. Built for passionate software engineers mastering distributed systems and architecture.
