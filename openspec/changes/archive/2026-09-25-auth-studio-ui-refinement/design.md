# Design: Refine Studio Auth Layout to Authentic Design Specification

## Context
Image #1 represents the canonical Dev-Learning Studio Cockpit authentication design. The current implementation in `frontend/pages/login.vue` has drifted by incorporating extraneous telemetry elements—including a `PING 18ms` badge (Image #4), an unstandardized `IDE v2.5.0-sys` pill (Image #5), latency indicators (`ĐỘ TRỄ 14MS` in Image #3), and a crowded sticky footer (Image #2). Furthermore, a placeholder GitHub OAuth button was provisionally rendered.

This design document specifies the architectural and layout adjustments required to align the interface 1:1 with Image #1, standardizing on TechDaily branding, single-provider Google OAuth, and clean telemetry separation.

## Goals / Non-Goals

**Goals:**
- **Header Refinement**:
  - Replace the left brand group with: `TechDaily` book emblem + `TechDaily` title + `IDE` pill (`v1.0` badge, `Engineering Cockpit · Density 8/10` subtitle separated by a subtle vertical border).
  - Completely eliminate the `PING 18ms` badge and `IDE v2.5.0-sys` pill.
  - Retain the right-side `ThemeToggle.vue`.
- **Status Sub-Header Simplification**:
  - Left: `• ALL SERVICES OPERATIONAL` in a rounded container with an emerald indicator dot.
  - Right: `⚡ SYSTEM INVARIANT: DAILY DELIBERATE PRACTICE`.
  - Eliminate any numeric latency indicators (`14ms`, `18ms`).
- **OAuth Stack Consolidation**:
  - Remove the GitHub button entirely.
  - Center Google Sign-In (`Google ⌘L`) as the dedicated 1-click developer OAuth action.
- **Mastery Showcase Alignment (Left Column)**:
  - Header: `STAFF+ TRACK` `v2.4-DRILL`, `Distributed Systems Mastery`, and staff+ micro-learning mission statement.
  - Metric bars: `SESSION INTERVAL TARGET` (`99.4% HIT`, 94% progress) and `SM-2 SPACED DECAY` (`24.8 Hrs`, gradient progress).
  - Code Snippet: `<> Consensus_Promise.ts` with lock icon and `RaftQuorum` consensus code snippet.
  - Footnote: Shield icon with `END-TO-END CRYPTOGRAPHIC ATTESTATION`.
- **Body & Sticky Footer Separation**:
  - Position `TECHDAILY COCKPIT ENGINE // COMPLIANT WITH SOC2 TYPE II & RFC-7519 JWT` in page body flow below the two-column grid.
  - Restrict the sticky bottom footer to `• DISTRIBUTED COCKPIT // SECURE_AUTH_NODE` (left) and `TLS 1.3 AES-256-GCM` (right).

**Non-Goals:**
- Integrating GitHub OAuth on the backend (explicitly deferred by user).

## Decisions

### 1. Header Layout Alignment
Replace the fragmented header markup in `frontend/pages/login.vue` with:
```vue
<header class="relative z-20 w-full h-14 border-b border-slate-200/80 dark:border-white/[0.08] bg-slate-50/80 dark:bg-[#09090b]/80 backdrop-blur-md px-4 sm:px-8 flex items-center justify-between">
  <div class="flex items-center gap-3">
    <div class="flex items-center gap-2.5">
      <div class="w-8 h-8 rounded-lg bg-brand-500/20 border border-brand-500/40 flex items-center justify-center text-brand-400 shadow-sm shadow-brand-500/20">
        <BookOpen class="w-4 h-4" :stroke-width="1.75" />
      </div>
      <span class="font-bold tracking-tight text-slate-900 dark:text-white text-base">TechDaily</span>
      <span class="bg-brand-500/20 text-brand-700 dark:text-brand-300 font-mono text-[10px] font-semibold px-1.5 py-0.5 rounded border border-brand-500/30 tracking-wider">IDE</span>
    </div>
    <div class="hidden md:flex items-center gap-2 pl-3 border-l border-slate-300/80 dark:border-white/[0.08] text-xs font-mono text-slate-500 dark:text-slate-400">
      <span class="bg-brand-500/15 text-brand-600 dark:text-brand-300 px-1.5 py-0.5 rounded text-[11px] font-medium">v1.0</span>
      <span>Engineering Cockpit · Density 8/10</span>
    </div>
  </div>
  <div class="flex items-center gap-3">
    <ThemeToggle />
  </div>
</header>
```

### 2. Streamlined Google OAuth Action
Remove the 2-column grid (`grid-cols-2`) and GitHub button. Replace with a single full-width container for Google Sign-In:
```vue
<div class="w-full flex justify-center mb-5">
  <div class="relative flex items-center justify-center min-h-[40px] w-full max-w-xs">
    <div ref="googleBtnContainer" class="w-full flex justify-center" style="color-scheme: light;"></div>
    <div v-if="!config.public.googleClientId" class="w-full h-10 px-3.5 rounded-lg bg-slate-100 dark:bg-[#1c1b1d] border border-slate-200 dark:border-white/[0.08] text-slate-800 dark:text-white flex items-center justify-between shadow-sm text-sm font-medium">
      <div class="flex items-center gap-2.5">
        <GoogleIcon />
        <span>Google</span>
      </div>
      <kbd class="font-mono text-xs bg-black/10 dark:bg-black/40 px-1.5 py-0.5 rounded text-slate-500 dark:text-slate-400 border border-slate-200 dark:border-white/[0.06]">⌘L</kbd>
    </div>
  </div>
</div>
```

### 3. Body Flow Compliance String & Clean Sticky Footer
Place the compliance string in the central flow:
```vue
<div class="mt-6 font-mono text-[11px] text-slate-400 dark:text-slate-500 flex items-center justify-center gap-2 tracking-wider text-center">
  <span>TECHDAILY COCKPIT ENGINE // COMPLIANT WITH SOC2 TYPE II & RFC-7519 JWT</span>
</div>
```
Keep the sticky footer minimal:
```vue
<footer class="relative z-20 w-full h-9 border-t border-slate-200/80 dark:border-white/[0.08] bg-slate-50/90 dark:bg-[#09090b]/90 backdrop-blur-md px-4 sm:px-8 flex items-center justify-between font-mono text-xs text-slate-500 dark:text-slate-400">
  <div class="flex items-center gap-2">
    <span class="h-1.5 w-1.5 rounded-full bg-brand-400"></span>
    <span>DISTRIBUTED COCKPIT // SECURE_AUTH_NODE</span>
  </div>
  <div>
    <span>TLS 1.3 AES-256-GCM</span>
  </div>
</footer>
```

## Risks / Trade-offs

- **Risk: Unit test assertions expecting removed badges (`PING 18ms`, `v2.5.0-sys`, `GitHub`)**:
  - *Mitigation*: Update unit tests in `frontend/tests/pages/login.spec.ts` to assert the authentic header (`v1.0`, `Engineering Cockpit`), verify Google OAuth presence, and assert absence of GitHub and `PING 18ms`.
