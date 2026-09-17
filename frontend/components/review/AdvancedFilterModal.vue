<script setup lang="ts">
import { ref, watch } from 'vue'
import { X, SlidersHorizontal, RotateCcw, Check } from 'lucide-vue-next'
import type { ReviewFilterState } from '~/stores/useReviewStore'

const props = defineProps<{
  isOpen: boolean
  currentFilters: ReviewFilterState
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'apply', filters: ReviewFilterState): void
  (e: 'reset'): void
}>()

const localStatus = ref<number | null>(null)
const localSourceType = ref<number | null>(null)
const localUrgency = ref<string | null>(null)
const localSortBy = ref<string | null>(null)

watch(
  () => props.isOpen,
  (open) => {
    if (open) {
      localStatus.value = props.currentFilters.status ?? null
      localSourceType.value = props.currentFilters.sourceType ?? null
      localUrgency.value = props.currentFilters.urgency ?? null
      localSortBy.value = props.currentFilters.sortBy ?? null
    }
  },
  { immediate: true }
)

function handleReset() {
  localStatus.value = null
  localSourceType.value = null
  localUrgency.value = null
  localSortBy.value = null
  emit('reset')
}

function handleApply() {
  emit('apply', {
    status: localStatus.value,
    sourceType: localSourceType.value,
    urgency: localUrgency.value,
    sortBy: localSortBy.value
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
        class="w-full max-w-xl rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-900 dark:text-white shadow-2xl p-5 sm:p-6 space-y-6 max-h-[90vh] overflow-y-auto animate-in zoom-in-95 duration-200"
        role="dialog"
        aria-modal="true"
      >
        <!-- Modal Header -->
        <div class="flex items-center justify-between border-b border-slate-100 dark:border-slate-800 pb-4">
          <div class="flex items-center gap-2.5">
            <div class="w-9 h-9 rounded-2xl bg-brand-50 dark:bg-brand-950/60 text-brand-600 dark:text-brand-400 border border-brand-200 dark:border-brand-800 flex items-center justify-center">
              <SlidersHorizontal class="w-4 h-4" />
            </div>
            <div>
              <h2 class="text-base font-bold text-slate-900 dark:text-white">
                {{ $t('review.filter_modal_title') }}
              </h2>
              <p class="text-xs text-slate-500 dark:text-slate-400">
                {{ $t('review.subtitle') }}
              </p>
            </div>
          </div>

          <button
            @click="emit('close')"
            class="p-2 rounded-xl text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
          >
            <X class="w-5 h-5" />
          </button>
        </div>

        <!-- Section 1: Knowledge Source -->
        <div class="space-y-2.5">
          <label class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
            {{ $t('review.filter_knowledge_source') }}
          </label>
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
            <button
              type="button"
              @click="localSourceType = null"
              :class="[
                'px-3 py-2 rounded-xl text-xs font-semibold border transition-all text-center flex items-center justify-center gap-1.5',
                localSourceType === null
                  ? 'bg-brand-600 text-white border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <Check v-if="localSourceType === null" class="w-3.5 h-3.5" />
              <span>{{ $t('review.source_all') }}</span>
            </button>
            <button
              type="button"
              @click="localSourceType = 0"
              :class="[
                'px-3 py-2 rounded-xl text-xs font-semibold border transition-all text-center flex items-center justify-center gap-1.5',
                localSourceType === 0
                  ? 'bg-sky-600 text-white border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <Check v-if="localSourceType === 0" class="w-3.5 h-3.5" />
              <span>{{ $t('review.source_topic') }}</span>
            </button>
            <button
              type="button"
              @click="localSourceType = 1"
              :class="[
                'px-3 py-2 rounded-xl text-xs font-semibold border transition-all text-center flex items-center justify-center gap-1.5',
                localSourceType === 1
                  ? 'bg-amber-600 text-white border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <Check v-if="localSourceType === 1" class="w-3.5 h-3.5" />
              <span>{{ $t('review.source_highlight') }}</span>
            </button>
            <button
              type="button"
              @click="localSourceType = 2"
              :class="[
                'px-3 py-2 rounded-xl text-xs font-semibold border transition-all text-center flex items-center justify-center gap-1.5',
                localSourceType === 2
                  ? 'bg-rose-600 text-white border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <Check v-if="localSourceType === 2" class="w-3.5 h-3.5" />
              <span>{{ $t('review.source_quiz_mistake') }}</span>
            </button>
          </div>
        </div>

        <!-- Section 2: Mastery Stage -->
        <div class="space-y-2.5">
          <label class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
            {{ $t('review.filter_mastery_stage') }}
          </label>
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
            <button
              type="button"
              @click="localStatus = null"
              :class="[
                'px-3 py-2 rounded-xl text-xs font-semibold border transition-all text-center flex items-center justify-center gap-1.5',
                localStatus === null
                  ? 'bg-brand-600 text-white border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <Check v-if="localStatus === null" class="w-3.5 h-3.5" />
              <span>{{ $t('review.status_all') }}</span>
            </button>
            <button
              type="button"
              @click="localStatus = 0"
              :class="[
                'px-3 py-2 rounded-xl text-xs font-semibold border transition-all text-center flex items-center justify-center gap-1.5',
                localStatus === 0
                  ? 'bg-amber-500 text-slate-950 border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <Check v-if="localStatus === 0" class="w-3.5 h-3.5" />
              <span>{{ $t('review.status_learning') }}</span>
            </button>
            <button
              type="button"
              @click="localStatus = 1"
              :class="[
                'px-3 py-2 rounded-xl text-xs font-semibold border transition-all text-center flex items-center justify-center gap-1.5',
                localStatus === 1
                  ? 'bg-purple-600 text-white border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <Check v-if="localStatus === 1" class="w-3.5 h-3.5" />
              <span>{{ $t('review.status_reviewing') }}</span>
            </button>
            <button
              type="button"
              @click="localStatus = 2"
              :class="[
                'px-3 py-2 rounded-xl text-xs font-semibold border transition-all text-center flex items-center justify-center gap-1.5',
                localStatus === 2
                  ? 'bg-emerald-600 text-white border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <Check v-if="localStatus === 2" class="w-3.5 h-3.5" />
              <span>{{ $t('review.status_mastered') }}</span>
            </button>
          </div>
        </div>

        <!-- Section 3: Due Urgency -->
        <div class="space-y-2.5">
          <label class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
            {{ $t('review.filter_urgency') }}
          </label>
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
            <button
              type="button"
              @click="localUrgency = null"
              :class="[
                'px-3 py-2 rounded-xl text-xs font-semibold border transition-all text-center flex items-center justify-center gap-1.5',
                localUrgency === null
                  ? 'bg-brand-600 text-white border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <Check v-if="localUrgency === null" class="w-3.5 h-3.5" />
              <span>{{ $t('review.urgency_all') }}</span>
            </button>
            <button
              type="button"
              @click="localUrgency = 'due'"
              :class="[
                'px-3 py-2 rounded-xl text-xs font-semibold border transition-all text-center flex items-center justify-center gap-1.5',
                localUrgency === 'due'
                  ? 'bg-amber-600 text-white border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <Check v-if="localUrgency === 'due'" class="w-3.5 h-3.5" />
              <span>{{ $t('review.urgency_due') }}</span>
            </button>
            <button
              type="button"
              @click="localUrgency = 'overdue'"
              :class="[
                'px-3 py-2 rounded-xl text-xs font-semibold border transition-all text-center flex items-center justify-center gap-1.5',
                localUrgency === 'overdue'
                  ? 'bg-rose-600 text-white border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <Check v-if="localUrgency === 'overdue'" class="w-3.5 h-3.5" />
              <span>{{ $t('review.urgency_overdue') }}</span>
            </button>
            <button
              type="button"
              @click="localUrgency = 'upcoming'"
              :class="[
                'px-3 py-2 rounded-xl text-xs font-semibold border transition-all text-center flex items-center justify-center gap-1.5',
                localUrgency === 'upcoming'
                  ? 'bg-purple-600 text-white border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <Check v-if="localUrgency === 'upcoming'" class="w-3.5 h-3.5" />
              <span>{{ $t('review.urgency_upcoming') }}</span>
            </button>
          </div>
        </div>

        <!-- Section 4: Sort Options -->
        <div class="space-y-2.5">
          <label class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
            {{ $t('review.filter_sort') }}
          </label>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
            <button
              type="button"
              @click="localSortBy = null"
              :class="[
                'px-3.5 py-2 rounded-xl text-xs font-semibold border transition-all text-left flex items-center justify-between',
                localSortBy === null
                  ? 'bg-brand-600 text-white border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <span>{{ $t('review.sort_next_review_asc') }}</span>
              <Check v-if="localSortBy === null" class="w-3.5 h-3.5" />
            </button>
            <button
              type="button"
              @click="localSortBy = 'nextReviewDate_desc'"
              :class="[
                'px-3.5 py-2 rounded-xl text-xs font-semibold border transition-all text-left flex items-center justify-between',
                localSortBy === 'nextReviewDate_desc'
                  ? 'bg-brand-600 text-white border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <span>{{ $t('review.sort_next_review_desc') }}</span>
              <Check v-if="localSortBy === 'nextReviewDate_desc'" class="w-3.5 h-3.5" />
            </button>
            <button
              type="button"
              @click="localSortBy = 'difficulty'"
              :class="[
                'px-3.5 py-2 rounded-xl text-xs font-semibold border transition-all text-left flex items-center justify-between',
                localSortBy === 'difficulty'
                  ? 'bg-brand-600 text-white border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <span>{{ $t('review.sort_difficulty') }}</span>
              <Check v-if="localSortBy === 'difficulty'" class="w-3.5 h-3.5" />
            </button>
            <button
              type="button"
              @click="localSortBy = 'recent'"
              :class="[
                'px-3.5 py-2 rounded-xl text-xs font-semibold border transition-all text-left flex items-center justify-between',
                localSortBy === 'recent'
                  ? 'bg-brand-600 text-white border-transparent shadow-sm'
                  : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
              ]"
            >
              <span>{{ $t('review.sort_recent') }}</span>
              <Check v-if="localSortBy === 'recent'" class="w-3.5 h-3.5" />
            </button>
          </div>
        </div>

        <!-- Footer Actions -->
        <div class="flex items-center justify-between pt-4 border-t border-slate-100 dark:border-slate-800 gap-3">
          <button
            type="button"
            @click="handleReset"
            class="inline-flex items-center gap-1.5 px-4 py-2.5 rounded-2xl text-xs sm:text-sm font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
          >
            <RotateCcw class="w-3.5 h-3.5" />
            <span>{{ $t('review.reset_filters') }}</span>
          </button>

          <div class="flex items-center gap-2">
            <button
              type="button"
              @click="emit('close')"
              class="px-4 py-2.5 rounded-2xl text-xs sm:text-sm font-semibold text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
            >
              {{ $t('review.cancel') }}
            </button>

            <button
              type="button"
              @click="handleApply"
              class="px-5 py-2.5 rounded-2xl text-xs sm:text-sm font-bold bg-brand-600 hover:bg-brand-500 text-white transition-all shadow-sm cursor-pointer"
            >
              {{ $t('review.apply_filters') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>
