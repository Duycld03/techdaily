# Proposal

## Why

Following the completion of Phases 1–6 of the Dev-Learning Studio UI modernization (App Shell, Home Command Center, Focus Studio Reader, Curriculum Roadmap, 3D Knowledge Cosmos, and Practice Studio), three key Knowledge Hub surfaces still rely on legacy Slate palettes (`slate-950`, `slate-900`, `slate-800`) and legacy Indigo accents (`indigo-600`):

1. **`frontend/pages/library.vue`**: Document catalog, import modal, and PDF/Web crawler still render over legacy dark slate backgrounds with opaque borders.
2. **`frontend/pages/notes.vue`**: Highlights notebook and flashcard generator use legacy `indigo-600` buttons and slate card containers.
3. **`frontend/pages/insights.vue`**: Flash insights and AI generation use legacy indigo gradient banners and opaque slate cards.

Modernizing these three surfaces to the unified Dev-Learning Studio design tokens (Obsidian `dark:bg-canvas`, Iris Violet `brand-600`/`brand-500`, `.glass-card`, `.glass-panel`, and translucent hairline borders `border-white/[0.08]`) will complete the Knowledge Hub tier.

## What Changes

- **Library Studio (`library.vue`)**:
  - Replace `dark:bg-slate-950` root with `dark:bg-canvas` (`#09090b`).
  - Upgrade filter pills and search input to glass styling (`bg-white dark:bg-canvas-subtle border-slate-200/80 dark:border-white/[0.08]`).
  - Convert book grid cards to `.glass-card` with Iris Violet progress bars (`bg-gradient-to-r from-brand-600 to-brand-500`) and translucent tag badges.
  - Modernize the Import modal (Markdown, PDF dropzone, and Web Crawler) with `.glass-panel` elevation, refined tabs, and Iris Violet action triggers.
- **Highlights & Notes Studio (`notes.vue`)**:
  - Replace `dark:bg-slate-950` root with `dark:bg-canvas`.
  - Upgrade horizontal tag filter bar to use Iris Violet active chip state (`bg-brand-600 text-white`) and subtle dark glass inactive chips.
  - Transform highlight cards into `.glass-card` containers with Iris Violet source tags, sleek markdown quote styling, and glass action buttons.
  - Update quick flashcard creation CTA and edit inputs to match Studio design tokens.
- **Architectural Insights Studio (`insights.vue`)**:
  - Modernize the Header Banner with `.glass-panel` elevation, removing legacy `from-indigo-50` / `to-indigo-950` gradient backgrounds.
  - Upgrade Shuffle and "Generate with AI" buttons to Studio brand styles (`bg-brand-600 hover:bg-brand-500`).
  - Refine Insight quote cards with high-contrast obsidian backgrounds, crisp category badges, and smooth glass action controls (Save, Share, Flashcard).
  - Modernize the AI Generation modal with glass overlays and hairline borders.

## Capabilities

### Modified Capabilities

- `library`: Update Library view requirements to specify Dev-Learning Studio styling (Obsidian canvas, glass card catalog, and refined import crawler modal).
- `notes`: Update Notes view requirements to specify Dev-Learning Studio styling (Obsidian canvas, glass highlight cards, and Iris Violet tag chips).
- `insights`: Update Insights view requirements to specify Dev-Learning Studio styling (Obsidian canvas, glass telemetry banner, and modernized insight cards).

## Impact

- **Affected Code**: `frontend/pages/library.vue`, `frontend/pages/notes.vue`, `frontend/pages/insights.vue`, and nearby test assertions.
- **User Experience**: Consistent, engineering-grade visual theme across all knowledge retrieval and note-taking interfaces.
- **Breaking Changes**: None.
