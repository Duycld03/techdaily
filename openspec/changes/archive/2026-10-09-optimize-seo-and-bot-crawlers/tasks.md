# Tasks

## 1. Frontend Crawler Directives & Sitemap

- [x] 1.1 Update `frontend/public/robots.txt` to block aggressive AI training crawlers (`GPTBot`, `CCBot`, `Bytespider`, `ClaudeBot`, `Anthropic-ai`, `PerplexityBot`, `Diffbot`, `Amazonbot`), disallow private app and API paths (`/today`, `/read/`, `/library`, `/quiz`, `/review`, `/notes`, `/graph`, `/roadmap`, `/settings`, `/profile`, `/showcase`, `/api/`, `/scalar/`, `/health`, `/uploads/`), allow public paths and rendering assets (`/`, `/login`, `/register`, `/_nuxt/`, `/favicon.*`, `/icons/`), and reference canonical sitemap.
- [x] 1.2 Create `frontend/public/sitemap.xml` conforming to Sitemaps.org schema with entries for `https://deeppace.duckdns.org/` (priority 1.0, daily) and `https://deeppace.duckdns.org/login` (priority 0.8, monthly), including bilingual `xhtml:link` alternate mappings for `en` and `vi`.

## 2. Frontend Metadata & Structured Data

- [x] 2.1 Update `frontend/nuxt.config.ts` `app.head` with canonical link tag (`https://deeppace.duckdns.org`), theme-color meta (`#09090b`), and complete Open Graph protocol tags (`og:site_name`, `og:type`, `og:title`, `og:description`, `og:url`, `og:image`, `og:locale`, `og:locale:alternate`).
- [x] 2.2 Update `frontend/nuxt.config.ts` `app.head` with Twitter Card tags (`twitter:card="summary_large_image"`, `twitter:title`, `twitter:description`, `twitter:image`).
- [x] 2.3 Inject Schema.org JSON-LD structured data (`WebApplication` / `EducationalApplication`) into `app.head.script` in `frontend/nuxt.config.ts`.

## 3. Verification & Dual-Gate Testing

- [x] 3.1 Create Vitest unit test suite `frontend/tests/config/seo-metadata.spec.ts` asserting `nuxt.config.ts` contains valid canonical links, Open Graph tags, Twitter Cards, and valid parseable JSON-LD structured data.
- [x] 3.2 Execute `npm test` in `frontend/` to verify all Vitest test suites pass 100%.
- [x] 3.3 Execute `npm run build` in `frontend/` to verify zero build errors or SSR bundle regressions.
- [x] 3.4 Verify HTTP response headers and payloads for `/robots.txt` and `/sitemap.xml` on the running preview server, and conduct headless browser visual inspection.
