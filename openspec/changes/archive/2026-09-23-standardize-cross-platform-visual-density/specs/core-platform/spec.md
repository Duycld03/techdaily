# Spec Delta

## MODIFIED Requirements

### Requirement: Dev-Learning Studio Visual Language & Responsive Density
The web frontend SHALL implement the Dev-Learning Studio visual language with platform-independent visual density and uniform typography metrics:
1. **Font Metrics Parity**: The application SHALL load the `Inter` webfont across all supported browsers and platforms, preventing font metric discrepancies between operating systems (such as wider `Segoe UI` tracking on Windows versus condensed `Ubuntu` on Linux).
2. **Decoupled Typography Hierarchy**:
   - **Reading Prose Scale**: Long-form curriculum document text and article paragraphs SHALL use `text-base md:text-lg` (16px–18px) with `leading-relaxed` for reading ergonomics.
   - **Interactive Control Scale**: Interactive elements, scenario options, form inputs, buttons, card headers, and UI widgets SHALL use `text-sm md:text-base` (14px–16px), strictly preventing `text-lg` from bloating interactive controls.
3. **Card & Surface Spacing Tokens**: Standard card surfaces (`.glass-card`, `.glass-panel`) SHALL use balanced padding (`p-4 sm:p-5`) with standard radius (`rounded-2xl`), eliminating disproportionate padding (`p-7`, `p-8`, `md:p-10`).
4. **Natural Viewport Flow**: Primary dashboard and studio views SHALL utilize natural vertical scrolling (`min-h-[calc(100dvh-3.5rem)]`) and SHALL NOT lock container height with `overflow-hidden` on desktop displays, ensuring all widgets remain accessible when viewports are constrained by browser chrome and taskbars.

#### Scenario: Inter font loaded uniformly across operating systems
- **WHEN** a user accesses TechDaily from any desktop operating system (Windows 11, Linux, macOS)
- **THEN** the browser loads and renders the `Inter` webfont family with consistent letter spacing, character width, and x-height metrics.

#### Scenario: Interactive controls adhere to UI typography scale
- **WHEN** viewing interactive controls, option choices, buttons, and form inputs on desktop viewports
- **THEN** text is styled between `text-sm` (14px) and `text-base` (16px) rather than scaling up to `text-lg` (18px).

#### Scenario: Dashboard widgets remain accessible on constrained desktop viewports
- **WHEN** the dashboard is viewed in a browser with bookmarks bar and OS taskbar visible (available height $\le 860\text{px}$)
- **THEN** all Bento cards (including knowledge constellation) are reachable via smooth vertical scrolling without overflow clipping.
