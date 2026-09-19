# Spec Delta: Core Platform

## ADDED Requirements

### Requirement: Application Shell Navigation & Brand Logo Routing
The application shell topbar (`AppHeader.vue`) SHALL provide intuitive, direct navigation back to the user's primary dashboard overview. Clicking the primary TechDaily brand logo and monogram in the top header SHALL navigate to the root Home Bento Dashboard (`/`) rather than an internal practice subroute.

#### Scenario: Brand logo navigation returns user to home dashboard
- **WHEN** a user clicks the TechDaily brand logo in the top application header (`AppHeader.vue`)
- **THEN** the application navigates to the root Home Bento Dashboard (`/`) instead of `/today`.
