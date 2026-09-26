import { describe, it, expect } from 'vitest'
import {
  CATEGORY_PALETTE,
  EDGE_FAMILY_LINK_WIDTH_3D,
  EDGE_FAMILY_STYLE,
  NODE_TYPE_COLOR,
  SM2_STATUS_COLOR,
  normalizeCategory,
  normalizeSm2Status,
  resolveEdgeFamily
} from '~/utils/graphVisualTokens'

describe('graphVisualTokens', () => {
  describe('resolveEdgeFamily', () => {
    it('maps structural anchors to the solid family', () => {
      expect(resolveEdgeFamily('TopicToPillar')).toBe('solid')
      expect(resolveEdgeFamily('BookToPillar')).toBe('solid')
    })

    it('maps atomic card links to the dotted family', () => {
      expect(resolveEdgeFamily('CardToHighlight')).toBe('dotted')
      expect(resolveEdgeFamily('CardToPillar')).toBe('dotted')
      expect(resolveEdgeFamily('CardToTopic')).toBe('dotted')
    })

    it('maps SharedTag and other associative links to the dashed family', () => {
      expect(resolveEdgeFamily('SharedTag')).toBe('dashed')
      expect(resolveEdgeFamily('BookToTopic')).toBe('dashed')
      expect(resolveEdgeFamily('HighlightToBook')).toBe('dashed')
      expect(resolveEdgeFamily('HighlightToTopic')).toBe('dashed')
    })

    it('defaults unknown, empty, and nullish relation types to dashed', () => {
      expect(resolveEdgeFamily('SomethingNew')).toBe('dashed')
      expect(resolveEdgeFamily('')).toBe('dashed')
      expect(resolveEdgeFamily(null)).toBe('dashed')
      expect(resolveEdgeFamily(undefined)).toBe('dashed')
    })

    it('is case-insensitive', () => {
      expect(resolveEdgeFamily('topictopillar')).toBe('solid')
      expect(resolveEdgeFamily('cardtotopic')).toBe('dotted')
    })
  })

  describe('edge family style', () => {
    it('carries the v3.4.1 line width, dash, and curvature per family', () => {
      expect(EDGE_FAMILY_STYLE.solid).toMatchObject({ width: 2.0, opacity: 0.85, lineStyle: 'solid', curvature: 0.35 })
      expect(EDGE_FAMILY_STYLE.dotted).toMatchObject({ width: 1.2, opacity: 0.6, lineStyle: 'dotted' })
      expect(EDGE_FAMILY_STYLE.dotted.dashPattern).toEqual([3, 4])
      expect(EDGE_FAMILY_STYLE.dashed).toMatchObject({ width: 1.5, lineStyle: 'dashed' })
      expect(EDGE_FAMILY_STYLE.dashed.dashPattern).toEqual([4, 4])
    })

    it('orders 3D link width solid > dashed > dotted', () => {
      expect(EDGE_FAMILY_LINK_WIDTH_3D.solid).toBeGreaterThan(EDGE_FAMILY_LINK_WIDTH_3D.dashed)
      expect(EDGE_FAMILY_LINK_WIDTH_3D.dashed).toBeGreaterThan(EDGE_FAMILY_LINK_WIDTH_3D.dotted)
    })
  })

  describe('category palette', () => {
    it('resolves the canonical fill for each pillar', () => {
      expect(CATEGORY_PALETTE.FrontendWeb.fill).toBe('#f59e0b')
      expect(CATEGORY_PALETTE.BackendDotNet.fill).toBe('#0284c7')
      expect(CATEGORY_PALETTE.DatabaseStorage.fill).toBe('#0891b2')
      expect(CATEGORY_PALETTE.SystemDesign.fill).toBe('#7c3aed')
      expect(CATEGORY_PALETTE.EngineeringCraft.fill).toBe('#ec4899')
    })

    it('normalizes API category aliases onto canonical keys', () => {
      expect(normalizeCategory('Frontend')).toBe('FrontendWeb')
      expect(normalizeCategory('BackendRuntime')).toBe('BackendDotNet')
      expect(normalizeCategory('DotNet')).toBe('BackendDotNet')
      expect(normalizeCategory('Postgres')).toBe('DatabaseStorage')
      expect(normalizeCategory('DistributedSystems')).toBe('SystemDesign')
      expect(normalizeCategory('Craft')).toBe('EngineeringCraft')
      expect(normalizeCategory(null)).toBe('BackendDotNet')
    })
  })

  describe('node type and SM-2 colors', () => {
    it('uses cyan highlights and indigo books', () => {
      expect(NODE_TYPE_COLOR.highlight).toBe('#06b6d4')
      expect(NODE_TYPE_COLOR.book).toBe('#6366f1')
    })

    it('maps SM-2 status to amber/blue/violet with mastered violet', () => {
      expect(SM2_STATUS_COLOR[normalizeSm2Status('Learning')]).toBe('#f59e0b')
      expect(SM2_STATUS_COLOR[normalizeSm2Status('Reviewing')]).toBe('#3b82f6')
      expect(SM2_STATUS_COLOR[normalizeSm2Status('Mastered')]).toBe('#7c3aed')
      // Unknown/nullish status defaults to reviewing.
      expect(normalizeSm2Status(null)).toBe('reviewing')
    })
  })
})
