# Design: Engineer Profile & Career Portfolio Restructure

## Context

TechDaily's Home Dashboard (`/`) provides a rich, real-time daily operational cockpit (active reading slice, scenario drill, concentric metrics, and 7-day consistency matrix with streak flames). Meanwhile, `/profile` currently duplicates the active daily streak and freeze credits, while suffering from a disjointed 2-column layout where the user identity card is on the right column and the form to edit that identity is on the left column.

This design restructures `/profile` into an executive **Senior Engineer Career Portfolio & Passport**, separating daily operational metrics from cumulative career achievements.

## Goals / Non-Goals

**Goals:**
- Provide a full-width **Engineer Identity Passport** banner across the top of `/profile` (`frontend/components/profile/EngineerIdentityPassport.vue`).
- Consolidate all account and security form controls into a unified **Account & Security Hub** in the left column.
- Transform the right column into an **Engineering Telemetry & Domain Mastery Portfolio**:
  - 4-cell Bento Grid displaying cumulative achievements:
    1. Architecture Drills completed with average score (`{totalDrillsCompleted}`, `{averageScore}/10`).
    2. Interview Quiz accuracy with mastered ratio (`{accuracyRate}%`, `{masteredCount}/{totalAnswered}`).
    3. Spaced Repetition Memory Vault (`{totalCardsInDeck}` active SM-2 cards).
    4. Architecture Source Highlights (`{totalHighlightsSaved}` saved highlights).
  - 4 Universal Pillars Domain Mastery Progress Bars (`Backend Runtime`, `Data Storage`, `Distributed Systems`, `Frontend/Browser`).
- Eliminate redundant active daily streak cards from profile, reserving streak tracking for the global topbar and Home Dashboard while retaining all-time `Longest Streak` as a trophy badge on the passport banner.

**Non-Goals:**
- Altering any backend endpoint, database migration, or DTO contract.
- Changing the decluttering boundary between `/profile` and `/settings` (notification schedule remains strictly in `/settings`).

## Decisions

### 1. Architectural Layout & Grid Balance
- **Decision**: Adopt a full-width header + balanced 2-column desktop grid (`lg:grid-cols-2 gap-6`).
- **Rationale**:
  - *Top Banner*: Spanning full width gives user identity executive visual weight, displaying avatar, name, email, target role, account provider, member since date, and all-time longest streak trophy without feeling cramped.
  - *50/50 Split*: Gives the Account & Security Hub ample width for responsive form controls and chips on the left, while giving the 4-cell bento and Domain Mastery progress bars equal prominence on the right.
  - *Mobile Responsive Stacking*: On screens `< 1024px`, elements stack cleanly:
    1. Top Identity Passport
    2. 4-Cell Cumulative Milestones Bento
    3. 4-Pillar Domain Mastery Tracker
    4. Account & Security Form

### 2. Component Decomposition
- **Decision**: Refactor `EngineerProfileHero.vue` into two dedicated, single-responsibility components:
  1. `frontend/components/profile/EngineerIdentityPassport.vue`: Handles the top passport banner.
  2. `frontend/components/profile/EngineerMilestonesCard.vue`: Handles the 4-cell cumulative telemetry bento grid.
- **Rationale**: Clean separation of concerns, zero monolithic component debt, and isolated unit testability.

### 3. Metric Selection & Data Mapping
- **Decision**: Replace `currentStreak` and `freezeCreditsRemaining` in the milestones card with `totalCardsInDeck` and `totalHighlightsSaved`.
- **Mapping**:
  | Card | Metric | Source Field | Icon | Semantic Accent |
  |---|---|---|---|---|
  | 1 | Architecture Drills | `stats.totalDrillsCompleted`, `stats.averageScore` | `Target` / `Terminal` | Violet |
  | 2 | Interview Quiz Accuracy | `quizStats.accuracyRate`, `quizStats.masteredCount` | `CheckCircle2` | Emerald |
  | 3 | Memory Vault (SM-2) | `stats.totalCardsInDeck` | `Brain` / `Layers` | Sky |
  | 4 | Architecture Highlights | `stats.totalHighlightsSaved` | `Highlighter` / `Bookmark` | Amber |
  | Header | Longest Streak Trophy | `stats.longestStreak` | `Trophy` | Gold / Amber |
  | Header | Member Since | `stats.memberSince` | `Calendar` | Slate |

## Risks / Trade-offs

- **Test Suite Updates**:
  - `frontend/tests/pages/profile.spec.ts` asserts user identity and statistics text content.
  - *Mitigation*: Because the text assertions (`Taylor TechLead`, `35`, `profile.domain_mastery`, etc.) are independent of column position, decomposing the component into `EngineerIdentityPassport.vue` and `EngineerMilestonesCard.vue` preserves test assertions with minimal wrapper adjustments.
