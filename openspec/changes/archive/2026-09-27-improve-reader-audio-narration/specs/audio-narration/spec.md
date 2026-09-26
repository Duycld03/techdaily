# Spec Delta

<!--
  These requirements ADD to the `audio-narration` capability introduced by the
  `add-reader-audio-narration` change. Archive this change only after that base
  change is archived, so the `audio-narration` capability exists in
  `openspec/specs/` and these requirements append to it.
-->

## ADDED Requirements

### Requirement: GPU-Accelerated On-Device Synthesis with CPU Fallback

The reader SHALL run on-device narration synthesis on the GPU when the browser exposes GPU compute to the synthesis worker, and SHALL fall back to CPU execution when GPU compute is unavailable or fails to initialize, so synthesis completes on any supported browser. The choice of execution backend SHALL NOT change the produced audio or any user-facing control. A backend-initialization failure SHALL NOT surface as a narration error while a working fallback exists, and the backend selection SHALL NOT be re-probed or re-failed per sentence within a synthesis run.

#### Scenario: GPU backend used when available
- **WHEN** GPU compute is available to the synthesis worker and a user plays narration
- **THEN** synthesis runs on the GPU backend and produces playable audio.

#### Scenario: Falls back to CPU when GPU is unavailable
- **WHEN** GPU compute is unavailable or its initialization fails
- **THEN** synthesis falls back to CPU, still completes successfully, and produces the same audio without surfacing a narration error.

#### Scenario: Backend chosen once per model
- **WHEN** the execution backend has been selected for a language's model
- **THEN** subsequent sentences in the run reuse that backend without re-probing or repeating a failed initialization.

### Requirement: Multi-Threaded Synthesis Enabled on Reader Routes Only

On reader routes, the application SHALL be cross-origin isolated so on-device synthesis can use multi-threaded execution. This isolation SHALL be confined to reader routes and SHALL NOT be applied to the authentication route or other routes, so cross-origin sign-in — which depends on cross-window communication — continues to function. Cross-origin resources the reader legitimately needs (web fonts, document images) SHALL continue to load under the isolation policy.

#### Scenario: Reader route is cross-origin isolated
- **WHEN** a reader route (`/read/...`) is loaded
- **THEN** the page reports cross-origin isolation, enabling multi-threaded synthesis.

#### Scenario: Sign-in route is not isolated and OAuth still works
- **WHEN** the sign-in route is loaded and the user completes cross-origin sign-in
- **THEN** the page is not cross-origin isolated and sign-in completes successfully.

#### Scenario: Reader cross-origin assets still load
- **WHEN** a reader slice references cross-origin fonts or images
- **THEN** those resources still load and render under the reader's isolation policy.

### Requirement: Narration Resource Lifecycle on Reader Exit

When the user leaves the reader view — navigating to another route or otherwise unmounting it — the reader SHALL stop narration playback, cancel any in-progress synthesis, and release the synthesis worker and loaded model, so no audio continues without visible controls and no worker or model accumulates across reader visits. A synthesis that resolves after the reader has been left SHALL NOT start playback.

#### Scenario: Leaving the reader stops playback and releases the worker
- **WHEN** narration is playing and the user navigates away from the reader
- **THEN** playback stops and the synthesis worker is terminated.

#### Scenario: In-progress synthesis is cancelled on exit
- **WHEN** synthesis is still in progress and the user navigates away from the reader
- **THEN** the in-progress synthesis is cancelled and its later completion does not start playback.

#### Scenario: No per-visit resource leak
- **WHEN** the user opens and leaves the reader repeatedly
- **THEN** synthesis workers and loaded models are not accumulated across visits.
