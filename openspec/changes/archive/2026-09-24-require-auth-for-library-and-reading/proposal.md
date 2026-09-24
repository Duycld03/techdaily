# Proposal: Require Authentication for Library & Reader Routes

## Why

The Technical Document Library (`/library`) and Reader interface (`/read/[bookId]`) currently permit unauthenticated visitors to navigate the catalog and reader view. However, the library encompasses user-specific and sensitive operations including document importation (PDF uploads up to 350MB, URL web crawling, and Markdown imports), document deletion, personalized reading progress tracking, and bookmark resumption. Permitting unauthenticated access causes fragmented user experiences and UI errors when guests attempt actions requiring authenticated credentials. Protecting `/library` and `/read` behind authentication ensures coherent session management and aligns with TechDaily's clean architecture invariants.

## What Changes

- **Route Guard Protection (`frontend/middleware/auth.global.ts`)**:
  - Add `/library` (`to.path.startsWith('/library')`) and `/read` (`to.path.startsWith('/read')`) to the `isAuthRequired` route list in the global authentication middleware.
  - Automatically redirect unauthenticated visitors to `/login?redirect=<targetPath>`, preserving the return destination upon successful login.
  - Retain seamless access for authenticated users with active tokens.
- **Automated Route Guard Testing**:
  - Add dedicated unit tests in `frontend/tests/middleware/auth.spec.ts` asserting that `/library` and `/read/[bookId]` require authentication and redirect guests to `/login`, while allowing logged-in sessions.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `library`: Update requirements to enforce authenticated access on the technical document library and reader interfaces.

## Impact

- **User Flow**: Unauthenticated users visiting `/library` or `/read/[bookId]` will be prompted to log in or register before accessing documentation. Upon logging in, they will be redirected back to the exact book or library view they requested.
- **Security & Hygiene**: Prevents anonymous users from triggering import modals, attempting unauthorized backend calls, or maintaining unattached client-side reading states.
