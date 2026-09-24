# Design

## Context

In `frontend/pages/read/[bookId].vue`, text selection triggers a teleported floating toolbar (`floatingToolbar`) with 3 actions (`handleExplainSelection`, `handleHighlightAndNote`, `handleCopySelection`), where `handleHighlightAndNote` expands a nested reflection popover (`isNotePopoverOpen`). 

While the functional logic operates smoothly, the visual execution diverges from the **Dev-Learning Studio** design system (see `proposal.md` for motivation):
- The `Highlight/Note` button uses an aggressive `bg-amber-500` / `text-slate-950` styling that clashes with the primary Iris Violet theme.
- The reflection `<textarea>` and tag `<input>` omit explicit placeholder bindings, rendering as empty dark boxes with no prompt text.
- The focus styling on the textarea produces a thick, bright neon-purple border outline.
- The popover container lacks subtle depth, elevation, and refined glassmorphic styling.

## Goals / Non-Goals

**Goals:**
- Unify the floating toolbar button group into a cohesive Dev-Learning Studio hierarchy:
  - `Explain with Gemini`: Primary Iris Violet accent pill (`bg-brand-600 hover:bg-brand-500 text-white`).
  - `Highlight/Note`: Subtle brand-tinted glass (`bg-brand-500/10 text-brand-300 border border-brand-500/20 hover:bg-brand-500/20 hover:text-white`), elevating to solid brand violet (`bg-brand-600 text-white`) when the note popover is actively open. Zero amber or orange colors.
  - `Copy`: Neutral translucent glass button with hover contrast.
- Fix missing input placeholders by binding `:placeholder="$t('reader.note_placeholder')"` and `:placeholder="$t('reader.tags_placeholder')"`.
- Replace the harsh neon focus outline with a soft, refined focus glow (`focus:ring-1 focus:ring-brand-500/40 focus:border-brand-500/60`).
- Elevate popover shell aesthetics with glassmorphic backdrop blur, hairline translucent borders (`border-slate-700/60 dark:border-white/[0.12]`), responsive width (`w-72 sm:w-80 max-w-[calc(100vw-2rem)]`), and an Iris Violet quote border accent (`border-l-2 border-brand-500/60`).
- Ensure 100% bilingual responsiveness (`whitespace-nowrap shrink-0`) in English and Vietnamese.

**Non-Goals:**
- Altering the backend API contracts (`POST /api/v1/notes/highlights` or `PUT /api/v1/notes/highlights/{id}`).
- Redesigning the full-screen reader layout or markdown body typography.
- Adding third-party component libraries (keep pure Vue 3 + Tailwind CSS + Lucide icons).

## Decisions

### Decision 1: Unified Semantic Button Palette in Floating Toolbar
- **Rationale**: The previous amber button clapped loudly against the dark canvas and violet Gemini button. Using Iris Violet brand tones with varying elevation and opacity (`bg-brand-600` for primary action, `bg-brand-500/10` with `border-brand-500/20` for secondary highlight action) creates a serene, unified IDE-grade learning environment.
- **Alternatives Considered**: Keeping amber for "Highlight" to denote yellow highlighter pens. Rejected because it introduces visual noise and violates the single primary brand palette invariant defined in `AGENTS.md`.

### Decision 2: Explicit Placeholder Bindings on Inputs
- **Rationale**: The translation keys `reader.note_placeholder` ("Viết đúc kết hoặc suy ngẫm kiến trúc của bạn..." / "Write your reflection or architectural takeaway...") and `reader.tags_placeholder` ("Thẻ phân loại (vd: storage, concurrency)" / "Tags (e.g. storage, concurrency)") already exist in `en.json` and `vi.json` but were never bound via `:placeholder`. Adding these bindings instantly provides clear mental scaffolding for engineers.

### Decision 3: Soft Focus Ring & Hairline Borders
- **Rationale**: Heavy focus outlines look unpolished. Applying `focus:ring-1 focus:ring-brand-500/40 focus:border-brand-500/60` over `bg-slate-900 dark:bg-canvas-subtle` and `border-slate-700/80 dark:border-white/[0.10]` delivers subtle, accessible focus indication without jarring edges.

## Risks / Trade-offs

- **Viewport Boundary Clipping**: On small mobile devices ($375\text{px}\text{--}390\text{px}$), a fixed $320\text{px}$ popover could overflow the screen edge.
  - *Mitigation*: Restrict width to `w-72 sm:w-80 max-w-[calc(100vw-2rem)]` and ensure `floatingToolbar.x` positioning clamps within screen margins.
- **Bilingual Button Width**: Vietnamese button titles (`Đánh dấu / Ghi chú`, `Giải Thích Bằng Gemini`) are longer than English (`Highlight / Note`, `Explain with Gemini`).
  - *Mitigation*: Add `whitespace-nowrap shrink-0` and responsive button padding (`px-2.5 py-1.5 sm:px-3`) so text never awkwardly wraps across two lines.
