# Auth Spec Delta

## ADDED Requirements

### Requirement: Server-enforced account and audit administration permission
Account administration SHALL require a valid authenticated identity and server-verified account-management permission. Reading administrative history SHALL require server-verified audit-read permission. Anonymous requests SHALL receive 401; authenticated callers without the required permission SHALL receive 403. Client role fields or cached capabilities SHALL NOT grant access.

#### Scenario: Learner calls account or audit APIs directly
- **WHEN** an authenticated learner requests account details, submits suspension or reads audit events without the required permission
- **THEN** the server returns 403 without disclosing administrative data or changing state.

#### Scenario: Guest calls account or audit APIs
- **WHEN** a caller without valid authentication requests account administration or audit history
- **THEN** the server returns 401.

### Requirement: Active account authentication admission
Only an existing Active, non-deleted account SHALL establish or renew an authenticated session or use protected APIs. Suspended accounts SHALL be blocked across password and Google sign-in, refresh and access-token authentication. Reactivation SHALL require fresh sign-in; password recovery or OAuth linkage SHALL NOT clear suspension.

#### Scenario: Suspended account signs in with password
- **WHEN** correct password credentials belong to a Suspended account
- **THEN** sign-in returns 403 with localized code `AUTH_ACCOUNT_SUSPENDED` without issuing access or refresh tokens.

#### Scenario: Suspended account signs in with Google
- **WHEN** a valid Google identity maps to a Suspended account
- **THEN** sign-in returns 403 with `AUTH_ACCOUNT_SUSPENDED` without recreating the account or issuing a session.

#### Scenario: Suspended account presents existing credentials
- **WHEN** a Suspended account presents a previously valid access token or refresh cookie
- **THEN** the request returns 401 without protected data or replacement credentials.

#### Scenario: Password recovery while suspended
- **WHEN** a Suspended account completes the existing password recovery flow
- **THEN** the recovery flow does not reactivate the account or grant authenticated access.

### Requirement: Server-verifiable session revocation
Protected API authentication SHALL verify the presented access token against current server account/session state, in addition to existing signature, issuer, audience and expiry checks. Suspension and administrator all-device sign-out SHALL invalidate previously issued access tokens and all refresh families after commit. Normal per-device logout SHALL retain its existing isolation.

#### Scenario: Access token has not expired
- **WHEN** an account presents an unexpired, correctly signed access token issued before a committed all-device sign-out
- **THEN** authentication returns 401 without allowing a learner or administrator operation.

#### Scenario: Refresh races with revocation
- **WHEN** session issuance or refresh overlaps suspension or all-device sign-out
- **THEN** no credentials based on the pre-revocation session state authorize requests after that revocation commits
- **AND** refresh grace-window handling cannot revive a revoked session.

#### Scenario: Ordinary device logout
- **WHEN** a user revokes one refresh-token family through normal logout
- **THEN** other devices' valid sessions remain usable according to existing per-family behavior.

#### Scenario: Browser receives terminal authentication rejection
- **WHEN** an old access token returns 401 and refresh is definitively rejected
- **THEN** session and privileged state are cleared and the browser redirects once to the login for its current area
- **AND** late administrative responses cannot restore account or audit data.

### Requirement: Session-version token deployment compatibility
An access token missing the required server-verifiable session version SHALL NOT authorize protected requests. An existing eligible refresh session SHALL be able to obtain a versioned replacement token while preserving its persistence mode. Missing, expired or revoked refresh sessions SHALL require sign-in rather than bypassing the new check.

#### Scenario: Existing browser can renew its token
- **WHEN** a browser holds an older access token without a session version and a valid unrevoked refresh cookie for an Active account
- **THEN** the protected request is rejected, transparent refresh obtains a versioned access token, and the request can be retried normally.

#### Scenario: Legacy token arrives through administrator SSR
- **WHEN** an administrator deep link arrives with an older access token and a refresh cookie that may renew the session
- **THEN** server rendering exposes only a neutral admission boundary until client refresh and capability verification complete
- **AND** no protected content is rendered while renewal remains uncertain.

#### Scenario: Revoked legacy session
- **WHEN** administrator sign-out revokes an account's legacy refresh families
- **THEN** neither its older access tokens nor refreshing those families restores access.

#### Scenario: Existing browser cannot renew its token
- **WHEN** a browser holds an older access token without an eligible refresh session
- **THEN** protected requests remain rejected and the normal sign-in flow is required.

### Requirement: Account authority availability handling
When current account or session state cannot be verified because of a temporary service failure, protected access and session issuance SHALL fail closed with an unavailable response rather than invalid-credential or permission-denied feedback. Clients SHALL preserve stored credentials while presenting retry, without treating uncertainty as authorization.

#### Scenario: Account state store is temporarily unavailable
- **WHEN** authentication cannot read authoritative account/session state due to a database outage
- **THEN** the server returns 503 without protected data or replacement tokens
- **AND** the client preserves credentials and offers retry instead of a forced logout.

## MODIFIED Requirements

### Requirement: Operator-controlled administrator designation
Catalog, account-management and audit-read access SHALL be assigned only by trusted deployment configuration referencing existing Active, non-deleted user identifiers. The existing configured administrator cohort SHALL receive these capabilities. An empty designation SHALL grant no administrator access. Registration, OAuth, profile updates and token refresh SHALL NOT let users choose or acquire administrator permission.

#### Scenario: No configured administrator
- **WHEN** no valid administrator identifiers are configured
- **THEN** all authenticated users are denied catalog management while published learner discovery remains usable
- **AND** account administration and audit history are also denied.

#### Scenario: Administrator removal
- **WHEN** an operator removes a user from the designation and the updated configuration is loaded
- **THEN** that user's subsequent management requests return 403 even with an unexpired access token
- **AND** account-management and audit-read requests also lose permission.

#### Scenario: Spoofed administrator field
- **WHEN** a regular user supplies administrator fields during registration or profile editing or alters browser storage
- **THEN** the user gains no management permission.

#### Scenario: Designated account is suspended
- **WHEN** a configured administrator account is Suspended
- **THEN** it receives no administrator capabilities or authenticated access until it is reactivated and signs in again.

### Requirement: Administrator capability discovery and route protection
The server SHALL expose the current user's catalog-management, account-management and audit-read capabilities. Every protected admin route SHALL verify its required capability before rendering UI or fetching data. Anonymous or denied visitors SHALL reach `/admin/login` with a validated local admin return path. Transient checks SHALL retain valid credentials and show a neutral retry state without protected content.

#### Scenario: Administrator opens management page
- **WHEN** a designated administrator opens `/admin/catalog`
- **THEN** the server confirms permission and the catalog workspace loads inside the administrator shell.

#### Scenario: Client capability becomes stale
- **WHEN** an administrator's access is revoked while their page is open
- **THEN** a denied request clears privileged data, aborts pending privileged requests, stops polling and redirects once to `/admin/login`
- **AND** the browser does not treat its cached capability as authorization.

#### Scenario: Non-administrator opens management page
- **WHEN** an authenticated non-administrator visits a protected administrator route
- **THEN** the browser redirects to `/admin/login` without rendering protected SSR markup or fetching draft data
- **AND** the existing learner session remains available for switching to learning.

#### Scenario: New administration destinations
- **WHEN** a user opens `/admin/users` or `/admin/audit`
- **THEN** the required account-management or audit-read capability is verified independently of browser navigation visibility
- **AND** a learner cannot enter by spoofing cached flags or using a direct URL.

#### Scenario: Legacy catalog capability consumer
- **WHEN** an existing client requests the catalog-management capability through its current endpoint
- **THEN** the existing catalog capability contract remains available with its original permission meaning.
