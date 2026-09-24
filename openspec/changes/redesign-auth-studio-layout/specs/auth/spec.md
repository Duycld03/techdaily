# Spec Delta: auth

## ADDED Requirements

### Requirement: Dev-Learning Studio Authentication Surface & Shell Isolation
The platform SHALL provide a dedicated, balanced authentication surface conforming to the **Dev-Learning Studio** visual language, isolating guest authentication flows from internal application navigation chrome.

1. **Application Shell Isolation**:
   - The global layout shell (`app.vue`) SHALL detect authentication routes (`/login`) and conditionally suppress the internal application sidebar (`AppSidebar.vue`).
   - The top header (`AppHeader.vue`) on `/login` SHALL render in a streamlined guest state, omitting internal navigation drawer triggers while retaining theme toggle, language switcher, and brand home link.

2. **Studio Auth 2-Column Archetype**:
   - On desktop viewports ($\ge 1024\text{px}$), the authentication view (`pages/login.vue`) SHALL render within a balanced container (`max-w-4xl` to `max-w-5xl`) organized into two complementary columns:
     - **Left Column (Platform Value Stage)**: Displays TechDaily's engineering brand identity, mission statement ("Master Senior Software Engineering Daily"), and 3 core learning pillars (Scenario Architecture Drills, SM-2 Spaced Mastery, Daily System Design Doses) with subtle studio badges.
     - **Right Column (Interactive Auth Card)**: Houses the `.glass-panel` container (`dark:bg-canvas-elevated`, `dark:border-white/[0.08]`, `rounded-3xl`, `shadow-2xl`) containing mode switching tabs, input forms, and actions.
   - On mobile and tablet viewports ($< 1024\text{px}$), the layout SHALL gracefully stack, placing a compact brand header above the authentication card.

3. **Responsive 2-Column Registration Form**:
   - When in Register mode (`authMode === 'register'`), input fields SHALL arrange into a responsive 2-column grid on screens $\ge 640\text{px}$ (`grid sm:grid-cols-2 gap-3.5`):
     - Field 1: Full Name (`name`)
     - Field 2: Email Address (`email`)
     - Field 3: Password (`password`) with visibility toggle
     - Field 4: Confirm Password (`confirmPassword`) with visibility toggle
   - The 2-column layout SHALL reduce vertical card height by at least 40%, ensuring all form controls and action buttons remain visible above the fold on standard $1080\text{p}$ monitors.
   - On mobile screens ($< 640\text{px}$), fields SHALL stack vertically with standard $44\text{px}$ touch targets.

4. **Divider Layout Stability**:
   - The third-party OAuth divider SHALL use an absolute centering architecture (`absolute inset-0 flex items-center` with a relative centered text pill), eliminating flexbox dimension blowout and guaranteeing centered text alignment across all viewports.

#### Scenario: Guest user arrives at login on desktop monitor
- **WHEN** an unauthenticated user navigates to `/login` on a 1920x1080 display
- **THEN** the internal navigation sidebar (`AppSidebar`) is hidden
- **AND** the authentication surface displays the 2-column Studio Auth layout with the brand value stage on the left and the interactive card on the right
- **AND** the entire authentication card fits cleanly within the viewport without requiring page scrolling.

#### Scenario: User switches to Register mode on desktop
- **WHEN** the user clicks the "Register" tab on a desktop viewport
- **THEN** the form expands into a 2-column grid displaying Name and Email on row 1, and Password and Confirm Password on row 2
- **AND** the card width remains stable (`max-w-4xl` to `max-w-5xl`) with zero layout shift or width blowout.

#### Scenario: Mobile viewport responsiveness
- **WHEN** the user views `/login` on a mobile screen (390px width)
- **THEN** the layout stacks vertically with a compact brand header
- **AND** all input fields span 100% width with touch targets of at least 44px.
