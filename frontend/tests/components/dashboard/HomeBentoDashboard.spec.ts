import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import HomeBentoDashboard from '~/components/dashboard/HomeBentoDashboard.vue'
import { useAuthStore } from '~/stores/useAuthStore'
import { useDailyFocusStore } from '~/stores/useDailyFocusStore'
import { useReviewStore } from '~/stores/useReviewStore'
import { useKnowledgeGraphStore } from '~/stores/useKnowledgeGraphStore'

describe('HomeBentoDashboard.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('mounts cleanly without crashing when all stores are empty', () => {
    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          ConcentricMetricCard: true,
          DomainConstellationCard: true,
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    expect(wrapper.exists()).toBe(true)
    expect(wrapper.text()).toContain('dashboard.welcome_back')
    expect(wrapper.text()).toContain('Engineer')
  })

  it('renders user details when authStore has a user', () => {
    const authStore = useAuthStore()
    authStore.user = {
      id: 'u-1',
      name: 'Alex Developer',
      email: 'alex@example.com',
      preferredLocale: 'en'
    } as any

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          ConcentricMetricCard: true,
          DomainConstellationCard: true,
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    expect(wrapper.text()).toContain('Alex')
  })

  it('renders curriculum slice and scenario drill when focusStore has data', () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = {
      topic: {
        id: 'top-1',
        title: 'Distributed Consensus & Raft',
        summary: 'Deep dive into state machine replication and log entries.',
        dayOrder: 5
      },
      pacer: {
        bookId: 'b-1',
        bookTitle: 'Distributed Systems Patterns',
        currentChunkOrder: 3,
        totalChunks: 15
      },
      scenario: {
        title: 'Handling Network Partitions in Cluster',
        situation: 'Two nodes lose heartbeat connection simultaneously.'
      },
      currentStreak: 7,
      freezeCreditsRemaining: 2
    } as any

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          ConcentricMetricCard: true,
          DomainConstellationCard: true,
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    expect(wrapper.text()).toContain('Distributed Consensus & Raft')
    expect(wrapper.text()).toContain('Distributed Systems Patterns')
    expect(wrapper.text()).toContain('Handling Network Partitions in Cluster')
    expect(wrapper.text()).toContain('7')
    expect(wrapper.text()).toContain('2/2')
  })

  it('passes correct node and edge counts to DomainConstellationCard', () => {
    const graphStore = useKnowledgeGraphStore()
    graphStore.rawData = {
      nodes: new Array(180).fill({ id: '1' }),
      edges: new Array(250).fill({ id: 'e1' }),
      stats: { totalNodes: 180, totalEdges: 250, nodeTypeCounts: {}, pillarCounts: {}, masteredCardsCount: 0 }
    } as any

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          ConcentricMetricCard: true,
          DomainConstellationCard: {
            template: '<div class="constellation-stub">{{ nodeCount }} - {{ edgeCount }}</div>',
            props: ['nodeCount', 'edgeCount']
          },
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    expect(wrapper.find('.constellation-stub').text()).toContain('180 - 250')
  })

  it('binds Spaced Repetition deck statistics correctly to ConcentricMetricCard', () => {
    const reviewStore = useReviewStore()
    reviewStore.deckStatistics = {
      totalCards: 42,
      learningCount: 10,
      reviewingCount: 12,
      masteredCount: 20
    }
    reviewStore.totalCardsDue = 5

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          ConcentricMetricCard: {
            template: '<div class="metric-stub">{{ masteredCards }}/{{ totalCards }} due:{{ dueCards }}</div>',
            props: ['masteredCards', 'totalCards', 'dueCards']
          },
          DomainConstellationCard: true,
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    expect(wrapper.find('.metric-stub').text()).toContain('20/42 due:5')
  })

  it('renders dynamic slice progress badge and routes to gitbook reader on start reading', async () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = {
      topic: { id: 'top-1', title: 'ASP.NET Core Architecture', dayOrder: 3 },
      pacer: { bookId: 'aspnet-doc', currentChunkOrder: 4, totalChunks: 23 },
      scenario: { title: 'Middleware Pipeline Debugging' }
    } as any

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          ConcentricMetricCard: true,
          DomainConstellationCard: true,
          NuxtLink: true
        }
      }
    })

    // Checks dynamic slice badge
    expect(wrapper.text()).toContain('Slice 4 / 23')
    // Find and click Continue Reading button
    const continueBtn = wrapper.findAll('button').find(b => b.text().includes('dashboard.continue_reading'))
    expect(continueBtn).toBeDefined()
    await continueBtn!.trigger('click')

    expect((globalThis as any).navigateTo).toHaveBeenCalledWith({
      path: '/read/aspnet-doc',
      query: { slice: '4' }
    })
  })

  it('routes to /today when solve challenge is clicked', async () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = {
      topic: { id: 'top-1', title: 'ASP.NET Core Architecture', dayOrder: 3 },
      scenario: { title: 'Middleware Pipeline Debugging' }
    } as any

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          ConcentricMetricCard: true,
          DomainConstellationCard: true,
          NuxtLink: true
        }
      }
    })

    const challengeBtn = wrapper.findAll('button').find(b => b.text().includes('dashboard.solve_challenge'))
    expect(challengeBtn).toBeDefined()
    await challengeBtn!.trigger('click')

    expect((globalThis as any).navigateTo).toHaveBeenCalledWith('/today')
  })
})
