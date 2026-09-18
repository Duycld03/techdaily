# Spec Delta: today

## MODIFIED Requirements

### Requirement: Strict Authentication on Today Page
The `/today` focus studio SHALL require an authenticated user session. Unauthenticated requests to `/today` SHALL redirect to `/login?redirect=/today`. The backend endpoint `GET /api/v1/daily/today` SHALL require authorization and return HTTP 401 Unauthorized when requested without a valid JWT token.

#### Scenario: Unauthenticated visitor visits /today
- **WHEN** unauthenticated visitor navigates to `/today`
- **THEN** route middleware redirects to `/login` with redirect query parameter `/today`.

#### Scenario: Unauthenticated visitor visits root url /
- **WHEN** unauthenticated visitor navigates to `/`
- **THEN** route middleware redirects to `/login` with redirect query parameter `/`.

#### Scenario: Unauthenticated API request to /api/v1/daily/today
- **WHEN** unauthenticated request is sent to `GET /api/v1/daily/today`
- **THEN** server returns HTTP 401 Unauthorized.

## REMOVED Requirements

### Requirement: Bento Grid Dashboard & Concentric Learning Metrics
This requirement has been relocated to `core-platform` under the Home Command Center Dashboard (`/`), dedicating `/today` purely to the distraction-free Focus Studio reading and scenario workspace.
