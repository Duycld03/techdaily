import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import HomeBentoDashboard from '~/components/dashboard/HomeBentoDashboard.vue'
import { useAuthStore, type AuthUser } from '~/stores/useAuthStore'
import { useDailyFocusStore, type TodayFocusResponse } from '~/stores/useDailyFocusStore'
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
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    expect(wrapper.exists()).toBe(true)
    expect(wrapper.text()).toContain('dashboard.active_reading_slice')
    expect(wrapper.text()).toContain('Senior Software Engineer')
  })

  it('renders user details when authStore has a user', () => {
    const authStore = useAuthStore()
    authStore.user = {
      id: 'u-1',
      name: 'Alex Developer',
      email: 'alex@example.com',
      preferredLocale: 'en',
      targetRole: 'Staff Infrastructure Architect'
    } as unknown as AuthUser

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    expect(wrapper.text()).toContain('Staff Infrastructure Architect')
  })

  it('renders active reading slice and scenario drill when focusStore has data', () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = {
      documentChunk: {
        id: 'chunk-1',
        chapterTitle: 'Distributed Consensus & Raft',
        summaryMarkdown: 'Deep dive into state machine replication and log entries.'
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
    } as unknown as TodayFocusResponse

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    expect(wrapper.text()).toContain('Distributed Consensus & Raft')
    expect(wrapper.text()).toContain('Distributed Systems Patterns')
    expect(wrapper.text()).toContain('7')
    expect(wrapper.text()).toContain('2/2')
  })

  it('renders node and edge counts directly in knowledge constellation telemetry', () => {
    const graphStore = useKnowledgeGraphStore()
    graphStore.rawData = {
      nodes: new Array(180).fill({ id: '1' }),
      edges: new Array(250).fill({ id: 'e1' }),
      stats: { totalNodes: 180, totalEdges: 250, nodeTypeCounts: {}, pillarCounts: {}, masteredCardsCount: 0 }
    } as unknown as NonNullable<typeof graphStore.rawData>

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    expect(wrapper.text()).toContain('180')
    expect(wrapper.text()).toContain('250')
  })

  it('renders calculated learnedChunksCount across availableBooks in knowledge telemetry', () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = {
      pacer: {
        bookId: 'book-1',
        bookTitle: 'System Design',
        chapterTitle: 'Microservices',
        currentChunkOrder: 5,
        totalChunks: 100,
        progressPercentage: 4,
        hasPrevious: true,
        hasNext: true,
        availableBooks: [
          {
            id: 'book-1',
            title: 'System Design',
            currentChunkOrder: 5,
            totalChunks: 100,
            progressPercentage: 4,
            isActive: true
          },
          {
            id: 'book-2',
            title: 'Complete Guide',
            currentChunkOrder: 50,
            totalChunks: 50,
            progressPercentage: 100,
            isActive: false
          }
        ]
      }
    } as unknown as TodayFocusResponse

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    // book-1 completed 4 slices (current 5); book-2 has 100% progress so 50 slices; total = 54
    expect(wrapper.text()).toContain('54')
  })

  it('binds Spaced Repetition deck statistics correctly in telemetry dock', () => {
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
          NuxtLink: true
        },
        mocks: {
          $t: (key: string) => key
        }
      }
    })

    expect(wrapper.text()).toContain('20/42 SM-2')
    expect(wrapper.text()).toContain('5')
  })
  it('renders dynamic slice progress badge and routes to gitbook reader on start reading', async () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = {
      documentChunk: { id: 'chunk-1', chapterTitle: 'ASP.NET Core Architecture', summaryMarkdown: 'Architecture summary' },
      pacer: { bookId: 'aspnet-doc', currentChunkOrder: 4, totalChunks: 23 },
      scenario: { title: 'Middleware Pipeline Debugging' }
    } as unknown as TodayFocusResponse
    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
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
      query: { slice: '4', from: '/' }
    })
  })

  it('routes to /today when start today practice is clicked', async () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = {
      documentChunk: { id: 'chunk-1', chapterTitle: 'ASP.NET Core Architecture', summaryMarkdown: 'Architecture summary' },
      scenario: { title: 'Middleware Pipeline Debugging' }
    } as unknown as TodayFocusResponse
    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          NuxtLink: true
        }
      }
    })

    const practiceBtn = wrapper.findAll('button').find(b => b.text().includes('dashboard.start_today_practice'))
    expect(practiceBtn).toBeDefined()
    await practiceBtn!.trigger('click')
    expect((globalThis as any).navigateTo).toHaveBeenCalledWith({
      path: '/today',
      query: { tab: 'challenge' }
    })
  })

  it('renders unified focus card with pending drill status badge', () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = {
      documentChunk: { id: 'chunk-1', chapterTitle: 'ASP.NET Core Architecture', summaryMarkdown: 'Architecture summary' },
    } as unknown as TodayFocusResponse

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          NuxtLink: true
        }
      }
    })

    expect(wrapper.text()).toContain('+10 dashboard.points_reward')
    expect(wrapper.text()).toContain('dashboard.start_today_practice')
  })

  it('renders unified focus card with completed drill status and review CTA', async () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = {
      documentChunk: { id: 'chunk-1', chapterTitle: 'Concurrency & Channels', summaryMarkdown: 'Channels summary' },
      drill: { status: 'Submitted', isCorrect: true, score: 10 }
    } as unknown as TodayFocusResponse

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          NuxtLink: true
        }
      }
    })

    expect(wrapper.text()).toContain('dashboard.status_completed')
    expect(wrapper.text()).toContain('dashboard.review_today_practice')

    const reviewBtn = wrapper.findAll('button').find(b => b.text().includes('dashboard.review_today_practice'))
    expect(reviewBtn).toBeDefined()
    await reviewBtn!.trigger('click')
    expect((globalThis as any).navigateTo).toHaveBeenCalledWith({
      path: '/today',
      query: { tab: 'challenge' }
    })
  })

  it('routes to /quiz when senior dilemma solve CTA is clicked', async () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = {
      documentChunk: { id: 'chunk-1', chapterTitle: 'Concurrency & Channels', summaryMarkdown: 'Channels summary' },
      scenario: { title: 'High-Concurrency Bottleneck', situation: 'Thread pool starvation under load' }
    } as unknown as TodayFocusResponse

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          NuxtLink: true
        }
      }
    })

    const dilemmaBtn = wrapper.findAll('button').find(b => b.text().includes('dashboard.solve_dilemma_cta'))
    expect(dilemmaBtn).toBeDefined()
    await dilemmaBtn!.trigger('click')

    expect((globalThis as any).navigateTo).toHaveBeenCalledWith('/quiz')
  })
  it('renders failed drill with needs review status badge when score is 0', () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = {
      documentChunk: { id: 'chunk-1', chapterTitle: 'Concurrency & Channels', summaryMarkdown: 'Channels summary' },
      drill: { status: 'Submitted', isCorrect: false, score: 0 }
    } as unknown as TodayFocusResponse

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          NuxtLink: true
        }
      }
    })

    expect(wrapper.text()).toContain('dashboard.status_needs_review: 0/10')
    expect(wrapper.text()).not.toContain('dashboard.status_completed')
  })

  it('renders Card A and Card B as distinct cards in action-stage without drill badges in Card A', () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = {
      documentChunk: { id: 'chunk-1', chapterTitle: 'Kafka Partitions', summaryMarkdown: 'Kafka summary' },
      pacer: { bookId: 'b-1', bookTitle: 'Kafka Internals', currentChunkOrder: 2, totalChunks: 10 },
      scenario: { title: 'Consumer Rebalance Storm', situation: 'High consumer lag scenario' },
      drill: { status: 'Submitted', isCorrect: true, score: 10 }
    } as unknown as TodayFocusResponse

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          NuxtLink: true
        }
      }
    })

    // Both action buttons exist
    const continueBtn = wrapper.findAll('button').find(b => b.text().includes('dashboard.continue_reading'))
    const practiceBtn = wrapper.findAll('button').find(b => b.text().includes('dashboard.review_today_practice'))
    expect(continueBtn).toBeDefined()
    expect(practiceBtn).toBeDefined()

    // Scenario title in Card B
    expect(wrapper.text()).toContain('Consumer Rebalance Storm')
    // Drill status in Card B
    expect(wrapper.text()).toContain('dashboard.status_completed: 10/10')
  })
  it('renders Tier 2 split practice subgrid with daily drill and senior dilemma cards', () => {
    const focusStore = useDailyFocusStore()
    focusStore.data = {
      scenario: { title: 'Async Replication Failover', situation: 'Leader election split brain' },
      drill: { status: 'Pending' }
    } as unknown as TodayFocusResponse
    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          NuxtLink: true
        }
      }
    })

    expect(wrapper.text()).toContain('dashboard.daily_drill_label')
    expect(wrapper.text()).toContain('dashboard.senior_dilemma_label')
    expect(wrapper.text()).toContain('Async Replication Failover')
    expect(wrapper.text()).toContain('dashboard.solve_dilemma_cta')
  })

  it('renders 2x2 telemetry grid with 4 metrics in Knowledge Radar card', () => {
    const graphStore = useKnowledgeGraphStore()
    graphStore.rawData = {
      nodes: [{ id: 'n1' }, { id: 'n2' }, { id: 'n3' }],
      edges: [{ id: 'e1' }, { id: 'e2' }]
    } as any

    const reviewStore = useReviewStore()
    reviewStore.deckStatistics = {
      totalCards: 20,
      masteredCount: 8
    } as any
    reviewStore.totalCardsDue = 5

    const wrapper = mount(HomeBentoDashboard, {
      global: {
        stubs: {
          NuxtLink: true
        }
      }
    })

    expect(wrapper.text()).toContain('dashboard.connected_nodes')
    expect(wrapper.text()).toContain('dashboard.active_relations')
    expect(wrapper.text()).toContain('dashboard.stat_cards_due')
    expect(wrapper.text()).toContain('dashboard.stat_mastered_cards')
    expect(wrapper.text()).toContain('3') // total nodes
    expect(wrapper.text()).toContain('2') // total edges
    expect(wrapper.text()).toContain('5') // due cards
    expect(wrapper.text()).toContain('8') // mastered cards
  })
})
