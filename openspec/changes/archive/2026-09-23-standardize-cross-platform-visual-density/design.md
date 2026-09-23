# Design

## Context

TechDaily's frontend is built with Nuxt 4 and Tailwind CSS. While `frontend/tailwind.config.js` lists `Inter` as the primary sans-serif font family, no webfont stylesheet or `@font-face` declaration is actually loaded in `nuxt.config.ts` or `main.css`. Consequently, browsers fall back to default operating system fonts (`Segoe UI` on Windows 11 versus `Ubuntu` on Linux). Because `Segoe UI` has wider average glyph width (+8.75%) and taller line bounding boxes, text wraps onto additional lines and occupies significantly more vertical space on Windows.

Simultaneously, a previous frontend enhancement scaled text on interactive components up to `md:text-lg` (18px) and card padding up to `p-8` / `md:p-10`. When viewed on standard 1080p displays (1920x1080) with browser toolbars and Windows taskbar (leaving ~856px usable viewport height), components such as the Scenario Challenge Dock (`InterviewChallengePane.vue`), Flashcard Deck (`FlashcardDeck.vue`), and Dashboard Bento (`HomeBentoDashboard.vue`) overflow the vertical fold or get clipped by rigid `overflow-hidden` constraints.

## Goals / Non-Goals

**Goals:**
- Eliminate cross-platform font metrics drift by loading the genuine `Inter` webfont with `font-display: swap` across all operating systems.
- Decouple reading prose typography from interface typography, ensuring interactive controls remain compact (`text-sm md:text-base`) while long technical articles retain comfortable reading sizes (`text-base md:text-lg`).
- Guarantee complete visibility of all four multiple-choice options (A, B, C, D) and submission controls in the Scenario Challenge Dock on standard 1080p desktop displays without scrolling.
- Eliminate vertical widget clipping on the Dashboard by transitioning from rigid `overflow-hidden` height locking to natural, smooth vertical scrolling.
- Compact Flashcard Deck dimensions (`min-h-[300px]`, `p-5 sm:p-6`) and Library cards (`p-4 sm:p-5`) to eliminate wasted whitespace.
- Maintain 100% pass rate across the Vitest frontend unit test suite.

**Non-Goals:**
- Changing the dark mode obsidian color palette (`canvas`, `brand`, `glass-card`).
- Decreasing text sizes for long technical articles or markdown prose in `DocReaderPane.vue` and `read/[bookId].vue`.
- Altering backend API contracts, database entities, or server logic.

## Decisions

### 1. Webfont Loading Strategy
- **Decision**: Add preconnect links and Google Fonts stylesheet for `Inter` (weights 400, 500, 600, 700, 800) into `app.head.link` in `frontend/nuxt.config.ts`:
  ```typescript
  link: [
    { rel: 'preconnect', href: 'https://fonts.googleapis.com' },
    { rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: '' },
    { rel: 'stylesheet', href: 'https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap' },
    { rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' }
  ]
  ```
- **Rationale**: Guarantees identical font metrics, x-height, and line wraps across Windows 11, Ubuntu Linux, and macOS.
- **Alternatives Considered**: Self-hosting font files in `public/fonts/`. Google Fonts with `preconnect` and `display: swap` provides instant edge caching and eliminates repository binary bloat while maintaining zero-CLS.

### 2. Two-Tier Typography Standard
- **Tier A (Reading Prose Scale)**:
  - Scope: Article bodies, book markdown chunks, explain term paragraphs.
  - Scale: `text-base md:text-lg` with `leading-relaxed`.
  - Applies to: `DocReaderPane.vue`, `read/[bookId].vue`.
- **Tier B (Interactive UI & Control Scale)**:
  - Scope: Scenario option cards, input fields, flashcard questions, dashboard metric badges, action buttons, table cells.
  - Scale: `text-sm md:text-base` (14px–16px) with titles at `md:text-xl font-bold`.
  - Applies to: `InterviewChallengePane.vue`, `FlashcardDeck.vue`, `HomeBentoDashboard.vue`, `library.vue`, `review.vue`, `insights.vue`, `profile.vue`.
- **Rationale**: Resolves the root cause of bloated controls without sacrificing readability on long articles.

### 3. Scenario Challenge Dock Layout Density (`InterviewChallengePane.vue`)
- **Title**: Change from `text-base sm:text-xl md:text-2xl font-bold` to `text-base sm:text-lg md:text-xl font-bold`.
- **Option Button**: Change padding from `p-3.5 sm:p-5 rounded-2xl` to `p-3 sm:p-3.5 rounded-xl`.
- **Option Text**: Change from `text-sm sm:text-base md:text-lg` to `text-sm sm:text-base`.
- **Letter Badge**: Change from `w-7 h-7 sm:w-8 sm:h-8` to `w-7 h-7` with `text-sm`.
- **Rationale**: Reduces the height of each option from ~140px to ~78px–86px. The 4 options now consume ~330px total instead of ~560px, ensuring the entire dock fits comfortably inside ~700px vertical height (well within the ~856px usable viewport).

### 4. Natural Dashboard Viewport Flow (`HomeBentoDashboard.vue`)
- **Container**: Replace `lg:h-[calc(100dvh-3.5rem)] lg:overflow-hidden` with `min-h-[calc(100dvh-3.5rem)] pb-8`.
- **Rationale**: On displays where the browser taskbar and bookmarks bar reduce available height to $\le 860\text{px}$, setting `overflow-hidden` cuts off Card E (Domain Constellation). Natural scrolling ensures all cards remain fully accessible.

### 5. Flashcard Deck Geometry Compaction (`FlashcardDeck.vue`, `review.vue`)
- **Flashcard Deck**:
  - Min Height: Reduce `min-h-[340px] sm:min-h-[400px]` to `min-h-[280px] sm:min-h-[320px]`.
  - Card Padding: Reduce `p-6 sm:p-8 rounded-3xl` to `p-5 sm:p-6 rounded-2xl`.
  - Question Title: Reduce `text-lg sm:text-2xl font-extrabold` to `text-base sm:text-xl font-bold`.
  - CTA Button Row: Reduce margin from `mt-8 pt-6` to `mt-5 pt-4`.
- **Review Page**:
  - Outer Padding: Reduce `md:p-10` to `p-4 sm:p-6 md:p-8`.
  - Tab Switcher Margin: Reduce `mb-6 sm:mb-8` to `mb-4 sm:mb-6`.
- **Rationale**: Prevents the action row ("Show Answer Space" / SM-2 grading buttons) from getting pushed against the bottom screen edge.

### 6. Library Card Optimization (`library.vue`)
- **Card**: Reduce padding from `p-6 sm:p-7 rounded-3xl` to `p-4 sm:p-5 rounded-2xl`.
- **Spacing**: Reduce internal vertical spacing from `space-y-4` to `space-y-3`.
- **Grid Gap**: Use `gap-4 sm:gap-5` instead of `gap-6`.
- **Rationale**: Gives cards a sleek, modern proportion and reduces dead vertical whitespace.

## Risks / Trade-offs

- **[Risk] Unit tests asserting on obsolete CSS class names** -> Mitigation: Update test fixtures in `InterviewChallengePane.spec.ts` if any assertions check specific typography classes.
- **[Risk] Visual regressions on mobile screens** -> Mitigation: Mobile breakpoints (`text-sm`, `p-3`, `p-4`) remain preserved; modifications specifically address the desktop breakpoint over-scaling (`md:`, `lg:`, `sm:p-5/8`).
