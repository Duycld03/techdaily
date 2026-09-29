# Proposal

## Why

Two requirements that describe the daily drill (the `/today` scenario challenge) currently live inside the `quiz` capability spec instead of `drills`. Runtime behavior is already correctly separated — `drills` is the auto-generated per-reading-slice challenge on `/today`, `quiz` is the user-initiated `/quiz` studio — but the spec ownership does not match, which muddies the capability boundary on paper and risks future edits landing in the wrong spec.

## What Changes

- Relocate `### Requirement: Daily Drill Status Resiliency & State Deserialization` from the `quiz` spec to the `drills` spec, verbatim (header, description, and both scenarios).
- Relocate `### Requirement: Natural Vertical Layout for Scenario Challenge Interface` from the `quiz` spec to the `drills` spec, verbatim (header, description, and both scenarios).
- No code, API, database, or runtime behavior changes — this is a pure spec-ownership realignment. The relocated text is unchanged, so no acceptance criteria change; only which capability spec owns them changes.
- Out of scope: `### Requirement: Flashcard Generation Idempotency from Multiple Sources` stays in `quiz` — it is a cross-cutting SM-2 bridge concern spanning reader highlights and quiz mistakes, not a drill concern, and moving it is a separate decision.

## Capabilities

### New Capabilities

<!-- None. -->

### Modified Capabilities

- `quiz`: Remove the two daily-drill requirements listed above (they describe `drills` behavior, not quiz behavior).
- `drills`: Add the two relocated daily-drill requirements so the capability owns the daily scenario challenge's refresh-state resiliency and vertical layout contracts.

## Impact

- **Specs only**: `openspec/specs/quiz/spec.md` (two requirements removed) and `openspec/specs/drills/spec.md` (two requirements added), realized when this change is synced/archived.
- **No source code, API, DB, or test changes**: the drill implementation (`InterviewChallengePane.vue`, `today.vue`, `DrillStatus`, `GET /api/v1/daily/today`) already satisfies both requirements; only their spec home moves.
