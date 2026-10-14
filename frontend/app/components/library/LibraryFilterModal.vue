<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { X, SlidersHorizontal, RotateCcw } from 'lucide-vue-next'

export interface LibraryFilterState {
  category?: number
  language?: string
}

const props = defineProps<{
  isOpen: boolean
  currentFilters: LibraryFilterState
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'apply', filters: LibraryFilterState): void
  (e: 'reset'): void
}>()

const { t } = useI18n()

const localCategory = ref<number | undefined>(undefined)
const localLanguage = ref<string | undefined>(undefined)

watch(
  () => props.isOpen,
  (open) => {
    if (open) {
      localCategory.value = props.currentFilters.category
      localLanguage.value = props.currentFilters.language
    }
  },
  { immediate: true }
)

const categories = computed(() => [
  { id: undefined, label: t('library.categories.all') },
  { id: 0, label: t('library.categories.frontend') },
  { id: 1, label: t('library.categories.backend') },
  { id: 2, label: t('library.categories.database') },
  { id: 3, label: t('library.categories.system_design') },
  { id: 4, label: t('library.categories.craft') },
  { id: 5, label: t('library.categories.mental_models') },
  { id: 6, label: t('library.categories.habits') }
])

const languages = computed(() => [
  { id: undefined, label: t('library.discover.language_all') },
  { id: 'en', label: t('library.language_english') },
  { id: 'vi', label: t('library.language_vietnamese') }
])

const pendingFilterCount = computed(() => {
  let count = 0
  if (localCategory.value !== undefined) count++
  if (localLanguage.value !== undefined && localLanguage.value !== '') count++
  return count
})

function handleReset() {
  localCategory.value = undefined
  localLanguage.value = undefined
  emit('reset')
}

function handleApply() {
  emit('apply', {
    category: localCategory.value,
    language: localLanguage.value
  })
  emit('close')
}

function handleBackdropClick(e: MouseEvent) {
  if (e.target === e.currentTarget) {
    emit('close')
  }
}
</script>

<template>
  <Teleport to="body">
    <div
      v-if="isOpen"
      class="fixed inset-0 z-50 flex items-center justify-center p-4 sm:p-6 bg-slate-950/60 backdrop-blur-sm animate-in fade-in duration-200"
      @click="handleBackdropClick"
    >
      <div
        class="w-full max-w-xl rounded-3xl glass-panel text-slate-900 dark:text-white shadow-2xl p-5 sm:p-6 space-y-6 max-h-[90vh] overflow-y-auto animate-in zoom-in-95 duration-200"
        role="dialog"
        aria-modal="true"
      >
        <!-- Modal Header -->
        <div class="flex items-center justify-between border-b border-slate-100 dark:border-white/[0.08] pb-4">
          <div class="flex items-center gap-2.5">
            <div class="w-9 h-9 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center">
              <SlidersHorizontal class="w-4 h-4" />
            </div>
            <div>
              <h2 class="text-base font-bold text-slate-900 dark:text-white">
                {{ $t('library.filter_modal_title') }}
              </h2>
              <p class="text-xs text-slate-500 dark:text-slate-400">
                {{ $t('library.filter_modal_subtitle') }}
              </p>
            </div>
          </div>

          <button
            type="button"
            data-testid="library-filter-modal-close-btn"
            :aria-label="$t('common.close')"
            @click="emit('close')"
            class="p-2 rounded-xl text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors cursor-pointer"
          >
            <X class="w-5 h-5" />
          </button>
        </div>

        <!-- Section 1: Categories (All 7 DeepPace Categories + All) -->
        <div class="space-y-2.5">
          <label class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
            {{ $t('library.category_label') }}
          </label>
          <div class="flex flex-wrap items-center gap-2">
            <button
              v-for="cat in categories"
              :key="String(cat.id)"
              type="button"
              @click="localCategory = cat.id"
              :class="[
                'px-3.5 py-1.5 rounded-xl text-xs font-semibold border transition-all text-center whitespace-nowrap cursor-pointer',
                localCategory === cat.id
                  ? 'bg-brand-600 text-white font-bold border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200/80 dark:border-white/[0.06] hover:bg-slate-100 dark:hover:bg-white/[0.04]'
              ]"
            >
              <span>{{ cat.label }}</span>
            </button>
          </div>
        </div>

        <!-- Section 2: Content Language -->
        <div class="space-y-2.5">
          <label class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
            {{ $t('library.discover.language_filter') }}
          </label>
          <div class="flex flex-wrap items-center gap-2">
            <button
              v-for="lang in languages"
              :key="String(lang.id)"
              type="button"
              @click="localLanguage = lang.id"
              :class="[
                'px-3.5 py-1.5 rounded-xl text-xs font-semibold border transition-all text-center whitespace-nowrap cursor-pointer',
                (localLanguage === lang.id || (lang.id === undefined && !localLanguage))
                  ? 'bg-brand-600 text-white font-bold border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200/80 dark:border-white/[0.06] hover:bg-slate-100 dark:hover:bg-white/[0.04]'
              ]"
            >
              <span>{{ lang.label }}</span>
            </button>
          </div>
        </div>

        <!-- Footer Actions -->
        <div class="flex items-center justify-between pt-4 border-t border-slate-100 dark:border-white/[0.08] gap-3">
          <button
            type="button"
            data-testid="library-filter-modal-reset-btn"
            @click="handleReset"
            class="inline-flex items-center gap-1.5 px-3 sm:px-4 py-2 rounded-2xl text-xs sm:text-sm font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors cursor-pointer whitespace-nowrap shrink-0"
          >
            <RotateCcw class="w-3.5 h-3.5" />
            <span>{{ $t('review.reset_filters') }}</span>
          </button>

          <div class="flex items-center gap-2 shrink-0">
            <button
              type="button"
              data-testid="library-filter-modal-cancel-btn"
              @click="emit('close')"
              class="px-3 sm:px-4 py-2 rounded-2xl text-xs sm:text-sm font-semibold text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors cursor-pointer whitespace-nowrap shrink-0"
            >
              {{ $t('common.cancel') }}
            </button>

            <button
              type="button"
              data-testid="library-filter-modal-apply-btn"
              @click="handleApply"
              class="inline-flex items-center gap-1.5 px-4 sm:px-5 py-2 rounded-2xl text-xs sm:text-sm font-bold bg-brand-600 hover:bg-brand-500 text-white transition-all shadow-sm cursor-pointer whitespace-nowrap shrink-0"
            >
              <span>{{ $t('review.apply_filters') }}</span>
              <span
                v-if="pendingFilterCount > 0"
                class="px-1.5 py-0.2 rounded-full text-[10px] font-bold bg-white/20 text-white"
              >
                {{ pendingFilterCount }}
              </span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>
