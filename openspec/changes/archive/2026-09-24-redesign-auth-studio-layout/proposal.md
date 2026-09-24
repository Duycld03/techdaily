# Proposal: Redesign Authentication Surface into Dev-Learning Studio Layout

## Why

The authentication surface (`pages/login.vue`) currently renders as a narrow phone-width card (`max-w-md` = 448px) with up to 12 stacked elements, causing excessive vertical elongation and visual isolation within an empty dark void on 1080p desktop monitors. Additionally, the authenticated internal navigation sidebar (`AppSidebar`) erroneously displays on guest auth routes, and flawed flexbox divider markup caused horizontal layout blowout during initial navigation. Redesigning the authentication surface delivers a modern, balanced, distraction-free Studio Auth experience conforming to TechDaily's **Dev-Learning Studio** visual language.

## What Changes

- **Auth Shell Isolation (`app.vue`)**: Suppress `AppSidebar` and internal authenticated navigation controls on guest authentication routes (`/login`), providing an immersive, dedicated login experience.
- **Spacious 2-Column Studio Auth Archetype (`login.vue`)**:
  - Replace the isolated single narrow column with a balanced, desktop-optimized Studio Auth layout (`max-w-4xl` to `max-w-5xl` container).
  - **Left Brand Stage**: Showcases TechDaily's engineering mission ("Master Senior Software Engineering Daily"), core platform pillars (Architecture Drills, SM-2 Spaced Repetition, System Design Doses), and telemetry indicators.
  - **Right Authentication Card**: Houses a refined `.glass-panel` card with mode switcher tabs, credentials form, Google OAuth, and terms, naturally fitting desktop viewports without requiring vertical page scrolling.
- **2-Column Responsive Form Layout in Register Mode**:
  - In Register mode, form inputs utilize a clean 2-column grid on desktop (`sm:grid-cols-2 gap-3.5`):
    - Row 1: Full Name (`name`) and Email (`email`)
    - Row 2: Password (`password`) and Confirm Password (`confirmPassword`)
  - Reduces card vertical length by over 40%, completely eliminating the cramped, overly tall aspect ratio.
  - Seamlessly degrades to a single column on mobile screens ($<640\text{px}$) for touch ergonomics.
- **Bulletproof Absolute Divider**:
  - Refactor the OAuth divider to use `absolute inset-0 flex items-center` with a centered uppercase text badge, preventing flex width blowout and eliminating text misalignment across all viewports.

## Capabilities

### Modified Capabilities
- `auth`: Update requirements for authentication page visual architecture, responsive registration form geometry, and shell isolation on guest routes.

## Impact

- `frontend/app.vue`: Conditional navigation chrome based on auth route detection.
- `frontend/pages/login.vue`: Complete template and layout redesign to Studio 2-column archetype.
- `frontend/tests/pages/login.spec.ts`: Unit tests validating responsive layout, 2-column form fields, and guest shell isolation.
