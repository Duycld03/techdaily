# Proposal

## Why

Secrets and configuration are scattered across **two naming conventions** and **two delivery mechanisms**, so the same value is defined in up to four places and drifts silently:

- **Two conventions in one `.env`**: flat `SCREAMING_CASE` (`GEMINI_API_KEY`, `JWT_SECRET`) vs .NET-native `Section__Key` (`ConnectionStrings__DefaultConnection`, `WebPush__PrivateKey`). The flat names **do not bind** to .NET configuration keys (`Gemini:ApiKey`, `Jwt:Secret`), so locally they are dead decoration.
- **Two mechanisms**: locally the app actually reads secrets from gitignored `appsettings.Development.json` **and** `appsettings.Local.json` (near-duplicate, 14 keys each); in production the container reads secrets only from `.env` via `docker-compose.prod.yml` `environment:` maps.

This drift already caused a **production gap**: `Email__Smtp__*` was added to `appsettings.Local.json` for local OTP/password-reset email, but never to the prod compose file — so production currently has **no SMTP configuration**. The pattern guarantees "works locally, missing in prod" every time a new secret is added.

## What Changes

- **Single source of truth**: all secrets live in one `.env` per environment using .NET-native `Section__Key` names; `run-dev.sh` `source .env` binds them directly, matching how production already consumes `.env`.
- **Remove secret-bearing JSON**: delete gitignored `appsettings.Development.json` and `appsettings.Local.json`. Committed `appsettings.json` keeps only non-secret defaults and empty placeholders.
- **Connection string (single password)**: `.env` holds only `POSTGRES_PASSWORD`; `run-dev.sh` (local, `Host=localhost`) and compose (prod, `Host=db`) compose the full `ConnectionStrings__DefaultConnection` from it.
- **Google Client ID (derive)**: one canonical `GOOGLE_CLIENT_ID`, derived by `run-dev.sh`/compose into both `Authentication__Google__ClientId` (backend) and `NUXT_PUBLIC_GOOGLE_CLIENT_ID` (frontend).
- **Web Push**: standardize on native `WebPush__PublicKey`/`WebPush__PrivateKey`; drop `VAPID_*` env keys and remove the dead `?? configuration["VAPID_*"]` fallback in `Program.cs` and `WebPushService.cs` (clean cutover).
- **Rename remaining flat keys to native**: `GEMINI_API_KEY→Gemini__ApiKey`, `GEMINI_MODEL→Gemini__Model`, `JWT_SECRET→Jwt__Secret`, `GOOGLE_CLIENT_SECRET→Authentication__Google__ClientSecret`.
- **Prod injection**: `docker-compose.prod.yml` injects the secret block via `env_file: .env`; `environment:` keeps only env-specific/computed values (`ASPNETCORE_ENVIRONMENT`, composed connection string, `Cors__*`, `NUXT_PUBLIC_*`, `API_INTERNAL_URL`). This restores the missing `Email__Smtp__*` in production.
- **Docs**: update `.env.example` to the standardized native names.

No runtime behavior, API contract, or database change. This is a configuration-sourcing refactor.

## Capabilities

`skip_specs: true` is set in `.openspec.yaml`: this is a pure configuration/ops refactor with **no externally observable behavior change**. The configuration keys and behaviors that are specced remain satisfied unchanged — `auth`: *Mandatory secret configuration at startup* (`Jwt:Secret`, `WebPush:*` fail-fast), *Google Identity Services Client Configuration & Fallback* (still supports `GOOGLE_CLIENT_ID` and `NUXT_PUBLIC_GOOGLE_CLIENT_ID`), *Production CORS origin isolation*; `core-platform`: *Web Push Subscription & VAPID Infrastructure*, *Controlled Offline Mock Vector Configuration* (`Gemini:UseOfflineMock` stays in `appsettings.json`). No requirement text changes, so no delta is invented.

### New Capabilities

_None._

### Modified Capabilities

_None._

## Impact

- **Code / config files**: `.env`, `.env.example`, `run-dev.sh`, `docker-compose.prod.yml`, `docker-compose.yml`, `backend/src/TechDaily.Api/appsettings.json`; **delete** `appsettings.Development.json` + `appsettings.Local.json`; remove VAPID fallback in `backend/src/TechDaily.Api/Program.cs` and `backend/src/TechDaily.Infrastructure/Services/WebPushService.cs`.
- **Deployment (VPS `/home/truongduy2003/techdaily/.env`)**: must adopt native `Section__Key` names, add `Email__Smtp__*`, and drop dead duplicate `WebPush__*`/`VAPID_*` keys **before the next deploy**. `env_file: .env` in compose depends on these names.
- **No change** to the database, API surface, frontend runtime, or user-facing behavior.
- **Risk**: connection strings and VAPID private keys are composed at launch — must not be logged (existing `refresh-tokens` *Credential logging prohibition* requirement still holds).
