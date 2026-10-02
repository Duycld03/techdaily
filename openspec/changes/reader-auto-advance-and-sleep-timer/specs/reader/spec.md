# Spec Delta

## ADDED Requirements

### Requirement: Audio-Driven Automated Slice Progression and Bookmark Synchronization
When reader audio auto-advances to a subsequent slice, the reader view SHALL automatically synchronize reading state, content display, and progress tracking.
1. **View Transition**: The reader SHALL update `activeChunkIndex`, switch the visible slice content, re-render markdown formatting, and reset the reader viewport to the top of the new slice.
2. **Bookmark Persistence**: Upon audio-driven progression, the reader SHALL immediately persist the new slice index to local storage (`techdaily_bookmark_{bookId}`), ensuring that reloading the reader resumes at the newly reached slice.
3. **Pacer Synchronization**: The reader SHALL trigger progress synchronization with the backend `UserBookPacer` API (`completedSlices` update), crediting reading time and streak milestones without requiring manual navigation card clicks.
4. **Active Slice Audio Isolation**: The previous slice's audio or synthesis tasks SHALL be fully detached and stopped before the new slice begins, preventing audio overlap or duplicate workers.

#### Scenario: Audio auto-advances and synchronizes reader bookmark
- **WHEN** audio narration completes on slice 2 and auto-advances to slice 3
- **THEN** the reader view displays slice 3 content, `localStorage.getItem('techdaily_bookmark_<bookId>')` is updated to index 3, and reading progress is recorded with the backend pacer.

#### Scenario: User refreshes page after audio auto-advance
- **WHEN** the user refreshes `/read/[bookId]` after audio has auto-advanced through 3 slices
- **THEN** the reader initializes at slice 3 with saved bookmark intact.
