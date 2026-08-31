<script setup lang="ts">
import { ref, computed } from 'vue'
import { Terminal, PenTool, Mic, Send, CheckCircle2, ChevronDown, Sparkles } from 'lucide-vue-next'
import confetti from 'canvas-confetti'
import type { InterviewQuestion, DailyDrill } from '~/stores/useDailyFocusStore'
import CodeMirrorEditor from '~/components/today/CodeMirrorEditor.vue'
import AudioRecorderModal from '~/components/today/AudioRecorderModal.vue'
import ScorecardAiReview from '~/components/today/ScorecardAiReview.vue'

const props = defineProps<{
  question: InterviewQuestion
  drill: DailyDrill
}>()

const focusStore = useDailyFocusStore()
const { locale } = useI18n()

const inputMode = ref<'write' | 'voice'>('write')
const answerText = ref(props.drill.userAnswerText || '')
const audioBase64 = ref<string | null>(null)
const audioMimeType = ref<string>('audio/webm')
const showExpectedPoints = ref(false)

const isReviewed = computed(() => props.drill.status === 2 && props.drill.aiReview)
const canSubmit = computed(() => {
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
  <div class="h-full flex flex-col bg-slate-950 p-5 md:p-8 overflow-y-auto space-y-6">
    <!-- Question Header -->
    <div>
      <div class="flex items-center gap-2 text-xs font-semibold text-emerald-400 uppercase tracking-wider mb-2">
        <Terminal class="w-3.5 h-3.5" />
        <span>{{ $t('today.interview_challenge') }}</span>
      </div>

      <h2 class="text-lg md:text-xl font-bold text-white leading-snug">
        {{ question.questionText }}
      </h2>

      <!-- Expected Key Points Accordion -->
      <div class="mt-3">
        <button
          @click="showExpectedPoints = !showExpectedPoints"
          class="flex items-center gap-2 text-xs font-medium text-slate-400 hover:text-slate-200 transition-colors"
        >
          <Sparkles class="w-3.5 h-3.5 text-brand-400" />
          <span>{{ $t('today.expected_points') }} ({{ question.expectedKeyPoints?.length || 0 }})</span>
          <ChevronDown :class="['w-3.5 h-3.5 transition-transform', showExpectedPoints ? 'rotate-180' : '']" />
        </button>

        <ul
          v-if="showExpectedPoints"
          class="mt-2.5 p-3.5 rounded-xl bg-slate-900/80 border border-slate-800 space-y-1.5 animate-in fade-in"
        >
          <li
            v-for="(point, i) in question.expectedKeyPoints"
            :key="i"
            class="text-xs text-slate-300 flex items-start gap-2"
          >
            <span class="w-1 h-1 rounded-full bg-brand-400 mt-2 shrink-0"></span>
            <span>{{ point }}</span>
          </li>
        </ul>
      </div>
    </div>

    <!-- Mode Selector Tabs (if not already reviewed) -->
    <div v-if="!isReviewed" class="flex items-center justify-between pt-2">
      <div class="flex items-center p-1 rounded-xl bg-slate-900 border border-slate-800">
        <button
          @click="inputMode = 'write'"
          :class="[
            'flex items-center gap-2 px-3.5 py-1.5 rounded-lg text-xs font-semibold transition-all',
            inputMode === 'write' ? 'bg-brand-600 text-slate-950 shadow-sm' : 'text-slate-400 hover:text-white'
          ]"
        >
          <PenTool class="w-3.5 h-3.5" />
          <span>{{ $t('today.write_mode') }}</span>
        </button>

        <button
          @click="inputMode = 'voice'"
          :class="[
            'flex items-center gap-2 px-3.5 py-1.5 rounded-lg text-xs font-semibold transition-all',
            inputMode === 'voice' ? 'bg-brand-600 text-slate-950 shadow-sm' : 'text-slate-400 hover:text-white'
          ]"
        >
          <Mic class="w-3.5 h-3.5" />
          <span>{{ $t('today.voice_mode') }}</span>
        </button>
      </div>

      <div class="text-xs text-slate-500 font-mono">
        {{ inputMode === 'write' ? `${answerText.length} chars` : '1-Pass Multimodal' }}
      </div>
    </div>

    <!-- Input Area -->
    <div v-if="!isReviewed" class="flex-1 min-h-[280px] flex flex-col">
      <div v-if="inputMode === 'write'" class="flex-1 h-full min-h-[300px]">
        <CodeMirrorEditor
          v-model="answerText"
          :placeholder="$t('today.editor_placeholder')"
        />
      </div>

      <div v-else class="flex-1 flex items-center justify-center">
        <AudioRecorderModal @update:audio="handleAudioUpdate" />
      </div>

      <!-- Submit button -->
      <div class="mt-4 flex justify-end">
        <button
          @click="handleSubmit"
          :disabled="!canSubmit"
          class="flex items-center gap-2 px-6 py-3 rounded-xl bg-brand-500 hover:bg-brand-400 disabled:opacity-40 disabled:cursor-not-allowed text-slate-950 font-bold text-sm shadow-lg shadow-brand-500/20 transition-all active:scale-[0.98]"
        >
          <Send v-if="!focusStore.isSubmitting" class="w-4 h-4" />
          <span v-if="focusStore.isSubmitting" class="w-4 h-4 border-2 border-slate-950 border-t-transparent rounded-full animate-spin"></span>
          <span>{{ focusStore.isSubmitting ? $t('today.submitting') : $t('today.submit_drill') }}</span>
        </button>
      </div>
    </div>

    <!-- AI Review Scorecard (when completed) -->
    <div v-else class="space-y-4">
      <div class="flex items-center gap-2 p-3 rounded-xl bg-emerald-950/40 border border-emerald-800 text-xs font-semibold text-emerald-300">
        <CheckCircle2 class="w-4 h-4" />
        <span>{{ $t('today.reviewed') }}</span>
      </div>

      <ScorecardAiReview :review="drill.aiReview!" />
    </div>
  </div>
</template>
