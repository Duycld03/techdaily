# audio-narration Spec Delta

## ADDED Requirements

### Requirement: Mobile Device Resource Safeguards & Single-Threaded WASM

On mobile browser environments (including iOS Safari, WebKit webviews, and Android mobile browsers), the reader's on-device text-to-speech engine SHALL adapt its execution parameters to respect mobile memory ceilings and process threading restrictions:

1. **Single-Threaded WASM Execution**: The Web Worker SHALL restrict WebAssembly execution to single-threaded mode (`numThreads = 1`) on mobile devices, regardless of whether the document context is cross-origin isolated or reports multiple logical CPU cores (`navigator.hardwareConcurrency > 1`). This prevents thread pool allocation failures, WebKit process crashes, and mobile out-of-memory errors.
2. **Quantized Model Weights on Mobile**: When running on mobile devices, the synthesis pipeline SHALL request quantized model weights (`q8` / `model_quantized.onnx`, ~36.6 MB) instead of unquantized FP32 weights (`model.onnx`, ~109 MB), reducing the memory footprint by over 60% and lowering network bandwidth requirements.
3. **Resilient Pipeline Construction**: Pipeline construction error handling in the Web Worker SHALL properly await fallback promises, ensuring that any WebGPU or precision failures cleanly fall back to WASM without leaving unhandled rejections or uninitialized states.

#### Scenario: Mobile browser forces single-threaded WASM execution
- **WHEN** a user on a mobile device (iOS Safari or Android Chrome) initiates on-device narration in a cross-origin isolated page reporting 8 CPU cores
- **THEN** the on-device Web Worker initializes ONNX WebAssembly with exactly 1 thread (`numThreads = 1`), preventing mobile browser process crash and thread exhaustion.

#### Scenario: Mobile browser loads lightweight quantized model weights
- **WHEN** an on-device narration model is downloaded on a mobile browser
- **THEN** the Web Worker downloads the quantized 8-bit ONNX model (~36.6 MB) rather than the 109 MB FP32 weights, conserving device memory and mobile data.

### Requirement: Actionable Error Diagnostics & Cloud Fallback Recovery

The reader audio player SHALL provide clear, actionable diagnostic feedback whenever audio synthesis or playback encounters an error, rather than masking failures behind opaque generic error messages:

1. **Specific Error Diagnostics**: The audio composable and player SHALL capture and display specific diagnostic context (such as memory exhaustion, worker initialization error, network failure, or HTTP error status) instead of an unexplained generic toast.
2. **Mobile Hardware Failure Cloud Recommendation**: When on-device synthesis fails on a mobile client due to hardware constraints, memory limits, or worker initialization errors, the player SHALL present an informative localized message explaining that on-device narration is constrained on this device and provide a direct 1-tap action to switch to the Google Cloud engine.
3. **Graceful Engine Switch on Error**: Activating the Cloud fallback action from the error state SHALL immediately switch the active engine mode to Google Cloud, clear the error state, and initiate Cloud narration for the slice without requiring a manual page refresh.

#### Scenario: On-device synthesis failure surfaces diagnostic reason
- **WHEN** on-device synthesis fails due to a worker or memory error
- **THEN** the reader UI displays an error notice containing actionable context explaining the failure rather than a blanket "unknown error".

#### Scenario: Mobile on-device failure offers 1-tap switch to Google Cloud TTS
- **WHEN** on-device synthesis encounters an error on a mobile device and the user's monthly cloud quota is not exhausted
- **THEN** the player displays a localized notification recommending Google Cloud narration with an action button that immediately switches to Cloud mode and plays the audio.
