# Design

## Context

See `proposal.md` for motivation.
The platform's underlying engine—composed of document chunking, sliding look-ahead pre-generation, SuperMemo SM-2 spaced repetition, Google Cloud TTS audio narration, and knowledge graphing—is fundamentally agnostic to domain content. However, the surface lexicon, domain enums, AI system prompts, and UI components are coupled to developer interview preparation.

This design details how to cleanly pivot the system to **DeepPace** ("Deep Learning at a Sustainable Daily Pace") while preserving 100% of the technical code insights, benchmark metrics, and existing architecture.

## Goals / Non-Goals

**Goals:**
- Rebrand the platform from TechDaily to DeepPace across all user surfaces, metadata, and design tokens.
- Replace interview-only jargon with universal deliberate practice terminology (Decision Drills, Reflex Practice, Mastery Depth Stages).
- Retain the high-value code insight generator (`problemSnippet`, `solutionSnippet`, `underTheHoodMarkdown`, `benchmarkStats`) while extending the prompt engine to support cognitive models and behavioral habits.
- Maintain full database backward compatibility without destructive schema migrations.

**Non-Goals:**
- Foreign language learning features (explicitly deferred to preserve product focus).
- Renaming low-level database table names (e.g. `InterviewQuestions`, `QuizQuestions` tables remain stable internally to prevent destructive data migration).

## Decisions

### 1. Zero-Downtime Semantic Enum Mapping
Instead of executing destructive database column migrations, domain enums maintain their existing integer ordinal values while updating semantic display names and DTO mappings:
- `QuizLevel`:
  - `0` $\rightarrow$ **Foundation** (formerly `Fresher / Entry`): Core concepts and definitions.
  - `1` $\rightarrow$ **Applied** (formerly `Junior`): Routine practical application and common edge cases.
  - `2` $\rightarrow$ **Advanced** (formerly `Middle`): Complex integration, trade-offs, and multi-component systems.
  - `3` $\rightarrow$ **Mastery** (formerly `Senior`): High-leverage architecture, under-the-hood mechanics, and systemic trade-offs.
- `Category`:
  - Retain `0` (FrontendWeb), `1` (BackendRuntime), `2` (DatabaseStorage), `3` (SystemDesign), `4` (EngineeringCraft).
  - Treat Category `4` as the overarching **Craftsmanship & Cognitive Systems** bridge, and introduce `5` (**MentalModels & DecisionMaking**) and `6` (**Habits & Productivity**) without breaking historical foreign keys.

*Alternatives considered:*
- Dropping and recreating tables: Rejected as unnecessarily destructive.
- Renaming enum member identifiers: Safe with aliasing and JSON string converters.

### 2. Adaptive Dual-Mode Prompt Engine in `GeminiAiService`
The prompt engine adapts based on the active topic's domain:
- **When Technical / Software Engineering**: Gemini generates concrete code snippets (bad pattern vs optimal pattern), runtime mechanics (CLR, V8, GC, IOCP), and benchmark metrics (`⚡ 10x throughput | 0 B allocated`).
- **When Human Craft / Mindset / Habits**: Gemini generates behavioral patterns (naive habit trap vs master focus routine), cognitive neuroscience mechanics (Prefrontal Cortex, dopamine loops, cognitive load), and impact metrics (`⚡ 2 hours saved/day | 0 distraction`).

*Alternatives considered:*
- Creating two separate AI services: Rejected; a single unified service handling domain-aware lenses avoids code duplication.

### 3. Frontend Localization & Surface Lexicon Cutover
Update `frontend/locales/en.json` and `frontend/locales/vi.json` to systematically replace interview preparation terms:
- "Senior Scenario Challenge" $\rightarrow$ **Decision Drill** (*Thử thách tình huống & ra quyết định*).
- "Interview Quiz Arena" $\rightarrow$ **Reflex Practice Arena** (*Phòng luyện phản xạ tư duy*).
- "Executive Cockpit" $\rightarrow$ **Daily Practice Workspace** (*Không gian rèn luyện mỗi ngày*).
- "Mistake Review Queue" $\rightarrow$ **Blindspot Remediation Deck** (*Sổ tay khắc phục điểm mù*).

## Risks / Trade-offs

- **[Risk]** Existing user bookmarks and quiz attempts might reference legacy topic titles.
  $\rightarrow$ **Mitigation:** DTO mappers gracefully fall back to default taxonomy labels without throwing null references.
- **[Risk]** Users accustomed to pure coding challenges might miss the explicit developer focus.
  $\rightarrow$ **Mitigation:** Retain Software Craftsmanship as a dedicated first-class pillar with rich code blocks and benchmark stats.

## Migration Plan

1. Update domain enums, DTOs, and localization strings.
2. Update `GeminiAiService` system prompts to support both Software Craftsmanship and Mental Craft.
3. Update frontend studio headers, logos, and page titles from TechDaily to DeepPace.
4. Verify end-to-end with unit test suite (`dotnet test` and `npm test`).
