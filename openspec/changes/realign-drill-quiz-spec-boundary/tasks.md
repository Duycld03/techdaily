# Tasks

## 1. Spec Relocation Integrity

- [x] 1.1 Confirm the two requirements in `specs/drills/spec.md` (ADDED) are verbatim copies of the current blocks in `openspec/specs/quiz/spec.md` — headers, descriptions, and all four scenarios identical. Verify: a text diff of each relocated block against its source block reports no content differences.
- [x] 1.2 Confirm `specs/quiz/spec.md` (REMOVED) lists both requirement headers, each with a `**Reason**` and `**Migration**`. Verify: `openspec validate realign-drill-quiz-spec-boundary --strict` passes.

## 2. Sync & Archive Verification

- [ ] 2.1 After the change is synced/archived, confirm `openspec/specs/quiz/spec.md` no longer contains either relocated requirement and `openspec/specs/drills/spec.md` now contains both with all scenarios intact. Verify: searching the quiz main spec for the two requirement names returns zero matches, and the drills main spec contains both requirement headers plus their four scenarios.
- [ ] 2.2 Confirm no other `quiz` requirement was altered and the pre-existing `drills` requirements are unchanged. Verify: `openspec validate --specs` reports both `quiz` and `drills` valid with no unintended requirement additions or removals.
