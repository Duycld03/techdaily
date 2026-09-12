# Delta Spec: Core Platform Error Envelopes & Frontend Localization

## ADDED Requirements

### Requirement: Standardized Machine-Readable Error Response Envelope
All API endpoints returning error responses (HTTP 4xx and 5xx) SHALL include a structured JSON envelope containing:
- `code` (string): Unique uppercase snake_case machine-readable identifier (e.g. `RESOURCE_NOT_FOUND`, `AUTH_INVALID_CREDENTIALS`).
- `error` (string): English developer-facing descriptive error message.
- `details` (object or null): Optional structured metadata or validation failure details.

#### Scenario: Endpoint returns bad request error
- **WHEN** client sends an invalid request
- **THEN** server returns HTTP 400 with body `{ "code": "VALIDATION_FAILED", "error": "...", "details": null }`.

#### Scenario: Resource not found
- **WHEN** client queries a non-existent entity
- **THEN** server returns HTTP 404 with body `{ "code": "RESOURCE_NOT_FOUND", "error": "The requested resource was not found.", "details": null }`.

---

### Requirement: Client-Side Dynamic Error Code Localization
The web client (`useApiError` composable) SHALL resolve API error codes against the active i18n locale (`api_errors.<CODE>`). When an error code is present in the locale dictionary, the translated text SHALL be displayed. If no match is found, the system SHALL display the provided localized fallback message or generic localized error message.

#### Scenario: API returns AUTH_INVALID_CREDENTIALS with Vietnamese locale
- **WHEN** API responds with `{ "code": "AUTH_INVALID_CREDENTIALS", "error": "Invalid email or password." }` and client locale is `vi`
- **THEN** client renders toast: "Email hoặc mật khẩu không chính xác."

#### Scenario: API returns AUTH_INVALID_CREDENTIALS with English locale
- **WHEN** API responds with `{ "code": "AUTH_INVALID_CREDENTIALS", "error": "Invalid email or password." }` and client locale is `en`
- **THEN** client renders toast: "Invalid email or password."

---

### Requirement: 100% i18n Coverage for UI Notifications and Banners
All toast notifications, confirmation dialog texts, action button loading states, and contextual alert banners across the frontend application SHALL use `@nuxtjs/i18n` translation keys (`$t` or `t()`). No user-facing notification strings SHALL be hardcoded in component scripts or templates.

#### Scenario: User performs action triggering notification
- **WHEN** user copies text, saves a highlight, creates or deletes an item, or encounters an action error
- **THEN** the emitted toast or inline message reflects the active locale using defined i18n dictionary keys.
