# Spec Delta

## ADDED Requirements

### Requirement: Low-Memory Production Host & Container Resource Boundaries
The platform SHALL provide a dedicated `docker-compose.1gb.yml` profile for 1.0 GB RAM hosts while preserving `docker-compose.prod.yml` for high-memory hosts. The 1 GB profile SHALL cap aggregate container memory below 750 MB, restrict database shared buffers to 128 MB, enforce .NET workstation garbage collection under 256 MB heap ceilings, and require a 2 GB host swap file. The local development script (`run-dev.sh`) SHALL allocate 4096 MB heap to prevent worker out of memory errors.

#### Scenario: Database container launches under 1gb profile constraints
- **WHEN** `docker compose -f docker-compose.1gb.yml up -d` initializes the PostgreSQL container
- **THEN** PostgreSQL initializes with `shared_buffers = 128MB`, `work_mem = 4MB`, `maintenance_work_mem = 64MB`, and `effective_cache_size = 512MB`
- **AND** the database container does not encounter shared memory allocation errors or exceed its `220M` cgroup limit.

#### Scenario: Backend container initializes under workstation GC and heap ceiling in 1gb profile
- **WHEN** the backend API container starts in the 1 GB profile environment
- **THEN** .NET 10 initializes with `DOTNET_gcServer = 0` and `DOTNET_GCHeapHardLimit = 268435456`
- **AND** the backend process triggers garbage collection deterministically within 256 MB without being terminated by the host OOM killer.

#### Scenario: Production stack memory usage remains within 1 GB host headroom
- **WHEN** all production containers in `docker-compose.1gb.yml` are active under standard traffic
- **THEN** individual container limits enforce `db: 220M`, `backend: 300M`, `frontend: 150M`, `nginx: 30M`
- **AND** the sum of container memory usage remains below `750MB`
- **AND** the host operating system maintains at least 200 MB of available physical RAM.

#### Scenario: Development server launches with extended heap capacity
- **WHEN** developer or agent executes `./run-dev.sh`
- **THEN** the script exports `NODE_OPTIONS="--max-old-space-size=4096"` prior to starting the Nuxt dev server
- **AND** the Vite/Nitro worker threads compile and reload modules without triggering `ERR_WORKER_OUT_OF_MEMORY`.
