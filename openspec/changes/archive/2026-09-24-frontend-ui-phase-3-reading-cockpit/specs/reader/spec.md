# Spec Delta: reader

## ADDED Requirements

### Requirement: Modular Reader Chrome Architecture
The reader studio at `/read/[bookId]` SHALL decompose its monolithic interface into dedicated, single-responsibility sub-components located in `frontend/components/reader/`:
1. **ReaderHeaderBar (`ReaderHeaderBar.vue`)**: Renders top navigation bar containing library return link, responsive Table of Contents trigger, document title, chapter label, slice progress bar, novel-style typography settings popover (`Aa`), theme toggle, and 1-click active recall quiz button.
2. **ReaderTocSidebar (`ReaderTocSidebar.vue`)**: Renders collapsible left TOC sidebar on desktop (>= 768px) and off-canvas slide-over drawer with backdrop blur on mobile (< 768px), featuring chapter search filter, slice reading time estimates, active slice highlight, and completion status indicators.
3. **ReaderNavigationCards (`ReaderNavigationCards.vue`)**: Renders bottom navigation containing two symmetric glass cards for preceding slice and succeeding slice (or return to library on completion), respecting minimum touch targets and preventing text clipping or wrapping across both English and Vietnamese locales.

#### Scenario: User navigates reading interface on desktop
- **WHEN** user opens `/read/[bookId]` on a desktop screen
- **THEN** the modular header, collapsible TOC sidebar, and bottom navigation cards render coherently with shared typography and dark mode theme state without template duplication.

#### Scenario: User toggles mobile TOC drawer
- **WHEN** user taps contents button on a mobile viewport (< 768px)
- **THEN** `ReaderTocSidebar` renders as an off-canvas drawer over backdrop blur and dismisses upon chapter selection or backdrop touch.

### Requirement: Centered Reading Canvas & High-DPI Visual Density
The reading article body at `/read/[bookId]` SHALL render inside a centered container (`#main`) constrained by the active reading width preset (`max-w-3xl` for standard, `max-w-4xl` for wide, `max-w-full` for full width) with symmetric gutters (`px-4 sm:px-8 md:px-12`):
1. **Symmetric Margin Alignment**: On wide screens and high-DPI displays (including Windows 11 1080p/2K viewports), reading prose SHALL remain centered without left-biased drift or uneven whitespace when the TOC sidebar collapses or expands.
2. **Typography Scaling Integrity**: Paragraphs, lists, blockquotes, and headings within the reading prose container SHALL reactively scale according to the active `useReaderTypography` scale factor without overflowing or clipping container boundaries.

#### Scenario: User toggles wide reading width
- **WHEN** user selects "Wide" reading width in the typography settings popover
- **THEN** the `#main` reading article container expands smoothly to `max-w-4xl mx-auto` while preserving symmetric horizontal padding.
