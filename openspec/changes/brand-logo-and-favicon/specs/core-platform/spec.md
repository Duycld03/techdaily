# Spec Delta: Core Platform

## MODIFIED Requirements

### Requirement: Global Application Shell Topbar Architecture
The top application header (`AppHeader.vue`) SHALL provide persistent global navigation and identity controls adhering to the Dev-Learning Studio design language across desktop and mobile viewports.

1. **Brand Identity & Emblem**:
   - The topbar and mobile drawer SHALL feature the **Stitch Developer Emblem**:
     - Squircle boundary with dark obsidian gradient (`#151419` to `#08080a`) and subtle violet border stroke.
     - Symmetrical regular hexagon ring with electric violet gradient (`#c4b5fd` to `#5b21b6`).
     - Inner concentric hairline hexagon for architectural precision.
     - Symmetrical left and right code brackets (`< >`) flanking the core.
     - Central glowing memory nucleus node with soft ambient radial halo.
   - The brand title SHALL display "TechDaily" with high-contrast gradient text.
   - Clicking the brand emblem or title SHALL navigate to the root Home Bento Dashboard (`/`).

2. **Quick Jump & Global Shortcuts**:
   - Centered `⌘K Quick Jump` pill trigger button displaying localized placeholder and `⌘K` keyboard badge on desktop viewports.
   - Interactive streak pill displaying the user's active streak count with an amber glow flame icon.
   - Locale switcher (`LocaleSelector.vue`) with smooth `transition-colors` and user profile ring.

#### Scenario: User clicks brand logo in application header
- **WHEN** user clicks the TechDaily brand emblem or title in `AppHeader.vue`
- **THEN** the application navigates to the root Home Bento Dashboard (`/`).
- **AND** the emblem renders the Stitch Developer Emblem with crisp vector lines.

#### Scenario: User opens mobile navigation drawer
- **WHEN** user opens the mobile navigation drawer on a smartphone viewport
- **THEN** the drawer brand header renders the Stitch Developer Emblem alongside the localized navigation groups.

---

## ADDED Requirements

### Requirement: Universal Scalable Vector Favicon
The application SHALL serve a high-fidelity scalable vector favicon (`/favicon.svg`) derived from the Stitch Developer Emblem to provide immediate brand recognition across browser tabs, bookmarks, and mobile PWA home screens.

1. **Vector Geometry & Legibility**:
   - The favicon SHALL use SVG vector definitions optimized for multi-resolution rendering (16x16, 32x32, 48x48, and 512x512).
   - The graphic elements (brackets, hexagon, glowing node) SHALL maintain high contrast against both dark and light browser chrome backgrounds.

#### Scenario: Browser loads site favicon
- **WHEN** any page of TechDaily is loaded in a web browser
- **THEN** the browser tab displays the Stitch Developer Emblem favicon (`/favicon.svg`).
- **AND** the icon is crisp and clearly identifiable on both dark and light browser tab bars.
