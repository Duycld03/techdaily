# Spec Delta: core-platform

## MODIFIED Requirements

### Requirement: Executive Cockpit Bento Dashboard Layout Integration
The primary root route `/` (`HomeBentoDashboard.vue`) SHALL implement the `BentoDashboardLayout` archetype (`BentoDashboardLayout.vue`), decoupling layout shell geometry from individual card content and providing distinct, uncluttered action pathways for continuous reading and daily practice routines.

1. **Header Slot (`#header`)**:
   - Houses the Welcome & Orientation Banner, displaying the personalized engineer greeting, role target, active reading slice badge, and streak status.

2. **Action Stage Slot (`#action-stage`)**:
   - **Card A (Active Reading Slice Hero)**:
     - Displays the user's ongoing book reading progress: book title, active slice title, summary, estimated read time, and progress bar with percentage.
     - Provides a prominent "Continue Reading →" ("Đọc Tiếp →") primary CTA button that navigates directly to the dedicated Library Reader at that slice (`/read/${bookId}?slice=${currentChunkOrder}`).
   - **Card B (Today's Practice Session Cockpit)**:
     - Serves as the primary entry point for the user's scheduled daily curriculum session (`/today`).
     - **Header & Badges**: Displays the session emblem (`CalendarDays` or `Flame` / `Target`), uppercase category pill "TODAY'S PRACTICE" ("LUYỆN TẬP HÔM NAY"), and curriculum progress badge ("Day {dayOrder} / 30" / "Lộ trình Ngày {dayOrder} / 30").
     - **Status Indicator**: Displays real-time drill status:
       - *Pending*: Subtle pending indicator ("Ready to practice" / "Sẵn sàng luyện tập" or "+10 Points Available").
       - *Submitted / Completed*: Completed badge with score ("Completed: {score}/10" or "Đã hoàn thành").
     - **Session Focus & Itinerary**: Renders the daily curriculum topic title (`topic?.title`) and a structured itinerary breakdown indicating the dual daily components (1 In-Depth Concept Reading + 1 Architectural Scenario Drill).
     - **Action CTA**: Displays a high-contrast action button "Start Today's Practice →" ("Vào Luyện Tập →" / "Bắt Đầu Bài Hôm Nay") that navigates directly to the Daily Focus session (`/today`).

3. **Telemetry Dock Slot (`#telemetry-dock`)**:
   - Houses Card C (7-day Consistency Heatmap and SM-2 Due Count) and Card D (Domain Knowledge Constellation Card), equalizing total vertical height with the action stage.

#### Scenario: User navigates to Today's Practice from Home Dashboard
- **WHEN** an authenticated user clicks the "Start Today's Practice" ("Vào Luyện Tập") CTA button on Card B of the Home Bento Dashboard
- **THEN** the browser navigates directly to `/today`
- **AND** the Daily Focus Cockpit opens with today's reading slice and interview challenge ready for practice.

#### Scenario: User distinguishes between Continue Reading and Today's Practice
- **WHEN** an authenticated user views the Home Bento Dashboard
- **THEN** Card A clearly identifies the active document slice with action "Continue Reading" ("Đọc Tiếp") targeting `/read/${bookId}`
- **AND** Card B clearly identifies the scheduled daily curriculum session with action "Start Today's Practice" ("Vào Luyện Tập") targeting `/today`
- **AND** neither card presents ambiguous or duplicate routing destinations.

#### Scenario: Today's Practice Card reflects daily drill completion state
- **GIVEN** an authenticated user who has already submitted today's architectural drill (`drill.status === 'Submitted'`)
- **WHEN** the user views Card B on the Home Bento Dashboard
- **THEN** Card B displays the completed status badge with earned score
- **AND** the action button indicates "Review Practice" ("Xem Lại Buổi Học") while still routing to `/today`.
