# Proposal: UI Palette Refinement & Visual Declutter

## Why

The current dark studio UI exhibits chromatic saturation fog ("purple overload") where purple/violet tints are over-applied simultaneously across canvas backgrounds, subtle card containers, radial blur glows, active sidebar states, category badges, icons, progress bars, CTA buttons, and metric rings. This creates visual fatigue and eliminates focal hierarchy because primary actions cannot stand out against an already saturated background. 

Furthermore, competing accent hues (purple, cyan, yellow, green) juxtaposed with multi-layered visual effects (glows, heavy gradients, borders, and rounded surfaces) create a claustrophobic, busy aesthetic. Refining the palette to a neutral obsidian dark base with desaturated, surgical iris violet accents will elevate the interface to a clean, premium developer-studio standard (Linear/Raycast aesthetic) that is comfortable for extended reading.

## What Changes

- **Neutral Obsidian Canvas Base**: Neutralize `canvas.DEFAULT` from `#09080e` (purple-tinted black) to `#09090b` (true neutral dark obsidian/zinc-950), `canvas.subtle` from `#12101b` to `#121215`, and `canvas.elevated` from `#1a1726` to `#18181b`, eliminating purple background fog across the application.
- **De-purple Secondary Badges & Icon Containers**: Convert category labels (e.g. `ACTIVE READING SLICE`, `SYSTEM SCENARIO`) and card header icon wrappers from bright purple to muted, neutral slate/glass surfaces (`text-slate-400 font-mono text-[11px]`, `bg-white/[0.04] text-slate-400`).
- **Surgical, Desaturated Brand Accent**: Refine `brand-600`/`brand-500` to a deeper, more sophisticated Iris Violet (`#7c3aed`/`#6d28d9`), reserving high saturation strictly for primary interaction focal points (the "Continue Reading" CTA and core progress indicators).
- **Eliminate Ambient Glow Pollution**: Remove unnecessary ambient purple radial blur glows (`bg-brand-500/10 blur-3xl`) and heavy container gradients in dashboard cards in favor of crisp, flat dark glass with subtle hairline borders (`border-white/[0.06]`).
- **Sidebar Active Indicator Refinement**: Replace heavy purple background flooding in the active sidebar item (`bg-brand-500/15`) with a subtle glass highlight (`bg-white/[0.06] text-white`) anchored by a delicate 2px vertical accent bar on the left edge.
- **Palette Hierarchy Harmony**: Balance secondary telemetry indicators (Cyan Radar, Amber Streak, Yellow Points) to muted secondary roles so the active reading curriculum remains the unmistakable primary visual focal point.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `core-platform`: Update the design system token specifications to require neutral obsidian canvas backgrounds, surgical accent application, and decluttered surface effects.
