# Design: Dev-Learning Studio Cockpit Account Recovery & Forgot Password UI

## Context
TechDaily's authentication surface (`frontend/pages/login.vue`) has been redesigned into the Dev-Learning Studio Cockpit archetype. The mode switcher currently presents two tabs (`Sign In` and `Register`) with a secondary reset trigger. The account recovery mode (`forgot-password`) remains a plain single-input form that does not match the technical sophistication, telemetry indicators, and security guidance established in the prototype (`paste-1.md`).

This change adapts the prototype's recovery view and 3-tab segmented control into TechDaily's Vue 3 / Nuxt 3 architecture, standardizing on Lucide icons, Tailwind CSS semantic tokens, and bilingual localization without slash text.

## Goals / Non-Goals

**Goals:**
- Provide a unified 3-tab segmented switcher (`[ Sign In ]`, `[ Register ]`, `[ ↻ Recover ]`) in the authentication cockpit card with smooth transition and zero layout shift.
- Modernize the `forgot-password` mode with:
  - Header: "Recover Cockpit Access" (`auth.recover_cockpit_title`) and magic link explanation (`auth.recover_cockpit_subtitle`).
  - Monospace telemetry input: `ACCOUNT REGISTRATION EMAIL` (`auth.account_email_label`) with leading `Mail` icon.
  - Advisory callout: Contextual note explaining that GitHub OAuth or hardware keys can authenticate directly without password reset (`auth.oauth_bypass_notice`).
  - Primary button: High-contrast Iris Violet action button "Send Recovery Magic Link" (`auth.send_recovery_link_btn`) with `Send` icon and keyboard shortcut chip (`↵ RETURN`).
  - Back action: "Back to Sign In" (`auth.back_to_signin_btn`) with `ArrowLeft` icon.
- Introduce card security footer: `ZERO-KNOWLEDGE AUTH` (`auth.zero_knowledge_badge`) with emerald `Lock` icon, plus Terms and Privacy links.
- Maintain 100% bilingual parity (`en.json` and `vi.json`) with zero slash text (`/`).
- Automated unit test suite in Vitest and visual browser verification across Desktop/Mobile and Dark/Light modes.

**Non-Goals:**
- Backend email dispatch pipeline or SMTP server integration (frontend handler simulates dispatch with user-facing toast feedback, preserving clean separation of concerns).
- Modifying backend authentication schemas or database migrations.

## Decisions

### 1. Lucide Icons over Material Symbols
The prototype in `paste-1.md` loads Google Material Symbols via external Google Fonts CDN. In accordance with TechDaily's design invariants, all icons must use local `lucide-vue-next` components (`KeyRound`, `RotateCcw`, `Mail`, `Info`, `Send`, `ArrowLeft`, `Lock`, `Shield`).

### 2. 3-Tab Segmented Control Ergonomics
Instead of an isolated reset icon button, the top switcher container (`flex p-1 rounded-xl bg-slate-100 dark:bg-canvas border border-slate-200/80 dark:border-white/[0.08]`) will host three structured tab triggers:
- `Sign In` (`auth.sign_in_tab`)
- `Register` (`auth.register_tab`)
- `Recover` (`auth.recovery_tab_title` with icon)
This makes account recovery a first-class, easily discoverable state on the authentication card while keeping the card width and height balanced.

### 3. Compact Contextual Advisory Panel
To prevent vertical overflow in `forgot-password` mode, the OAuth bypass guidance box is styled with compact padding (`p-2.5 sm:p-3 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-700 dark:text-amber-300 text-xs`) and an `Info` icon. This keeps the total card height well under 650px on desktop, preventing viewport scrolling.

### 4. Zero-Knowledge Security Footnote
The card footer adds an attestation indicator:
```vue
<div class="mt-4 pt-3 border-t border-slate-200/80 dark:border-white/[0.08] flex items-center justify-between text-slate-400 font-mono text-[11px]">
  <div class="flex items-center gap-1.5">
    <Lock class="w-3.5 h-3.5 text-emerald-500" :stroke-width="1.75" />
    <span class="tracking-wide">{{ $t('auth.zero_knowledge_badge') }}</span>
  </div>
  <div class="flex items-center gap-2">
    <NuxtLink to="/terms" class="hover:text-slate-600 dark:hover:text-white transition-colors">{{ $t('auth.terms_link') }}</NuxtLink>
    <span>·</span>
    <NuxtLink to="/privacy" class="hover:text-slate-600 dark:hover:text-white transition-colors">{{ $t('auth.privacy_link') }}</NuxtLink>
  </div>
</div>
```

## Risks / Trade-offs

- **Risk: Viewport height on small laptop screens (768px height)**:
  - *Mitigation*: The entire authentication surface uses flex centering with responsive margins (`p-3 sm:p-5 lg:p-6`) and compact input row spacing (`space-y-3` in forgot-password mode), ensuring the complete card fits effortlessly without vertical scrolling.
