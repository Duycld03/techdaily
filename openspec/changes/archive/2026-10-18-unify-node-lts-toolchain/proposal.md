# Proposal

## Why

The workstation selects Node 22 for DeepPace while important global tools, including OpenSpec, are installed under separate Node 24 installations, so opening a new terminal can make previously working commands disappear. The developer wants one current LTS installation that works inside and outside the project without routine version selection or duplicate global-tool installation.

## What Changes

- Select the newest officially published Node.js LTS release at cutover and pin its exact version across the workstation and DeepPace; the verified planning target is `24.22.0`.
- Reuse the existing `fnm` installation, make the selected runtime the workstation default, and retain automatic shell initialization rather than introducing another Node installer, version manager, or command wrapper.
- Migrate the union of installed global tools into the selected runtime, preferring the versions currently installed under `24.21.0` for duplicates. The user approved retaining the existing standalone omp `18.12.1` instead of its duplicate npm package `18.6.1`, avoiding an additional Bun installation. Preserve CLI configurations and credentials without copying old `node_modules` trees. Keep npm governed by the existing project `packageManager` declaration, currently `npm@11.21.1`.
- Align `.node-version`, frontend Node engine metadata, frontend build/runtime images, and README instructions. Reuse CI's existing `.node-version` and `packageManager` consumption.
- Verify global commands, fresh-shell behavior, frontend checks, and the built application before uninstalling the superseded runtimes through `fnm`.
- **BREAKING**: Retire Node 22 as DeepPace's supported runtime and remove the superseded workstation installations only after the migration gate passes. A migration with old runtimes still installed is not complete.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `core-platform`: Replace the Node 22-specific deterministic-installation requirement with a pinned current-LTS contract; add observable workstation runtime consistency, global-tool preservation, and verification-before-removal requirements.

## Impact

- Repository: `.node-version`, `frontend/package.json`, npm-generated `frontend/package-lock.json` metadata where necessary, `frontend/Dockerfile`, and the existing README toolchain section. `.github/workflows/ci.yml` already consumes the canonical version declarations and should not be restructured.
- Workstation: user-owned `fnm` runtime installations/default aliases and only the shell initialization or stale command references required for this cutover. Host operations must be explicitly included in apply authorization; repo-local planning does not grant unrestricted home-directory edits.
- Verification: installed CLI entry points, native dependency installation, existing frontend lint/tests/typecheck/build, development startup, and local container startup. This proposal does not claim these have passed on the new runtime.
- No backend behavior, database data, authentication, UI design, production deployment, unrelated project pins, or unrelated application dependency upgrades are included.
