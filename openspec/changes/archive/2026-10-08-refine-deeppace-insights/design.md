# Design

## Context

Following the pivot to **DeepPace**, the `/insights` page exhibits visual glitches and conceptual drift:
- In `GetInsightsMetaHandler.cs`, suggested topics are extracted from all `DocumentChunks` in the library without filtering or limit, resulting in 40+ raw chapter titles (`CHƯƠNG X:...`) being sent to the client.
- In `frontend/pages/insights.vue`, the AI generation modal lacks vertical height constraints (`max-h-[85dvh]`, `overflow-y-auto`), causing the entire dialog to stretch into an uncontained vertical column across the viewport.
- Empty state containers in `insights.vue` omit the icon components inside the `w-12 h-12` wrapper div, rendering an empty purple box.
- Category badges in `getCategoryBadge` use hardcoded English strings and lack cases for categories 4, 5, and 6, defaulting to `Core Architecture`.
- The database seed file `tech-insights.json` has 0 cards for `EngineeringCraft`, `MentalModels`, and `HabitsProductivity`.
- Modal placeholder and copy are statically hardcoded to backend framework internals regardless of the selected domain.

## Goals / Non-Goals

**Goals:**
- Cap suggested topic extraction to 8 curated items per category in `GetInsightsMetaHandler` and strip raw chapter numbering noise.
- Ensure the AI generation modal stays strictly within `max-h-[85dvh]` with interior scrolling.
- Restore visible Lucide icons in both Explore and Saved empty states.
- Localize category badges dynamically according to the active i18n locale and category metadata.
- Sanitize hashtag formatting by trimming whitespace (`#.NET` instead of `# .NET`).
- Provide production-grade seed insights for categories 4, 5, and 6 in `tech-insights.json`.
- Adapt modal descriptions and input placeholders dynamically based on the selected category.

**Non-Goals:**
- Schema changes to the PostgreSQL `TechInsights` or `DocumentChunks` tables.
- Reworking the underlying Gemini prompt structure or LLM model endpoint.
- Changing bookmarking and note integration behavior.

## Decisions

### 1. Topic Extraction & Noise Filtering in Backend
In `GetInsightsMetaHandler.cs`:
- Strip noise patterns from chapter titles: `Regex.Replace(title, @"^(Chương\s+\d+[:\-\s]*|Chapter\s+\d+[:\-\s]*|Tóm tắt\s+chương.*|\(Section\s+\d+\))", "", RegexOptions.IgnoreCase).Trim()`.
- Filter out empty or uninformative titles (length < 3).
- Apply `.Take(8)` per category to prevent payload bloat.
- When fewer than 2 topics exist, fall back to the enriched `DefaultTopics` dictionary.

### 2. Rich Seed Insights for Software Craft & Cognitive Models
Add 6 initial seed entries to `backend/src/TechDaily.Infrastructure/Data/tech-insights.json`:
- **Category 4 (`EngineeringCraft`)**:
  1. *Refactoring God Classes with the Specification & Strategy Patterns*: Decoupling monolithic business rules into composable, testable unit invariants.
  2. *Defensive Invariant Validation over Null-Forgiving Operators*: Replacing permissive null coalescing with fail-fast domain boundary guards.
- **Category 5 (`MentalModels`)**:
  1. *Charlie Munger's Inversion Principle (Pre-Mortem)*: Designing resilient architectures and habits by mapping failure modes first.
  2. *First Principles Decomposition*: Breaking down legacy monolithic assumptions into fundamental truths.
- **Category 6 (`HabitsProductivity`)**:
  1. *Ultradian 90-Minute Focus Cycles & Context Switching Elimination*: Aligning high-leverage engineering bursts with biological energy curves.
  2. *Dopamine Prediction Error & Habit Cue Inversion*: Systematic habit loop restructuring for sustainable daily practice.

### 3. Responsive Modal Geometry & Dynamic Copy in Frontend
In `frontend/pages/insights.vue`:
- Modal panel: Apply `max-h-[85dvh] flex flex-col overflow-hidden` to the `.glass-panel` container.
- Content body: Make the middle form area scrollable with `overflow-y-auto pr-1`.
- Topic inspiration chips container: Constrain to `max-h-36 overflow-y-auto flex flex-wrap gap-1.5`.
- Dynamic Copy Computed:
  - When `selectedCategory === 4`: Placeholder `"Ví dụ: Refactoring God Class, Specification Pattern, TDD Invariants..."`
  - When `selectedCategory === 5`: Placeholder `"Ví dụ: First Principles, Charlie Munger Inversion, Second-Order Thinking..."`
  - When `selectedCategory === 6`: Placeholder `"Ví dụ: Deep Work 90m blocks, Dopamine Loop, Thói quen nguyên tử..."`
  - When `selectedCategory in [0, 1, 2, 3]`: Placeholder `"Ví dụ: Kestrel Pipeline, ArrayPool, EF Core Internals, Postgres B-Tree..."`
  - Fallback / All categories: Generalized deliberate practice placeholder.

### 4. Localized Category Badges & Tag Sanitization
- Refactor `getCategoryBadge(cat)` to dynamically look up the localized label from `computedCategories` matching `cat`, with domain-specific color palettes (sky for frontend, purple for backend, blue for DB, indigo for system, amber for craft, emerald for mental models, rose for habits).
- Replace `#{{ tag }}` with `#{{ String(tag).trim().replace(/^#+/, '') }}` to eliminate leading whitespace and duplicate hash characters.

### 5. Empty State Icons
- Inside Explore empty state (`div.w-12.h-12`), mount `<Lightbulb class="w-6 h-6 text-brand-500" />`.
- Inside Saved empty state (`div.w-12.h-12`), mount `<BookmarkCheck class="w-6 h-6 text-brand-500" />`.

## Risks / Trade-offs

- **Existing Database State**: `TechInsightsSeeder` matches on `Slug`. New entries with unique slugs will be added without overwriting existing user bookmarks.
- **User Library Variation**: If a user uploads a PDF with unusual chapter formatting, the regex cleaner strips known markers while preserving the chapter subject; `.Take(8)` guarantees an upper bound regardless of title length.
