# Proposal: Modernize Stack Tooling, Scalar API Explorer & VueUse Integration

## Why

TechDaily's development and runtime stack has three specific opportunities for modernization:
1. **API Documentation**: In .NET 9 and .NET 10, Microsoft officially deprecated Swashbuckle in favor of built-in `Microsoft.AspNetCore.OpenApi`. The current `Swashbuckle.AspNetCore 7.2.0` setup lacks modern OpenAPI 3.1 support and native Dark Mode alignment with TechDaily's Dev-Learning Studio theme. Replacing it with `Scalar.AspNetCore` delivers a modern, high-performance API client and documentation viewer.
2. **Frontend Reactivity & Composables**: The frontend currently implements repetitive manual event listeners, outside-click handlers, and window event listeners across components (`AppSelect.vue`, `FlashcardDeck.vue`, `QuickHelpModal.vue`). Installing `@vueuse/nuxt` provides battle-tested composables (`useEventListener`, `onClickOutside`, `useScroll`, `useDebounceFn`) with automatic Nuxt SSR hydration safety and memory leak prevention.
3. **AI Embedding Model Optimization**: While `text-embedding-004` is obsolete/404 on Gemini Developer API v1beta, live verification proves `gemini-embedding-2` and `gemini-embedding-001` are available on the Free Tier and natively support `outputDimensionality = 768`. Aligning configuration with `gemini-embedding-2` upgrades semantic retrieval quality while maintaining 768-D pgvector compatibility.
4. **Agent Skill Enhancement**: Incorporating curated Vue/Nuxt/Pinia/Vitest engineering rules from `antfu/skills` into project agent conventions reinforces strict reactivity patterns, avoids common hydration pitfalls, and standardizes Vitest testing.

## What Changes

- **Backend API Documentation**:
  - Remove `Swashbuckle.AspNetCore` package reference.
  - Add `Microsoft.AspNetCore.OpenApi` and `Scalar.AspNetCore` to `TechDaily.Api.csproj`.
  - Configure `builder.Services.AddOpenApi()` with JWT security schemes in `Program.cs`.
  - Map interactive Scalar API Reference at `/scalar/v1` in development mode, with `/swagger` redirect to `/scalar/v1`.
- **Frontend Composable Utilities**:
  - Add `@vueuse/nuxt` and `@vueuse/core` to `frontend/package.json`.
  - Register `@vueuse/nuxt` in `frontend/nuxt.config.ts` modules.
  - Refactor manual DOM listeners in key components (e.g. `AppSelect.vue`) to use `onClickOutside` and `useEventListener`.
- **Embedding Configuration**:
  - Live test confirmed `text-embedding-004` is 404 on Gemini Developer API v1beta.
  - Live test confirmed `gemini-embedding-2` (Google's latest multimodal embedding) and `gemini-embedding-001` succeed with 768 dimensions under the Gemini Free Tier.
  - Configure `Gemini:EmbeddingModel` to `gemini-embedding-2` (or keep `gemini-embedding-001`).
- **Agent Guidelines & Skills**:
  - Add `antfu/skills` packages (`nuxt`, `vue`, `pinia`, `vitest`) into `.omp/skills/` or `AGENTS.md` to guide AI agents during coding.

## Capabilities

### New Capabilities
*(None - this change modernizes developer tooling, runtime documentation, and foundational libraries within existing capabilities)*

### Modified Capabilities
- `core-platform`: Updates API documentation requirements from Swashbuckle/Swagger to Scalar.AspNetCore/OpenAPI, and registers `@vueuse/nuxt` in frontend architecture.
- `vector-embeddings`: Confirms configuration alignment with verified Gemini Developer API embedding models (`gemini-embedding-2` or `gemini-embedding-001`, 768-D output) on the Free Tier.

## Impact

- **API Compatibility**: Zero breaking changes to REST endpoints or request/response payloads.
- **Frontend Compatibility**: Zero breaking changes. `@vueuse/nuxt` is strictly additive.
- **Developer Experience**: Modern dark-mode API testing via Scalar, reduced boilerplate in Vue components, and clearer AI agent rules.
