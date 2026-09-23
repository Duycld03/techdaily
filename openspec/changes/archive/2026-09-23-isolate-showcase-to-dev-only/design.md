# Design: Dev-Only Isolation for Showcase & Sandboxes

## Context

TechDaily maintains an interactive living design system showcase (`frontend/pages/showcase.vue`) and UI playground sandboxes (`frontend/pages/playground/*`) for rapid prototyping, component verification, and visual density testing.

Under Nuxt's default file-based routing, all files residing in `frontend/pages/` are automatically compiled into production router tables and client JavaScript chunks. Furthermore, `frontend/composables/useNavigationMenu.ts` currently registers `/showcase` unconditionally in the primary sidebar navigation menu.

See `proposal.md` for motivation and background.

## Goals / Non-Goals

**Goals:**
- **Zero Production Bundle Footprint**: Completely remove `/showcase` and `/playground` route chunks and router entries from production distributions.
- **Leak-Proof DevTools Inspection**: Ensure inspecting client router manifests (`router.getRoutes()`), network requests, or source maps in production reveals zero references to sandbox components.
- **Uncompromised Developer Experience (DX)**: Maintain full, seamless access to `/showcase` and `/playground` during local development (`npm run dev`).
- **Clean Production Sidebar**: Show only authenticated user-facing application navigation links in production.

**Non-Goals:**
- Deleting showcase or playground component source code from the repository.
- Introducing complex server-side authentication gates or runtime role checks for developer pages (build-time pruning is architecturally simpler, leak-proof, and eliminates bundle weight).

## Decisions

### Decision 1: Nuxt `pages:extend` Hook for Build-Time Route Pruning
In `frontend/nuxt.config.ts`, implement a `pages:extend` hook:
```typescript
hooks: {
  'pages:extend'(pages) {
    if (process.env.NODE_ENV === 'production') {
      const devRoutePrefixes = ['/showcase', '/playground']
      for (let i = pages.length - 1; i >= 0; i--) {
        if (devRoutePrefixes.some(prefix => pages[i].path.startsWith(prefix))) {
          pages.splice(i, 1)
        }
      }
    }
  }
}
```

*Rationale*:
- The `pages:extend` hook executes before Vite bundles the client router manifest.
- Removing routes at this stage prevents Vite from creating entry points or prefetching chunks for these pages in production builds.
- Any manual URL probe to `https://techdaily.duckdns.org/showcase` naturally triggers Nuxt's standard 404 handler, matching non-existent routes exactly.

*Alternatives Considered*:
- *Route Middleware with 404 throw*: Still bundles the page code into production client assets; opening DevTools allows inspecting the chunk code.
- *Nitro Route Rules*: Only affects server-side responses; does not prune client router entries.

### Decision 2: Environment-Gated Navigation Menu (`useNavigationMenu.ts`)
In `frontend/composables/useNavigationMenu.ts`, dynamically construct the `nav.group_account` links based on `import.meta.dev`:
```typescript
const accountLinks: NavLink[] = [
  { name: 'nav.settings_profile', path: '/settings', icon: Settings }
]

if (import.meta.dev) {
  accountLinks.push({ name: 'nav.showcase', path: '/showcase', icon: Palette })
}
```

*Rationale*:
- Vite statically replaces `import.meta.dev` with `false` during production bundling.
- Tree-shaking / dead-code elimination guarantees the link and icon references are purged if not used elsewhere in production code.

### Decision 3: Comprehensive Automated Testing
Update `frontend/tests/composables/useNavigationMenu.spec.ts` to verify:
1. `navGroups` structure in development mode includes `/showcase`.
2. Mocking `import.meta.dev = false` excludes `/showcase` and retains only `/settings`.

## Risks / Trade-offs

- **Risk**: A developer might mistakenly expect `/showcase` to be accessible on staging/production environments for live verification.
  - *Mitigation*: Local dev (`npm run dev`) and local preview provide instant access. If staging preview is ever required in the future, it can be gated via a dedicated environment variable (e.g. `ENABLE_DEV_PAGES=true`).
