# Reader Capability Delta Specification

## MODIFIED Requirements

### Requirement: Responsive Term Explainer Modal Layout
The `TermExplainerModal` SHALL provide a responsive, overflow-resistant header layout that cleanly displays category metadata, term title, instant cache indicators, and modal dismiss controls across all screen sizes without text wrapping or button collision.

#### Scenario: Long book category title with cache hit
- **WHEN** user opens the term explainer modal for a term whose category/book title exceeds 30 characters and the term was resolved from the cache (`isFromCache == true`)
- **THEN** the category text is truncated cleanly with an ellipsis (`truncate max-w-[180px] sm:max-w-xs`), the `⚡ Instant Cache` badge maintains a single line with `whitespace-nowrap shrink-0`, and the modal close button remains unclipped and interactive at `shrink-0`.

#### Scenario: Mobile viewport header containment
- **WHEN** user views the term explainer modal on a mobile device (<640px viewport width)
- **THEN** the header elements respect flex boundaries (`min-w-0 flex-1`), preventing horizontal scroll or content leaking outside the modal container.

---

### Requirement: Comprehensive i18n Localization in Reader Term Explainer
All copy inside `TermExplainerModal` SHALL be fully localized through the platform's internationalization framework (`useI18n`), dynamically rendering translated labels for cache status, loading animations, branding footers, and copy-to-clipboard interactions based on the active locale (`en` or `vi`).

#### Scenario: Explainer modal rendered in Vietnamese locale
- **WHEN** a user with locale set to Vietnamese (`vi`) opens the term explainer modal
- **THEN** the cache badge displays "⚡ Bộ nhớ tức thì", the loading state displays "Đang phân tích thuật ngữ với Google Gemini...", the footer displays "Được hỗ trợ bởi Google Gemini", and the action button displays "Sao chép giải thích" (transitioning to "Đã sao chép" upon click).

#### Scenario: Explainer modal rendered in English locale
- **WHEN** a user with locale set to English (`en`) opens the term explainer modal
- **THEN** all modal copy displays corresponding English translations without missing key warnings or raw localization fallback keys.

---

### Requirement: Surrounding Document Context Extraction for In-Reader Term Explanations
When invoking the AI term explainer from an active text selection in `/read/[bookId]`, the reader SHALL extract the surrounding document context from the enclosing DOM element (up to 500 characters enclosing the selected term) and pass this text to `currentContext`, falling back to the chapter title only when surrounding DOM text is unavailable.

#### Scenario: User selects a term inside a narrative paragraph
- **WHEN** user selects a technical term (such as "Write-Ahead Log") within a paragraph in the reader pane
- **THEN** the reader captures up to 500 characters of the surrounding paragraph text encompassing the selection and sends it as the `context` parameter to the `/explain-term` API request.

#### Scenario: Context extraction within dense code or list elements
- **WHEN** user selects a term within an inline code block or list item
- **THEN** the reader traverses to the nearest parent block container (`p`, `li`, `blockquote`, `div`), retrieves up to 500 characters of surrounding text, and supplies it as contextual grounding.

#### Scenario: Fallback when selection container text is inaccessible
- **WHEN** the selected text DOM node cannot be resolved or contains no additional textual context
- **THEN** the reader gracefully defaults `currentContext` to the active slice chapter title (`currentChunk.chapterTitle`).
