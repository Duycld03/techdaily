# Proposal: Unified Notes and Flashcard Management

## Why

TechDaily aims to provide senior software engineers with an immersive technical reading and retention system. However, user feedback and workflow analysis have revealed three critical friction points across reading, annotation, and knowledge retention:

1. **Toolbar Clutter and Disrupted Reading Immersion in the Reader (`/read/[bookId]`):**
   - The reader's text selection floating toolbar currently presents five separate buttons: `Explain with Gemini`, `Highlight`, `Flashcard`, `Add Note`, and `Copy`.
   - The presence of a 1-click `⚡ Flashcard` button directly in the selection toolbar encourages premature card generation while engineers are still comprehending the text, disrupting deep technical reading flow.
   - Highlighting and note-taking are artificially bifurcated into two separate actions (`Highlight` and `Add Note`), forcing users to perform repetitive gestures or leaving highlights disconnected from personal reflections.

2. **Feature Duplication and Editing Gaps in the Notes Hub (`/notes`):**
   - The `/notes` view currently contains a redundant "Saved Insights" tab, duplicating the bookmarks management already provided on the `/insights` page via its `[🔖 Đã lưu]` filter tab.
   - Once a reading highlight is saved, users cannot edit its personal reflection note or update its technical tags—there is no backend update API (`PUT /api/v1/notes/highlights/{id}`) or frontend inline editor. Engineers must delete and recreate annotations from scratch if their thoughts evolve.

3. **Incomplete Flashcard Lifecycle and Lack of Deck Management (`/review`):**
   - The `/review` route is strictly limited to an interactive daily review player (`/deck`), showing only cards due today.
   - Users have no ability to inspect their full flashcard library (Learning, Reviewing, Mastered), search cards by technical keywords, filter by source (Topic, Highlight, QuizMistake), edit markdown content (fix typos or clarify prompts), reset SM-2 progression for forgotten concepts, or delete obsolete cards.

Solving these issues now will consolidate TechDaily's knowledge pipeline—moving seamlessly from focused reading to reflective note curation, and ultimately to deliberate spaced repetition mastery.

---

## What Changes

We propose a unified, three-pillar overhaul of annotation, notes, and flashcard management:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      UNIFIED KNOWLEDGE PIPELINE                             │
│                                                                             │
│  Pillar 1: Streamlined Reader Floating Toolbar (/read/[bookId])             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ [✨ Explain with Gemini]   [🖍️ Highlight / Note]   [📋 Copy]           │  │
│  └───────────────────────────────────┬───────────────────────────────────┘  │
│                                      │ Click creates highlight & opens      │
│                                      ▼ reflection popover optionally        │
│                                                                             │
│  Pillar 2: Dedicated Reading Notes Hub (/notes)                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ • Prune redundant "Saved Insights" tab (exclusively managed in        │  │
│  │   /insights [🔖 Đã lưu])                                              │  │
│  │ • Add PUT /api/v1/notes/highlights/{id} (UpdateHighlightHandler)      │  │
│  │ • Inline reflection & tag editor on saved highlight cards             │  │
│  │ • Retain deliberate "⚡ Flashcard SM-2" generation on note cards       │  │
│  └───────────────────────────────────┬───────────────────────────────────┘  │
│                                      │ Deliberate flashcard creation        │
│                                      ▼                                      │
│                                                                             │
│  Pillar 3: Comprehensive Flashcard Deck Management (/review)                │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ Dual-Mode Tab Switcher:                                               │  │
│  │ ├─ Tab 1: Review Session / Ôn tập hôm nay (Interactive SM-2 player)   │  │
│  │ └─ Tab 2: Deck Management / Kho thẻ của tôi                           │  │
│  │    • Deck statistics (Total, Learning, Reviewing, Mastered)           │  │
│  │    • GET /api/v1/review/cards (pagination, search, status/source)     │  │
│  │    • PUT /api/v1/review/cards/{id} (edit Front/Back Markdown)         │  │
│  │    • DELETE /api/v1/review/cards/{id} (soft-delete BaseEntity)        │  │
│  │    • POST /api/v1/review/cards/{id}/reset (SM-2 progression reset)    │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Pillar 1: Streamlined Reader Floating Toolbar (`/read/[bookId]`)
- **Reduce actions to 3 essential buttons:** `Explain with Gemini`, `Highlight/Note`, and `Copy`.
- **Remove the direct Flashcard button** (`⚡ Flashcard`) from the floating text selection tooltip to protect reading immersion and avoid premature card generation.
- **Unify Highlight and Note:** Clicking `Highlight/Note` immediately saves the highlight quote to the database, and smoothly presents an attached reflection popover where the user can optionally type personal notes and technical tags before closing.

### Pillar 2: Dedicated Reading Notes Hub (`/notes`)
- **Remove the redundant "Saved Insights" tab** from `/notes`. Insight bookmarks remain cleanly segregated and fully managed under `/insights` via the `[🔖 Đã lưu]` tab.
- **Implement backend API `PUT /api/v1/notes/highlights/{id}`:** Managed by `UpdateHighlightHandler` in `TechDaily.Application.Features.Notes`, allowing users to update `Note` and `Tags` on existing highlights.
- **Add inline reflection editing:** Provide an intuitive inline edit mode directly on highlight cards in `/notes` with Markdown preview, saving edits seamlessly without page reload.
- **Retain the deliberate "Flashcard SM-2" creation path:** Keep the card generation button on highlight cards in `/notes` as the primary, intentional avenue for converting curated notes into spaced repetition flashcards.

### Pillar 3: Comprehensive Flashcard Deck Management (`/review`)
- **Dual-mode navigation:** Introduce a top-level tab switcher on `/review`:
  - **Tab 1 ("Review Session" / "Ôn tập hôm nay"):** Existing 3D interactive flip-card study player for cards due today, including SM-2 grading (Again, Hard, Good, Easy) and completion celebrations.
  - **Tab 2 ("Deck Management" / "Kho thẻ của tôi"):** Full-featured card library interface.
- **Deck statistics overview:** Real-time counter metrics displaying Total Cards, Learning, Reviewing, and Mastered.
- **Backend Deck Management APIs:**
  - `GET /api/v1/review/cards`: Paginated query supporting search keywords, `Status` filter (`Learning`, `Reviewing`, `Mastered`), and `SourceType` filter (`Topic`, `Highlight`, `QuizMistake`).
  - `PUT /api/v1/review/cards/{id}`: Update `FrontMarkdown` and `BackMarkdown` for a card with FluentValidation.
  - `DELETE /api/v1/review/cards/{id}`: Soft-delete card (`card.SoftDelete()`).
  - `POST /api/v1/review/cards/{id}/reset`: Reset SM-2 metrics ($Repetition=0, Interval=1, EaseFactor=2.50, Status=Learning, NextReviewDate=Today$).
- **Deck Management UI components:**
  - Search input with debounced execution and filter chips.
  - Responsive card list / table with SM-2 badges (Status, Interval, Ease Factor, Next Review Date).
  - Edit modal with live split Markdown preview.
  - Delete confirmation modal.
  - Reset progression confirmation modal.

---

## Capabilities

### New Capabilities
- `notes`: Dedicated reading notes hub providing highlight curation, tag filtering, inline reflection editing, and intentional SM-2 flashcard generation.
- `review`: Comprehensive spaced repetition deck management offering full library querying, card content editing, SM-2 progression reset, soft-deletion, and interactive 3D flip-card review sessions.

### Modified Capabilities
- `reader`: Streamlined 3-button floating selection toolbar (Explain with Gemini, Highlight/Note, Copy) with unified highlight-and-reflection interaction, removing the distracting direct flashcard creation button.

---

## Value & Impact

| Layer | Changes & Enhancements | Concrete Impact |
|---|---|---|
| **Reader UX** | 3-button toolbar; unified highlight + popover; no direct flashcard button. | Zero distraction during deep technical reading; seamless capture of excerpts and reflections. |
| **Notes Hub** | Prune Saved Insights tab; add inline reflection & tag editor; retain SM-2 conversion. | Eliminates cross-page duplication with `/insights`; enables continuous note refinement. |
| **Review Deck** | Dual-tab UI; statistics; search & filters; edit modal; SM-2 reset; soft deletion. | Transforms `/review` from a black-box queue into an empowering, user-controlled spaced repetition system. |
| **Backend API** | `PUT /api/v1/notes/highlights/{id}`, `GET/PUT/DELETE/POST /api/v1/review/cards*`. | Robust, CQRS-aligned endpoints with FluentValidation and EF Core soft-delete compliance. |
| **Localization** | 100% complete English and Vietnamese translations in `en.json` and `vi.json`. | Consistent multi-language experience with clean localized terminology (no hardcoded English in parentheses). |

---

## Risks & Mitigations

1. **Highlight Deletion Cascade Risk:**
   - *Risk:* Deleting a highlight might accidentally cascade and delete or corrupt an associated flashcard.
   - *Mitigation:* The database foreign key `FK_SpacedRepetitionCards_UserHighlights_SourceHighlightId` is configured with `onDelete: ReferentialAction.SetNull`. Furthermore, flashcards store their own independent `FrontMarkdown` and `BackMarkdown` snapshots, ensuring zero data loss upon highlight deletion.

2. **Soft Delete Inconsistencies:**
   - *Risk:* Deleting cards or highlights might leave orphaned records or violate global query filters.
   - *Mitigation:* Both `UserHighlight` and `SpacedRepetitionCard` inherit from `BaseEntity`, utilizing `SoftDelete()` (`IsDeleted = true`). EF Core global query filters automatically exclude soft-deleted entities from all queries.

3. **Performance of Paginated Deck Queries:**
   - *Risk:* Querying flashcard decks with full-text search across `FrontMarkdown` and `BackMarkdown` could cause slow queries as decks grow.
   - *Mitigation:* Scoped queries filter strictly by `UserId` (indexed in `IX_SpacedRepetitionCards_UserId_NextReviewDate`), utilize `AsNoTracking()`, and apply indexed status/source type filters before projecting to DTOs.
