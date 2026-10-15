# SEO And Crawlers Spec Delta

## MODIFIED Requirements

### Requirement: Semantic Structured Data (Schema.org JSON-LD)
The web application SHALL inject a Schema.org JSON-LD document in `<script type="application/ld+json">` within the root document `<head>`:

1. **Entity Definition**:
   - The JSON-LD schema SHALL define `@type: "WebApplication"` and `"EducationalApplication"`.
   - The schema SHALL declare `name` ("DeepPace"), `url` ("https://deeppace.duckdns.org"), and an accurate technical description. It SHALL omit the retired TechDaily `alternateName` rather than advertise the old product identity.
2. **Platform & Pricing Metadata**:
   - The schema SHALL declare `applicationCategory: "EducationalApplication"`, `operatingSystem: "All"`, and free-tier access offering.

#### Scenario: Search engine extracts structured data
- **WHEN** Google Structured Data Testing Tool or Rich Results Validator parses the root document
- **THEN** it detects valid JSON-LD with `@type` WebApplication
- **AND** identifies name, URL, applicationCategory, and description without schema errors.

#### Scenario: Product identity after cleanup
- **WHEN** a crawler reads the current application structured data
- **THEN** DeepPace is the product name and the canonical URL remains the existing DeepPace deployment
- **AND** no TechDaily alternate-name alias is advertised.
