# Design: IDE Reader and Concept Studio

## Context
TechDaily's reading surface on `/today` currently renders a static 50/50 dual-pane layout (`DocReaderPane.vue` on the left, `InterviewChallengePane.vue` on the right) with a basic top pacer bar. While functional, it lacks key engineering-grade studio affordances:
1. No outline/chapter navigator to jump between curriculum slices without using external menus.
2. Inflexible 50/50 split that prevents full-width distraction-free immersion when desired.
3. Plain styling on the scenario challenge options and evaluation cards that does not reflect the Kiro/Linear studio aesthetic.

## Goals / Non-Goals

**Goals:**
- Implement an engineering-grade **3-Column IDE Concept Studio** in `frontend/pages/today.vue`:
  - **Left Rail (Outline Navigator):** Collapsible slices list with completion checkmarks, active slice indicator, and estimated duration badges.
  - **Center Canvas (Markdown Reader):** Distraction-free reading canvas with top studio breadcrumb trail (`Book Title > Chapter Title > Slice N`), duration badge, typography popover, and panel toggles.
  - **Right Dock (Scenario Copilot):** Docked architectural scenario challenge with collapsible toggle for side-by-side problem-solving or full-screen reading immersion.
- Modernize `InterviewChallengePane.vue` with glassmorphic option selector pills, hairline borders, and glowing evaluation feedback cards.
- Support responsive viewport adaptability (simultaneous 3 columns on $\ge 1280\text{px}$, side-by-side with drawer on $1024\text{px} - 1279\text{px}$, and sleek mobile tabs on $< 768\text{px}$).

**Non-Goals:**
- Backend API or schema modifications.
- Changes to Markdown-it AST parsing or syntax highlighter plugins.
- Modifying SM-2 algorithm intervals or backend evaluation endpoints.

## Decisions

### 1. Panel Layout & Drawer State
- **State Properties in `today.vue`:**
  - `isOutlineOpen = ref(false)` (toggles left chapter/slice drawer; auto-opened on $\ge 1440\text{px}$).
  - `isChallengeDockOpen = ref(true)` (toggles right scenario challenge dock; when collapsed, center reader expands to 100% width).
- **Control Bar Toggles:**
  - `[ 📑 Outline ]` button on the left of the studio control bar.
  - `[ ⚡ Scenario Challenge ]` toggle button on the right of the studio control bar.

### 2. Studio Breadcrumb Trail & Time-to-Read Badge
- Studio control bar replaces raw pacer text with a breadcrumb trail:
  `[ Book Title ] > [ Chapter Title ] • Slice {current}/{total} ({percentage}%)`
- Reading pace indicator: `⚡ {estimatedMinutes} min read` with pulse badge.

### 3. Modernized Scenario Selection Pills (`InterviewChallengePane.vue`)
- Replace generic radio-style buttons with studio-grade option cards:
  - Surface: `glass-card p-4 hover:border-brand-500/40 cursor-pointer transition-all`
  - Selected State: `border-brand-500 bg-brand-500/10 text-white shadow-sm shadow-brand-500/20`
  - Option Letter Badge: Sleek square badge (`w-7 h-7 rounded-lg font-mono font-bold bg-canvas-elevated text-brand-400 border border-white/[0.08]`).

## Risks / Trade-offs

- **Screen Width on 13" Laptops (1024px - 1200px):**
  - Having all 3 panels open simultaneously would make the text columns too narrow.
  - **Mitigation:** Left outline navigator is closed by default below 1280px and opens as an off-canvas drawer with backdrop blur when toggled, preserving comfortable reading widths for the center canvas and right dock.
