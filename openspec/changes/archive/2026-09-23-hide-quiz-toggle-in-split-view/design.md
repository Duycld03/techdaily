# Design

## Context

The Today page (`frontend/pages/today.vue`) offers two primary learning layouts:
1. **Desktop / Tablet (`≥md`)**: Two-column split view with the Document Reader on the left and the Scenario Challenge Dock on the right.
2. **Mobile (`<md`)**: Single-column view with a persistent bottom tab bar to switch between Reader and Scenario Challenge.

The header toolbar included a purple toggle button (Terminal icon) wired to `isChallengeDockOpen = !isChallengeDockOpen`.

## Goals / Non-Goals

**Goals:**
- Eliminate redundant controls in the mobile header toolbar where screen width is constrained.
- Maintain full user control on desktop to collapse the dock into "Full Immersion Reader" mode and reopen it.

**Non-Goals:**
- Removing the Scenario Challenge dock or modifying its internal state management.

## Decisions

### 1. Hide on Mobile via Responsive Classes (`hidden md:flex`)
On mobile, navigation between Reader and Challenge Dock is handled by the bottom tab bar. Showing a second toggle button in the compact header crowded out essential pacer controls (slice stepper, book title). Changing the button's display class to `hidden md:flex` declutters the mobile header.

### 2. Maintain Toggle Button on Desktop Regardless of Dock State
An initial implementation used `v-show="!isChallengeDockOpen"`, hiding the button whenever the dock was open. This created a UX defect: desktop users had no way to collapse the dock to enter focused reading mode.
The button is therefore kept visible on desktop (`md:flex`), displaying "Dock" when open (active styling) and "Scenario" when collapsed.

## Risks / Trade-offs

- **[Risk] Users unaware that dock can collapse on desktop** → Mitigation: Active state styling (`bg-brand-600`) and clear tooltip text (*"Collapse Scenario Dock (Full Immersion Reader)"*) make the interactive affordance obvious.
