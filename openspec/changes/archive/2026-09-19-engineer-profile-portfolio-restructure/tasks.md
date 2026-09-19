# Tasks

## 1. Component Architecture & Decomposition

- [x] 1.1 Create `frontend/components/profile/EngineerIdentityPassport.vue` as the full-width header banner displaying large avatar with active status indicator, display name, email, target role badge, account provider (Google / Email), membership tenure (`Member since`), and personal best `Longest Streak` trophy badge.
- [x] 1.2 Create `frontend/components/profile/EngineerMilestonesCard.vue` as the 4-cell cumulative achievement bento grid displaying Architecture Drills (completed count & average score /10), Interview Quiz Accuracy (% and mastered ratio), SM-2 Memory Vault concepts (`totalCardsInDeck`), and Saved Architecture Highlights (`totalHighlightsSaved`).
- [x] 1.3 Add bilingual localization keys to `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` for cumulative milestones (`profile.memory_vault`, `profile.memory_vault_desc`, `profile.highlights_vault`, `profile.highlights_vault_desc`, `profile.member_since`).

## 2. Profile Page Layout & Integration

- [x] 2.1 Refactor `frontend/pages/profile.vue` to implement the executive layout: render `EngineerIdentityPassport.vue` full-width across the top, followed by a balanced 2-column desktop grid (`lg:grid-cols-2 gap-6`) housing the Account & Security Hub on the left, and `EngineerMilestonesCard.vue` + `DomainGoalTracker.vue` on the right.
- [x] 2.2 Cleanly remove legacy `frontend/components/profile/EngineerProfileHero.vue` now that its responsibilities are decoupled into dedicated passport and milestone cards.

## 3. Automated Verification & Testing

- [x] 3.1 Run full frontend test suite (`npm --prefix frontend test`) to ensure 100% pass rate across all test files including `profile.spec.ts`.
- [x] 3.2 Validate OpenSpec change specifications and sync requirements with `openspec validate --changes` and `openspec validate --specs`.
