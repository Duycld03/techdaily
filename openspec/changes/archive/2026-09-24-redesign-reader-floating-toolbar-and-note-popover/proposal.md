# Proposal

## Why

The current reader text-selection floating toolbar and expandable note popover suffer from visual inconsistencies and ergonomics issues that violate the Dev-Learning Studio design system. The toolbar presents jarring color clashes (a harsh amber/orange button directly adjacent to an Iris Violet Gemini button), the reflection textarea and tag input completely lack visible placeholder text (leaving blank dark voids with zero guidance), and the textarea displays an unrefined, glaring purple focus outline. 

Upgrading this surface aligns the in-reader highlight and reflection workflow with the rest of the application's sleek, obsidian glassmorphic architecture, ensuring clean visual hierarchy, clear input affordances, and zero color disharmony.

## What Changes

- **Cohesive Floating Toolbar Button Hierarchy**: Redesign the 3 actions (`Explain with Gemini`, `Highlight/Note`, `Copy`) using unified Dev-Learning Studio semantic tokens:
  - Replace the jarring bright amber button on `Highlight/Note` with an Iris Violet-tinted glass control (`bg-brand-500/10 text-brand-300 border-brand-500/20 hover:bg-brand-500/20 hover:text-white` when inactive, transitioning to an elevated active state when the note popover is open).
  - Unify `Explain with Gemini` as a primary Iris Violet accent button (`bg-brand-600 hover:bg-brand-500 text-white`).
  - Style `Copy` as a refined translucent neutral glass button with hover contrast.
- **Input Placeholders & Guidance**:
  - Add explicit `:placeholder="$t('reader.note_placeholder')"` to the reflection textarea so users immediately understand its purpose.
  - Add explicit `:placeholder="$t('reader.tags_placeholder')"` to the tag input with hashtag prefix affordance.
- **Refined Popover Shell & Focus States**:
  - Replace the thick neon-purple outline on the textarea with a subtle hairline border (`border-slate-700/80 dark:border-white/[0.10]`) and a soft focus ring (`focus:ring-1 focus:ring-brand-500/40 focus:border-brand-500/60`).
  - Upgrade the popover container with glassmorphic obsidian styling (`bg-slate-950/95 dark:bg-canvas-elevated/95 backdrop-blur-xl border border-slate-700/60 dark:border-white/[0.12] rounded-2xl shadow-2xl`).
  - Format the quote preview with Iris Violet left accent bar (`border-l-2 border-brand-500/60 pl-2.5 py-0.5 text-xs text-slate-300 dark:text-slate-300 italic line-clamp-2`).
- **Footer Actions Polish**:
  - Align "Cancel" (`$t('reader.cancel')`) and "Save Note" (`$t('reader.save_note')`) buttons with proper hover states, loading spinner on save, and keyboard Enter support.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `reader`: Update the floating selection toolbar and expandable note popover specifications to enforce Dev-Learning Studio design system tokens, cohesive button hierarchy, visible input placeholders, and refined focus outlines.

## Impact

- **Frontend**: `frontend/pages/read/[bookId].vue` (and any related floating toolbar child components or styles).
- **Localization**: Uses existing keys in `frontend/i18n/locales/en.json` and `vi.json` (`reader.note_placeholder`, `reader.tags_placeholder`, `reader.highlight_note`, etc.).
- **Tests**: `frontend/tests/pages/read.spec.ts` (or component test covering text selection toolbar).
