# Reader Specification Delta: Debounced Lookahead Prefetching

## Requirements

### Requirement: Debounced Lookahead Prefetching

The reader view (`/read/[bookId]`) and the daily focus view (`/today`) SHALL apply a 2.5-second debounce delay to lookahead prefetch triggers (`triggerLookaheadPrefetch` / `triggerNextDayPrefetch`). If the user changes slices or navigates away before the 2.5-second timer elapses, the pending prefetch timer SHALL be immediately canceled.

#### Scenario: User pauses on a slice to read

- **GIVEN** user is viewing slice 4
- **WHEN** user remains on slice 4 for at least 2.5 seconds
- **THEN** system triggers lookahead prefetch for slice 5 in the background.

#### Scenario: User rapidly clicks through table of contents

- **GIVEN** user is browsing the chapter list
- **WHEN** user clicks slice 1, then slice 2, then slice 3 within 1.5 seconds
- **THEN** intermediate prefetch timers for slice 2 and slice 3 are canceled before any HTTP request is dispatched
- **AND** only slice 3 schedules a prefetch timer once the user pauses.

#### Scenario: Component unmount cancels pending prefetch

- **WHEN** user leaves the reader route or closes the page while a prefetch timer is pending
- **THEN** the timer is cleared and no background request is dispatched after navigation.
