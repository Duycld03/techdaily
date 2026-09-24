# Proposal: Cleanup GitHub v0.dev and Vercel Artifacts

## Why

During initial design exploration with v0.dev and Vercel, external prototype integrations registered stray artifacts on the GitHub repository. These include an orphaned remote branch (`v0/design-system-showcase`), automatic `vercel[bot]` GitHub deployment environments (`Production` and `Preview`), and a repository homepage URL pointing to `techdaily-phi.vercel.app`. Because actual production deployment is hosted on a Google Cloud VPS via GHCR and Docker Compose, these orphaned artifacts create operational ambiguity and misrepresent the production environment. Cleaning them up restores repository hygiene and accurate deployment tracking.

## What Changes

- **Delete Remote Prototype Branch**: Prune `origin/v0/design-system-showcase` from the remote repository.
- **Update Repository Homepage**: Reconfigure GitHub repository homepage metadata from `https://techdaily-phi.vercel.app` to the canonical production URL `https://techdaily.duckdns.org`.
- **Purge Obsolete Deployment Environments**: Remove `Production` and `Preview` deployment records and environments created by `vercel[bot]` via the GitHub REST API.
- **Document Vercel App Disconnection**: Provide standard operational procedure for disconnecting the Vercel GitHub App to prevent future rogue deployments on push to `main`.

## Capabilities

### Modified Capabilities
- `cleanup`: Add requirement for external prototype branch and third-party deployment environment hygiene.

## Impact

- **Affected Surfaces**: GitHub repository settings, remote branches, deployment environments widget.
- **Dependencies**: `gh` CLI, Git.
- **Breaking Changes**: None. No production services or codebases are disrupted.
