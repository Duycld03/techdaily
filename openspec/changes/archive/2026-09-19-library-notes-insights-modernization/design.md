# Design

## Context

Following the modernization of core platform views (App Shell, Bento Dashboard, IDE Reader, Roadmap, 3D Knowledge Cosmos, and Practice Studio), the three knowledge curation pages (`library.vue`, `notes.vue`, and `insights.vue`) still contain legacy Slate and Indigo styles:
- Opaque backgrounds (`dark:bg-slate-950`, `dark:bg-slate-900`) instead of obsidian canvas (`dark:bg-canvas`).
- Heavy borders (`border-slate-800`, `border-slate-700`) instead of translucent hairline borders (`border-white/[0.08]`).
- Indigo accents (`indigo-600`, `from-indigo-50`, `to-indigo-950`) instead of the unified Iris Violet (`brand-600`, `brand-500`).

## Goals / Non-Goals

**Goals:**
- Replace legacy dark slate backgrounds with `dark:bg-canvas` across all 3 pages.
- Convert cards, filter bars, and modal dialogs to `.glass-card` and `.glass-panel` elevation.
- Migrate all action buttons and active selection chips from legacy Indigo to Iris Violet (`brand-600`, `brand-500`).
- Ensure zero breaking changes to existing stores, API endpoints, or user interaction behavior.

**Non-Goals:**
- Altering backend APIs, DTOs, or database schemas.
- Changing business logic for PDF crawling, highlight creation, or insight generation.

## Decisions

### 1. Library Studio Modernization (`library.vue`)
- **Page Container**: Change `bg-slate-50 dark:bg-slate-950` to `bg-slate-50 dark:bg-canvas`.
- **Category Filter Pills**: Replace `dark:bg-slate-800 border-slate-700` with `bg-white dark:bg-canvas-subtle border-slate-200/80 dark:border-white/[0.08]`. Active state uses `dark:bg-canvas-elevated text-brand-400 border-brand-500/30`.
- **Search Bar**: Upgrade to `dark:bg-canvas-subtle border-slate-200 dark:border-white/[0.08]`.
- **Document Cards**: Replace `bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800` with `.glass-card border-slate-200/80 dark:border-white/[0.08] hover:border-brand-500/30`.
- **Progress Bar**: Apply `bg-gradient-to-r from-brand-600 to-brand-500` over `dark:bg-canvas-elevated`.
- **Import Modal**: Apply `.glass-panel` to modal container with backdrop blur `backdrop-blur-md bg-black/60`.

### 2. Notes & Highlights Hub Modernization (`notes.vue`)
- **Page Container**: Change `bg-slate-50 dark:bg-slate-950` to `bg-slate-50 dark:bg-canvas`.
- **Tag Chip Bar**: Replace `indigo-600` active state with `bg-brand-600 text-white shadow-sm shadow-brand-500/20`. Inactive chips use `bg-white dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200 dark:border-white/[0.08] hover:border-white/[0.16]`.
- **Highlight Cards**: Replace `bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800` with `.glass-card border-slate-200/80 dark:border-white/[0.08]`.
- **Excerpt Block**: Add Iris Violet left accent line (`border-l-2 border-brand-500/60 pl-3 bg-white/[0.02]`).
- **Action Buttons**: Convert `⚡ Flashcard SM-2` to `text-brand-400 hover:text-brand-300 bg-brand-500/10 border-brand-500/20`.

### 3. Insights Studio Modernization (`insights.vue`)
- **Header Banner**: Replace `from-indigo-50/80 via-white to-brand-50/50 dark:from-slate-900 dark:via-slate-900 dark:to-indigo-950/40` with `.glass-panel border-slate-200/80 dark:border-white/[0.08]`.
- **Action Buttons**:
  - Shuffle: `bg-white dark:bg-canvas-subtle hover:dark:bg-canvas-elevated border-slate-200 dark:border-white/[0.08] text-slate-200`.
  - Generate AI: `bg-brand-600 hover:bg-brand-500 text-white shadow-brand-500/20`.
- **View Mode Switcher**: Glass pill container with `bg-brand-600/15 text-brand-400 border-brand-500/30` for active tab.
- **Card Reader**: Glass card container with syntax-highlighted code blocks rendered in dark obsidian panels (`bg-black/40 border-white/[0.06]`).
- **AI Modal**: Glass panel with dark glass input fields and Iris Violet triggers.

## Risks / Trade-offs

- **Risk**: Test assertions looking for specific CSS classes (e.g. `indigo-600`).
  - *Mitigation*: Existing tests in `library.spec.ts`, `notes.spec.ts`, and `insights.spec.ts` primarily test text content, pagination, and store actions. We will run tests after each file edit.
