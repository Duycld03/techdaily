# Proposal

## Why

TechDaily's public web presence currently uses a permissive `robots.txt` (`Allow: /`) without a sitemap or bot segmentation, causing search crawlers to scan private authenticated routes (triggering 302 redirect loops and soft-404 errors) while leaving the server exposed to aggressive AI training scrapers (such as GPTBot, CCBot, and Bytespider) that exhaust rate limits and server bandwidth. Additionally, the application lacks social graph metadata (Open Graph / Twitter Cards), canonical URLs, and Schema.org structured data needed for search engine ranking, rich snippets, and professional social link previews.

## What Changes

- **Crawler Governance & AI Scraping Mitigation (`robots.txt`)**:
  - Explicitly disallow aggressive commercial AI scrapers and harvesters (`GPTBot`, `CCBot`, `Bytespider`, `ClaudeBot`, `Anthropic-ai`, `PerplexityBot`, `Diffbot`, `Amazonbot`).
  - Disallow search crawlers from indexing private authenticated app routes (`/today`, `/library`, `/read/`, `/quiz`, `/review`, `/notes`, `/graph`, `/roadmap`, `/settings`, `/profile`, `/showcase`) to preserve crawl budget and eliminate 302 soft-404 indexing errors.
  - Disallow internal backend endpoints (`/api/`, `/scalar/`, `/health`, `/uploads/`).
  - Explicitly allow search engines (Googlebot, Bingbot, etc.) to crawl public entry points (`/`, `/login`, `/register`) and static rendering bundles (`/_nuxt/`, `/favicon.*`).
  - Reference the canonical sitemap (`Sitemap: https://techdaily.duckdns.org/sitemap.xml`).
- **Canonical XML Sitemap (`sitemap.xml`)**:
  - Provide a standards-compliant XML sitemap (`frontend/public/sitemap.xml`) referencing public canonical URLs (`/`, `/login`) with `priority`, `changefreq`, `lastmod`, and bilingual alternate links (`hreflang="en"` and `hreflang="vi"`).
- **Metadata & Social Graph Optimization (`nuxt.config.ts`)**:
  - Configure global canonical link tag (`<link rel="canonical" href="https://techdaily.duckdns.org" />`).
  - Configure Open Graph protocol tags (`og:site_name`, `og:type`, `og:title`, `og:description`, `og:url`, `og:image`, `og:locale`, `og:locale:alternate`).
  - Configure Twitter Card metadata (`twitter:card="summary_large_image"`, `twitter:title`, `twitter:description`, `twitter:image`).
  - Set browser theme color meta (`theme-color: #09090b`) matching the Dark Mode design token.
- **Structured Data Integration (Schema.org JSON-LD)**:
  - Inject `WebApplication` / `EducationalApplication` JSON-LD schema into `app.head` in `nuxt.config.ts` to enable rich search snippets and semantic platform discovery.

## Capabilities

### New Capabilities

- `seo-and-crawlers`: Establishes search engine discovery, crawler access control, sitemap specification, social graph previews, and structured metadata across the web platform.

### Modified Capabilities

*(None)*

## Impact

- **Affected Systems**: Frontend (`frontend/public/robots.txt`, `frontend/public/sitemap.xml`, `frontend/nuxt.config.ts`).
- **User / Consumer Impact**: Zero disruption to authenticated user workflows. Greatly improved social link previews (Facebook, LinkedIn, Zalo, Slack, Discord) and search engine indexing efficiency.
- **Infrastructure Impact**: Drastically reduced unwanted crawler traffic on Kestrel backend and Nginx reverse proxy from AI scraping bots.
