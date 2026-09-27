# Spec Delta: auth

## MODIFIED Requirements

### Requirement: Google Identity Services Client Configuration & Fallback
The web frontend application SHALL resolve the Google Client ID from runtime configuration using flexible environment variable resolution, supporting either `NUXT_PUBLIC_GOOGLE_CLIENT_ID` or `GOOGLE_CLIENT_ID`.

The system SHALL guarantee that:
1. When either `NUXT_PUBLIC_GOOGLE_CLIENT_ID` or `GOOGLE_CLIENT_ID` is set in the hosting environment (including local development runner `.env`), the frontend initializes Google OAuth2 token client (`google.accounts.oauth2.initTokenClient`) with the resolved Client ID and `openid email profile` scopes.
2. In local development environments executed via `run-dev.sh`, environment variables from `.env` are automatically propagated to the frontend development server process.
3. The Google Sign-In action SHALL render as a custom-styled button adhering to the Dev-Learning Studio design system (`bg-slate-100 dark:bg-[#202024]`, centered Google SVG icon, localized label) without iframe overlays, clipping containers, or white icon-box rendering anomalies.
4. Clicking the Google Sign-In button SHALL trigger the OAuth2 account selection popup via `tokenClient.requestAccessToken({ prompt: 'select_account' })`.
5. The backend endpoint `POST /api/v1/auth/google` SHALL accept both Google OAuth2 Access Tokens (`AccessToken`) and standard JWT ID Tokens (`IdToken`), validating Access Tokens against Google's `userinfo` endpoint and ID Tokens against Google's token signature verifier, returning an authenticated TechDaily session upon verification.
6. If neither variable is configured (empty string), the application SHALL NOT initialize Google OAuth2 services with an empty Client ID, preventing `400: invalid_request (Missing required parameter: client_id)` authorization errors.

#### Scenario: Local development environment with GOOGLE_CLIENT_ID defined in .env
- **WHEN** a developer starts the development stack with `run-dev.sh` and root `.env` defines `GOOGLE_CLIENT_ID`
- **THEN** the Nuxt frontend runtime configuration resolves `googleClientId` to the value of `GOOGLE_CLIENT_ID`
- **AND** the Google Sign-In button initializes with the configured Client ID without `client_id` missing parameter errors.

#### Scenario: Production or CI deployment with NUXT_PUBLIC_GOOGLE_CLIENT_ID defined
- **WHEN** the application runs in a container or environment where `NUXT_PUBLIC_GOOGLE_CLIENT_ID` is defined
- **THEN** the Nuxt frontend runtime configuration resolves `googleClientId` to the value of `NUXT_PUBLIC_GOOGLE_CLIENT_ID`.

#### Scenario: Environment without Google OAuth credentials configured
- **WHEN** neither `NUXT_PUBLIC_GOOGLE_CLIENT_ID` nor `GOOGLE_CLIENT_ID` is defined
- **THEN** the frontend recognizes the missing Client ID, avoids passing an empty string to Google OAuth2 initialization, and does not render a broken Google Sign-In authorization flow.

#### Scenario: User clicks custom Google Sign-In button
- **WHEN** an unauthenticated visitor clicks the custom-styled "Sign in with Google" button on `/login`
- **THEN** the system triggers Google's OAuth2 account selection popup directly
- **AND** zero iframe visual clipping or anti-clickjacking event blockage occurs.

#### Scenario: Backend authenticates OAuth2 Access Token
- **WHEN** the frontend submits an OAuth2 access token (`ya29...`) to `POST /api/v1/auth/google`
- **THEN** the server retrieves user profile information (`email`, `name`, `picture`, `sub`) from Google's userinfo endpoint
- **AND** creates or updates the user account and returns an authenticated session with JWT and refresh token cookies.

#### Scenario: Backend authenticates JWT ID Token
- **WHEN** a client submits a standard JWT ID token to `POST /api/v1/auth/google`
- **THEN** the server validates the signature and audience against Google's token validator and returns an authenticated session.
