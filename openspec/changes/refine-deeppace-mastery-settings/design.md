# Design

## Context

See `proposal.md` for motivation. DeepPace is a technology-agnostic deliberate practice platform.
Currently:
1. In `User.cs`, the default `TargetRole` is `"Senior Engineer"`.
2. In `AuthEndpoints.cs`, new registrations seed `TargetRole = "Senior Engineer"`.
3. In `settings.vue`, the dropdown options are constrained to software engineer interview titles (`Senior Engineer`, `Staff Engineer`, `Principal Architect`, `Tech Lead`, `Mid-Level Engineer`, `Junior Engineer`).
4. Several frontend and backend files still contain hardcoded references to `'TechDaily'` in user-facing metadata:
   - `frontend/app/composables/useSliceAudio.ts` (`artist: payload.artist || 'TechDaily'`)
   - `frontend/public/sw.js` (`self.registration.showNotification(payload.title || 'TechDaily', ...)`)
   - `backend/src/TechDaily.Api/Endpoints/NotificationEndpoints.cs` (`"TechDaily Test Push 🚀"`)
   - `frontend/app/pages/showcase.vue` (`title: 'Design System — TechDaily'`)
   - `frontend/app/pages/login.vue` (diagnostic logs)

## Goals / Non-Goals

**Goals:**
- Replace the single-discipline interview roles in `settings.vue` with DeepPace **Mastery Tracks** (`settings.mastery_track`).
- Introduce 5 universal DeepPace mastery tracks:
  1. `Deep Work & Focus Practitioner`
  2. `System Architect & Decision Maker`
  3. `Lifelong Polymath & Cognitive Explorer`
  4. `Clean Code & Software Craftsperson`
  5. `Technical Leader & Engineering Mentor`
- Maintain 100% backward compatibility for existing database values (`Senior Engineer`, `Staff Engineer`, etc.) so existing users do not experience empty selection states or UI crashes.
- Update default new user `TargetRole` in `User.cs` and `AuthEndpoints.cs` to `"Deep Work & Focus Practitioner"`.
- Replace all user-facing occurrences of `'TechDaily'` in audio metadata, service worker notification titles, push test payloads, and page titles with `'DeepPace'`.

**Non-Goals:**
- Renaming the internal database table column `Users.TargetRole` (altering the column name would require a database migration and break backwards compatibility; preserving `TargetRole` as the internal string column preserves zero-downtime compatibility).
- Changing internal C# namespace names like `TechDaily.Api` (internal architectural namespaces are not user-facing).

## Decisions

### 1. Unified Mastery Track Option Mapping
In `settings.vue`, `difficultyOptions` is replaced with `masteryTrackOptions` where new DeepPace tracks are featured prominently, while legacy roles are retained for backward compatibility:
```typescript
const masteryTrackOptions = computed(() => [
  // DeepPace Universal Mastery Tracks
  { value: 'Deep Work & Focus Practitioner', label: t('settings.track_deep_work') },
  { value: 'System Architect & Decision Maker', label: t('settings.track_system_architect') },
  { value: 'Lifelong Polymath & Cognitive Explorer', label: t('settings.track_polymath') },
  { value: 'Clean Code & Software Craftsperson', label: t('settings.track_clean_code') },
  { value: 'Technical Leader & Engineering Mentor', label: t('settings.track_tech_lead') },
  // Backward compatibility for existing users
  { value: 'Senior Engineer', label: t('settings.role_senior') },
  { value: 'Staff Engineer', label: t('settings.role_staff') },
  { value: 'Principal Architect', label: t('settings.role_principal') },
  { value: 'Tech Lead', label: t('settings.role_tech_lead') },
  { value: 'Mid-Level Engineer', label: t('settings.role_mid') },
  { value: 'Junior Engineer', label: t('settings.role_junior') }
])
```
*Rationale*: If an existing user previously saved `"Senior Engineer"`, their option remains selectable and properly labeled in both `en` and `vi`. New users will default to `"Deep Work & Focus Practitioner"`.

### 2. Localization Keys
In `en.json` and `vi.json`:
- `settings.mastery_track`: "Mastery Track" / "Lộ trình rèn luyện chuyên sâu"
- `settings.track_deep_work`: "Deep Work & Focus Practitioner" / "Thực Hành Tập Trung Sâu & Nhịp Sống Bền Vững"
- `settings.track_system_architect`: "System Architect & Decision Maker" / "Kiến Trúc Sư Hệ Thống & Ra Quyết Định"
- `settings.track_polymath`: "Lifelong Polymath & Cognitive Explorer" / "Học Giả Đa Ngành & Khám Phá Nhận Thức"
- `settings.track_clean_code`: "Clean Code & Software Craftsperson" / "Chuyên Gia Kỹ Thuật & Mã Sạch"
- `settings.track_tech_lead`: "Technical Leader & Engineering Mentor" / "Lãnh Đạo Kỹ Thuật & Cố Vấn"

### 3. Micro-Branding Clean Cutover
- In `frontend/app/composables/useSliceAudio.ts`:
  ```typescript
  artist: payload.artist || 'DeepPace',
  album: payload.album || 'DeepPace Reader',
  ```
- In `frontend/public/sw.js`:
  ```javascript
  self.registration.showNotification(payload.title || 'DeepPace', options)
  ```
- In `backend/src/TechDaily.Api/Endpoints/NotificationEndpoints.cs`:
  ```csharp
  var payload = new PushNotificationPayload(
      "DeepPace Test Push 🚀",
      "Web Push notifications are successfully configured and active!",
      "/today",
      "deeppace-test"
  );
  ```

## Risks / Trade-offs

- **Risk: Breaking existing user settings on legacy role values**:
  - *Mitigation*: Handled by design: we keep legacy role values in the computed list so any user who already has `"Senior Engineer"` in their profile won't have an empty select box.
- **Risk: Nuxt 4 directory migration in parallel session**:
  - *Mitigation*: We are strictly writing planning artifacts in `openspec/changes/refine-deeppace-mastery-settings/`. Code changes will only be applied after the Nuxt 4 migration is merged.
