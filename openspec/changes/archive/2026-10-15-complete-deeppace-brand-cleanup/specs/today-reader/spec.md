# Today Reader Spec Delta

## MODIFIED Requirements

### Requirement: Shared Reactive Typography Composable
The frontend application SHALL provide a centralized reactive typography composable `useReaderTypography()` located at `frontend/app/composables/useReaderTypography.ts`.

The composable SHALL:
1. **Reactive State Management:** Manage reactive, singleton-backed or shared state for:
   - `fontSize`: `'sm'` (14px / 85%), `'base'` (16px / 100%, default), `'lg'` (18px / 115%), `'xl'` (20px / 130%), `'2xl'` (22px / 145%).
   - `fontFamily`: `'sans'` (Inter, system-ui, default), `'serif'` (Merriweather, Georgia, serif), `'mono'` (JetBrains Mono, monospace).
   - `lineSpacing`: `'normal'` (1.5), `'relaxed'` (1.75, default), `'loose'` (2.05).
   - `readingWidth`: `'standard'` (`max-w-3xl`, default), `'wide'` (`max-w-4xl`), `'full'` (`max-w-full`).
2. **Stepped Font Size Helpers:** Provide stepped decrement (`decreaseFontSize`) and increment (`increaseFontSize`) helper functions with boundary bounds checking (`canDecreaseFontSize` and `canIncreaseFontSize`).
3. **Computed Typography Helpers:** Provide computed mappings for direct template binding:
   - `fontSizePx`: Returns active font size in pixel units (e.g. `'16px'`).
   - `lineHeightValue`: Returns active line height unitless string (e.g. `'1.75'`).
   - `fontFamilyClass`: Returns Tailwind font family class (`font-sans`, `font-serif`, `font-mono`).
   - `readingWidthClass`: Returns Tailwind max-width class (`max-w-3xl`, `max-w-4xl`, `max-w-full`).
   - `fontScalePercentages`: Maps size keys to scale percentages (`85%`, `100%`, `115%`, `130%`, `145%`).
4. **LocalStorage Persistence:** Automatically serialize and persist typography state to browser `localStorage` under key `deeppace_reader_typography` whenever any property changes.
5. **Safe Hydration:** Hydrate saved preferences on client-side mount, safely validating property values and falling back to defaults (`{ fontSize: 'base', fontFamily: 'sans', lineSpacing: 'relaxed', readingWidth: 'standard' }`) if storage is empty or contains malformed JSON.
6. **Cross-View Synchronization:** Ensure updating typography settings in either `/read/[bookId]` or `/today` updates the shared state and persists to `localStorage`, synchronizing reading preferences across all reader surfaces.

#### Scenario: User modifies typography via shared composable
- **GIVEN** a component consuming `useReaderTypography()`
- **WHEN** user invokes `increaseFontSize()` or sets `fontFamily = 'serif'`
- **THEN** reactive typography state updates immediately
- **AND** the new configuration is persisted to `localStorage.getItem('deeppace_reader_typography')`.

#### Scenario: Composable hydration from localStorage
- **GIVEN** a browser session with previously saved typography preferences `{ fontSize: 'lg', fontFamily: 'serif', lineSpacing: 'loose', readingWidth: 'wide' }`
- **WHEN** any reading view mounts and invokes `useReaderTypography()`
- **THEN** the composable initializes with the saved preferences rather than defaults.

