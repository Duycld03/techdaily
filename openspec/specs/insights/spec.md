# Insights Specification

## Purpose
Provides an infinite feed of bite-sized architectural tech insights (anti-patterns vs idiomatic solutions), on-demand AI insight generation via Gemini, and one-click bookmarking to personal notes.

## Requirements

### Requirement: Tech Insights Feed Data Model & Query API
The system SHALL maintain a standalone `TechInsight` catalog decoupled from library documents and expose paginated/random browsing APIs.

#### Scenario: User requests next technical insight card
- **WHEN** user sends `GET /api/v1/insights/feed` with optional `category` or `tag` query parameters
- **THEN** the system returns a sequence of concise senior technical insight cards containing problem context, under-the-hood analysis, bad vs good code snippets, and benchmark performance stats.

#### Scenario: User navigates insights on frontend card reader
- **WHEN** user visits `/insights` and clicks "Next Insight ➔" or presses Space/ArrowRight
- **THEN** the card reader smoothly transitions to the next technical insight with syntax-highlighted code blocks and category badges.

---

### Requirement: On-Demand AI Insight Synthesizer
The system SHALL support generating fresh, high-impact senior technical insights on-demand via Google Gemini 3.6 Flash.

#### Scenario: User triggers AI insight generation
- **WHEN** user sends `POST /api/v1/insights/generate` with a specified technical topic or category
- **THEN** the system invokes Gemini 3.6 Flash to synthesize a concrete senior-level breakdown with code snippets, saves the result to `TechInsights` table, and returns the newly created insight card.

---

### Requirement: Insight 1-Click Bookmark & Note Saving
The system SHALL allow users to save insights directly to their personal notes or spaced repetition review deck.

#### Scenario: User bookmarks an insight card
- **WHEN** user clicks "Save to Notes" on an insight card
- **THEN** the system persists the bookmark and increments the card's bookmark count.
