import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { mount, flushPromises } from '@vue/test-utils'
import LibraryPage from '~/pages/library.vue'
import LibraryFilterModal from '~/components/library/LibraryFilterModal.vue'

const { get, post, replace } = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn(), replace: vi.fn() }))
vi.mock('~/composables/useApiClient', () => ({ useApiClient: () => ({ get, post }) }))
const id = '33333333-3333-4000-8000-333333333333'
const descriptor = { id, slug: 'novel-subject', nameEn: 'Novel subject', nameVi: 'Chủ đề mới', isActive: true, sortOrder: 1, version: 1 }
const render = () => mount(LibraryPage, { global: { stubs: { NuxtLink: true, Teleport: true } } })

describe('library UUID category contract', () => {
  afterEach(() => vi.unstubAllGlobals())
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    vi.stubGlobal('useRoute', () => ({ query: { categoryId: id, search: 'kept' } }))
    vi.stubGlobal('useRouter', () => ({ replace, push: vi.fn() }))
    get.mockImplementation(async (url: string) => url === '/api/v1/categories' ? [descriptor] : { books: [] })
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
    expect(replace).toHaveBeenCalledWith({ query: expect.objectContaining({ categoryId: id, search: 'kept', language: 'vi', category: undefined }) })
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
})
