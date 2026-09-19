# Proposal: Standardize Lucide Icon System and Streamline Studio Visuals

## Why

Across the TechDaily frontend, icon usage currently lacks strict visual hierarchy and exhibits decorative inconsistencies:
- Heavy default 2px stroke weights appear overly chunky against dark obsidian surfaces (`#09090b`), lacking the refined aesthetic of tools like Linear and Raycast.
- `Sparkles` icons are overused as generic catch-all decorations, including spinning sparkle animations (`<Sparkles class="animate-spin" />`) used as loading indicators instead of dedicated engineering loaders.
- Icon sizing and containers are inconsistently applied—some icons float unanchored without containers while others use mismatched tile geometries and scattered rainbow colors.

Standardizing the Lucide icon system with a sleek 1.5px stroke width, consistent 3-tier size hierarchy, unified translucent tile containers, and clean semantic icon mappings will deliver a calm, professional, distraction-free Dev Studio aesthetic.

## What Changes

- **Sleek 1.5px Stroke Width Standard**:
  - Adopt a crisp, consistent 1.5px stroke width (`:stroke-width="1.5"`) across primary navigation, Bento studio headers, action triggers, and card footers to eliminate clunky 2px line weights on high-contrast dark mode surfaces.
- **Strict 3-Tier Icon Size Hierarchy**:
  - **Micro Icons (`w-3.5 h-3.5` / 14px)**: Reserved exclusively for compact status tags, metadata chips, and inline badges.
  - **Standard Action & Navigation Icons (`w-4 h-4` / 16px)**: Standard size for navigation links, input triggers, tab switchers, and action buttons.
  - **Feature Tile & Studio Header Icons (`w-5 h-5` / 20px)**: Prominent icons hosted inside standardized glass tile containers.
- **Standardized Translucent Icon Tile Containers**:
  - Unify feature card and studio section headers using a standard icon tile container: `p-2.5 rounded-2xl bg-brand-500/10 border border-brand-500/20 text-brand-600 dark:text-brand-400 shrink-0`.
- **Prune `Sparkles` Misuse & Semantic Realignment**:
  - Replace spinning sparkle loading indicators (`<Sparkles class="animate-spin" />`) in `AISynthesisCard.vue`, `DocReaderPane.vue`, and `[bookId].vue` with standard engineering spinners (`Loader2 class="animate-spin"`).
  - Reassign `Sparkles` in navigation and overview headers to semantic, purpose-built icons (e.g. `Compass` for Insights navigation in `AppHeader.vue` and `AppSidebar.vue`, `BookmarkCheck` or `CheckCircle2` for reader takeaways).
- **Color Discipline & Rainbow Tone Elimination**:
  - Normalize scattered saturated rainbow colors in milestone cards and dashboard widgets to studio neutral slate (`text-slate-400 dark:text-slate-500`) with Deep Iris Violet (`text-brand-500 dark:text-brand-400`) for primary active accents.
- **Zero Breaking Changes**:
  - Existing celebration behaviors (such as queue completion confetti) are preserved as specified in existing capabilities. No backend changes, no database migrations, and no API contract modifications.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `core-platform`: Update the Dev-Learning Studio design standard to mandate the 1.5px stroke width standard, 3-tier size hierarchy, standardized icon tile containers, and semantic icon mapping.

## Impact

- **Frontend**: Core layout components (`AppHeader.vue`, `AppSidebar.vue`), reader views (`DocReaderPane.vue`, `pages/read/[bookId].vue`), studio cards (`AISynthesisCard.vue`, `EngineerMilestonesCard.vue`, `FlashcardHeroCard.vue`), and page navigation across `/quiz`, `/insights`, `/library`, `/notes`, `/review`.
- **Specs**: `openspec/specs/core-platform/spec.md`.
- **User Experience**: A cohesive, elegant developer aesthetic with consistent stroke geometry, clear visual hierarchy, and elimination of distracting decorative sparkle animations.
