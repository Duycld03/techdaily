# Design: Modernize Library Card Status Badge

## Context

See `proposal.md` for motivation.

In `frontend/pages/library.vue`, document cards in the catalog display either a resume bookmark badge or a readiness status badge at the bottom of the card metadata section.
Currently:
```vue
<div v-else-if="book.status === 'Ready' || (book.status as any) === 2" class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-emerald-50 dark:bg-emerald-500/10 border border-emerald-200/80 dark:border-emerald-500/20 text-emerald-700 dark:text-emerald-400 text-xs sm:text-sm font-semibold">
  <CheckCircle2 class="w-3.5 h-3.5 text-emerald-500" />
  <span>{{ $t('library.ready_to_read') }}</span>
</div>
```

This design outlines the refactoring of this badge to align with the Dev-Learning Studio design language and the project's TypeScript typing rules.

## Goals / Non-Goals

**Goals:**
- Replace green emerald styling on the unread ready badge with Dev-Learning Studio neutral Obsidian tokens (`bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-slate-600 dark:text-slate-400`).
- Replace `CheckCircle2` with `BookOpen` icon (`w-3.5 h-3.5 text-slate-500 dark:text-slate-400`) to visually communicate documentation reading readiness.
- Remove `(book.status as any)` to satisfy the `ts-no-any` invariant.

**Non-Goals:**
- Changing book card layout, grid structure, or bookmark resume badge logic.
- Altering backend `ProcessingStatus` enum or library API contracts.

## Decisions

### 1. Neutral Studio Tokens for Static Availability

- **Approach**:
  Update badge classes:
  ```html
  <div
    v-else-if="book.status === 'Ready' || (book.status as unknown as number) === 2"
    class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-slate-600 dark:text-slate-400 text-xs sm:text-sm font-semibold"
  >
    <BookOpen class="w-3.5 h-3.5 text-slate-500 dark:text-slate-400" />
    <span>{{ $t('library.ready_to_read') }}</span>
  </div>
  ```
- **Rationale**:
  In Dev-Learning Studio, Emerald is strictly reserved for positive evaluation states (100% quiz scores, optimal architectural choices). A ready-to-read book in a catalog is a neutral availability state, best expressed by subtle obsidian glass styling.

### 2. Icon Shift from CheckCircle2 to BookOpen

- **Approach**:
  Use `BookOpen` (already imported in `library.vue` from `lucide-vue-next`).
- **Rationale**:
  A checkmark communicates task completion (e.g. "you have read this"), whereas `BookOpen` communicates an invitation to begin reading.

## Risks / Trade-offs

- **Visual Contrast**:
  The neutral badge must provide sufficient contrast in both light and dark modes. `text-slate-600 dark:text-slate-400` over `bg-slate-100 dark:bg-canvas-subtle` meets WCAG 2.1 AA contrast requirements ($> 4.5:1$).
