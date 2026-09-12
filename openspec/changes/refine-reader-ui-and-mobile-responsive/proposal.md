# Refine Reader UI and Mobile Responsive Experience

## Why

The current reader interface (`/read/[bookId]`) suffers from layout clutter, formatting anomalies, and sub-optimal mobile ergonomics. Desktop readers are squeezed between two stacked navigation bars (global header + reader header) and two parallel sidebars (global app sidebar + book TOC), occupying over 40% of horizontal space. On mobile, vertical screen real estate is consumed by duplicate headers, while inline code badges display unwanted backticks, code copy buttons trigger runtime errors, slice titles duplicate within markdown bodies, and slice order badges display misleading "Day X of Y" labels.

Enhancing the reader UI with an immersive distraction-free mode on desktop, a streamlined single-header layout on mobile with an off-canvas TOC drawer, clean typography, and sanitized markdown rendering restores an enjoyable, high-focus reading experience.

## What Changes

- **Standalone Immersive Layout**: Automatically hide the global `AppHeader` and `AppSidebar` when navigating to `/read/*`, giving 100% screen width and height to the technical document.
- **Unified Reader Header**: Combine navigation (`< Library`), book title, slice progress, chapter TOC toggle, theme toggle, and 1-click quiz launcher into a single, compact, responsive header.
- **Deduplicated Chapter Titles**: Detect and suppress redundant heading tags at the beginning of markdown content when they match the chapter title already rendered in the H1 title element.
- **Clean Inline Code & Code Block Copy Fix**:
  - Add `prose-code:before:content-none prose-code:after:content-none` to eliminate Tailwind Typography's default backtick pseudo-elements around inline code pills.
  - Fix variable reference scope bug in `useMarkdownRenderer.ts` so code block "Copy" buttons reliably decode and copy code snippets without runtime errors.
  - Normalize 4-space indented blocks in scraped/imported markdown so explanatory text does not unexpectedly render as dark monospace code blocks.
- **Accurate Slice Badge Terminology**: Replace the daily drill `"Day X of Y"` label with `"Slice X of Y"` / `"Lát cắt X / Y"`.
- **Mobile-First Touch Ergonomics**:
  - Off-canvas slide-over Table of Contents drawer with backdrop blur and instant chapter jump.
  - Full-width, thumb-friendly next slice action button at the bottom of the reading pane on mobile.
  - Safe horizontal scroll for code blocks and tables without causing page-level layout shifts.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `reader`: Update requirements for dedicated reading route layout (standalone fullscreen mode without global chrome), responsive TOC drawer behavior on mobile, deduplicated slice headings, sanitized inline code rendering, and thumb-friendly slice navigation footer.

## Impact

- **Frontend Pages**: `frontend/pages/read/[bookId].vue`, `frontend/app.vue` (conditional global chrome rendering on reader route).
- **Composables & Utilities**: `frontend/composables/useMarkdownRenderer.ts` (Shiki copy helper fix, inline code styling, heading deduplication).
- **Localization**: `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` (accurate slice badges, thumb-friendly navigation labels).
- **Zero Backend / Database Schema Impact**: API contracts and database entities remain unchanged.
