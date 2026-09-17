# Tasks: Quiz Bento Dashboard, Review Daily Completion Hub, and Notes Dynamic Tags

## 1. Quiz Bento Dashboard & Seniority Bug Fix (`frontend/pages/quiz.vue`)

- [x] 1.1 In `frontend/pages/quiz.vue`, implement the `formatSeniorityLevel(level: string | number)` helper function to map both string enum values (`"Fresher"`, `"Junior"`, `"Middle"`, `"Senior"`) and numeric values (`0`, `1`, `2`, `3`) to the corresponding seniority metadata object in `seniorityLevels`.
- [x] 1.2 In `frontend/pages/quiz.vue`, replace the existing 4-box flat number counter and linear seniority list in the Stats tab with an asymmetric 4-card Bento Grid Dashboard.
- [x] 1.3 Implement **Bento Card 1 (Hero Performance Card)**: Render overall accuracy rate, total answered count, unmastered review queue count (`quizStore.stats.reviewQueueCount`), readiness tier badge, and a primary CTA button to start mistake review (`activeTab = 'review'`).
- [x] 1.4 Implement **Bento Card 2 (Spaced Mastery Gauge Card)**: Render semi-circular radial SVG gauge showing Mastered / Total Answered questions and percentage, accompanied by proficiency tier badges.
- [x] 1.5 Implement **Bento Card 3 (Seniority Matrix Card)**: Render distinct Fresher, Junior, Mid-Level, and Senior bars using `formatSeniorityLevel(lvl.level)`, with individual color accents (Emerald, Sky, Amber, Violet), mastered/answered counts, and accuracy percentages.
- [x] 1.6 Implement **Bento Card 4 (Topic Strengths & Weaknesses Radar Card)**: Render `quizStore.stats.topicBreakdown` with topic names, counts, and color-coded accuracy badges (Emerald for $\ge 80\%$, Amber for $50\%\text{--}79\%$, Rose for $< 50\%$).

---

## 2. Review Daily Completion Hub (`frontend/pages/review.vue`)

- [x] 2.1 In `frontend/pages/review.vue`, replace the isolated floating dark card rendered when `activeTab === 'session' && (!currentCard || reviewStore.cards.length === 0)` with a balanced, rich Daily Review Completion Hub.
- [x] 2.2 In the completion hub, embed the **Celebratory Hero Banner** with celebratory icon, localized congratulatory heading (`review.no_cards`), encouraging retention copy (`review.no_cards_desc`), and automatic confetti burst upon completing the final review card.
- [x] 2.3 In the completion hub, embed the **MasteryGaugeCard** component passing `:mastered-count="reviewStore.deckStatistics.masteredCount"` and `:total-count="reviewStore.deckStatistics.totalCards"` to display overall retention rate and tier status directly within the session tab.
- [x] 2.4 In the completion hub, embed the **ReviewForecastChart** component passing `:cards="reviewStore.deckCards"` to visualize upcoming review load across the next 7 days without switching tabs.
- [x] 2.5 In the completion hub, implement the **Dual Action Controls**:
  - Primary CTA: "Browse Full Deck (N cards)" (`review.browse_deck_btn`) to switch to `activeTab = 'management'` with the total deck card count displayed.
  - Secondary CTA: "Cram / Extended Practice" routing to `/today` or practice drills.
- [x] 2.6 Ensure `fetchDeck(1)` is invoked on page mount so that `deckStatistics` and `deckCards` are pre-populated for the completion hub even if the user has not yet visited the Deck Management tab.

---

## 3. Notes Dynamic Tags Filter (`frontend/pages/notes.vue`)

- [x] 3.1 In `frontend/pages/notes.vue`, create a computed property `tagCounts` that extracts all unique tags from `notesStore.highlights`, removes `#` prefixes, computes occurrences, and sorts tags descending by count, then alphabetically.
- [x] 3.2 In `frontend/pages/notes.vue`, declare reactive state `selectedTag = ref<string | null>(null)` and helper `selectTag(tag: string | null)` to toggle tag selection.
- [x] 3.3 In `frontend/pages/notes.vue`, add a horizontal scrollable tag chip bar (`overflow-x-auto no-scrollbar py-1 flex items-center gap-2`) above the highlights list:
  - First chip: "Tất cả (N)" with total highlight count, active when `selectedTag === null`.
  - Dynamic chips: `#${item.tag} (${item.count})` for each extracted tag, active when `selectedTag === item.tag`.
- [x] 3.4 In `frontend/pages/notes.vue`, update `filteredHighlights` to filter conjunctively: highlights must match `selectedTag` (if active) AND match `highlightSearchQuery` (across excerpt, note, book title, chapter title, or tags).
- [x] 3.5 In `frontend/pages/notes.vue`, wire tag buttons on individual highlight cards to trigger `selectTag(tag)` to enable 1-click filtering directly from card footers.

---

## 4. Localization & Frontend Unit Tests

- [x] 4.1 In `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, add all required localization keys:
  - `quiz.bento_hero_title`, `quiz.bento_seniority_title`, `quiz.bento_topic_title`, `quiz.bento_mastery_title`
  - `quiz.readiness_ready`, `quiz.readiness_building`, `quiz.readiness_starting`, `quiz.btn_review_mistakes`
  - `review.cards_unit`, `review.cram_practice_btn`
  - `notes.tag_all`
- [x] 4.2 Add or update frontend unit tests in `frontend/tests/` to verify:
  - `formatSeniorityLevel` mapping behavior with string and numeric inputs.
  - Tag extraction and conjunctive filtering in `/notes`.
  - Review Completion Hub rendering when `cards.length === 0`.
- [x] 4.3 Run the frontend test suite (`npm run test`) and verify zero regression failures.

---

## 5. Deployment & Live Verification via MCP Tools

- [x] 5.1 Commit all changes and push to remote `main` branch: `git push origin main`.
- [x] 5.2 Monitor the GitHub Actions CI/CD workflow until completion (approximately 4–5 minutes for multi-arch Docker build, GHCR push, VPS container restart, health checks, and nginx reload).
- [x] 5.3 Conduct live end-to-end verification on production `https://techdaily.duckdns.org` using MCP browser tools:
  - **Quiz Stats Tab (`/quiz`)**:
    - Log in to production.
    - Navigate to `/quiz` and select the Stats tab.
    - Verify that the 4-card Bento Grid Dashboard is rendered.
    - Verify that the Seniority Matrix displays distinct levels ("Fresher / Entry", "Junior", "Mid-Level", "Senior / Staff") rather than repeating 'Senior' for all rows.
    - Verify Topic Strengths & Weaknesses card displays colored badges.
  - **Review Completion Hub (`/review`)**:
    - Navigate to `/review` when 0 cards are due (or complete a session).
    - Verify the celebratory banner, Mastery Gauge Card, and 7-day Review Forecast chart are displayed without switching tabs.
    - Click "Browse Full Deck (N cards)" and verify immediate switch to Deck Management tab.
  - **Notes Dynamic Tags (`/notes`)**:
    - Navigate to `/notes`.
    - Verify horizontal scrollable tag chip bar renders with "Tất cả (N)" active.
    - Click a dynamic tag chip and verify that the highlights list filters immediately to matching notes.
    - Test conjunctive search with a keyword in the search bar.
    - Click "Tất cả" and verify that all highlights are restored.
