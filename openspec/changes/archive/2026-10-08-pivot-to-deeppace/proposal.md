# Proposal

## Why

TechDaily was initially framed narrowly as an interview preparation app for software engineers. This artificial boundary limited its potential, forcing all micro-learning materials into coding interview jargon even when studying foundational mental models, focus principles, and personal mastery. Furthermore, learners experienced cold-start friction because the platform required uploading custom PDF books without offering curated starter tracks.

By pivoting to **DeepPace** ("Deep Learning at a Sustainable Daily Pace"), the platform transforms into a deliberate practice engine uniting **Mindset & Life Craft** (habits, mental models, focus, decision making) and **Deep Technical Craftsmanship** (preserving code insights, runtime under-the-hood analysis, and performance benchmarks). This opens the product to lifelong learners, engineers, and knowledge workers seeking deliberate daily progress.

## What Changes

- **Rebranding to DeepPace**:
  - Update product name, metadata, layout headers, and branding tokens from TechDaily to DeepPace across frontend and backend configurations.
- **De-Jargonize Developer-Only Lexicon**:
  - Reframe "Interview Questions" and "Senior Scenario Challenges" into universal **"Decision Drills"** (Thử thách tình huống & ra quyết định).
  - Reframe "Interview Quiz" and "Interview Arena" into **"Reflex Practice"** (Luyện phản xạ tư duy).
  - Reframe candidate seniority tiers (`Fresher`, `Junior`, `Middle`, `Senior`) into mastery depth levels (`Foundation`, `Applied`, `Advanced`, `Mastery`).
  - Reframe dashboard headers from "Executive Cockpit" to "Daily Practice Workspace".
- **Preserve and Refine CodeCraft Insights**:
  - Retain 100% of the code snippet generation, compiler/runtime under-the-hood explanations, Shiki syntax highlighting, and performance benchmarks for technical topics.
  - Expand Insight categories and prompt lenses to support both Technical Craft (Performance, Concurrency, Memory, Storage) and Mental Craft (Habits, Cognitive Biases, Deep Work, Stoic Resilience).
- **Curated Starter Tracks (Cold-Start Resolution)**:
  - Introduce pre-seeded foundational reading tracks (e.g. *Atomic Habits*, *Deep Work*, *The Great Mental Models*) so new accounts immediately have high-quality reading slices without hunting for PDFs.

## Capabilities

### Modified Capabilities
- `drills`: Generalize scenario challenges from software interview scenarios to universal decision drills evaluating real-world trade-offs across both technical and behavioral domains.
- `quiz`: Transition terminology and framing from job interview preparation to deliberate recall and reflex practice across four mastery depth stages.
- `insights`: Retain under-the-hood code snippets and benchmark statistics while expanding domain categorization and prompt lenses to support both software craftsmanship and cognitive mental models.
- `today`: Update Daily Focus Studio terminology and layout titles to reflect the DeepPace daily practice paradigm.

## Impact

- **Backend Domain & Enums**: `QuizLevel` naming mapped or aliased to mastery depth stages; `Category` enum enriched with Mindset & Human Craft domains while preserving existing numeric mappings.
- **AI Prompts (`GeminiAiService`)**: System prompts tuned to adopt a Master Mentor persona that evaluates deep trade-offs in either technical software engineering (with code and benchmarks) or human behavioral practice (with cognitive mechanisms), depending on the active topic.
- **Frontend Localization (`i18n`)**: Update English (`en-US`) and Vietnamese (`vi-VN`) translation dictionaries to replace interview/job-hunt terms with deliberate practice, decision drills, and mastery vocabulary.
- **Branding & Layout**: Application title, logos, navbar badges, and meta tags rebranded to DeepPace.
