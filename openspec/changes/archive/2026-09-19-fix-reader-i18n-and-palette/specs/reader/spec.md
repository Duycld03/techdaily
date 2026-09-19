# Spec Delta: Reader

## MODIFIED Requirements

### Requirement: Sanitized Markdown Rendering and Code Block Copying
The reader SHALL sanitize markdown rendering by suppressing duplicate first-line headings that match the active slice chapter title, suppressing redundant trailing Key Takeaways sections from the markdown body when structured takeaways are present, formatting inline code (`code:not(pre code)`) with Dev-Learning Studio tokens (neutral pill in light mode with `bg-slate-100 text-slate-800 border-slate-200/90`, and Obsidian pill in dark mode with `dark:bg-canvas-elevated dark:text-brand-300 dark:border-white/[0.08] font-medium`), formatting markdown links with system primary brand tokens (`prose-a:text-brand-600 dark:prose-a:text-brand-400 hover:prose-a:underline`), parsing technical alert callouts (`NOTE`, `TIP`, `IMPORTANT`, `WARNING`, `CAUTION`), and providing reliable 1-click syntax-highlighted code block copying with localized confirmation feedback.

#### Scenario: Accurate slice badge display
- **WHEN** reading any document slice
- **THEN** the badge above the title displays "Slice {current} of {total}" in English and "Lát cắt {current} / {total}" in Vietnamese instead of day drill labels
- **AND** the badge renders via valid Vue template interpolation without outputting unparsed JavaScript expressions or raw `$t(...)` code text.

#### Scenario: Key Takeaways deduplication in article body
- **WHEN** the slice contains structured key takeaways (`hasValidTakeaways` is true)
- **THEN** the markdown renderer suppresses any trailing `### Key Takeaways` or `## Key Takeaways` heading and bullet list from the article markdown body
- **AND** key takeaways render exclusively inside the dedicated callout card below the article body, eliminating duplicate content and avoiding untranslated English headings in the Vietnamese locale.

#### Scenario: Key Takeaways callout card brand palette
- **WHEN** the Key Takeaways callout card renders below the reading article body
- **THEN** it renders with Dev-Learning Studio system primary brand violet tokens (`bg-brand-50/50 dark:bg-brand-500/10`, `border-brand-200/80 dark:border-brand-500/20`, `text-brand-900 dark:text-brand-300`, `Sparkles text-brand-600 dark:text-brand-400`, bullet dots `bg-brand-500`)
- **AND** no warning amber colors (`amber-50`, `amber-950`, `amber-200`, `amber-500`) are applied to the key takeaways card.

---

### Requirement: Dedicated Reading Route
The system SHALL provide a dedicated reader page at `/read/[bookId]` with query parameter `?slice={chunkOrder}`, navigating from book cards in `/library`. The reading route SHALL render in a standalone distraction-free immersion mode that hides the global application header and global navigation sidebar, providing an integrated reader header containing back navigation, slice progress, chapter drawer toggle, novel-style typography settings toggle (`Aa`), theme toggle, and 1-click quiz launcher. The reader page shell, sticky header, popovers, and backdrop elements SHALL render with Dev-Learning Studio obsidian canvas tokens (`dark:bg-canvas`, `dark:bg-canvas-subtle`, `dark:bg-canvas-elevated`) and hairline translucent borders (`border-slate-200/80 dark:border-white/[0.08]`). All interactive controls, typography adjusters, navigation hints, and loading indicators SHALL be fully localized through i18n message catalogs in both English and Vietnamese.

#### Scenario: Reader interface 100% localization coverage
- **WHEN** user views the dedicated reader route in any supported locale (English or Vietnamese)
- **THEN** all typography controls (font families "Sans", "Serif", "Mono", size adjusters "Smaller Font" / "Larger Font"), slice navigation hints ("Previous Slice (Shift + ←)" / "Next Slice (Shift + →)"), Table of Contents close buttons, and chapter loading spinners render with localized text from the i18n message catalog without raw English literals.
