# Proposal: Practice Studio & Core Assessment Modernization

## Why

While the main application dashboard, navigation shell, and knowledge graph have been updated to the engineering-grade Dev-Learning Studio theme (Obsidian Base `#09090b` + Deep Iris `#7c3aed`), the core practice and assessment workflows—Interview Quiz (`/quiz`), Spaced Repetition Flashcards (`/review`), Authentication (`/login`), and System Error Handling (`error.vue`)—still rely on legacy styles. They contain opaque slate containers (`dark:bg-slate-900/60`), outdated emerald accents (`bg-emerald-600`), and harsh borders (`border-slate-800`).

Modernizing all internal tabs, sub-views, and modals within these assessment routes ensures complete visual consistency, improves legibility during intense interview prep and flashcard review, and replaces legacy green/slate artifacts with clean dark glass and hairline borders.

## What Changes

- **Interview Quiz Studio (`/quiz`) Across All 5 Tabs**:
  - **Generate Tab**: Modernize topic selection input, book-grounded toggle, question count pills, and difficulty tier cards with `.glass-card` and hairline borders.
  - **Arena Tab**: Re-skin question stepper, active question text container, multiple-choice options (A, B, C, D) with clean hover and selected states, and the explanation markdown container.
  - **Summary Tab**: Upgrade score badges, performance cards, and the review question list.
  - **Review Queue Tab**: Modernize mistake flashcards, question explanations, and pagination controls.
  - **Stats Tab**: Transform the 4-card assessment Bento dashboard into dark glass surfaces with refined readiness badges.
- **Spaced Repetition Review Studio (`/review`) Across All Tabs & Modals**:
  - **Session Tab**: Modernize interactive SM-2 flip cards, ease rating buttons (Again, Hard, Good, Easy), and completion celebration card.
  - **Management Tab**: Elevate Bento overview cards, search & filter toolbar, and deck card lists with `.glass-card`.
  - **Modals**: Modernize Edit Card Modal (including Markdown edit/preview sub-tabs), Reset Progress Modal, and Delete Card Modal with `.glass-panel` and hairline borders.
- **Global Error Experience (`error.vue`)**:
  - Purge legacy Emerald styling (`bg-emerald-600`, `text-emerald-400`, `dark:bg-slate-950`).
  - Adopt neutral obsidian canvas, `.glass-panel` elevation, and Deep Iris Violet CTAs.
- **Authentication Studio (`/login`) Across Both Modes**:
  - Re-skin Login and Register modes with clean glass paneling, subtle inputs, and Deep Iris Violet submit buttons, removing legacy emerald gradients.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `quiz`: Modernize Interview Quiz interface across all 5 internal tabs with Dev-Learning Studio tokens.
- `review`: Modernize Spaced Repetition Review interface across session, deck management, and modals.
- `auth`: Modernize Authentication and Register forms with obsidian glass aesthetics.
