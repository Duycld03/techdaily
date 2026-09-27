# Design

## Context

See `proposal.md` for background and problem motivation.

In `ReaderAudioPlayer.vue` and `useSliceAudio.ts`, three UX issues occur during audio state transitions:
1. `onToggleEngine` in `ReaderAudioPlayer.vue` unconditionally reloaded and played audio whenever `loadedId.value` was truthy, even when the user had explicitly paused or stopped playback before clicking Cloud or Device.
2. In `useSliceAudio.ts`, local on-device TTS playback uses HTML5 `<audio>` element with progressive chunk streaming. When background synthesis completes and assembles the full slice audio (`complete`), it calls `setSource(complete)` and assigns `el.currentTime = offsetSec` synchronously. Because assigning `el.src` drops `el.readyState` to `0` (`HAVE_NOTHING`), the synchronous `currentTime` assignment fails or is wiped out when metadata loads. When the user later attempts to resume playback, the audio element is stuck, ignores play commands, or stays frozen at the old minute.
3. When restoring on-device partial progress from IndexedDB (e.g. 5 of 15 sentences), `playPreRoll` only took the first 2 chunks (`buffers.slice(0, target)`), discarding sentences 2, 3, 4 from the restored audio, and causing index desynchronization when background synthesis delivered subsequent chunks.

## Goals / Non-Goals

**Goals:**
- Eliminate unwanted autoplay when toggling narration engines while paused.
- Support seamless engine switching with continued playback when toggling while actively playing.
- Ensure 1-tap Cloud fallback recovery button explicitly starts playback.
- Fix local on-device TTS resume so audio resumes cleanly from the exact paused timestamp without freezing or getting stuck at the old minute.
- Ensure audio restarts from 0:00 when clicking Play on an ended audio track.
- Fully restore all $K$ partial chunks on page refresh or engine re-selection, providing immediate playback for all $K$ sentences and continuing background synthesis for sentences $K \dots N-1$.

**Non-Goals:**
- Altering Google Cloud TTS proxy backend endpoints or caching semantics.
- Changing Transformers.js model weights or quantization logic.

## Decisions

### 1. State-Aware Engine Toggle in `ReaderAudioPlayer.vue`

**Decision:**
In `ReaderAudioPlayer.vue`, modify `onToggleEngine` to accept an `autoPlay` parameter defaulting to `playing.value`:
```typescript
function onToggleEngine(mode: AudioEngine, autoPlay = playing.value): void {
  if (mode === 'cloud' && (isNearQuota.value || isQuotaExhausted.value)) {
    return
  }
  setEngineMode(mode)
  if (loadedId.value) {
    pause()
    loadedId.value = null
    if (autoPlay && source.value) {
      loadedId.value = source.value.chunkId
      void loadAndPlay(source.value)
    } else {
      currentTime.value = 0
      duration.value = 0
      status.value = 'idle'
    }
  }
}
```
For `onFallbackToCloud()`, pass `autoPlay = true`:
```typescript
function onFallbackToCloud(): void {
  onToggleEngine('cloud', true)
}
```

**Rationale:**
- When the user is paused, `playing.value === false` $\rightarrow$ `autoPlay === false`. Switching engines updates the engine selection and clears old timers, leaving the player in a clean paused state.
- When the user is playing, `playing.value === true` $\rightarrow$ `autoPlay === true`. Switching engines reloads and continues playback smoothly.
- When clicking the error fallback recovery button, `autoPlay === true` immediately begins Cloud playback as requested.

### 2. Reliable `loadedmetadata` Seeking in `useSliceAudio.ts`

**Decision:**
In `useSliceAudio.ts`, enhance `setSource` to accept an optional `initialOffset` and register a one-time `loadedmetadata` listener:
```typescript
function setSource(blob: Blob, initialOffset = 0): void {
  const el = ensureAudio()
  if (!el) return
  if (objectUrl) URL.revokeObjectURL(objectUrl)
  objectUrl = URL.createObjectURL(blob)
  el.src = objectUrl
  el.playbackRate = speed.value

  if (initialOffset > 0) {
    const applyOffset = () => {
      try {
        el.currentTime = initialOffset
      } catch {
        // Ignored if media element prevents seeking
      }
    }
    if (el.readyState >= 1) { // HAVE_METADATA
      applyOffset()
    } else {
      el.addEventListener('loadedmetadata', applyOffset, { once: true })
    }
  }
}
```

**Rationale:**
Setting `el.src` asynchronously reloads the audio resource. Synchronous assignments to `currentTime` before `loadedmetadata` are dropped by browser rendering engines. Listening for `loadedmetadata` ensures `currentTime` is applied as soon as the media timeline is established.

### 3. Handle Ended Audio in `play()`

**Decision:**
In `useSliceAudio.ts`:
```typescript
async function play(): Promise<void> {
  const el = ensureAudio()
  if (!el) return
  try {
    if (el.ended || (el.duration > 0 && el.currentTime >= el.duration)) {
      el.currentTime = 0
    }
    await el.play()
  } catch {
    // Autoplay rejections are non-fatal
  }
}
```

**Rationale:**
Calling `play()` on an HTML5 `<audio>` element that reached `ended` without resetting `currentTime` does not restart playback on many browsers, appearing stuck at the final timestamp.

### 4. Comprehensive Partial Cache Restoration & Continuous Playback

**Decision:**
In `useSliceAudio.ts`, when restoring partial chunks from IndexedDB ($K$ chunks where $K \ge \text{target}$):
```typescript
const playPreRoll = (count = target) => {
  if (activeKey !== key || buffers.length < count) return
  isPlayingPreRoll = true
  currentPlayingIndex = count - 1
  const preRollWav = encodeWav(concatFloat32(buffers.slice(0, count)), sampleRate)
  setSource(preRollWav)
  status.value = 'ready'
  void play()
}

if (buffers.length >= target) {
  // If restoring from partial cache with e.g. 5 chunks, assemble all 5 chunks into preRoll!
  playPreRoll(buffers.length)
}
```

**Rationale:**
If 5 sentences were already generated and stored in IndexedDB, assembling all 5 sentences into the initial playable audio gives the user instant access to ~30 seconds of speech. The background worker synthesizes sentences 5 through 14 while the user listens to sentences 0 through 4. When chunk 4 ends, `onChunkEndedCallback` naturally chains into chunk 5.

## Risks / Trade-offs

- **Risk:** Rapid double-clicking of the engine toggle button while audio is loading.
  - *Mitigation:* `loadedId` and `activeKey` isolation discard out-of-order chunks and abort previous worker synthesis.
