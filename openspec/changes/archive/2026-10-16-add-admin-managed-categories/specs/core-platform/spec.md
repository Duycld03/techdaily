# Spec Delta

## MODIFIED Requirements

### Requirement: User Profile Management, Route Guards & Security
The user profile subsystem SHALL retain authenticated identity/profile/password management, technology-agnostic mastery tracks, daily pace, schedules and alert preferences. Missing JWT authentication SHALL return HTTP 401 and protected frontend routes SHALL remain guarded.

Mastery Track settings SHALL retain Deep Work & Focus Practitioner, System Architect & Decision Maker, Lifelong Polymath & Cognitive Explorer, Clean Code & Software Craftsperson, and Technical Leader & Engineering Mentor. Existing Senior Engineer, Staff Engineer, Principal Architect, Tech Lead, Mid-Level Engineer and Junior Engineer values SHALL continue to display their localized matching options. New accounts SHALL retain Deep Work & Focus Practitioner as their default track.

Existing profile update semantics SHALL remain unchanged: identity-only updates send `name`, `targetRole` and `dailyGoalMinutes`; schedule/timezone fields that are null or omitted SHALL retain their existing values, and only non-null supplied properties SHALL be updated. Schedule updates retain documented validation. Current unified Settings navigation and `/profile` redirection SHALL remain governed by existing Settings/redirect requirements. This taxonomy change SHALL NOT recreate a removed dashboard, mastery widget or statistics endpoint.

Where a profile dashboard is rendered, its existing identity passport, four milestone metrics (drill count/score, quiz accuracy/mastered ratio, active memory-card count and saved highlights), account/security controls, responsive arrangement and bilingual text SHALL remain intact. Category identity SHALL NOT change account target-role options, daily pace choices or notification schedules.

Any existing domain-mastery presentation SHALL group actual owned reading/recall activity by stable category UUID and display current stored names, including inactive categories with history. It SHALL NOT infer four universal pillars from keywords or create metrics for categories without activity. This conditional rule does not require adding a currently absent presentation.

#### Scenario: Unauthenticated request to user profile
- **WHEN** a client requests the profile without authentication
- **THEN** HTTP 401 is returned.

#### Scenario: User updates profile settings
- **WHEN** an authenticated user submits target-track and learning-goal changes
- **THEN** the profile is updated and the updated DTO is returned.

#### Scenario: User configures personal study schedule and timezone
- **WHEN** an authenticated user submits preferred study time 07:30, streak alert 21:00, timezone Asia/Ho_Chi_Minh, and push enabled
- **THEN** the timezone is validated and preferences are persisted and returned.

#### Scenario: Validation of invalid timezone identifier
- **WHEN** the timezone is invalid or unrecognized
- **THEN** the existing safe UTC fallback or HTTP 400 `VALIDATION_FAILED` behavior remains available.

#### Scenario: User updates personal profile information from `/profile`
- **WHEN** an identity-only profile action updates name, target role or daily goal
- **THEN** only those fields are submitted to the profile update endpoint
- **AND** omitted schedule/timezone preferences remain unchanged and a localized success toast appears.

#### Scenario: User selects Deep Work & Focus Practitioner mastery track
- **WHEN** the user selects Deep Work & Focus Practitioner in Settings and saves
- **THEN** the profile request retains that target-role value and returns HTTP 200 after persistence.

#### Scenario: Existing user with legacy Senior Engineer role views settings
- **WHEN** an existing Senior Engineer account opens Settings
- **THEN** its localized legacy track label appears without a blank option or console error.

#### Scenario: Taxonomy administration does not reintroduce obsolete profile pillars
- **WHEN** categories are introduced or renamed
- **THEN** `/profile` continues to redirect to unified Settings and no absent four-pillar widget is recreated
- **AND** category-free profile statistics and identity/security forms retain their existing contracts.

### Requirement: Universal DeepPace Production Micro-Branding & Media Session Identity
All user-facing media sessions, service-worker notification handlers and diagnostic payloads SHALL retain the DeepPace brand:
1. Audio MediaSession defaults SHALL use artist DeepPace and album DeepPace Reader unless book attribution overrides them.
2. Push notifications with no custom title SHALL use DeepPace.
3. The authenticated test-push endpoint SHALL use title `DeepPace Test Push 🚀` and tag `deeppace-test`.
4. Page titles SHALL use DeepPace and authentication diagnostics the `[DeepPace Auth]` prefix.

Existing identity/profile milestone rendering and partial-update behavior SHALL remain unchanged where those surfaces exist; current Settings/redirect requirements govern actual route placement. Category-related displays SHALL use stable IDs/current bilingual descriptors rather than four fixed keyword-matched pillars. No new profile widget is required by this change.

#### Scenario: Background audio player registers MediaSession
- **WHEN** a user plays narration
- **THEN** OS media metadata displays DeepPace and DeepPace Reader unless overridden by book author metadata.

#### Scenario: Service worker receives push event with empty title
- **WHEN** a push arrives without a title
- **THEN** its title is DeepPace.

#### Scenario: Developer triggers test push notification
- **WHEN** an authenticated user selects Send Test Push in Settings
- **THEN** the payload contains `DeepPace Test Push 🚀` and `deeppace-test`.

#### Scenario: User inspects `/profile` interface for decluttering
- **WHEN** an existing identity-only profile presentation is displayed
- **THEN** it contains no notification schedule/timezone controls or navigation bridges duplicating Settings
- **AND** the current profile redirect remains unchanged.

#### Scenario: Server processes partial profile update from `/profile`
- **WHEN** name, target role and daily goal are supplied while schedule/timezone fields are null or omitted
- **THEN** only non-null supplied properties and the update timestamp change, null/omitted preferences retain their values, and HTTP 200 returns the profile DTO.

#### Scenario: Desktop 2-column vs mobile single-column stacked responsive layout
- **WHEN** an existing profile dashboard is displayed on desktop
- **THEN** identity, milestone telemetry and balanced account/mastery panels retain their established responsive arrangement
- **WHEN** it is displayed on mobile
- **THEN** the content stacks with touch-friendly actions and no horizontal overflow
- **AND** this change does not add a removed dashboard to the current Settings flow.

#### Scenario: User inspects identity and learning milestones widget
- **WHEN** an existing profile milestones presentation is rendered
- **THEN** drills/score, quiz accuracy/mastered ratio, active SM-2 cards and saved highlights retain their authentic counts and localized compact labels, with separate hairline-bordered surfaces.

#### Scenario: User monitors curriculum domain mastery goal progress
- **WHEN** an existing domain-mastery presentation displays reading/recall activity
- **THEN** it groups by authoritative category UUID and stored names, including administrator-created subjects
- **AND** no four-pillar keyword mapping or invented category totals are used.

#### Scenario: Bilingual visual verification for identity widget, milestone stats, and domain goal tracker
- **WHEN** an existing identity/telemetry presentation changes locale
- **THEN** titles, milestone labels and any category names update without diacritic clipping or badge collisions
- **AND** concise quiz-accuracy labels and a single nonduplicated page subtitle remain intact.

### Requirement: AI Term Explanation & Cache Protocol
Term explanation SHALL check exact and semantic caches before invoking the configured Gemini generation service. An uncached explanation failure SHALL return `AiService.Unavailable` with a non-200 error, never generic boilerplate as success. Free-text explanation context SHALL be named `contextLabel`, not treated as a document category UUID or a fixed curriculum selection.

The term-explanation endpoint SHALL preserve its term, surrounding context and locale semantics. Valid historical free-text cache labels/explanations/vectors SHALL survive the rename and startup unchanged; existing documented hygiene removal of poisoned boilerplate records remains in force. Category renaming SHALL NOT rewrite caches or regenerate embeddings, and missing context labels SHALL not force Software Architecture or another category assignment.

#### Scenario: Term explanation fails during cache miss
- **WHEN** an uncached term request fails at the AI service
- **THEN** the API returns a failure status and `AiService.Unavailable`
- **AND** it neither caches nor returns generic boilerplate text.

#### Scenario: Historical context label is not a taxonomy reference
- **WHEN** a retained cache entry contains a free-text label such as Postgres or Software Architecture
- **THEN** its label remains available as `contextLabel` without creating or assigning a category
- **AND** its explanation/vector remains unchanged and does not block migration as an unknown enum value.

#### Scenario: Category rename preserves term cache contents
- **WHEN** a category used by an open reader is renamed
- **THEN** the reader displays the updated category descriptor separately from its term-explanation context
- **AND** no historical cache/vector rewrite or AI generation occurs as a side effect.

### Requirement: Interactive API Documentation & OpenAPI Explorer
The backend SHALL generate OpenAPI 3.1 metadata and serve Scalar at `/scalar/v1` during development, with JWT Bearer input, concise navigation, complete response schemas/status codes and automated TypeScript generation.

1. **Security and authorization:** `/openapi/v1.json` SHALL define `components.securitySchemes.Bearer` and document-level `security: [{ "Bearer": [] }]` so Scalar injects the supplied token.
2. **Operation copy:** Minimal API summaries SHALL be concise human-readable 2–5 word titles. Detailed behavior, invariants and limits SHALL remain in descriptions. Every endpoint, including Library, Notes and health, SHALL declare tags, summary and description. `/health` SHALL be categorized as System Diagnostics & Health with summary System Health & Database Liveness.
3. **Legacy documentation URLs:** `/swagger` and `/swagger/index.html` SHALL return HTTP 302 to `/scalar/v1` and remain excluded from OpenAPI. Existing project documentation SHALL reference Scalar and `/openapi/v1.json`.
4. **Canonical frontend generation:** `npm run gen:api` SHALL use openapi-typescript to query the running backend and output `frontend/app/types/api.generated.ts`, not a second `frontend/types/api.generated.ts`. Generated category contracts SHALL use UUIDs and descriptors.
5. **Typed bodies:** Every payload-producing endpoint SHALL declare a named concrete success DTO, including authentication, notifications, profile, AI health and health probes. Non-JSON responses such as Markdown export SHALL declare their actual content type/status.
6. **Error contracts:** All producible statuses SHALL be documented. Authenticated operations include 401, input validation includes 400 and identifier lookup includes 404. Domain/validation failures SHALL use RFC 7807 application/problem+json with a stable code, not anonymous code/error JSON.
7. **Runtime/documentation parity:** Resource creation returning its representation SHALL use 201 and Location where a canonical URL exists; bodyless mutations 204; payload reads/commands 200. Session endpoints retain 200. Documented and runtime status sets SHALL match.
8. **Technology-agnostic API:** No operation copy SHALL reference the retired fixed 30-day curriculum; no GET curriculum/roadmap endpoint SHALL appear. Roadmap data continues to derive from owned books and ordered chunks.

#### Scenario: Developer accesses interactive API documentation in development
- **WHEN** a developer opens `/scalar/v1` in development
- **THEN** Scalar uses the Moon dark theme and concise sidebar operation titles without multi-sentence wrapping.

#### Scenario: Developer authorizes API requests via JWT Bearer in Scalar
- **WHEN** a valid token is entered in Scalar
- **THEN** subsequent requests include the Authorization Bearer header.

#### Scenario: Legacy Swagger URL is requested
- **WHEN** `/swagger` or `/swagger/index.html` is requested
- **THEN** HTTP 302 redirects to Scalar and the redirect routes are absent from its index.

#### Scenario: Frontend developer generates OpenAPI TypeScript types
- **WHEN** a developer runs `npm run gen:api` from frontend with the backend running
- **THEN** it queries `http://localhost:5000/openapi/v1.json`
- **AND** writes all endpoint paths/request/response schemas to `frontend/app/types/api.generated.ts` without a competing generated file.

#### Scenario: Developer inspects an authenticated read operation in Scalar
- **WHEN** an authenticated read is inspected
- **THEN** its 200 response displays the concrete DTO fields and its 401 response uses problem details rather than an empty No Body panel.

#### Scenario: Developer inspects and calls a resource-creation operation
- **WHEN** a resource-creation operation is inspected and invoked
- **THEN** documented and runtime responses both use 201 with the concrete created-resource body.

#### Scenario: Endpoint returns a domain error
- **WHEN** a domain or validation error occurs
- **THEN** its problem+json body contains the domain code and its status is documented for the operation.

#### Scenario: Curriculum roadmap operation copy is technology-agnostic
- **WHEN** Scalar and the OpenAPI document are inspected
- **THEN** no GET curriculum/roadmap operation or fixed 30-day curriculum copy appears.
