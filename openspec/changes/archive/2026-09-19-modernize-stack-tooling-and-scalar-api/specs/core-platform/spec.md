# Spec Delta: Core Platform

## ADDED Requirements

### Requirement: Interactive API Documentation & OpenAPI Explorer
The backend system SHALL generate OpenAPI 3.1 specification metadata and serve an interactive developer API explorer via `Scalar.AspNetCore` at route `/scalar/v1` during development environment runs. The API documentation SHALL support JWT Bearer authentication input, accurately reflect all Minimal API route groupings and status codes, and feature dark-theme styling consistent with TechDaily's Dev-Learning Studio theme. Legacy `/swagger` route requests SHALL be gracefully redirected to `/scalar/v1`.

#### Scenario: Developer accesses interactive API documentation in development
- **WHEN** a developer navigates to `/scalar/v1` in the development environment
- **THEN** the system serves the Scalar API explorer rendered with dark theme
- **AND** all registered Minimal API endpoints, parameter contracts, and RFC 7807 problem detail schemas are listed.

#### Scenario: Developer authorizes API requests via JWT Bearer in Scalar
- **WHEN** a developer provides a valid JWT token in Scalar's security definition dialog
- **THEN** subsequent test requests executed from the Scalar UI include the `Authorization: Bearer <token>` header.

#### Scenario: Legacy Swagger URL is requested
- **WHEN** a user or client requests `/swagger` or `/swagger/index.html`
- **THEN** the server responds with a redirect to `/scalar/v1`.

---

### Requirement: Frontend Composable Utilities & DOM Lifecycle Hygiene
The web frontend SHALL integrate `@vueuse/nuxt` to standardize declarative DOM event listeners, outside-click detection, and debounce utilities across Vue components. Components requiring document-level event listeners or outside-click triggers MUST use VueUse composables (`onClickOutside`, `useEventListener`, `useDebounceFn`) rather than manual `document.addEventListener` / `window.addEventListener` bindings to eliminate memory leaks and ensure SSR hydration safety.

#### Scenario: User clicks outside a floating dropdown or modal
- **WHEN** a component utilizing `onClickOutside` (such as `AppSelect.vue` or `QuickHelpModal.vue`) is open and user clicks outside its target boundary
- **THEN** the component state closes smoothly without throwing SSR mismatch warnings or leaving unmanaged event listeners on unmount.

#### Scenario: Keyboard shortcuts and window event listeners detach cleanly
- **WHEN** a component registering window shortcuts via `useEventListener` is unmounted
- **THEN** all associated event listeners are automatically detached by VueUse without manual `onBeforeUnmount` boilerplate.
