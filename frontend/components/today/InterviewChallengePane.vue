<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { Terminal, CheckCircle2, XCircle, Sparkles, Lock, ArrowRight, Check, AlertCircle, PenTool, Mic, Send } from 'lucide-vue-next'
import confetti from 'canvas-confetti'
import type { InterviewQuestion, DailyDrill } from '~/stores/useDailyFocusStore'
import { useDailyFocusStore } from '~/stores/useDailyFocusStore'
import { useAuthStore } from '~/stores/useAuthStore'
import { useMarkdownRenderer } from '~/composables/useMarkdownRenderer'
import CodeMirrorEditor from '~/components/today/CodeMirrorEditor.vue'
import AudioRecorderModal from '~/components/today/AudioRecorderModal.vue'
import ScorecardAiReview from '~/components/today/ScorecardAiReview.vue'

const props = defineProps<{
  question: InterviewQuestion
  drill: DailyDrill
}>()

const authStore = useAuthStore()
const router = useRouter()
const focusStore = useDailyFocusStore()
const { locale } = useI18n()
const { render: renderMarkdown } = useMarkdownRenderer()

const optionLetters = ['A', 'B', 'C', 'D', 'E', 'F']

// Selected option state (initialized from drill if already submitted)
const selectedOption = ref<number | null>(props.drill.selectedOptionIndex ?? null)

// Watch for drill changes
watch(() => props.drill, (newDrill) => {
  if (newDrill.selectedOptionIndex !== undefined && newDrill.selectedOptionIndex !== null) {
    selectedOption.value = newDrill.selectedOptionIndex
  }
}, { immediate: true })

const isReviewed = computed(() => props.drill.status === 2)
const isMultipleChoice = computed(() => props.question.options && props.question.options.length > 0)

const isCorrect = computed(() => {
  if (props.drill.isCorrect !== undefined && props.drill.isCorrect !== null) {
    return props.drill.isCorrect
  }
  if (props.question.correctOptionIndex !== undefined && props.question.correctOptionIndex !== null && selectedOption.value !== null) {
    return selectedOption.value === props.question.correctOptionIndex
  }
  return false
})

const renderedExplanation = computed(() => {
  if (props.question.explanationMarkdown) {
    return renderMarkdown(props.question.explanationMarkdown)
  }
  return ''
})

// Legacy write/voice fallback state
const inputMode = ref<'write' | 'voice'>('write')
const answerText = ref(props.drill.userAnswerText || '')
const audioBase64 = ref<string | null>(null)
const audioMimeType = ref<string>('audio/webm')

function handleAudioUpdate(blob: Blob | null, b64: string | null) {
  audioBase64.value = b64
  if (blob) {
    audioMimeType.value = blob.type
  }
}

async function handleOptionSelect(index: number) {
  if (isReviewed.value) return
  selectedOption.value = index
}

async function handleOptionSubmit() {
  if (!authStore.isLoggedIn) {
    router.push({ path: '/login', query: { redirect: '/today' } })
    return
  }

  if (selectedOption.value === null || focusStore.isSubmitting) return

  try {
    const res = await focusStore.submitOption(selectedOption.value, locale.value)
    if (res?.isCorrect) {
      confetti({
        particleCount: 80,
        spread: 60,
        origin: { y: 0.6 }
      })
    }
  } catch (err) {
    // handled in store
  }
}

async function handleLegacySubmit() {
  if (!authStore.isLoggedIn) {
    router.push({ path: '/login', query: { redirect: '/today' } })
    return
  }

  try {
    const res = await focusStore.submitDrill({
      answerText: inputMode.value === 'write' ? answerText.value : undefined,
      audioBase64: inputMode.value === 'voice' ? audioBase64.value! : undefined,
      audioMimeType: audioMimeType.value,
      locale: locale.value
    })

    if (res?.review && res.review.score >= 8) {
      confetti({
        particleCount: 80,
        spread: 60,
        origin: { y: 0.6 }
      })
    }
  } catch (err) {
    // handled in store
  }
}
</script>

<template>
  <div class="h-full flex flex-col bg-white dark:bg-slate-900/60 p-4 sm:p-6 md:p-9 overflow-y-auto space-y-5 sm:space-y-6 transition-colors duration-200">
    <!-- Header -->
    <div class="space-y-2.5 sm:space-y-3">
      <div class="flex items-center justify-between gap-2">
        <div class="flex items-center gap-2 text-xs sm:text-sm font-bold text-brand-600 dark:text-brand-400 uppercase tracking-wider">
          <Terminal class="w-4 h-4 shrink-0" />
          <span>{{ $t('today.scenario_challenge') }}</span>
        </div>

        <div class="flex items-center gap-1.5 sm:gap-2 shrink-0">
          <span class="px-2.5 py-1 rounded-full text-xs font-semibold uppercase tracking-wider bg-purple-100 dark:bg-purple-950/60 text-purple-700 dark:text-purple-300 border border-purple-200 dark:border-purple-800/60">
            Senior Drill
          </span>
          <span
            v-if="isReviewed"
            :class="[
              'px-2.5 py-1 rounded-full text-xs font-semibold flex items-center gap-1.5 border',
              isCorrect
                ? 'bg-emerald-100 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300 border-emerald-200 dark:border-emerald-800'
                : 'bg-amber-100 dark:bg-amber-950/60 text-amber-800 dark:text-amber-300 border-amber-200 dark:border-amber-800'
            ]"
          >
            <CheckCircle2 v-if="isCorrect" class="w-3.5 h-3.5" />
            <AlertCircle v-else class="w-3.5 h-3.5" />
            <span>{{ isCorrect ? '+10 Pts' : '0 Pts' }}</span>
          </span>
        </div>
      </div>

      <!-- Question Text -->
      <h2 class="text-base sm:text-xl md:text-2xl font-bold text-slate-900 dark:text-white leading-snug">
        {{ question.questionText }}
      </h2>
    </div>

    <!-- Scenario Multiple-Choice Interface -->
    <div v-if="isMultipleChoice" class="space-y-5 sm:space-y-6 flex-1 flex flex-col justify-between">
      <!-- Options List -->
      <div class="space-y-3">
        <div class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {{ $t('today.select_option_hint') }}
        </div>

        <div class="space-y-2.5 sm:space-y-3">
          <button
            v-for="(option, index) in question.options"
            :key="index"
            type="button"
            @click="handleOptionSelect(index)"
            :disabled="isReviewed || focusStore.isSubmitting"
            :class="[
              'w-full text-left p-3.5 sm:p-5 rounded-2xl text-xs sm:text-sm font-medium border transition-all duration-200 flex items-start gap-3 sm:gap-4 relative group select-none',
              !isReviewed && selectedOption === index
                ? 'border-brand-500 bg-brand-50/70 dark:bg-brand-500/10 text-brand-950 dark:text-brand-100 ring-2 ring-brand-500/30 shadow-sm'
                : !isReviewed
                ? 'border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900/40 text-slate-800 dark:text-slate-200 hover:border-brand-300 dark:hover:border-slate-700 hover:bg-white dark:hover:bg-slate-800/50 cursor-pointer'
                : isReviewed && index === question.correctOptionIndex
                ? 'border-emerald-500 bg-emerald-50 dark:bg-emerald-500/15 text-emerald-950 dark:text-emerald-100 font-semibold ring-2 ring-emerald-500/30'
                : isReviewed && selectedOption === index && index !== question.correctOptionIndex
                ? 'border-rose-500 bg-rose-50 dark:bg-rose-500/15 text-rose-950 dark:text-rose-100 ring-2 ring-rose-500/30'
                : 'border-slate-200/60 dark:border-slate-800/60 bg-slate-50/30 dark:bg-slate-950/20 text-slate-500 dark:text-slate-400 opacity-60'
            ]"
          >
            <!-- Option Letter Badge -->
            <div
              :class="[
                'w-6.5 h-6.5 sm:w-8 sm:h-8 rounded-xl flex items-center justify-center font-bold text-xs shrink-0 transition-colors duration-200 mt-0.5 sm:mt-0',
                !isReviewed && selectedOption === index
                  ? 'bg-brand-600 text-white'
                  : !isReviewed
                  ? 'bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-300 group-hover:border-brand-400'
                  : isReviewed && index === question.correctOptionIndex
                  ? 'bg-emerald-600 text-white'
                  : isReviewed && selectedOption === index && index !== question.correctOptionIndex
                  ? 'bg-rose-600 text-white'
                  : 'bg-slate-200 dark:bg-slate-800 text-slate-400'
              ]"
            >
              {{ optionLetters[index] || (index + 1) }}
            </div>

            <!-- Option Text -->
            <div class="flex-1 min-w-0 break-words pt-0.5 leading-relaxed">
              {{ option }}
            </div>

            <!-- Status Indicator Icon / Badges -->
            <div v-if="isReviewed" class="shrink-0 flex items-center gap-1 sm:gap-1.5 pt-0.5">
              <span
                v-if="index === question.correctOptionIndex"
                class="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-xs font-bold bg-emerald-600 text-white shadow-sm"
              >
                <Check class="w-3.5 h-3.5" />
                <span>{{ $t('today.optimal_choice') }}</span>
              </span>
              <span
                v-else-if="selectedOption === index && index !== question.correctOptionIndex"
                class="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-xs font-bold bg-rose-600 text-white shadow-sm"
              >
                <XCircle class="w-3.5 h-3.5" />
                <span>{{ $t('today.your_choice') }}</span>
              </span>
            </div>
          </button>
        </div>
      </div>

      <!-- Guest Sign-in prompt banner if not logged in -->
      <div
        v-if="!authStore.isLoggedIn"
        class="p-3.5 sm:p-4 rounded-2xl bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900/60 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 text-xs sm:text-sm"
      >
        <div class="flex items-center gap-2.5 text-amber-900 dark:text-amber-200 font-semibold">
          <Lock class="w-4 h-4 text-amber-600 dark:text-amber-400 shrink-0" />
          <span>Sign in to verify your architectural answer, build your daily streak, and schedule SM-2 reviews.</span>
        </div>
        <NuxtLink
          to="/login"
          class="w-full sm:w-auto text-center px-4 py-2 rounded-xl bg-amber-500 hover:bg-amber-400 text-slate-950 font-bold text-xs shrink-0 shadow transition-transform active:scale-95"
        >
          Sign In Now
        </NuxtLink>
      </div>

      <!-- Submit Action Bar (Before Review) -->
      <div v-if="!isReviewed" class="flex justify-end pt-2">
        <button
          type="button"
          @click="handleOptionSubmit"
          :disabled="selectedOption === null || focusStore.isSubmitting"
          class="w-full sm:w-auto flex items-center justify-center gap-2 px-7 py-3.5 rounded-2xl bg-brand-600 hover:bg-brand-500 disabled:opacity-40 disabled:cursor-not-allowed text-white font-semibold text-sm shadow-lg shadow-brand-500/20 transition-all active:scale-[0.98]"
        >
          <span v-if="focusStore.isSubmitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
          <ArrowRight v-else class="w-4 h-4" />
          <span>
            {{ focusStore.isSubmitting ? $t('today.option_submitting') : $t('today.submit_option') }}
          </span>
        </button>
      </div>

      <!-- Post-Submission Feedback & Deep-Dive Explanation -->
      <div v-else class="space-y-5 pt-2 animate-in fade-in slide-in-from-bottom-2 duration-300">
        <!-- Result Banner -->
        <div
          :class="[
            'p-4 sm:p-5 rounded-2xl border flex items-start gap-3.5',
            isCorrect
              ? 'bg-emerald-50 dark:bg-emerald-950/40 border-emerald-200 dark:border-emerald-800 text-emerald-950 dark:text-emerald-200'
              : 'bg-amber-50 dark:bg-amber-950/40 border-amber-200 dark:border-amber-800 text-amber-950 dark:text-amber-200'
          ]"
        >
          <CheckCircle2 v-if="isCorrect" class="w-6 h-6 text-emerald-600 dark:text-emerald-400 shrink-0 mt-0.5" />
          <AlertCircle v-else class="w-6 h-6 text-amber-600 dark:text-amber-400 shrink-0 mt-0.5" />

          <div class="space-y-1">
            <h3 class="font-bold text-sm sm:text-base">
              {{ isCorrect ? $t('today.correct_solution') : $t('today.incorrect_solution') }}
            </h3>
            <p class="text-xs sm:text-sm text-slate-700 dark:text-slate-300 leading-relaxed">
              {{ isCorrect ? 'Your senior engineering analysis matches optimal production best practices.' : $t('today.scheduled_sm2') }}
            </p>
          </div>
        </div>

        <!-- Architectural Deep-Dive Explanation Card -->
        <div v-if="question.explanationMarkdown" class="p-5 sm:p-6 rounded-3xl bg-slate-50 dark:bg-slate-950/50 border border-slate-200 dark:border-slate-800 shadow-sm space-y-3">
          <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400">
            <Sparkles class="w-4 h-4" />
            <span>{{ $t('today.correct_explanation_header') }}</span>
          </div>

          <div
            class="prose dark:prose-invert max-w-none text-xs sm:text-sm leading-relaxed text-slate-700 dark:text-slate-300"
            v-html="renderedExplanation"
          ></div>
        </div>
      </div>
    </div>

    <!-- Legacy Free-Text / Voice Mode Fallback (if no options provided) -->
    <div v-else class="space-y-6 flex-1 flex flex-col justify-between">
      <div v-if="!isReviewed" class="flex-1 flex flex-col justify-between space-y-4">
        <div class="flex p-1 rounded-xl bg-slate-100 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 text-xs font-semibold w-fit">
          <button
            @click="inputMode = 'write'"
            :class="[
              'flex items-center gap-1.5 px-3 py-1.5 rounded-lg transition-colors outline-none',
              inputMode === 'write' ? 'bg-white dark:bg-slate-800 text-brand-700 dark:text-brand-400 shadow-sm font-bold' : 'text-slate-500 hover:text-slate-900 dark:hover:text-slate-300'
            ]"
          >
            <PenTool class="w-3.5 h-3.5" />
            <span>{{ $t('today.write_mode') }}</span>
          </button>
          <button
            @click="inputMode = 'voice'"
            :class="[
              'flex items-center gap-1.5 px-3 py-1.5 rounded-lg transition-colors outline-none',
              inputMode === 'voice' ? 'bg-white dark:bg-slate-800 text-brand-700 dark:text-brand-400 shadow-sm font-bold' : 'text-slate-500 hover:text-slate-900 dark:hover:text-slate-300'
            ]"
          >
            <Mic class="w-3.5 h-3.5" />
            <span>{{ $t('today.voice_mode') }}</span>
          </button>
        </div>

        <div v-if="inputMode === 'write'" class="flex-1 min-h-[300px]">
          <CodeMirrorEditor
            v-model="answerText"
            :placeholder="$t('today.editor_placeholder')"
          />
        </div>

        <div v-else class="flex-1 flex items-center justify-center min-h-[250px]">
          <AudioRecorderModal @update:audio="handleAudioUpdate" />
        </div>

        <div class="flex justify-end pt-2">
          <button
            @click="handleLegacySubmit"
            :disabled="focusStore.isSubmitting"
            class="flex items-center gap-2 px-7 py-3.5 rounded-2xl bg-brand-600 hover:bg-brand-500 disabled:opacity-40 text-white font-semibold text-sm shadow-lg shadow-brand-500/20"
          >
            <Send class="w-4 h-4" />
            <span>{{ focusStore.isSubmitting ? $t('today.submitting') : $t('today.submit_drill') }}</span>
          </button>
        </div>
      </div>

      <div v-else class="space-y-4">
        <ScorecardAiReview v-if="drill.aiReview" :review="drill.aiReview" />
      </div>
    </div>
  </div>
</template>
