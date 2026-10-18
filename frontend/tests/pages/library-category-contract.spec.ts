import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { mount, flushPromises, enableAutoUnmount } from '@vue/test-utils'
import { createMemoryHistory, createRouter, useRoute as useVueRoute, type Router } from 'vue-router'
import LibraryPage from '~/pages/library.vue'
import LibraryFilterModal from '~/components/library/LibraryFilterModal.vue'
import CatalogFilterModal from '~/components/catalog/CatalogFilterModal.vue'
import BasePagination from '~/components/common/BasePagination.vue'

enableAutoUnmount(afterEach)

const { get, post } = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn() }))
vi.mock('~/composables/useApiClient', () => ({ useApiClient: () => ({ get, post }) }))
const id = '33333333-3333-4000-8000-333333333333'
const descriptor = { id, slug: 'novel-subject', nameEn: 'Novel subject', nameVi: 'Chủ đề mới', isActive: true, sortOrder: 1, version: 1 }
let router: Router
const render = () => mount(LibraryPage, { global: { plugins: [router], stubs: { NuxtLink: true, Teleport: true, BasePagination: true } } })

function lastListQuery(tab: 'mine' | 'discover') {
  const path = tab === 'mine' ? '/library/books?' : '/catalog/documents?'
  const request = get.mock.calls.filter(([url]) => String(url).includes(path)).at(-1)?.[0]
  expect(request).toBeDefined()
  return new URL(String(request), 'https://example.test').searchParams
}

describe('library UUID category contract', () => {
  afterEach(() => vi.unstubAllGlobals())
  beforeEach(async () => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/library', component: { template: '<div />' } }] })
    vi.stubGlobal('useRoute', useVueRoute)
    vi.stubGlobal('useRouter', () => router)
    await router.push({ path: '/library', query: { categoryId: id, search: 'kept' } })
    get.mockImplementation(async (url: string) => url === '/api/v1/categories'
      ? [descriptor]
      : {
          books: [{ id: 'book-1', title: 'Concurrency handbook', slug: 'concurrency', sourceType: 0, categoryId: id, category: descriptor, totalChunks: 1, isPublished: true, status: 'Ready', createdAt: '2026-10-01T00:00:00Z', language: 'vi' }],
          items: [{ id: 'catalog-1', title: 'Concurrency handbook', description: 'Concurrency reference', categoryId: id, category: descriptor, language: 'vi', sourceType: 'PdfBook', attributionText: 'Reference author', totalSlices: 1, estimatedReadMinutes: 5, publishedAt: '2026-10-01T00:00:00Z', authorOrSourceUrl: null, existingBookId: null }],
          totalCount: 24,
          page: Number(new URL(url, 'https://example.test').searchParams.get('page') ?? 1),
          pageSize: 12,
          totalPages: 2
        })
  })

  it('loads novel metadata on a fresh visit and restores a UUID route filter', async () => {
    const wrapper = render()
    await flushPromises()
    expect(get).toHaveBeenCalledWith('/api/v1/categories', { cache: 'no-store' })
    expect(wrapper.text()).toContain('Novel subject')
    const request = get.mock.calls.find(([url]) => String(url).includes('/library/books'))![0]
    const query = new URL(request, 'https://example.com').searchParams
    expect(query.get('categoryId')).toBe(id)
    expect(query.has('category')).toBe(false)
    wrapper.findComponent(LibraryFilterModal).vm.$emit('apply', { categoryId: id, language: 'vi' })
    await flushPromises()
    expect(router.currentRoute.value.query).toMatchObject({ categoryId: id, search: 'kept', language: 'vi' })
    expect(lastListQuery('mine').get('language')).toBe('vi')
    wrapper.unmount()
  })

  describe.each(['mine', 'discover'] as const)('%s committed query state', tab => {
    it('restores every supported deep-link criterion in the input and request', async () => {
      await router.push({ path: '/library', query: { tab, search: 'consensus', categoryId: id, language: 'vi', sourceType: tab === 'discover' ? 'PdfBook' : undefined, page: '2' } })
      const wrapper = render()
      await flushPromises()
      expect((wrapper.get('input[type="text"]').element as HTMLInputElement).value).toBe('consensus')
      const query = lastListQuery(tab)
      expect(query.get('search')).toBe('consensus')
      expect(query.get('categoryId')).toBe(id)
      expect(query.get('language')).toBe('vi')
      expect(query.get('page')).toBe('2')
      expect(query.get('sourceType')).toBe(tab === 'discover' ? 'PdfBook' : null)
      wrapper.unmount()
    })

    it('commits then clears search without losing other URL criteria or history', async () => {
      await router.push({ path: '/library', query: { tab, categoryId: id, language: 'vi', marker: 'retained' }, hash: '#results' })
      const wrapper = render()
      await flushPromises()
      const input = wrapper.get('input[type="text"]')
      await input.setValue('  consensus  ')
      await input.trigger('keyup.enter')
      await flushPromises()
      expect(router.currentRoute.value.query.search).toBe('consensus')
      expect(lastListQuery(tab).get('search')).toBe('consensus')
      input.element.parentElement!.querySelector('button')!.click()
      await flushPromises()
      expect(router.currentRoute.value.query.search).toBeUndefined()
      expect(router.currentRoute.value.query.marker).toBe('retained')
      expect(router.currentRoute.value.hash).toBe('#results')
      expect((wrapper.get('input[type="text"]').element as HTMLInputElement).value).toBe('')
      expect(lastListQuery(tab).has('search')).toBe(false)
      expect(lastListQuery(tab).get('categoryId')).toBe(id)
      expect(lastListQuery(tab).get('language')).toBe('vi')
      const navigation = new Promise<void>(resolve => {
        const stop = router.afterEach(() => { stop(); resolve() })
      })
      router.back()
      await navigation
      await flushPromises()
      expect(router.currentRoute.value.query.search).toBe('consensus')
      expect((wrapper.get('input[type="text"]').element as HTMLInputElement).value).toBe('consensus')
      expect(lastListQuery(tab).get('search')).toBe('consensus')
      wrapper.unmount()
    })

    it('distinguishes category-only All from a complete reset', async () => {
      await router.push({ path: '/library', query: { tab, search: 'kept', categoryId: id, language: 'vi', sourceType: tab === 'discover' ? 'PdfBook' : undefined, page: '2' } })
      const wrapper = render()
      await flushPromises()
      await wrapper.findAll('button').find(button => button.text() === 'library.categories.all')!.trigger('click')
      await flushPromises()
      expect(lastListQuery(tab).has('categoryId')).toBe(false)
      expect(lastListQuery(tab).get('search')).toBe('kept')
      expect(lastListQuery(tab).get('language')).toBe('vi')
      wrapper.findComponent(tab === 'mine' ? LibraryFilterModal : CatalogFilterModal).vm.$emit('reset')
      await flushPromises()
      for (const field of ['search', 'categoryId', 'language', 'sourceType']) {
        expect(router.currentRoute.value.query[field]).toBeUndefined()
        expect(lastListQuery(tab).has(field)).toBe(false)
      }
      expect(lastListQuery(tab).get('page')).toBe('1')
      wrapper.unmount()
    })

    it('restores the selected page through Back and a query-driven remount', async () => {
      await router.push({ path: '/library', query: { tab, search: 'kept', categoryId: id } })
      let wrapper = render()
      await flushPromises()
      wrapper.findComponent(BasePagination).vm.$emit('change', 2)
      await flushPromises()
      expect(router.currentRoute.value.query.page).toBe('2')
      expect(lastListQuery(tab).get('page')).toBe('2')
      wrapper.unmount()
      wrapper = render()
      await flushPromises()
      expect((wrapper.get('input[type="text"]').element as HTMLInputElement).value).toBe('kept')
      expect(lastListQuery(tab).get('page')).toBe('2')
      const navigation = new Promise<void>(resolve => {
        const stop = router.afterEach(() => { stop(); resolve() })
      })
      router.back()
      await navigation
      await flushPromises()
      expect(lastListQuery(tab).get('page')).toBe('1')
      expect(lastListQuery(tab).get('search')).toBe('kept')
      wrapper.unmount()
    })
  })

  it('retains common criteria on tab switch and removes discovery-only source type', async () => {
    await router.push({ path: '/library', query: { tab: 'discover', search: 'kept', categoryId: id, language: 'vi', sourceType: 'PdfBook', page: '2' } })
    const wrapper = render()
    await flushPromises()
    await wrapper.findAll('button').find(button => button.text() === 'library.tabs.mine')!.trigger('click')
    await flushPromises()
    expect(router.currentRoute.value.query).toMatchObject({ search: 'kept', categoryId: id, language: 'vi' })
    expect(router.currentRoute.value.query.sourceType).toBeUndefined()
    expect(router.currentRoute.value.query.page).toBeUndefined()
    expect(lastListQuery('mine').get('language')).toBe('vi')
    expect(lastListQuery('mine').has('sourceType')).toBe(false)
    await wrapper.findAll('button').find(button => button.text() === 'library.tabs.discover')!.trigger('click')
    await flushPromises()
    expect(lastListQuery('discover').get('search')).toBe('kept')
    expect(lastListQuery('discover').get('language')).toBe('vi')
    expect(lastListQuery('discover').has('sourceType')).toBe(false)
    wrapper.unmount()
  })

  it.each(['empty', 'error'])('blocks assignment with %s metadata without inventing options', async state => {
    get.mockImplementation(async (url: string) => {
      if (url !== '/api/v1/categories') return { books: [] }
      if (state === 'error') throw new Error('Offline')
      return []
    })
    const wrapper = render()
    await flushPromises()
    await wrapper.find('button.bg-brand-600').trigger('click')
    await flushPromises()
    const submit = wrapper.findAll('button').find(button => button.text().includes('library.import_action'))
    expect(submit?.attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain(state === 'error' ? 'categories.metadataUnavailable' : 'categories.noActiveCategories')
    expect(post).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('invalidates pending list request when navigating before delayed categories resolve and commits active query', async () => {
    const { promise: promiseA, resolve: resolveA } = Promise.withResolvers<unknown>()
    const { promise: categoriesPromiseB, resolve: resolveCategoriesB } = Promise.withResolvers<unknown>()

    let routePhase: 'A' | 'B' = 'A'
    get.mockImplementation(async (url: string) => {
      if (url === '/api/v1/categories') {
        return routePhase === 'A' ? [descriptor] : categoriesPromiseB
      }
      if (String(url).includes('search=queryA')) {
        return promiseA
      }
      if (String(url).includes('search=queryB')) {
        return {
          books: [{ id: 'book-B', title: 'Result B book', slug: 'result-b', sourceType: 0, categoryId: id, category: descriptor, totalChunks: 1, isPublished: true, status: 'Ready', createdAt: '2026-10-01T00:00:00Z', language: 'en' }],
          totalCount: 1,
          page: 1,
          pageSize: 12
        }
      }
      return { books: [], totalCount: 0, page: 1, pageSize: 12 }
    })

    await router.push({ path: '/library', query: { tab: 'mine', search: 'queryA' } })
    const wrapper = render()
    await flushPromises()

    routePhase = 'B'
    await router.push({ path: '/library', query: { tab: 'mine', search: 'queryB' } })

    resolveA({
      books: [{ id: 'book-A', title: 'Result A stale', slug: 'result-a', sourceType: 0, categoryId: id, category: descriptor, totalChunks: 1, isPublished: true, status: 'Ready', createdAt: '2026-10-01T00:00:00Z', language: 'en' }],
      totalCount: 1,
      page: 1,
      pageSize: 12
    })
    await flushPromises()

    expect(wrapper.text()).not.toContain('Result A stale')

    resolveCategoriesB([descriptor])
    await flushPromises()

    expect(wrapper.text()).toContain('Result B book')
    expect(wrapper.text()).not.toContain('Result A stale')
    wrapper.unmount()
  })
})
