# Insights Capability Delta Specification

## MODIFIED Requirements

### Requirement: On-Demand AI Insight Synthesizer
The system SHALL support generating fresh, high-impact senior technical insights on-demand via Google Gemini Flash Lite.

#### Scenario: User triggers AI insight generation
- **WHEN** user sends `POST /api/v1/insights/generate` with a specified technical topic or category
- **THEN** the system invokes Google Gemini Flash Lite to synthesize a concrete senior-level breakdown with code snippets, saves the result to `TechInsights` table, and returns the newly created insight card.
