import { describe, it, expect, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import DomainConstellationCard from '~/components/dashboard/DomainConstellationCard.vue'
import { useKnowledgeGraphStore } from '~/stores/useKnowledgeGraphStore'

describe('DomainConstellationCard.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('renders default fallback node and edge counts', () => {
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

  it('renders custom node and edge counts correctly via props', () => {
    const wrapper = mount(DomainConstellationCard, {
      props: {
        nodeCount: 320,
        edgeCount: 540,
        cardCount: 42,
        highlightCount: 18,
        chunkCount: 9
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
    expect(wrapper.text()).toContain('42')
    expect(wrapper.text()).toContain('18')
    expect(wrapper.text()).toContain('9')
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

  it('renders knowledge graph metrics from store when props are omitted', () => {
    const store = useKnowledgeGraphStore()
    store.rawData = {
      nodes: [],
      edges: [],
      stats: {
        totalNodes: 85,
        totalEdges: 120,
        nodeTypeCounts: {
          card: 35,
          highlight: 12,
          chunk: 7
        },
        pillarCounts: {},
        masteredCardsCount: 20
      }
    }

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

    expect(wrapper.text()).toContain('85')
    expect(wrapper.text()).toContain('120')
    expect(wrapper.text()).toContain('35')
    expect(wrapper.text()).toContain('12')
    expect(wrapper.text()).toContain('7')
  })

  it('does NOT render static SVG constellation vertices or lines', () => {
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

    // Assert that the old static SVG constellation canvas is eliminated
    expect(wrapper.find('svg.overflow-visible').exists()).toBe(false)
    expect(wrapper.findAll('line').length).toBe(0)
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
