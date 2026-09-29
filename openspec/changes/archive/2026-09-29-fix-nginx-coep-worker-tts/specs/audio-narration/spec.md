# Spec Delta

## MODIFIED Requirements

### Requirement: Multi-Threaded Synthesis Enabled on Reader Routes Only

On reader routes, the application SHALL be cross-origin isolated so on-device synthesis can use multi-threaded execution. This isolation SHALL be established across deployment tiers without duplicate or conflicting response headers. Response headers `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless` SHALL be emitted cleanly without repetition. This isolation SHALL be confined to reader routes and SHALL NOT be applied to the authentication route or other routes, so cross-origin sign-in — which depends on cross-window communication — continues to function. Cross-origin resources the reader legitimately needs (web fonts, document images) SHALL continue to load under the isolation policy.

Static assets and worker script endpoints (such as `/_nuxt/**`) SHALL deliver `Cross-Origin-Embedder-Policy: credentialless` and `Cross-Origin-Resource-Policy: cross-origin` across reverse proxy deployment tiers, ensuring that dedicated Web Workers spawned by isolated reader pages are not blocked with `ERR_BLOCKED_BY_RESPONSE` (`coep-frame-resource-needs-coep-header`).

When entering a reader route from an unisolated browsing context (such as post-login or client-side navigation from library routes), the client application SHALL ensure the browsing context acquires cross-origin isolation (executing a full document navigation if `self.crossOriginIsolated` is not yet active), so that `self.crossOriginIsolated === true` reliably holds on reader views.

When cross-origin isolation is active (`self.crossOriginIsolated === true`), the synthesis worker SHALL configure the CPU (WASM) execution backend to run across multiple threads, scaling the thread count to the machine's available logical cores (`navigator.hardwareConcurrency`), so that a many-core CPU without a usable GPU is fully utilized for synthesis rather than running on a single thread. The configured thread count SHALL be bounded by the reported hardware concurrency to avoid oversubscription. When cross-origin isolation or multi-threaded execution is unavailable, the worker SHALL clamp thread allocation to 1 without emitting console errors; thread configuration SHALL NOT change the produced audio.

#### Scenario: Clean non-duplicate cross-origin headers delivered
- **WHEN** a client requests a `/read/**` document
- **THEN** the HTTP response contains single instances of `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless` without duplicate header values.

#### Scenario: Worker script delivered with COEP on production reverse proxy
- **WHEN** a cross-origin isolated reader page requests a Web Worker script under `/_nuxt/**` through the production reverse proxy
- **THEN** the HTTP response contains `Cross-Origin-Embedder-Policy: credentialless` and `Cross-Origin-Resource-Policy: cross-origin`
- **AND** the browser instantiates the worker without `ERR_BLOCKED_BY_RESPONSE` or COEP violation errors.

#### Scenario: Navigation into reader ensures active cross-origin isolation
- **WHEN** a user navigates to `/read/[bookId]` from an unisolated route
- **THEN** the reader browsing context establishes `window.crossOriginIsolated === true`.

#### Scenario: Sign-in route is not isolated and OAuth still works
- **WHEN** the sign-in route is loaded and the user completes cross-origin sign-in
- **THEN** the page is not cross-origin isolated and sign-in completes successfully.

#### Scenario: Reader cross-origin assets still load
- **WHEN** a reader slice references cross-origin fonts or images
- **THEN** those resources still load and render under the reader's isolation policy.

#### Scenario: Many-core CPU uses multiple synthesis threads
- **WHEN** a reader route is cross-origin isolated on a machine with no usable GPU and multiple logical cores, and a user plays narration
- **THEN** the CPU synthesis backend runs across multiple threads scaled to the available cores rather than on a single thread.

#### Scenario: Single-threaded fallback still completes
- **WHEN** cross-origin isolation or multi-threaded execution is unavailable
- **THEN** synthesis still completes correctly on a single thread and produces the same audio.
