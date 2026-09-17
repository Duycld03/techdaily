<script setup lang="ts">
import { computed } from 'vue'
import { ChevronLeft, ChevronRight } from 'lucide-vue-next'

export interface BasePaginationProps {
  currentPage: number
  totalPages: number
  totalCount?: number
  pageSize?: number
  showSummary?: boolean
  showItemSummary?: boolean
  disabled?: boolean
}

const props = withDefaults(defineProps<BasePaginationProps>(), {
  currentPage: 1,
  totalPages: 1,
  pageSize: 10,
  showSummary: false,
  showItemSummary: false,
  disabled: false
})

const emit = defineEmits<{
  (e: 'update:currentPage', page: number): void
  (e: 'change', page: number): void
}>()

const hasSummary = computed(() => Boolean(props.showSummary || props.showItemSummary))

const shouldRender = computed(() => props.totalPages > 1 || hasSummary.value)

let t: (key: string, params?: Record<string, any>) => string

try {
  const i18n = useI18n()
  t = i18n.t
} catch {
  t = (key: string, params?: Record<string, any>) => {
    if (params) {
      let res = key
      for (const [k, v] of Object.entries(params)) {
        res = res.replace(new RegExp(`{${k}}`, 'g'), String(v))
      }
      return res
    }
    return key
  }
}

function translate(key: string, params?: Record<string, any>, fallback?: string): string {
  try {
    const val = t(key, params)
    if (val && !val.startsWith('common.pagination.')) {
      return val
    }
  } catch {
    // fallback to provided default
  }
  return fallback || key
}

const paginationLabel = computed(() =>
  translate('common.pagination.navigation_label', undefined, 'Pagination Navigation')
)

const prevLabel = computed(() =>
  translate('common.pagination.previous', undefined, 'Previous')
)

const nextLabel = computed(() =>
  translate('common.pagination.next', undefined, 'Next')
)

const ellipsisLabel = computed(() =>
  translate('common.pagination.ellipsis', undefined, 'More pages')
)

function getPageLabel(page: number) {
  return translate('common.pagination.page_number', { page }, `Page ${page}`)
}

const visiblePages = computed<(number | string)[]>(() => {
  const total = props.totalPages
  const current = props.currentPage

  if (total <= 7) {
    return Array.from({ length: Math.max(0, total) }, (_, i) => i + 1)
  }

  if (current <= 3) {
    return [1, 2, 3, 4, '...', total]
  }

  if (current >= total - 2) {
    return [1, '...', total - 3, total - 2, total - 1, total]
  }

  return [1, '...', current - 1, current, current + 1, '...', total]
})

const summaryText = computed(() => {
  if (props.totalCount !== undefined && props.pageSize !== undefined) {
    const start = props.totalCount === 0 ? 0 : (props.currentPage - 1) * props.pageSize + 1
    const end = Math.min(props.currentPage * props.pageSize, props.totalCount)
    return translate(
      'common.pagination.showing_range',
      { start, end, total: props.totalCount },
      `Showing ${start}–${end} of ${props.totalCount}`
    )
  }

  return translate(
    'common.pagination.page_of_total',
    { current: props.currentPage, total: props.totalPages },
    `Page ${props.currentPage} of ${props.totalPages}`
  )
})

function goToPage(page: number | string) {
  if (typeof page !== 'number') return
  if (props.disabled) return
  if (page < 1 || page > props.totalPages) return
  emit('update:currentPage', page)
  emit('change', page)
}

defineExpose({
  visiblePages,
  summaryText,
  goToPage
})
</script>

<template>
  <nav
    v-if="shouldRender"
    role="navigation"
    :aria-label="paginationLabel"
    class="flex flex-col sm:flex-row items-center justify-between gap-3 pt-6 pb-2 select-none"
  >
    <!-- Summary Info -->
    <div
      v-if="hasSummary"
      class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-medium whitespace-nowrap shrink-0"
    >
      {{ summaryText }}
    </div>

    <!-- Pagination Controls -->
    <div
      v-if="totalPages > 1"
      class="flex items-center gap-1 sm:gap-1.5 shrink-0 justify-center sm:justify-end w-full sm:w-auto"
    >
      <!-- Previous Button -->
      <button
        type="button"
        :disabled="currentPage <= 1 || disabled"
        :aria-label="prevLabel"
        :aria-disabled="currentPage <= 1 || disabled ? 'true' : undefined"
        class="min-w-[40px] min-h-[40px] h-10 px-3 flex items-center justify-center gap-1.5 rounded-xl border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:pointer-events-none transition-colors shrink-0 text-xs sm:text-sm font-medium whitespace-nowrap cursor-pointer"
        @click="goToPage(currentPage - 1)"
      >
        <ChevronLeft class="w-4 h-4 shrink-0" />
        <span class="hidden sm:inline">{{ prevLabel }}</span>
      </button>

      <!-- Page Numbers & Ellipses -->
      <template v-for="(page, idx) in visiblePages" :key="idx">
        <button
          v-if="typeof page === 'number'"
          type="button"
          :aria-label="getPageLabel(page)"
          :aria-current="page === currentPage ? 'page' : undefined"
          :data-page="page"
          class="min-w-[40px] min-h-[40px] h-10 w-10 flex items-center justify-center rounded-xl text-sm transition-colors shrink-0 cursor-pointer"
          :class="[
            page === currentPage
              ? 'bg-brand-600 text-white font-bold shadow-sm'
              : 'text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-slate-900 dark:hover:text-white font-medium'
          ]"
          @click="goToPage(page)"
        >
          {{ page }}
        </button>
        <span
          v-else
          :aria-label="ellipsisLabel"
          class="min-w-[40px] min-h-[40px] h-10 px-2 flex items-center justify-center text-slate-400 dark:text-slate-500 select-none shrink-0 text-sm font-medium"
        >
          ...
        </span>
      </template>

      <!-- Next Button -->
      <button
        type="button"
        :disabled="currentPage >= totalPages || disabled"
        :aria-label="nextLabel"
        :aria-disabled="currentPage >= totalPages || disabled ? 'true' : undefined"
        class="min-w-[40px] min-h-[40px] h-10 px-3 flex items-center justify-center gap-1.5 rounded-xl border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:pointer-events-none transition-colors shrink-0 text-xs sm:text-sm font-medium whitespace-nowrap cursor-pointer"
        @click="goToPage(currentPage + 1)"
      >
        <span class="hidden sm:inline">{{ nextLabel }}</span>
        <ChevronRight class="w-4 h-4 shrink-0" />
      </button>
    </div>
  </nav>
</template>
