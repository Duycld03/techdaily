# Spec Delta

## MODIFIED Requirements

### Requirement: Deterministic Frontend Dependency Installation in CI Pipeline
The frontend dependency configuration SHALL support reproducible `npm ci` using the exact Node.js LTS release declared in `.node-version` and the npm version declared in `frontend/package.json`. Local development, CI, and frontend build/runtime images SHALL agree on that Node release. The selected release SHALL be the newest official LTS at cutover and SHALL remain pinned rather than silently floating afterward.

#### Scenario: GitHub Actions CI executes frontend dependency installation
- **GIVEN** a push to branch `main` or pull request
- **WHEN** the `Frontend CI` job runs `npm ci` in `frontend/`
- **THEN** npm resolves and installs packages cleanly without `EUSAGE` or missing package mismatch errors
- **AND** downstream build and test steps execute successfully.

#### Scenario: Host and container use the selected release
- **WHEN** the developer inspects the configured local runtime and executes Node in the frontend build and runtime images
- **THEN** all report the exact Node release selected for the cutover
- **AND** CI uses that same release instead of an independent major-version default.

#### Scenario: The latest LTS changes after a completed cutover
- **WHEN** a newer LTS release becomes available
- **THEN** opening the project or rebuilding an existing revision does not silently upgrade its pinned runtime
- **AND** a subsequent explicit toolchain upgrade selects and verifies the new release coherently.

#### Scenario: Dependency installation fails
- **WHEN** a dependency, runtime, or lockfile incompatibility makes `npm ci` fail
- **THEN** its nonzero result blocks verification and old-runtime removal
- **AND** forced installation or relaxed application quality checks do not count as successful migration.

## ADDED Requirements

### Requirement: Single-LTS workstation runtime
After migration, the workstation SHALL retain exactly one user-managed Node installation, matching DeepPace's selected LTS release. Fresh interactive shells outside the project, at its root, and inside `frontend/` SHALL resolve that runtime without manual version-selection commands, per-project CLI reinstalls, or an additional Node installation.

#### Scenario: Developer opens a fresh terminal
- **WHEN** the developer opens a new interactive shell in the home directory
- **THEN** Node resolves to the retained LTS release without a manual environment command.

#### Scenario: Developer enters or leaves DeepPace
- **WHEN** the developer moves between the home directory, project root, and `frontend/`
- **THEN** the Node release and migrated CLI availability remain consistent
- **AND** the move neither requests a version choice nor reinstalls Node or global tools.

#### Scenario: Migration reports completion
- **WHEN** the migration is declared complete
- **THEN** the user-managed runtime inventory contains only the selected release
- **AND** default selection and persistent command references do not depend on removed installations.

### Requirement: Global-tool continuity during runtime migration
Runtime migration SHALL preserve the installed global-tool set and configuration, except the user-approved duplicate npm omp package, which SHALL be replaced by the existing standalone omp. Duplicate retained packages SHALL use designated source versions; source-only tools SHALL not be dropped. The declared npm version SHALL take precedence over old runtime npm versions.

#### Scenario: Global tools exist across several old installations
- **WHEN** the migration transfers the inventory to the selected runtime
- **THEN** every distinct retained tool is available under that runtime, with omp available through its preserved standalone installation
- **AND** duplicate tools match the designated source versions rather than being silently upgraded or downgraded.

#### Scenario: A tool exists only in an older installation
- **WHEN** a tool is absent from the designated source but present in another old installation
- **THEN** that tool and its inventoried version are preserved before the old installation is removed.

#### Scenario: Migrated tools are launched normally
- **WHEN** the developer invokes OpenSpec, installed coding-agent CLIs, and language servers through their ordinary entry points
- **THEN** they start under the supported environment without absolute paths into removed runtime directories
- **AND** existing user configuration and credentials remain unchanged.

#### Scenario: Old runtime contains a different npm version
- **WHEN** the source global inventory contains npm that differs from the project declaration
- **THEN** the selected runtime uses the project's declared npm version
- **AND** this exception does not discard other inventoried tools or alter application dependencies arbitrarily.

#### Scenario: The existing standalone omp supersedes its duplicate npm package
- **GIVEN** the user's explicit choice to keep standalone omp `18.12.1` instead of npm omp `18.6.1`
- **WHEN** the global-tool inventory is reconciled
- **THEN** the duplicate npm omp package is removed from the selected runtime
- **AND** the existing standalone binary and settings remain unchanged and its normal command starts successfully without installing Bun.

### Requirement: Verified and non-destructive runtime retirement
Old runtimes SHALL remain until CLI/fresh-shell checks, clean installation, native lint, the full test suite, production build, and dev/container startup succeed. Independently evidenced pre-existing typecheck diagnostics SHALL remain visible but SHALL not alone block retirement; new typecheck failures SHALL block it. Active dependencies on old runtimes SHALL block removal until controlled handover. Removal SHALL preserve unrelated files and application data.

#### Scenario: Tool migration or application verification fails
- **WHEN** a required CLI fails to migrate or verification finds a new runtime-related failure
- **THEN** old installations remain recoverable and migration is not reported complete
- **AND** the failure remains visible rather than being bypassed to permit cleanup.

#### Scenario: A running tool still needs an old installation
- **WHEN** a process or persistent launcher still depends on a retiring runtime or its packages
- **THEN** removal waits for an explicit, verified handover
- **AND** migration does not kill the user's terminal, coding session, or unrelated services merely to finish removal.

#### Scenario: Existing type diagnostics are observed during verification
- **WHEN** standalone typechecking reports independently evidenced pre-existing diagnostics
- **THEN** its actual nonzero result is reported separately from the absence of new migration regressions
- **AND** it is not described as passing or suppressed by this migration.

#### Scenario: A mandatory check has a pre-existing failure
- **WHEN** dependency installation, native lint, the full test suite, or production build fails even for a pre-existing reason
- **THEN** retirement remains blocked
- **AND** the pre-existing-typecheck exception does not reclassify that failed mandatory check as acceptable.

#### Scenario: Retirement succeeds
- **WHEN** all required verification and process handovers have completed
- **THEN** only the superseded user-managed Node installations and obsolete references are removed
- **AND** the final shell and CLI smoke checks still succeed without those installations.
