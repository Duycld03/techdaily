# Proposal: Engineer Profile & Career Portfolio Restructure

## Why

Following the deployment of the Home Bento Dashboard (`/`) and persistent global topbar with active streak badges, the User Profile view (`frontend/pages/profile.vue`) suffers from two key architectural issues:
1. **Redundant Daily Metric Duplication**: The active daily streak and freeze credits are duplicated across the topbar, the dashboard 7-day matrix, and the profile page, diluting the purpose of the profile.
2. **Cross-Column Layout Disconnection**: User identity (avatar, name, email) is rendered on the right column while the form to update that identity is rendered on the left column, with domain mastery progress squeezed underneath.

Restructuring `/profile` establishes a clear separation of concerns: the Home Dashboard serves as the **Daily Operational Cockpit**, while `/profile` becomes the **Senior Engineer Career Portfolio & Passport**, highlighting cumulative long-term engineering assets, domain mastery, and streamlined account management.

## What Changes

- **Full-Width Top Engineer Identity Passport Banner**:
  - Position a prominent, full-width `.glass-card` header across the top of `/profile` featuring large avatar, active status indicator, engineer display name, email, target role badge, account provider (Google / Standard), membership tenure (`Member since`), and personal best record (`Longest Streak` trophy badge).
- **Left Column: Account & Security Hub**:
  - Consolidate all user management into a focused 2-tab glass panel:
    - *Personal Info Tab*: Full name, Target Role selector, interactive Daily Goal Pace chips (`5m`, `10m`, `15m`, `30m`).
    - *Security Tab*: Password update form with current password verification, new password with dynamic strength indicator, confirm password matching, and Google account hint banner.
- **Right Column: Cumulative Milestones & Domain Mastery**:
  - **4-Cell Cumulative Achievement Bento Grid**: Replaces active daily streak with four genuine long-term engineering telemetry cards:
    1. *Architecture Drills*: Total senior interview scenario drills completed and average score (e.g. `8.8 / 10`).
    2. *Interview Quiz Accuracy*: Accuracy percentage and ratio of mastered concepts to total answered.
    3. *Spaced Repetition Memory Vault*: Total active technical concepts retained in the SM-2 review deck (`totalCardsInDeck`).
    4. *Architecture Source Highlights*: Total highlighted code quotes and design takeaways saved (`totalHighlightsSaved`).
  - **4 Universal Pillars Domain Mastery Tracker**:
    - Retain and elevate the four universal engineering pillar progress bars (`Backend Runtime & Concurrency`, `Data Storage & Persistence`, `Distributed Systems & Architecture`, `Frontend & Browser Engineering`) with high-contrast telemetry counters and gradient fills.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Update requirement `User Profile Management, Route Guards & Security` to specify the top full-width Engineer Identity Passport, Left-column Account & Security Hub, Right-column Cumulative Milestones Bento and 4-Pillar Domain Mastery Tracker, and the removal of redundant active streak counters from profile cards.

## Impact

- **API & Domain Contracts**: Zero breaking changes. `GET /api/v1/user/profile`, `PUT /api/v1/user/profile`, and `PUT /api/v1/user/change-password` remain 100% untouched.
- **State & Stores**: `useProfileStore` and `useInterviewQuizStore` already supply all required fields (`totalCardsInDeck`, `totalHighlightsSaved`, `totalDrillsCompleted`, `averageScore`, `accuracyRate`).
- **Unit Tests**: Existing tests in `tests/pages/profile.spec.ts` continue to pass with minimal updates matching the consolidated component layout.
