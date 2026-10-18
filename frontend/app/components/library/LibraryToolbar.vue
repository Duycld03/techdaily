<script setup lang="ts">
import { computed } from 'vue'
import { Search, SlidersHorizontal, X } from 'lucide-vue-next'

const props = defineProps<{
  scope: 'library' | 'catalog'
  searchPlaceholder: string
  categories: { id: string; label: string }[]
  selectedCategory: string | null
  activeFilterCount: number
  filtersOpen: boolean
}>()
const search = defineModel<string>({ required: true })
const emit = defineEmits<{
  search: []
  clear: []
  category: [id?: string]
  filters: []
}>()
const QUICK_CATEGORY_LIMIT = 4
const quickCategories = computed(() => props.categories.slice(0, QUICK_CATEGORY_LIMIT))
</script>

<template>
  <div class="w-full min-w-0 space-y-3 py-1">
    <div class="flex w-full min-w-0 items-center gap-2 sm:gap-3">
      <div role="search" class="relative min-w-0 flex-1">
        <label :for="`${scope}-search`" class="sr-only">{{ searchPlaceholder }}</label>
        <Search aria-hidden="true" class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
        <input
          :id="`${scope}-search`"
          v-model="search"
          type="text"
          :data-testid="`${scope}-search-input`"
          :placeholder="searchPlaceholder"
          class="h-11 w-full rounded-xl border border-slate-200/80 bg-white pl-10 pr-10 text-sm text-slate-900 shadow-sm transition-colors placeholder:text-slate-400 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20 dark:border-white/[0.08] dark:bg-canvas-subtle dark:text-slate-200 sm:text-base"
          @keyup.enter="emit('search')"
        />
        <button
          v-if="search"
          type="button"
          :aria-label="$t('common.clear_search')"
          class="absolute right-1.5 top-1/2 flex h-8 w-8 shrink-0 -translate-y-1/2 items-center justify-center rounded-lg text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-700 focus-visible:outline focus-visible:outline-2 focus-visible:outline-brand-500 dark:hover:bg-white/[0.06] dark:hover:text-white"
          @click="emit('clear')"
        >
          <X aria-hidden="true" class="h-4 w-4" />
        </button>
      </div>
      <button
        type="button"
        :data-testid="`${scope}-advanced-filter-btn`"
        aria-haspopup="dialog"
        :aria-expanded="filtersOpen"
        class="inline-flex h-11 shrink-0 items-center justify-center gap-1.5 whitespace-nowrap rounded-xl border border-slate-200/80 bg-white px-3 text-sm font-semibold text-slate-700 shadow-sm transition-colors hover:border-brand-500/50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-500 dark:border-white/[0.08] dark:bg-canvas-subtle dark:text-slate-200 sm:gap-2 sm:px-4 sm:text-base"
        @click="emit('filters')"
      >
        <SlidersHorizontal aria-hidden="true" class="h-4 w-4 shrink-0" />
        <span>{{ $t('library.discover.advanced_filter_btn') }}</span>
        <span v-if="activeFilterCount > 0" class="shrink-0 whitespace-nowrap rounded-full bg-brand-500 px-1.5 py-0.5 text-xs font-bold text-white">{{ activeFilterCount }}</span>
      </button>
    </div>
    <div role="group" :aria-label="$t('library.category_label')" :data-testid="`${scope}-category-chips`" class="flex min-w-0 flex-wrap items-center gap-2">
      <button
        type="button"
        :aria-pressed="!selectedCategory"
        class="shrink-0 whitespace-nowrap rounded-xl border px-3 py-1.5 text-sm font-semibold transition-colors duration-150 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-500 sm:text-base"
        :class="!selectedCategory ? 'border-brand-500/40 bg-brand-500/10 text-brand-600 dark:text-brand-300' : 'border-slate-200/80 bg-white text-slate-600 dark:border-white/[0.08] dark:bg-canvas-subtle dark:text-slate-400'"
        @click="emit('category')"
      >
        {{ $t('library.categories.all') }}
      </button>
      <button
        v-for="category in quickCategories"
        :key="category.id"
        type="button"
        :title="category.label"
        :aria-pressed="selectedCategory === category.id"
        class="min-w-0 max-w-full shrink-0 whitespace-nowrap rounded-xl border px-3 py-1.5 text-sm font-semibold transition-colors duration-150 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-500 sm:max-w-sm sm:text-base"
        :class="selectedCategory === category.id ? 'border-brand-500/40 bg-brand-500/10 text-brand-600 dark:text-brand-300' : 'border-slate-200/80 bg-white text-slate-600 hover:border-brand-500/40 dark:border-white/[0.08] dark:bg-canvas-subtle dark:text-slate-400'"
        @click="emit('category', category.id)"
      >
        <span class="block truncate">{{ category.label }}</span>
      </button>
    </div>
  </div>
</template>
