# Design

## Context

In `frontend/assets/css/main.css`, a high-contrast focus ring rule introduced for keyboard navigation applied `outline: 2px solid #8b5cf6 !important; outline-offset: 2px !important; box-shadow: 0 0 0 4px rgba(139, 92, 246, 0.2) !important;` indiscriminately to all `input:focus-visible:not(.bg-transparent)` and `textarea:focus-visible:not(.bg-transparent)`.

Because text input fields throughout DeepPace use modern rounded border geometry (`rounded-xl`, `rounded-2xl`), the 2px outline offset creates an empty gap between the input border and an outer floating neon purple rectangular halo. This overrides component-level Tailwind classes (`focus:outline-none`), resulting in an awkward double-border visual glitch. When a modal opens with an `autofocus` input (such as the AI generator modal on `/insights`), this detached halo appears immediately.

## Goals / Non-Goals

**Goals:**
- Eliminate the detached purple outline halo on text inputs and textareas across the entire application.
- Retain WCAG 2.1 AA keyboard focus contrast on interactive controls (`button`, `a`, `select`, `[role="button"]`).
- Enable form inputs and textareas to display flush, border-radius-conforming focus states (`focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20`).
- Cleanly localize the "Cancel" button in the `/insights` AI generation modal.

**Non-Goals:**
- No changes to keyboard focus indication on buttons, navigation links, or dropdown selectors.
- No changes to backend API contracts or AI generation logic.

## Decisions

### 1. Segregate Discrete Action Buttons from Form Input Fields in `main.css`

Update `main.css`:
```css
/* High-contrast studio focus ring for keyboard navigation (WCAG 2.1 AA) */
button:focus-visible,
a:focus-visible,
[role="button"]:focus-visible,
select:focus-visible {
  outline: 2px solid #8b5cf6 !important;
  outline-offset: 2px !important;
  box-shadow: 0 0 0 4px rgba(139, 92, 246, 0.2) !important;
  -webkit-tap-highlight-color: transparent !important;
}

/* Form inputs & textareas use flush studio focus styling conforming to border-radius */
input:focus-visible,
textarea:focus-visible {
  outline: none !important;
}
```

*Rationale*: Discrete controls (buttons, links, select menus) have fixed rectangular/pill boundaries where a 2px offset outline works well. In contrast, text input fields and textareas contain editable text cursors and custom rounded corners; their focus state must be rendered flush along their border using border color changes and inset/flush rings (`ring-2 ring-brand-500/20`).

### 2. Update Generator Modal Input & Action Button in `insights.vue`

1. Update the input field:
```html
<input
  v-model="customTopicInput"
  @keyup.enter="handleGenerateSubmit"
  type="text"
  :placeholder="modalPlaceholder"
  class="w-full px-4 py-3 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-50 dark:bg-canvas-elevated text-sm text-slate-900 dark:text-white placeholder:text-slate-400 focus:outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 transition-all"
  autofocus
/>
```
2. Update the Cancel button:
```html
<button
  @click="isGenerateModalOpen = false"
  class="px-4 py-2.5 rounded-xl text-xs font-bold text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-canvas-elevated transition-colors cursor-pointer"
>
  {{ $t('insights.cancel') }}
</button>
```

### 3. Localization Consistency in `en.json` and `vi.json`

Add `"cancel": "Hủy Bỏ"` to `vi.json` and `"cancel": "Cancel"` to `en.json` under `insights`.

## Risks / Trade-offs

- **WCAG Keyboard Visibility**: Inputs and textareas will still clearly indicate focus through the high-contrast `border-brand-500` color transition and `ring-2 ring-brand-500/20` glow, satisfying WCAG 2.1 AA while eliminating the detached visual defect.
