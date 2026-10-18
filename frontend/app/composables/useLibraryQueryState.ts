import { computed, shallowRef, watch } from 'vue'
import type { LocationQuery, LocationQueryRaw } from 'vue-router'
import type { components } from '~/types/api.generated'

type LibraryTab = 'mine' | 'discover'
type SourceType = components['schemas']['SourceType']

export interface LibraryListQuery {
  tab: LibraryTab
  search: string
  categoryId: string | null
  language: 'en' | 'vi' | null
  sourceType: SourceType | null
  page: number
}

const controlledKeys = ['tab', 'search', 'categoryId', 'category', 'language', 'sourceType', 'page'] as const
const categoryIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

function queryString(value: LocationQuery[string] | undefined): string {
  return typeof value === 'string' ? value : ''
}

function parseQuery(raw: LocationQuery): LibraryListQuery {
  const tab = raw.tab === 'discover' ? 'discover' : 'mine'
  const categoryId = queryString(raw.categoryId).trim()
  const language = queryString(raw.language).trim().toLowerCase()
  const sourceType = queryString(raw.sourceType)
  const page = Number(queryString(raw.page))
  return {
    tab,
    search: queryString(raw.search).trim(),
    categoryId: categoryIdPattern.test(categoryId) ? categoryId : null,
    language: language === 'en' || language === 'vi' ? language : null,
    sourceType: tab === 'discover' && (sourceType === 'PdfBook' || sourceType === 'MarkdownSeries' || sourceType === 'WebDocUrl') ? sourceType : null,
    page: Number.isSafeInteger(page) && page > 0 ? page : 1
  }
}

export function useLibraryQueryState() {
  const route = useRoute()
  const router = useRouter()
  const query = computed(() => parseQuery(route.query))
  const searchDraft = shallowRef(query.value.search)

  watch(() => query.value.search, search => { searchDraft.value = search }, { flush: 'sync' })

  function locationFor(next: LibraryListQuery) {
    const serialized: LocationQueryRaw = {
      ...route.query,
      tab: next.tab === 'discover' ? 'discover' : undefined,
      search: next.search || undefined,
      category: undefined,
      categoryId: next.categoryId || undefined,
      language: next.language || undefined,
      sourceType: next.tab === 'discover' ? next.sourceType || undefined : undefined,
      page: next.page > 1 ? String(next.page) : undefined
    }
    return { query: serialized, hash: route.hash }
  }

  function commit(changes: Partial<LibraryListQuery>) {
    return router.push(locationFor({ ...query.value, ...changes }))
  }

  const currentTab = computed<LibraryTab>({
    get: () => query.value.tab,
    set: tab => { void commit({ tab, page: 1 }) }
  })

  function commitSearch() {
    searchDraft.value = searchDraft.value.trim()
    return commit({ search: searchDraft.value, page: 1 })
  }

  function clearSearch() {
    searchDraft.value = ''
    return commit({ search: '', page: 1 })
  }

  function selectCategory(categoryId?: string) {
    return commit({ categoryId: categoryId || null, page: 1 })
  }

  function applyFilters(filters: { categoryId?: string; language?: string; sourceType?: SourceType }) {
    return commit({
      categoryId: filters.categoryId || null,
      language: filters.language === 'en' || filters.language === 'vi' ? filters.language : null,
      sourceType: filters.sourceType || null,
      page: 1
    })
  }

  function resetFilters() {
    searchDraft.value = ''
    return commit({ search: '', categoryId: null, language: null, sourceType: null, page: 1 })
  }

  function changePage(page: number) {
    return commit({ page })
  }

  async function canonicalize() {
    const location = locationFor(query.value)
    if (controlledKeys.every(key => route.query[key] === location.query[key])) return false
    await router.replace(location)
    return true
  }

  return { query, searchDraft, currentTab, commitSearch, clearSearch, selectCategory, applyFilters, resetFilters, changePage, canonicalize }
}
