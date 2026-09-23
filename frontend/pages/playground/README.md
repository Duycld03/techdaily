# Frontend Playground Sandbox

This directory (`frontend/pages/playground/`) is an isolated development sandbox for prototyping new page layouts, complex UI widgets, and experimental designs with self-contained mock data before cutting over into production routes.

## Invariants & Rules
1. **Mock Data Only**: Playground prototypes must use self-contained mock data. Do not bind playground pages to global mutation stores or fire destructive API calls.
2. **Visual Review Gate**: Before applying a major UI change to production (`frontend/pages/*.vue`), prototype the layout here, capture a 1080p desktop screenshot in Dark Mode, and obtain review/approval.
3. **No Production Links**: Playground pages must not be included in production navigation bars (`AppSidebar.vue`, `AppHeader.vue`).
4. **Clean Cutover**: After the layout is approved and integrated into its destination production route, temporary playground files may be archived or removed.
