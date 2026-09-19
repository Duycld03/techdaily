# Design: Modernize Quiz Generator Studio Architecture

## Context

See `proposal.md` for motivation.

The Quiz Generation interface (`frontend/pages/quiz.vue`, Tab 1) currently stacks all generation controls in a single vertical `.glass-card`. While functional, this layout causes excessive scrolling on desktop, underutilizes horizontal screen space, uses micro-text below standard font guidelines, and lacks the sophisticated Bento organization seen in `/profile` and `/notes`.

User directives strictly require:
1. Enhancing the interface with Dev-Learning Studio tokens and ergonomics.
2. **Strictly omitting glowing badge halos, pulse animations, and gradient clutter.**
3. **Strictly avoiding emoji icons or icon spam on seniority level cards**, prioritizing crisp typography and calm minimalism.

## Goals / Non-Goals

**Goals:**
- Implement a responsive 2-Column Bento Studio grid (`lg:grid-cols-12`) for Tab 1:
  - Left Bento (7 columns): Topic text entry, quick topic chips, and grounded-in-book toggle with `AppSelect`.
  - Right Bento (5 columns): 4-tier seniority level cards, segmented question count pills, and the generation CTA.
- Design sleek typographic seniority level cards with clear hierarchy, readable descriptions (`text-xs sm:text-sm`), and a discrete accent indicator.
- Unify question count selection into an integrated segmented pill bar.
- Refactor the page header into a clean studio header without decorative halos.
- Preserve 100% of existing Pinia store bindings, reactive refs, and test identifiers (`data-testid`).

**Non-Goals:**
- Modifying Tab 2 (Arena question runner), Tab 3 (Review queue), or Tab 4 (Stats).
- Altering backend API contracts (`POST /api/v1/quiz/generate`) or DTO structures.
- Introducing decorative emoji prefixes, floating particle halos, or complex canvas physics.

## Decisions

### 1. 2-Column Bento Grid Layout (Desktop 7/5 Split)

**Decision**: On desktop screens (`lg:grid-cols-12 gap-6`), split generation controls into two balanced `.glass-card` containers:
- **Left Container (7 cols)**: Handles *Source & Scope* (Topic input, quick chips, grounded document toggle + `AppSelect`).
- **Right Container (5 cols)**: Handles *Configuration & Execution* (Seniority level cards, question count segmented pills, generation CTA).
- **Mobile/Tablet**: On screens `<1024px`, the grid collapses into a single column (`grid-cols-1`), maintaining logical top-to-bottom reading order.

*Rationale*: Separating content definition from configuration parameters reduces visual cognitive load and fits comfortably within standard desktop viewport heights without requiring deep vertical scrolling.

### 2. Minimalist Typographic Seniority Level Cards

**Decision**: Remove all potential icon decorations, emojis, and visual badges from the 4 seniority levels (`Fresher / Entry`, `Junior`, `Mid-Level`, `Senior / Staff`).
- Card structure:
  - Header line: Bold level title (`text-sm sm:text-base font-bold`) and a discrete 8px accent dot (`w-2 h-2 rounded-full bg-brand-500`) when selected.
  - Body copy: Clear explanation of technical depth (`text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-normal leading-relaxed`).
- States:
  - Selected: `border-brand-500 bg-brand-500/10 text-brand-900 dark:text-white ring-1 ring-brand-500/30`.
  - Unselected: `border-slate-200/80 dark:border-white/[0.06] bg-white/60 dark:bg-white/[0.02] hover:border-slate-300 dark:hover:border-white/[0.15]`.

*Rationale*: Directly honors user directive ("không phải spam icon là tốt"). Developers appreciate high-density, clean, distraction-free interfaces that convey information through typography rather than novelty icons.

### 3. Integrated Segmented Pill Question Count

**Decision**: Replace isolated square buttons with an integrated segmented container:
```html
<div class="flex items-center p-1 rounded-xl bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08]">
  <button :class="selectedCount === 5 ? 'bg-brand-600 text-white shadow-sm font-bold' : 'text-slate-600 dark:text-slate-400'">
    5 Questions
  </button>
  <button :class="selectedCount === 10 ? 'bg-brand-600 text-white shadow-sm font-bold' : 'text-slate-600 dark:text-slate-400'">
    10 Questions
  </button>
</div>
```

*Rationale*: Creates a cohesive visual unit consistent with `AppSelect` and the pace chips in `/profile`, preventing visual fragmentation.

### 4. Clean Studio Header (No Halos / No Pulse)

**Decision**: Construct the page header using a clean glass icon badge and typographic lockup:
- Icon tile: `w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0` (static, no pulse animations, no outer glow blur).
- Title & Subtitle: High-contrast title (`text-xl sm:text-2xl font-black`) accompanied by a subtle subtitle explaining the studio function.

*Rationale*: Aligns with the Dev-Learning Studio brand language while remaining calm and purposeful.

## Component Anatomy

```
+---------------------------------------------------------------------------------+
| STUDIO HEADER: [Icon] Quiz Arena Studio — Generate AI Interview Drills          |
| Tabs: [ Sparkles: Generate ] [ Swords: Arena ] [ RotateCcw: Review ] [ Stats ]  |
+---------------------------------------------------------------------------------+
| BENTO GENERATION GRID (lg:grid-cols-12 gap-6)                                   |
|                                                                                 |
|  LEFT BENTO: Source & Scope (7 cols)    |  RIGHT BENTO: Config & Action (5 cols)|
|  +------------------------------------+  +------------------------------------+ |
|  | Topic Input & Enter Handler        |  | Seniority Level (4 Typographic)    | |
|  | [ Text input: e.g. .NET Internals] |  | +--------------------------------+ | |
|  |                                    |  | | Fresher / Entry             (•)| | |
|  | Quick Topic Chips                  |  | | Core syntax, OOP basics        | | |
|  | [ Chip ] [ Chip ] [ Chip ]         |  | +--------------------------------+ | |
|  |                                    |  | | Senior / Staff              (•)| | |
|  | Grounded in Document (Toggle)      |  | | Under-the-hood runtime, memory | | |
|  | +--------------------------------+ |  | +--------------------------------+ | |
|  | | [BookOpen] Grounded in Book [X]| |  |                                    | |
|  | | [ AppSelect: Choose document ] | |  | Question Count (Segmented Pills)   | |
|  | +--------------------------------+ |  | [ 5 Questions ] [ 10 Questions ]  | |
|  |                                    |  |                                    | |
|  |                                    |  | [ Button: Generate Interview Quiz ]| |
|  +------------------------------------+  +------------------------------------+ |
+---------------------------------------------------------------------------------+
```

## Risks / Trade-offs

- **Risk**: Stacking on mobile screens could place the primary generation CTA far down the page if the left column is lengthy.
  - **Mitigation**: The left column contents are compact (topic input, 7 chips, and a single toggle card); on mobile, the entire form fits comfortably in ~1.5 viewport heights.
- **Risk**: Existing test suites might fail if CSS selector expectations or DOM hierarchy change.
  - **Mitigation**: Retain all critical `data-testid` attributes (`generate-tab-btn`, `generate-quiz-btn`, `arena-tab-btn`), reactive variable names, and event handlers.
