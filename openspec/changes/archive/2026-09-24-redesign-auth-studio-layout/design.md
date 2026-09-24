# Technical Design: Dev-Learning Studio Authentication Surface Redesign

## Context

`frontend/pages/login.vue` provides authentication (Sign In and Registration) and Google OAuth. In `frontend/app.vue`, `<AppSidebar v-if="!isReaderMode" />` is rendered globally, which inadvertently renders the internal navigation rail on guest-only authentication routes (`/login`). In addition, the form is constrained to a narrow phone-width single column (`max-w-md`), causing vertical elongation and an empty visual void on desktop screens, while a flawed flexbox divider caused horizontal width blowout.

See `proposal.md` for motivation.

## Goals / Non-Goals

**Goals:**
- Implement a modern, balanced Studio Auth archetype (`max-w-5xl` container) on desktop viewports ($\ge 1024\text{px}$), pairing a brand stage with an interactive auth card.
- Suppress `AppSidebar` on `/login` in `frontend/app.vue`, providing a clean, distraction-free guest shell.
- Re-architect Register mode into a responsive 2-column grid (`sm:grid-cols-2 gap-3.5`), cutting vertical card height by over 40% and keeping all controls above the fold on $1080\text{p}$ monitors.
- Refactor the OAuth divider using `absolute inset-0 flex items-center`, eliminating width blowout and misaligned text.
- Preserve all existing authentication flows, Google Identity Services initialization, validation logic, and unit test pass rates.

**Non-Goals:**
- Changing backend authentication endpoints or JWT issuance contracts.
- Adding third-party UI libraries or CSS frameworks.

## Decisions

### 1. App Shell Guest Route Isolation (`frontend/app.vue`)
Add computed helper in `app.vue`:
```ts
const isAuthPage = computed(() => route.path === '/login')
```
Conditionally suppress the internal sidebar:
```html
<AppSidebar v-if="!isReaderMode && !isAuthPage" />
```
This isolates the guest authentication experience from internal protected routes.

### 2. Studio Auth 2-Column Viewport Architecture (`frontend/pages/login.vue`)
Wrap the view in a centered, spacious container:
```html
<div class="min-h-[calc(100dvh-3.5rem)] flex items-center justify-center p-4 sm:p-6 lg:p-8 bg-slate-50 dark:bg-canvas transition-colors duration-200">
  <div class="w-full max-w-5xl grid lg:grid-cols-12 gap-8 lg:gap-12 items-center">
    <!-- Left Stage: Platform Value & Mission (lg:col-span-5) -->
    <!-- Right Stage: Interactive Authentication Card (lg:col-span-7) -->
  </div>
</div>
```

#### Left Column: Brand & Value Stage (`lg:col-span-5 hidden lg:block`)
- TechDaily emblem with Deep Iris Violet glow.
- Mission heading: "Master Senior Software Engineering Daily".
- 3 Value Proposition feature cards with translucent borders (`dark:border-white/[0.08]`):
  1. 🎯 **Scenario Architecture Drills**: Interactive system design decisions under real-world scale.
  2. 🧠 **SM-2 Spaced Mastery**: Cognitive spaced repetition for durable concept retention.
  3. ⚡ **Curated Reading Slices**: 3-5 minute daily authoritative doc excerpts.

#### Right Column: Interactive Auth Card (`lg:col-span-7 w-full`)
- Card surface: `.glass-panel shadow-2xl rounded-3xl p-6 sm:p-8 space-y-5`.
- Mobile brand header (`lg:hidden text-center`): Compact BookOpen emblem and title for smaller viewports.
- Mode switcher: Segmented tabs for "Sign In" and "Register".
- Input form:
  - In Login mode: 1-column layout for Email and Password.
  - In Register mode: 2-column grid (`grid sm:grid-cols-2 gap-3.5`):
    - Row 1: Full Name (`name`) and Email (`email`).
    - Row 2: Password (`password`) and Confirm Password (`confirmPassword`).

### 3. Bulletproof Divider Architecture
Replace fragile flex container with absolute centered divider:
```html
<div class="relative my-4">
  <div class="absolute inset-0 flex items-center" aria-hidden="true">
    <div class="w-full border-t border-slate-200 dark:border-white/[0.08]" />
  </div>
  <div class="relative flex justify-center text-xs uppercase">
    <span class="bg-white dark:bg-canvas-elevated px-3 text-slate-400 dark:text-slate-500 font-semibold tracking-wider">
      {{ $t('auth.or_continue_with') }}
    </span>
  </div>
</div>
```

## Risks / Trade-offs

- **Small Desktop Heights (e.g. 768px)**: The 2-column registration form layout significantly reduces height, fitting comfortably within ~600px total height, preventing vertical scrolling.
- **Mobile Stack**: On screens $<1024\text{px}$, the left brand column is hidden, and the right auth card renders as a clean centered card (`max-w-md` or `max-w-lg`) with full-width inputs.
