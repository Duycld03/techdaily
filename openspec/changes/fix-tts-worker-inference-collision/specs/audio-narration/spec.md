# audio-narration Spec Delta

## ADDED Requirements

### Requirement: Serialized Worker Inference & Cancellation Mutual Exclusion

The on-device text-to-speech Web Worker and client audio player controller SHALL enforce strict mutual exclusion for model inference across concurrent or overlapping synthesis requests:

1. **Sequential Model Inference Execution**: The Web Worker SHALL serialize all sentence synthesis calls through an internal queue, guaranteeing that no two neural network inference executions (`entry.synth()`) execute concurrently on the same ONNX Runtime session.
2. **Job Sequencing and Stale Request Invalidation**: Each synthesis request received by the Web Worker SHALL be tagged with a sequential job identifier. When a new synthesis job arrives or cancellation is requested, any queued or in-flight sentences associated with superseded jobs or cancelled request tokens SHALL be discarded immediately without passing to the model inference pipeline.
3. **Unconditional Worker Cancellation on Engine Switch**: When switching narration engines or loading a new slice audio session (`loadAndPlay`), the client SHALL unconditionally issue a cancellation signal to any in-flight worker task before dispatching new synthesis requests.
4. **Worker Fault Recovery**: If the Web Worker terminates unexpectedly or emits an unhandled error (`worker.onerror`), the client layer SHALL nullify and discard the existing worker instance, ensuring that subsequent audio generation attempts spawn a fresh Web Worker rather than failing against a faulted thread.
5. **Debounced Action Triggers During Loading**: The reader audio player action buttons SHALL ignore click events while audio synthesis or buffering is actively in progress (`isLoading === true`), preventing rapid user interactions from queueing duplicate parallel synthesis requests.

#### Scenario: Switching engines mid-synthesis discards prior worker job
- **WHEN** on-device synthesis is actively generating sentences and the user switches to Google Cloud narration
- **THEN** a cancellation message is dispatched to the Web Worker, and remaining sentences from the cancelled job are discarded without running further ONNX model inference.

#### Scenario: Resuming on-device playback executes without ONNX session collision
- **WHEN** playback is paused, switched to Google Cloud, switched back to On-Device, and resumed via the Play button
- **THEN** on-device synthesis resumes sequentially from cached sentences without encountering `Another run() is already in progress` or worker deadlock.

#### Scenario: Clicking Listen button while buffering does not spawn duplicate requests
- **WHEN** the reader audio player is in the loading state (`isLoading === true`)
- **THEN** clicking the Listen/Play button is ignored and does not trigger secondary concurrent synthesis calls.

#### Scenario: Worker error resets worker reference for clean recreation
- **WHEN** the Web Worker encounters an unrecoverable runtime fault
- **THEN** the client discards the worker instance reference, allowing subsequent narration requests to instantiate a clean Web Worker instance.
