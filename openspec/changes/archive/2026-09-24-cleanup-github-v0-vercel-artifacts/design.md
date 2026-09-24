# Design: Cleanup GitHub v0.dev and Vercel Artifacts

## Context

See `proposal.md - Why`. The GitHub repository `Duycld03/techdaily` currently has:
1. An orphaned remote branch: `origin/v0/design-system-showcase`.
2. Two stray GitHub Deployment Environments: `Production` and `Preview`, created by `vercel[bot]`.
3. A repository homepage URL pointing to `https://techdaily-phi.vercel.app`.

The authoritative production deployment is executed via GitHub Actions (`.github/workflows/ci.yml`), which builds multi-stage Docker images, pushes them to GitHub Container Registry (GHCR), and deploys via SSH to a Google Cloud VPS running Docker Compose behind Nginx at `https://techdaily.duckdns.org`.

## Goals / Non-Goals

**Goals:**
- Prune the remote prototype branch `v0/design-system-showcase` from `origin`.
- Update repository homepage metadata to point to `https://techdaily.duckdns.org`.
- Purge orphaned `Production` and `Preview` deployment records and environments via GitHub REST API.
- Document the verification and revocation steps for the Vercel GitHub App integration.

**Non-Goals:**
- Modifying local feature branches or local git history.
- Altering the existing production CI/CD workflow (`ci.yml`), which already targets Google Cloud VPS.
- Modifying application code or Docker configurations.

## Decisions

### 1. Remote Branch Pruning via Git
- Use `git push origin --delete v0/design-system-showcase` to remove the branch from the remote server atomically. Local branch tracking references will be pruned with `git remote prune origin`.

### 2. Environment and Deployment Cleanup via GitHub REST API
- Deployments attached to environments in GitHub can prevent direct environment deletion unless deactivated or purged.
- Execute `gh api -X DELETE /repos/Duycld03/techdaily/environments/{environment_name}` for both `Production` and `Preview`. If deployment IDs linger, delete them via `gh api -X DELETE /repos/Duycld03/techdaily/deployments/{deployment_id}` before removing the environment.

### 3. Canonical Repository Homepage Update
- Execute `gh repo edit --homepage "https://techdaily.duckdns.org"` to ensure GitHub repository UI correctly directs visitors to the operational VPS instance.

### 4. Vercel App Access Boundary
- If Vercel remains installed as an authorized GitHub App with repository access to `Duycld03/techdaily`, any subsequent commit to `main` could re-trigger a deployment. We provide explicit instructions to revoke repository permissions under GitHub Account Settings -> Installed GitHub Apps -> Vercel.

## Risks / Trade-offs

- **Risk**: Deleting the remote branch might disrupt anyone referencing it.
  - *Trade-off / Mitigation*: The branch was an automated export from v0.dev for exploratory design, and all necessary design components have already been migrated and committed to `main`.
- **Risk**: Automated re-creation of environments by Vercel webhooks.
  - *Mitigation*: Unlinking the repository in the Vercel dashboard or adjusting GitHub App permissions ensures permanent cleanup.
