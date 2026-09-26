// Canonical knowledge-graph visual tokens (v3.4.1 design).
//
// Single source of truth for node/edge colors, shapes, and edge-family styling,
// consumed by GraphCanvas.vue (2D Cytoscape), GraphCanvas3D.vue (WebGL),
// GraphLegend.vue, and GraphMinimap.vue so the renderers cannot drift apart.

export type PillarCategory =
  | 'FrontendWeb'
  | 'BackendDotNet'
  | 'DatabaseStorage'
  | 'SystemDesign'
  | 'EngineeringCraft'

export interface CategoryColor {
  /** Pillar-hub fill. */
  fill: string
  /** Accent border, also used as the lighter topic-node tone. */
  border: string
}

/**
 * Per-pillar palette. Category aliases coming from the API
 * (e.g. "Frontend", "BackendRuntime", "DotNet", "Postgres",
 * "DistributedSystems", "Craft") normalize onto these five canonical keys.
 */
export const CATEGORY_PALETTE: Record<PillarCategory, CategoryColor> = {
  FrontendWeb: { fill: '#f59e0b', border: '#fbbf24' },
  BackendDotNet: { fill: '#0284c7', border: '#38bdf8' },
  DatabaseStorage: { fill: '#0891b2', border: '#22d3ee' },
  SystemDesign: { fill: '#7c3aed', border: '#a78bfa' },
  EngineeringCraft: { fill: '#ec4899', border: '#fb7185' }
}

/** Map any raw category string onto a canonical pillar key. */
export function normalizeCategory(category?: string | null): PillarCategory {
  const c = (category ?? '').toLowerCase()
  if (c.includes('frontend') || c.includes('web')) return 'FrontendWeb'
  if (c.includes('database') || c.includes('storage') || c.includes('postgres')) return 'DatabaseStorage'
  if (c.includes('system') || c.includes('distributed')) return 'SystemDesign'
  if (c.includes('craft')) return 'EngineeringCraft'
  // Backend / DotNet / runtime and the safe default.
  return 'BackendDotNet'
}

/** Fixed per-type node fills that are not derived from a pillar category. */
export const NODE_TYPE_COLOR = {
  book: '#6366f1',
  highlight: '#06b6d4'
} as const

export type Sm2Status = 'learning' | 'reviewing' | 'mastered'

/** Flashcard SM-2 retention colors. Mastered is brand violet (never emerald). */
export const SM2_STATUS_COLOR: Record<Sm2Status, string> = {
  learning: '#f59e0b',
  reviewing: '#3b82f6',
  mastered: '#7c3aed'
}

/** Border accent applied to mastered flashcard diamonds. */
export const MASTERED_BORDER = '#c4b5fd'

export function normalizeSm2Status(status?: string | null): Sm2Status {
  const s = (status ?? '').toLowerCase()
  if (s === 'mastered') return 'mastered'
  if (s === 'learning') return 'learning'
  return 'reviewing'
}

export type EdgeFamily = 'solid' | 'dotted' | 'dashed'

// Structural anchors to a hub/parent.
const SOLID_RELATIONS: Record<string, true> = {
  topictopillar: true,
  booktopillar: true
}
// Atomic flashcard links.
const DOTTED_RELATIONS: Record<string, true> = {
  cardtohighlight: true,
  cardtopillar: true,
  cardtotopic: true
}
// Everything else (SharedTag, BookToTopic, HighlightToBook, HighlightToTopic,
// and any unknown relation) falls back to the associative dashed family.

/** Resolve a relation type to exactly one visual edge family. */
export function resolveEdgeFamily(relationType?: string | null): EdgeFamily {
  const r = (relationType ?? '').toLowerCase()
  if (SOLID_RELATIONS[r]) return 'solid'
  if (DOTTED_RELATIONS[r]) return 'dotted'
  return 'dashed'
}

export interface EdgeFamilyStyle {
  /** 2D Cytoscape line width in px. */
  width: number
  /** Active-edge opacity. */
  opacity: number
  lineStyle: 'solid' | 'dotted' | 'dashed'
  /** Dash pattern for dotted/dashed families. */
  dashPattern?: [number, number]
  /** Bézier control-point weight for the solid family. */
  curvature?: number
}

export const EDGE_FAMILY_STYLE: Record<EdgeFamily, EdgeFamilyStyle> = {
  solid: { width: 2.0, opacity: 0.85, lineStyle: 'solid', curvature: 0.35 },
  dotted: { width: 1.2, opacity: 0.6, lineStyle: 'dotted', dashPattern: [3, 4] },
  dashed: { width: 1.5, opacity: 0.7, lineStyle: 'dashed', dashPattern: [4, 4] }
}

/** Relative 3D link width per family (dashes are not expressible in WebGL lines). */
export const EDGE_FAMILY_LINK_WIDTH_3D: Record<EdgeFamily, number> = {
  solid: 1.2,
  dashed: 0.9,
  dotted: 0.6
}

/** Uniform dimmed-element opacity used by filter / search / legend-hover isolation. */
export const DIM_OPACITY = 0.15

/** 3D directional particle pulse rate for active edges. */
export const EDGE_PARTICLE_SPEED = 0.007
