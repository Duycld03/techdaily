# Spec Delta

## MODIFIED Requirements

### Requirement: User Profile Management, Route Guards & Security

The user profile management subsystem (`GET /api/v1/user/profile`, `PUT /api/v1/user/profile`, `pages/settings.vue`) SHALL support managing user identity, multi-disciplinary mastery tracks (`TargetRole`), daily study pace (`DailyGoalMinutes`), study schedules, and notification alert preferences.

1. **Mastery Tracks in Settings**:
   - The user profile settings in `/settings` SHALL present the field as **Mastery Track** (`settings.mastery_track`, "Lộ trình rèn luyện chuyên sâu"), replacing legacy single-track interview difficulty labels.
   - The available options SHALL offer diverse, technology-agnostic deliberate practice tracks:
     - `Deep Work & Focus Practitioner` ("Thực Hành Tập Trung Sâu & Nhịp Sống Bền Vững")
     - `System Architect & Decision Maker` ("Kiến Trúc Sư Hệ Thống & Ra Quyết Định")
     - `Lifelong Polymath & Cognitive Explorer` ("Học Giả Đa Ngành & Khám Phá Nhận Thức")
     - `Clean Code & Software Craftsperson` ("Chuyên Gia Kỹ Thuật & Mã Sạch")
     - `Technical Leader & Engineering Mentor` ("Lãnh Đạo Kỹ Thuật & Cố Vấn")
   - For backwards compatibility, legacy roles (`Senior Engineer`, `Staff Engineer`, `Principal Architect`, `Tech Lead`, `Mid-Level Engineer`, `Junior Engineer`) SHALL be preserved in option matching so existing accounts display localized labels without empty fallback values.
2. **Default New Account Track**:
   - Newly provisioned user accounts (`AuthEndpoints.cs`, `User.cs`) SHALL default `TargetRole` to `"Deep Work & Focus Practitioner"`.

#### Scenario: User selects Deep Work & Focus Practitioner mastery track
- **WHEN** an authenticated user opens `/settings` in the Profile tab
- **THEN** the track selector displays "Mastery Track" / "Lộ trình rèn luyện"
- **WHEN** user selects "Deep Work & Focus Practitioner" and saves their profile
- **THEN** the client sends `PUT /api/v1/user/profile` with `targetRole: "Deep Work & Focus Practitioner"`
- **AND** the server persists the updated track and returns `200 OK`.

#### Scenario: Existing user with legacy Senior Engineer role views settings
- **WHEN** an existing user whose database `TargetRole` is `"Senior Engineer"` opens `/settings`
- **THEN** the track dropdown cleanly displays the localized label ("Lộ trình Senior" / "Senior Track") without blank selection or console errors.

---

## ADDED Requirements

### Requirement: Universal DeepPace Production Micro-Branding & Media Session Identity

All user-facing media sessions, service worker notification handlers, and API diagnostic test payloads SHALL standardize on the **DeepPace** brand name, eliminating legacy application names.

1. **Audio Player MediaSession Metadata**:
   - The audio player composable (`useSliceAudio.ts`) SHALL populate Navigator MediaSession metadata with default `artist: 'DeepPace'` and `album: 'DeepPace Reader'` (eliminating legacy 'TechDaily' fallbacks).
2. **Service Worker Push Notifications**:
   - The background service worker (`sw.js`) SHALL use `'DeepPace'` as the default notification fallback title when the push payload omits a custom title.
3. **Push Notification Test Diagnostic Endpoint**:
   - The push notification verification endpoint (`POST /api/v1/notifications/push/test`) SHALL send notification title `"DeepPace Test Push 🚀"` and tag `"deeppace-test"`.
4. **Application Page Titles & Diagnostics**:
   - Document head meta titles (such as `/showcase`) SHALL display `DeepPace`.
   - Client authentication warning diagnostics in `/login` SHALL log with prefix `[DeepPace Auth]`.

#### Scenario: Background audio player registers MediaSession
- **WHEN** user plays an audio narration slice for an imported book or article
- **THEN** the operating system lockscreen and media notification bar displays artist as "DeepPace" and album as "DeepPace Reader" unless overridden by book author metadata.

#### Scenario: Service worker receives push event with empty title
- **WHEN** a web push notification arrives with no title in the payload
- **THEN** `sw.js` displays a notification with title `"DeepPace"`.

#### Scenario: Developer triggers test push notification
- **WHEN** an authenticated user clicks "Send Test Push" in `/settings`
- **THEN** the test notification payload arrives with title `"DeepPace Test Push 🚀"` and tag `"deeppace-test"`.
