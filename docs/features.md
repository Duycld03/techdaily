# 📋 TechDaily — Feature Matrix & Specifications

This document serves as the single source of truth for all implemented, active, and planned features across the TechDaily platform.

---

## 🚦 Status Legend
- 🟢 **DONE**: Fully implemented, tested, and deployed to production.
- 🟡 **IN PROGRESS**: Under active design or implementation in `openspec/changes/`.
- 🔵 **PLANNED**: Specified and prioritized for upcoming sprints.
- ⚪ **BACKLOG**: Identified future enhancement / backlog candidate.

---

## 🏗️ 1. Core Platform, Navigation & Layout
| Feature | Description | Status | Reference / Spec |
|---|---|---|---|
| **3-Tier Grouped Navigation Shell** | Desktop sidebar & mobile drawer structured into *Practice*, *Knowledge*, *System* | 🟢 DONE | `frontend/components/layout/` |
| **Dual-Pane Responsive App Shell** | Collapsible desktop sidebar, mobile bottom navigation, and sticky topbar | 🟢 DONE | `frontend/layouts/default.vue` |
| **Bilingual Localization (i18n)** | 100% parity across English (`en-US`) and Vietnamese (`vi-VN`) | 🟢 DONE | `frontend/i18n/locales/` |
| **Dark / Light Mode Switching** | Seamless theme persistence with zero CSS flashing | 🟢 DONE | `@nuxtjs/color-mode` |
| **Global Toast Notification** | Glassmorphic floating feedback system without blocking dialogs | 🟢 DONE | `frontend/composables/useToast.ts` |
| **Production Modal System** | Custom Vue teleported modals with Tailwind CSS (Zero `alert`/`confirm`) | 🟢 DONE | `AGENTS.md` Rule 7 |

---

## 🏠 2. Daily Focus Hub (`/today`) & Roadmap (`/roadmap`)
| Feature | Description | Status | Reference / Spec |
|---|---|---|---|
| **Curated Daily Doc Slices** | 3–5 min authoritative excerpts with takeaways and quick-check quizzes | 🟢 DONE | `docs/curriculum-30-days.md` |
| **Floating AI Term Explainer** | Highlight terms up to 500 chars to get instant Gemini markdown explainer | 🟢 DONE | `TermExplanationCaches` |
| **Daily Senior Scenario Challenge**| Architecture trade-off drills with instant grading & Principal-level review | 🟢 DONE | `openspec/changes/archive/2026-09-01-multiple-choice-interview-drills/` |
| **Streak Engine & Freeze Retention**| Automated streak incrementing, longest streak, and monthly freeze credits | 🟢 DONE | `UserLearningStats` |
| **30-Day Senior Roadmap Tree** | Visual progression across .NET 10, PostgreSQL 17, Nuxt 4, Distributed Systems | 🟢 DONE | `frontend/pages/roadmap.vue` |

---

## 🧠 3. Spaced Repetition Flashcards (`/review`)
| Feature | Description | Status | Reference / Spec |
|---|---|---|---|
| **Mathematical SM-2 Algorithm** | $EF \in [1.30, 2.50]$, interval progression $I_1=1, I_2=6, I_n = I_{n-1} \times EF$ | 🟢 DONE | `Domain/Entities/SpacedRepetitionCard.cs` |
| **3D Interactive Card Flip** | Hardware-accelerated CSS 3D transform card flip interaction | 🟢 DONE | `frontend/pages/review.vue` |
| **4 Grading Scales** | *Again (0)*, *Hard (3)*, *Good (4)*, *Easy (5)* with queue re-scheduling | 🟢 DONE | `frontend/components/review/Sm2GradingButtons.vue` |
| **Auto-Card Generation from Slices**| Automatic SM-2 card extraction when completing daily learning slices | 🟢 DONE | `Application/UseCases/Review/` |

---

## 📚 4. Technical Library & Ingestion (`/library`)
| Feature | Description | Status | Reference / Spec |
|---|---|---|---|
| **Markdown Note Ingestion** | Raw markdown parsing and auto-slicing by headings (`#`, `##`) | 🟢 DONE | `Application/UseCases/Library/CreateBook` |
| **Large PDF Stream Ingestion** | Zero-LOH streaming for PDFs up to 200MB (800 pages) via PdfPig | 🟢 DONE | `openspec/changes/pdf-upload-and-web-crawler/` |
| **Web Article Crawler** | Crawl documentation URLs (Microsoft Learn, Dev.to, Medium) to Markdown | 🟢 DONE | `openspec/changes/pdf-upload-and-web-crawler/` |
| **Cascading Document Soft Delete** | Safe removal of books with automatic chunk cleanup | 🟢 DONE | `Application/UseCases/Library/DeleteBook` |

---

## 📖 5. Immersive Book Reader (`/read/[bookId]`)
| Feature | Description | Status | Reference / Spec |
|---|---|---|---|
| **Chapter Table of Contents** | Real-time chapter tree navigation and active reading slice indicator | 🟢 DONE | `openspec/changes/immersive-book-reader/` |
| **1-Click AI Quiz Practice** | 1-Click "Luyện Quiz Chương Này" jump prefilling `/quiz?topic=...` | 🟢 DONE | `frontend/pages/read/[bookId].vue` |
| **IDE-Grade Syntax Highlighting**| Shiki TextMate highlighting for C#, TS, JS, SQL, Python, Go, Dockerfile | 🟢 DONE | `frontend/plugins/shiki.client.ts` |
| **Reading Progress Persistence** | Live scroll percentage counter and local slice progress bookmarking | 🟢 DONE | `frontend/pages/read/[bookId].vue` |
| **Floating Action Toolbar** | Scoped text selection toolbar for AI Explainer, highlight, copy | 🟢 DONE | `AGENTS.md` Rule 3 & 10 |

---

## ✨ 6. Infinite Senior Tech Insights Feed (`/insights`)
| Feature | Description | Status | Reference / Spec |
|---|---|---|---|
| **Bite-Sized Architectural Cards** | Anti-Patterns vs Idiomatic Solutions across .NET, PostgreSQL, Vue 3, Go | 🟢 DONE | `openspec/changes/insights-feed-and-roadmap/` |
| **Database Bookmark Sync** | Real-time bookmarking and filter tab ("🔖 Đã Lưu") | 🟢 DONE | `Application/UseCases/Insights/BookmarkInsight` |
| **On-Demand AI Insights Synthesis**| Generate custom deep-dive technical insights via Gemini 3.1 Flash Lite | 🟢 DONE | `Application/UseCases/Insights/GenerateInsight` |
| **Keyboard Navigation** | Quick review controls via `[Space]`, `[→]`, `[←]` | 🟢 DONE | `frontend/pages/insights.vue` |

---

## 🖍️ 7. Architectural Notes & Highlights (`/notes`)
| Feature | Description | Status | Reference / Spec |
|---|---|---|---|
| **Saved Insights Management** | Centralized list of bookmarked cards with instant modal removal | 🟢 DONE | `frontend/pages/notes.vue` |
| **Reading Highlights Hub** | Categorized quote archive by book/chapter with reflections & tags | 🟢 DONE | `Application/UseCases/Notes/` |

---

## 🎯 8. Senior Interview Quiz Arena (`/quiz`)
| Feature | Description | Status | Reference / Spec |
|---|---|---|---|
| **High-Speed AI Quiz Synthesis** | Structured JSON generation of 5-10 questions in <5s (Gemini 3.1 Flash Lite)| 🟢 DONE | `openspec/changes/interview-quiz-generator/` |
| **Resilient Balanced JSON Parser** | Depth-balanced bracket parser (`ExtractJsonArray`) immune to trailing syntax hallucinations | 🟢 DONE | `Infrastructure/Services/GeminiAiService.cs` |
| **Clean Drill Session Isolation** | Pristine unselected state (`LastSelectedOptionIndex = null`) on every newly generated session | 🟢 DONE | `GenerateQuizHandler.cs` & `quiz.vue` |
| **Timed Arena & Mistake Review** | Real-time timer, instant grading, architectural rationale, retry mistakes | 🟢 DONE | `frontend/pages/quiz.vue` |
| **Spaced Mastery Engine** | Question mastery tracking (2 consecutive correct attempts = Mastered) | 🟢 DONE | `InterviewQuizQuestions` |
| **Topic Strengths & Weaknesses** | Aggregated accuracy analysis ranking weakest to strongest skills | 🟢 DONE | `frontend/stores/useInterviewQuizStore.ts` |

---

## 🛡️ 9. Authentication, Profile & Settings (`/login`, `/profile`, `/settings`)
| Feature | Description | Status | Reference / Spec |
|---|---|---|---|
| **PBKDF2 Password Security** | 16-byte random salt, 100k SHA-256 iterations | 🟢 DONE | `Infrastructure/Security/PasswordHasher.cs` |
| **Google OAuth 2.0 Integration** | One-Tap & standard OAuth flow | 🟢 DONE | `openspec/changes/google-oauth-password-setup/` |
| **Hybrid Password Setup** | Enable email/password login for Google OAuth accounts | 🟢 DONE | `Application/UseCases/User/ChangePassword` |
| **Decluttered Profile Page** | Minimal 3-card stats, clean personal details & security tabs | 🟢 DONE | `frontend/pages/profile.vue` |
| **Centralized System Settings** | Interface language, color theme, and Telegram notifications | 🟢 DONE | `frontend/pages/settings.vue` |

---

## 🤖 10. Notifications & Telegram Bot
| Feature | Description | Status | Reference / Spec |
|---|---|---|---|
| **Morning Curriculum Dispatch** | Daily 08:00 AM Telegram lesson link push | 🟢 DONE | `Infrastructure/Telegram/` |
| **Streak Preservation Reminder** | Daily 20:00 PM alert to prevent streak loss | 🟢 DONE | `Infrastructure/Telegram/` |
