# Spec Delta: library

## MODIFIED Requirements

### Requirement: Document Language Fidelity in AI Slice Formatting and Callouts

The AI slice formatter (`GeminiAiService.FormatSliceAsync` and `IAiMarkdownFormatter`) SHALL adapt formatting rules based on the document's content language (`chunk.Language`, e.g. `en` or `vi`).

When formatting slices:
1. **Language Invariance Across All Structural Elements:** All synthesized additions—including the executive context note (`> [!NOTE]`), practical/architectural alerts (`> [!TIP]`, `> [!WARNING]`, `> [!IMPORTANT]`), takeaway headings and bullet points, and the scenario challenge drill—MUST strictly match the language of the source document:
   - For Vietnamese documents (`Language == "vi"`): All callout titles, callout body prose, summary overviews, key takeaways, and scenario drills SHALL be written in natural, idiomatically accurate Vietnamese. The formatter SHALL NOT emit English callouts, English bullet points, or English scenarios for Vietnamese literature.
   - For English documents (`Language == "en"`): All callout titles, callout body prose, summary overviews, key takeaways, and scenario drills SHALL be written in English.
2. **Alert Title Localization:** Alert tags (`NOTE`, `TIP`, `WARNING`, `IMPORTANT`) SHALL retain standard GitHub Alert identifiers (`> [!NOTE]`, `> [!TIP]`, `> [!WARNING]`) so CSS callout styling renders correctly in the reader, while their explanatory content is in the document language.

#### Scenario: Vietnamese document slice curates callouts and takeaways in Vietnamese
- **WHEN** AI formats a slice of a Vietnamese book (`Language == "vi"`, such as *827-thoi-quen-nguyen-tu-thuviensach.vn*)
- **THEN** the executive note (`> [!NOTE]`), practical alerts (`> [!TIP]`, `> [!WARNING]`), key takeaways, and scenario challenge are all generated in Vietnamese, with zero untranslated English narrative blocks.

#### Scenario: English document slice curates callouts and takeaways in English
- **WHEN** AI formats a slice of an English book (`Language == "en"`, such as *ASP.NET Core Architecture Guide*)
- **THEN** all callouts, takeaways, and scenario challenges are generated in English.
