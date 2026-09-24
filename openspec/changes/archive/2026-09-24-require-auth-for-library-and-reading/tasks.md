# Tasks

## 1. Frontend Route Middleware Hardening

- [x] 1.1 Update `frontend/middleware/auth.global.ts` to include `to.path.startsWith('/library')` and `to.path.startsWith('/read')` in `isAuthRequired`.

## 2. Verification & Automated Testing

- [x] 2.1 Create `frontend/tests/middleware/auth.spec.ts` to verify `/library` and `/read` route guards (unauthenticated redirect to `/login` with `redirect` query, authenticated pass-through).
- [x] 2.2 Run full frontend test suite (`npm test`) to ensure all test suites pass with zero regressions.
