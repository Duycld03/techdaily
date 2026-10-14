# Tasks

## 1. Infrastructure (1 GB Production Docker Profile)

- [x] 1.1 Create `docker-compose.1gb.yml` with low-memory PostgreSQL parameters (`shared_buffers=128MB`, `work_mem=4MB`, `maintenance_work_mem=64MB`, `effective_cache_size=512MB`, `wal_buffers=8MB`), verifying syntax via `docker compose -f docker-compose.1gb.yml config`.
- [x] 1.2 Add .NET 10 Workstation GC (`DOTNET_gcServer=0`) and GC heap ceiling (`DOTNET_GCHeapHardLimit=268435456`) to the `backend` service in `docker-compose.1gb.yml`.
- [x] 1.3 Add Node.js memory ceiling (`NODE_OPTIONS=--max-old-space-size=192`) to `frontend` and enforce Docker cgroup limits (`db: 220M`, `backend: 300M`, `frontend: 150M`, `nginx: 30M`) in `docker-compose.1gb.yml`.
- [x] 1.4 Document the 2 GB host swap setup procedure and migration deployment instructions for 1 GB cloud hosts.

## 2. Frontend Development Environment Hardening

- [x] 2.1 Update `run-dev.sh` to export `NODE_OPTIONS="--max-old-space-size=4096"` prior to starting the Nuxt 4 development server, verifying that the environment variable propagates to child worker processes.
- [x] 2.2 Verify local dev startup and test execution (`npm test`), confirming zero regressions and clean startup.
