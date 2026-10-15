# Proposal

## Why

DeepPace plans to eventually support deploying to resource-constrained cloud virtual machines (specifically 1.0 GB RAM hosts such as Google Cloud Always Free `e2-micro`). However, the existing production environment is hosted on an 8 GB RAM instance (`n4-standard-2`) where PostgreSQL is configured with `shared_buffers=1024MB` and .NET 10 uses Server GC for maximum performance. Overwriting the primary `docker-compose.prod.yml` would artificially throttle the powerful 8 GB VPS. Conversely, running the current 8 GB configuration on a 1 GB machine triggers immediate fatal out-of-memory (OOM) termination. In addition, the local development script (`run-dev.sh`) lacks explicit Node.js heap limits, causing `ERR_WORKER_OUT_OF_MEMORY` crashes in Vite and Nitro worker threads during extended coding sessions.

Introducing a dedicated `docker-compose.1gb.yml` profile leaves the primary 8 GB production stack completely untouched at full performance while providing a turnkey, OOM-safe configuration for 1 GB hosts, alongside a local dev server heap fix.

## What Changes

- **Dedicated 1 GB Production Profile (`docker-compose.1gb.yml`)**:
  - Keep `docker-compose.prod.yml` 100% untouched for high-performance (8 GB+) hosts.
  - Introduce `docker-compose.1gb.yml` specifically tuned for 1.0 GB RAM machines:
    - **PostgreSQL**: `shared_buffers=128MB`, `work_mem=4MB`, `maintenance_work_mem=64MB`, `effective_cache_size=512MB`, `wal_buffers=8MB`.
    - **Backend .NET 10**: Workstation GC (`DOTNET_gcServer=0`) and strict GC heap ceiling (`DOTNET_GCHeapHardLimit=268435456`, 256 MB).
    - **Frontend Nuxt Nitro**: `NODE_OPTIONS="--max-old-space-size=192"`.
    - **Docker Cgroup Limits**: Strict memory caps (`db: 220M`, `backend: 300M`, `frontend: 150M`, `nginx: 30M`), keeping total container memory under 750 MB and preserving 250+ MB for host OS and kernel.
- **Local Development Server Memory Hardening**:
  - Export `NODE_OPTIONS="--max-old-space-size=4096"` in `run-dev.sh` to prevent `ERR_WORKER_OUT_OF_MEMORY` crashes during local development.
- **Host Swap File Standard for 1 GB VPS**:
  - Document the required 2 GB swap file creation protocol for 1 GB VPS instances to absorb transient spikes.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Adds requirement `Low-Memory Production Host & Container Resource Boundaries` specifying memory allocation ceilings and cgroup limits for the `docker-compose.1gb.yml` profile.

## Impact

- **Affected Files**:
  - `docker-compose.1gb.yml`: New dedicated compose file for 1 GB RAM deployments.
  - `docker-compose.prod.yml`: UNCHANGED (remains high-performance for 8 GB+ hosts).
  - `run-dev.sh`: Updated with `NODE_OPTIONS` export for local dev stability.
  - `openspec/specs/core-platform/spec.md`: Runtime specification delta for 1 GB host boundaries.
- **Zero Breaking Changes**: Existing production deployments on 8 GB VPS continue running at full capacity without throttling.
