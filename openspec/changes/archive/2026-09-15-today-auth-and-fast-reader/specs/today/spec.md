## ADDED Requirements

### Requirement: Strict Authentication on Today Page
The `/today` hub and the root navigation redirect route `/` SHALL require an authenticated user session. Unauthenticated requests to `/today` or `/` SHALL redirect to `/login?redirect=/today`. The backend endpoint `GET /api/v1/daily/today` SHALL require authorization and return HTTP 401 Unauthorized when requested without a valid JWT token.

#### Scenario: Unauthenticated visitor visits /today
- **WHEN** unauthenticated visitor navigates to `/today`
- **THEN** route middleware redirects to `/login` with redirect query parameter `/today`.

#### Scenario: Unauthenticated visitor visits root url /
- **WHEN** unauthenticated visitor navigates to `/`
- **THEN** route middleware redirects to `/login` with redirect query parameter `/today`.

#### Scenario: Unauthenticated API request to /api/v1/daily/today
- **WHEN** unauthenticated request is sent to `GET /api/v1/daily/today`
- **THEN** server returns HTTP 401 Unauthorized.
