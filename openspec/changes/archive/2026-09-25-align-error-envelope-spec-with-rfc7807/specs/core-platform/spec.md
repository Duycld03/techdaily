# Spec Delta: Core Platform

## MODIFIED Requirements

### Requirement: Standardized Machine-Readable Error Response Envelope
All API endpoints returning error responses (HTTP 4xx and 5xx) SHALL emit an RFC 7807 `application/problem+json` body containing:
- `type` (string): A URI reference identifying the problem type.
- `title` (string): A short, human-readable summary of the problem category (e.g. `Bad Request`, `Not Found`, `Conflict`).
- `status` (number): The HTTP status code.
- `detail` (string): English developer-facing descriptive message for this specific occurrence.
- `code` (string, problem extension): Unique uppercase snake_case machine-readable identifier (e.g. `RESOURCE_NOT_FOUND`, `AUTH_INVALID_CREDENTIALS`), carried as a problem-details extension member so clients retain a stable error identifier.

Additional extension members MAY carry structured metadata (for example, push delivery counts) alongside `code`. There SHALL NOT be a separate top-level `error` string or a top-level `details` object; the human-readable message is `detail` and structured metadata rides in problem extensions. Error responses SHALL be served with `Content-Type: application/problem+json`.

#### Scenario: Endpoint returns bad request error
- **WHEN** client sends an invalid request
- **THEN** server returns HTTP 400 with `Content-Type: application/problem+json` and a body containing `"status": 400`, a `"detail"` describing the failure, and `"code": "VALIDATION_FAILED"`.

#### Scenario: Resource not found
- **WHEN** client queries a non-existent entity
- **THEN** server returns HTTP 404 with `Content-Type: application/problem+json` and a body containing `"status": 404`, `"detail": "The requested resource was not found."`, and `"code": "RESOURCE_NOT_FOUND"`.

### Requirement: Client-Side Dynamic Error Code Localization
The web client (`useApiError` composable) SHALL resolve API error codes against the active i18n locale (`api_errors.<code>`), reading the `code` extension member from the RFC 7807 problem-details body. When the code is present in the locale dictionary, the translated text SHALL take precedence over the developer-facing problem-details `detail` message. If no match is found, the system SHALL display the problem-details `detail`, the caller-provided localized fallback message, or a generic localized error message.

#### Scenario: API returns AUTH_INVALID_CREDENTIALS with Vietnamese locale
- **WHEN** API responds with a problem-details body `{ "detail": "Invalid email or password.", "code": "AUTH_INVALID_CREDENTIALS" }` and client locale is `vi`
- **THEN** client renders toast: "Email hoặc mật khẩu không chính xác."

#### Scenario: API returns AUTH_INVALID_CREDENTIALS with English locale
- **WHEN** API responds with a problem-details body `{ "detail": "Invalid email or password.", "code": "AUTH_INVALID_CREDENTIALS" }` and client locale is `en`
- **THEN** client renders toast: "Invalid email or password."

#### Scenario: API returns error code with matching translation in locale dictionary
- **WHEN** API responds with a problem-details body `{ "detail": "Push subscription has expired.", "code": "PUSH_SUBSCRIPTION_EXPIRED" }`
- **AND** the active locale is Vietnamese (`vi`)
- **THEN** `formatError` returns the localized message `"Đăng ký thông báo đẩy đã hết hạn. Vui lòng tắt và bật lại thông báo để làm mới."` (`api_errors.PUSH_SUBSCRIPTION_EXPIRED`) instead of the `detail` string.

#### Scenario: API returns error code without translation in locale dictionary
- **WHEN** API responds with a problem-details body `{ "detail": "Something went wrong.", "code": "UNKNOWN_ERROR_CODE" }`
- **AND** no matching entry exists in `api_errors`
- **THEN** `formatError` falls back to the problem-details `detail` or the caller-provided `fallbackKey`.
