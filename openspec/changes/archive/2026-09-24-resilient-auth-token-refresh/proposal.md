# Proposal: Resilient Auth Token Refresh Lifecycle

## Why

Users are experiencing frequent unexpected logouts and being forced to re-authenticate multiple times during normal usage. This occurs because the global Nuxt route middleware (`auth.global.ts`) and Pinia auth store (`useAuthStore.ts`) synchronously check JWT expiration and immediately purge client sessions without attempting token refresh, while the backend unconditionally enforces `Secure=true` on HttpOnly refresh token cookies even over plain HTTP in local/LAN development environments.

## What Changes

- **Non-Destructive Store Initialization (`useAuthStore.ts`)**: Stop unconditionally calling `clearSession()` on expired tokens during synchronous store initialization (`init()`). Preserve stored session state while marking the token as requiring refresh.
- **Asynchronous Token Refresh in Global Route Middleware (`auth.global.ts`)**: Enhance route middleware to asynchronously attempt a refresh via `authStore.tryRefreshToken()` when a route requires authentication and the access token is expired, before falling back to session purging and `/login` redirection.
- **Dedicated Store Refresh Action (`tryRefreshToken`)**: Expose a clear, concurrency-guarded `tryRefreshToken()` action in `useAuthStore` that coordinates with `useApiClient` and the Web Locks API (`navigator.locks`).
- **Environment-Aware Cookie Security (`AuthEndpoints.cs`)**: Dynamically set the `Secure` attribute on HttpOnly `refreshToken` cookies based on request protocol (`context.Request.IsHttps` or `X-Forwarded-Proto == "https"`), fulfilling the OpenSpec invariant for HTTP development environments while maintaining strict HTTPS security in production.
- **Configurable Access Token Expiration**: Bind JWT access token lifetime to `Jwt:ExpiryMinutes` configuration rather than a hardcoded literal, ensuring flexible operation in development and production environments.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `refresh-tokens`: Update requirements for frontend route middleware lifecycle, proactive refresh coordination, and protocol-aware cookie flags to ensure zero disruptive logouts during active user sessions.
