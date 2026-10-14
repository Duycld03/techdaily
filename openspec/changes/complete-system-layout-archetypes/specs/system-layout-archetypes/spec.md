# Spec Delta

## MODIFIED Requirements

### Requirement: StudioLayout Interactive Practice Geometry Invariant
Interactive practice surfaces (`quiz.vue` Arena mode and `review.vue` Tab 1 Flashcard Session) SHALL implement the `StudioLayout` archetype (`StudioLayout.vue`), enforcing balanced engineering density (8/10) on standard 1080p desktop viewports ($W \ge 1280\text{px}$):
1. **Action Stage (`#main`)**: Occupies 65% to 70% width (`lg:w-[68%]`), providing a centered, comfortable stage bounded vertically to prevent bottom fold collisions.
2. **Telemetry Dock (`#dock`)**: Occupies 30% to 35% width (`lg:w-[32%]`), housing live session telemetry (countdown timers, progress meters, SM-2 readouts, keyboard shortcuts) without competing with the primary interaction.
3. **Zero Layout Shifts**: Answering state transitions (question selection, flipping card, answer grading) MUST NOT cause height bouncing, font weight shifts, or page scroll jumping.
4. **Mobile Responsive Degradation**: On mobile screens ($< 1024\text{px}$), the main action stage SHALL occupy 100% width, with telemetry dock elements stacking sequentially below or into a collapsible bottom drawer.

#### Scenario: Desktop 1080p StudioLayout practice distribution
- **WHEN** an engineer begins an interactive quiz or flashcard session on desktop ($W \ge 1280\text{px}$)
- **THEN** the question/flashcard action stage renders in `#main` (68% width), the telemetry dock renders in `#dock` (32% width), and neither column causes window scroll jumping or viewport overflow.

#### Scenario: Mobile viewport responsiveness
- **WHEN** an engineer uses interactive practice on a mobile screen ($< 1024\text{px}$)
- **THEN** the primary interaction fills 100% screen width and telemetry docks collapse cleanly below without clipping option choices or action buttons.
