import { defineStore } from 'pinia'
import { computed, shallowRef, unref, watch } from 'vue'
import { useAuthStore } from '~/stores/useAuthStore'
import { useApiClient } from '~/composables/useApiClient'

export interface CategoryDescriptor {
  id: string
  slug: string
  nameEn: string
  nameVi: string
  sortOrder: number
  isActive: boolean
  version: number
}

export interface AdminCategoryDetail extends CategoryDescriptor {
  usageCount?: number
  canDelete: boolean
}

export interface CreateCategoryPayload {
  slug: string
  nameEn: string
  nameVi: string
  sortOrder: number
  isActive: boolean
}

export interface UpdateCategoryPayload {
  nameEn: string
  nameVi: string
  sortOrder: number
  isActive: boolean
  expectedVersion: number
}


function resolveCurrentLocale(): string {
  try {
    if (typeof useI18n === 'function') {
      const i18n = useI18n()
      const loc = unref(i18n.locale)
      if (typeof loc === 'string') return loc
    }
  } catch {
    // Fallback if outside Nuxt/i18n context
  }
  return 'en'
}

export const useCategoryStore = defineStore('category', () => {
  const auth = useAuthStore()
  const api = useApiClient()

  const categories = shallowRef<CategoryDescriptor[]>([])
  const isLoading = shallowRef(false)
  const error = shallowRef<string | null>(null)
  const hasLoaded = shallowRef(false)

  let flight: Promise<CategoryDescriptor[]> | null = null
  let generation = 0

  const allCategories = computed<CategoryDescriptor[]>(() => {
    return [...categories.value].sort((a, b) =>
      a.sortOrder !== b.sortOrder ? a.sortOrder - b.sortOrder : a.slug.localeCompare(b.slug)
    )
  })

  const activeCategories = computed<CategoryDescriptor[]>(() => {
    return allCategories.value.filter(c => c.isActive)
  })

  function reset() {
    generation++
    flight = null
    categories.value = []
    isLoading.value = false
    error.value = null
    hasLoaded.value = false
  }

  watch(
    [() => auth.user?.id, () => auth.sessionGeneration, () => auth.adminGeneration],
    reset,
    { flush: 'sync' }
  )

  async function fetchCategories(force = false): Promise<CategoryDescriptor[]> {
    auth.init()
    if (flight && !force) return flight

    const currentGen = ++generation
    isLoading.value = true
    error.value = null

    const request = (async () => {
      try {
        const res = await api.get<CategoryDescriptor[]>('/api/v1/categories', {
          cache: 'no-store'
        })
        if (currentGen !== generation) return categories.value
        categories.value = Array.isArray(res) ? res : []
        hasLoaded.value = true
        return categories.value
      } catch (err: unknown) {
        if (currentGen !== generation) return categories.value
        error.value = err instanceof Error ? err.message : 'Failed to fetch categories'
        return categories.value
      } finally {
        if (currentGen === generation) {
          isLoading.value = false
          flight = null
        }
      }
    })()

    flight = request
    return request
  }

  function ensureCategories(): Promise<CategoryDescriptor[]> {
    return fetchCategories()
  }

  function getCategoryById(id: string | null | undefined): CategoryDescriptor | undefined {
    if (!id) return undefined
    return categories.value.find(c => c.id === id)
  }

  function getCategoryName(
    categoryOrId: CategoryDescriptor | string | null | undefined,
    locale?: string
  ): string {
    if (!categoryOrId) return ''
    const category = typeof categoryOrId === 'string'
      ? getCategoryById(categoryOrId)
      : getCategoryById(categoryOrId.id) ?? categoryOrId
    if (!category) return ''

    const targetLocale = (locale || resolveCurrentLocale()).toLowerCase()
    if (targetLocale === 'vi' || targetLocale.startsWith('vi')) {
      return category.nameVi || category.nameEn || ''
    }
    return category.nameEn || category.nameVi || ''
  }
  async function createCategory(payload: CreateCategoryPayload): Promise<CategoryDescriptor> {
    const created = await api.post<CategoryDescriptor>('/api/v1/admin/categories', payload)
    await fetchCategories(true)
    return created
  }

  async function updateCategory(id: string, payload: UpdateCategoryPayload): Promise<CategoryDescriptor> {
    const updated = await api.patch<CategoryDescriptor>(`/api/v1/admin/categories/${id}`, payload)
    await fetchCategories(true)
    return updated
  }

  async function deleteCategory(id: string, expectedVersion: number): Promise<void> {
    await api.delete(`/api/v1/admin/categories/${id}?expectedVersion=${expectedVersion}`)
    await fetchCategories(true)
  }

  async function fetchCategoryDetail(id: string): Promise<AdminCategoryDetail> {
    return await api.get<AdminCategoryDetail>(`/api/v1/admin/categories/${id}`, { cache: 'no-store' })
  }

  return {
    categories,
    isLoading,
    error,
    hasLoaded,
    ensureCategories,
    activeCategories,
    allCategories,
    fetchCategories,
    getCategoryById,
    getCategoryName,
    createCategory,
    updateCategory,
    deleteCategory,
    fetchCategoryDetail,
    reset
  }
})
