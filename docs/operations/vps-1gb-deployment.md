# DeepPace — 1 GB RAM VPS Deployment Guide

This guide establishes the host configuration and deployment procedure for hosting DeepPace on resource-constrained cloud virtual machines, specifically **Google Cloud Platform (GCP) Always Free `e2-micro`** (2 shared vCPUs, 1.0 GB RAM, 30 GB disk).

---

## 1. Architectural Strategy

The standard production configuration (`docker-compose.prod.yml`) is preserved for standard hosts with 8 GB+ RAM (e.g. `n4-standard-2`).

When deploying to a 1.0 GB RAM host, operators MUST use the dedicated profile:
```bash
docker compose -f docker-compose.1gb.yml up -d
```

### Resource Allocation Budget (700 MB Total Containers)

| Service | Technology | Cgroup Limit | Memory Reservation | Optimization Details |
|---|---|---|---|---|
| `db` | PostgreSQL 17 + pgvector | **220 MB** | 128 MB | `shared_buffers=128MB`, `work_mem=4MB`, `effective_cache_size=512MB` |
| `backend` | ASP.NET Core (.NET 10) | **300 MB** | 180 MB | `DOTNET_gcServer=0` (Workstation GC), `DOTNET_GCHeapHardLimit=256MB` |
| `frontend` | Nuxt 4 (Node.js Nitro) | **150 MB** | 64 MB | `NODE_OPTIONS="--max-old-space-size=192"` |
| `nginx` | Nginx Alpine | **30 MB** | 10 MB | Worker connections 1024, SSL session cache 10 MB |
| **Host OS & Kernel** | Linux Debian / Ubuntu | **~300 MB** | Unallocated | Preserved for Linux page cache, systemd, and Docker daemon |

---

## 2. Mandatory Host Preparation: 2 GB Swap Setup

On a 1 GB host with 0 Swap, temporary memory spikes (such as Docker image extraction, container initialization, or .NET JIT warming) will trigger the Linux kernel out-of-memory killer (`oom-killer`). A **2 GB swap file on SSD/NVMe storage is mandatory**.

Run the following commands on the VPS host as `root` (or with `sudo`):

```bash
# 1. Allocate 2 GB swapfile
sudo fallocate -l 2G /swapfile

# 2. Restrict permissions (read/write by root only)
sudo chmod 600 /swapfile

# 3. Format as swap
sudo mkswap /swapfile

# 4. Enable swap
sudo swapon /swapfile

# 5. Persist across reboots
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab

# 6. Verify swap is active
free -h
```

Expected output:
```text
               total        used        free      shared  buff/cache   available
Mem:           960Mi       120Mi       500Mi        10Mi       340Mi       800Mi
Swap:          2.0Gi          0B       2.0Gi
```

---

## 3. Migration & Deployment Procedure

### Step 1: Clone Repository & Create `.env`
```bash
git clone https://github.com/duycld03/techdaily.git ~/techdaily
cd ~/techdaily

# Ensure production .env is configured with database credentials, secrets, and Google OAuth
cp .env.example .env
nano .env
```

### Step 2: Log into GitHub Container Registry (GHCR)
Images are built remotely in GitHub Actions CI/CD; no compilation occurs on the VPS:
```bash
echo "$GHCR_TOKEN" | docker login ghcr.io -u "$GHCR_USER" --password-stdin
```

### Step 3: Pull and Launch Using the 1 GB Profile
```bash
# Pull pre-built images from GHCR
docker compose -f docker-compose.1gb.yml pull

# Launch containers with cgroup resource limits
docker compose -f docker-compose.1gb.yml up -d --no-build --remove-orphans

# Restart Nginx to refresh upstream container IP mappings
docker compose -f docker-compose.1gb.yml restart nginx
```

---

## 4. Verification & Operational Health Check

To verify that all services are healthy and running within their allocated boundaries:

```bash
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"
```

Expected operational baseline:
- `deeppace_postgres_prod`: ~60–80 MB / 220 MB
- `deeppace_backend_prod`: ~140–180 MB / 300 MB
- `deeppace_frontend_prod`: ~50–65 MB / 150 MB
- `deeppace_nginx_prod`: ~4–8 MB / 30 MB
- Total physical memory usage: $\le 650\text{ MB}$.

---

## 5. Authorization Readiness & Failed-Startup Recovery (F03, F08, F09)

### 5.1 Deployment State Distinctions

DeepPace strictly enforces authorization readiness before accepting traffic or starting background ingestion consumers:

1. **Ready & Initialized**: All seven system-protected grants (`RoleDefaults.ProtectedAdminGrants`) match the database baseline exactly, receipt schema is readable, and one-time operator initialization is complete. Both learner workflows and administrative routes (`/admin/*`) are operational.
2. **Ready & Uninitialized**: Schema and seed roles are intact, but one-time operator initialization has not yet occurred. Host startup succeeds for real Learners (registration, login, reading); administrative APIs and capability checks return `503 Service Unavailable` with `Cache-Control: no-store`.
3. **Unready Authority (Failed Required Readiness)**: If required schema is missing, seed roles or permissions are corrupted/missing, learner role holds unauthorized permissions, or receipt lookup encounters a database error:
   - Host startup terminates before `app.Run` with exit code `1`.
   - HTTP listener and ingestion worker do NOT start.
   - The host's health endpoint is **completely unavailable** (connection refused), preventing a "zombie" host where web traffic is accepted while the ingestion consumer is dead.

### 5.2 Restart & Recovery Procedure

If a host fails startup admission due to temporary database unreadiness or grant mismatches:

1. **Inspect Host Logs**:
   ```bash
   docker compose -f docker-compose.1gb.yml logs --tail=50 backend
   ```
   Confirm the explicit readiness failure message:
   `Startup failed: Authorization readiness check failed - <Reason>`

2. **Restore Required Authority**:
   Ensure the database is running, migrations are applied, and the exact seven protected admin grants are present. If initializing a fresh deployment, run:
   ```bash
   docker compose -f docker-compose.1gb.yml exec backend dotnet DeepPace.Api.dll authorization initialize --admin-user-id <admin_uuid>
   ```

3. **Restart the Host**:
   ```bash
   docker compose -f docker-compose.1gb.yml restart backend
   ```
   Verify health endpoint returns `200 OK`:
   ```bash
   curl -s http://127.0.0.1:5000/health
   # Expected: {"status":"healthy","database":"connected","error":null,"timestamp":"..."}
   ```

4. **Queue & Attempt Integrity Verification**:
   Upon successful restart, `PdfIngestionWorker` starts listening and resumes processing. Any attempts that were queued prior to failed startup are picked up using their original attempt IDs without generating extraneous retries or leaving orphaned queued records.

> **Operational Invariants**:
> - Do NOT change the Compose container restart policy.
> - Do NOT introduce an in-code startup retry loop around readiness checks.
> - NEVER describe or treat a failed host's health endpoint as available.
