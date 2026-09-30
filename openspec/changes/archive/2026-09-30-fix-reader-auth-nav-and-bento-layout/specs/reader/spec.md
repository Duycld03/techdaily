# Spec Delta

## ADDED Requirements

### Requirement: Cross-Origin Isolation Safe History Navigation
The dedicated reader route (`/read/[bookId]`) SHALL enforce cross-origin isolation reloads using document location replacement (`window.location.replace`) rather than location assignment (`window.location.assign`). Reloading under isolation MUST NOT push duplicate or unisolated history entries into the browser history stack, ensuring that activating the browser's back button (`history.back()`) directly navigates back to the preceding application page without triggering reload loops or auth session eviction.

#### Scenario: Reader hard reload replaces history entry
- **WHEN** a user navigates to `/read/[bookId]` via client-side routing and the document is not yet `crossOriginIsolated`
- **THEN** the reader middleware reloads the document using `window.location.replace(to.fullPath)` without pushing an extra entry into browser history

#### Scenario: Browser back button returns to prior page cleanly
- **WHEN** a user on `/read/[bookId]` clicks the browser back button
- **THEN** the browser returns immediately to the page visited before entering the reader, preserving user authentication cookies and storage state without redirecting to `/login`

### Requirement: Contextual Reader Header Back Navigation
The `ReaderHeaderBar` component SHALL render an adaptive back navigation control that returns the user to their originating view (such as the Home Dashboard `/` or Daily Focus `/today`) when navigated from those pages, falling back to `/library` when entered directly or without history.

#### Scenario: Navigating back from reader to dashboard
- **WHEN** a user opens a reading slice from the Home Bento Dashboard and clicks the back button in `ReaderHeaderBar`
- **THEN** the application returns to `/` rather than forcing navigation to `/library`
