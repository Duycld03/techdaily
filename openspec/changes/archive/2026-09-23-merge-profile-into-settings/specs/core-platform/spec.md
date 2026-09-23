# Spec Delta: core-platform

## ADDED Requirements

### Requirement: Unified Account & Settings Master-Detail Architecture
The Settings interface (`frontend/pages/settings.vue`) SHALL serve as the unified Master-Detail Hub for all account identity, credentials, learning telemetry, interface preferences, notifications, and scheduling, consolidating previously separate `/profile` and `/settings` surfaces into a single comprehensive layout using `MasterDetailLayout.vue`.
1. **Consolidated Category Navigation Rail (`#nav`)**:
   - Pinned on the left (`w-full md:w-64 shrink-0`) featuring category icons, titles, and active pills for 3 consolidated sections:
     - `general` (**General & Profile**): Developer identity card (avatar initials, name, email, level badge), 2-column form grid for difficulty track, daily goal, timezone, theme, full name, and interface language, followed by auto-advance toggle card and contextual "Save changes" submit action.
     - `notifications` (**Web Push Notifications**): Browser push toggle switch, active endpoint status banner, test push trigger, and Study & Alert schedule time pickers with dedicated "Save Schedule Preferences" action.
     - `security` (**Security & Password**): Account password change form with real-time strength bar, Google OAuth connection status, and dedicated "Update Password" action.
   - Navigation rail buttons SHALL maintain clean typography without persistent numeric badge counters.
2. **Contextual Per-Tab Action Invariant**:
   - The `#header` template of `MasterDetailLayout` SHALL display only the clean section icon and title, without global save buttons.
   - Each tab SHALL own its dedicated save action button positioned at the bottom right of its respective form content panel.
3. **Comprehensive Bilingual i18n Invariant**:
   - All form controls, select dropdowns, options, placeholders, status callouts, and action buttons SHALL be 100% localized in English and Vietnamese.

#### Scenario: Accessing Unified Settings on Desktop
- **WHEN** an authenticated user opens `/settings` on a desktop browser
- **THEN** the left rail displays the 3 consolidated configuration categories without unread badge noise and the active tab displays its controls and dedicated save button.
---

### Requirement: Zero-Flicker Tab Navigation Invariant
All navigation rail tab buttons in `MasterDetailLayout` and interactive tab switchers SHALL enforce a constant 1px border baseline (`border border-transparent` in inactive state, `border-brand-500/20` in active state) and scoped `transition-colors` rather than `transition-all`.
1. **Layout Shift Elimination**:
   - Tab switching SHALL NOT cause 1px box-sizing height/width jumps or border flashing.
2. **Visual Consistency**:
   - Inactive buttons maintain consistent padding and alignment with active pill buttons.

#### Scenario: Switching Tabs in Master-Detail Settings
- **WHEN** a user clicks between navigation rail tabs in `/settings`
- **THEN** the active tab updates smoothly with zero border flashing, zero layout jumping, and immediate visual feedback.

---

### Requirement: Deep-Linked Tab Synchronization & Profile Route Redirection
The Settings interface SHALL support deep-linking and state preservation via URL search parameters, and existing `/profile` routes SHALL seamlessly redirect to the unified settings view.
1. **URL Query Synchronization**:
   - The active tab SHALL synchronize with `route.query.tab` (e.g. `/settings?tab=security`).
   - Clicking a rail tab updates the URL query without triggering full page reloads.
2. **Profile Route Redirection**:
   - Navigating to `/profile` SHALL immediately redirect to `/settings?tab=profile`, preserving backward compatibility for bookmarks and cached links.
3. **Application Shell Integration**:
   - The topbar user avatar chip (`AppHeader.vue`) SHALL link directly to `/settings?tab=profile`.
   - The sidebar navigation menu (`useNavigationMenu.ts`) under `nav.group_account` SHALL consolidate the separate Profile link into a unified "Settings & Profile" destination.

#### Scenario: Navigating from Legacy Profile Link
- **WHEN** a user navigates to `/profile` or clicks their user avatar in `AppHeader.vue`
- **THEN** the browser lands on `/settings?tab=profile` with the Profile & Identity tab actively selected.
