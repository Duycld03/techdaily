# Spec Delta: auth

## ADDED Requirements

### Requirement: Responsive Viewport-Bounded Authentication Card
The authentication surface at `/login` SHALL provide a responsive, viewport-bounded authentication container adhering to the Dev-Learning Studio design system:

1. **Card Geometry & Centering**:
   - The authentication card SHALL be centered horizontally and vertically within the viewport (`min-h-[calc(100dvh-4rem)] flex items-center justify-center p-4`).
   - The card width SHALL be constrained to `w-full max-w-md` with refined glassmorphic styling (`dark:bg-canvas-subtle/80 backdrop-blur-xl border border-slate-200/90 dark:border-white/[0.08] shadow-2xl rounded-3xl`).
2. **Mode Switcher Zero-Shift Stability**:
   - Toggling between Login (`authMode === 'login'`) and Registration (`authMode === 'register'`) SHALL execute smoothly without vertical jumping, layout displacement, or browser scrollbar popping.
   - Mode switcher tabs SHALL utilize pill button styling with high-contrast active highlights (`bg-brand-600 text-white` / `bg-slate-100 dark:bg-canvas-elevated text-slate-600 dark:text-slate-300`).
3. **Responsive Input Groups & Touch Targets**:
   - Form inputs (Email, Password, Name) SHALL enforce minimum 44px touch heights (`py-2.5 sm:py-3`), comfortable leading icon clearance, and high-visibility focus rings (`focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20`).
   - Form submit buttons and third-party Google OAuth buttons SHALL span 100% width with clear loading indicators during async credentials verification.

#### Scenario: User visits login page on desktop
- **WHEN** user navigates to `/login` on desktop
- **THEN** the authentication card is vertically and horizontally centered with clean glassmorphic elevation
- **AND** the submit button and inputs fit comfortably within the viewport without requiring full-page scrolling.

#### Scenario: User switches between login and registration modes
- **WHEN** user clicks the "Register" or "Login" tab on `/login`
- **THEN** the form inputs transition smoothly without sudden height jumping or layout stutter
- **AND** existing valid email input is preserved across modes.
