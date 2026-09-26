# Spec Delta

## ADDED Requirements

### Requirement: Explicit Document Language Selection at Import

The document import experience (PDF upload and remote-PDF / URL import) SHALL let the user explicitly select the document's content language (English or Vietnamese) as a first-class field. The selector SHALL default to the current interface locale but SHALL be user-overridable, and the import SHALL send the selected language as the document's language rather than silently using the interface locale. The selected language SHALL be applied to the document's slices so language-dependent downstream behavior (such as narration voice) matches the document's actual language regardless of the interface locale at import time. The selector SHALL be an accessible in-page control (not a native browser `<select>`) with localized `en`/`vi` labels.

#### Scenario: Language selector shown and defaulted to interface locale
- **WHEN** a user opens the PDF upload or URL import form
- **THEN** a document-language selector is shown, defaulting to the current interface locale.

#### Scenario: English document uploaded under a Vietnamese interface
- **WHEN** a user whose interface is Vietnamese uploads an English document and sets the language selector to English
- **THEN** the document and its slices are tagged English, not Vietnamese.

#### Scenario: Narration voice follows the chosen import language
- **WHEN** a document imported with a chosen language is later narrated
- **THEN** the narration voice matches the language chosen at import, independent of the current interface locale.
