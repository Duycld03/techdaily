// Canonical knowledge-graph visual tokens (v3.4.1 design).
//
// Single source of truth for node/edge colors, shapes, and edge-family styling,
// consumed by GraphCanvas.vue (2D Cytoscape), GraphCanvas3D.vue (WebGL),
// GraphLegend.vue, and GraphMinimap.vue so the renderers cannot drift apart.

export interface CategoryColor {
  /** Pillar-hub fill. */
  fill: string
  /** Accent border, also used as the lighter topic-node tone. */
  border: string
}

export const PALETTE_LIST: CategoryColor[] = [
  { fill: '#f59e0b', border: '#fbbf24' },
  { fill: '#0284c7', border: '#38bdf8' },
  { fill: '#0891b2', border: '#22d3ee' },
  { fill: '#7c3aed', border: '#a78bfa' },
  { fill: '#ec4899', border: '#fb7185' },
  { fill: '#6366f1', border: '#818cf8' },
  { fill: '#10b981', border: '#34d399' },
  { fill: '#f97316', border: '#fb923c' },
  { fill: '#14b8a6', border: '#2dd4bf' },
  { fill: '#8b5cf6', border: '#c084fc' },
  { fill: '#06b6d4', border: '#67e8f9' },
  { fill: '#d946ef', border: '#f0abfc' }
]

export function hashStringToColorIndex(str: string, modulo: number): number {
  let hash = 0
  for (let i = 0; i < str.length; i++) {
    hash = (hash << 5) - hash + str.charCodeAt(i)
    hash |= 0
  }
  return Math.abs(hash) % modulo
}

export const UNCLASSIFIED_CATEGORY_COLOR: CategoryColor = { fill: '#64748b', border: '#94a3b8' }

/** Stable across metadata edits, locale changes, ordering and graph renderers. */
export function getCategoryPalette(categoryId?: string | null): CategoryColor {
  if (!categoryId) return UNCLASSIFIED_CATEGORY_COLOR
  return PALETTE_LIST[hashStringToColorIndex(categoryId.toLowerCase(), PALETTE_LIST.length)]!
}
/** Fixed per-type node fills that are not derived from a pillar category. */
export const NODE_TYPE_COLOR = {
  book: '#6366f1',
  chunk: '#0ea5e9',
  highlight: '#06b6d4',
  card: '#f59e0b'
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
  booktopillar: true,
  chunktobook: true
}
// Atomic flashcard links.
const DOTTED_RELATIONS: Record<string, true> = {
  cardtochunk: true,
  cardtopillar: true,
  cardtohighlight: true,
  highlighttochunk: true
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
