# Design

## Context

See `proposal.md` for background and motivation.
Google Identity Services offers two paradigms:
1. `google.accounts.id`: Renders standard iframe buttons (`renderButton`) or One Tap prompts (`prompt`). Directly customizing the button styling inside the iframe is forbidden by Google, and placing transparent overlays on top of the iframe triggers Google's anti-clickjacking defense, invalidating click events.
2. `google.accounts.oauth2`: Provides programmatic token clients (`initTokenClient`) designed specifically for custom HTML/CSS buttons that call `requestAccessToken()` on direct user interaction.

## Goals / Non-Goals

**Goals:**
- Provide a pixel-perfect, custom-styled Google button that conforms to the Dev-Learning Studio UI (`bg-slate-100 dark:bg-[#202024]`, centered Google SVG icon, localized label).
- Trigger Google's OAuth2 account selection dialog on user click without cross-origin iframe styling limitations or clickjacking event cancellations.
- Expand `POST /api/v1/auth/google` in ASP.NET Core to support both OAuth2 Access Tokens (`ya29...`) and JWT ID Tokens (`eyJ...`), ensuring complete interoperability with existing mobile/web flows.
- Ensure all backend unit tests pass without requiring explicit `IHttpClientFactory` registrations in mock test hosts.

**Non-Goals:**
- Replacing existing email/password or OTP authentication flows.
- Implementing Google Drive or Calendar OAuth scopes (only `openid email profile` scopes are requested).

## Decisions

### 1. Client-Side Authentication Engine: `google.accounts.oauth2.initTokenClient`
- **Decision:** Initialize `google.accounts.oauth2.initTokenClient` in `frontend/pages/login.vue` with `client_id`, `scope: 'openid email profile'`, and a callback handler.
- **Rationale:** Unlike `google.accounts.id.renderButton` which forces an iframe with native styling (causing white icon background clash and text displacement in custom dark themes), `initTokenClient` allows binding a native `<button @click="triggerGoogleSignIn">` that fires `tokenClient.requestAccessToken({ prompt: 'select_account' })`.
- **Alternative Considered:** Overlaying a transparent iframe with CSS opacity tricks. Rejected because modern Chromium and Google GSI explicitly block pointer events when opacity < 1 or when dimensions do not match iframe specifications.

### 2. Backend Dual-Token Acceptance in `POST /api/v1/auth/google`
- **Decision:** Support both `IdToken` and `AccessToken` in `GoogleAuthRequest(string? IdToken = null, string? AccessToken = null)`. Detect token type:
  - If token starts with `ya29.` or does not contain `.` (OAuth2 Access Token), request user profile from `https://www.googleapis.com/oauth2/v3/userinfo` with `Bearer <token>`.
  - If token contains JWT segments (ID Token), validate with `GoogleJsonWebSignature.ValidateAsync`.
- **Rationale:** Guarantees backward compatibility with any ID token clients while effortlessly supporting the new Access Token flow.

### 3. Service-Locating `IHttpClientFactory` in Endpoint Delegate
- **Decision:** Resolve `IHttpClientFactory` from `HttpContext.RequestServices.GetService<IHttpClientFactory>()` with fallback to `new HttpClient()` rather than declaring it as a required parameter in `MapPost`.
- **Rationale:** Minimal API route endpoints that declare unconfigured services throw `InvalidOperationException` during route table compilation in isolated test hosts (e.g. `AuthEndpointsTests`). Service location maintains DI purity in production while keeping unit test suites green.

## Risks / Trade-offs

- **Risk:** Popups blocked by aggressive browser popup blockers.
  - **Mitigation:** `requestAccessToken` is executed synchronously in direct response to user button click (`@click`), which modern browsers classify as trusted user interaction and permit popups.
- **Risk:** Google userinfo endpoint availability.
  - **Mitigation:** Google OAuth2 `userinfo` SLA matches Google identity infrastructure with 99.99%+ availability.
