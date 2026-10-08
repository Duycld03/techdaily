# Spec Delta: auth

## MODIFIED Requirements

### Requirement: DeepPace Client Session Persistence & Token Storage Migration
The web frontend and authentication service SHALL standardize session credentials on `deeppace_*` keys while preserving seamless backward-compatible migration for existing sessions:

1. **Cookie & LocalStorage Identifiers**:
   - The primary access token SHALL be persisted to `deeppace_token` (cookie and `localStorage`).
   - The active user profile SHALL be persisted to `deeppace_user` (cookie and `localStorage`).
   - Secondary reader and roadmap preferences SHALL use prefix `deeppace_*` (e.g. `deeppace_reader_typography`, `deeppace_roadmap_view_mode`).
2. **Transparent Session Migration**:
   - During frontend initialization (`authStore.init()`), if `deeppace_token` is absent, the client SHALL inspect legacy keys `techdaily_token` and `techdaily_user`.
   - If a valid legacy token is present, the store SHALL automatically write it to `deeppace_token` and `deeppace_user`, and clear legacy keys, preventing existing logged-in users from being forcefully logged out.
3. **JWT Configuration Invariants**:
   - The default JWT Issuer SHALL be `"DeepPace"`, and the default JWT Audience SHALL be `"DeepPaceUsers"`.

#### Scenario: Existing user loads application with legacy cookies
- **GIVEN** a browser holding `techdaily_token` and `techdaily_user` but no `deeppace_token`
- **WHEN** the application initializes on client-side mount
- **THEN** `authStore.init()` copies the credentials to `deeppace_token` and `deeppace_user`
- **AND** removes the legacy `techdaily_token` and `techdaily_user` entries
- **AND** evaluates `isLoggedIn = true` without redirecting the user to `/login`.

#### Scenario: New user signs in and receives DeepPace credentials
- **WHEN** a user logs in via `POST /api/v1/auth/login`
- **THEN** the client stores the access token into `deeppace_token`
- **AND** the token payload reflects Issuer `"DeepPace"`.
