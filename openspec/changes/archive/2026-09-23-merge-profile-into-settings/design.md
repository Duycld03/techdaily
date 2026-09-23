# Design: Unified Account & Settings Master-Detail Architecture

## Context

See `proposal.md` for problem background and motivation.

The TechDaily frontend currently maintains two separate account-related pages:
- `/profile`: Manages personal identity (`EngineerIdentityPassport.vue`), milestones (`EngineerMilestonesCard.vue`), personal settings (name, targetRole, daily goal), password security, and domain mastery (`DomainGoalTracker.vue`).
- `/settings`: Adopts `MasterDetailLayout.vue` with a 256px navigation rail, but only hosts 3 configuration panels (`appearance`, `notifications`, `schedule`), resulting in significant unused vertical space on 1080p desktop displays.
- Tab switching in `MasterDetailLayout.vue` exhibits a 1px border flash caused by missing `border border-transparent` in the inactive button style.

## Goals / Non-Goals

**Goals:**
- Consolidate all account profile, security credentials, learning milestones, and system preferences into `frontend/pages/settings.vue`.
- Eliminate the 1px tab switching border flash and layout shift across all `MasterDetailLayout` tab buttons.
- Support bidirectional URL query synchronization (`?tab=<id>`), enabling deep links directly to any settings category.
- Gracefully redirect `/profile` to `/settings?tab=profile` with zero 404s or broken links.
- Maintain 100% backward compatibility with backend endpoints (`/api/v1/user/profile`, `/api/v1/user/change-password`, etc.).

**Non-Goals:**
- Altering backend API contracts, DTOs, or database schemas.
- Modifying the internal logic of domain cards (`EngineerIdentityPassport.vue`, `EngineerMilestonesCard.vue`, `DomainGoalTracker.vue`).

## Decisions

### 1. Tab Border Flicker Elimination Pattern
- **Problem**: Inactive tab buttons had no border defined, while active tab buttons applied `border border-brand-500/20`. When switching tabs, the addition of the 1px border caused a box-sizing subpixel shift and momentary flashing.
- **Decision**: All navigation rail tab buttons SHALL enforce `border border-transparent` in their inactive state:
  ```vue
  :class="[
    'w-full flex items-center justify-between gap-2.5 px-3 py-2.5 rounded-xl text-xs sm:text-sm font-semibold transition-colors duration-200 text-left border',
    activeTab === tab.id
      ? 'bg-brand-500/10 text-brand-600 dark:text-brand-400 font-bold border-brand-500/20 shadow-xs'
      : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-white/[0.04] border-transparent'
  ]"
  ```
- **Rationale**: Retaining a constant 1px border on all states prevents box-sizing recalculation. Scoping the transition to `transition-colors` prevents width/padding micro-jitters.

### 2. Six-Category Master-Detail Taxonomy
- **Decision**: Structure the unified Settings navigation rail into 6 intuitive categories:
  ```
  +-----------------------+-----------------------------------------------+
  | Nav Rail (w-64)       | Content Panel (flex-1)                        |
  +-----------------------+-----------------------------------------------+
  | [User] Profile        | Engineer Passport + Personal Details Form     |
  | [Lock] Security       | Password Change Form + Google OAuth Link      |
  | [Trophy] Mastery      | Milestones Card + Domain Goal Tracker Grid    |
  | [Globe] Appearance    | Language (EN/VI) + Color Mode (Dark/Light)    |
  | [Bell] Notifications  | Web Push Toggle, Permissions, Test Push       |
  | [Clock] Schedule      | Study Time, Streak Alert, IANA Timezone       |
  +-----------------------+-----------------------------------------------+
  ```
- **Rationale**: Groups personal identity, credential security, and learning achievements under the same administrative hub, eliminating empty desktop voids while keeping each panel focused and uncluttered.

### 3. Deep-Link Tab Query Synchronization
- **Decision**: 
  - On mount, parse `route.query.tab` as `SettingsTab`. If valid, set `activeTab.value = route.query.tab`.
  - When the user clicks a rail button, call `router.replace({ query: { ...route.query, tab: newTab } })`.
  - Using `router.replace` avoids polluting the browser back-button history while preserving the active tab during page reloads or bookmark sharing.

### 4. Seamless `/profile` Route Redirect
- **Decision**: In `frontend/pages/profile.vue`, execute a client-side and SSR redirect:
  ```vue
  <script setup lang="ts">
  definePageMeta({
    middleware: [
      () => navigateTo('/settings?tab=profile', { redirectCode: 301, replace: true })
    ]
  })
  </script>
  ```
- **Rationale**: Guarantees that existing bookmarks, direct URLs, or stale references route seamlessly to the new location without user friction.

### 5. Application Shell Navigation Consolidation
- **Decision**:
  - In `frontend/composables/useNavigationMenu.ts`: Under `nav.group_account`, remove the standalone Profile entry and rename Settings to `nav.settings_profile` ("Settings & Profile" / "Cài Đặt & Hồ Sơ") linking to `/settings`.
  - In `frontend/components/layout/AppHeader.vue`: Update the user avatar chip link from `/profile` to `/settings?tab=profile`.

## Risks / Trade-offs

- **Risk**: Existing unit tests in `frontend/tests/pages/settings.spec.ts` expect specific elements on mount.
  - **Mitigation**: Update tests to select the appropriate tab before interacting with controls, or test tab navigation explicitly.
- **Risk**: Performance when loading multiple stores on a single page.
  - **Mitigation**: Pinia stores (`useProfileStore`, `useQuizStore`, `useWebPush`) are already cached and lightweight. Content panels are conditionally rendered (`v-if="activeTab === '...'"`), avoiding redundant DOM tree creation.
