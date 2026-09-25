# Spec Delta: Auth

## MODIFIED Requirements

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
4. **Consistent Password Visibility Toggle Focus Styling**:
   - Password and Confirm Password visibility toggle buttons SHALL maintain consistent vertically centered positioning (`top-1/2 -translate-y-1/2 right-3.5`).
   - When navigated via keyboard (Tab), the focus indicator on password visibility toggle buttons SHALL render as a bounded, compact focus ring (`rounded-md focus-visible:ring-2 focus-visible:ring-brand-500`) that does not stretch across the container height or clip outside the input's rounded container.

#### Scenario: User tabs through password field to visibility toggle
- **WHEN** user focuses the password input on `/login` and presses the Tab key to navigate to the visibility toggle button
- **THEN** the password visibility toggle button displays a clean, compact focus ring centered within the input field
- **AND** the focus ring does not stretch vertically to the top and bottom borders of the input container
- **AND** the focus ring does not produce sharp rectangular corner clipping outside the input container's rounded border.

#### Scenario: User toggles password visibility via keyboard
- **WHEN** the password visibility toggle button is focused via keyboard navigation and the user presses Enter or Space
- **THEN** the password input switches between masked (`password`) and plaintext (`text`) modes
- **AND** the `:aria-label` updates dynamically to reflect the current state (`Show password` or `Hide password`).
