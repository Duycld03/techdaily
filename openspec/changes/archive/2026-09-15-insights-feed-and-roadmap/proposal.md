# Proposal: Infinite Tech Insights Feed & 30-Day Curriculum Roadmap

## 1. Why (Problem & Motivation)

TechDaily currently offers a structured 30-day curriculum via the Daily Focus hub (`/today`). While effective for disciplined daily practice, engineers encounter two key user experience limitations:
1. **Lack of Macro Visibility (Roadmap Missing):** Users only see the current day on `/today`. They cannot visualize their full 30-day journey, explore upcoming or past modules (.NET Internals, Postgres Storage, Frontend, System Design), track holistic milestone completion percentages, or revisit prior days without manual URL tampering.
2. **Desire for Casual, Infinite Micro-Learning (Insights Feed Missing):** When engineers have spare time outside their 5-minute daily drill, they want to browse high-value technical insights, bite-sized code tricks, performance optimizations, and architectural takeaways without being bound to the rigid daily schedule or needing to search for and upload large PDF books.

Adding a **30-Day Curriculum Roadmap** (`/roadmap`) and an **Infinite Tech Insights Feed** (`/insights`) provides both macro progression visibility and serendipitous, on-demand micro-learning.

---

## 2. What (Scope & Deliverables)

### Capability 1: 30-Day Curriculum Roadmap (`/roadmap`)
- **Visual Skill Tree & Timeline:** Interactive 30-day node map grouped into 4 core technical modules (Frontend & Browser, .NET 10 Internals, PostgreSQL 17 Engine, Distributed Systems).
- **Progress Tracking & Status:** Visual indicators for Completed (green checkmark), Active/Today (golden flame), and Locked/Upcoming (gray lock).
- **Historical Navigation:** Ability to click any unlocked day to review its authoritative reading material, deep-dive architectural analysis, and practice scenario questions.
- **Milestone Stats:** Overall completion rate (`X/30 Days`), estimated graduation date, and module mastery badges.

### Capability 2: Infinite Tech Insights Feed (`/insights`)
- **Dedicated Independent Data Engine (`TechInsights`):**
  - Standalone catalog of bite-sized, high-yield senior technical cards completely decoupled from `/library` books.
  - Curated seed catalog (~50+ cards across .NET, PostgreSQL, Frontend, and System Design).
  - On-Demand AI Synthesizer: Gemini 3.6 Flash endpoint generating fresh insight cards on-demand.
- **Card-Centric Reader Experience:**
  - Fast 45–60s read per card: Topic tag, catchy problem hook, under-the-hood breakdown, bad vs good code comparison, benchmark performance stats.
  - Seamless "Next ➔" and "Previous ⬅️" navigation with keyboard shortcuts (Space / ArrowRight) and swipe gestures.
  - 1-Click Actions: "Save to Notes", "Add to SM-2 Review Deck", "Ask Gemini Follow-up".
  - Topic filtering (All, .NET/C#, PostgreSQL, Frontend/Browser, System Design).

### Navigation & Sidebar Integration
- Add `Roadmap` (`/roadmap` - Map/Compass icon) and `Insights` (`/insights` - Sparkles/Zap icon) to `AppSidebar.vue`.
- Update i18n locales (`en.json`, `vi.json`) with bilingual translations.

---

## 3. Impact & Non-Goals
- **Impact:** Increases user engagement, provides macro learning journey orientation, and enables infinite casual discovery without burdening users with PDF uploads.
- **Non-Goals:** Merging Insights into user-uploaded Library documents (Insights remains 100% autonomous and separate).
