# 🚀 DeepPace — Senior Engineering & Deliberate Practice Platform

[![Live Production](https://img.shields.io/badge/Live-techdaily.duckdns.org-10b981?style=flat-square&logo=nginx&logoColor=white)](https://techdaily.duckdns.org)
[![Backend](https://img.shields.io/badge/.NET%2010-ASP.NET%20Core-512bd4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Frontend](https://img.shields.io/badge/Nuxt%204-Vue%203.5-00DC82?style=flat-square&logo=nuxtdotjs&logoColor=white)](https://nuxt.com/)
[![Database](https://img.shields.io/badge/PostgreSQL%2017-pgvector-4169e1?style=flat-square&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind-CSS-38bdf8?style=flat-square&logo=tailwindcss&logoColor=white)](https://tailwindcss.com)
[![CI/CD](https://img.shields.io/github/actions/workflow/status/Duycld03/techdaily/ci.yml?branch=main&style=flat-square&logo=githubactions&logoColor=white&label=CI%2FCD)](https://github.com/Duycld03/techdaily/actions)
[![Spec-Driven Development](https://img.shields.io/badge/Workflow-OpenSpec-7c3aed?style=flat-square)](https://github.com/Fission-AI/OpenSpec)

> **Live Production (HTTPS):** [https://techdaily.duckdns.org](https://techdaily.duckdns.org)

An AI-powered, daily bite-sized learning and technical interview preparation platform designed for Senior Fullstack & Backend (.NET) Engineers.

---

## 🎯 Project Overview

DeepPace solves the two most critical challenges for senior engineers preparing for architectural roles:
1. **Daily Micro-Learning from Real Documentation:** Curated 3–5 minute reading slices extracted directly from authoritative sources (*Microsoft Learn, PostgreSQL 17 Internals, Vue 3 / Nuxt 4 Docs, Designing Data-Intensive Applications, CLR via C#*) with key takeaways, inline AI explanations, and IDE-grade Shiki syntax highlighting.
2. **Active Recall & Scenario Drills:** Daily Senior-level engineering architecture scenario challenges with multiple-choice trade-off decisions, instant verification, and Principal-level deep-dive explanations.
---

## 🏗️ Architecture & Technology Stack

```
DeepPace (Clean Architecture)
├── Api            → ASP.NET Core Minimal APIs (.NET 10, C# 13), JWT Bearer Auth, RFC 7807 Problem Details
├── Application    → Pure DI Use-Case Handlers, FluentValidation, Result Pattern, DTOs
├── Domain         → Rich Domain Entities, SM-2 Spaced Repetition Invariants, PBKDF2 Password Security
├── Infrastructure → PostgreSQL 17 (pgvector, EF Core 10), Gemini 3.5 Flash Lite Client, PdfPig, ReverseMarkdown, SMTP Email Sender
└── Frontend       → Nuxt 4, Vue 3.5, Pinia, Tailwind CSS, @nuxtjs/i18n (en/vi), @nuxtjs/color-mode, Shiki, Three.js, Web Worker Neural TTS
```

| Layer | Technology | Key Responsibilities |
|---|---|---|
| **Backend API** | **ASP.NET Core (.NET 10)** | Clean Architecture, C# 13, Plain Use-Case Handlers (Pure DI), Rich Domain Model, RFC 7807 problem details, 100% English codebase |
| **Data Persistence** | **EF Core 10 + Npgsql** | PostgreSQL 17 (`pgvector`), JSONB (`ToJson()`) for takeaways/quizzes/options, User Bookmarks with Unique Indexes, UserBookPacer |
| **AI Synthesis Engine** | **Gemini 3.5 Flash Lite API** | Structured Output (JSON Schema), High-Speed Quiz & Challenge Synthesis (<5s), Semantic Term Cache, JIT Look-Ahead Buffer Pre-generation |
| **Document Ingestion** | **PdfPig + ReverseMarkdown** | Asynchronous Channel-based queue with zero-LOH disk spooling for PDFs up to 300MB (8,000+ pages), native PDF Bookmarks/Outline segmentation, HTML-to-Markdown Web Crawler |
| **Frontend Web** | **Nuxt 4 + Vue 3.5** | Dual-Pane SSR/PWA app, Tailwind CSS + `@tailwindcss/typography`, Pinia, `@nuxtjs/i18n` (en/vi), `@nuxtjs/color-mode` (Dark/Light), Shiki TextMate Syntax Highlighter |
| **Audio Narration Engine** | **Web Worker Neural TTS + Web Speech** | Client-side neural TTS via ONNX Runtime Web / Transformers.js with Web Worker offloading (zero server cost), 3-tier cascading fallback (Device ONNX → System Web Speech → Cloud TTS), seekable sentence scrubber, and IndexedDB caching |
| **Knowledge Visualization** | **Three.js + 3D-Force-Graph + Cytoscape** | Interactive personal associative knowledge graph in 2D Canvas & 3D WebGL, visualizing relational connections between books, reading slices, highlights, and flashcards anchored to architectural pillars |
| **Notifications** | **Web Push (VAPID)** | Real-time browser push notifications (VAPID, Service Worker) for Morning Curriculum and Streak Preservation with auto-detected IANA timezones |
| **Identity & Security** | **JWT + Rotating Refresh Tokens + Email OTP** | PBKDF2 password hashing (16-byte salt, 100,000 SHA-256 iterations), OTP-verified email registration & password reset (SMTP transactional email), refresh-token family rotation with reuse detection, opt-in 30-day "Remember Me" persistence, Google OAuth 2.0 |
---

## 🌟 Comprehensive Feature Set

> 📖 **Feature Specifications & Capabilities:** See [openspec/specs/](openspec/specs/)
### 💡 Key Retention & Architecture Highlights
- **On-Device Neural Audio Narration (TTS):** Zero-latency client-side neural speech synthesis run in Web Workers via ONNX Runtime Web / Transformers.js with 100% offline playback, IndexedDB chunk audio caching, and cascading fallbacks (Device ONNX → System Web Speech → Cloud TTS).
- **Interactive 2D & 3D Knowledge Graph:** Personal associative knowledge cosmos rendered with 2D Canvas & 3D WebGL (Three.js), projecting real-time relational connections between user books, slices, highlights, and flashcards anchored to architectural pillars in a single PostgreSQL query pass.
- **Web Push Notifications (VAPID):** Real-time browser push notifications for Morning Curriculum (08:00) and Streak Preservation (20:00) with automatic IANA timezone detection and Brave browser guidance.
- **1-Click Flashcard (SM-2) from Reading Highlights:** Transform any highlighted technical passage in `/notes` or `/read/[bookId]` into a spaced repetition card with backend idempotency and deduplication.
- **Markdown Knowledge Export:** Export book notes and highlights with YAML frontmatter formatted for Obsidian and Logseq second-brain workflows.
- **Bilingual Resilient AI Explainer:** DOM context window extraction (±300 characters) for accurate explanations, responsive non-wrapping layout, and HTTP 500 translation resilience with retry button.
- **Decluttered Profile & Centralized Settings:** Clean separation between personal identity/career track (`/profile`) and system preferences/notifications (`/settings`).


### 1. 🏠 Daily Focus Hub (`/today`)
- **Daily Doc Slice & Pacer Bar:** Curated 3–5 minute excerpt from official docs preserving original source language with structured takeaways, reading progress metrics, calendar day change auto-advance, and 1-click active book switching dropdown.
- **Integrated Audio Narration Player:** Direct client-side speech playback of daily slices with seekable scrubber, sleep timer, auto-advance, and multi-engine fallback.
- **Bilingual Resilient AI Explainer:** Highlight any complex technical term or sentence to get instant popover explanation localized to your language, powered by DOM context window extraction (±300 characters), responsive non-wrapping layout, and HTTP 500 translation resilience with instant retry button (backed by `TermExplanationCaches`).
- **Senior Scenario Challenge:** Real-world architectural decision drill with instant option grading, trade-off analysis, and deep-dive explanations.
- **JIT Look-Ahead Buffer:** Background pre-generation service keeping 3 scenario challenges ahead using Gemini 3.5 Flash Lite with resilient 6-second timeout fallback.
- **Streak & Freeze Retention:** Automatic streak incrementing, longest streak tracking, effective streak decay calculation, and monthly streak freeze credits.
### 2. 🗺️ 30-Day Senior Fullstack Roadmap (`/roadmap`)
- **Core Pillars & Custom Track:** Frontend & Browser Internals, .NET 10 & C# 13 Runtime Internals, PostgreSQL 17 Storage Engine, Distributed Systems Architecture, or your active custom technical book.
- **Skill Tree & Milestone Progression:** Chapter-level milestone progression with live slice indicators, completed slice counters, and sprint progress tracking.
- **Direct Navigation:** Jump directly into any unlocked slice or milestone focus topic and challenge.

### 3. 🧠 Spaced Repetition Flashcards (`/review`)
- **SuperMemo SM-2 Engine:** Strict mathematical intervals ($EF \in [1.30, 2.50]$, progression intervals $I_1=1, I_2=6, I_n = I_{n-1} \times EF$).
- **Interactive 3D Cards:** Smooth flip animation between prompt/question and model architecture answer.
- **4 Quality Grades:** *Again (0)*, *Hard (3)*, *Good (4)*, *Easy (5)* with real-time next review queue calculations.
- **1-Click Flashcard from Highlights:** Convert highlighted technical passages from `/notes` or `/read/[bookId]` into spaced repetition cards with automatic question-answer synthesis, backend idempotency, and deduplication (`SourceType = 'Highlight'`).

### 4. 📚 Technical Library & Document Ingestion (`/library`)
- **3-Tab Modern Ingestion Modal:**
  - **Markdown Tab:** Paste raw technical notes and auto-slice by markdown headings (`#`, `##`).
  - **Async PDF Ingestion Tab:** Upload large technical books, cheatsheets, and slides up to **300 MB (8,000+ pages)** with zero-LOH disk spooling, asynchronous Channels queue, native PDF Bookmarks/Outline tree chapter extraction, and real-time polling progress.
  - **URL Article Crawler Tab:** Crawl any Microsoft Learn, Dev.to, Medium, or GitHub raw documentation link with live markdown preview and syntax tag preservation.
- **Document Management:** Safe soft-deletion with cascading chunk cleanup.

### 5. 📖 Immersive Book Reader (`/read/[bookId]`)
- **Table of Contents Sidebar:** Real-time chapter navigation, active slice indicator, and reading completion status.
- **On-Device & Multi-Engine Audio Narration:** Full two-row responsive audio player bar with seekable sentence scrubber slider, cascading engine fallback (Device ONNX Web Worker → System Web Speech API → Cloud TTS), sleep timer (5–60 min), auto-advance to next slice, and IndexedDB audio chunk caching.
- **IDE-Grade Shiki Highlighting:** Multi-language syntax highlighting for C#, TypeScript, JavaScript, SQL, Python, Go, JSON, Bash, YAML, Dockerfile with 1-click clipboard copying.
- **Reading Progress Bar:** Live percentage counter and automatic local bookmark persistence (`localStorage`).
- **Floating Selection Toolbar:** 1-click AI Explanation with Gemini, text highlighting, 1-click SM-2 flashcard creation, and clipboard copying.
- **Markdown Knowledge Export:** 1-click export of book chapters, executive summaries, takeaways, and user highlights with Obsidian/Logseq YAML frontmatter.

### 6. ✨ Infinite Senior Tech Insights Feed (`/insights`)
- **Bite-Sized Architectural Lessons:** Curated feed of Senior Anti-Patterns vs Idiomatic Solutions across C#, Rust, Go, Python, TypeScript, Vue 3, and PostgreSQL.
- **Under-The-Hood Mechanics:** Memory layouts, lock contention, compiler lowerings, and OS syscalls with benchmark statistics.
- **Database-Persisted Bookmarks:** Save important lessons with real-time bookmark toggle and "🔖 Đã Lưu" filter view.
- **Multimodal AI Synthesis:** Generate on-demand, deep-dive insights on any custom technology or topic using Gemini 3.5 Flash Lite.
- **Keyboard Navigation:** Fast card flipping using `[Space]`, `[→]`, and `[←]` keys.

### 7. 🖍️ Architectural Highlights & Saved Insights Hub (`/notes`)
- **2-Tab Knowledge Base:**
  - **🔖 Saved Insights:** Manage all bookmarked senior technical insights with summaries, fast jump links, and unbookmarking.
  - **🖍️ Reading Highlights:** Centralized archive of all highlighted quotes categorized by book and chapter with custom reflections and tagging.
- **Production Confirmation Modals:** Sleek, accessible delete and unbookmark confirmation dialogs (Zero native browser popups).
- **1-Click Flashcard Creation:** Generate spaced repetition cards directly from saved highlights with Gemini active recall prompt synthesis.
- **Markdown Knowledge Export:** Export complete book notes, chapter reflections, and tagged highlights formatted for second-brain tools.

### 8. 🕸️ Interactive Associative Knowledge Graph (`/graph`)
- **2D Canvas & 3D WebGL Visualization:** High-performance personal knowledge cosmos rendered with Three.js (`3d-force-graph`) and 2D Cytoscape visualizers with particle-pulse edge streams and smooth orbit controls.
- **Relational Knowledge Extraction:** Single-pass PostgreSQL 17 query projection (`GET /api/v1/graph`) linking user flashcards, highlights, and document slices to the 5 Core Architectural Pillars without auxiliary graph databases.
- **Interactive Telemetry & Navigation:** Category filtering, node search with auto-centering camera fly-to animation, and 1-click navigation bridges directly into Reader slices and Flashcards.

### 9. 🎯 Senior Technical Interview Quiz & Mastery Arena (`/quiz`)
- **High-Speed AI Quiz Synthesis:** Generate 5 or 10 real-world interview scenario questions tailored to seniority level (Fresher to Senior/Staff) in under 5 seconds with Gemini 3.5 Flash Lite.
- **Interactive Arena & Mistake Review:** Timed scenario questions with instant option grading, detailed markdown architectural explanations, and 1-click mistake retry sessions.
- **Spaced Repetition Mastery:** Automatically tracks user progress in PostgreSQL. Questions are marked as `Mastered` after 2 consecutive correct submissions.
- **Weak Topics Analysis:** Aggregated analytics dashboard tracking accuracy rate, mastered cards, and ranking weakest vs strongest engineering topics.

### 10. 🛡️ User Profile, Centralized Settings & Hybrid Auth (`/login`, `/profile`, `/settings`)
- **Decluttered Profile & Centralized Settings:** Clean separation between personal identity/career track (`/profile`) and system preferences/notifications (`/settings`).
- **OTP-Verified Email Registration:** No account is created until a 6-digit email verification code is confirmed (10-minute expiry, hashed & single-use, 5-attempt limit, 60-second resend cooldown).
- **Standard Email/Password:** Secure PBKDF2 hashing with 16-byte random salt and 100,000 SHA-256 iterations.
- **Passwordless Reset via Email OTP:** Forgot-password / reset-password flow that emails a one-time code and revokes every refresh-token family on success (signs out all devices).
- **Remember-Me Session Persistence:** Opt-in "Remember session (30 days)" — a 30-day persistent refresh cookie when checked, or a browser-session-only cookie that ends on browser close when unchecked.
- **Google OAuth 2.0:** One-Tap & standard Google authentication (always persistent).
- **Hybrid Password Setup:** Seamlessly set an initial password for Google accounts to enable multi-device / mobile login without OAuth.
- **Password Strength Analyzer:** Real-time entropy & security feedback.
- **Global Toast Notification System:** Non-blocking, glassmorphic top-right toast alerts for all user actions.
- **Bilingual & Dual Theme:** 100% Vietnamese (`vi-VN`) & English (`en-US`) parity with Dark/Light mode support.

### 11. 🔔 Web Push Notifications & Retention Hub (`/settings`)
- **Web Push Notifications (VAPID):** Real-time browser push notifications for Morning Curriculum (08:00) and Streak Preservation (20:00) with automatic IANA timezone detection and Brave browser guidance.
- **Multi-Device Sync:** Persistent endpoint subscriptions stored in `UserPushSubscriptions` with automatic device cleanup and test dispatch validation.
---

## ⚡ Quick Start (Local Development)

### Prerequisites
- [Docker & Docker Compose](https://www.docker.com/) (PostgreSQL 17 + `pgvector`)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22+ & npm](https://nodejs.org/)

### Configure Secrets
All secrets load from a single gitignored `.env` at the repo root using native .NET `Section__Key` names (double underscore → `Section:Key`). Copy the template and fill in your values before starting:
```bash
cp .env.example .env
# then set: POSTGRES_PASSWORD, Jwt__Secret, Gemini__ApiKey, Authentication__Google__ClientSecret,
#           WebPush__PublicKey / WebPush__PrivateKey, Email__Smtp__From / Username / Password
```

### Single-Command Start
Run the fullstack development environment with all services wired:
```bash
./run-dev.sh
```

### Access URLs
- **Frontend Application:** [http://localhost:3000](http://localhost:3000)
- **Backend API:** [http://localhost:5000](http://localhost:5000)
- **Scalar API Reference:** [http://localhost:5000/scalar/v1](http://localhost:5000/scalar/v1)
- **OpenAPI 3.1 Specification:** [http://localhost:5000/openapi/v1.json](http://localhost:5000/openapi/v1.json)

---

## 🧪 Testing & Verification

Run the entire automated test suite:

```bash
# Run Backend Unit & Integration Tests
dotnet test backend/DeepPace.sln

# Run Frontend Component & Store Tests (664 Tests)
npm --prefix frontend test

# Total: 1,015 Automated Tests (100% Passing)
```

---

## 📖 Architecture & Developer Documentation

| Document | Description |
|---|---|
| [**`openspec/specs/`**](openspec/specs/) | Single source of truth for modular capability specifications, requirements, and acceptance criteria. |
| [**`openspec/changes/`**](openspec/changes/) | Active lifecycle change proposals, delta specs, technical designs, and task checklists. |
| [**`DeepPace.Api/Endpoints/`**](backend/src/DeepPace.Api/Endpoints/) | Executable Minimal API endpoint contracts with interactive Scalar API Reference at `/scalar/v1`. |
| [**`DeepPace.Domain/Entities/`**](backend/src/DeepPace.Domain/Entities/) | Executable domain models, EF Core migrations, and pgvector schema definitions. |

---

## 🚢 VPS Production Deployment & Database Migration

### 1. Production `.env` Configuration
On the production host, configure `.env` based on `.env.example`:
```bash
# Database Credentials
POSTGRES_USER=deeppace_user
POSTGRES_DB=deeppace_db
POSTGRES_PASSWORD=<strong-random-password>

# JWT Security
Jwt__Secret=<64-char-csprng-secret>
Jwt__Issuer=DeepPace
Jwt__Audience=DeepPaceUsers
Jwt__ExpiryMinutes=60

# Production CORS & Allowed Origins
CORS_ALLOWED_ORIGINS=https://deeppace.duckdns.org,https://techdaily.duckdns.org

# OAuth & Public Configuration
GOOGLE_CLIENT_ID=<google-oauth-client-id>
NUXT_PUBLIC_GOOGLE_CLIENT_ID=<google-oauth-client-id>
Authentication__Google__ClientSecret=<google-oauth-client-secret>

# Third-Party Engines (AI, TTS, VAPID, SMTP)
Gemini__ApiKey=<gemini-api-key>
Google__TtsApiKey=<google-tts-key>
WebPush__PublicKey=<vapid-public>
WebPush__PrivateKey=<vapid-private>
WebPush__Subject=mailto:support@deeppace.app
```

### 2. Zero-Downtime Database Migration / In-Place Rename
If upgrading an existing production deployment with legacy `techdaily_db` data:
```bash
# Option A: Automated restoration pipeline (recommended)
./scripts/restore-prod-database.sh

# Option B: In-place database & user rename inside PostgreSQL
docker exec -it deeppace_postgres_prod psql -U postgres -c "ALTER DATABASE techdaily_db RENAME TO deeppace_db;"
docker exec -it deeppace_postgres_prod psql -U postgres -c "ALTER USER techdaily_user RENAME TO deeppace_user;"
```

### 3. Deploy Production Containers
```bash
# Validate compose configuration substitutions
docker compose -f docker-compose.prod.yml config

# Boot production services
docker compose -f docker-compose.prod.yml up -d
```

### 4. Google Cloud Console OAuth 2.0 Credentials
When activating `https://deeppace.duckdns.org`, register the domain origins in Google Cloud Console:
1. Navigate to **Google Cloud Console** > **APIs & Services** > **Credentials**.
2. Edit your OAuth 2.0 Web Client ID.
3. Under **Authorized JavaScript origins**, add:
   - `https://deeppace.duckdns.org`
   - `https://techdaily.duckdns.org` (retained for backward-compatible login)
4. Under **Authorized redirect URIs**, add:
   - `https://deeppace.duckdns.org`
   - `https://deeppace.duckdns.org/login`
---
## 📜 License
MIT License. Built for passionate software engineers mastering distributed systems and architecture.
