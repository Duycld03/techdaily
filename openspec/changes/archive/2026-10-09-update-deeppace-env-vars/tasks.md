# Tasks

## 1. Environment Variable Template & Documentation

- [x] 1.1 Update `.env.example` with DeepPace branding, clear section headers, and comprehensive coverage across all 9 functional areas.
- [x] 1.2 Update `.env.example` database variables (`POSTGRES_DB=deeppace_db`, `POSTGRES_USER=deeppace_user`).
- [x] 1.3 Update `.env.example` JWT configuration (`Jwt__Issuer=DeepPace`, `Jwt__Audience=DeepPaceUsers`).
- [x] 1.4 Update `.env.example` CORS default to `CORS_ALLOWED_ORIGINS=https://deeppace.duckdns.org,https://techdaily.duckdns.org`.
- [x] 1.5 Update Web Push contact subject in `.env.example` to `WebPush__Subject=mailto:support@deeppace.app`.

## 2. Local Development Configuration Alignment

- [x] 2.1 Audit and synchronize local `.env` to ensure all DeepPace keys are present, matching `.env.example`.
- [x] 2.2 Verify that `./run-dev.sh` boots the local development environment cleanly without missing variable warnings.

## 3. Production VPS Deployment Alignment

- [x] 3.1 Document the VPS production `.env` template with production CORS, connection strings, and Google OAuth credentials.
- [x] 3.2 Document zero-downtime database rename commands for existing VPS PostgreSQL volumes (`ALTER DATABASE` and `ALTER USER`).
- [x] 3.3 Validate `docker compose -f docker-compose.prod.yml config` parses all environment substitutions cleanly.

## 4. Production Database Data Restoration & Migration Script

- [x] 4.1 Create `scripts/restore-prod-database.sh` with automated volume detection for `techdaily_pgdata_prod` and `deeppace_pgdata_prod`.
- [x] 4.2 Implement automated dump-and-restore pipeline transferring database records from `techdaily_db` to `deeppace_db`.
- [x] 4.3 Add post-restoration verification querying table counts for `Users`, `DocumentBooks`, `DocumentChunks`, and `SpacedRepetitionCards`.
- [x] 4.4 Set executable permissions (`chmod +x scripts/restore-prod-database.sh`).
