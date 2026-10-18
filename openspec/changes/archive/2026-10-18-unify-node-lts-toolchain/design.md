# Design

## Context

See `proposal.md` for motivation and `specs/core-platform/spec.md` for the behavioral contract.

Observed during planning:

- The workstation has `fnm` installations `22.23.3` (default), `24.12.0`, and `24.21.0`. `.node-version` selects `22.23.3`.
- Global tools are isolated in each installation. Node 22 contains npm and Corepack, while `24.21.0` contains the current coding tools and language servers. An absolute-path OpenSpec invocation works; ordinary invocation in the Node 22 environment does not.
- The existing Zsh initialization already evaluates `fnm env --use-on-cd --version-file-strategy=recursive`; no replacement shell framework is needed.
- CI reads `.node-version` and installs the npm version in `frontend/package.json`. Both frontend Docker stages currently use `node:22-bookworm-slim`.
- The frontend declares `npm@11.21.1`; the `24.21.0` installation instead contains npm `12.2.0`. Runtime migration must not silently change the project's package-manager policy.
- The locked Nuxt version accepts Node `^24.15.0`; locked Vite, Vitest, and ESLint engine ranges also accept Node 24. These declarations establish eligibility, not successful runtime verification.
- `run-dev.sh` invokes ambient npm, starts the backend, and terminates processes on ports 3000/5000 during startup/cleanup. It is not a safe unattended migration probe against a working developer environment.
- The running omp executable resolves to the existing standalone `~/.local/bin/omp`. This is not an obsolete Node installation and must not be removed as part of runtime cleanup. Active process dependencies must be checked again at apply time.

The official [download page](https://nodejs.org/en/download) and [release announcement](https://nodejs.org/en/blog/release/v24.22.0) identify `24.22.0` as LTS, released on 2026-10-14. Availability of its matching Docker image and actual project/tool execution on that runtime remain implementation checks.

## Goals / Non-Goals

**Goals:**

- End with one user-managed Node runtime and one effective global package inventory, usable from a fresh terminal without manual version selection.
- Make host, repository, CI, and frontend container versions agree while preserving existing application and CLI behavior.
- Keep a recoverable migration window; remove old installations only after verification and process handover.

**Non-Goals:**

- No new runtime manager, Homebrew Node installation, shared npm-prefix scheme, permanent migration daemon, command wrapper, or automatic update-on-directory-change behavior.
- No unrelated package upgrades, application refactors, backend/database changes, production deployment, or changes to other repositories' version files.
- No promise that one Node version satisfies every unrelated project or that future LTS upgrades happen without verification.
- No deletion of caches, credential stores, unrelated dotfiles, the standalone omp executable, or embedded runtimes shipped inside other applications.

## Decisions

### 1. Select current LTS once, then pin the exact release

Use `24.22.0` as the planning target. Recheck official release metadata immediately before implementation; if a newer LTS supersedes it, record the selected exact version in this change before touching the workstation or project. Never substitute the non-LTS Current channel or use floating `latest`/`lts` values in committed build inputs.

Keep `fnm` as the installation owner and set its default and relevant aliases to the selected release. Multiple installations are acceptable only during the migration window. A generic `system` entry in manager output is not itself an installed Node binary; verify actual paths rather than treating labels as extra installations.

Alternative rejected: keeping Node 22 and merely reinstalling OpenSpec would not meet the requested newest-LTS/single-installation outcome.

### 2. Preserve the global-tool union without unrelated upgrades

Snapshot package names, versions, installation prefixes, and executable names from all existing runtimes before mutation. Use `24.21.0` as the authoritative source for duplicate package versions; supplement with any packages found only under the other installations. Refresh this inventory at apply time rather than assuming the planning snapshot is immutable.

Planning snapshot from `24.21.0`, excluding npm:

| Package | Version to preserve |
| --- | --- |
| `@anthropic-ai/claude-code` | `2.1.291` |
| `@fission-ai/openspec` | `1.14.1` |
| `@oh-my-pi/pi-coding-agent` | `18.6.1` |
| `@tailwindcss/language-server` | `0.16.0` |
| `@vue/language-server` | `3.3.12` |
| `bash-language-server` | `5.8.1` |
| `corepack` | `0.36.0` |
| `dockerfile-language-server-nodejs` | `0.15.0` |
| `prettier` | `3.9.9` |
| `pyright` | `1.1.414` |
| `typescript-language-server` | `6.0.1` |
| `typescript` | `7.0.2` |
| `vscode-langservers-extracted` | `4.10.0` |
| `yaml-language-server` | `1.24.0` |

Install these exact packages through npm under the selected runtime. Do not copy old `node_modules`, repoint bins into retiring directories, use `--force` to hide engine failures, or install unpinned CLI upgrades. Preserve configuration/authentication files outside retiring prefixes in place and keep the migration inventory free of credentials. Check each retiring prefix for configuration or authentication files; if any are present, block removal until their preservation is explicitly authorized and verified without including their contents in the inventory.

npm is the explicit exception: install `npm@11.21.1`, as declared by the project, rather than copying the source runtime's npm 12 or accepting a different bundled npm. If preserving a tool proves incompatible with the selected LTS, stop before retirement and surface that specific compatibility decision instead of silently dropping/upgrading it.

Apply-time user decision: preserve the existing standalone omp `18.12.1` and exclude the duplicate npm package `@oh-my-pi/pi-coding-agent@18.6.1` from the retained global inventory. The npm package requires Bun `>=1.3.14`, which is not installed. The user explicitly chose the working standalone binary rather than adding Bun. Uninstall only the newly migrated duplicate npm package; preserve the standalone executable, running sessions, and settings. The original recovery inventory remains unchanged as a record of the pre-cutover state.

### 3. Reuse existing repository version consumers

- Set `.node-version` to the selected exact release.
- Declare the selected LTS major (currently Node 24) in `frontend/package.json` engine metadata, with a minimum matching the selected release. Preserve `packageManager: npm@11.21.1` and existing application dependency versions.
- Update any changed root lockfile metadata with npm, not manual edits. Reinstall native dependencies under the selected runtime; do not reuse artifacts built against Node 22. Do not upgrade `@types/node` or other dependencies solely to make version numbers look uniform.
- Pin both frontend Docker stages to the selected exact release on the existing Debian Bookworm slim base. Keep the existing install/build commands and ONNX installation policy.
- Reuse CI's `node-version-file` and package-manager selection. No unrelated workflow rewrite or new typecheck enforcement stage is included.
- Update the existing README toolchain section with the selected runtime, one-time setup, global-tool migration, and ordinary subsequent startup. Leave `run-dev.sh` unchanged unless a directly demonstrated runtime incompatibility requires an explicitly reviewed adjustment.

Explicit Docker pins duplicate the selected version text outside `.node-version`. This small trade-off preserves normal `docker build frontend` usage without adding build-argument plumbing; verify the actual build and runner Node versions against the canonical pin. CI already consumes the canonical file directly.

### 4. Keep shell selection automatic, not installer-driven

Reuse working Zsh/fnm initialization and verify the default runtime outside the repository as well as recursive selection inside it. Remove or correct only observed stale references to retiring installations, including executable symlinks, manager aliases, and any relevant ancestor version pin. Do not rewrite unrelated shell customizations or other repositories.

A new shell must work without first running `fnm use`, installing tools again, or specifying an old absolute OpenSpec path. Noninteractive jobs and long-lived terminals may inherit stale PATH entries; refresh or restart the specific consumer through an explicit handover instead of assuming a default-alias change rewrites already running processes.

### 5. Separate repo edits from workstation operations

The OpenSpec action context permits repo-local edits. The requested host migration is a distinct operator scope: apply authorization must explicitly cover the user's fnm installations/default selection and any necessary shell-reference changes before those operations occur. Do not expand that permission to system-wide package removal or arbitrary home-directory edits.

Use fnm's supported uninstall operations for the inventoried old runtimes, not broad filesystem deletion. Do not copy private CLI settings or install host coding tools into the application's production container.

## Risks / Trade-offs

- Native packages or CLI install scripts may reject the new runtime -> reinstall from exact package versions, execute the affected entry points, and block retirement on incompatibility.
- npm 12 in the source inventory conflicts with npm 11 in the project -> retain the project declaration consistently on the host and build path; changing npm policy is not implicit in selecting newer Node.
- Existing shells or background tools still reference old packages -> verify launcher and process paths, refresh consumers deliberately, and do not remove a required installation while a consumer still depends on it.
- Existing application diagnostics can be mistaken for migration regressions -> preserve their real exit codes and compare against trustworthy pre-change evidence; do not suppress diagnostics or claim a failed check passed.
- A local startup probe could stop unrelated services or trigger backend migrations -> use direct frontend startup on a free loopback port and a local disposable container, not unattended `run-dev.sh` execution or production deployment.
- Other repositories may require an old runtime -> do not edit them or install a fallback automatically; report a concrete conflict before retirement if one is discovered.
- A matching pinned Docker tag may be unavailable -> check before cutover and block rather than substituting a floating image.

## Migration Plan

1. Confirm the exact LTS release and matching container tag, refresh the versioned global inventory, and capture current runtime/default/shell-reference state. Use existing diagnostic evidence where available; obtain a narrowly scoped baseline only where needed to identify an otherwise unknown regression.
2. Within authorized host scope, install the target runtime alongside the old ones, install the declared npm version and retained inventoried global tools, and verify that installed package versions match the reconciled inventory with the approved standalone-omp exception.
3. Align the repository version declarations/images, set the target as the workstation default, and prove fresh-shell behavior in the home directory, repository root, and frontend directory. Keep old installations intact while verification runs.
4. Run a clean frontend dependency install, native lint, the full existing frontend test suite, standalone typecheck, and production build. Require install/lint/tests/build to succeed; preserve and separately report any independently evidenced pre-existing type diagnostics. A newly introduced failure blocks retirement. Do not weaken check policy or add source-text/CSS-class tests.
5. Start the real frontend dev server on an unused loopback port and observe an anonymous route response. Build the local frontend image, inspect Node versions in the build and runner stages, and observe an anonymous route from the running image. Use existing required environment values; introduce no configuration fallbacks. These are runtime smokes, not UI visual verification.
6. Smoke the retained global tools using supported version/help commands, or an initialize/shutdown exchange for language servers without such flags. Check OpenSpec's normal invocation and project listing, coding-agent entry points without making paid/auth-changing calls, compiler/formatter tools, and all retained language-server executables. Verify the existing standalone omp and the absence of its user-excluded duplicate npm package.
7. Complete any required process handover, then uninstall `22.23.3`, `24.12.0`, and `24.21.0` if still present and superseded. Recheck actual inventory, aliases, fresh-shell resolution, and CLI startup after removal. Do not mark the migration complete while a required removal or handover remains pending.
8. Update the README with verified usage and migration evidence. Keep private inventory/recovery material outside committed files and remove any disposable probes or containers.

### Rollback

Before retirement, restore the recorded default selection and only the repository/shell changes made by this migration; reinstall project dependencies under the restored runtime when necessary. Leave unrelated user edits, CLI credentials, and running services intact.

After retirement, recovery requires reinstalling the recorded prior runtime and exact CLI versions from the inventory before restoring prior selection/project declarations. Old installations are not retained permanently as a compatibility layer. Registry access is therefore a recovery prerequisite after final removal.
