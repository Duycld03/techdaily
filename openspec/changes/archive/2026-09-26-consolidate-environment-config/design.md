# Design

## Context

See `proposal.md` for motivation. Grounding facts from the current tree:

- Config precedence (`backend/src/TechDaily.Api/Program.cs:22-26`): `appsettings.json` < `appsettings.Development.json` < `appsettings.Local.json` < environment variables (env wins).
- `.NET` binds env vars to config keys only via the double-underscore form (`Section__Key` → `Section:Key`). Flat names like `GEMINI_API_KEY`/`JWT_SECRET` **do not** map to `Gemini:ApiKey`/`Jwt:Secret`.
- Local (`run-dev.sh`): `set -a; source .env`, then it already bridges `Authentication__Google__ClientId/Secret` and `NUXT_PUBLIC_GOOGLE_CLIENT_ID` from `GOOGLE_CLIENT_ID`. Backend runs `ASPNETCORE_ENVIRONMENT=Development`.
- Prod (`docker-compose.prod.yml:38-50`): backend `environment:` maps `${VAR}` → `Section__Key`; the container image bakes only `appsettings.json` (Local/Development are gitignored, absent from the image).
- Committed `appsettings.json` secret leaves are already empty (only `ConnectionStrings:DefaultConnection` is a placeholder) — no secret is committed today.
- Backend accepts a legacy fallback `configuration["WebPush:PublicKey"] ?? configuration["VAPID_PUBLIC_KEY"]` in `Program.cs:61-62` and `WebPushService.cs:35-36`.
- Frontend obtains the VAPID public key from `GET /api/v1/notifications/push/vapid-public-key` (`useWebPush.ts:71`), **not** from an env var.

## Goals / Non-Goals

**Goals:**
- One canonical config convention (.NET-native `Section__Key`) and one secret source (`.env`) per environment.
- Adding a new secret touches exactly one file per environment (`.env`); no per-key translation table.
- Close the production SMTP gap as a natural consequence, not a special case.

**Non-Goals:**
- No new runtime validation beyond the existing startup fail-fast (`auth` spec). Not adding fail-fast for Gemini/SMTP in this change.
- No change to API surface, database, frontend runtime, or the VAPID public-key delivery endpoint.
- Not fixing the unrelated stale spec key name `WebPush:VapidPublicKey` in `core-platform` (out of scope).
- No secret rotation.

## Decisions

### Target key map (both environments)

| .NET config key | Old local source | Old prod source | New source |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | `.env` native + Local.json + Dev.json (dup) | compose, composed from `POSTGRES_PASSWORD` | **composed at launch** from `POSTGRES_PASSWORD` (run-dev: `Host=localhost`; compose: `Host=db`) |
| `Gemini:ApiKey` | Local.json (`GEMINI_API_KEY` dead) | compose `${GEMINI_API_KEY}` | `.env` `Gemini__ApiKey` |
| `Jwt:Secret` | Local.json (`JWT_SECRET` dead) | compose `${JWT_SECRET}` | `.env` `Jwt__Secret` |
| `Authentication:Google:ClientId` | run-dev bridge ← `GOOGLE_CLIENT_ID` | compose `${GOOGLE_CLIENT_ID}` | **derived** from `GOOGLE_CLIENT_ID` (run-dev + compose) |
| `Authentication:Google:ClientSecret` | run-dev bridge + Local.json | compose `${GOOGLE_CLIENT_SECRET}` | `.env` `Authentication__Google__ClientSecret` |
| `WebPush:PrivateKey` / `WebPush:PublicKey` | `.env` native (+ dead `VAPID_*`) | compose ← `${VAPID_*}` | `.env` `WebPush__PrivateKey` / `WebPush__PublicKey` |
| `Email:Smtp:From/Username/Password` | Local.json only | **missing (bug)** | `.env` `Email__Smtp__*` |
| `Email:Smtp:Host/Port/UseStartTls` | appsettings.json | appsettings.json | appsettings.json (non-secret default) |
| `Gemini:Model`, `Jwt:Issuer/Audience/ExpiryMinutes`, `Gemini:UseOfflineMock`, `WebPush:Subject` | appsettings.json | appsettings.json | appsettings.json default, `.env`-overridable |
| `Cors:AllowedOrigins:0` | dev default | compose `${CORS_ALLOWED_ORIGINS}` | env-specific: compose `environment:` (prod), appsettings/dev default (local) |
| `POSTGRES_PASSWORD`, `GOOGLE_CLIENT_ID`, `NUXT_PUBLIC_*` | `.env` | `.env` | `.env` (single truth; derive helpers) |

### D1 — Canonical convention: .NET-native `Section__Key`
Only native names bind without a translation layer; production already uses them; `.env.example` already uses them for `ConnectionStrings`/`Email`. **Alternative** (extend the `run-dev.sh` bridge for every flat key) rejected: it grows an unbounded translation table and keeps two formats alive.

### D2 — `.env` is the single secret source; delete the secret JSON files
Delete `appsettings.Development.json` + `appsettings.Local.json`; committed `appsettings.json` keeps only non-secret defaults + empty secret placeholders. Production already reads only `.env`; local was the outlier. Empty placeholders preserve the existing startup fail-fast for `Jwt:Secret`/`WebPush:*`. **Alternative** (keep JSON for local) rejected: perpetuates the dual mechanism and the "works locally, missing in prod" failure.

### D3 — Connection string composed at launch from `POSTGRES_PASSWORD`
The only env-specific part is `Host` (`localhost` vs `db`), which the launcher already knows. `.env` stores the password once (`POSTGRES_PASSWORD`, also consumed by the `db` container); run-dev and compose each compose the full string. **Alternative** (full literal `ConnectionStrings__DefaultConnection` in each `.env`) rejected: the password would then exist in two forms in the same file.

### D4 — Google Client ID derived from one `GOOGLE_CLIENT_ID`
The value feeds two consumers with different required names — backend `Authentication__Google__ClientId`, frontend `NUXT_PUBLIC_GOOGLE_CLIENT_ID`. A single canonical `GOOGLE_CLIENT_ID` + launcher derivation avoids writing the same value twice and preserves the `auth` spec's dual-name resolution requirement.

### D5 — Standardize Web Push on `WebPush__*`; remove the `VAPID_*` fallback
Once `.env` uses native `WebPush__PublicKey`/`WebPush__PrivateKey`, the `?? configuration["VAPID_*"]` fallback is dead code. Remove it from `Program.cs` and `WebPushService.cs` and drop `VAPID_*` from `.env` (clean cutover). Frontend is unaffected (it fetches the key from the API).

### D6 — Prod injection via `env_file: .env` + minimal `environment:`
Add `env_file: .env` to the backend service for the secret block; reduce `environment:` to values that genuinely differ per environment or are computed: `ASPNETCORE_ENVIRONMENT`, the composed `ConnectionStrings__DefaultConnection`, `Cors__AllowedOrigins__0`, `NUXT_PUBLIC_*`, `API_INTERNAL_URL`.

## Risks / Trade-offs

- **VPS `.env` still on old flat names after cutover → app boots with empty Gemini/SMTP secrets** → Migration requires updating the VPS `.env` to native names + `Email__Smtp__*` before the next deploy. Startup fail-fast catches `Jwt`/`WebPush`; Gemini/SMTP have no fail-fast, so verify by smoke after deploy.
- **Composed connection string / VAPID private key leaking into logs** → the `refresh-tokens` *Credential logging prohibition* requirement still holds; the launcher must not echo composed values.
- **`env_file: .env` injects helper keys (`POSTGRES_PASSWORD`, `GOOGLE_CLIENT_ID`) into the backend container** → harmless: the config binder ignores non-matching names, and `POSTGRES_PASSWORD` is needed to compose the connection string anyway.
- **`appsettings.Development.json` also held dev Logging levels** → move the needed non-secret dev logging defaults into `appsettings.json` (which already has a `Logging` section) so verbosity is unchanged.

## Migration Plan

1. Rewrite `.env.example` to the canonical native names (reference for both environments).
2. Update local `.env`: native secret names; keep `POSTGRES_PASSWORD` + `GOOGLE_CLIENT_ID` as derive sources; add `Email__Smtp__*`; remove `VAPID_*`.
3. Update `run-dev.sh`: compose `ConnectionStrings__DefaultConnection` from `POSTGRES_PASSWORD` (`Host=localhost`); keep/extend derivation of `Authentication__Google__ClientId` and `NUXT_PUBLIC_GOOGLE_CLIENT_ID` from `GOOGLE_CLIENT_ID`.
4. Strip secrets from `appsettings.json` to empty placeholders; retain non-secret defaults (SMTP `Host/Port/UseStartTls`, `Jwt:Issuer/Audience/ExpiryMinutes`, `Gemini:Model/EmbeddingModel/UseOfflineMock`, `WebPush:Subject`, `Logging`, `AllowedHosts`).
5. Delete `appsettings.Development.json` + `appsettings.Local.json` (back them up first — they are gitignored, so not recoverable from git history).
6. Remove the `?? VAPID_*` fallback in `Program.cs` and `WebPushService.cs`.
7. Update `docker-compose.prod.yml`: add `env_file: .env` to backend; reduce `environment:` to env-specific/computed values; ensure `Email__Smtp__*` flows via `env_file`.
8. Update the VPS `.env` to native names + `Email__Smtp__*`, drop dead `WebPush__*`/`VAPID_*`, **before deploy**.
9. Verify: local `run-dev.sh` boots and login / Gemini / Web Push / SMTP work; prod smoke after deploy (`docker compose restart nginx` per repo ops rule).

**Rollback:** revert the commit for code/compose/appsettings. Because the deleted JSON files are gitignored (not in history), restore them from the pre-change backup taken in step 5.

## Open Questions

- Whether non-secret tunables (`Gemini:Model`, `Jwt:ExpiryMinutes`) should live in `.env` or stay as `appsettings.json` defaults is deferrable — native `.env` override works either way and it does not affect the approach or task breakdown. Default: keep them as `appsettings.json` defaults, overridable by `.env` when needed.
