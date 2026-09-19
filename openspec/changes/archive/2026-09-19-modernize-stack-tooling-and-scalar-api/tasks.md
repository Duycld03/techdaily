# Tasks

## 1. Backend: Scalar.AspNetCore & OpenAPI Migration

- [x] 1.1 Remove `Swashbuckle.AspNetCore` from `backend/src/TechDaily.Api/TechDaily.Api.csproj` and add `Microsoft.AspNetCore.OpenApi` and `Scalar.AspNetCore`.
- [x] 1.2 In `backend/src/TechDaily.Api/Program.cs`, replace `AddSwaggerGen` with `AddOpenApi()` configuring the OpenAPI 3.1 document with JWT Bearer security scheme definition.
- [x] 1.3 In `backend/src/TechDaily.Api/Program.cs`, replace `UseSwagger()` / `UseSwaggerUI()` with `MapOpenApi()`, `MapScalarApiReference()` with dark theme (Moon), and map `/swagger` redirects to `/scalar/v1`.

## 2. Frontend: VueUse Integration

- [x] 2.1 Add `@vueuse/nuxt` and `@vueuse/core` to `frontend/package.json` and register `@vueuse/nuxt` in `frontend/nuxt.config.ts` modules.
- [x] 2.2 Refactor `frontend/components/common/AppSelect.vue` to use `onClickOutside` and `useEventListener` from VueUse, removing manual `document.addEventListener` and `onBeforeUnmount` cleanup.

## 3. Infrastructure & AI: Gemini Embedding Verification

- [x] 3.1 Update `Gemini:EmbeddingModel` in `backend/src/TechDaily.Api/appsettings.json` to `gemini-embedding-2` (validated 768-D, Gemini Developer API Free Tier).
- [x] 3.2 Ensure `GeminiEmbeddingService.cs` correctly passes `outputDimensionality = 768` and handles both online API and offline mock modes.

## 4. Documentation & Agent Skills Integration

- [x] 4.1 Install curated `antfu/skills` packages (`nuxt`, `vue`, `pinia`, `vitest`) into `.omp/skills/`.
- [x] 4.2 Update `AGENTS.md` to incorporate curated frontend engineering conventions inspired by `antfu/skills` (Pinia `storeToRefs`, Nuxt SSR data fetching, and VueUse DOM hygiene).

## 5. Automated Verification & Testing

- [x] 5.1 Run backend test suite (`dotnet test backend/tests/TechDaily.Tests`) to ensure 100% build and test pass rate.
- [x] 5.2 Run frontend test suite (`npm --prefix frontend test`) to verify all components and stores pass cleanly.
- [x] 5.3 Validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.
