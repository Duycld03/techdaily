# Design

## Context

DeepPace's production stack runs via Docker Compose on a Linux VPS hosting 4 containers: `db` (PostgreSQL 17 with pgvector), `backend` (.NET 10 Minimal APIs), `frontend` (Nuxt 4 / Node.js Nitro), and `nginx` (reverse proxy with Let's Encrypt SSL termination). The live production host is an 8 GB RAM instance (`n4-standard-2`) where PostgreSQL runs with `shared_buffers=1024MB` and .NET utilizes Server GC at full capacity.

The project plans to eventually migrate to resource-constrained virtual machines (such as Google Cloud's Always Free `e2-micro`, which provides 2 shared vCPUs and 1.0 GB RAM). Forcing 1 GB constraints onto the existing `docker-compose.prod.yml` would degrade the performance of the current 8 GB server unnecessarily. Instead, a dual-profile strategy preserves high performance on the 8 GB host while maintaining a dedicated 1 GB profile.

Additionally, the local development script (`run-dev.sh`) launches `nuxt dev` without `NODE_OPTIONS`, causing Vite and Nitro worker threads to trigger `ERR_WORKER_OUT_OF_MEMORY` during extended development sessions.

## Goals / Non-Goals

**Goals:**
- Preserve `docker-compose.prod.yml` unchanged for standard/high-performance hosts (8 GB+ RAM).
- Create a dedicated `docker-compose.1gb.yml` file tuned specifically for 1.0 GB RAM machines:
  - Database: `shared_buffers=128MB`, `work_mem=4MB`, `maintenance_work_mem=64MB`, `effective_cache_size=512MB`.
  - Backend: Workstation GC (`DOTNET_gcServer=0`) and a 256 MB hard heap limit (`DOTNET_GCHeapHardLimit=268435456`).
  - Frontend: 192 MB Node heap cap (`NODE_OPTIONS="--max-old-space-size=192"`).
  - Cgroup Caps: Hard limits totaling ~700 MB across all containers, leaving 300+ MB for OS, kernel, and Docker daemon.
- Configure `run-dev.sh` to export `NODE_OPTIONS="--max-old-space-size=4096"` to eliminate local dev worker OOM crashes.
- Document the 2 GB host swap setup protocol for 1 GB cloud deployments.

**Non-Goals:**
- Modifying `docker-compose.prod.yml` or degrading the live 8 GB VPS.
- Compiling code on the VPS (CI/CD already compiles on GitHub Actions runners).
- Business logic or application code changes.

## Decisions

### Decision 1: Dedicated Profile (`docker-compose.1gb.yml`) over In-Place Throttling
- **Choice**: Introduce `docker-compose.1gb.yml` rather than modifying `docker-compose.prod.yml`.
- **Rationale**: The live VPS has 8 GB of RAM and 2 full Xeon vCPUs. Keeping `docker-compose.prod.yml` intact ensures the current production environment continues operating at peak throughput. When migrating to a 1 GB host in the future, the operator simply references `docker-compose.1gb.yml`.
- **Alternatives Considered**:
  - *Overwriting `docker-compose.prod.yml`*: Throttles the 8 GB VPS unnecessarily.
  - *Complex environment variable interpolation*: Harder to maintain cgroup limits and PostgreSQL command arguments cleanly.

### Decision 2: Workstation GC on the 1 GB Profile
- **Choice**: Set `DOTNET_gcServer=0` and `DOTNET_GCHeapHardLimit=268435456` in `docker-compose.1gb.yml`.
- **Rationale**: Server GC creates dedicated heaps per core, consuming significant base memory. Workstation GC uses a single shared heap with smaller segments, keeping base .NET memory within ~120–150 MB.

### Decision 3: 128 MB PostgreSQL Shared Buffers for 1 GB Hosts
- **Choice**: Set `shared_buffers=128MB` and `effective_cache_size=512MB` in `docker-compose.1gb.yml`.
- **Rationale**: With the database size at ~67 MB, 128 MB caches the entire active dataset in memory while staying safely within the 220 MB container cgroup limit.

### Decision 4: Local Dev Heap Hardening (`NODE_OPTIONS=--max-old-space-size=4096`)
- **Choice**: Export `NODE_OPTIONS="--max-old-space-size=4096"` in `run-dev.sh`.
- **Rationale**: Local dev machines have 16+ GB RAM. Giving Node 4 GB prevents Vite HMR AST caches and Shiki syntax tables from triggering `ERR_WORKER_OUT_OF_MEMORY`.

## Risks / Trade-offs

- **[Risk] Maintaining two compose files**:
  → *Mitigation*: Both files share identical service names, environment keys, and volumes; the only differences are memory arguments, GC flags, and cgroup limits.
- **[Risk] Swap thrashing on 1 GB hosts**:
  → *Mitigation*: Capping total container memory at 700 MB leaves 300 MB of physical RAM headroom. The 2 GB swap acts strictly as a shock absorber.
