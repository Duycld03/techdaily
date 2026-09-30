# Spec Delta

## MODIFIED Requirements

### Requirement: Spaced Repetition Dual-Mode Navigation
The `/review` page SHALL provide a top-level standardized header banner and a three-tab navigation architecture matching the platform layout archetype:

1. **Standardized Page Header Banner**:
   - **Left**: Icon badge (`w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20`) rendering `<Layers class="w-5 h-5 sm:w-6 sm:h-6" :stroke-width="1.5" />`, primary title (`$t('review.title')` / "Thẻ Ôn Tập"), and subtitle (`$t('review.subtitle')` / "Hệ Thống Lặp Lại Ngắt Quãng & Quản Lý Kho Thẻ").
   - **Right**: Tab buttons switcher enclosed in a refined rounded glass pill container (`rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-100 dark:bg-canvas-subtle p-1`).
   - **Container Bound**: Enforces `max-w-7xl mx-auto space-y-6 px-4 sm:px-6 py-6`, eliminating the container width mismatch between header and content.

2. **Three-Tab Functional Hierarchy**:
   - **Tab 1: `session` ("Review Session" / "Ôn tập hôm nay")**:
     - Dedicated exclusively to active review practice.
     - While due cards exist, renders the active card player (`FlashcardDeck.vue`) in mutual exclusion with the celebratory completion state.
     - When zero cards are due, renders the centered celebratory completion hero card (`max-w-xl`, confetti celebration) with primary CTA transitioning to Deck Management (`activeTab = 'management'`) and secondary CTA routing to `/today`.
   - **Tab 2: `management` ("Deck Management" / "Kho thẻ của tôi")**:
     - Dedicated exclusively to browsing and managing the flashcard inventory.
     - SHALL render the search input (`⌘K`), quick filter chips (`All`, `Due Today`, `Mastered`), advanced filter trigger, and responsive flashcard bento grid directly at the top of the content viewport, without embedding the 6 analytics bento cards.
   - **Tab 3: `stats` ("Analytics" / "Thống kê")**:
     - Dedicated exclusively to comprehensive retention metrics and forecast analytics.
     - Hosts the two rows of 6 Bento cards in an uncrowded analytics hub.

#### Scenario: User navigates between review session and deck management
- **WHEN** user loads `/review`
- **THEN** page defaults to the "Review Session" tab if due cards exist, or allows switching to "Deck Management" or "Analytics"
- **WHEN** user clicks the "Deck Management" tab
- **THEN** client transitions to the deck management view and loads card statistics and the paginated card library directly without page reload.

#### Scenario: User completes review session
- **WHEN** user finishes grading all due cards in the "Review Session" tab
- **THEN** player displays a completion state with confetti celebration and provides a 1-click shortcut to inspect the full card deck in the "Deck Management" tab
- **AND** the completion card renders with expanded max-width bounds (`max-w-xl`), generous internal padding (`p-10 sm:p-12 md:p-14`), and relaxed vertical spacing (`space-y-6 sm:space-y-7`, `pt-4 sm:pt-6`), preventing action buttons and content from crowding container borders.

#### Scenario: User completes daily review session and views focused completion hero card
- **GIVEN** an authenticated user who finishes grading the last due card or has zero cards due today
- **WHEN** user views the Review Session tab (`activeTab === 'session'`)
- **THEN** client renders only the centered Celebratory Completion Hero Card with confetti
- **AND** the card container applies generous internal padding (`p-10 sm:p-12 md:p-14`), expanded max-width (`max-w-xl`), and relaxed vertical spacing (`space-y-6 sm:space-y-7`)
- **AND** displays the primary CTA `[ 📚 Khám phá kho thẻ (N thẻ) ]` and secondary CTA `[ ⚡ Luyện tập mở rộng ]` with ample horizontal breathing room and touch-friendly padding
- **AND** does NOT render the duplicate Mastery Gauge Card or Review Forecast Chart beneath the hero card.

#### Scenario: User transitions from completion hero card to deck management tab
- **GIVEN** user is viewing the Celebratory Completion Hero Card
- **WHEN** user clicks `[ 📚 Khám phá kho thẻ (N thẻ) ]`
- **THEN** client immediately switches `activeTab` to `'management'`
- **AND** renders the flashcard inventory and search filters directly at the top of the viewport.

#### Scenario: User completes daily review session and views rich completion hub
- **GIVEN** an authenticated user who has graded the final due card in `/review` or has zero cards due today (`reviewStore.cards.length === 0`)
- **WHEN** the user views the active review session tab
- **THEN** the client renders the Daily Review Completion Hub featuring the celebratory banner, embedded Mastery Gauge Card, and embedded 7-Day Review Forecast Chart
- **AND** triggers a celebratory confetti burst.

#### Scenario: User inspects 7-day review forecast on completion hub without switching tabs
- **GIVEN** the user has completed all cards due today and is viewing the Daily Review Completion Hub
- **WHEN** the user inspects the embedded Review Forecast Chart
- **THEN** the chart displays upcoming review card volumes for the next 7 days (Monday through Sunday)
- **AND** clearly reveals how many cards will become due tomorrow without requiring navigation to the Deck Management tab.

#### Scenario: User switches to deck management via completion hub CTA
- **GIVEN** the user is viewing the Daily Review Completion Hub with a total deck size of 42 cards
- **WHEN** the user clicks the "Browse Full Deck (42 cards)" action button
- **THEN** the client immediately transitions to the Deck Management tab (`activeTab = 'management'`)
- **AND** displays the full paginated card library and advanced search filters without reloading the page.

#### Scenario: User launches extended practice from completion hub
- **GIVEN** the user is viewing the Daily Review Completion Hub
- **WHEN** the user clicks the "Cram / Extended Practice" action button
- **THEN** the application routes to `/today` to practice daily scenario drills and reading challenges.

#### Scenario: Responsive bento layout of completion hub on mobile and desktop viewports
- **GIVEN** the user is viewing the Daily Review Completion Hub
- **WHEN** viewed on a desktop viewport ($\ge 1280\text{px}$)
- **THEN** the completion hub displays the celebratory banner followed by a balanced 2-column or 3-column layout embedding the Mastery Gauge and 7-day Review Forecast side-by-side
- **WHEN** viewed on a mobile viewport ($375\text{px}\text{--}390\text{px}$)
- **THEN** the completion hub smoothly stacks the banner, Mastery Gauge, Review Forecast, and action buttons into a single vertical column with zero horizontal scrolling.

#### Scenario: User reviews cards with mutual exclusion from completion state
- **GIVEN** an authenticated user has 1 or more flashcards due for review
- **WHEN** the user views the active review session tab (`activeTab === 'session'`)
- **THEN** only the active flashcard review component is rendered
- **AND** the completion hero card container is completely omitted from the layout.

#### Scenario: User completes the final due card in session
- **GIVEN** the user is grading the last remaining flashcard in the deck
- **WHEN** the final grade is submitted and the queue becomes empty (`reviewStore.cards.length === 0`)
- **THEN** the active flashcard component unmounts immediately
- **AND** the celebratory completion hero card renders with confetti celebration and deck exploration CTAs.

#### Scenario: User views standardized page header on review route
- **WHEN** user loads `/review` on any viewport
- **THEN** the top section renders the standardized header banner with `<Layers>` icon badge, primary title "Thẻ Ôn Tập", subtitle "Hệ Thống Lặp Lại Ngắt Quãng & Quản Lý Kho Thẻ", and the 3-tab navigation switcher on the right
- **AND** the header shares the same `max-w-7xl` container bound as the content below.

#### Scenario: User navigates to the dedicated analytics tab
- **WHEN** user clicks the "Analytics" ("Thống kê") tab
- **THEN** client transitions to `activeTab = 'stats'` and renders the two 3-card bento rows of retention analytics and forecast metrics.

---

### Requirement: Deck Management Retention Analytics Panel
The `/review` route SHALL surface retention analytics from `GET /api/v1/review/analytics` and bento overview cards in the dedicated **Retention Analytics & Stats Tab** (`activeTab === 'stats'`), completely decoupled from the Deck Management inventory list to avoid visual crowding.

The analytics panel SHALL present:
1. **Row 1: Bento Overview (3 Cards)**:
   - `FlashcardHeroCard`: Cards due today, estimated review time, and 1-click CTA button switching to `activeTab = 'session'`.
   - `MasteryGaugeCard`: Semi-circular gauge visualizing mastery percentage and tier badge.
   - `ReviewForecastChart`: 7-day upcoming review volume mini-bar chart.
2. **Row 2: Retention Analytics (3 Cards)**:
   - `AtRiskLeechCard`: Displays `overdueCount`, `leechCount` localized as **"Hay quên"** (`$t('review.atrisk_leech')`), and total at-risk count, with a 1-click action opening the Deck Management card list ordered by due urgency.
   - `SourceChannelRetentionCard`: Compares retention across recall sources (reader highlights, quiz mistakes, daily drills) with distinct per-source labels.
   - `EaseFactorDistributionCard`: Visualizes ease-factor distribution across three distinct stability tiers: **"Chưa vững"** (`$t('review.ease_struggling')` for $[1.30, 1.70]$), **"Đang củng cố"** (`$t('review.ease_developing')` for $(1.70, 2.10)$), and **"Vững vàng"** (`$t('review.ease_comfortable')` for $[2.10, 2.50]$).

The three analytics cards in each row SHALL lay out in a three-column row on large viewports (`lg` and above), eliminating wide empty gaps, and SHALL stack into a single column on mobile viewports without horizontal scrolling.

All labels, counts, and action controls SHALL be fully localized in English (`en`) and Vietnamese (`vi`), and action controls SHALL use `whitespace-nowrap shrink-0` to prevent text wrapping collisions across locales.

#### Scenario: User views retention analytics in Deck Management
- **GIVEN** an authenticated user with graded cards across multiple sources
- **WHEN** the user opens the Analytics tab (`activeTab === 'stats'`) on `/review`
- **THEN** the At-Risk & Leech card, Source-Channel Retention card, and Ease-Factor Distribution card render with data from `GET /api/v1/review/analytics`
- **AND** the existing Mastery Gauge and Review Forecast remain the sole instances of those components.

#### Scenario: User jumps from the at-risk card to the deck list
- **GIVEN** the At-Risk & Leech card reports a non-zero `atRiskCount`
- **WHEN** the user activates its review action
- **THEN** client transitions to the Deck Management tab (`activeTab = 'management'`) ordered by soonest due date so overdue at-risk cards surface first, without a full page reload.

#### Scenario: Source-channel rows render distinct labels
- **GIVEN** a deck with cards from more than one recall source (e.g. reader highlights and daily drills)
- **WHEN** the user views the Source-Channel Retention card
- **THEN** each source row shows a distinct localized label matching its `CardSourceType`
- **AND** no two rows display the same label.

#### Scenario: Ease-factor distribution card renders in a three-column analytics row
- **GIVEN** an authenticated user with graded cards
- **WHEN** the user opens the Analytics tab on a large viewport (`\ge 1024\text{px}`)
- **THEN** the analytics row shows the At-Risk & Leech, Source-Channel Retention, and Ease-Factor Distribution cards in a three-column layout with no oversized empty gaps
- **AND** the Ease-Factor Distribution card reflects the `strugglingCount`, `developingCount`, and `comfortableCount` from the analytics response.

#### Scenario: Retention analytics render in both locales
- **WHEN** the user toggles the application locale between `en` and `vi` on the Analytics tab
- **THEN** all retention card titles, metric labels, and action controls update reactively and render without truncation or layout overflow in either locale.

#### Scenario: Vietnamese memory retention labels disambiguate leech from low ease factor
- **WHEN** user views the retention analytics tab in Vietnamese locale (`vi-VN`)
- **THEN** the At-Risk card displays "Hay quên" for leech cards with low ease or repeated lapses
- **AND** the Ease Factor Distribution card displays "Chưa vững" for the $[1.30, 1.70]$ bucket
- **AND** neither card displays duplicate or colliding "Khó nhớ" labels.

---

### Requirement: Deck Management Tab Persistence Across Pagination
The `/review` page SHALL keep the user on the active tab across navigation and page remounts. The active tab state (`session`, `management`, `stats`) SHALL be encoded bidirectionally in the URL query string (`?tab=session`, `?tab=management`, `?tab=stats`) so that any navigation that recreates the page — including a full remount triggered by a `route.fullPath` change from a pagination query update — restores the active tab accurately.

#### Scenario: Paginating the deck keeps the Deck Management tab active
- **GIVEN** the user is on the Deck Management tab with more than one page of cards
- **WHEN** the user clicks "Next", "Previous", or a numbered page button
- **THEN** the requested page renders within the Deck Management tab
- **AND** the view does NOT switch to the Review Session or Analytics tab.

#### Scenario: Reloading a deck page URL restores the Deck Management tab
- **GIVEN** a URL that encodes the Deck Management tab and a page index (e.g. `?tab=management&page=3`)
- **WHEN** the user loads or reloads that URL
- **THEN** the page opens on the Deck Management tab showing the requested page.

#### Scenario: Reloading an analytics page URL restores the Analytics tab
- **GIVEN** a URL that encodes the Analytics tab (e.g. `/review?tab=stats`)
- **WHEN** the user loads or reloads that URL
- **THEN** the page opens directly on the Analytics tab with the 6 bento analytics cards rendered.
