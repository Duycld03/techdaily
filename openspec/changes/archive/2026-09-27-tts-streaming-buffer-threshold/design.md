# Design

## Context

Streaming playback immediately upon receiving the first sentence chunk (chunk 0) creates audio underruns when chunk 0 is short (e.g. a 1-second heading like "Overview"). The audio element finishes playing in 1.5 seconds, while chunk 1 (a 200-character paragraph) takes 15–20 seconds to synthesize on a single CPU core. As a result, the audio element reaches `'ended'` prematurely, leading to stuttering, looping, or audio dropouts.

See `proposal.md` for user motivation and feedback.

## Goals / Non-Goals

**Goals:**
- **Pre-Roll Playback Buffer**: Buffer at least 33% of sentences (or 100% for slices $\le 3$ sentences) before starting audible playback.
- **Unified Pre-Roll Audio Block**: Concatenate the initial buffered chunks into a single contiguous WAV segment to provide 20–30+ seconds of continuous playback runway.
- **Runway for Background Worker**: Allow the Web Worker to compute subsequent sentences comfortably while the pre-roll audio plays, avoiding starvation.
- **Accurate Progress Feedback**: Display buffering progress (e.g. `Generating audio... 3/7 (Buffering)`) so the user understands audio will play shortly.

**Non-Goals:**
- Returning to the 8-minute full-slice blocking wait.
- Replacing the sequential chunk chaining or final full-slice WAV caching.

## Decisions

### 1. Adaptive Buffer Threshold Formula

**Decision:** Define the pre-roll buffer target as:
```typescript
const targetBufferCount = sentences.length <= 3 
  ? sentences.length 
  : Math.max(2, Math.ceil(sentences.length / 3))
```

*Rationale:*
- For a 19-sentence slice (e.g. Slice 2): $\lceil 19 / 3 \rceil = 7$ sentences. 7 sentences of technical content average 25–40 seconds of speech. Synthesizing 7 sentences on CPU takes ~35–45 seconds (vastly better than 489 seconds), giving the user a reasonable wait while guaranteeing that playback never runs out of audio.
- For small slices (1–3 sentences): Wait until all sentences are ready, since synthesis only takes a few seconds anyway.

### 2. Pre-Roll Chunk Concatenation

**Decision:** When `buffers.length === targetBufferCount`, concatenate all $T$ buffered Float32 chunks using `concatFloat32(buffers.slice(0, targetBufferCount))` and encode them into the initial playable WAV.

*Rationale:*
Rather than switching clips 7 times across 7 short individual files, playing the first 7 sentences as a single seamless audio file eliminates 6 clip transition boundaries and ensures 100% gapless audio during the most critical initial listening period.

### 3. Subsequent Chunk Advancement

**Decision:** 
- Set `currentPlayingIndex = targetBufferCount - 1`.
- When the initial concatenated block finishes (`ended` event), advance to chunk `targetBufferCount`.
- Subsequent chunks $T, T+1, \dots$ are queued and played individually (or in batch) until synthesis completes.
- Once all sentences are finished, cut over to the full assembled WAV with playback offset synchronization and store in IndexedDB (`techdaily-audio`).

## Risks / Trade-offs

- **Slightly higher initial start time than single-chunk playback**: Waiting for 7 sentences on 19-sentence slices takes ~30–45s instead of ~7s on a single CPU core.
  *Mitigation:* 30–45s is vastly faster than the original 489s (8.1 minutes) and completely eliminates stuttering. Furthermore, on multi-core environments with cross-origin isolation or WebGPU, 7 sentences compute in <5 seconds.
