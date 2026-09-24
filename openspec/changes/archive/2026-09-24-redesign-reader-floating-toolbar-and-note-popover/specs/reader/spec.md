# Spec Delta: reader

## MODIFIED Requirements

### Requirement: Scoped Floating Mini-Toolbar & Active Recall Quiz
The reader floating selection toolbar SHALL render exactly 3 streamlined action buttons: `Explain with Gemini`, `Highlight/Note`, and `Copy`. The direct flashcard creation button SHALL be removed from the reader selection tooltip to protect reading immersion and avoid premature card generation. The `Highlight/Note` action SHALL unify text highlighting and note-taking into a single continuous action: clicking the button immediately creates and persists a highlight record (`POST /api/v1/notes/highlights`), while smoothly opening an attached reflection popover where users can optionally add personal reflection notes and technical tags.

The floating toolbar container and attached note popover SHALL render with **Dev-Learning Studio** elevated obsidian panels (`bg-slate-900/95 dark:bg-canvas-elevated/95 backdrop-blur-xl border border-slate-700/60 dark:border-white/[0.12] rounded-2xl shadow-2xl`) and adhere to the following design system invariants:
1. **Toolbar Button Hierarchy**:
   - `Explain with Gemini`: Styled as a primary Iris Violet accent button (`bg-brand-600 hover:bg-brand-500 text-white font-semibold text-xs shadow rounded-xl`).
   - `Highlight/Note`: Styled with Iris Violet brand-tinted glass (`bg-brand-500/10 text-brand-300 border border-brand-500/20 hover:bg-brand-500/20 hover:text-white rounded-xl`), transitioning to an elevated active state when the note popover is open. Harsh amber/orange button styling (`bg-amber-500`) is strictly prohibited.
   - `Copy`: Styled as a refined neutral translucent glass button (`text-slate-300 hover:text-white hover:bg-white/[0.08] rounded-xl`).
2. **Input Placeholders & Guidance**:
   - The reflection note textarea SHALL display the localized placeholder text (`reader.note_placeholder`) to guide architectural reflection and prevent blank void states.
   - The tag input field SHALL display the localized placeholder text (`reader.tags_placeholder`).
3. **Refined Focus Outlines & Quote Display**:
   - Inputs SHALL render with subtle hairline borders (`border-slate-700/80 dark:border-white/[0.10]`) and soft focus rings (`focus:ring-1 focus:ring-brand-500/40 focus:border-brand-500/60`), eliminating harsh, thick neon borders.
   - Excerpt quotes SHALL display subtle Iris Violet accent borders (`border-l-2 border-brand-500/60 pl-2.5 py-0.5 text-xs text-slate-300 dark:text-slate-300 italic line-clamp-2`).

#### Scenario: User highlights text in reader pane
- **WHEN** user selects text (2 to 500 characters) inside the reader markdown container
- **THEN** floating selection toolbar appears directly above the selection displaying exactly 3 buttons: `Explain with Gemini`, `Highlight/Note`, and `Copy` without a direct flashcard creation button.

#### Scenario: User opens floating note popover on text selection
- **WHEN** user selects text in the reader markdown pane and clicks `Highlight/Note`
- **THEN** an inline popover appears above the selection containing the quote preview, a reflection note textarea, and a tag input field with Save and Close actions.

#### Scenario: User saves a highlight with attached personal reflection
- **WHEN** user types a reflection note and tags into the reflection popover and clicks `Save Note`
- **THEN** client dispatches `PUT /api/v1/notes/highlights/{id}` with updated `note` and `tags`, persists the highlight note in PostgreSQL, displays a localized confirmation toast, and smoothly closes the popover.

#### Scenario: User saves a simple highlight without a note
- **WHEN** user clicks `Highlight/Note` and dismisses the popover without typing a note or adding tags
- **THEN** the system preserves the initially persisted highlight with `note = null` and displays a confirmation toast (`reader.toast_highlight_success`).

#### Scenario: User copies selected text or triggers Gemini explanation
- **WHEN** user clicks `Copy` on the floating toolbar
- **THEN** selected text is copied to clipboard and toolbar closes with a confirmation toast (`reader.toast_copy`)
- **WHEN** user clicks `Explain with Gemini`
- **THEN** floating toolbar closes and opens the Gemini Term Explainer modal populated with selected text and surrounding context.

#### Scenario: Floating toolbar and reflection popover obsidian styling
- **WHEN** the floating selection toolbar or note popover renders
- **THEN** container styles use elevated obsidian tokens (`dark:bg-canvas-elevated/95`, `dark:border-white/[0.12]`)
- **AND** quote preview renders with a primary brand border indicator (`border-brand-500/60`).

#### Scenario: Reflection textarea and tag inputs display localized placeholders
- **WHEN** user opens the reflection note popover from the floating selection toolbar
- **THEN** the reflection textarea displays the placeholder "Viết đúc kết hoặc suy ngẫm kiến trúc của bạn..." in Vietnamese or "Write your reflection or architectural takeaway..." in English
- **AND** the tag input displays "Thẻ phân loại (vd: storage, concurrency)" in Vietnamese or "Tags (e.g. storage, concurrency)" in English
- **AND** focusing the textarea activates a soft Iris Violet focus ring (`focus:ring-1 focus:ring-brand-500/40`) without glaring thick outlines.

#### Scenario: Unified Dev-Learning Studio button hierarchy in floating toolbar
- **WHEN** the floating selection toolbar appears
- **THEN** the `Highlight/Note` button renders using Iris Violet brand glass tokens (`bg-brand-500/10 text-brand-300 border-brand-500/20`)
- **AND** zero amber or orange background styling is rendered.
