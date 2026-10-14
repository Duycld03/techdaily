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
