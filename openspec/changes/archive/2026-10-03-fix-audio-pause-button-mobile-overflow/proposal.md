# Proposal

## Why

When listening to audio narration in the technical reader on mobile viewports (360px–390px), transitioning the audio play/pause toggle into its active playing state swaps the short label ("Nghe" / "Listen") for the verbose translation string `reader.audio_pause` ("Tạm dừng giọng đọc" in Vietnamese, "Pause narration" in English). 

Because both the left playback cluster (play/pause toggle, engine switch) and right utility cluster (auto-advance, sleep timer, speed button) enforce `shrink-0 whitespace-nowrap` within a `justify-between` flex container, the expansion of the pause button pushes the rightmost utility controls—specifically the 1x/2x playback speed button—completely outside the reader card boundary and off-screen.

## What Changes

- **Concise Play/Pause Localized Action Labels**: Update `reader.audio_pause` in `vi.json` to "Tạm dừng" (down from "Tạm dừng giọng đọc") and in `en.json` to "Pause" (down from "Pause narration"), ensuring concise, balanced action labels symmetrical with "Nghe" / "Listen".
- **Accessible ARIA Descriptions**: Maintain descriptive accessibility labels for screen readers (`reader.audio_play` and `reader.audio_pause_narration` or accessible aria-labels) so accessibility context is preserved without bloating visible button dimensions.
- **Mobile Responsive Control Layout**: Refine Row 1 control clustering and spacing in `ReaderAudioPlayer.vue` so that all controls (Play/Pause, Engine Switch, Auto-Advance, Sleep Timer, Speed Selector) remain 100% visible and contained inside the reader card on mobile screens down to 360px without horizontal overflow.
- **Test Suite Updates**: Update test fixtures and assertions in `ReaderAudioPlayer.spec.ts` to assert the concise button label contract across playback state transitions.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `audio-narration`: Update the `Reader Audio Playback Controls` requirement to mandate that the play/pause button uses concise localized labels ("Tạm dừng" / "Pause") during playback and that all primary and utility audio controls maintain complete visibility inside the player card on mobile viewports down to 360px without horizontal overflow.

## Impact

- Frontend components: `frontend/components/reader/ReaderAudioPlayer.vue`
- Localization catalogs: `frontend/i18n/locales/vi.json`, `frontend/i18n/locales/en.json`
- Frontend unit tests: `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts`
- No backend, API, or database impact.
