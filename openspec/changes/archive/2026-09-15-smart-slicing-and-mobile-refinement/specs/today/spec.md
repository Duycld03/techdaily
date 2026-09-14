# Delta Spec: Mobile Loading Banner & Viewport Sizing

## ADDED REQUIREMENTS

### Requirement: Centered Padded Loading Banner on Mobile
The `/today` focus loading state SHALL have at least 24px (`p-6`) padding, centered alignment, and a constrained width (`max-w-sm sm:max-w-md`) on mobile viewports.

#### Scenario: User visits /today on mobile during synthesis
- **GIVEN** a user on a mobile viewport (<640px wide) opens `/today`
- **WHEN** the daily focus or scenario challenge is loading
- **THEN** the loading icon and explanatory text are vertically and horizontally centered with comfortable margins
- **AND** the explanatory text does not collide with or touch the device edges.
