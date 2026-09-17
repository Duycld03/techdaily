# Proposal: Quiz Bento Dashboard, Review Daily Completion Hub, and Notes Dynamic Tags

## Why

TechDaily's core retention and interview readiness loops rely on three primary engineering surfaces: Interview Quiz (`/quiz`), Spaced Repetition Review (`/review`), and Reading Notes (`/notes`). However, recent user feedback, design audits, and codebase inspection have surfaced critical usability defects and visual imbalances that undermine the user experience:

1. **Seniority Level Deserialization Bug & Plain Layout on `/quiz` Stats Tab:**
   - In `frontend/pages/quiz.vue` (lines 814–830), the seniority breakdown renders:
     ```html
     {{ seniorityLevels[lvl.level]?.label || 'Senior' }}
     ```
   - The backend serializes the `QuizLevel` enum as strings (`"Fresher"`, `"Junior"`, `"Middle"`, `"Senior"` via ASP.NET Core `JsonStringEnumConverter`). Because `seniorityLevels` is an indexed array (`[0, 1, 2, 3]`), array indexing with a string key returns `undefined`. Consequently, the expression falls back to `'Senior'` for every single row, incorrectly displaying four rows of "Senior"!
   - Furthermore, the Stats tab presents a dated 4-box flat number counter and a plain linear level list. Valuable backend analytics—specifically `TopicBreakdown` returned by `GetQuizStatsResponse`—are completely omitted from the UI. Users cannot visualize their strengths and weaknesses or launch targeted mistake reviews from the statistics view.

2. **Isolated Empty State on Daily Review (`/review`):**
   - When users finish grading their cards or visit `/review` when no cards are due (`reviewStore.cards.length === 0`), the page renders an isolated dark card in the center of an empty viewport.
   - Users are unable to see their overall retention achievements or view upcoming cards due tomorrow without manually toggling to the secondary "Deck Management" tab.
   - The empty state lacks momentum and celebratory feedback, missing an opportunity to present a rich **Daily Review Completion Hub** equipped with mastery metrics, 7-day review forecasts, and immediate cram/deck practice pathways.

3. **Missing Tag-Based Chip Navigation on `/notes`:**
   - The `/notes` page displays saved reading highlights with associated tags (e.g. `#dotnet`, `#architecture`, `#performance`), but offers only a freeform text search input.
   - Users cannot see at a glance what topics they have curated or filter their notes with a single tap.
   - A modern horizontal scrollable tag chip bar (`Tất cả (N)`, `#tag1 (count)`, `#tag2 (count)`) is needed to allow rapid, 1-click filtering that seamlessly composes with the existing keyword search.

4. **Deployment & Live Verification Requirement:**
   - The platform needs verified end-to-end delivery: pushing changes to remote `main`, monitoring the automated Docker build and GHCR deployment workflow on the production VPS, and performing real browser verification via MCP tools on `https://techdaily.duckdns.org` to ensure visual polish and correctness in the live environment.

---

## What Changes

We propose a comprehensive frontend modernization across `/quiz`, `/review`, and `/notes`:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│             QUIZ BENTO, REVIEW COMPLETION HUB & NOTES TAGS                  │
│                                                                             │
│  1. /quiz Stats Tab: Bento Grid Dashboard & Seniority Bug Fix               │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Fix seniority mapping with helper formatSeniorityLevel(lvl.level)   │  │
│  │ • Bento 1: Hero Performance Card (accuracy %, unmastered review CTA)  │  │
│  │ • Bento 2: Spaced Mastery Gauge Card (semi-circular radial SVG)       │  │
│  │ • Bento 3: Seniority Matrix Card (Fresher, Junior, Mid, Senior bars)  │  │
│  │ • Bento 4: Topic Strengths & Weaknesses Radar Card (TopicBreakdown)   │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  2. /review: Daily Review Completion Hub                                     │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Replace lonely card with balanced Daily Review Completion Hub       │  │
│  │ • Celebratory banner & confetti on daily review completion            │  │
│  │ • Embedded Mastery Gauge Card (retention rate & tier badge)           │  │
│  │ • Embedded 7-Day Review Forecast Chart (upcoming review volumes)      │  │
│  │ • Action CTAs: "Browse Full Deck (N cards)" & "Cram / Extended"       │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  3. /notes: Dynamic Tags Filter Chip Bar                                    │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Horizontal scrollable tag chip bar above highlights list            │  │
│  │ • Default chip: "Tất cả (N)" showing total highlight count            │  │
│  │ • Dynamic tag chips extracted from user highlights with counts        │  │
│  │ • Interactive selection & conjunctive filtering with search input     │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  4. Production Deployment & Live MCP Verification                            │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Push to remote main -> GitHub Actions CI/CD (Docker / GHCR / VPS)    │  │
│  │ • Live verification on https://techdaily.duckdns.org via MCP tools    │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1. Quiz Bento Dashboard & Seniority Bug Fix (`/quiz`)
- **Seniority Bug Resolution**:
  - Introduce `formatSeniorityLevel(level: string | number)` helper in `frontend/pages/quiz.vue`.
  - Maps both string enum representations (`"Fresher"`, `"Junior"`, `"Middle"`, `"Senior"`) and integer indices (`0`, `1`, `2`, `3`) safely to the corresponding metadata object `{ id, key, label, desc }`.
  - Eliminates the silent fallback bug where all levels rendered as `'Senior'`.
- **4-Card Bento Grid Dashboard Layout**:
  - **Bento 1: Hero Performance Card**:
    - Highlights overall accuracy rate percentage (`quizStore.stats.accuracyRate`%), total questions answered, and review queue count.
    - Displays motivational readiness assessment badge (e.g. "Sẵn sàng phỏng vấn" / "Cần luyện tập thêm").
    - Features a 1-click CTA button (`activeTab = 'review'`) to immediately practice mistakes from the review queue.
  - **Bento 2: Spaced Mastery Gauge Card**:
    - Semi-circular radial SVG gauge displaying Mastery Rate = $(Mastered / TotalAnswered) \times 100\%$.
    - Dynamic proficiency tier badges (e.g. "Trí nhớ xuất sắc" / "Đang rèn luyện") matching the spaced repetition design language.
  - **Bento 3: Seniority Matrix Card**:
    - Color-coded progress bars for Fresher, Junior, Mid-Level, and Senior tiers.
    - Displays exact mastered count, answered count, and accuracy percentages per level.
  - **Bento 4: Topic Strengths & Weaknesses Card**:
    - Renders `TopicBreakdown` from `GetQuizStatsResponse`.
    - Features accuracy badges: Emerald for strengths ($\ge 80\%$), Amber for developing topics ($50\%\text{--}79\%$), and Rose for areas requiring reinforcement ($< 50\%$).

### 2. Review Daily Completion Hub (`/review`)
- **Rich Completion Hub**:
  - Replaces the small floating card in the vast empty container when `reviewStore.cards.length === 0`.
  - Centers a structured, balanced 2-column or 3-part layout:
    1. **Celebratory Hero Banner**: Congratulates the user on completing today's reviews, with celebratory icon, heading, and motivating copy.
    2. **Embedded Mastery Gauge Card**: Reuses `MasteryGaugeCard.vue` showing total flashcard deck mastery rate and tier badge directly on the session screen.
    3. **Embedded 7-Day Review Forecast**: Reuses `ReviewForecastChart.vue` showing upcoming card review counts for the week, allowing users to anticipate tomorrow's review load without switching tabs.
  - **Dual Action CTAs**:
    - Primary CTA: "Browse Full Deck (N cards)" (`$t('review.browse_deck_btn')`), switching to the Deck Management tab with total deck count reflected.
    - Secondary CTA: "Cram / Extended Practice" (`$t('review.continue_drill')` or cramming mode), routing to `/today` or practice drills.

### 3. Notes Dynamic Tags Filter (`/notes`)
- **Horizontal Scrollable Tag Chip Bar**:
  - Rendered above the highlights list in `frontend/pages/notes.vue`.
  - Supports smooth horizontal scrolling on both desktop and mobile viewports (`overflow-x-auto no-scrollbar py-1 flex items-center gap-2`).
- **Dynamic Chip Generation & Selection**:
  - Default chip: `Tất cả (N)` representing all highlights.
  - Dynamically extracts all unique tags from `notesStore.highlights` (normalizing `#` prefixes) and calculates their counts (e.g. `#dotnet (4)`, `#architecture (3)`, `#performance (2)`).
  - Chips are sorted by frequency descending, then alphabetically.
  - Active chip has distinctive brand styling; clicking an active chip or "Tất cả" resets the tag filter.
- **Conjunctive Search Integration**:
  - `filteredHighlights` filters highlights where both the active tag matches and the text search query in `highlightSearchQuery` matches.

### 4. Deployment & Live Verification
- Commit and push to remote repository `main` branch.
- Wait for automated GitHub Actions workflow to build multi-arch Docker images, push to GHCR, deploy to VPS, restart containers, and reload nginx.
- Perform live verification using MCP tools on `https://techdaily.duckdns.org` covering `/quiz`, `/review`, and `/notes`.

---

## Capabilities

### Modified Capabilities
- `quiz`: Modernizes the `/quiz` statistics view into a 4-card Bento Dashboard (Hero Performance Card with review CTA, Spaced Mastery Gauge, Seniority Matrix Card, Topic Strengths & Weaknesses Radar) and resolves the seniority level enum string-indexing bug via `formatSeniorityLevel`.
- `review`: Replaces the empty session state on `/review` with a rich Daily Review Completion Hub embedding celebratory feedback, the Mastery Gauge Card, and the 7-day Review Forecast Chart with 1-click navigation to deck management and practice drills.
- `notes`: Enhances the `/notes` reading notes hub with a dynamic, horizontal scrollable tag chip bar (`Tất cả (N)`, dynamic tags with counts) that integrates conjunctively with keyword search.

---

## Impact

- **Frontend Components & Pages**:
  - `frontend/pages/quiz.vue`: Refactored stats tab to Bento Grid layout with 4 cards and `formatSeniorityLevel` helper.
  - `frontend/pages/review.vue`: Refactored empty state into rich Daily Review Completion Hub embedding `MasteryGaugeCard.vue` and `ReviewForecastChart.vue`.
  - `frontend/pages/notes.vue`: Added horizontal scrollable tag chip bar and updated `filteredHighlights` computed logic.
- **Pinia Stores & State**:
  - Utilizes existing store contracts in `useInterviewQuizStore`, `useReviewStore`, and `useNotesStore` without breaking changes.
- **Internationalization (i18n)**:
  - Adds localized strings in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` for bento titles, completion hub actions, and tag chips.
- **Backend & Database**:
  - Zero database schema migrations required; zero breaking changes to backend API endpoints.

---

## Verification / Testing & Quality Assurance

- **Local Verification**:
  - Run frontend test suite (`npm run test`) to ensure no regressions in existing component or page tests.
  - Verify that `formatSeniorityLevel` handles `"Fresher"`, `"Junior"`, `"Middle"`, `"Senior"`, `"0"`, `"1"`, `"2"`, `"3"`, and integer inputs correctly.
  - Verify tag extraction and filtering in `notes.vue` with edge cases (empty tags, special characters, mixed casing).
- **Production Deployment & Live Verification via MCP**:
  - Deploy to production VPS via CI/CD push to `main`.
  - Live inspection on `https://techdaily.duckdns.org`:
    - Navigate to `/quiz`, switch to Stats tab, verify distinct levels (Fresher, Junior, Mid-Level, Senior) and 4 Bento cards.
    - Navigate to `/review` with 0 cards due, verify celebratory Completion Hub with Mastery Gauge and 7-day Forecast.
    - Navigate to `/notes`, verify tag chip bar with "Tất cả" default and test 1-click filtering.
