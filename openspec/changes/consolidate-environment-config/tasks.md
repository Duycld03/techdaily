# Tasks

## 1. Canonical `.env` contract & docs

- [x] 1.1 Rewrite `.env.example` to .NET-native `Section__Key` names: `Gemini__ApiKey`, `Gemini__Model`, `Jwt__Secret`, `Authentication__Google__ClientSecret`, `WebPush__PublicKey`, `WebPush__PrivateKey`, `Email__Smtp__From/Username/Password`; keep `POSTGRES_PASSWORD`, `GOOGLE_CLIENT_ID`, `NUXT_PUBLIC_GOOGLE_CLIENT_ID` as derive sources; remove `VAPID_*` and the full `ConnectionStrings__DefaultConnection` literal (composed at launch). Verify: `grep -E 'GEMINI_API_KEY|JWT_SECRET|VAPID_|GOOGLE_CLIENT_SECRET=' .env.example` returns nothing and every secret key uses `__`.
- [x] 1.2 Update the local `.env` to the same native names, add `Email__Smtp__*`, and remove `VAPID_*`/dead `WebPush__*` duplicates. Verify: `grep -E '__' .env` lists `Jwt__Secret`, `Gemini__ApiKey`, `Authentication__Google__ClientSecret`, `WebPush__PublicKey/PrivateKey`, `Email__Smtp__*`; no `VAPID_`/`GEMINI_API_KEY`/`JWT_SECRET` remain (boot in 6.1 confirms binding).
- [x] 1.3 Update `AGENTS.md` "Secrets Management" invariant (Section 2) so it names `.env` (native `Section__Key`) as the single secret source and drops the reference to `appsettings.Local.json`. Verify: `AGENTS.md` no longer states secrets reside in `appsettings.Local.json`.

## 2. Local launcher (`run-dev.sh`)

- [x] 2.1 Compose `ConnectionStrings__DefaultConnection` from `POSTGRES_PASSWORD` with `Host=localhost` in `run-dev.sh` (export after `source .env`). Verify: boot in 6.1 connects to Postgres; a masked `echo "${ConnectionStrings__DefaultConnection%%Password=*}Password=***"` shows `Host=localhost`.
- [x] 2.2 Keep the derivation of `Authentication__Google__ClientId` and `NUXT_PUBLIC_GOOGLE_CLIENT_ID` from `GOOGLE_CLIENT_ID`; remove the now-obsolete `Authentication__Google__ClientSecret` bridge line (the secret is native in `.env`). Verify: after `source .env` + the launcher exports, all three Google keys are set (masked print) and no bridge references `GOOGLE_CLIENT_SECRET`.

## 3. Backend config surface & code cutover

- [x] 3.1 Strip secret values in `backend/src/TechDaily.Api/appsettings.json` to empty placeholders; retain non-secret defaults (`Email:Smtp:Host/Port/UseStartTls`, `Jwt:Issuer/Audience/ExpiryMinutes`, `Gemini:Model/EmbeddingModel/UseOfflineMock`, `WebPush:Subject`, `AllowedHosts`); dev `Logging` verbosity moved to `.env` (`Logging__LogLevel__Default=Debug`) so prod stays at Information. Verify: a key-path dump shows every secret leaf empty and dev boots at Debug via `.env`.
- [x] 3.2 Back up then delete `appsettings.Development.json` and `appsettings.Local.json` (both gitignored, not in git history); remove their now-dead `AddJsonFile` lines from `Program.cs`. Verify: both files absent; `dotnet build` succeeds and the API boots reading secrets only from `.env`.
- [x] 3.3 Remove the `?? configuration["VAPID_PUBLIC_KEY"]`/`?? configuration["VAPID_PRIVATE_KEY"]` fallbacks in `backend/src/TechDaily.Api/Program.cs` (~L61-62) and `backend/src/TechDaily.Infrastructure/Services/WebPushService.cs` (~L35-36), reading only `WebPush:PublicKey`/`WebPush:PrivateKey`. Verify: `grep -rn 'VAPID_' backend/src` returns nothing and `dotnet build` succeeds.
- [x] 3.4 Run `dotnet test` (from `backend/`). Verify: full suite passes 100% (config-sourcing change must not regress existing tests).

## 4. Production compose

- [x] 4.1 In `docker-compose.prod.yml`, add `env_file: .env` to the `backend` service; reduce its `environment:` to env-specific/computed values only (`ASPNETCORE_ENVIRONMENT`, composed `ConnectionStrings__DefaultConnection` with `Host=db`, `Cors__AllowedOrigins__0`); drop the per-secret `${VAR}` maps now covered by `env_file` (including the `WebPush__* ← VAPID_*` maps). Verify: `docker compose -f docker-compose.prod.yml config` renders the backend env with `Jwt__Secret`, `Gemini__ApiKey`, `Authentication__Google__ClientSecret`, `WebPush__PrivateKey`, and `Email__Smtp__Password` present and no `VAPID_*` key.
- [x] 4.2 Confirm `docker-compose.yml` (local `db` service) still resolves `POSTGRES_PASSWORD` from `.env` after the rename. Verify: `docker compose config` is valid and `docker compose up -d db` reports the container healthy.

## 5. Deployment cutover (VPS)

- [ ] 5.1 Update the VPS `.env` (`/home/truongduy2003/techdaily/.env`) to the native names, add `Email__Smtp__*`, and remove dead `WebPush__*`/`VAPID_*` duplicates — before the next deploy. Verify: over SSH, `docker compose -f docker-compose.prod.yml config` on the VPS renders `Jwt__Secret`, `Gemini__ApiKey`, and `Email__Smtp__Password` in the backend env with non-empty resolved values.

## 6. Integration verification

- [x] 6.1 Local end-to-end: start the stack (`run-dev.sh`) with the secret JSON files deleted; confirm the API boots from `.env` only, `POST /api/v1/auth/login` succeeds (Jwt from `.env`), a Gemini-backed request works, `GET /api/v1/notifications/push/vapid-public-key` returns the configured key, and an OTP email dispatches via SMTP. Verify: all five observables pass.
- [ ] 6.2 Production smoke after deploy: trigger an OTP email in production and confirm delivery (the SMTP gap this change closes), then `docker compose restart nginx` per the ops rule. Verify: the registration/reset OTP email is received in production.
