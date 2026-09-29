# Spec Delta

## MODIFIED Requirements

### Requirement: GPU-Accelerated On-Device Synthesis with CPU Fallback

The reader SHALL run on-device narration synthesis on the GPU when the browser exposes GPU compute to the synthesis worker and the target neural network architecture is compatible with available WebGPU shader kernels, and SHALL fall back to CPU execution when GPU compute is unavailable or fails to initialize, so synthesis completes on any supported browser. The choice of execution backend SHALL NOT change the produced audio or any user-facing control. A backend-initialization failure SHALL NOT surface as a narration error while a working fallback exists, and the backend selection SHALL NOT be re-probed or re-failed per sentence within a synthesis run.

When the browser exposes a choice of GPU adapters, the worker SHALL request a high-performance adapter so a discrete GPU is preferred over integrated graphics. The worker SHALL report the resolved execution device to the reader, and the reader SHALL surface a localized indication of the active compute device (GPU or CPU) within the audio control, following responsive typography and `whitespace-nowrap shrink-0` layout invariants across both English and Vietnamese locales.

For Meta VITS / MMS-TTS architecture models (`Xenova/mms-tts-*`), whose duration predictors require INT64 indexing on `GatherND` operators unsupported by ONNX Runtime Web's WebGPU kernels, the synthesis engine SHALL route execution directly to the CPU (WASM) backend and SHALL NOT request WebGPU adapters or attempt WebGPU pipeline construction. This guarantees stability against unsupported INT64 shader exceptions and eliminates WebGPU initialization failures across desktop and mobile browsers.

#### Scenario: GPU backend used when available
- **WHEN** GPU compute is available to the synthesis worker and a user plays narration for a compatible model
- **THEN** synthesis runs on the GPU backend and produces playable audio.

#### Scenario: Falls back to CPU when GPU is unavailable
- **WHEN** GPU compute is unavailable or its initialization fails
- **THEN** synthesis falls back to CPU, still completes successfully, and produces the same audio without surfacing a narration error.

#### Scenario: Backend chosen once per model
- **WHEN** the execution backend has been selected for a language's model
- **THEN** subsequent sentences in the run reuse that backend without re-probing or repeating a failed initialization.

#### Scenario: High-performance adapter preferred when a choice exists
- **WHEN** the browser exposes both an integrated and a discrete GPU adapter to the worker
- **THEN** the worker requests the high-performance adapter so the discrete GPU is used for synthesis.

#### Scenario: Active compute device shown in the reader
- **WHEN** synthesis resolves to CPU because the browser exposes no GPU adapter, and the user opens the audio control
- **THEN** the reader displays a localized indication that narration is running on the CPU, in place of relying on the browser console notice.

#### Scenario: MMS-TTS models execute on CPU WASM without WebGPU probing
- **WHEN** a user initiates on-device narration for an English or Vietnamese slice using an MMS-TTS voice model on a device with an active GPU
- **THEN** the synthesis worker selects the CPU (WASM) backend directly without attempting WebGPU adapter initialization or throwing INT64 shader exceptions.

#### Scenario: Audio produced on CPU WASM is complete and seekable
- **WHEN** narration synthesizes on the CPU (WASM) backend
- **THEN** audio completes sentence by sentence, streams without underrun, and is cached in browser IndexedDB for seekable playback.

#### Scenario: Compute device badge indicates CPU execution
- **WHEN** on-device synthesis initializes for an MMS-TTS model
- **THEN** the reader audio player displays the localized CPU compute indicator cleanly without console warnings.

---

### Requirement: Backend-Appropriate Weight Precision

The synthesis worker SHALL load the narration model at the weight precision that minimizes synthesis latency on the selected execution backend, rather than the framework's default. On the CPU (WASM) backend it SHALL use full precision (fp32): the framework's default WASM weights are quantized (int8), which run materially slower on this model because ONNX Runtime Web has no fast int8 kernels, so full precision is faster on the CPU path. On the GPU (WebGPU) backend it SHALL use half precision (fp16). If the preferred precision fails to build for a backend, the worker SHALL fall back to full precision without surfacing a narration error. The choice of weight precision SHALL NOT change any user-facing control, the audio cache key `(chunkId, voice, contentHash)`, or the produced audio.

On mobile devices, the worker SHALL load quantized 8-bit (`q8`) weights (`model_quantized.onnx`, ~36.6 MB), reducing memory consumption and preventing mobile browser tab crashes.

For Meta VITS / MMS-TTS architecture models (`Xenova/mms-tts-*`), the worker SHALL NOT attempt to load half-precision (`fp16`) weights, as the remote ONNX graph definition contains type discrepancies that fail validation and corrupt the framework model pipeline cache.

#### Scenario: CPU path uses full precision for lowest latency
- **WHEN** a user plays narration and synthesis runs on the CPU (WASM) backend on desktop
- **THEN** the worker loads full-precision (fp32) weights rather than the framework's default quantized weights, and produces playable narration.

#### Scenario: GPU path uses half precision
- **WHEN** a user plays narration and synthesis runs on the WebGPU backend
- **THEN** the worker loads half-precision (fp16) weights and produces playable narration.

#### Scenario: Full-precision fallback on preferred-build failure
- **WHEN** the preferred precision for the selected backend fails to build
- **THEN** the worker falls back to full-precision weights, completes synthesis, and surfaces no narration error.

#### Scenario: Precision does not alter cache or controls
- **WHEN** narration is synthesized and later replayed
- **THEN** the cached audio is reused under the same `(chunkId, voice, contentHash)` key and no additional or changed playback control appears.

#### Scenario: Desktop environment loads full-precision weights
- **WHEN** on-device narration is initiated on a desktop browser
- **THEN** the synthesis pipeline loads `fp32` weights and executes multi-threaded WASM inference.

#### Scenario: Mobile environment loads quantized weights
- **WHEN** on-device narration is initiated on a mobile browser (Android Chrome, iOS Safari, or mobile WebKit)
- **THEN** the synthesis pipeline loads `q8` quantized weights (~36.6 MB) and executes single-threaded WASM inference.

#### Scenario: Invalid fp16 weights are never requested for MMS-TTS
- **WHEN** the synthesis worker constructs a pipeline for `Xenova/mms-tts-vie` or `Xenova/mms-tts-eng`
- **THEN** the worker never requests `dtype: 'fp16'`, avoiding graph validation failures and model cache corruption.

---

### Requirement: Actionable Error Diagnostics & Cloud Fallback Recovery

The reader audio composable and player component SHALL capture, diagnose, and present specific diagnostic feedback whenever audio synthesis or playback encounters an error, rather than masking failures behind opaque generic error messages:

1. **Specific Error Diagnostics**: The audio composable and player SHALL capture and display specific diagnostic context (such as memory exhaustion, worker initialization error, network failure, or HTTP error status) instead of an unexplained generic toast.
2. **Console Diagnostics**: The composable layer SHALL log full raw error details and stack traces to `console.error('[useSliceAudio] Device TTS Error:', err)` for all worker errors, lifecycle faults, and unhandled rejections.
3. **Network Error Classification**: Resource loading failures, including WebKit/Safari's `TypeError: Load failed`, CDN connection timeouts, and offline fetch rejections, SHALL be classified as `NETWORK_ERROR` rather than `DEVICE_INIT_FAILED`.
4. **Mobile Hardware Failure Cloud Recommendation**: When on-device synthesis fails on a mobile client due to hardware constraints, memory limits, or worker initialization errors, the player SHALL present an informative localized message explaining that on-device narration is constrained on this device and provide a direct 1-tap action to switch to the Google Cloud engine.
5. **Graceful Engine Switch on Error**: Activating the Cloud fallback action from the error state SHALL immediately switch the active engine mode to Google Cloud, clear the error state, and initiate Cloud narration for the slice without requiring a manual page refresh.

#### Scenario: On-device synthesis failure surfaces diagnostic reason
- **WHEN** on-device synthesis fails due to a worker or memory error
- **THEN** the reader UI displays an error notice containing actionable context explaining the failure rather than a blanket "unknown error".

#### Scenario: Mobile on-device failure offers 1-tap switch to Google Cloud TTS
- **WHEN** on-device synthesis encounters an error on a mobile device and the user's monthly cloud quota is not exhausted
- **THEN** the player displays a localized notification recommending Google Cloud narration with an action button that immediately switches to Cloud mode and plays the audio.

#### Scenario: Worker errors output detailed console diagnostics
- **WHEN** an on-device synthesis worker encounters an error during model download or sentence inference
- **THEN** `useSliceAudio` logs the raw error message to the browser console under `[useSliceAudio] Device TTS Error:` before updating player state.

#### Scenario: WebKit "Load failed" categorized as network error
- **WHEN** a mobile Safari or WebKit browser fails to download model assets and throws `TypeError: Load failed`
- **THEN** the composable categorizes the error as `NETWORK_ERROR` and displays the localized network error message rather than a device hardware failure message.

#### Scenario: Hardware error provides 1-tap Cloud fallback
- **WHEN** on-device synthesis fails due to hardware constraints and the user's monthly cloud quota is not exhausted
- **THEN** the player displays an error notice recommending Google Cloud narration with an action button that immediately switches to Cloud mode and plays the audio.
