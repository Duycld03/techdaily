# Tasks: Cleanup GitHub v0.dev and Vercel Artifacts

## 1. Remote Branch Pruning

- [x] 1.1 Delete orphaned remote branch `v0/design-system-showcase` from GitHub via `git push origin --delete v0/design-system-showcase`
- [x] 1.2 Prune stale remote tracking references locally using `git remote prune origin`

## 2. GitHub Repository Metadata & Deployment Environments

- [x] 2.1 Update GitHub repository homepage metadata to point to `https://techdaily.duckdns.org` via `gh repo edit --homepage "https://techdaily.duckdns.org"`
- [x] 2.2 Query and purge active deployments associated with `vercel[bot]` via `gh api -X DELETE /repos/Duycld03/techdaily/deployments/{id}`
- [x] 2.3 Delete orphaned GitHub deployment environments `Production` and `Preview` via `gh api -X DELETE /repos/Duycld03/techdaily/environments/{env}`

## 3. Verification & Security Hygiene

- [x] 3.1 Verify via `git branch -r` and `gh repo view` that remote branches and homepage reflect only canonical production endpoints
- [x] 3.2 Verify via `gh api repos/Duycld03/techdaily/environments` that zero third-party SaaS deployment environments linger
- [x] 3.3 Document instructions for revoking repository access in the Vercel GitHub App to prevent future rogue builds
