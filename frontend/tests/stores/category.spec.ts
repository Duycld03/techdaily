import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import type * as UseApiClientModule from '~/composables/useApiClient'
import { useCategoryStore, type CategoryDescriptor } from '~/stores/useCategoryStore'
import { useAuthStore } from '~/stores/useAuthStore'
import { adminTestJwt, clearAuthStorage, deferred } from '../helpers/admin'

const { get } = vi.hoisted(() => ({ get: vi.fn() }))
vi.mock('~/composables/useApiClient', async importOriginal => ({
  ...await importOriginal<typeof UseApiClientModule>(),
  useApiClient: () => ({ get })
}))

const sampleCategories: CategoryDescriptor[] = [
  {
    id: '10000000-0000-4000-8000-000000000001',
    slug: 'backend-runtime',
    nameEn: 'Backend & Runtime',
    nameVi: 'Backend & Runtime',
    sortOrder: 1,
    isActive: true,
    version: 1
  },
  {
    id: '10000000-0000-4000-8000-000000000000',
    slug: 'frontend-web',
    nameEn: 'Frontend & Web',
    nameVi: 'Frontend & Web',
    sortOrder: 0,
    isActive: true,
    version: 1
  },
  {
    id: '10000000-0000-4000-8000-000000000002',
    slug: 'database-storage',
    nameEn: 'Database & Storage',
    nameVi: 'Cơ Sở Dữ Liệu & Lưu Trữ',
    sortOrder: 2,
    isActive: false,
    version: 1
  },
  {
    id: '10000000-0000-4000-8000-000000000003',
    slug: 'system-design',
    nameEn: 'Distributed Systems',
    nameVi: 'Hệ Thống Phân Tán',
    sortOrder: 2,
    isActive: true,
    version: 1
  }
]

describe('useCategoryStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    clearAuthStorage()
    get.mockReset()
  })

  it('retries metadata failures and refreshes remote changes at the next consumer entry', async () => {
    const store = useCategoryStore()
    const novel = { ...sampleCategories[0]!, id: '33333333-3333-4000-8000-333333333333', nameEn: 'Novel subject' }
    get.mockRejectedValueOnce(new Error('Offline')).mockResolvedValueOnce([novel])
    await store.ensureCategories()
    expect(store.error).toBe('Offline')
    expect(store.hasLoaded).toBe(false)
    await store.ensureCategories()
    get.mockResolvedValueOnce([{ ...novel, nameVi: 'Chủ đề cập nhật', isActive: false, version: 2 }])
    await store.ensureCategories()
    expect(store.activeCategories).toEqual([])
    expect(store.getCategoryName(novel.id, 'vi')).toBe('Chủ đề cập nhật')
    expect(store.hasLoaded).toBe(true)
    store.reset()
    get.mockResolvedValueOnce([])
    await store.ensureCategories()
    expect(get).toHaveBeenCalledTimes(4)
    expect(store.activeCategories).toEqual([])
  })

  it('uses the latest bilingual metadata for existing inactive descriptors', () => {
    const store = useCategoryStore()
    const historical = { ...sampleCategories[2]!, nameEn: 'Old name', nameVi: 'Tên cũ' }
    store.categories = [{ ...historical, nameEn: 'Renamed topic', nameVi: 'Chủ đề đã đổi tên', version: 2 }]
    expect(store.getCategoryName(historical, 'en')).toBe('Renamed topic')
    expect(store.getCategoryName(historical, 'vi')).toBe('Chủ đề đã đổi tên')
    expect(store.activeCategories).toEqual([])
  })

  it('initializes with default empty state', () => {
    const store = useCategoryStore()
    expect(store.categories).toEqual([])
    expect(store.isLoading).toBe(false)
    expect(store.error).toBeNull()
    expect(store.allCategories).toEqual([])
    expect(store.activeCategories).toEqual([])
  })

  it('orders allCategories by sortOrder then slug without mutating original state', () => {
    const store = useCategoryStore()
    store.categories = [...sampleCategories]

    const sorted = store.allCategories
    expect(sorted.map(c => c.slug)).toEqual([
      'frontend-web',     // sortOrder 0
      'backend-runtime',  // sortOrder 1
      'database-storage', // sortOrder 2, slug 'database-storage'
      'system-design'     // sortOrder 2, slug 'system-design'
    ])

    // Verify original array is unchanged
    expect(store.categories[0].slug).toBe('backend-runtime')
  })

  it('filters activeCategories by isActive while maintaining sortOrder and slug ordering', () => {
    const store = useCategoryStore()
    store.categories = [...sampleCategories]

    const active = store.activeCategories
    expect(active.map(c => c.slug)).toEqual([
      'frontend-web',
      'backend-runtime',
      'system-design'
    ])
    expect(active.every(c => c.isActive)).toBe(true)
    expect(active.some(c => c.slug === 'database-storage')).toBe(false)
  })

  it('finds category by id and returns undefined for non-existent or empty id', () => {
    const store = useCategoryStore()
    store.categories = [...sampleCategories]

    expect(store.getCategoryById('10000000-0000-4000-8000-000000000000')?.slug).toBe('frontend-web')
    expect(store.getCategoryById('non-existent-id')).toBeUndefined()
    expect(store.getCategoryById('')).toBeUndefined()
    expect(store.getCategoryById(null)).toBeUndefined()
    expect(store.getCategoryById(undefined)).toBeUndefined()
  })


  it('resolves localized category name by descriptor or UUID in Vietnamese and English', () => {
    const store = useCategoryStore()
    store.categories = [...sampleCategories]

    const dbCategory = store.categories[2] // database-storage

    // By descriptor
    expect(store.getCategoryName(dbCategory, 'vi')).toBe('Cơ Sở Dữ Liệu & Lưu Trữ')
    expect(store.getCategoryName(dbCategory, 'en')).toBe('Database & Storage')

    // By id
    expect(store.getCategoryName('10000000-0000-4000-8000-000000000002', 'vi')).toBe('Cơ Sở Dữ Liệu & Lưu Trữ')
    expect(store.getCategoryName('10000000-0000-4000-8000-000000000002', 'en')).toBe('Database & Storage')

    // Slugs are metadata, not alternate assignment identifiers.
    expect(store.getCategoryName('database-storage', 'vi')).toBe('')
    expect(store.getCategoryName('database-storage', 'en')).toBe('')

    // Empty or missing fallbacks
    const fallbackCategory: CategoryDescriptor = {
      id: 'fallback-id',
      slug: 'fallback',
      nameEn: 'English Only',
      nameVi: '',
      sortOrder: 10,
      isActive: true,
      version: 1
    }
    expect(store.getCategoryName(fallbackCategory, 'vi')).toBe('English Only')

    const viOnlyCategory: CategoryDescriptor = {
      id: 'vi-id',
      slug: 'vi-only',
      nameEn: '',
      nameVi: 'Tiếng Việt Duy Nhất',
      sortOrder: 11,
      isActive: true,
      version: 1
    }
    expect(store.getCategoryName(viOnlyCategory, 'en')).toBe('Tiếng Việt Duy Nhất')

    // Invalid or unknown inputs
    expect(store.getCategoryName(null)).toBe('')
    expect(store.getCategoryName(undefined)).toBe('')
    expect(store.getCategoryName('')).toBe('')
    expect(store.getCategoryName('unknown-id')).toBe('')
  })

  it('defaults to current i18n locale when locale parameter is omitted', () => {
    const store = useCategoryStore()
    store.categories = [...sampleCategories]

    // Global mock in setup.ts has locale: 'en'
    expect(store.getCategoryName('10000000-0000-4000-8000-000000000002')).toBe('Database & Storage')
  })

  it('fetches categories from API with no-store cache and populates state', async () => {
    const store = useCategoryStore()
    get.mockResolvedValueOnce(sampleCategories)

    const fetchPromise = store.fetchCategories()
    expect(store.isLoading).toBe(true)

    const result = await fetchPromise
    expect(get).toHaveBeenCalledWith('/api/v1/categories', { cache: 'no-store' })
    expect(store.isLoading).toBe(false)
    expect(store.error).toBeNull()
    expect(result).toEqual(sampleCategories)
    expect(store.categories).toEqual(sampleCategories)
  })

  it('deduplicates concurrent fetch requests into a single in-flight call', async () => {
    const store = useCategoryStore()
    const pending = deferred<CategoryDescriptor[]>()
    get.mockReturnValue(pending.promise)

    const call1 = store.fetchCategories()
    const call2 = store.fetchCategories()

    expect(get).toHaveBeenCalledTimes(1)
    expect(store.isLoading).toBe(true)

    pending.resolve(sampleCategories)
    const [res1, res2] = await Promise.all([call1, call2])

    expect(res1).toEqual(sampleCategories)
    expect(res2).toEqual(sampleCategories)
    expect(store.isLoading).toBe(false)
  })

  it('bypasses in-flight promise when force=true', async () => {
    const store = useCategoryStore()
    const pending1 = deferred<CategoryDescriptor[]>()
    const pending2 = deferred<CategoryDescriptor[]>()
    get.mockReturnValueOnce(pending1.promise).mockReturnValueOnce(pending2.promise)

    const call1 = store.fetchCategories()
    expect(get).toHaveBeenCalledTimes(1)

    const call2 = store.fetchCategories(true)
    expect(get).toHaveBeenCalledTimes(2)

    pending2.resolve(sampleCategories)
    const res2 = await call2
    expect(res2).toEqual(sampleCategories)
    expect(store.categories).toEqual(sampleCategories)
  })

  it('preserves existing data and captures error message on failure', async () => {
    const store = useCategoryStore()
    store.categories = [...sampleCategories]

    get.mockRejectedValueOnce(new Error('Network connectivity lost'))
    await store.fetchCategories()

    expect(store.isLoading).toBe(false)
    expect(store.error).toBe('Network connectivity lost')
    expect(store.categories).toEqual(sampleCategories)
  })

  it('discards in-flight response when user session resets or logs out', async () => {
    const auth = useAuthStore()
    auth.setSession(adminTestJwt(), { id: 'admin-1', email: 'admin@example.test', name: 'Admin', preferredLocale: 'en' })

    const store = useCategoryStore()
    const pending = deferred<CategoryDescriptor[]>()
    get.mockReturnValue(pending.promise)

    const fetchPromise = store.fetchCategories()
    expect(store.isLoading).toBe(true)

    // User logs out while request is in flight
    auth.logout()
    expect(store.categories).toEqual([])
    expect(store.isLoading).toBe(false)

    // Late network arrival
    pending.resolve(sampleCategories)
    await fetchPromise

    // Store state must remain clean and not populated by late response
    expect(store.categories).toEqual([])
    expect(store.isLoading).toBe(false)
  })

  it('resets state completely on reset()', () => {
    const store = useCategoryStore()
    store.categories = [...sampleCategories]
    store.isLoading = true
    store.error = 'Some error'

    store.reset()

    expect(store.categories).toEqual([])
    expect(store.isLoading).toBe(false)
    expect(store.error).toBeNull()
  })
})
