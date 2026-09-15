## ADDED Requirements

### Requirement: Route Middleware Token Expiry Validation
The client-side route guard SHALL check token validity against expiration time before allowing access to protected routes or redirecting away from guest-only pages like `/login`. A raw unvalidated cookie string SHALL NOT be considered proof of active authentication.

#### Scenario: User visits /login with expired token
- **WHEN** visitor navigates to `/login` with an expired token cookie
- **THEN** middleware recognizes the token as expired, clears session, and allows the visitor to stay on `/login` without redirecting back to `/today`.

#### Scenario: Active logged-in user visits /login
- **WHEN** authenticated user with a valid non-expired token navigates to `/login`
- **THEN** middleware redirects to `/today`.
