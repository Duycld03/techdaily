# Design: Modernize Stack Tooling, Scalar API Explorer & VueUse Integration

## Context

TechDaily's backend runs on ASP.NET Core (.NET 10) with Minimal APIs, currently documented with `Swashbuckle.AspNetCore 7.2.0`. Microsoft officially deprecated Swashbuckle in .NET 9/10, recommending `Microsoft.AspNetCore.OpenApi` with third-party renderers like Scalar.

The frontend uses Nuxt 3 (Vue 3.5), Pinia, and Tailwind CSS. Several interactive components (`AppSelect.vue`, `FlashcardDeck.vue`, `QuickHelpModal.vue`) currently attach manual DOM listeners for outside-clicks and keyboard events, which require manual teardown and risk SSR hydration mismatches or memory leaks.

The vector embedding engine uses Google Gemini API with pgvector (768-D). Live testing reveals `text-embedding-004` returns 404 on Gemini Developer API v1beta, whereas `gemini-embedding-2` and `gemini-embedding-001` are active and return 768-D vectors with HTTP 200 on the Free Tier.

## Goals / Non-Goals

**Goals:**
- Replace `Swashbuckle.AspNetCore` with `Microsoft.AspNetCore.OpenApi` and `Scalar.AspNetCore` in `TechDaily.Api`.
- Expose modern dark-mode API documentation at `/scalar/v1`, with a graceful redirect from `/swagger`.
- Add `@vueuse/nuxt` and `@vueuse/core` to the frontend, refactoring `AppSelect.vue` to demonstrate declarative event and outside-click handling.
- Align `Gemini:EmbeddingModel` to `gemini-embedding-2` (or keep `gemini-embedding-001`), confirmed at 768-D on the Gemini Developer API Free Tier.
- Codify proven conventions from `antfu/skills` into `.omp/skills/` and `AGENTS.md` (Pinia `storeToRefs`, Nuxt SSR data fetching, and Vitest component practices).

**Non-Goals:**
- **Tailwind CSS v4 Migration**: Explicitly excluded. Tailwind v4 introduces breaking changes for `@nuxtjs/tailwindcss` modules and replaces CSS class configurations with `@theme` blocks. TechDaily stays on stable Tailwind v3.4.17 LTS.
- **Nuxt 4 Migration**: Nuxt 4 is not yet final; TechDaily already has `future: { compatibilityVersion: 4 }` enabled in `nuxt.config.ts`, providing Nuxt 4 folder conventions while maintaining Nuxt 3.15 stability.
- **pgvector Dimensionality Changes**: TechDaily's schema is fixed at 768 dimensions. Any embedding model must output 768-D vectors.

## Decisions

### 1. API Explorer: `Scalar.AspNetCore` + `Microsoft.AspNetCore.OpenApi`
- **Rationale**: Microsoft's official guidance for .NET 9/10 is `AddOpenApi()` paired with Scalar. Scalar provides a native dark theme (Moon/Saturn), offline-bundled client, OpenAPI 3.1 support, and an interactive test console with JWT authorization.
- **Route Mapping**:
  ```csharp
  if (app.Environment.IsDevelopment())
  {
      app.MapOpenApi();
      app.MapScalarApiReference(options =>
      {
          options.WithTitle("TechDaily API")
                 .WithTheme(ScalarTheme.Moon)
                 .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
      });
      app.MapGet("/swagger", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
      app.MapGet("/swagger/index.html", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
  }
  ```
- **JWT Security Scheme**: Configured via an `OpenApiDocumentTransformer` in `AddOpenApi()`.

### 2. Frontend Composable Utilities: `@vueuse/nuxt`
- **Rationale**: VueUse is the de-facto standard composable library maintained by Anthony Fu. `@vueuse/nuxt` handles auto-imports and SSR-safe cleanup automatically.
- **Initial Refactor (`AppSelect.vue`)**:
  - Replace `document.addEventListener('click', onDocumentClick)` with `onClickOutside(selectRef, () => { isOpen.value = false; })`.
  - Replace `window.addEventListener('keydown', ...)` with `useEventListener('keydown', ...)`.

### 3. Embedding Model: Google Gemini `gemini-embedding-2` (or `gemini-embedding-001`)
- **Live Test Findings**:
  - `text-embedding-004`: Returns 404 NOT_FOUND on `generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent`.
  - `gemini-embedding-2`: Returns 200 OK, supports `outputDimensionality = 768`, free on Gemini Developer API Free Tier.
  - `gemini-embedding-001`: Returns 200 OK, supports `outputDimensionality = 768`.
- **Recommendation**: Upgrade to `gemini-embedding-2` for superior semantic nuance, retaining `outputDimensionality = 768` to avoid database schema alterations.

### 4. Agent Guidelines Integration (`antfu/skills`)
- **Integration Mechanism**:
  - Add skill definitions (`nuxt`, `vue`, `pinia`, `vitest`) to `.omp/skills/`.
  - Embed top rules into `AGENTS.md` for zero-friction adherence:
    - **Pinia**: Always use `storeToRefs(store)` when destructuring reactive state and getters; never destructure actions with `storeToRefs`.
    - **Nuxt SSR**: Use `useFetch` / `useAsyncData` for SSR-compatible data fetching; restrict `$fetch` to event handlers and client actions.
    - **DOM & Reactivity**: Use VueUse composables for DOM listeners to guarantee lifecycle cleanup.

## Risks / Trade-offs

- **Scalar Package Compatibility**: `Scalar.AspNetCore` version `2.x` requires .NET 9 or .NET 10. TechDaily targets `net10.0`, ensuring full compatibility.
- **OpenAPI Schema Generation**: Minimal API endpoints in TechDaily already use `.Produces<T>()` and typed results, which map cleanly to OpenAPI 3.1.
