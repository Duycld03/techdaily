# audio-narration Spec Delta

## MODIFIED Requirements

### Requirement: Seekable Full-Slice Audio Cache

The reader on-device narration engine SHALL clearly indicate the total slice scope and synthesis progress during streaming playback:

1. **Estimated Total Duration During Streaming**: While on-device narration is streaming sentence chunks and has not finished synthesizing the entire slice (`synthIndex < synthTotal`):
   - The reader SHALL compute an estimated total duration for the slice based on average sentence duration or word count.
   - The player time display SHALL present the estimated total duration or indicate that the currently loaded time represents the buffered segment of a longer slice (e.g. displaying synthesis count `(18/67)` alongside the current time).
2. **Persistent Synthesis Progress Visibility**: The reader SHALL display the synthesis progress counter (e.g. `(Đang tạo 18/67)` / `(Synthesizing 18/67)`) whenever on-device synthesis is incomplete, both while audio is playing and while audio is paused or prepared, reassuring the user that the audio will continue through the entire slice.
3. **Seamless Multi-Minute Streaming**: When on-device playback reaches the end of the initial buffered pre-roll audio (e.g. 2:08 across 18 sentences), playback SHALL smoothly continue into subsequent chunks until all sentences across the entire ~6-minute slice are heard.

#### Scenario: Displaying total slice scope during streaming synthesis
- **WHEN** a user views an on-device slice where 18 of 67 sentences have been synthesized (2:08 of audio)
- **THEN** the player displays a synthesis progress indicator `(18/67)` indicating that 18 of the total 67 sentences are buffered and the remaining sentences will continue generating.

#### Scenario: Uninterrupted playback across pre-roll to completion
- **WHEN** a user listens to on-device narration on a 6-minute document slice
- **THEN** playback starts immediately with the buffered pre-roll and continuously plays through sentence 67 without halting or terminating at the initial pre-roll duration.
