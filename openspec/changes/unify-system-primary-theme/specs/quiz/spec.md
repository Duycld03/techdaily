# Spec Delta

## MODIFIED Requirements

### Requirement: Daily Drill Status Resiliency & State Deserialization
The daily interview challenge components (`InterviewChallengePane.vue` and `today.vue`) SHALL handle both string (`"Reviewed"`, `"reviewed"`, `"Submitted"`) and integer (`2`, `1`) representations of `DrillStatus`. Upon page refresh, when a previously completed drill is retrieved from `GET /api/v1/daily/today`, the interface SHALL preserve the reviewed state, restore the user's selected option, and display the score, architectural explanation, and header completed badge without reverting to an unsubmitted state.

#### Scenario: User refreshes page after completing daily interview challenge
- **WHEN** an authenticated user completes the daily scenario challenge and later refreshes `/today`
- **THEN** the API returns the persisted drill entity with `status = "Reviewed"`
- **AND** the scenario challenge pane recognizes the drill as reviewed (`isReviewed = true`)
- **AND** the header completed badge remains visible
- **AND** the submit button remains hidden, rendering the architectural explanation card and feedback banner.

#### Scenario: Preserving selected option index and feedback across sessions
- **WHEN** user reloads the today view for an already reviewed drill with `selectedOptionIndex = 2`
- **THEN** option 2 is visually selected and styled with appropriate correctness highlighting (primary brand violet for correct, rose for incorrect)
- **AND** the user cannot re-submit or alter the completed drill.

---

### Requirement: Interview Quiz Studio Visual Layout & Interactive Tokens
The `/quiz` route SHALL implement the **Dev-Learning Studio** visual language and 100% bilingual localization across all 5 internal tabs (`generate`, `arena`, `summary`, `review`, `stats`):

1. **Canvas & Card Consistency:**
   - The root `/quiz` container SHALL render over `dark:bg-canvas` (`#09090b` obsidian base) instead of legacy slate.
   - All tab content containers, topic generation cards, and Bento statistics cards SHALL utilize `.glass-card` styling with subtle hairline borders (`border-white/[0.06]`).

2. **Interactive Tab Switcher & Navigation:**
   - The top tab bar (`generate`, `arena`, `review`, `stats`) SHALL display a glass container (`bg-slate-100 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06] p-1 rounded-2xl`) with active tabs elevated via subtle neutral glass (`bg-white dark:bg-white/[0.08] text-brand-600 dark:text-white font-bold shadow-sm`).
   - The review queue navigation button SHALL bind to `quiz.tab_review_queue` ("Review Queue" / "Ôn Tập"), resolving missing key errors and never displaying raw untranslated key strings.

3. **Arena Question & Option Cards:**
   - Multiple-choice option cards (A, B, C, D) SHALL present neutral dark glass surfaces in unselected states (`border-white/[0.06] bg-white/[0.03] text-slate-200 hover:border-white/[0.15] hover:bg-white/[0.06]`).
   - Selected options SHALL highlight cleanly with Deep Iris Violet (`border-brand-500 bg-brand-500/10 text-white ring-1 ring-brand-500/30`).
   - Answered states SHALL use calibrated primary brand violet (`border-brand-500/80 bg-brand-500/10 text-brand-700 dark:text-brand-300`) for correct options and rose (`border-rose-500/80 bg-rose-500/10 text-rose-300`) for incorrect choices without heavy opaque backgrounds.
   - Mastered questions, correct choice banners, and review queue completed empty states SHALL display primary brand styling (`text-brand-600 dark:text-brand-400`, `bg-brand-500/10`, `border-brand-500/30`), completely replacing disparate emerald green accents.

4. **Explanation & Markdown Presentation:**
   - The post-answer explanation container SHALL use `.glass-card` with clean typography and Shiki code block integration.

5. **Bilingual Localization Parity:**
   - All status badges, attempt counters, and completion scores SHALL bind to localized dictionary keys:
     - Session score percentage: `{percentage}% {accuracy}` via `quiz.stats_accuracy`.
     - Mistake badge: `quiz.incorrect_badge` ("Incorrect" / "Chưa chính xác").
     - Mistake attempt counter: `quiz.incorrect_attempts` ("{count} incorrect attempts" / "{count} lần làm sai").
     - SM-2 push failure alert: `quiz.toast_push_sm2_failed` ("Failed to push to SM-2 Deck." / "Không thể đưa vào bộ ôn tập SM-2.").

#### Scenario: User navigates across quiz studio tabs
- **WHEN** user selects any tab in `/quiz` (`generate`, `arena`, `review`, `stats`)
- **THEN** the active tab highlights with a neutral glass elevation and white text
- **AND** the tab content renders on an obsidian base with hairline borders.

#### Scenario: User selects multiple-choice option in arena
- **WHEN** user clicks an option card (A, B, C, or D) before submitting
- **THEN** the option card highlights with Deep Iris Violet borders and subtle violet tint
- **AND** does not display harsh opaque colors or visual noise.

#### Scenario: User submits correct option in arena
- **WHEN** user submits the correct answer to a multiple-choice question in the arena
- **THEN** the correct option highlights with primary brand violet styling (`border-brand-500/80 bg-brand-500/10 text-brand-700 dark:text-brand-300`)
- **AND** the result feedback banner renders with primary brand accents instead of emerald green.

#### Scenario: Bilingual localization of review queue tab and mistake indicators
- **WHEN** user views `/quiz` in Vietnamese mode (`vi`)
- **THEN** the review queue tab displays "Ôn Tập" via `quiz.tab_review_queue` instead of the raw key string `quiz.tab_review_queue`
- **AND** incorrect answer badges in summary and review list render "Chưa chính xác" and "{count} lần làm sai" via `quiz.incorrect_badge` and `quiz.incorrect_attempts`
- **AND** the session summary score renders `{percentage}% Chính Xác` instead of hardcoded English.
