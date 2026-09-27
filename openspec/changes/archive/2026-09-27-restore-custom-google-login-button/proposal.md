# Proposal

## Why

The native Google Identity Services (GSI) iframe button rendered with visual anomalies (awkward white icon container background clashing against the dark card aesthetic, misaligned text, and broken styling). Concurrently, previous attempts using transparent overlays were blocked by Google's anti-clickjacking security. Restoring the custom-styled Google Sign-In button matching the Dev-Learning Studio design system with direct `google.accounts.oauth2.initTokenClient` integration guarantees both visual elegance and 100% reliable click-to-popup authentication across all browsers and viewports.

## What Changes

- **Restore Custom Google Sign-In UI**: Reinstate the custom styled button (`bg-slate-100 dark:bg-[#202024]`, centered official Google SVG, rounded-xl borders, localized text "Đăng nhập với Google" / "Sign in with Google") without iframe overlays or third-party CSS collisions.
- **Client-Side OAuth2 Token Flow**: Switch Google Sign-In trigger from iframe button rendering to `google.accounts.oauth2.initTokenClient`, directly requesting user authorization popup via `requestAccessToken({ prompt: 'select_account' })`.
- **Backend Dual-Token Verification**: Extend `POST /api/v1/auth/google` to accept and authenticate both OAuth2 Access Tokens (via Google's `userinfo` endpoint) and standard JWT ID Tokens (via `GoogleJsonWebSignature`), ensuring seamless backward compatibility with GIS One Tap while supporting custom button popups.
- **Resilient Minimal API Dependency Resolution**: Ensure `IHttpClientFactory` is resolved via service provider with fallback to prevent test host startup failures when running API unit test suites without pre-registered client factories.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `auth`: Update requirement for Google Identity Services Client Configuration & Fallback to specify custom styled button integration via `google.accounts.oauth2` token client, alongside dual-token authentication support in the backend.

## Impact

- **Affected Systems**: `frontend/pages/login.vue`, `frontend/stores/useAuthStore.ts`, `backend/src/TechDaily.Api/Endpoints/AuthEndpoints.cs`, `openspec/specs/auth/spec.md`.
- **User Experience**: 100% aesthetic consistency with the Studio design system, zero white-square visual defects, instant responsive feedback on Google login button clicks.
- **Breaking Changes**: None. The API maintains full backward compatibility with existing ID token payloads.
