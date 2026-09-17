## MODIFIED Requirements

### Requirement: Dedicated Reading Route
The system SHALL provide a dedicated reader page at `/read/[bookId]` with query parameter `?slice={chunkOrder}`, navigating from book cards in `/library`. The reading route SHALL render in a standalone distraction-free immersion mode that hides the global application header and global navigation sidebar, providing an integrated reader header containing back navigation, slice progress, chapter drawer toggle, novel-style typography settings toggle (`Aa`), theme toggle, and 1-click quiz launcher.

#### Scenario: User opens a book from the library
- **GIVEN** an authenticated or guest user browsing `/library`
- **WHEN** user clicks "Read Book" on a book card in `/library`
- **THEN** application navigates to `/read/[bookId]` loading the first slice or bookmarked slice without global application chrome.

#### Scenario: Mobile viewport reader header layout
- **GIVEN** user is reading on a mobile screen width (<768px)
- **WHEN** user views `/read/[bookId]`
- **THEN** reader header displays a single compact top bar with back arrow, truncated book title with slice indicator, table of contents button, typography toggle (`Aa`), and theme toggle without vertical header duplication.

---

## ADDED Requirements

### Requirement: Novel-Style Reader Typography Controls and Layout Customization
The reader page (`/read/[bookId]`) SHALL provide a comprehensive typography customization popover accessible via an `Aa` action button in the reader top navigation bar immediately adjacent to the `ThemeToggle` component. The typography system SHALL allow readers to adjust font size, font family, line spacing, and reading column width, immediately applying changes to the reading pane and persisting preferences in browser storage.

The typography engine SHALL support the following configuration dimensions:
1. **Font Size Scaling:**
   - Five distinct size scale presets: `sm` (14px), `base` (16px, default), `lg` (18px), `xl` (20px), and `2xl` (22px).
   - Stepped decrement (`A-`) and increment (`A+`) buttons with a visual indicator showing the active scale step.
2. **Font Family Selection:**
   - **Sans-Serif (Default):** Modern UI sans-serif stack (`Inter`, `system-ui`, `-apple-system`, `sans-serif`).
   - **Serif (Novel Standard):** High-readability editorial serif stack (`Merriweather`, `Georgia`, `Lora`, `serif`).
   - **Monospace (Technical):** Fixed-width programming stack (`JetBrains Mono`, `ui-monospace`, `monospace`).
3. **Line Spacing (Leading):**
   - **Standard:** `leading-normal` (line height 1.5).
   - **Relaxed (Default):** `leading-relaxed` (line height 1.625).
   - **Loose:** `leading-loose` (line height 2.0).
4. **Reading Column Width:**
   - **Standard (Default):** `max-w-3xl` (~768px, optimal for focused novel reading).
   - **Wide:** `max-w-4xl` (~896px, optimal for multi-column code comparisons).
   - **Full:** `max-w-full` (fluid edge-to-edge reading container).
5. **Preference Persistence:**
   - Settings SHALL automatically save to `localStorage` under key `techdaily_reader_typography` as a serialized JSON object.
   - Upon mounting `/read/[bookId]`, the reader SHALL hydrate typography state from `localStorage`, gracefully falling back to defaults (`{ fontSize: 'base', fontFamily: 'sans', lineSpacing: 'relaxed', readingWidth: 'standard' }`) if no saved preferences exist.
6. **Paragraph Formatting & Rhythm:**
   - Prose text inside the reader container SHALL render cleanly separated `<p>` tags with vertical margins (`my-4` to `my-5`) rather than merging into a continuous wall of text.

#### Scenario: User opens typography controls popover from reader topbar
- **GIVEN** an active reading session on `/read/[bookId]`
- **WHEN** user clicks the `Aa` button in the top navigation bar
- **THEN** a floating typography popover appears directly beneath the button
- **AND** the popover displays controls for Font Size (`A-` / `A+`), Font Family (Sans, Serif, Mono), Line Spacing (Standard, Relaxed, Loose), and Reading Width (Standard, Wide, Full).

#### Scenario: User adjusts font size across 5 scale presets
- **GIVEN** the typography popover is open with default font size `base` (16px)
- **WHEN** user clicks the `A+` button
- **THEN** font size increases to `lg` (18px)
- **AND** the reader article prose updates reactively in real time
- **WHEN** user clicks `A+` again until reaching `2xl` (22px)
- **THEN** the `A+` button becomes visually disabled at the maximum scale boundary.

#### Scenario: User switches font family between Sans, Serif, and Monospace
- **GIVEN** the reader is displaying prose in the default Sans-Serif font family
- **WHEN** user selects the "Serif" font family button in the typography popover
- **THEN** the reader article element dynamically applies the serif font family stack
- **AND** user observes high-readability literary serif typography suitable for book reading.

#### Scenario: User adjusts line spacing between Standard, Relaxed, and Loose
- **GIVEN** the typography popover is open
- **WHEN** user selects "Loose" line spacing
- **THEN** the article element switches class to `leading-loose`
- **AND** vertical spacing between text lines increases to 2.0em for relaxed reading.

#### Scenario: User adjusts reading width between Standard, Wide, and Full
- **GIVEN** the reader view is displayed on a wide desktop screen ($\ge 1280\text{px}$)
- **WHEN** user selects "Wide" reading width in the typography popover
- **THEN** the article container width expands from `max-w-3xl` to `max-w-4xl`
- **WHEN** user selects "Full" reading width
- **THEN** the article container width expands to `max-w-full`.

#### Scenario: Typography preferences persist across sessions and books via LocalStorage
- **GIVEN** a user configures font size `lg`, font family `serif`, line spacing `loose`, and reading width `wide`
- **WHEN** the user navigates away to `/library` or reloads the browser tab
- **THEN** `localStorage.getItem('techdaily_reader_typography')` contains the updated preferences
- **AND** opening any book at `/read/[bookId]` automatically hydrates and applies the saved typography settings.

#### Scenario: Paragraph breaks render cleanly as distinct HTML <p> tags with vertical margins
- **GIVEN** a book slice containing multiple paragraphs separated by double newlines (`\n\n`)
- **WHEN** `useMarkdownRenderer` renders the content inside `/read/[bookId]`
- **THEN** each paragraph is contained within an individual `<p>` element
- **AND** paragraphs are separated by distinct vertical margins without run-on text collapse.
