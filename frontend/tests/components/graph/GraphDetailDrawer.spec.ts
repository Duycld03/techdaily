import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import GraphDetailDrawer from '~/components/graph/GraphDetailDrawer.vue'
import { useKnowledgeGraphStore, type GraphNode } from '~/stores/useKnowledgeGraphStore'

const mockTopicNode: GraphNode = {
  id: 'topic_1',
  label: 'PostgreSQL MVCC & Vacuum',
  type: 'topic',
  category: 'DatabaseStorage',
  subtitle: 'Day 3',
  dayOrder: 3,
  summary: 'Multi-version concurrency control mechanics and autovacuum tuning',
  difficulty: 'Advanced',
  tags: ['database', 'postgres', 'concurrency']
}

const mockBookNode: GraphNode = {
  id: 'book_1',
  label: 'Designing Data-Intensive Applications',
  type: 'book',
  category: 'DatabaseStorage',
  subtitle: 'Martin Kleppmann • 12 Chapters',
  summary: 'Comprehensive guide to reliable, scalable, and maintainable systems.',
  bookId: 'book_1'
}

const mockCardNode: GraphNode = {
  id: 'card_1',
  label: 'What causes PostgreSQL table bloat?',
  type: 'card',
  category: 'DatabaseStorage',
  status: 'Mastered',
  intervalDays: 25,
  easeFactor: 2.6,
  repetitionCount: 4,
  tags: ['postgres', 'vacuum']
}

const mockHighlightNode: GraphNode = {
  id: 'hl_1',
  label: 'MVCC creates a new tuple version on every update statement in PostgreSQL.',
  type: 'highlight',
  category: 'DatabaseStorage',
  summary: 'Crucial insight into why frequent updates lead to table and index bloat.',
  bookId: 'book_1',
  documentChunkId: 'chunk_7',
  tags: ['mvcc', 'concurrency', 'postgres']
}

const mockPillarNode: GraphNode = {
  id: 'pillar-BackendDotNet',
  label: 'Backend (.NET)',
  type: 'pillar',
  category: 'BackendDotNet',
  subtitle: '.NET Runtime, CLR, Concurrency & Async I/O',
  summary: 'Core architectural pillar for .NET backend development and high-throughput services.'
}

const NuxtLinkStub = {
  name: 'NuxtLink',
  props: ['to'],
  template: '<a :href="to" class="mock-nuxt-link"><slot /></a>'
}

describe('GraphDetailDrawer.vue', () => {
  function createTestStore() {
    setActivePinia(createPinia())
    const store = useKnowledgeGraphStore()
    store.rawData = {
      nodes: [mockTopicNode, mockBookNode, mockCardNode, mockHighlightNode, mockPillarNode],
      edges: [
        { id: 'edge_pillar_topic', source: 'topic_1', target: 'pillar-BackendDotNet', relationType: 'TopicToPillar' },
        { id: 'edge_pillar_book', source: 'book_1', target: 'pillar-BackendDotNet', relationType: 'BookToPillar' }
      ],
      stats: {
        totalNodes: 4,
        totalEdges: 0,
        nodeTypeCounts: {},
        pillarCounts: {},
        masteredCardsCount: 1
      }
    }
    return store
  }

  it('renders nothing when no node is selected', () => {
    const store = createTestStore()
    store.selectNode(null)
    const wrapper = mount(GraphDetailDrawer, {
      global: {
        stubs: { NuxtLink: NuxtLinkStub }
      }
    })

    expect(wrapper.find('aside').exists()).toBe(false)
  })

  it('renders topic node metadata and 1-click action bridges with whitespace-nowrap shrink-0', () => {
    const store = createTestStore()
    store.selectNode('topic_1')
    const wrapper = mount(GraphDetailDrawer, {
      global: {
        stubs: { NuxtLink: NuxtLinkStub }
      }
    })

    expect(wrapper.find('aside').exists()).toBe(true)
    expect(wrapper.text()).toContain('PostgreSQL MVCC & Vacuum')
    expect(wrapper.text()).toContain('DatabaseStorage')
    expect(wrapper.text()).toContain('Advanced')
    expect(wrapper.text()).toContain('Curriculum Day 3')
    expect(wrapper.text()).toContain('Multi-version concurrency control mechanics and autovacuum tuning')

    // Verify action bridge buttons
    const links = wrapper.findAllComponents(NuxtLinkStub)
    expect(links.length).toBeGreaterThanOrEqual(2)

    // Practice quiz link
    const quizLink = links.find((l) => l.props('to')?.includes('/quiz?topic='))
    expect(quizLink).toBeDefined()
    expect(quizLink?.props('to')).toContain('/quiz?topic=postgresql-mvcc-vacuum')
    expect(quizLink?.classes()).toContain('whitespace-nowrap')
    expect(quizLink?.classes()).toContain('shrink-0')

    // View roadmap link
    const roadmapLink = links.find((l) => l.props('to')?.includes('/roadmap#3'))
    expect(roadmapLink).toBeDefined()
    expect(roadmapLink?.classes()).toContain('whitespace-nowrap')
    expect(roadmapLink?.classes()).toContain('shrink-0')

    // Verify badges have whitespace-nowrap shrink-0
    const badges = wrapper.findAll('span.whitespace-nowrap.shrink-0')
    expect(badges.length).toBeGreaterThan(0)
  })

  it('renders book node metadata and navigation links with whitespace-nowrap shrink-0', () => {
    const store = createTestStore()
    store.selectNode('book_1')
    const wrapper = mount(GraphDetailDrawer, {
      global: {
        stubs: { NuxtLink: NuxtLinkStub }
      }
    })

    expect(wrapper.text()).toContain('Designing Data-Intensive Applications')
    expect(wrapper.text()).toContain('Martin Kleppmann • 12 Chapters')
    expect(wrapper.text()).toContain('Comprehensive guide to reliable, scalable, and maintainable systems.')

    const links = wrapper.findAllComponents(NuxtLinkStub)

    // Browse library link
    const libLink = links.find((l) => l.props('to') === '/library')
    expect(libLink).toBeDefined()
    expect(libLink?.classes()).toContain('whitespace-nowrap')
    expect(libLink?.classes()).toContain('shrink-0')

    // Read slices link
    const readLink = links.find((l) => l.props('to') === '/read/book_1')
    expect(readLink).toBeDefined()
    expect(readLink?.classes()).toContain('whitespace-nowrap')
    expect(readLink?.classes()).toContain('shrink-0')
  })

  it('renders card node SM-2 metrics, mastery status badge, and review action button', () => {
    const store = createTestStore()
    store.selectNode('card_1')
    const wrapper = mount(GraphDetailDrawer, {
      global: {
        stubs: { NuxtLink: NuxtLinkStub }
      }
    })

    expect(wrapper.text()).toContain('What causes PostgreSQL table bloat?')
    expect(wrapper.text()).toContain('25 days')
    expect(wrapper.text()).toContain('2.60')
    expect(wrapper.text()).toContain('4 reviews')
    expect(wrapper.text()).toContain('Mastered')

    // Mastery status badge has whitespace-nowrap shrink-0
    const statusBadge = wrapper
      .findAll('span')
      .find((el) => el.text() === 'Mastered' && el.classes().includes('whitespace-nowrap') && el.classes().includes('shrink-0'))
    expect(statusBadge).toBeDefined()

    // Review flashcard action bridge
    const links = wrapper.findAllComponents(NuxtLinkStub)
    const reviewLink = links.find((l) => l.props('to') === '/review?cardId=card_1')
    expect(reviewLink).toBeDefined()
    expect(reviewLink?.classes()).toContain('whitespace-nowrap')
    expect(reviewLink?.classes()).toContain('shrink-0')
  })

  it('renders highlight node quote, note, tags, and action bridge buttons', () => {
    const store = createTestStore()
    store.selectNode('hl_1')
    const wrapper = mount(GraphDetailDrawer, {
      global: {
        stubs: { NuxtLink: NuxtLinkStub }
      }
    })

    expect(wrapper.text()).toContain('MVCC creates a new tuple version on every update statement in PostgreSQL.')
    expect(wrapper.text()).toContain('Crucial insight into why frequent updates lead to table and index bloat.')

    // Blockquote
    const quoteEl = wrapper.find('blockquote')
    expect(quoteEl.exists()).toBe(true)
    expect(quoteEl.text()).toContain('MVCC creates a new tuple version')

    // Tags
    expect(wrapper.text()).toContain('#mvcc')
    expect(wrapper.text()).toContain('#concurrency')
    expect(wrapper.text()).toContain('#postgres')

    // Read chapter and view notes action links
    const links = wrapper.findAllComponents(NuxtLinkStub)
    const readChapterLink = links.find((l) => l.props('to')?.includes('/read/book_1#slice-chunk_7'))
    expect(readChapterLink).toBeDefined()
    expect(readChapterLink?.classes()).toContain('whitespace-nowrap')
    expect(readChapterLink?.classes()).toContain('shrink-0')

    const viewNotesLink = links.find((l) => l.props('to')?.includes('/notes?highlightId=hl_1'))
    expect(viewNotesLink).toBeDefined()
    expect(viewNotesLink?.classes()).toContain('whitespace-nowrap')
    expect(viewNotesLink?.classes()).toContain('shrink-0')
  })

  it('closes the drawer when close button is clicked', async () => {
    const store = createTestStore()
    store.selectNode('topic_1')
    const wrapper = mount(GraphDetailDrawer, {
      global: {
        stubs: { NuxtLink: NuxtLinkStub }
      }
    })

    expect(store.selectedNodeId).toBe('topic_1')
    const closeBtn = wrapper.find('button[aria-label="Close detail drawer"]')
    expect(closeBtn.exists()).toBe(true)

    await closeBtn.trigger('click')
    expect(store.selectedNodeId).toBeNull()
  })

  it('renders pillar node metadata, domain summary, metrics, and filter action with whitespace-nowrap shrink-0', async () => {
    const store = createTestStore()
    store.selectNode('pillar-BackendDotNet')
    const wrapper = mount(GraphDetailDrawer, {
      global: {
        stubs: { NuxtLink: NuxtLinkStub }
      }
    })

    expect(wrapper.find('aside').exists()).toBe(true)
    // Title, subtitle, and summary
    expect(wrapper.text()).toContain('Backend (.NET)')
    expect(wrapper.text()).toContain('.NET Runtime, CLR, Concurrency & Async I/O')
    expect(wrapper.text()).toContain('Core architectural pillar for .NET backend development and high-throughput services.')
    // Category badge
    expect(wrapper.text()).toContain('BackendDotNet')

    // Connected metrics
    const topicsCount = wrapper.find('[data-test="pillar-topics-count"]')
    expect(topicsCount.exists()).toBe(true)
    expect(topicsCount.text()).toBe('1')

    const booksCount = wrapper.find('[data-test="pillar-books-count"]')
    expect(booksCount.exists()).toBe(true)
    expect(booksCount.text()).toBe('1')

    // Filter action button
    const filterBtn = wrapper.find('[data-test="filter-to-pillar"]')
    expect(filterBtn.exists()).toBe(true)
    expect(filterBtn.classes()).toContain('whitespace-nowrap')
    expect(filterBtn.classes()).toContain('shrink-0')

    // Clicking the filter button sets category in store and closes drawer
    const setCategorySpy = vi.spyOn(store, 'setCategory')
    await filterBtn.trigger('click')
    expect(setCategorySpy).toHaveBeenCalledWith('BackendDotNet')
    expect(store.selectedNodeId).toBeNull()
  })
})
