# Design

## Context
See `proposal.md` for overall motivation. TechDaily's frontend already runs with Nuxt 4 forward compatibility enabled in `nuxt.config.ts` (`future: { compatibilityVersion: 4 }`), meaning project conventions and route configurations already conform to Nuxt 4 expectations. However, `package.json` pins `nuxt: ^3.15.4`, and external documentation/portfolio artifacts still suffer from discrepancies regarding test counts, live demo URLs, and recent architectural capabilities (on-device neural TTS, 3D WebGL knowledge graph).

## Goals / Non-Goals

**Goals:**
- Cleanly upgrade `frontend/package.json` to `nuxt: ^4.6.0` and verify module compatibility (`@nuxtjs/i18n`, `@pinia/nuxt`, `@nuxtjs/color-mode`).
- Maintain full test contract integrity (all 664 frontend Vitest tests pass).
- Synchronize `README.md` with current platform metrics (1,015 total tests: 351 backend + 664 frontend) and comprehensive feature documentation for Audio Narration and the Knowledge Graph.
- Synchronize `duycld03.github.io` portfolio showcase with live production link (`https://techdaily.duckdns.org`), Nuxt 4 framework tag, and accurate technical descriptions.
- Synchronize `career-vault` (`profile.json`, `template.html`, `interview-prep.md`), validating that the generated CV PDF strictly maintains the single-page A4 invariant.

**Non-Goals:**
- Modifying backend .NET 10 APIs, domain entities, or EF Core migrations.
- Rewriting UI layouts or adding new unrequested features.
- Altering the existing single-source-of-truth OpenSpec governance model.

## Decisions

### Decision 1: Dependency Upgrade & Verification Strategy
- **Action**: Install `nuxt@^4.6.0` in `frontend/package.json`, execute `npx nuxi prepare` to regenerate TypeScript declarations, and run `npm --prefix frontend test`.
- **Rationale**: A dry-run installation confirmed clean resolution with zero dependency conflicts. Upgrading directly eliminates forward-compatibility ambiguity while capitalizing on Nuxt 4 performance improvements.
- **Alternatives Considered**: Remaining on Nuxt 3 with compatibility flags. Rejected because official Nuxt 4 is now the standard release, and showcasing an outdated major version contradicts portfolio positioning.

### Decision 2: README Feature Showcase Architecture
- **Action**: Update `README.md` test metrics from 811 to 1,015 tests. Add two prominent feature sections:
  1. *On-Device & Multi-Engine Audio Narration (TTS)* (`/read/[bookId]`, `/today`): Web Worker neural TTS, ONNX Runtime Web, IndexedDB audio chunk caching, cascading fallback to System Web Speech & Cloud TTS.
  2. *Interactive Associative Knowledge Graph* (`/graph`): 2D Canvas & 3D WebGL graph visualizer of personal learning artifacts anchored to architectural pillars.
- **Rationale**: Reflects months of deep technical engineering already committed and specified in `openspec/specs/`.

### Decision 3: External Portfolio & Career Vault Synchronization
- **Action**:
  - In `Duycld03.github.io`: Add `demoUrl: "https://techdaily.duckdns.org"` to `techdaily.md`, keep `Nuxt 4`, and refine description to separate Gemini JSON synthesis from in-browser neural TTS.
  - In `career-vault`: Align `profile.json` and `template.html` with Nuxt 4 and live production domain. Verify single-page PDF output using the established Python page-counting script.
  - In `career-vault/interview-prep.md`: Add dedicated technical interview defense scenarios covering SM-2 algorithm invariants, in-browser neural TTS Web Workers, and PostgreSQL graph relational projections.
- **Rationale**: Satisfies the "Defense Rule" in `cv-rules.md`, ensuring all claims match actual codebase artifacts and can be thoroughly defended in technical interviews.

## Risks / Trade-offs

- **Risk: Breaking changes in Nuxt 4 builder or routeRules**  
  *Mitigation*: The test suite of 664 unit/component tests will be run immediately. Cross-origin isolation headers on `/read/**` will be checked to guarantee multi-threaded Web Worker WASM remains operational.
- **Risk: Career Vault CV overflowing onto page 2**  
  *Mitigation*: The Python PDF page counter (`len(re.findall(rb"/Type\s*/Page\b", f.read())) == 1`) will be executed immediately after compilation. If page count exceeds 1, margins and text density will be adjusted per `cv-rules.md`.
