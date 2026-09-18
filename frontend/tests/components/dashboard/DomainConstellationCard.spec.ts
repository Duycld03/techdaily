import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import DomainConstellationCard from '~/components/dashboard/DomainConstellationCard.vue'

describe('DomainConstellationCard.vue', () => {
  it('renders default node and edge counts', () => {
    const wrapper = mount(DomainConstellationCard, {
      global: {
        stubs: {
          NuxtLink: {
            template: '<a :href="to"><slot /></a>',
            props: ['to']
          }
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    expect(wrapper.text()).toContain('148')
    expect(wrapper.text()).toContain('210')
  })

  it('renders custom node and edge counts correctly', () => {
    const wrapper = mount(DomainConstellationCard, {
      props: {
        nodeCount: 320,
        edgeCount: 540
      },
      global: {
        stubs: {
          NuxtLink: {
            template: '<a :href="to"><slot /></a>',
            props: ['to']
          }
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    expect(wrapper.text()).toContain('320')
    expect(wrapper.text()).toContain('540')
  })

  it('contains navigation link to /graph', () => {
    const wrapper = mount(DomainConstellationCard, {
      global: {
        stubs: {
          NuxtLink: {
            template: '<a :href="to"><slot /></a>',
            props: ['to']
          }
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    const link = wrapper.find('a')
    expect(link.exists()).toBe(true)
    expect(link.attributes('href')).toBe('/graph')
  })

  it('renders constellation SVG nodes and edges', () => {
    const wrapper = mount(DomainConstellationCard, {
      global: {
        stubs: {
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    const svg = wrapper.find('svg')
    expect(svg.exists()).toBe(true)
    const lines = wrapper.findAll('line')
    expect(lines.length).toBeGreaterThan(0)
    expect(wrapper.text()).toContain('Distributed')
    expect(wrapper.text()).toContain('Database')
  })

  it('renders stationary pulsing vertex aura with animate-pulse without animate-ping', () => {
    const wrapper = mount(DomainConstellationCard, {
      global: {
        stubs: {
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    const pulseCircle = wrapper.find('circle.animate-pulse')
    expect(pulseCircle.exists()).toBe(true)
    expect(wrapper.find('circle.animate-ping').exists()).toBe(false)
  })

  it('header link has stable positioning without hover translation jitter', () => {
    const wrapper = mount(DomainConstellationCard, {
      global: {
        stubs: {
          NuxtLink: {
            template: '<a :href="to" :class="$attrs.class"><slot /></a>',
            props: ['to']
          }
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    const link = wrapper.find('a')
    expect(link.attributes('class')).not.toContain('group-hover:translate-x-0.5')
  })
})
