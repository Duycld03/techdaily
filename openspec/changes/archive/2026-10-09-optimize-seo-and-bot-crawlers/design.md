# Design

## Context

TechDaily (branded as DeepPace) runs on Nuxt 4 with Nitro SSR behind an Nginx reverse proxy on `techdaily.duckdns.org`. The application enforces default-deny authentication in `frontend/app/middleware/auth.global.ts`, redirecting unauthenticated visitors to `/login?redirect=...`. Currently, `frontend/public/robots.txt` permits all paths (`Allow: /`) and provides no sitemap or bot-specific filtering, leading search crawlers to scan private paths while exposing server resources to high-volume AI scrapers.

## Goals / Non-Goals

**Goals:**
- Provide a structured, multi-tier `robots.txt` that blocks commercial AI training harvesters, disallows private application paths and backend APIs, allows public entry routes and assets, and references the canonical sitemap.
- Supply a static, standards-compliant `sitemap.xml` with priority, change frequency, and bilingual `xhtml:link` alternate relationships (`en` and `vi`).
- Enrich `frontend/nuxt.config.ts` with comprehensive Open Graph, Twitter Card, canonical link, theme color, and Schema.org JSON-LD structured data.

**Non-Goals:**
- Dynamically generating per-book sitemap URLs from the database (all document slices and pacer interactions are private authenticated features).
- Introducing third-party SEO analytics or tracking dependencies (e.g. Google Tag Manager, Segment).
- Modifying authentication route guards or backend endpoint logic.

## Decisions

### 1. Static File Delivery for `robots.txt` and `sitemap.xml`
- **Choice**: Place `robots.txt` and `sitemap.xml` directly within `frontend/public/`.
- **Rationale**: Nuxt / Nitro automatically serves files from `public/` at the root path (`/robots.txt`, `/sitemap.xml`) with zero SSR compute overhead, ideal cacheability, and deterministic behavior. Since the public searchable pages are finite (`/` and `/login`), a dynamic server route is unnecessary complexity.

### 2. Permitting `/_nuxt/` and Asset Paths for Search Engines
- **Choice**: Explicitly ensure `/_nuxt/`, `/favicon.*`, and `/icons/` remain accessible to general search engine crawlers (`User-agent: *`).
- **Rationale**: Modern search engines (specifically Googlebot and Bingbot) render client-side JavaScript and CSS to evaluate page layout, typography, and mobile responsiveness. Blocking `/_nuxt/` results in "blocked resource" warnings and severely damages search rankings.

### 3. Granular Route Disallowance Strategy
- **Choice**: Explicitly disallow `/today`, `/library`, `/read/`, `/quiz`, `/review`, `/notes`, `/graph`, `/roadmap`, `/settings`, `/profile`, `/showcase`, `/api/`, `/scalar/`, `/health`, and `/uploads/`.
- **Rationale**: Eliminates crawl budget waste on endpoints that return 302 redirects or 401 JSON responses, completely preventing soft-404 reporting in Google Search Console.

### 4. Direct JSON-LD Injection via `app.head.script`
- **Choice**: Embed Schema.org JSON-LD structured data directly inside `frontend/nuxt.config.ts` under `app.head.script` with `type: 'application/ld+json'`.
- **Rationale**: Ensures the structured data is present directly in the SSR HTML response on initial document delivery, allowing immediate indexing by bots that do not execute client hydration.

## Risks / Trade-offs

- **AI Search Citations**: Chaining `Disallow: /` for `GPTBot`, `ClaudeBot`, and `PerplexityBot` means AI tools will not index or directly cite the platform in conversational search results. This is an intentional tradeoff accepted to protect technical learning material and prevent resource depletion.
- **Static Sitemap Maintenance**: Adding future public landing pages will require adding corresponding entries in `frontend/public/sitemap.xml`.
