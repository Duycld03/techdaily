import { afterEach, describe, it, expect, vi } from 'vitest'
import { enableAutoUnmount, mount } from '@vue/test-utils'
import type { AnimationOptions, Core } from 'cytoscape'
import GraphMinimap from '~/components/graph/GraphMinimap.vue'

enableAutoUnmount(afterEach)

describe('GraphMinimap.vue', () => {
  it.each([
    { zoom: 2, clickX: 246, clickY: 117, panX: 0, panY: 160 },
    { zoom: 2, clickX: 314, clickY: 83, panX: -400, panY: 360 },
    { zoom: 0.5, clickX: 314, clickY: 83, panX: 200, panY: 315 }
  ])('centers the clicked model point at zoom $zoom for ($clickX, $clickY)', async ({ zoom, clickX, clickY, panX, panY }) => {
    const animate = vi.fn<(options: AnimationOptions) => void>()
    const cy = {
      on: vi.fn(),
      off: vi.fn(),
      nodes: () => ({
        length: 2,
        boundingBox: () => ({ x1: 100, y1: -80, x2: 500, y2: 120, w: 400, h: 200 }),
        forEach: vi.fn()
      }),
      extent: () => ({ x1: 0, y1: 0, x2: 100, y2: 100 }),
      width: () => 800,
      height: () => 600,
      zoom: () => zoom,
      animate
    }
    const wrapper = mount(GraphMinimap, { props: { cy: cy as unknown as Core } })
    const canvas = wrapper.get('canvas')
    vi.spyOn(canvas.element, 'getBoundingClientRect').mockReturnValue(new DOMRect(200, 50, 160, 100))

    await canvas.trigger('click', { clientX: clickX, clientY: clickY })

    const target = animate.mock.calls[0]?.[0].pan
    expect(target?.x).toBeCloseTo(panX)
    expect(target?.y).toBeCloseTo(panY)
  })
})
