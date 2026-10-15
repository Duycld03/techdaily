# Design

## Context

Current administration pages (`/admin/audit`, `/admin/documents`, `/admin/catalog`) contain localized text mixed with hardcoded English strings, native `<select>` controls, and unnecessary visual indicators. See `proposal.md` for background motivation.

## Goals / Non-Goals

**Goals:**
- Eliminate raw HTML form select elements across admin routes, replacing them with `AppSelect.vue`.
- Clean up status badge presentation in `AdminCatalogDocumentDetail.vue` by removing the leading bullet indicator dot.
- Deliver 100% complete bilingual localization across `/admin/audit` and `/admin/documents` for filters, placeholders, status pills, pagination, metadata labels, error reasons, and attempt records.
- Preserve responsive layout geometry and accessibility across desktop (1440px) and mobile (390px) viewports.

**Non-Goals:**
- Modifying backend API contracts, DTOs, or domain retry logic.
- Adding new administrative features or query filters.
- Refactoring unrelated administrative subpages (`/admin/users`, `/admin/login`).

## Decisions

### 1. Status Pill Cleanup in `AdminCatalogDocumentDetail.vue`
- **Decision**: Remove the inner `<span class="w-2 h-2 rounded-full shrink-0 ..."></span>` indicator dot from the `processingStatus` badge.
- **Rationale**: Eliminating the leading dot creates visual uniformity across the header pill group (`[ĐÃ ẨN]`, `[Sẵn sàng]`, `[Bản sửa đổi 1]`), removing confusing bullet-list connotations while maintaining color-coded semantic borders and backgrounds.

### 2. Custom `AppSelect` Adoption for Audit Log Outcome Filter
- **Decision**: Replace `<select v-model="selectedOutcome">` with `<AppSelect v-model="selectedOutcome" :options="outcomeOptions" size="sm" @change="applyFilters" />` wrapped in a constrained container (`w-36`).
- **Rationale**: Direct adherence to DeepPace Pillar 2. Eliminates unstyled browser native popovers on Linux/Chromium and guarantees accessible keyboard navigation and dark-theme glass styling.

### 3. Localized Formatters in `audit.vue` and `documents.vue`
- **Decision**: Centralize translation lookups through lightweight component-level helper functions:
  - In `audit.vue`: `formatOutcome(outcome: string)` resolves `admin_audit.outcome_*` keys for both the event list and detail modal.
  - In `documents.vue`:
    - `formatDocumentStatus(status: string)` maps status strings to `admin_documents.status_*`.
    - `formatScope(scope: string)` maps to `admin_documents.scope_*`.
    - `formatSourceAvailability(avail: string)` maps to `admin_documents.source_availability_*`.
    - `formatAttemptState(state: string)` maps to `admin_documents.attempt_state_*`.
    - `formatIneligibleReason(code?: string | null, fallback?: string | null)` maps known reason codes (`LearningActivityExists`, `TargetDeleted`, `AlreadyReady`, etc.) to clear localized messages.
- **Rationale**: Keeps templates clean, prevents undefined translation lookup crashes, and provides safe fallbacks if unforeseen values are encountered.

### 4. Symmetric Dictionary Expansion in `en.json` and `vi.json`
- **Decision**: Add all required tokens symmetrically to both dictionaries under `admin_audit` and `admin_documents` blocks:
  - Date placeholders: `filter_from`, `filter_to`.
  - Pagination: `prev`, `next`.
  - Timestamps: `timestamps_heading`, `created_at_label`, `observed_at_label`, `queued_at_label`, `started_at_label`, `ended_at_label`.
  - Ineligible reason guidance tokens.

## Risks / Trade-offs

- **Risk: Text length expansion in Vietnamese causing filter bar wrapping**:
  - *Mitigation*: Action buttons and select wrappers enforce `whitespace-nowrap shrink-0` and responsive gap spacing per DeepPace typography rules.
- **Risk: Missing translation fallbacks for uncommon attempt or error codes**:
  - *Mitigation*: Formatters return the original string or fallback whenever a localized key is absent.
