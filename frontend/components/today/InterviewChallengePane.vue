<script setup lang="ts">
import { ref, computed } from 'vue'
import { Terminal, PenTool, Mic, Send, CheckCircle2, ChevronDown, Sparkles, Lock } from 'lucide-vue-next'
import confetti from 'canvas-confetti'
import type { InterviewQuestion, DailyDrill } from '~/stores/useDailyFocusStore'
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

const inputMode = ref<'write' | 'voice'>('write')
const answerText = ref(props.drill.userAnswerText || '')
const audioBase64 = ref<string | null>(null)
const audioMimeType = ref<string>('audio/webm')
const showExpectedPoints = ref(false)

const isReviewed = computed(() => props.drill.status === 2 && props.drill.aiReview)
const canSubmit = computed(() => {
  if (!authStore.isLoggedIn) return true
  if (focusStore.isSubmitting) return false
  if (inputMode.value === 'write') return answerText.value.trim().length >= 10
  return !!audioBase64.value
})

function handleAudioUpdate(blob: Blob | null, b64: string | null) {
  audioBase64.value = b64
  if (blob) {
    audioMimeType.value = blob.type
  }
}

async function handleSubmit() {
  if (!authStore.isLoggedIn) {
    router.push({ path: '/login', query: { redirect: '/today' } })
    return
  }

  if (!canSubmit.value) return

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
  <div class="h-full flex flex-col bg-white dark:bg-slate-900/60 p-6 md:p-9 overflow-y-auto space-y-6 transition-colors duration-200">
    <!-- Header -->
    <div class="space-y-3">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2 text-xs sm:text-sm font-bold text-brand-600 dark:text-brand-400 uppercase tracking-wider">
          <Terminal class="w-4 h-4" />
          <span>{{ $t('today.interview_challenge') }}</span>
        </div>

        <!-- Mode Toggle Tabs (Write vs Voice) -->
        <div v-if="!isReviewed" class="flex p-1 rounded-xl bg-slate-100 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 text-xs font-semibold">
          <button
            @click="inputMode = 'write'"
            :class="[
              'flex items-center gap-1.5 px-3 py-1.5 rounded-lg transition-colors outline-none focus:outline-none',
              inputMode === 'write' ? 'bg-white dark:bg-slate-800 text-brand-700 dark:text-brand-400 shadow-sm font-bold' : 'text-slate-500 hover:text-slate-900 dark:hover:text-slate-300'
            ]"
          >
            <PenTool class="w-3.5 h-3.5" />
            <span>{{ $t('today.write_mode') }}</span>
          </button>
          <button
            @click="inputMode = 'voice'"
            :class="[
              'flex items-center gap-1.5 px-3 py-1.5 rounded-lg transition-colors outline-none focus:outline-none',
              inputMode === 'voice' ? 'bg-white dark:bg-slate-800 text-brand-700 dark:text-brand-400 shadow-sm font-bold' : 'text-slate-500 hover:text-slate-900 dark:hover:text-slate-300'
            ]"
          >
            <Mic class="w-3.5 h-3.5" />
            <span>{{ $t('today.voice_mode') }}</span>
          </button>
        </div>
      </div>

      <!-- Question Text -->
      <h2 class="text-xl sm:text-2xl font-bold text-slate-900 dark:text-white leading-snug">
        {{ question.questionText }}
      </h2>

      <!-- Expected Key Points Accordion -->
      <div v-if="question.expectedKeyPoints?.length" class="border border-slate-200 dark:border-slate-800/80 rounded-2xl overflow-hidden bg-slate-50 dark:bg-slate-950/40">
        <button
          @click="showExpectedPoints = !showExpectedPoints"
          class="w-full flex items-center justify-between p-3.5 text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 hover:text-slate-900 dark:hover:text-white transition-colors"
        >
          <span class="flex items-center gap-2">
            <Sparkles class="w-4 h-4 text-brand-600 dark:text-brand-400" />
            <span>{{ $t('today.expected_points') }} ({{ question.expectedKeyPoints.length }})</span>
          </span>
          <ChevronDown
            :class="['w-4 h-4 text-slate-400 transition-transform duration-200', showExpectedPoints ? 'rotate-180' : '']"
          />
        </button>

        <div v-if="showExpectedPoints" class="px-4 pb-3.5 space-y-1.5 animate-in fade-in">
          <div
            v-for="(point, i) in question.expectedKeyPoints"
            :key="i"
            class="text-xs sm:text-sm text-slate-600 dark:text-slate-400 flex items-start gap-2"
          >
            <span class="w-1.5 h-1.5 rounded-full bg-brand-500 mt-2 shrink-0"></span>
            <span>{{ point }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Input Area -->
    <div v-if="!isReviewed" class="flex-1 min-h-[300px] flex flex-col justify-between">
      <div v-if="inputMode === 'write'" class="flex-1 h-full min-h-[320px]">
        <CodeMirrorEditor
          v-model="answerText"
          :placeholder="$t('today.editor_placeholder')"
        />
      </div>

      <div v-else class="flex-1 flex items-center justify-center">
        <AudioRecorderModal @update:audio="handleAudioUpdate" />
      </div>

      <!-- Guest Sign-in prompt banner if not logged in -->
      <div v-if="!authStore.isLoggedIn" class="mt-4 p-4 rounded-2xl bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900/60 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 text-xs sm:text-sm">
        <div class="flex items-center gap-2.5 text-amber-900 dark:text-amber-200 font-semibold">
          <Lock class="w-4 h-4 text-amber-600 dark:text-amber-400 shrink-0" />
          <span>Sign in to evaluate with Gemini 3.5 Flash, save your scorecard, and build your daily streak.</span>
        </div>
        <NuxtLink
          to="/login"
          class="px-4 py-2 rounded-xl bg-amber-500 hover:bg-amber-400 text-slate-950 font-bold text-xs shrink-0 shadow transition-transform active:scale-95"
        >
          Sign In Now
        </NuxtLink>
      </div>

      <!-- Submit button -->
      <div class="mt-4 flex justify-end">
        <button
          @click="handleSubmit"
          :disabled="!canSubmit"
          class="flex items-center gap-2 px-7 py-3.5 rounded-2xl bg-brand-600 hover:bg-brand-500 disabled:opacity-40 disabled:cursor-not-allowed text-white font-semibold text-sm shadow-lg shadow-brand-500/20 transition-all active:scale-[0.98]"
        >
          <Lock v-if="!authStore.isLoggedIn" class="w-4 h-4" />
          <Send v-else-if="!focusStore.isSubmitting" class="w-4 h-4" />
          <span v-if="focusStore.isSubmitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
          <span>
            {{ !authStore.isLoggedIn ? 'Sign In to Submit with AI' : (focusStore.isSubmitting ? $t('today.submitting') : $t('today.submit_drill')) }}
          </span>
        </button>
      </div>
    </div>

    <!-- AI Review Scorecard (when completed) -->
    <div v-else class="space-y-4">
      <div class="flex items-center gap-2 p-3.5 rounded-2xl bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800 text-xs sm:text-sm font-bold text-emerald-800 dark:text-emerald-300">
        <CheckCircle2 class="w-4 h-4" />
        <span>{{ $t('today.reviewed') }}</span>
      </div>

      <ScorecardAiReview :review="drill.aiReview!" />
    </div>
  </div>
</template>
