# Design

## Context

See `proposal.md` for motivation. The Engineer Profile (`pages/profile.vue`), System Settings (`pages/settings.vue`), and Authentication (`pages/login.vue`) house identity, career goals, notifications, and security credentials. Under `antfu/skills`, the 3-tier Bento layout must be audited for narrow mobile devices ($320\text{px}$), the daily pace selector refactored into a 2x2 grid on mobile, and the settings surface verified for popover containment.

## Goals / Non-Goals

**Goals:**
- Refactor pace selector chips in profile account settings from static `grid-cols-4` to responsive `grid-cols-2 sm:grid-cols-4 gap-2`.
- Ensure Tier 1 Passport badges and Tier 2 Milestones telemetry cells adapt cleanly to 320px screens without text truncation.
- Verify `pages/settings.vue` notification scheduling (`AppTimePicker`), timezone selector, and Brave push guidance cards on mobile viewports.
- Standardize authentication tab switching (Sign In vs Register) in `pages/login.vue` with zero-shift borders.

**Non-Goals:**
- Changing backend user profile entities, password hashing iterations, or JWT claims.
- Modifying Web Push VAPID key exchange or browser subscription payloads.

## Decisions

### 1. Responsive 2x2 Pace Grid on Mobile
- *Rationale*: A 4-column inline row on 320px displays leaves each chip with $< 65\text{px}$ width, causing labels like "15 mins" or "30 phút" to truncate or wrap awkwardly. Switching to `grid-cols-2 sm:grid-cols-4` provides comfortable $130\text{px}+$ width per chip.

### 2. Tier 3 Vertical Stacking Order
- *Rationale*: On desktop, Tier 3 splits 50/50 between Domain Mastery and Account Settings. On mobile, `order-1` on Domain Mastery and `order-2` on Account Settings ensures users see progress before settings forms.

### 3. Google GIS Button Container Scaling
- *Rationale*: The Google GIS iframe can overflow narrow mobile screens if fixed pixel widths are used. Constraining the parent container with `w-full max-w-sm` and centering ensures clean alignment.

## Risks / Trade-offs

- **Risk**: Touch target overlap on dense settings toggles.
  - **Mitigation**: Ensure minimum vertical margins (`gap-4` or `space-y-4`) between switch rows.
