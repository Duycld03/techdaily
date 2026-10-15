# Design

## Context

See `proposal.md` for background and motivation.

In the current catalog administration ingestion UI (`AdminCatalogDraftForm.vue`), administrators can create drafts using either uploaded PDFs or Web URLs. When submitting a web document draft, the form invokes `catalogStore.importUrl()` with `{ sourceUrl: ... }`. However, `useCatalogStore.ts` and the backend endpoint `POST /api/v1/admin/catalog/documents/import-url` expect an object with property `url` conforming to `AdminImportCatalogUrlApiRequest`.

Furthermore, `AdminCatalogDraftForm.vue` maintains two distinct URL ref variables:
1. `previewUrlInput`: Bound to the primary "Import from Web URL" input in the URL preview section.
2. `sourceUrl`: Bound to the "Author or Source URL" metadata input field.

When `previewUrl` is invoked, `sourceUrl` is populated from the preview result. But if the administrator clicks "Create Draft" without clicking "Preview Extraction" first, `sourceUrl` is empty while `previewUrlInput` holds the target URL.

## Goals / Non-Goals

**Goals:**
- Correct the payload field name passed to `catalogStore.importUrl()` from `sourceUrl` to `url`.
- Robustly resolve `targetUrl` using `previewUrlInput.value.trim() || sourceUrl.value.trim()`.
- Ensure `authorOrSourceUrl` is passed as optional attribution metadata.
- Provide comprehensive Vitest test coverage for the web import form submission in `AdminCatalogComponents.spec.ts`.

**Non-Goals:**
- Modifying backend endpoints, DTOs, or database schemas.
- Modifying PDF upload flow (`uploadPdf`).
- Redesigning the Admin Catalog page layout or switching components.

## Decisions

### Decision 1: Single Source of Truth for Target Import URL
- **Choice**: Compute `const targetUrl = previewUrlInput.value.trim() || sourceUrl.value.trim()`.
- **Rationale**:
  - If the administrator entered a URL in the primary URL field (`previewUrlInput`), it is respected whether they previewed the article or not.
  - If they edited the metadata URL field (`sourceUrl`), it serves as a fallback.
  - Validates `!targetUrl` with `toast.error(t('admin_catalog.toasts.source_url_required'))` to prevent dispatching empty requests.
- **Alternative considered**: Requiring preview extraction before allowing submission. Rejected because forcing a preview creates friction for experienced administrators who already know the source URL.

### Decision 2: Preserve Metadata Attribution via `authorOrSourceUrl`
- **Choice**: Pass `authorOrSourceUrl: sourceUrl.value.trim() || undefined` alongside `url: targetUrl`.
- **Rationale**: The backend entity `CatalogDocument` stores `AuthorOrSourceUrl` for citations and links back to the original publisher, while `Url` is the crawling target. Passing both preserves attribution without conflating crawling targets with metadata text.

### Decision 3: Keep Backend Contract and Validation Intact
- **Choice**: Retain `AdminImportCatalogUrlApiRequest(Url, Title, Description, ...)` and `AdminImportCatalogUrlValidator` as is.
- **Rationale**: The backend contract is well-designed and tested in `CatalogIsolationAndBehavioralIntegrationTests.cs`. Aligning the frontend with the established API prevents regression across other administrative tools or future API consumers.

## Risks / Trade-offs

- **Risk: Malformed or unreachable URL submitted without preview**
  - **Mitigation**: Backend `AdminImportCatalogUrlValidator` validates absolute HTTP/HTTPS URL schemes, and `IWebArticleCrawler` handles unreachable hosts or SSRF attempts, returning structured RFC 7807 problem details that `formatError()` displays via `toast.error()`.
