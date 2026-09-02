# Proposal: Feature Cleanup, ERD Standardization & UI Navigation Grouping

## Executive Summary
As TechDaily expanded to include Tech Insights, 30-Day Senior Roadmap, Immersive Reader, and AI Quiz Arena, several legacy constructs, duplicate directories, and flat navigation menus required architectural unification and cleanup.

This change accomplishes:
1. **i18n & Locales Consolidation:** Removes duplicate `frontend/locales/` directory, standardizing exclusively on `frontend/i18n/locales/` (`en.json`, `vi.json`), and streamlines Vietnamese phrasing into concise, professional terminology.
2. **3-Tier Grouped Navigation:** Replaces flat 9-item menus on both desktop sidebar (`AppSidebar.vue`) and mobile drawer (`AppHeader.vue`) with 3 logical categories: *Luyện Tập (Practice)*, *Tri Thức & Ghi Nhớ (Knowledge)*, and *Hệ Thống (System)*.
3. **Cross-Feature 1-Click Integration:** Adds a 1-click **"Luyện Quiz Chương Này"** button to chapter readers (`/read/[bookId]`) that navigates to `/quiz?topic=...` with auto-prefilled topic generation.
4. **ERD Standardization & Dead Code Pruning:** Drops legacy `AiReviews` entity, voice audio columns (`UserAudioUrl`, `UserAnswerText`), and unused services (`LocalAudioStorageService`, `IAiReviewService`), scaffolding a clean EF Core migration `RemoveAiReviewsAndPruneDailyDrill`.

## Business & Technical Value
- **Higher Maintainability:** Eliminates duplicate locale files, dead database tables, and unused DI services.
- **Improved User Experience:** Grouped navigation makes feature discovery intuitive on both desktop and mobile; 1-click quiz integration connects deep reading directly to interactive recall testing.
- **Clean Schema:** Database ERD aligns 100% with domain entities without legacy audio interview artifacts.
