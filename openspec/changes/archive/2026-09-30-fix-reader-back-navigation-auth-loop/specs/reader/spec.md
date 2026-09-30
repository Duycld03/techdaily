# Spec Delta: reader

## MODIFIED Requirements

### Requirement: Dedicated Reading Route
The system SHALL provide a dedicated reader page at `/read/[bookId]` with query parameter `?slice={chunkOrder}`, navigating from book cards in `/library` or `/today`. The reading route SHALL render in a standalone distraction-free immersion mode that hides the global application header and global navigation sidebar, providing an integrated reader header containing back navigation, slice progress, chapter drawer toggle, novel-style typography settings toggle (`Aa`), theme toggle, and 1-click quiz launcher. The reader page shell, sticky header, popovers, and backdrop elements SHALL render with Dev-Learning Studio obsidian canvas tokens (`dark:bg-canvas`, `dark:bg-canvas-subtle`, `dark:bg-canvas-elevated`) and hairline translucent borders (`border-slate-200/80 dark:border-white/[0.08]`).

1. **Non-Destructive Origin Isolation & History Stack Preservation**:
   - Navigation to `/read/[bookId]` SHALL NOT overwrite or erase preceding browser history entries using destructive `window.location.replace()`.
   - Client-side navigation to `/read/[bookId]` SHALL preserve the referring route history intact, allowing browser back operations to return to the originating dashboard (`/`, `/today`, or `/library`).
   - Any page-level cross-origin isolation checks or reload workarounds SHALL strictly preserve the navigation entry or record an explicit return route (`from` query parameter) so backward traversal reliably restores the prior user context.

2. **Context-Aware Return Navigation**:
   - The reader header back button (`ReaderHeaderBar.vue`) SHALL inspect the route query parameter (`from`) or previous history state.
   - When an explicit `from` query parameter is present (e.g. `?from=/` or `?from=/library`), clicking the back button SHALL navigate directly to that path.
   - When no explicit `from` parameter exists, the back button SHALL use browser history back (`router.back()`) only if valid application history exists, safely defaulting to the root dashboard (`/`) rather than unhandled history states.

#### Scenario: User opens a book from the library
- **GIVEN** an authenticated or guest user browsing `/library`
- **WHEN** user clicks "Read Book" on a book card in `/library`
- **THEN** application navigates to `/read/[bookId]` loading the first slice or bookmarked slice without global application chrome.

#### Scenario: Mobile viewport reader header layout
- **GIVEN** user is reading on a mobile screen width (<768px)
- **WHEN** user views `/read/[bookId]`
- **THEN** reader header displays a single compact top bar with back arrow, truncated book title with slice indicator, table of contents button, typography toggle (`Aa`), and theme toggle without vertical header duplication.

#### Scenario: Dev-Learning Studio theme styling on reader shell
- **GIVEN** user views `/read/[bookId]` in dark mode
- **WHEN** the reader page and header render
- **THEN** the root container renders with `dark:bg-canvas` (`#09090b`)
- **AND** the top header renders with `dark:bg-canvas/90` with hairline border `dark:border-white/[0.08]`
- **AND** the 1-Click Quiz button renders with primary brand tokens (`bg-brand-50 dark:bg-brand-500/10 text-brand-700 dark:text-brand-400`) instead of hardcoded purple tokens.

#### Scenario: Reader interface 100% localization coverage
- **WHEN** user views the dedicated reader route in any supported locale (English or Vietnamese)
- **THEN** all typography controls (font families "Sans", "Serif", "Mono", size adjusters "Smaller Font" / "Larger Font"), slice navigation hints ("Previous Slice (Shift + ←)" / "Next Slice (Shift + →)"), Table of Contents close buttons, and chapter loading spinners render with localized text from the i18n message catalog without raw English literals.

#### Scenario: User navigates from Home Dashboard to Reader and clicks Back
- **WHEN** an authenticated user clicks "Đọc Tiếp" from `/` to navigate to `/read/[bookId]?slice=2`
- **THEN** the browser history SHALL retain `/` as the preceding entry
- **AND** clicking the `< Quay Lại` back button in the reader header bar SHALL return the user to `/` without jumping to `/today` or triggering a page reload.

#### Scenario: User navigates from Library or Today to Reader and clicks Back
- **WHEN** a user opens `/read/[bookId]?slice=1` from `/library` or `/today`
- **THEN** clicking the reader back button SHALL return the user to the referring surface (`/library` or `/today`) seamlessly.
