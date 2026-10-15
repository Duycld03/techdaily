# Tasks

## 1. Locale Dictionaries Update

- [x] 1.1 Add missing `filter_from` and `filter_to` tokens under `admin_audit` in `frontend/app/i18n/locales/en.json` and `vi.json`.
- [x] 1.2 Add missing `prev`, `next`, `scope_personal`, `scope_catalog`, `timestamps_heading`, `created_label`, `observed_label`, `queued_label`, `started_label`, `ended_label`, `source_missing`, `source_available`, `source_permanent`, `source_temporary`, and `ineligible_*` tokens under `admin_documents` in `en.json` and `vi.json`.

## 2. Catalog Admin Detail UI Polish

- [x] 2.1 Remove the bullet dot element (`<span class="w-2 h-2 rounded-full shrink-0 ..."></span>`) inside the processing status badge in `frontend/app/components/catalog/admin/AdminCatalogDocumentDetail.vue`, retaining clean text pill presentation.

## 3. Audit Log UI & Localization

- [x] 3.1 Replace native `<select><option>` with `AppSelect.vue` for the outcome filter in `frontend/app/pages/admin/audit.vue`, configuring `size="sm"` and reactive `outcomeOptions`.
- [x] 3.2 Update date filter inputs in `audit.vue` to bind localized placeholders `$t('admin_audit.filter_from')` and `$t('admin_audit.filter_to')`.
- [x] 3.3 Implement `formatOutcome(outcome: string)` in `audit.vue` to localize outcome badges in both the event list and detail modal.

## 4. Document Operations Localization

- [x] 4.1 Implement `formatDocumentStatus`, `formatScope`, and `formatSourceAvailability` in `frontend/app/pages/admin/documents.vue`, updating the document list cards and header badges.
- [x] 4.2 Replace hardcoded `Prev` and `Next` buttons with localized `$t('admin_documents.prev')` and `$t('admin_documents.next')` in `documents.vue`.
- [x] 4.3 Localize the Timestamps card heading and created/observed label prefixes in `documents.vue`.
- [x] 4.4 Localize ingestion attempt states and timestamp prefixes (`Queued:`, `Started:`, `Ended:`) in `documents.vue`.
- [x] 4.5 Map `ineligibleReasonCode` to localized guidance in the retry ineligible alert banner in `documents.vue`.

## 5. Dual-Gate Verification

- [x] 5.1 Run `npm test` in `frontend` to verify all Vitest unit and integration suites pass with zero regressions.
- [x] 5.2 Launch Headless Chromium via `eval` browser, inspect `/admin/catalog`, `/admin/audit`, and `/admin/documents` in Desktop and Mobile (390px) viewports in both English and Vietnamese, and verify zero visual collisions or unlocalized strings.
