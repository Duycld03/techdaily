# Proposal: Studio Auth Cockpit Canvas & Ambiance

## Why
The current authentication surface at `/login`, while functional, leaves excessive dead space on widescreen displays and exhibits a visual hover glitch where Google's native iframe button reveals an overflowing white square around the logo ("dư 1 chút ở trên và dưới"). 

To deliver the high-density, focused developer learning atmosphere established in Image #1 and `local://paste-3.md`, the authentication view must be upgraded to the full **Studio Auth Cockpit Canvas**:
1. Eliminating empty voids with multi-layered engineering ambiance (dot-matrix grid, iris radial ambient glow, and baseline hairlines).
2. Fixing the Google Sign-In hover glitch with a pixel-perfect, native-styled full-width button containing an inline SVG Google mark and responsive hover styling (`dark:bg-[#202024]`), completely eliminating the white square overflow defect.
3. Providing a clean, distraction-free studio shell with minimalist header (brand emblem, title, and language/theme toggles) without clutter.
4. Enriching the left learning pillar with interactive progress metrics (`MỤC TIÊU ĐỌC MỖI NGÀY 94%`, `ÔN TẬP NGẮT QUÃNG SM-2`), code window simulation (`>_ TECHDAILY_PRACTICE.TS`), and authentic spaced-repetition developer branding.

## What Changes
- **Ambient Canvas Background Layers**:
  - Add `.bg-grid-dots` radial gradient pattern (`rgba(255, 255, 255, 0.07)`).
  - Add centered Iris violet radial glow (`blur-[140px]`) and subtle accent glows.
  - Add clean hairline divider headers.
- **Clean Studio Header**:
  - Streamlined header with brand mark, title, and language/theme switches matching Image #1.
- **Left Telemetry & Mastery Stage**:
  - Update platform badges: `● TECHDAILY | SM-2 ACTIVE RECALL`.
  - Add 2 progress gauge cards: Daily Reading Goal (emerald gradient) and SM-2 Spaced Repetition (iris gradient).
  - Add code block window with macOS-style window dots, `>_ TECHDAILY_PRACTICE.TS`, lock icon, and Shiki/Tailwind styled code drill.
- **Right Auth Cockpit Card**:
  - Segmented tab control: `[ Sign In ]`, `[ Register ]`, `[ ↻ Recover ]`.
  - Fix Google button hover glitch: Replace native iframe hover edge with an integrated full-width button using official Google G SVG mark, clean typography (`Đăng nhập với Google` / `Sign in with Google`), and responsive hover states (`dark:bg-[#202024]`).
  - Form input styling: Terminal `>_` prefix for Email/Username, key icon for Password with eye toggle, and `Forgot password?` link in the label row.
  - Checkbox: Iris styled `Remember session (30 days)` checkbox.
  - Action button: Full-width purple submit button with `↵ RETURN` shortcut chip.
  - Card footer: `🛡️ SECURE & ENCRYPTED AUTH` badge on left, `Terms · Privacy` on right.
- **Bilingual Internationalization (EN / VI)**:
  - Add all new telemetry strings, tooltips, and badges to `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.
## Capabilities

### Modified Capabilities
- `auth`: Update Studio Auth Canvas requirements to incorporate ambient background layers, header and footer telemetry bars, learning micro-chips, and glitch-free Google Sign-In presentation matching Image #2.
