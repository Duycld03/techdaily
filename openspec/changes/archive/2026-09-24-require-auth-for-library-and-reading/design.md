# Design: Enforce Authentication on Library & Reader Routes

## Context

TechDaily protects core application surfaces using a global Nuxt route middleware (`frontend/middleware/auth.global.ts`). Currently, `auth.global.ts` guards routes such as `/`, `/today`, `/notes`, `/review`, `/quiz`, `/settings`, and `/insights`.

However, `/library` and `/read` were omitted from `isAuthRequired`. While backend document mutation endpoints (`/upload-pdf`, `/crawl-url`, `/import`, delete book) enforce JWT authorization with HTTP 401/403 responses, unauthenticated guests can still open the `/library` catalog and `/read/[bookId]` reader views. This results in broken user flows (e.g. attempting to upload a document or save reading state without an active session).

See `proposal.md` for motivation and background.

## Goals / Non-Goals

**Goals:**
- **Consistent Authentication Perimeter**: Guard `/library` and `/read` behind `frontend/middleware/auth.global.ts`.
- **Preserved Deep-Linking**: Ensure unauthenticated requests to `/library` or `/read/[bookId]?slice=2` redirect to `/login?redirect=<targetPath>`, returning the user to their requested reading slice upon authentication.
- **Automated Verification**: Implement comprehensive unit tests verifying that both routes require authentication for guests and permit authenticated users.

**Non-Goals:**
- Restricting backend public read-only catalog endpoints (`GET /api/v1/library/books`), which can remain decoupled for external API consumption.
- Adding complex per-book access control lists (ownership verification is already enforced on document mutation).

## Decisions

### Decision 1: Extend `isAuthRequired` in `auth.global.ts`
In `frontend/middleware/auth.global.ts`:
```typescript
const isAuthRequired =
  to.path === '/' ||
  to.path.startsWith('/today') ||
  to.path.startsWith('/insights') ||
  to.path.startsWith('/roadmap') ||
  to.path.startsWith('/review') ||
  to.path.startsWith('/notes') ||
  to.path.startsWith('/profile') ||
  to.path.startsWith('/settings') ||
  to.path.startsWith('/quiz') ||
  to.path.startsWith('/library') ||
  to.path.startsWith('/read')
```

*Rationale*:
- Using `startsWith` correctly handles nested subroutes such as `/read/[bookId]` and `/library?category=...`.
- Reuses the existing robust redirect pipeline (`navigateTo({ path: '/login', query: { redirect: to.fullPath } })`), preserving query parameters like `?slice=N`.

### Decision 2: Automated Middleware Unit Testing
Create `frontend/tests/middleware/auth.spec.ts` using Vitest to test:
1. Unauthenticated user accessing `/library` redirects to `/login?redirect=%2Flibrary`.
2. Unauthenticated user accessing `/read/book-123?slice=4` redirects to `/login?redirect=%2Fread%2Fbook-123%3Fslice%3D4`.
3. Authenticated user accessing `/library` passes through without redirection.
4. Logged-in user visiting `/login` is redirected to `/`.

## Risks / Trade-offs

- **Risk**: External links shared to specific documentation articles or books will require login first.
  - *Mitigation*: The `redirect` query parameter smoothly resumes navigation to the exact shared book link immediately after Google OAuth or email sign-in.
