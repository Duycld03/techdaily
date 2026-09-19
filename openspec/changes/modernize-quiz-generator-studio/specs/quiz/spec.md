# Spec Delta: Quiz

## MODIFIED Requirements

### Requirement: Interview Quiz Studio Visual Layout & Interactive Tokens
The `/quiz` route SHALL implement the **Dev-Learning Studio** visual language and 100% bilingual localization across all internal tabs (`generate`, `arena`, `summary`, `review`, `stats`):

1. **Canvas & Card Consistency:**
   - The root `/quiz` container SHALL render over `dark:bg-canvas` (`#09090b` obsidian base) instead of legacy slate.
   - All tab content containers, topic generation cards, and Bento statistics cards SHALL utilize `.glass-card` styling with subtle hairline borders (`border-white/[0.06]`).

2. **Clean Studio Header Banner**:
   - The page header SHALL feature a clean, minimalist studio presentation with an icon tile (`p-2.5 rounded-2xl bg-brand-500/10 border border-brand-500/20 text-brand-600 dark:text-brand-400`), high-contrast title (`quiz.title`), and localized subtitle.
   - The header SHALL strictly omit decorative pulse halos, glowing badge rings, or distracting gradient background overlays.

3. **2-Column Bento Generation Studio**:
   - When the `generate` tab is active, the generator interface SHALL render as a structured 2-column Bento grid on desktop viewports (`lg:grid-cols-12 gap-6`):
     - **Topic & Context Hub (Column 1 - 7 cols)**: A `.glass-card` container housing the custom topic text input, quick topic chips, and the grounded-in-book toggle card with `AppSelect` book dropdown.
     - **Seniority & Action Hub (Column 2 - 5 cols)**: A `.glass-card` container organizing the seniority level cards, question count segmented pills, and primary generation trigger button.
   - On mobile and tablet viewports (<1024px), the layout SHALL gracefully stack into a single column with consistent vertical rhythm.

4. **Minimalist Typographic Seniority Level Cards**:
   - The 4 seniority tiers (`Fresher / Entry`, `Junior`, `Mid-Level`, `Senior / Staff`) SHALL render as clean typographic cards emphasizing textual clarity and visual breathing room.
   - The level cards SHALL strictly prohibit decorative emojis, icon badges, and graphical embellishments to maintain a calm, professional engineering aesthetic.
   - Selected level cards SHALL indicate active state via subtle brand borders (`border-brand-500`), translucent background fill (`bg-brand-500/10`), a soft focus ring (`ring-1 ring-brand-500/30`), and a discrete circular accent dot (`w-2 h-2 rounded-full bg-brand-500`).
   - Unselected cards SHALL render neutral translucent surfaces (`border-slate-200/80 dark:border-white/[0.06] bg-white/60 dark:bg-white/[0.02] hover:border-slate-300 dark:hover:border-white/[0.15]`).
   - Role descriptions within level cards SHALL enforce responsive typography standards (`text-xs sm:text-sm font-normal text-slate-500 dark:text-slate-400`), preventing illegible micro-text across viewport sizes.

5. **Integrated Segmented Pill Question Count**:
   - The question count selector (5 vs 10 questions) SHALL render as a compact, studio-grade segmented pill container (`bg-slate-100 dark:bg-canvas-subtle p-1 rounded-xl border border-slate-200/80 dark:border-white/[0.08]`).
   - Active count options SHALL elevate with a solid brand pill (`bg-brand-600 text-white shadow-sm font-bold`), while inactive options remain unobtrusive text pills (`text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white`).

6. **Interactive Tab Switcher & Navigation:**
   - The top tab bar (`generate`, `arena`, `review`, `stats`) SHALL display a glass container (`bg-slate-100 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06] p-1 rounded-2xl`) with active tabs elevated via subtle neutral glass (`bg-white dark:bg-white/[0.08] text-brand-600 dark:text-white font-bold shadow-sm`).
   - The review queue navigation button SHALL bind to `quiz.tab_review_queue` ("Review Queue" / "Ôn Tập"), resolving missing key errors and never displaying raw untranslated key strings.

7. **Arena Question & Option Cards:**
   - Multiple-choice option cards (A, B, C, D) SHALL present neutral dark glass surfaces in unselected states (`border-white/[0.06] bg-white/[0.03] text-slate-200 hover:border-white/[0.15] hover:bg-white/[0.06]`).
   - Selected options SHALL highlight cleanly with Deep Iris Violet (`border-brand-500 bg-brand-500/10 text-white ring-1 ring-brand-500/30`).
   - Answered states SHALL use calibrated primary brand violet (`border-brand-500/80 bg-brand-500/10 text-brand-700 dark:text-brand-300`) for correct options and rose (`border-rose-500/80 bg-rose-500/10 text-rose-300`) for incorrect choices without heavy opaque backgrounds.
   - Mastered questions, correct choice banners, and review queue completed empty states SHALL display primary brand styling (`text-brand-600 dark:text-brand-400`, `bg-brand-500/10`, `border-brand-500/30`), completely replacing disparate emerald green accents.

8. **Explanation & Markdown Presentation:**
   - The post-answer explanation container SHALL use `.glass-card` with clean typography and Shiki code block integration.

9. **Bilingual Localization Parity:**
   - All status badges, attempt counters, and completion scores SHALL bind to localized dictionary keys:
     - Session score percentage: `{percentage}% {accuracy}` via `quiz.stats_accuracy`.
     - Mistake badge: `quiz.incorrect_badge` ("Incorrect" / "Chưa chính xác").
     - Mistake attempt counter: `quiz.incorrect_attempts` ("{count} incorrect attempts" / "{count} lần làm sai").
     - SM-2 push failure alert: `quiz.toast_push_sm2_failed` ("Failed to push to SM-2 Deck." / "Không thể đưa vào bộ ôn tập SM-2.").

#### Scenario: User navigates across quiz studio tabs
- **WHEN** user selects any tab in `/quiz` (`generate`, `arena`, `review`, `stats`)
- **THEN** the active tab highlights with a neutral glass elevation and white text
- **AND** the tab content renders on an obsidian base with hairline borders.

#### Scenario: User configures and generates quiz in 2-column bento studio
- **WHEN** user navigates to `/quiz` with the `generate` tab active on a desktop screen (>=1024px)
- **THEN** the interface renders a 2-column Bento Grid separating topic/source configuration on the left from seniority/action controls on the right
- **AND** the page header displays a clean icon tile without glowing badge halos or animated pulse rings.

#### Scenario: User selects seniority tier on minimalist typographic cards
- **WHEN** user clicks on any of the 4 seniority level cards (e.g. Senior / Staff)
- **THEN** the selected card highlights with primary brand violet borders and a discrete accent dot
- **AND** the card displays clean typography without any decorative emoji icons or graphical spam.

#### Scenario: Responsive level card descriptions adhere to typography standards
- **WHEN** user views seniority level options on any device
- **THEN** the descriptive text explaining each level's scope renders at least `text-xs` on mobile and scales to `text-sm` on larger viewports
- **AND** does not truncate or wrap awkwardly across English and Vietnamese locales.

#### Scenario: Question count switches via segmented pill control
- **WHEN** user clicks between "5 Questions" and "10 Questions"
- **THEN** the selected count highlights with an elevated brand pill inside the segmented container
- **AND** updates the generator payload accordingly.

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
