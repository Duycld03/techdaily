# Proposal

## Why

Following the platform pivot to **DeepPace** (Deep Learning & Deliberate Practice), the `/insights` workspace still retains legacy TechDaily dev-only bias, hardcoded English badges, broken empty states with invisible icons, and an uncontained AI generation modal that expands to full screen height when populated with raw book chunk titles. Furthermore, non-infrastructure categories such as Software Design, Mental Models, and Habits lack initial seed insights, presenting an empty state upon exploration.

This change refines the DeepPace Insights experience by bounding suggested topics, seeding rich craft and cognitive models, making modal controls context-aware across domains, fixing visual glitches, and unifying bilingual localization.

## What Changes

- **Bounded Topic Inspiration (Backend)**: Limit dynamic suggested topics in `GetInsightsMetaHandler` to a maximum of 8 items per category and filter out noisy slice titles (e.g., raw chapter/section markers).
- **Domain Seed Expansion (Backend)**: Add initial production-quality seed insights for `EngineeringCraft` (Clean Code & Design Patterns), `MentalModels` (Pre-Mortem, First Principles), and `HabitsProductivity` (Deep Work 90m Blocks, Ultradian Rhythms) in `tech-insights.json`.
- **Responsive AI Generation Modal (Frontend)**: Constrain modal dialog height with `max-h-[85dvh]` and `overflow-y-auto`, limit suggested chip container height, and prevent viewport stretching.
- **Context-Aware Modal Copy (Frontend & i18n)**: Make the AI generation description and placeholder adapt dynamically based on whether the active category is technical infrastructure, software design, mental models, or productivity habits.
- **Empty State Icon Fix (Frontend)**: Restore missing Lucide icon components inside the empty state containers (`<Lightbulb>` for explore empty state, `<BookmarkCheck>` for saved empty state).
- **Dynamic Localized Badges & Tag Sanitization (Frontend)**: Replace hardcoded English category badge text in `getCategoryBadge` with active i18n category labels, and sanitize tags by trimming whitespace to fix `# .NET` spacing bugs.
- **Category 4 Clarification (Domain & Locales)**: Clarify `EngineeringCraft` label from the overly generic "Kỹ Nghệ Phần Mềm" to "Mã Sạch & Thiết Kế Mã" / "Clean Code & Software Design".

## Capabilities

### Modified Capabilities
- `insights`: Updates requirements for modal responsive density, category-aware input placeholders, bounded topic extraction, and localized craft badges.

## Impact

- **Backend**: `GetInsightsMetaHandler.cs`, `tech-insights.json`, `TechInsightsSeeder.cs`.
- **Frontend**: `pages/insights.vue`, `i18n/locales/en.json`, `i18n/locales/vi.json`.
- **Specs**: `openspec/specs/insights/spec.md`.
