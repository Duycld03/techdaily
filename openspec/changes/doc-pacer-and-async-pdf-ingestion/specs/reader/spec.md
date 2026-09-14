# Delta Spec: Reader — Doc Pacer Navigation & Roadmap Synchronization

## MODIFIED Requirements

### Requirement: Active Book Pacer Navigation Bar on /today
The `/today` top navigation bar SHALL display the user's active document book pacer status, replacing the fixed 30-day selector. The pacer bar SHALL display the active book title, current chapter title, slice order, total slices, completion percentage, and previous/next navigation buttons.

#### Scenario: User navigates to /today with an active book
- **WHEN** user loads `/today`
- **THEN** top bar displays `[Book Title] • [Chapter Title] • Slice [X] of [Total] ([Y]%)`.
- **THEN** `DocReaderPane` loads slice $X$ of the active book.
- **THEN** `InterviewChallengePane` loads the trade-off challenge associated with slice $X$.

---

### Requirement: 1-Click Book Switcher & Featured Default Book
The system SHALL ensure the `/today` page is always populated with an active document book. Unauthenticated visitors or new users without an active book SHALL be seamlessly assigned the system's primary featured book (e.g. *PostgreSQL 17 Internals*). The pacer bar SHALL provide an integrated 1-click book switcher dropdown.

#### Scenario: Guest or first-time user visits /today
- **WHEN** an unauthenticated visitor or new user visits `/today`
- **THEN** system automatically loads the primary featured book starting at Slice 1 without displaying empty states or blocking onboarding dialogs.

#### Scenario: User switches active book from top bar
- **WHEN** user clicks the active book title in the pacer bar
- **THEN** a dropdown menu displays the user's in-progress books with their respective progress bars and a button to choose books from `/library`.
- **WHEN** user selects an alternative book from the dropdown
- **THEN** system sets `IsActive = true` for the selected book and refreshes `/today` with its current reading slice.

---

### Requirement: Chapter-Based Roadmap Synchronization on /roadmap
The `/roadmap` page SHALL dynamically reflect the chapter milestone progress of the user's active book rather than a static 30-day timeline. Each chapter milestone SHALL indicate completed slices, active slice, and upcoming slices.

#### Scenario: User views roadmap of active book
- **WHEN** user navigates to `/roadmap`
- **THEN** roadmap displays the active book title, total progress percentage, estimated days remaining at current pace, and sequential chapter milestone cards.
- **WHEN** user clicks the active chapter milestone
- **THEN** application navigates to `/today` positioned at the current reading slice.
