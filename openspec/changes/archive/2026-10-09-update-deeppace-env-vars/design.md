# Design

## Context

TechDaily has transitioned to DeepPace across its application code, C# projects, database containers, and domain. However, configuration templates (`.env.example`) and deployment configurations still contain legacy `techdaily` identifiers.

In this architecture:
- Local development (`run-dev.sh`) sources `.env` directly and sets up hot-reloading for ASP.NET Core and Nuxt 4.
- Production (`docker-compose.prod.yml`) injects `.env` via `env_file: .env` into `deeppace_backend_prod` and `deeppace_postgres_prod`.
- Secrets are NEVER committed; only `.env.example` with non-secret defaults and placeholders is tracked in git.

## Goals / Non-Goals

**Goals:**
- Audit and document 100% of environment variables required by DeepPace across all 9 functional areas.
- Update `.env.example` with clear comments, DeepPace identifiers, and valid placeholders.
- Provide explicit `.env` configurations tailored for:
  1. Local Development (`localhost`, `0.0.0.0`)
  2. VPS Production (`https://deeppace.duckdns.org`)
- Provide zero-data-loss database migration SQL commands for existing VPS PostgreSQL volumes.

**Non-Goals:**
- Committing real secrets to version control.
- Altering the .NET `IConfiguration` or Nuxt `runtimeConfig` binding mechanisms.

## Decisions

### 1. Canonical Variable Dictionary (9 Functional Areas)

| Section | Key Name | Local Dev Default | Production VPS Value | Purpose |
|---|---|---|---|---|
| **Database** | `POSTGRES_PASSWORD` | `<secure-password>` | `<production-password>` | PostgreSQL database password |
| | `POSTGRES_USER` | `deeppace_user` | `deeppace_user` | PostgreSQL database user |
| | `POSTGRES_DB` | `deeppace_db` | `deeppace_db` | PostgreSQL database name |
| **JWT** | `Jwt__Secret` | `<32+ char secret>` | `<64+ char secret>` | 256-bit HMAC-SHA256 signing key |
| | `Jwt__Issuer` | `DeepPace` | `DeepPace` | Token Issuer claim |
| | `Jwt__Audience` | `DeepPaceUsers` | `DeepPaceUsers` | Token Audience claim |
| | `Jwt__ExpiryMinutes` | `60` | `60` | Access token lifespan |
| **OAuth** | `GOOGLE_CLIENT_ID` | `<client-id>` | `<client-id>` | Canonical Google OAuth Client ID |
| | `NUXT_PUBLIC_GOOGLE_CLIENT_ID` | `<client-id>` | `<client-id>` | Frontend Google Sign-In button |
| | `Authentication__Google__ClientSecret` | `<client-secret>` | `<client-secret>` | Backend Google OAuth verification |
| **Frontend** | `NUXT_PUBLIC_API_BASE_URL` | `http://localhost:5000` | `""` (relative via Nginx) | Browser API call target |
| | `API_INTERNAL_URL` | N/A | `http://backend:5000` | Nitro SSR server-side proxy |
| **AI Gemini** | `Gemini__ApiKey` | `<api-key>` | `<api-key>` | Gemini 3.5 Flash Lite API key |
| | `Gemini__Model` | `gemini-3.5-flash-lite` | `gemini-3.5-flash-lite` | Text and quiz generation model |
| | `Gemini__EmbeddingModel` | `gemini-embedding-001` | `gemini-embedding-001` | 768-dim text embedding model |
| **TTS Audio** | `Google__TtsApiKey` | `<api-key>` | `<api-key>` | Neural Text-to-Speech API key |
| **Web Push** | `WebPush__PublicKey` | `<vapid-public>` | `<vapid-public>` | VAPID public key for browser push |
| | `WebPush__PrivateKey` | `<vapid-private>` | `<vapid-private>` | VAPID private key for dispatch |
| | `WebPush__Subject` | `mailto:support@deeppace.app` | `mailto:support@deeppace.app` | VAPID contact email header |
| **Email SMTP**| `Email__Smtp__From` | `<email>` | `<email>` | Transactional sender address |
| | `Email__Smtp__Username` | `<email>` | `<email>` | SMTP authentication user |
| | `Email__Smtp__Password` | `<app-password>` | `<app-password>` | SMTP app password |
| **E2E Tests** | `E2E_PROD_EMAIL` | `e2e@deeppace.app` | `e2e@deeppace.app` | Automated E2E test account |
| | `E2E_PROD_PASSWORD` | `<password>` | `<password>` | E2E test account password |
| **CORS** | `CORS_ALLOWED_ORIGINS` | N/A (Allows localhost) | `https://deeppace.duckdns.org,https://techdaily.duckdns.org` | Allowed browser origins |

### 2. VPS PostgreSQL Data Migration & Restoration Tooling (`scripts/restore-prod-database.sh`)
To guarantee zero data loss on existing VPS deployments, the automated script `scripts/restore-prod-database.sh` provides two deterministic paths:

#### Approach A: Dump & Restore Pipeline (Preserves `deeppace_pgdata_prod`)
1. Checks for the existence of Docker volume `techdaily_pgdata_prod`.
2. Launches an ephemeral, isolated container mounting `techdaily_pgdata_prod` on a temporary internal port.
3. Executes `pg_dump` with pgvector vector types and ownership remapping:
   ```bash
   docker exec temp_legacy_db pg_dump -U techdaily_user -d techdaily_db --clean --if-exists > /tmp/legacy_dump.sql
   ```
4. Pipes the SQL stream into `deeppace_postgres_prod` (`deeppace_db`):
   ```bash
   docker exec -i deeppace_postgres_prod psql -U deeppace_user -d deeppace_db < /tmp/legacy_dump.sql
   ```
5. Stops the ephemeral container and securely shreds `/tmp/legacy_dump.sql`.

#### Approach B: Direct Volume Re-attachment & In-Place Rename
If the administrator chooses to keep the legacy volume directly:
1. Set volume mount in `docker-compose.prod.yml`:
   ```yaml
   volumes:
     - techdaily_pgdata_prod:/var/lib/postgresql/data
   ```
2. Execute in-place schema renaming:
   ```sql
   ALTER DATABASE techdaily_db RENAME TO deeppace_db;
   ALTER USER techdaily_user RENAME TO deeppace_user;
   ```

### 3. Restoration Integrity Verification
The script queries key relational tables to prove data was restored:
```sql
SELECT 'Users' as table_name, count(*) FROM "Users"
UNION ALL
SELECT 'Books', count(*) FROM "DocumentBooks"
UNION ALL
SELECT 'Chunks', count(*) FROM "DocumentChunks"
UNION ALL
SELECT 'Flashcards', count(*) FROM "SpacedRepetitionCards";
```
## Risks / Trade-offs

- **[Risk] Container Connection Refused after DB Rename**:
  - *Mitigation*: Ensure `ConnectionStrings__DefaultConnection` and `POSTGRES_USER`/`POSTGRES_DB` match the database state before restarting `deeppace_backend_prod`.
