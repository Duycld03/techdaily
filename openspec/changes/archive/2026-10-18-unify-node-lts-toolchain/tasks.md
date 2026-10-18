# Tasks

Checkboxes track implementation and verified results. Domain, Application, and API code are unchanged.

## 1. Infrastructure: Cutover prerequisites and recovery inventory

- [x] 1.1 Reconfirm the newest official Node LTS release and availability of its exact Bookworm slim image; record the concrete selection in this change before mutation and verify that it agrees with the planned target (`24.22.0` unless explicitly refreshed).
- [x] 1.2 Obtain apply authorization for the workstation operations outside the repo-local edit root; record the specific fnm/default/shell-reference scope and verify that unrelated home-directory or system-wide changes are excluded.
- [x] 1.3 Refresh the global package/version/bin inventory for all installed runtimes, reconcile duplicate versions against `24.21.0`, and preserve a private credential-free recovery record of runtime/default/shell-reference state; verify that every distinct source package is accounted for and npm is the documented project-policy exception.

## 2. Infrastructure: Install and activate the shared LTS

- [x] 2.1 Install the selected release through existing fnm without removing old runtimes, then install the npm version declared by `frontend/package.json`; verify the target's `node --version` and `npm --version` against those selections.
- [x] 2.2 Install the reconciled retained global tools at their recorded versions under the selected runtime without copying old package trees; compare the resulting inventory and smoke each executable using a supported help/version command or language-server initialize/shutdown exchange. Preserve source-only packages and standalone omp `18.12.1`; remove its duplicate npm package `18.6.1` per the user's explicit choice, without installing Bun.
- [x] 2.3 Point fnm's default/relevant aliases at the selected release and correct only demonstrated stale shell/launcher references; verify outside-project default selection in a fresh interactive Zsh session, preserving unrelated shell settings and CLI credentials.
- [x] 2.4 Update the README's existing toolchain section with one-time host setup, global-tool preservation, and the normal no-manual-selection workflow; verify its setup instructions against the target installation rather than an old absolute CLI path.

## 3. Frontend: Align repository, CI, and container runtimes

- [x] 3.1 Update `.node-version` and frontend Node engine metadata to the selected LTS while retaining `npm@11.21.1`; regenerate changed lockfile metadata through npm and run a clean `npm ci` under the target, verifying that application dependency versions do not drift without a demonstrated compatibility need.
- [x] 3.2 Pin both frontend Docker stages to the selected exact Bookworm slim version and reuse CI's existing version-file/package-manager selection; build the local build and runner stages and verify their actual `node --version` matches `.node-version` without changing CI quality policy or ONNX installation behavior.
- [x] 3.3 Run the existing native frontend lint, full test suite, standalone typecheck, and production build under the target runtime; record real exit codes, require install/lint/tests/build success, and distinguish independently evidenced pre-existing type diagnostics from new migration regressions without suppressions or softened gates.
- [x] 3.4 Start the actual frontend dev server on an unused loopback port and the built frontend container on a separate free port; observe the expected anonymous route responses with existing environment configuration, without executing the disruptive fullstack launcher, changing production, or claiming UI visual proof.
- [x] 3.5 Finish the README's repository/container instructions and rollback guidance using the verified version and actual commands; verify that normal local startup remains the existing entry point and no routine manual fnm selection is required.

## 4. Infrastructure: Retire superseded installations safely

- [x] 4.1 Require the earlier successful migration gates and recheck fresh-shell Node/npm/CLI resolution in the home directory, repo root, and `frontend/`; inspect processes, persistent launchers, aliases, and retiring prefixes for dependencies or configuration/credentials, and block removal until handover and any authorized settings preservation are verified without killing the user's coding session or unrelated services.
- [x] 4.2 Uninstall each inventoried superseded fnm runtime, including `22.23.3`, `24.12.0`, and `24.21.0` when applicable; verify actual runtime directories contain only the selected release, with no dangling default/alias references and no removal of standalone tools, credentials, caches, or unrelated application data.

## 5. Infrastructure and Frontend: Final integration evidence

- [x] 5.1 After removal, open fresh interactive shells in the home directory, repo root, and `frontend/`; verify identical selected Node/npm versions, normal OpenSpec invocation/project listing, and the migrated CLI entry points without old-runtime paths, then report the final single-runtime inventory and prior frontend/container evidence. Leave migration incomplete if retirement or handover is still pending.

## Implementation evidence

- Selected Node `24.22.0`: reconfirmed on the official Node download page; the exact `node:24.22.0-bookworm-slim` OCI manifest includes Linux amd64.
- Workstation scope: the user's single-Node migration request and explicit apply invocation authorize user-owned fnm installs/removal, default/related aliases, global-package migration, and demonstrated stale shell/launcher references. Unrelated home-directory, system-wide, credential, and application-data changes are excluded.
- Recovery inventory saved outside the repo with directory mode `0700` and file mode `0600`: three old runtimes, original aliases/shell checksum, 14 reconciled packages exposing 24 bins, and the 1,462-entry dependency-version signature. Duplicate versions match `24.21.0`; npm remains `11.21.1`.
- Installed Node `v24.22.0` with fnm alongside the old runtimes; the selected installation reports npm `11.21.1`.
- The user explicitly selected standalone omp only after the npm duplicate failed its Bun prerequisite. Retained inventory excludes `@oh-my-pi/pi-coding-agent@18.6.1`; the existing standalone `omp/18.12.1` version/help checks succeed.
- Lockfile engine metadata was generated through Bash with `fnm exec --using=24.22.0 npm install --package-lock-only --ignore-scripts --no-audit --no-fund`; all 1,462 locked dependency versions retain their original signature. No manual lockfile edits are permitted.
- Clean frontend installation completed under Node `24.22.0` / npm `11.21.1`: `ONNXRUNTIME_NODE_INSTALL=skip fnm exec --using=24.22.0 npm ci` exited `0` and ran `nuxt prepare`; no application dependency versions changed.
- Retained globals match all 13 approved package versions plus npm; all 23 package bins passed supported CLI or language-server checks. All 13 LSPs completed initialize/shutdown/exit exchanges. Standalone omp remains `18.12.1`; Bun was not installed.
- `default` and `lts-latest` now select `24.22.0`; 14 existing user-bin symlinks and the Claude desktop handler were repointed without changing arguments. Fresh home-directory Zsh reports Node `24.22.0`, npm `11.21.1`, and OpenSpec `1.14.1`; the existing `.zshrc` checksum is unchanged. Active LSP daemon handover remains a separate retirement gate.
- Native gate exit codes: lint `0`, full tests `0` (104 suites / 1,008 tests passed), standalone typecheck `2`, production build `0`. Typecheck reports the same 115 pre-cutover file/position/TypeScript-code diagnostics, plus the existing Vue-router plugin-path warning; no suppressions or source/dependency upgrades were made.
- Docker build and runner stages both built successfully from an isolated tar context excluding dependencies, generated outputs, and environment files. Both actual images report Node `v24.22.0`; the build image reports npm `11.21.1`. Existing ONNX/install-script policy and CI version-file consumption are unchanged.
- Actual anonymous `/login` responses returned HTTP `200` with DeepPace HTML, password forms, and Nuxt assets on isolated dev port `3317` and production-container port `3318`. Dev startup sourced the existing root environment; the fullstack launcher and production were not run or changed. These are runtime smokes, not UI visual proof.
- README now includes the verified isolated container build/run commands, unchanged normal startup, explicit retirement gates, and recovery before/after fnm removal. Fresh interactive Zsh checks in home, repo root, and frontend all report Node `24.22.0`, npm `11.21.1`, and normal OpenSpec invocation; repo/frontend project listing resolves the correct root.
- Completed supported handover with `omp ps restart omp.lsp.mux --json`: only the supervised LSP mux was restarted, not the user session. Its replacement JSON server uses Node `24.22.0`; both parent inspection and independent security review found no active consumers of retiring prefixes. Filename/metadata inspection identified no non-package user configuration/authentication material in those prefixes.
- The disposable dev process and frontend container were stopped after successful HTTP smokes; the container was automatically removed. All mandatory retirement gates passed, with only the explicitly documented unchanged pre-existing typecheck diagnostics remaining visible.
- `fnm uninstall` successfully removed `22.23.3`, `24.12.0`, and `24.21.0`. Physical runtime-directory inspection shows only `v24.22.0` plus fnm's download-cache directory; `default` and `lts-latest` resolve to the retained installation.
- After removal, fresh home/root/frontend Zsh sessions again resolve Node `24.22.0`, npm `11.21.1`, and OpenSpec `1.14.1`; project listing works normally. All 23 retained package-bin smokes, including 13 LSP lifecycle exchanges, passed again. The live JSON-LSP document-symbol request succeeds, standalone omp remains `18.12.1`, all 14 user-bin links resolve into the retained prefix, the dependency-version signature and shell checksum remain unchanged, and both disposable probe ports are closed.
