# Spec Delta: cleanup

## ADDED Requirements

### Requirement: Repository Branch & Deployment Environment Hygiene
The GitHub repository configuration and remote branches SHALL reflect only official production infrastructure and active development streams:
1. **Remote Prototype Branch Pruning**: Transient prototype branches created by external third-party generation tools (e.g. `v0/*`) MUST be pruned from the remote repository once prototyping exploration concludes.
2. **Canonical Homepage Resolution**: The repository homepage metadata SHALL point exclusively to the authenticated production deployment host (`https://techdaily.duckdns.org`) or remain unset, prohibiting stray staging or external preview SaaS URLs.
3. **Deployment Environment Isolation**: GitHub Deployment Environments SHALL represent verified, operational application deployments. External automated preview bots (such as `vercel[bot]`) that are no longer part of the production deployment pipeline MUST be purged and disconnected to prevent phantom builds.

#### Scenario: Inspecting repository deployment and branch metadata
- **WHEN** an engineer or visitor views the repository homepage and deployment sidebar on GitHub
- **THEN** no stray `v0/*` branches appear in remote branches, the homepage URL points to the canonical VPS host, and third-party preview deployment widgets are absent.
