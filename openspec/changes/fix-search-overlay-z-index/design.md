# Design

## Context

The Command Palette is rendered via Vue `<Teleport to="body">` as a full-viewport modal overlay. It must appear on top of all page elements, including sticky headers (`z-40`), slide-over drawers (`z-50`), floating action bars (`z-50`), and dropdown menus (`z-50`).
`AppCommandPalette.vue` originally used `z-60`, which is not part of Tailwind CSS's default z-index scale (max `z-50`), resulting in no `z-index` property being generated.

## Goals / Non-Goals

**Goals:**
- Ensure the Command Palette overlay renders above all `z-40` and `z-50` elements with `z-index: 60`.
- Use idiomatic, maintainable Tailwind CSS syntax.

**Non-Goals:**
- Refactoring the entire z-index scale of the application.

## Decisions

### 1. Use Tailwind Arbitrary Value `z-[60]`
Tailwind CSS supports JIT arbitrary values via square brackets: `z-[60]`. This directly generates `z-index: 60` without requiring global `tailwind.config.js` theme extensions that may conflict or fail to compile numeric keys.
Applied to the backdrop wrapper in `frontend/components/app/AppCommandPalette.vue`:
```vue
class="fixed inset-0 z-[60] flex items-start justify-center..."
```

## Risks / Trade-offs

- **[Risk] Magic numbers in utility classes** → Mitigation: `z-index: 60` is standard for high-priority global dialog overlays and is clearly scoped to the single top-level teleported Command Palette component.
