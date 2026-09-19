import type { CurriculumRoadmapData } from '~/stores/useRoadmapStore'

export type NodeStatus = 'completed' | 'active_today' | 'upcoming'

export interface TreeSliceLeaf {
  id: string
  order: number
  title: string
  subtitle?: string
  estimatedMinutes?: number
  status: NodeStatus
  isCompleted: boolean
  isActiveToday: boolean
  isUpcoming: boolean
  raw?: unknown
}

export interface TreeChapterBranch {
  id: string
  index: number
  title: string
  subtitle?: string
  totalCount: number
  completedCount: number
  isCompleted: boolean
  isActive: boolean
  slices: TreeSliceLeaf[]
}

export interface TreeRoot {
  id: string
  title: string
  subtitle?: string
  totalCount: number
  completedCount: number
  progressPercentage: number
  trackType: 'book' | 'curriculum'
}

export interface PositionedNode<T> {
  data: T
  x: number
  y: number
  width: number
  height: number
}

export interface ConnectorEdge {
  id: string
  fromId: string
  toId: string
  sourceX: number
  sourceY: number
  targetX: number
  targetY: number
  path: string
  status: NodeStatus | 'default'
}

export interface TreeBoundingBox {
  minX: number
  minY: number
  maxX: number
  maxY: number
  width: number
  height: number
}

export interface TreeLayoutResult {
  root: PositionedNode<TreeRoot>
  chapters: PositionedNode<TreeChapterBranch>[]
  slices: PositionedNode<TreeSliceLeaf>[]
  edges: ConnectorEdge[]
  boundingBox: TreeBoundingBox
}

export interface TreeLayoutConfig {
  rootX?: number
  rootWidth?: number
  rootHeight?: number

  chapterX?: number
  chapterWidth?: number
  chapterHeight?: number

  sliceX?: number
  sliceWidth?: number
  sliceHeight?: number

  sliceGapY?: number
  chapterGapY?: number
  paddingTop?: number
  paddingBottom?: number
  paddingLeft?: number
  paddingRight?: number
}

export const DEFAULT_LAYOUT_CONFIG: Required<TreeLayoutConfig> = {
  rootX: 60,
  rootWidth: 280,
  rootHeight: 88,

  chapterX: 420,
  chapterWidth: 280,
  chapterHeight: 68,

  sliceX: 780,
  sliceWidth: 300,
  sliceHeight: 56,

  sliceGapY: 12,
  chapterGapY: 24,
  paddingTop: 60,
  paddingBottom: 60,
  paddingLeft: 40,
  paddingRight: 60
}

/**
 * Computes a smooth horizontal cubic Bezier curve path string between two points.
 */
export function computeBezierPath(x1: number, y1: number, x2: number, y2: number): string {
  const dy = Math.abs(y2 - y1)
  const curvature = dy <= 400 ? 0.5 : 0.6
  const dx = (x2 - x1) * curvature
  return `M ${x1} ${y1} C ${x1 + dx} ${y1}, ${x2 - dx} ${y2}, ${x2} ${y2}`
}

/**
 * Calculates (x, y) coordinates for a horizontal tidy tree layout (Root -> Chapters -> Slices).
 * Dynamically adjusts Y positions based on whether chapter branches are expanded or collapsed.
 */
export function computeRoadmapTreeLayout(
  rootData: TreeRoot,
  chaptersData: TreeChapterBranch[],
  expandedChapterIds: Set<string>,
  customConfig?: TreeLayoutConfig
): TreeLayoutResult {
  const cfg: Required<TreeLayoutConfig> = {
    ...DEFAULT_LAYOUT_CONFIG,
    ...customConfig
  }

  const positionedChapters: PositionedNode<TreeChapterBranch>[] = []
  const positionedSlices: PositionedNode<TreeSliceLeaf>[] = []
  const edges: ConnectorEdge[] = []

  let currentY = cfg.paddingTop

  for (const chapter of chaptersData) {
    const isExpanded = expandedChapterIds.has(chapter.id)
    const hasSlices = chapter.slices && chapter.slices.length > 0

    if (isExpanded && hasSlices) {
      const sliceCount = chapter.slices.length
      const totalSlicesHeight = sliceCount * cfg.sliceHeight + (sliceCount - 1) * cfg.sliceGapY
      const branchHeight = Math.max(cfg.chapterHeight, totalSlicesHeight)

      // Vertically center chapter node in its branch
      const chapterY = currentY + (branchHeight - cfg.chapterHeight) / 2
      const positionedChapter: PositionedNode<TreeChapterBranch> = {
        data: chapter,
        x: cfg.chapterX,
        y: chapterY,
        width: cfg.chapterWidth,
        height: cfg.chapterHeight
      }
      positionedChapters.push(positionedChapter)

      // Position slices
      const sliceStartY = currentY + (branchHeight - totalSlicesHeight) / 2
      for (let j = 0; j < sliceCount; j++) {
        const slice = chapter.slices[j]
        if (!slice) continue

        const sliceY = sliceStartY + j * (cfg.sliceHeight + cfg.sliceGapY)
        const positionedSlice: PositionedNode<TreeSliceLeaf> = {
          data: slice,
          x: cfg.sliceX,
          y: sliceY,
          width: cfg.sliceWidth,
          height: cfg.sliceHeight
        }
        positionedSlices.push(positionedSlice)

        // Edge from chapter to slice
        const sourceX = cfg.chapterX + cfg.chapterWidth
        const sourceY = chapterY + cfg.chapterHeight / 2
        const targetX = cfg.sliceX
        const targetY = sliceY + cfg.sliceHeight / 2

        edges.push({
          id: `edge-${chapter.id}-${slice.id}`,
          fromId: chapter.id,
          toId: slice.id,
          sourceX,
          sourceY,
          targetX,
          targetY,
          path: computeBezierPath(sourceX, sourceY, targetX, targetY),
          status: slice.status
        })
      }

      currentY += branchHeight + cfg.chapterGapY
    } else {
      // Collapsed branch
      const chapterY = currentY
      positionedChapters.push({
        data: chapter,
        x: cfg.chapterX,
        y: chapterY,
        width: cfg.chapterWidth,
        height: cfg.chapterHeight
      })

      currentY += cfg.chapterHeight + cfg.chapterGapY
    }
  }

  // Calculate Root Y position:
  // For standard/compact trees (<= 10 chapters), center vertically between first and last chapter.
  // For large trees (> 10 chapters), anchor to the vertical centroid of active/expanded chapters
  // so the root node stays visually connected to the active study window.
  let rootY = cfg.paddingTop
  if (positionedChapters.length > 0) {
    let targetCenterY: number

    const focusedChapters = positionedChapters.filter(
      (c) => c.data.isActive || expandedChapterIds.has(c.data.id)
    )

    if (positionedChapters.length > 10 && focusedChapters.length > 0) {
      targetCenterY =
        focusedChapters.reduce((sum, c) => sum + (c.y + c.height / 2), 0) / focusedChapters.length
    } else {
      const firstChapter = positionedChapters[0]
      const lastChapter = positionedChapters[positionedChapters.length - 1]
      if (firstChapter && lastChapter) {
        const firstChapterCenterY = firstChapter.y + cfg.chapterHeight / 2
        const lastChapterCenterY = lastChapter.y + cfg.chapterHeight / 2
        targetCenterY = (firstChapterCenterY + lastChapterCenterY) / 2
      } else {
        targetCenterY = cfg.paddingTop
      }
    }

    const maxAllowedY = Math.max(cfg.paddingTop, currentY - cfg.rootHeight)
    rootY = Math.max(cfg.paddingTop, Math.min(targetCenterY - cfg.rootHeight / 2, maxAllowedY))
  }
  const positionedRoot: PositionedNode<TreeRoot> = {
    data: rootData,
    x: cfg.rootX,
    y: rootY,
    width: cfg.rootWidth,
    height: cfg.rootHeight
  }

  // Edges from Root to Chapters
  const rootSourceX = cfg.rootX + cfg.rootWidth
  const rootSourceY = rootY + cfg.rootHeight / 2

  for (const chNode of positionedChapters) {
    const targetX = chNode.x
    const targetY = chNode.y + chNode.height / 2

    edges.push({
      id: `edge-root-${chNode.data.id}`,
      fromId: rootData.id,
      toId: chNode.data.id,
      sourceX: rootSourceX,
      sourceY: rootSourceY,
      targetX,
      targetY,
      path: computeBezierPath(rootSourceX, rootSourceY, targetX, targetY),
      status: chNode.data.isActive ? 'active_today' : (chNode.data.isCompleted ? 'completed' : 'default')
    })
  }

  // Calculate bounding box across all visible nodes
  const allNodes = [positionedRoot, ...positionedChapters, ...positionedSlices]
  const minX = Math.min(...allNodes.map(n => n.x)) - cfg.paddingLeft
  const minY = Math.min(...allNodes.map(n => n.y)) - cfg.paddingTop
  const maxX = Math.max(...allNodes.map(n => n.x + n.width)) + cfg.paddingRight
  const maxY = Math.max(...allNodes.map(n => n.y + n.height)) + cfg.paddingBottom

  const boundingBox: TreeBoundingBox = {
    minX,
    minY,
    maxX,
    maxY,
    width: Math.max(100, maxX - minX),
    height: Math.max(100, maxY - minY)
  }

  return {
    root: positionedRoot,
    chapters: positionedChapters,
    slices: positionedSlices,
    edges,
    boundingBox
  }
}

/**
 * Converts Document Book Chapter Milestones into unified Tree data.
 */
export function convertBookMilestonesToTree(
  bookTitle: string,
  milestones: Array<{
    chapterTitle: string
    chapterIndex: number
    isCompleted: boolean
    isActive: boolean
    completedSlicesCount: number
    totalSlicesCount: number
    slices: Array<{
      id: string
      chunkOrder: number
      sliceTitle: string
      chapterTitle?: string
      summaryMarkdown?: string
      estimatedReadMinutes?: number
      isCompleted: boolean
      isActiveToday: boolean
      isUpcoming: boolean
    }>
  }>,
  pacer?: { currentChunkOrder: number; totalChunks: number; progressPercentage: number } | null
): { root: TreeRoot; chapters: TreeChapterBranch[] } {
  const totalCount = pacer?.totalChunks ?? milestones.reduce((sum, m) => sum + m.totalSlicesCount, 0)
  const completedCount = pacer
    ? Math.max(0, pacer.currentChunkOrder - 1)
    : milestones.reduce((sum, m) => sum + m.completedSlicesCount, 0)
  const progressPercentage = pacer?.progressPercentage ?? (totalCount > 0 ? Math.round((completedCount / totalCount) * 100) : 0)

  const root: TreeRoot = {
    id: 'book-root',
    title: bookTitle || 'Active Document',
    subtitle: pacer ? `Chunk ${pacer.currentChunkOrder} of ${pacer.totalChunks}` : 'Document Track',
    totalCount,
    completedCount,
    progressPercentage,
    trackType: 'book'
  }

  const chapters: TreeChapterBranch[] = milestones.map(m => {
    const slices: TreeSliceLeaf[] = m.slices.map(s => {
      let status: NodeStatus = 'upcoming'
      if (s.isActiveToday) status = 'active_today'
      else if (s.isCompleted) status = 'completed'

      return {
        id: s.id,
        order: s.chunkOrder,
        title: s.sliceTitle,
        subtitle: s.summaryMarkdown,
        estimatedMinutes: s.estimatedReadMinutes,
        status,
        isCompleted: s.isCompleted,
        isActiveToday: s.isActiveToday,
        isUpcoming: s.isUpcoming,
        raw: s
      }
    })

    return {
      id: String(m.chapterIndex),
      index: m.chapterIndex,
      title: m.chapterTitle,
      subtitle: `${m.completedSlicesCount}/${m.totalSlicesCount} completed`,
      totalCount: m.totalSlicesCount,
      completedCount: m.completedSlicesCount,
      isCompleted: m.isCompleted,
      isActive: m.isActive,
      slices
    }
  })

  return { root, chapters }
}

/**
 * Converts Curriculum Roadmap Data into unified Tree data.
 */
export function convertCurriculumToTree(
  curriculum: CurriculumRoadmapData
): { root: TreeRoot; chapters: TreeChapterBranch[] } {
  const root: TreeRoot = {
    id: 'curriculum-root',
    title: '30-Day Senior Curriculum',
    subtitle: 'Core fullstack architecture skill tree',
    totalCount: curriculum.totalDays || 30,
    completedCount: curriculum.completedDaysCount || 0,
    progressPercentage: curriculum.overallProgressPercentage || 0,
    trackType: 'curriculum'
  }

  const chapters: TreeChapterBranch[] = (curriculum.modules || []).map(mod => {
    const isCompleted = mod.completedCount === mod.totalCount && mod.totalCount > 0
    const isActive = mod.days ? mod.days.some(d => d.isActiveToday) : false

    const slices: TreeSliceLeaf[] = (mod.days || []).map(d => {
      let status: NodeStatus = 'upcoming'
      if (d.isActiveToday) status = 'active_today'
      else if (d.isCompleted) status = 'completed'

      return {
        id: String(d.dayOrder),
        order: d.dayOrder,
        title: d.title,
        subtitle: d.summary,
        status,
        isCompleted: d.isCompleted,
        isActiveToday: d.isActiveToday,
        isUpcoming: !d.isCompleted && !d.isActiveToday,
        raw: d
      }
    })

    return {
      id: String(mod.category),
      index: mod.category + 1,
      title: mod.moduleTitle,
      subtitle: `Days ${mod.startDay}–${mod.endDay} • ${mod.description}`,
      totalCount: mod.totalCount,
      completedCount: mod.completedCount,
      isCompleted,
      isActive,
      slices
    }
  })

  return { root, chapters }
}
