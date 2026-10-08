# Spec Delta: seo-and-crawlers

## Purpose
Governs search engine optimization, crawler directives, sitemap indexing, social graph metadata, and semantic structured data across the public web surface of TechDaily.

## ADDED Requirements

### Requirement: Crawler Access Control & Scraping Mitigation (`robots.txt`)
The web application SHALL deliver a plain-text `robots.txt` file at the root path (`/robots.txt`) that establishes granular bot permissions, preserves search crawl budget, and protects platform resources from unauthorized high-frequency scrapers:

1. **AI & Automated Harvester Blockade**:
   - The document SHALL explicitly declare `Disallow: /` for known commercial LLM training bots and aggressive scrapers, including `GPTBot`, `CCBot`, `Bytespider`, `ClaudeBot`, `Anthropic-ai`, `PerplexityBot`, `Diffbot`, and `Amazonbot`.
2. **Private Application & API Route Protection**:
   - The document SHALL disallow search crawlers from accessing authenticated user workflows (`/today`, `/library`, `/read/`, `/quiz`, `/review`, `/notes`, `/graph`, `/roadmap`, `/settings`, `/profile`, `/showcase`) to eliminate 302 redirect loops and soft-404 search indexing penalties.
   - The document SHALL disallow search crawlers from accessing backend service routes (`/api/`, `/scalar/`, `/health`, `/uploads/`).
3. **Public Discovery & Asset Accessibility**:
   - The document SHALL permit universal crawlers (`User-agent: *`) to index public entry points (`/`, `/login`, `/register`) and required client rendering assets (`/_nuxt/`, `/favicon.*`, `/icons/`).
4. **Sitemap Reference**:
   - The document SHALL declare the absolute canonical sitemap URL (`Sitemap: https://techdaily.duckdns.org/sitemap.xml`).

#### Scenario: Legitimate search crawler accesses robots.txt
- **WHEN** Googlebot or Bingbot requests `GET /robots.txt`
- **THEN** the server returns `HTTP 200 OK` with `Content-Type: text/plain`
- **AND** the payload permits access to `/` and `/_nuxt/`
- **AND** disallows access to `/today`, `/read/`, and `/api/`
- **AND** contains `Sitemap: https://techdaily.duckdns.org/sitemap.xml`.

#### Scenario: Commercial AI scraper accesses robots.txt
- **WHEN** `GPTBot`, `CCBot`, or `Bytespider` checks crawl permissions in `robots.txt`
- **THEN** the payload matches the respective `User-agent` block declaring `Disallow: /`.

---

### Requirement: Canonical XML Sitemap Declaration (`sitemap.xml`)
The web application SHALL deliver a static, standards-compliant XML sitemap at `/sitemap.xml` conforming to the Sitemaps.org protocol (`http://www.sitemaps.org/schemas/sitemap/0.9`):

1. **Indexed Public URLs**:
   - The sitemap SHALL list the public root (`https://techdaily.duckdns.org/`) and authentication gateway (`https://techdaily.duckdns.org/login`).
   - Private or authenticated application routes SHALL NOT appear in the sitemap.
2. **Indexing Metadata**:
   - Each `<url>` entry SHALL define `<loc>`, `<changefreq>`, `<priority>`, and `<lastmod>`.
   - The home entry SHALL carry a priority of `1.0` and change frequency of `daily`.
   - The login entry SHALL carry a priority of `0.8` and change frequency of `monthly`.
3. **Bilingual Alternate Links**:
   - Each `<url>` entry SHALL include XHTML alternate link elements (`xmlns:xhtml="http://www.w3.org/1999/xhtml"`) for both supported languages (`en` and `vi`).

#### Scenario: Search engine requests sitemap.xml
- **WHEN** a search engine crawler requests `GET /sitemap.xml`
- **THEN** the server returns `HTTP 200 OK` with valid XML
- **AND** contains `<loc>https://techdaily.duckdns.org/</loc>` with `changefreq` daily and `priority` 1.0
- **AND** contains alternate `hreflang` links for `en` and `vi`
- **AND** contains zero private authenticated routes (`/today`, `/read/`).

---

### Requirement: Global Metadata, Canonical URLs & Social Graph Integration
The web application SHALL deliver complete Open Graph, Twitter Card, and canonical metadata within the document `<head>` on all rendered pages:

1. **Canonical Link**:
   - Every page SHALL specify `<link rel="canonical" href="https://techdaily.duckdns.org" />`.
2. **Open Graph Protocol**:
   - The document head SHALL render `og:site_name`, `og:type` (`website`), `og:title`, `og:description`, `og:url`, `og:image`, `og:locale` (`en_US`), and `og:locale:alternate` (`vi_VN`).
3. **Twitter Card Metadata**:
   - The document head SHALL render `twitter:card` with value `summary_large_image`, alongside `twitter:title`, `twitter:description`, and `twitter:image`.
4. **Theme Color**:
   - The document head SHALL declare `<meta name="theme-color" content="#09090b" />` matching the platform Dark Mode background.

#### Scenario: External social crawler previews shared link
- **WHEN** an external social bot (Facebook, LinkedIn, Slack, Telegram, Twitter) scrapes `https://techdaily.duckdns.org`
- **THEN** the rendered HTML contains `og:title`, `og:description`, `og:image`, and `twitter:card="summary_large_image"`
- **AND** allows rich card preview rendering with intact platform branding.

---

### Requirement: Semantic Structured Data (Schema.org JSON-LD)
The web application SHALL inject a Schema.org JSON-LD document in `<script type="application/ld+json">` within the root document `<head>`:

1. **Entity Definition**:
   - The JSON-LD schema SHALL define `@type: "WebApplication"` and `"EducationalApplication"`.
   - The schema SHALL declare `name` ("DeepPace"), `alternateName` ("TechDaily"), `url` ("https://techdaily.duckdns.org"), and an accurate technical description.
2. **Platform & Pricing Metadata**:
   - The schema SHALL declare `applicationCategory: "EducationalApplication"`, `operatingSystem: "All"`, and free-tier access offering.

#### Scenario: Search engine extracts structured data
- **WHEN** Google Structured Data Testing Tool or Rich Results Validator parses the root document
- **THEN** it detects valid JSON-LD with `@type` WebApplication
- **AND** identifies name, URL, applicationCategory, and description without schema errors.
