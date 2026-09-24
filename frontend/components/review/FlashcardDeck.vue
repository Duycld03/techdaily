<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useEventListener } from '@vueuse/core'
import {
  Layers,
  Eye,
  EyeOff,
  Sparkles,
  BookOpen,
  HelpCircle,
  CheckCircle2,
  Tag
} from 'lucide-vue-next'
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
    case 'BackendRuntime':
      return {
        label: 'Backend Runtime',
        badgeClass: 'bg-sky-500/10 text-sky-700 dark:text-sky-400 border-sky-500/20'
      }
    case 2:
    case 'DatabaseStorage':
      return {
        label: 'Database & Storage',
        badgeClass: 'bg-cyan-500/10 text-cyan-700 dark:text-cyan-400 border-cyan-500/20'
      }
    case 3:
    case 'SystemDesign':
      return {
        label: 'System Design',
        badgeClass: 'bg-purple-500/10 text-purple-700 dark:text-purple-400 border-purple-500/20'
      }
    case 4:
    case 'EngineeringCraft':
      return {
        label: 'Engineering Craft',
        badgeClass: 'bg-pink-500/10 text-pink-700 dark:text-pink-400 border-pink-500/20'
      }
    case 0:
    case 'FrontendWeb':
    default:
      return {
        label: 'Frontend & Browser',
        badgeClass: 'bg-brand-500/10 text-brand-600 dark:text-brand-400 border-brand-500/20'
      }
  }
})

const difficultyLabel = computed(() => {
  const diff = props.card.difficulty
  if (diff === 0 || diff === 'Fresher' || diff === 'Junior') return 'Junior'
  if (diff === 1 || diff === 'Middle' || diff === 'Intermediate') return 'Mid-Level'
  if (diff === 2 || diff === 'Senior') return 'Senior'
  if (diff === 3 || diff === 'Lead' || diff === 'Staff') return 'Staff'
  return 'Senior / Staff'
})

const formattedEaseFactor = computed(() => {
  const val = Number(props.card?.easeFactor ?? 2.5)
  return isNaN(val) ? '2.50' : val.toFixed(2)
})

const formattedInterval = computed(() => {
  const val = props.card?.intervalDays ?? 1
  return `${val}d`
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

const conceptTags = computed(() => {
  const card = props.card as any
  if (Array.isArray(card.conceptTags) && card.conceptTags.length) {
    return card.conceptTags
  }
  const tags: string[] = []
  const title = (card.topicTitle || '').toLowerCase()
  if (title.includes('proxy')) tags.push('Proxy', 'Object.defineProperty')
  if (title.includes('reactivity')) tags.push('Reactivity', 'Trap Handler')
  if (title.includes('vue')) tags.push('Vue 3 Engine')
  if (title.includes('mvcc')) tags.push('MVCC', 'Heap Tuples', 'VACUUM', 'Snapshot Isolation')
  if (title.includes('separation')) tags.push('Separation of Concerns', 'Clean Architecture', 'Unit Testing')
  if (title.includes('kestrel') || title.includes('pipe')) tags.push('PipeReader', 'MemoryPool', 'Zero-Allocation')

  if (!tags.length) {
    tags.push(categoryMeta.value.label, difficultyLabel.value)
  }
  return tags
})

const sourceExcerpt = computed(() => {
  const card = props.card as any
  if (card.sourceExcerpt) return card.sourceExcerpt
  if (card.sourceHighlightText) return card.sourceHighlightText
  if (card.sourceContext) return card.sourceContext
  return null
})

const sm2Forecast = computed(() => {
  const currentInterval = props.card.intervalDays || 1
  const ef = Number(props.card.easeFactor || 2.5)
  return [
    {
      grade: 1,
      score: 1,
      label: 'Chưa nhớ',
      interval: '1d',
      color: 'border-rose-500/40 text-rose-600 dark:text-rose-400 bg-rose-500/10 hover:bg-rose-500/20'
    },
    {
      grade: 2,
      score: 3,
      label: 'Khó',
      interval: `${Math.max(1, Math.round(currentInterval * 1.2))}d`,
      color: 'border-amber-500/40 text-amber-600 dark:text-amber-400 bg-amber-500/10 hover:bg-amber-500/20'
    },
    {
      grade: 3,
      score: 4,
      label: 'Tốt',
      interval: `${Math.max(1, Math.round(currentInterval * ef))}d`,
      color: 'border-brand-500/40 text-brand-600 dark:text-brand-400 bg-brand-500/10 hover:bg-brand-500/20'
    },
    {
      grade: 4,
      score: 5,
      label: 'Rất dễ',
      interval: `${Math.max(1, Math.round(currentInterval * ef * 1.3))}d`,
      color: 'border-emerald-500/40 text-emerald-600 dark:text-emerald-400 bg-emerald-500/10 hover:bg-emerald-500/20'
    }
  ]
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

useEventListener(typeof window !== 'undefined' ? window : null, 'keydown', handleKeyDown)
</script>

<template>
  <div class="perspective-box w-full">
    <div
      :class="[
        'card-flipper w-full rounded-2xl border transition-all duration-300 relative shadow-md dark:shadow-xl overflow-hidden',
        isFlipped
          ? 'border-brand-500/30 bg-white dark:bg-canvas-subtle'
          : 'border-slate-200/90 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle'
      ]"
    >
      <!-- FRONT FACE: Active Recall Challenge (Pixel-Perfect from temp.vue) -->
      <div v-if="!isFlipped" class="p-5 sm:p-7 space-y-4 sm:space-y-5">
        <!-- Category & Seniority Tags -->
        <div class="flex items-center justify-between gap-2 pb-3 border-b border-slate-100 dark:border-white/[0.06]">
          <div class="flex items-center gap-2">
            <span
              class="px-2.5 py-1 rounded-xl text-xs font-bold border whitespace-nowrap"
              :class="categoryMeta.badgeClass"
            >
              {{ categoryMeta.label }}
            </span>
            <span class="px-2 py-0.5 rounded-lg bg-slate-100 dark:bg-white/[0.06] text-slate-600 dark:text-slate-400 text-xs font-semibold">
              {{ difficultyLabel }}
            </span>
          </div>
          <div class="flex items-center gap-1.5 text-xs font-mono text-slate-400">
            <span>Repetition #{{ card.repetitionCount }}</span>
            <span>•</span>
            <span>EF: {{ formattedEaseFactor }} • {{ formattedInterval }}</span>
          </div>
        </div>

        <!-- Prompt Heading -->
        <div class="space-y-2">
          <div class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
            <HelpCircle class="w-3.5 h-3.5 text-brand-500" />
            <span>{{ $t('review.question_prompt') }}</span>
          </div>

          <h2 class="text-base sm:text-lg md:text-xl font-bold text-slate-900 dark:text-white leading-relaxed tracking-tight">
            {{ questionText }}
          </h2>
        </div>

        <!-- Core Concepts Tags -->
        <div v-if="conceptTags.length" class="flex flex-wrap items-center gap-1.5">
          <span class="text-[11px] text-slate-400 flex items-center gap-1 mr-1">
            <Tag class="w-3 h-3" /> Core Concepts:
          </span>
          <span
            v-for="tag in conceptTags"
            :key="tag"
            class="px-2 py-0.5 rounded-md text-[11px] font-mono bg-slate-100 dark:bg-white/[0.04] text-slate-600 dark:text-slate-300 border border-slate-200/80 dark:border-white/[0.06]"
          >
            {{ tag }}
          </span>
        </div>

        <!-- Source Excerpt Box -->
        <div v-if="sourceExcerpt" class="p-3 sm:p-3.5 rounded-xl bg-slate-50 dark:bg-canvas border border-slate-200/80 dark:border-white/[0.06] space-y-1">
          <div class="flex items-center gap-1.5 text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
            <BookOpen class="w-3 h-3 text-brand-500" />
            <span>Original Documentation Context</span>
          </div>
          <p class="text-xs text-slate-600 dark:text-slate-300 italic leading-relaxed">
            "{{ sourceExcerpt }}"
          </p>
        </div>

        <!-- Flip CTA Button -->
        <div class="pt-3 border-t border-slate-100 dark:border-white/[0.06] flex items-center justify-center">
          <button
            @click="isFlipped = true"
            type="button"
            class="group inline-flex items-center justify-center gap-2 px-8 py-3 rounded-2xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm shadow-md shadow-brand-500/25 transition-all cursor-pointer w-full sm:w-auto active:scale-[0.98]"
          >
            <Eye class="w-4 h-4 transition-transform group-hover:scale-110" />
            <span>{{ $t('review.show_answer') }}</span>
            <kbd class="hidden sm:inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-mono bg-white/20 text-white">Space</kbd>
          </button>
        </div>
      </div>

      <!-- BACK FACE: Architectural Solution & SM-2 Rating (Pixel-Perfect from temp.vue) -->
      <div v-else class="p-5 sm:p-7 space-y-4">
        <!-- Solution Header -->
        <div class="flex items-center justify-between gap-2 pb-2.5 border-b border-slate-100 dark:border-white/[0.06]">
          <span class="px-2.5 py-0.5 rounded-lg text-xs font-bold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20 flex items-center gap-1">
            <CheckCircle2 class="w-3.5 h-3.5 text-emerald-500" />
            {{ $t('review.core_solution') }}
          </span>
          <button
            @click="isFlipped = false"
            type="button"
            class="inline-flex items-center gap-1.5 text-xs text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 transition-colors cursor-pointer"
          >
            <EyeOff class="w-3.5 h-3.5" />
            <span>{{ $t('review.hide_answer') }}</span>
            <kbd class="px-1 rounded text-[10px] font-mono bg-slate-100 dark:bg-white/10">Esc</kbd>
          </button>
        </div>

        <!-- Summary -->
        <p class="text-xs sm:text-sm font-semibold text-slate-800 dark:text-slate-200 leading-relaxed">
          {{ answerText }}
        </p>

        <!-- Technical Mechanics (Deep Dive) -->
        <div
          v-if="hasDistinctDeepDive"
          class="space-y-2 p-3 sm:p-3.5 rounded-xl bg-slate-50 dark:bg-canvas border border-slate-200/80 dark:border-white/[0.06] overflow-y-auto max-h-[300px] prose prose-sm dark:prose-invert max-w-none text-slate-700 dark:text-slate-300 leading-relaxed"
          v-html="renderedDeepDive"
        ></div>

        <!-- SM-2 Grading Pill Bar -->
        <div class="pt-3 border-t border-slate-100 dark:border-white/[0.06] space-y-2">
          <div class="flex items-center justify-between text-[11px] text-slate-400 px-1 font-semibold">
            <span>{{ $t('review.grade_keyboard_hint') }}</span>
            <span class="font-mono">Keys [1] - [4]</span>
          </div>

          <Sm2GradingButtons @grade="handleGrade" />
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.perspective-box {
  perspective: 1000px;
}
</style>
