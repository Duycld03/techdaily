import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import type { Core } from 'cytoscape'
import GraphMinimap from '~/components/graph/GraphMinimap.vue'

describe('GraphMinimap.vue', () => {
  it('renders minimap container and canvas element', () => {
    const wrapper = mount(GraphMinimap, {
      props: { cy: null }
    })

    expect(wrapper.text()).toContain('Minimap')
    const canvas = wrapper.find('canvas')
    expect(canvas.exists()).toBe(true)
    expect(canvas.attributes('width')).toBe('160')
    expect(canvas.attributes('height')).toBe('100')
  })

  it('handles cytoscape instance and attaches lifecycle listeners', () => {
    const mockOn = vi.fn()
    const mockOff = vi.fn()
    const mockNodes = vi.fn(() => ({
      length: 0,
      boundingBox: () => ({ x1: 0, y1: 0, x2: 100, y2: 100, w: 100, h: 100 }),
      forEach: vi.fn()
    }))

    const mockCy = {
      on: mockOn,
      off: mockOff,
      nodes: mockNodes,
      extent: () => ({ x1: 0, y1: 0, x2: 50, y2: 50 }),
      animate: vi.fn()
    }

    const wrapper = mount(GraphMinimap, {
      props: { cy: mockCy as unknown as Core }
    })

    expect(mockOn).toHaveBeenCalledWith(
      'render pan zoom position add remove',
      expect.any(Function)
    )

    wrapper.unmount()
    expect(mockOff).toHaveBeenCalledWith(
      'render pan zoom position add remove',
      expect.any(Function)
    )
  })

  it('handles canvas click to pan viewport when nodes exist', async () => {
    const mockAnimate = vi.fn()
    const mockNodes = vi.fn(() => ({
      length: 2,
      boundingBox: () => ({ x1: 0, y1: 0, x2: 200, y2: 200, w: 200, h: 200 }),
      forEach: vi.fn()
    }))

    const mockCy = {
      on: vi.fn(),
      off: vi.fn(),
      nodes: mockNodes,
      extent: () => ({ x1: 0, y1: 0, x2: 100, y2: 100 }),
      animate: mockAnimate
    }

    const wrapper = mount(GraphMinimap, {
      props: { cy: mockCy as unknown as Core }
    })

    const canvas = wrapper.find('canvas')
    await canvas.trigger('click', { clientX: 50, clientY: 50 })

    expect(mockAnimate).toHaveBeenCalled()
  })
})
