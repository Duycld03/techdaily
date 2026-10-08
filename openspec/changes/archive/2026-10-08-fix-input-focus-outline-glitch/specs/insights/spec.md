# Spec Delta

## MODIFIED Requirements

### Requirement: On-Demand AI Insight Synthesizer
The AI topic synthesis dialog on `/insights` SHALL provide an ergonomic, visually cohesive modal input form:

1. **Flush Focus Ring Geometry & Stability**:
   - When the custom topic text input is focused (including automatic autofocus upon modal presentation), the input SHALL apply flush, border-radius-conforming focus styling (`focus:outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20`).
   - The input SHALL NOT render a detached, floating outer outline with an offset gap or double-border glitch.
2. **Bilingual Action Localization**:
   - The modal dismiss action button SHALL render localized text using `$t('insights.cancel')` ("Hủy Bỏ" in Vietnamese, "Cancel" in English) rather than hardcoded English text.

#### Scenario: User opens AI topic synthesis modal and inspects input focus
- **GIVEN** an authenticated user clicks "Tạo Với AI" on `/insights`
- **WHEN** the generator modal renders and the text input receives autofocus
- **THEN** the input displays flush rounded focus styling adhering strictly to the input's `rounded-2xl` geometry
- **AND** zero detached or offset purple outline rectangles appear around the input box.

#### Scenario: User inspects modal dismiss button localization
- **GIVEN** user views the AI generator modal in Vietnamese (`vi-VN`)
- **WHEN** inspecting the dialog action footer
- **THEN** the dismiss button displays "Hủy Bỏ"
- **WHEN** user switches interface locale to English (`en-US`)
- **THEN** the dismiss button displays "Cancel".
