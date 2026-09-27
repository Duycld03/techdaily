# audio-narration Spec Delta

## MODIFIED Requirements

### Requirement: Adaptive Pre-roll Playback Buffer Threshold

The reader audio engine SHALL enforce a low-latency pre-roll playback buffer threshold:

1. For short documents with 2 or fewer sentences, the engine SHALL buffer all sentences (`sentences.length`) before starting playback.
2. For documents with more than 2 sentences, the engine SHALL cap the pre-roll buffer target at a maximum of 2 sentences (`Math.min(2, sentences.length)`), preventing excessive multi-minute buffering delays on long reading slices.
3. The player SHALL concatenate and begin playing the initial buffered audio as soon as the first 2 sentences (or all sentences for short documents) are synthesized, while background synthesis continues sequentially streaming the remaining sentences.

#### Scenario: Long slice begins playback after 2 sentences
- **WHEN** a user initiates on-device narration on a 36-sentence reading slice
- **THEN** the reader audio engine sets the pre-roll buffer target to exactly 2 sentences and starts audio playback as soon as sentence 2 completes, rather than delaying playback until 12 or more sentences are synthesized.

#### Scenario: Short slice buffers all sentences
- **WHEN** a user initiates on-device narration on a 2-sentence reading slice
- **THEN** the reader audio engine sets the pre-roll buffer target to 2 sentences and starts playback once all 2 sentences are synthesized.

## ADDED Requirements

### Requirement: Model Download Progress Precedence & Compute Device Status Presentation

The reader audio player SHALL provide accurate visual feedback during on-device model initialization and compute execution:

1. **Download Progress Precedence**: While model weights are actively being downloaded over the network (`downloadProgress > 0` and `< 100`), the status display SHALL show the download percentage (`Downloading voice model… X%` / `Đang tải giọng đọc… X%`). The buffering indicator (`0/N`) SHALL NOT mask or shadow the download progress.
2. **Compute Device Badge Rendering**: The player SHALL bind and render the active compute backend badge (`[GPU]` or `[CPU]`) without emitting component instance template warnings, clearly informing the user whether narration is executing on hardware-accelerated WebGPU or multi-threaded CPU WebAssembly.

#### Scenario: Model download percentage takes visual precedence over buffering counter
- **WHEN** an on-device narration model is downloading 100 MB of ONNX weights over the network
- **THEN** the player displays "Downloading voice model… X%" (or "Đang tải giọng đọc… X%") with the live percentage, and does not display "Buffering audio… 0/N".

#### Scenario: Compute device badge displays active hardware backend
- **WHEN** on-device synthesis resolves the active device backend (WebGPU or WASM CPU)
- **THEN** the player displays the corresponding device badge and tooltip without logging property access errors in the browser console.
