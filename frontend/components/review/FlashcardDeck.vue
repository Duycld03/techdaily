<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { Layers, Eye, EyeOff, Sparkles, BookOpen, HelpCircle } from 'lucide-vue-next'
import type { ReviewCard } from '~/stores/useReviewStore'
import Sm2GradingButtons from '~/components/review/Sm2GradingButtons.vue'
import { useMarkdownRenderer } from '~/composables/useMarkdownRenderer'

const props = defineProps<{
  card: ReviewCard
  remainingCount: number
}>()

const emit = defineEmits<{
  (e: 'grade', grade: number): void
}>()

const isFlipped = ref(false)
const { render: renderMarkdown, isHighlighterReady } = useMarkdownRenderer()

// Reset flip state whenever the card changes
watch(
  () => props.card.id,
  () => {
    isFlipped.value = false
  }
)

const categoryMeta = computed(() => {
  const cat = props.card.category
  switch (cat) {
    case 1:
      return {
        label: 'Backend Runtime',
        badgeClass: 'bg-sky-500/10 text-sky-700 dark:text-sky-400 border-sky-500/20'
      }
    case 2:
      return {
        label: 'Database & Storage',
        badgeClass: 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-400 border-emerald-500/20'
      }
    case 3:
      return {
        label: 'System Design',
        badgeClass: 'bg-purple-500/10 text-purple-700 dark:text-purple-400 border-purple-500/20'
      }
    case 4:
      return {
        label: 'Engineering Craft',
        badgeClass: 'bg-pink-500/10 text-pink-700 dark:text-pink-400 border-pink-500/20'
      }
    case 0:
    default:
      return {
        label: 'Frontend & Browser',
        badgeClass: 'bg-amber-500/10 text-amber-700 dark:text-amber-400 border-amber-500/20'
      }
  }
})

const difficultyLabel = computed(() => {
  switch (props.card.difficulty) {
    case 0: return 'Junior'
    case 1: return 'Mid-Level'
    case 2: return 'Senior'
    case 3: return 'Staff'
    default: return 'Senior'
  }
})

const questionText = computed(() => {
  return props.card.frontMarkdown?.trim() || props.card.topicTitle
})

const answerText = computed(() => {
  return props.card.backMarkdown?.trim() || props.card.topicSummary
})

const hasDistinctDeepDive = computed(() => {
  const dd = props.card.topicDeepDiveMarkdown?.trim()
  const ans = answerText.value?.trim()
  return !!dd && dd !== ans
})

const renderedDeepDive = computed(() => {
  const _ = isHighlighterReady.value
  if (!hasDistinctDeepDive.value) return ''
  return renderMarkdown(props.card.topicDeepDiveMarkdown)
})

function handleGrade(score: number) {
  emit('grade', score)
  isFlipped.value = false
}

function isInteractiveInput(target: EventTarget | null): boolean {
  if (!target || !(target instanceof HTMLElement)) return false
  const tag = target.tagName.toLowerCase()
  return (
    tag === 'input' ||
    tag === 'textarea' ||
    tag === 'select' ||
    target.isContentEditable ||
    target.getAttribute('contenteditable') === 'true'
  )
}

function handleKeyDown(e: KeyboardEvent) {
  // Guard against triggering shortcuts while user is typing in search, inputs, or contentEditable
  if (isInteractiveInput(e.target)) return
  if (typeof document !== 'undefined' && isInteractiveInput(document.activeElement)) return

  if (!isFlipped.value) {
    if (e.code === 'Space' || e.key === ' ' || e.key === 'Enter') {
      e.preventDefault()
      isFlipped.value = true
    }
  } else {
    // 1: Again (1), 2: Hard (3), 3: Good (4), 4: Easy (5)
    if (e.key === '1') {
      e.preventDefault()
      handleGrade(1)
    } else if (e.key === '2') {
      e.preventDefault()
      handleGrade(3)
    } else if (e.key === '3') {
      e.preventDefault()
      handleGrade(4)
    } else if (e.key === '4') {
      e.preventDefault()
      handleGrade(5)
    } else if (e.key === 'Escape') {
      e.preventDefault()
      isFlipped.value = false
    }
  }
}

onMounted(() => {
  if (typeof window !== 'undefined') {
    window.addEventListener('keydown', handleKeyDown)
  }
})

onUnmounted(() => {
  if (typeof window !== 'undefined') {
    window.removeEventListener('keydown', handleKeyDown)
  }
})
</script>

<template>
  <div class="w-full max-w-2xl mx-auto flex flex-col items-center space-y-5 sm:space-y-6">
    <!-- Card Telemetry & Progress Bar -->
    <div class="flex items-center justify-between w-full text-xs sm:text-sm text-slate-600 dark:text-slate-400 font-semibold px-1">
      <span class="flex items-center gap-2 text-brand-600 dark:text-brand-400 font-bold">
        <Layers class="w-4 h-4" />
        <span>Card 1 of {{ remainingCount }}</span>
      </span>
      <div class="flex items-center gap-2">
        <span class="px-2.5 py-0.5 sm:px-3 sm:py-1 rounded-full bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-slate-700 dark:text-slate-300 font-mono text-[11px] sm:text-xs shadow-sm">
          EF: {{ card.easeFactor.toFixed(2) }} • {{ card.intervalDays }}d
        </span>
      </div>
    </div>

    <!-- 3D Perspective Flip Container -->
    <div class="perspective-container w-full">
      <Transition name="card-flip" mode="out-in">
        <!-- ================================================================= -->
        <!-- FRONT FACE: Question & Challenge (Zero Answer Leakage)            -->
        <!-- ================================================================= -->
        <div
          v-if="!isFlipped"
          key="front"
          class="w-full min-h-[340px] sm:min-h-[400px] p-6 sm:p-8 rounded-3xl glass-card border border-slate-200/80 dark:border-white/[0.08] shadow-xl dark:shadow-2xl flex flex-col justify-between transition-all"
        >
          <!-- Top Metadata Row -->
          <div>
            <div class="flex flex-wrap items-center justify-between gap-2 mb-4 sm:mb-5">
              <div class="flex items-center gap-2">
                <span
                  class="inline-flex items-center px-2.5 py-0.5 sm:px-3 sm:py-1 rounded-xl text-xs font-bold border whitespace-nowrap"
                  :class="categoryMeta.badgeClass"
                >
                  {{ categoryMeta.label }}
                </span>
                <span class="px-2 py-0.5 rounded-lg bg-slate-100 dark:bg-white/[0.06] text-slate-600 dark:text-slate-400 text-xs font-semibold">
                  {{ difficultyLabel }}
                </span>
              </div>
              <span class="text-xs font-mono text-slate-500 dark:text-slate-400">
                Repetition #{{ card.repetitionCount }}
              </span>
            </div>

            <!-- Question Label Header -->
            <div class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 mb-2">
              <HelpCircle class="w-3.5 h-3.5" />
              <span>{{ $t('review.question_prompt') }}</span>
            </div>

            <!-- Question Challenge Text -->
            <h2 class="text-lg sm:text-2xl font-extrabold text-slate-900 dark:text-white leading-snug tracking-tight">
              {{ questionText }}
            </h2>
          </div>

          <!-- Flip Action CTA -->
          <div class="mt-8 pt-6 border-t border-slate-100 dark:border-white/[0.06] flex flex-col sm:flex-row items-center justify-center gap-3">
            <button
              @click="isFlipped = true"
              type="button"
              class="group inline-flex items-center justify-center gap-2.5 px-8 py-3.5 rounded-2xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm sm:text-base shadow-lg shadow-brand-500/25 hover:shadow-brand-500/35 transition-all active:scale-[0.98] cursor-pointer w-full sm:w-auto"
            >
              <Eye class="w-4 h-4 transition-transform group-hover:scale-110" />
              <span>{{ $t('review.show_answer') }}</span>
              <kbd class="hidden sm:inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-mono font-bold bg-white/20 text-white border border-white/30">Space</kbd>
            </button>
            <span class="text-xs text-slate-400 dark:text-slate-500 sm:hidden">
              {{ $t('review.flip_card_hint') }}
            </span>
          </div>
        </div>

        <!-- ================================================================= -->
        <!-- BACK FACE: Architectural Solution & Deep Dive                     -->
        <!-- ================================================================= -->
        <div
          v-else
          key="back"
          class="w-full min-h-[340px] sm:min-h-[400px] p-6 sm:p-8 rounded-3xl glass-card border border-brand-500/20 dark:border-brand-500/30 shadow-xl dark:shadow-2xl flex flex-col justify-between transition-all"
        >
          <div>
            <!-- Top Metadata Row -->
            <div class="flex flex-wrap items-center justify-between gap-2 mb-4 pb-3 border-b border-slate-100 dark:border-white/[0.06]">
              <div class="flex items-center gap-2">
                <span
                  class="inline-flex items-center px-2.5 py-0.5 sm:px-3 sm:py-1 rounded-xl text-xs font-bold border whitespace-nowrap"
                  :class="categoryMeta.badgeClass"
                >
                  {{ categoryMeta.label }}
                </span>
                <span class="text-xs font-mono text-slate-500 dark:text-slate-400">
                  Repetition #{{ card.repetitionCount }}
                </span>
              </div>
              <button
                @click="isFlipped = false"
                type="button"
                class="inline-flex items-center gap-1.5 text-xs text-slate-500 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200 transition-colors cursor-pointer"
              >
                <EyeOff class="w-3.5 h-3.5" />
                <span>{{ $t('review.hide_answer') }}</span>
                <kbd class="hidden sm:inline-flex px-1 rounded text-[10px] font-mono bg-slate-200/60 dark:bg-white/10 text-slate-600 dark:text-slate-300">Esc</kbd>
              </button>
            </div>

            <!-- Question Reminder -->
            <div class="mb-4">
              <div class="text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 mb-1">
                {{ $t('review.question_prompt') }}
              </div>
              <p class="text-sm sm:text-base font-bold text-slate-700 dark:text-slate-300 line-clamp-2">
                {{ questionText }}
              </p>
            </div>

            <!-- Core Solution / Answer Container -->
            <div class="p-4 sm:p-5 rounded-2xl bg-slate-50/80 dark:bg-canvas-subtle/80 border border-slate-200/80 dark:border-white/[0.08] shadow-sm mb-4">
              <div class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400 mb-2">
                <Sparkles class="w-3.5 h-3.5 text-brand-500" />
                <span>{{ $t('review.core_solution') }}</span>
              </div>
              <p class="text-sm sm:text-base md:text-lg text-slate-800 dark:text-slate-200 leading-relaxed font-normal">
                {{ answerText }}
              </p>
            </div>

            <!-- Deep Dive & Code Walkthrough (if distinct from summary) -->
            <div v-if="hasDistinctDeepDive" class="p-4 sm:p-5 rounded-2xl bg-slate-50/40 dark:bg-canvas-subtle/40 border border-slate-200/60 dark:border-white/[0.06] mb-4">
              <div class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-400 mb-2.5">
                <BookOpen class="w-3.5 h-3.5 text-slate-500" />
                <span>{{ $t('review.deep_dive_explanation') }}</span>
              </div>
              <div
                class="markdown-body prose prose-slate dark:prose-invert max-w-none text-sm sm:text-base leading-relaxed overflow-x-auto"
                v-html="renderedDeepDive"
              ></div>
            </div>
          </div>

          <!-- Bottom Micro-Hint for Grading -->
          <div class="pt-4 border-t border-slate-100 dark:border-white/[0.06] text-center">
            <span class="text-[11px] sm:text-xs text-slate-400 dark:text-slate-500">
              {{ $t('review.grade_keyboard_hint') }}
            </span>
          </div>
        </div>
      </Transition>
    </div>

    <!-- SM-2 Grading Buttons (Only visible when flipped) -->
    <div v-if="isFlipped" class="w-full animate-in fade-in slide-in-from-bottom-2 duration-200">
      <Sm2GradingButtons @grade="handleGrade" />
    </div>
  </div>
</template>

<style scoped>
.perspective-container {
  perspective: 1000px;
}

.card-flip-enter-active,
.card-flip-leave-active {
  transition: all 0.28s cubic-bezier(0.4, 0, 0.2, 1);
  transform-style: preserve-3d;
}

.card-flip-enter-from {
  opacity: 0;
  transform: rotateY(-90deg) scale(0.97);
}

.card-flip-leave-to {
  opacity: 0;
  transform: rotateY(90deg) scale(0.97);
}
</style>
