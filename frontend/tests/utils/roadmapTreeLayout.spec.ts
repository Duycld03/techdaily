import { describe, it, expect } from 'vitest'
import {
  computeBezierPath,
  computeRoadmapTreeLayout,
  convertBookMilestonesToTree,
  convertCurriculumToTree,
  DEFAULT_LAYOUT_CONFIG,
  type TreeRoot,
  type TreeChapterBranch
} from '~/utils/roadmapTreeLayout'
import type { CurriculumRoadmapData } from '~/stores/useRoadmapStore'

describe('utils/roadmapTreeLayout', () => {
  describe('computeBezierPath', () => {
    it('generates a valid horizontal cubic Bezier curve SVG path string', () => {
      const path = computeBezierPath(100, 50, 300, 150)
      // dx = (300 - 100) * 0.5 = 100 -> cx1 = 200, cx2 = 200
      expect(path).toBe('M 100 50 C 200 50, 200 150, 300 150')
    })

    it('handles zero horizontal distance cleanly', () => {
      const path = computeBezierPath(100, 50, 100, 50)
      expect(path).toBe('M 100 50 C 100 50, 100 50, 100 50')
    })
  })

  describe('computeRoadmapTreeLayout', () => {
    const mockRoot: TreeRoot = {
      id: 'root-1',
      title: 'Distributed Systems',
      subtitle: 'Designing Data-Intensive Applications',
      totalCount: 10,
      completedCount: 3,
      progressPercentage: 30,
      trackType: 'book'
    }

    const mockChapters: TreeChapterBranch[] = [
      {
        id: 'ch-1',
        index: 1,
        title: 'Reliability and Scalability',
        totalCount: 2,
        completedCount: 2,
        isCompleted: true,
        isActive: false,
        slices: [
          {
            id: 'slice-1',
            order: 1,
            title: 'Fault Tolerance',
            status: 'completed',
            isCompleted: true,
            isActiveToday: false,
            isUpcoming: false
          },
          {
            id: 'slice-2',
            order: 2,
            title: 'Maintainability',
            status: 'completed',
            isCompleted: true,
            isActiveToday: false,
            isUpcoming: false
          }
        ]
      },
      {
        id: 'ch-2',
        index: 2,
        title: 'Data Models',
        totalCount: 2,
        completedCount: 1,
        isCompleted: false,
        isActive: true,
        slices: [
          {
            id: 'slice-3',
            order: 3,
            title: 'Relational vs Document',
            status: 'active_today',
            isCompleted: false,
            isActiveToday: true,
            isUpcoming: false
          },
          {
            id: 'slice-4',
            order: 4,
            title: 'Graph Models',
            status: 'upcoming',
            isCompleted: false,
            isActiveToday: false,
            isUpcoming: true
          }
        ]
      }
    ]

    it('handles empty chapters list gracefully', () => {
      const result = computeRoadmapTreeLayout(mockRoot, [], new Set())
      expect(result.root.x).toBe(DEFAULT_LAYOUT_CONFIG.rootX)
      expect(result.root.y).toBe(DEFAULT_LAYOUT_CONFIG.paddingTop)
      expect(result.chapters).toHaveLength(0)
      expect(result.slices).toHaveLength(0)
      expect(result.edges).toHaveLength(0)
      expect(result.boundingBox.width).toBeGreaterThan(0)
      expect(result.boundingBox.height).toBeGreaterThan(0)
    })

    it('positions collapsed chapters sequentially without child slices', () => {
      const expandedIds = new Set<string>() // all collapsed
      const result = computeRoadmapTreeLayout(mockRoot, mockChapters, expandedIds)

      expect(result.chapters).toHaveLength(2)
      expect(result.slices).toHaveLength(0)

      // Root to Chapter edges only (2 edges)
      expect(result.edges).toHaveLength(2)
      expect(result.edges[0].toId).toBe('ch-1')
      expect(result.edges[1].toId).toBe('ch-2')

      // Chapter Y positions
      const ch1 = result.chapters[0]
      const ch2 = result.chapters[1]
      expect(ch1.y).toBe(DEFAULT_LAYOUT_CONFIG.paddingTop)
      expect(ch2.y).toBe(ch1.y + DEFAULT_LAYOUT_CONFIG.chapterHeight + DEFAULT_LAYOUT_CONFIG.chapterGapY)

      // Root Y centered vertically with chapters
      const firstCenter = ch1.y + DEFAULT_LAYOUT_CONFIG.chapterHeight / 2
      const lastCenter = ch2.y + DEFAULT_LAYOUT_CONFIG.chapterHeight / 2
      const expectedRootCenter = (firstCenter + lastCenter) / 2
      expect(result.root.y).toBe(expectedRootCenter - DEFAULT_LAYOUT_CONFIG.rootHeight / 2)
    })

    it('expands chapter branch and positions slice leaves with dynamic height', () => {
      const expandedIds = new Set<string>(['ch-1'])
      const result = computeRoadmapTreeLayout(mockRoot, mockChapters, expandedIds)

      expect(result.chapters).toHaveLength(2)
      // ch-1 has 2 slices
      expect(result.slices).toHaveLength(2)
      expect(result.slices[0].data.id).toBe('slice-1')
      expect(result.slices[1].data.id).toBe('slice-2')

      // Slices horizontal position
      expect(result.slices[0].x).toBe(DEFAULT_LAYOUT_CONFIG.sliceX)
      expect(result.slices[1].x).toBe(DEFAULT_LAYOUT_CONFIG.sliceX)

      // Edges: 2 from root to chapters, 2 from ch-1 to slices
      expect(result.edges).toHaveLength(4)
      const sliceEdges = result.edges.filter(e => e.fromId === 'ch-1')
      expect(sliceEdges).toHaveLength(2)
      expect(sliceEdges[0].toId).toBe('slice-1')
      expect(sliceEdges[1].toId).toBe('slice-2')

      // Subsequent chapter ch-2 is pushed down by ch-1's branch height
      const sliceCount = 2
      const totalSlicesHeight = sliceCount * DEFAULT_LAYOUT_CONFIG.sliceHeight + (sliceCount - 1) * DEFAULT_LAYOUT_CONFIG.sliceGapY
      const branchHeight = Math.max(DEFAULT_LAYOUT_CONFIG.chapterHeight, totalSlicesHeight)
      expect(result.chapters[1].y).toBeGreaterThanOrEqual(DEFAULT_LAYOUT_CONFIG.paddingTop + branchHeight)
    })

    it('calculates bounding box encompassing all positioned nodes', () => {
      const expandedIds = new Set<string>(['ch-1', 'ch-2'])
      const result = computeRoadmapTreeLayout(mockRoot, mockChapters, expandedIds)

      const bb = result.boundingBox
      expect(bb.minX).toBeLessThanOrEqual(result.root.x)
      expect(bb.maxX).toBeGreaterThanOrEqual(result.slices[0].x + result.slices[0].width)
      expect(bb.width).toBe(bb.maxX - bb.minX)
      expect(bb.height).toBe(bb.maxY - bb.minY)
    })

    it('anchors root node near the active chapter rather than the distant global midpoint for 10+ chapters', () => {
      // Generate 20 chapters where chapter 3 is active
      const twentyChapters: TreeChapterBranch[] = Array.from({ length: 20 }, (_, i) => ({
        id: `ch-${i + 1}`,
        index: i + 1,
        title: `Chapter ${i + 1}`,
        totalCount: 2,
        completedCount: i < 2 ? 2 : 0,
        isCompleted: i < 2,
        isActive: i === 2, // Chapter 3 is active
        slices: []
      }))

      const expandedIds = new Set<string>(['ch-3'])
      const result = computeRoadmapTreeLayout(mockRoot, twentyChapters, expandedIds)

      const activeChapter = result.chapters.find(c => c.data.isActive)
      expect(activeChapter).toBeDefined()

      const firstChapter = result.chapters[0]
      const lastChapter = result.chapters[19]
      const globalMidpoint = (firstChapter.y + lastChapter.y) / 2

      // Root Y should be significantly closer to activeChapter than the global midpoint
      const distToActive = Math.abs(result.root.y - activeChapter!.y)
      const distToGlobalMidpoint = Math.abs(result.root.y - globalMidpoint)

      expect(distToActive).toBeLessThan(distToGlobalMidpoint)
    })
  })

  describe('convertBookMilestonesToTree', () => {
    it('converts document book milestones and pacer info into tree format', () => {
      const milestones = [
        {
          chapterTitle: 'Chapter 1: Storage',
          chapterIndex: 1,
          isCompleted: false,
          isActive: true,
          completedSlicesCount: 1,
          totalSlicesCount: 2,
          slices: [
            {
              id: 'c1-s1',
              chunkOrder: 1,
              sliceTitle: 'LSM Trees',
              summaryMarkdown: 'Log structured merge trees.',
              estimatedReadMinutes: 10,
              isCompleted: true,
              isActiveToday: false,
              isUpcoming: false
            },
            {
              id: 'c1-s2',
              chunkOrder: 2,
              sliceTitle: 'B-Trees',
              summaryMarkdown: 'Balanced trees in databases.',
              estimatedReadMinutes: 15,
              isCompleted: false,
              isActiveToday: true,
              isUpcoming: false
            }
          ]
        }
      ]

      const pacer = {
        currentChunkOrder: 2,
        totalChunks: 2,
        progressPercentage: 50
      }

      const { root, chapters } = convertBookMilestonesToTree('PostgreSQL Internals', milestones, pacer)

      expect(root.id).toBe('book-root')
      expect(root.title).toBe('PostgreSQL Internals')
      expect(root.totalCount).toBe(2)
      expect(root.completedCount).toBe(1)
      expect(root.progressPercentage).toBe(50)
      expect(root.trackType).toBe('book')

      expect(chapters).toHaveLength(1)
      expect(chapters[0].id).toBe('1')
      expect(chapters[0].title).toBe('Chapter 1: Storage')
      expect(chapters[0].isActive).toBe(true)

      expect(chapters[0].slices).toHaveLength(2)
      expect(chapters[0].slices[0].status).toBe('completed')
      expect(chapters[0].slices[1].status).toBe('active_today')
    })
  })

  describe('convertCurriculumToTree', () => {
    it('converts 30-day curriculum roadmap data into tree format', () => {
      const curriculumData: CurriculumRoadmapData = {
        totalDays: 30,
        completedDaysCount: 5,
        currentActiveDay: 6,
        overallProgressPercentage: 17,
        modules: [
          {
            category: 0,
            moduleTitle: 'Frontend & Architecture',
            description: 'Vue, Nuxt and Web Vitals',
            startDay: 1,
            endDay: 7,
            completedCount: 5,
            totalCount: 7,
            days: [
              {
                dayOrder: 1,
                slug: 'vue-reactivity',
                title: 'Vue 3 Reactivity Engine',
                summary: 'Deep dive into proxies.',
                difficulty: 1,
                isCompleted: true,
                isActiveToday: false,
                isUnlocked: true,
                drillScore: 90
              },
              {
                dayOrder: 6,
                slug: 'nuxt-ssr-hydration',
                title: 'Nuxt SSR Hydration Internals',
                summary: 'Hydration mismatches and streaming.',
                difficulty: 2,
                isCompleted: false,
                isActiveToday: true,
                isUnlocked: true,
                drillScore: null
              }
            ]
          }
        ]
      }

      const { root, chapters } = convertCurriculumToTree(curriculumData)

      expect(root.id).toBe('curriculum-root')
      expect(root.title).toBe('30-Day Senior Curriculum')
      expect(root.totalCount).toBe(30)
      expect(root.completedCount).toBe(5)
      expect(root.progressPercentage).toBe(17)
      expect(root.trackType).toBe('curriculum')

      expect(chapters).toHaveLength(1)
      expect(chapters[0].id).toBe('0')
      expect(chapters[0].title).toBe('Frontend & Architecture')
      expect(chapters[0].isActive).toBe(true)

      expect(chapters[0].slices).toHaveLength(2)
      expect(chapters[0].slices[0].status).toBe('completed')
      expect(chapters[0].slices[1].status).toBe('active_today')
    })
  })
})
