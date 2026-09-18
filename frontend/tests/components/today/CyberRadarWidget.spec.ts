import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import CyberRadarWidget from '~/components/today/CyberRadarWidget.vue'

describe('CyberRadarWidget.vue', () => {
  it('renders default telemetry statistics and status badges', () => {
    const wrapper = mount(CyberRadarWidget)

    expect(wrapper.text()).toContain('Telemetry Live')
    expect(wrapper.text()).toContain('ONLINE')
    expect(wrapper.text()).toContain('148')
    expect(wrapper.text()).toContain('210')
    expect(wrapper.find('svg').exists()).toBe(true)
  })

  it('accepts customized node and edge counts', () => {
    const wrapper = mount(CyberRadarWidget, {
      props: {
        nodeCount: 256,
        edgeCount: 512
      }
    })

    expect(wrapper.text()).toContain('256')
    expect(wrapper.text()).toContain('512')
  })

  it('renders concentric range circles and rotating sweep needle', () => {
    const wrapper = mount(CyberRadarWidget)

    const circles = wrapper.findAll('circle')
    // Ambient backdrop, 4 range rings, node blips, center hub dots
    expect(circles.length).toBeGreaterThan(6)

    // Animated rotating sweep group
    const rotatingGroup = wrapper.find('.animate-\\[spin_3\\.5s_linear_infinite\\]')
    expect(rotatingGroup.exists()).toBe(true)
    expect(rotatingGroup.find('line').exists()).toBe(true)
  })
})
