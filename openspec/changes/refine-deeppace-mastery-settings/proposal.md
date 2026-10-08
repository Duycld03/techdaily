# Proposal

## Why

While the platform pivot to DeepPace repositioned the product as a sustainable, lifelong deliberate practice engine for deep technical craftsmanship, mental models, and focused habits, the user profile settings in `/settings` remain bound to legacy software engineer interview levels ("Senior Track", "Staff Track", "Principal Track", "Tech Lead Track", "Mid-Level Track", "Junior Track"). This narrow focus conflicts with the technology-agnostic core invariant and excludes learners focused on cognitive performance, systems thinking, and deep work rituals.

Furthermore, several micro-branding artifacts still reference the legacy name "TechDaily" across media session audio metadata, service worker push notifications, test endpoints, and login console diagnostics.

## What Changes

- **De-Jargonize & Broaden Mastery Tracks in Settings (`settings.vue`)**:
  - Replace the legacy "Difficulty track" label with "Mastery Track" ("Lộ trình rèn luyện chuyên sâu").
  - Introduce universal DeepPace mastery tracks:
    - `Deep Work & Focus Practitioner` ("Thực Hành Tập Trung Sâu & Nhịp Sống Bền Vững")
    - `System Architect & Decision Maker` ("Kiến Trúc Sư Hệ Thống & Ra Quyết Định")
    - `Lifelong Polymath & Cognitive Explorer` ("Học Giả Đa Ngành & Khám Phá Nhận Thức")
    - `Clean Code & Software Craftsperson` ("Chuyên Gia Kỹ Thuật & Mã Sạch")
    - `Technical Leader & Engineering Mentor` ("Lãnh Đạo Kỹ Thuật & Cố Vấn")
  - Retain backwards-compatible mappings for legacy values (`Senior Engineer`, `Staff Engineer`, `Principal Architect`, etc.) so existing accounts render cleanly without label breakage.
  - Default new accounts in backend authentication endpoints (`AuthEndpoints.cs`) and `User` entity to `"Deep Work & Focus Practitioner"`.
- **Clean Up Lingering "TechDaily" Micro-Branding Artifacts**:
  - `frontend/app/composables/useSliceAudio.ts`: Update MediaSession metadata defaults from `artist: 'TechDaily'` and `album: 'TechDaily Reader'` to `artist: 'DeepPace'` and `album: 'DeepPace Reader'`.
  - `frontend/public/sw.js`: Update fallback push notification title from `TechDaily` to `DeepPace`.
  - `backend/src/TechDaily.Api/Endpoints/NotificationEndpoints.cs`: Update test push payload from `"TechDaily Test Push 🚀"` / tag `"techdaily-test"` to `"DeepPace Test Push 🚀"` / tag `"deeppace-test"`.
  - `frontend/app/pages/showcase.vue`: Update page title from `Design System — TechDaily` to `Design System — DeepPace`.
  - `frontend/app/pages/login.vue`: Update Google OAuth log warnings from `[TechDaily Auth]` to `[DeepPace Auth]`.

## Capabilities

### Modified Capabilities
- `core-platform`: Expand user learning track preferences from interview-bound roles to multi-disciplinary DeepPace mastery tracks and standardize production branding across audio and notifications.

## Impact

- **Backend**:
  - `backend/src/TechDaily.Domain/Entities/User.cs`: Default `TargetRole` updated to `"Deep Work & Focus Practitioner"`.
  - `backend/src/TechDaily.Api/Endpoints/AuthEndpoints.cs`: Seed default user target track to `"Deep Work & Focus Practitioner"`.
  - `backend/src/TechDaily.Api/Endpoints/NotificationEndpoints.cs`: Push test payload title and tag.
- **Frontend**:
  - `frontend/app/pages/settings.vue`: Track options and localization keys.
  - `frontend/app/i18n/locales/en.json`, `frontend/app/i18n/locales/vi.json`: Track labels and descriptions.
  - `frontend/app/composables/useSliceAudio.ts`: Audio reader metadata.
  - `frontend/public/sw.js`: Web push notification default title.
  - `frontend/app/pages/showcase.vue`, `frontend/app/pages/login.vue`.
  - `frontend/tests/pages/settings.spec.ts`.
