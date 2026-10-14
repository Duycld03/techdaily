# Design

## Context

See proposal.md for motivation. `frontend/app/pages/admin/catalog.vue` is currently a monolithic 1,296-line SFC containing multiple discrete UI domains: document creation (PDF drag-and-drop & URL preview), document inspection, metadata editing, slice review, and modal preview. The page relies on `useCatalogStore` (`frontend/app/stores/catalog.ts`), `useAuthStore`, `useToast`, `useApiError`, and `AppSelect`.

The upcoming `add-admin-workspace-dashboard` change expects to mount `/admin/catalog` within its isolated administrator shell (`AdminHeader` and `AdminSidebar`). Decomposing `catalog.vue` into modular components and establishing 100% bilingual i18n coverage now ensures the catalog surface is maintainable, responsive, and ready to be cleanly mounted without architectural debt.

## Goals / Non-Goals

**Goals:**
- Decompose `catalog.vue` from 1,296 lines into 6 focused, cohesive components under `frontend/app/components/catalog/admin/`, keeping the page orchestrator under 200 lines.
- Complete 100% bilingual coverage in `en.json` and `vi.json` for all form labels, placeholders, filter options, status messages, and 14 toast notifications.
- Enforce DeepPace design invariants: body text and form inputs must be at least `text-sm` (14px) on mobile and `text-base` (16px) on desktop; eliminate prohibited `text-xs` usage on primary content.
- Ensure all action buttons and badges use `whitespace-nowrap shrink-0` and responsive gap spacing to prevent layout wrapping collisions between English and Vietnamese.
- Preserve 100% backward compatibility with `useCatalogStore` state, actions, and API contracts.

**Non-Goals:**
- Modifying backend endpoints, DTO contracts, or database schemas.
- Altering `useCatalogStore` business logic, polling mechanisms, or error handling.
- Implementing the global admin shell, admin login, or dashboard metrics (owned by `add-admin-workspace-dashboard`).
- Modifying the learner discovery interface at `/library`.

## Decisions

### 1. Component Architecture & Decomposition

The monolithic page is refactored into the following hierarchy:

```
frontend/app/pages/admin/catalog.vue (Orchestrator, ~180 lines)
  │
  ├── MasterDetailLayout
        ├── #header slot
        │     └── AdminCatalogHeader.vue (~80 lines)
        │
        ├── #nav slot
        │     └── AdminCatalogNavRail.vue (~180 lines)
        │
        └── #content slot
              ├── AdminCatalogDraftForm.vue (~260 lines) [when isCreatingNewDraft]
              └── AdminCatalogDocumentDetail.vue (~320 lines) [when selectedDocId]
                    └── AdminCatalogSlicesReview.vue (~160 lines)
  │
  └── AdminCatalogSliceModal.vue (~90 lines) [teleported via AppModal]
```

- `pages/admin/catalog.vue` acts as the thin orchestrator, managing selection state (`selectedDocId`, `isCreatingNewDraft`), modal inspection state (`selectedSlice`, `isSliceModalOpen`), and top-level lifecycle.
- Components communicate via explicit Vue 3.5 props and emits, while reading shared state from `catalogStore` directly to minimize redundant prop forwarding.

### 2. Comprehensive i18n Dictionary Organization

All new translation tokens are grouped cleanly under the existing `"admin_catalog"` key in `en.json` and `vi.json`:

- `admin_catalog.filters`: search placeholder, visibility filter options (`All Visibility`, `Draft`, `Published`, `Hidden`), processing status options (`All Processing`, `Pending`, `Processing`, `Ready`, `Failed`), empty search indicator, loading state.
- `admin_catalog.form`: PDF vs. URL tabs, drag-and-drop zone copy, size limit hints, URL preview detected labels, field labels (`title`, `category`, `language`, `author`, `attribution`, `permission_basis`, `permission_evidence`), placeholders, and action buttons (`cancel`, `create_draft`).
- `admin_catalog.document`: revision badge, review pending badge, slice count description, raw/prepared badges, slice inspect tooltip, publication section title, current visibility, empty selection prompt.
- `admin_catalog.modal`: slice inspection title, estimated read time, summary header, source markdown header, close button.
- `admin_catalog.toasts`: 14 distinct notification messages covering validation failures, upload requirements, asynchronous preparation triggers, and publication successes.

### 3. Typography & Layout Governance

- Form labels, input fields, and descriptive summaries are updated to `text-sm sm:text-base`.
- `text-xs` (12px) is strictly confined to compact status badges (`DocStatusBadge`, `RevisionBadge`, `SliceOrderBadge`) and timestamps.
- Action buttons in header and detail views enforce `whitespace-nowrap shrink-0` and responsive padding (`px-3 py-2 sm:px-4 sm:py-2.5`) to eliminate text wrapping across long Vietnamese labels (e.g. *"Xác nhận xem xét quản trị viên"* vs *"Confirm Administrator Review"*).

## Risks / Trade-offs

- **Risk:** Event/state desynchronization between child forms and the page orchestrator.
  - **Mitigation:** Use `useCatalogStore` as the authoritative store for documents and draft operations; pass immutable document DTOs as props and emit discrete actions for user intents.
- **Risk:** Regressions in existing catalog store tests.
  - **Mitigation:** The refactor is purely presentational and structural within the frontend UI layer; no store signatures or API endpoints are changed. Existing Vitest suites must pass 100%.
