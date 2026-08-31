<script setup lang="ts">
import { ref } from 'vue'
import { Mic, Square, Trash2, CheckCircle2 } from 'lucide-vue-next'
import { useAudioRecorder } from '~/composables/useAudioRecorder'

const emit = defineEmits<{
  (e: 'update:audio', blob: Blob | null, base64: string | null): void
}>()

const { isRecording, durationSeconds, error, startRecording, stopRecording } = useAudioRecorder()
const audioBlob = ref<Blob | null>(null)
const audioUrl = ref<string | null>(null)

async function handleStop() {
  const blob = await stopRecording()
  if (blob) {
    audioBlob.value = blob
    audioUrl.value = URL.createObjectURL(blob)

    // Convert to base64
    const reader = new FileReader()
    reader.readAsDataURL(blob)
    reader.onloadend = () => {
      const base64Data = reader.result as string
      // Strip metadata prefix (e.g. data:audio/webm;base64,)
      const base64Clean = base64Data.split(',')[1]
      emit('update:audio', blob, base64Clean)
    }
  }
}

function handleReset() {
  audioBlob.value = null
  if (audioUrl.value) {
    URL.revokeObjectURL(audioUrl.value)
    audioUrl.value = null
  }
  emit('update:audio', null, null)
}

function formatDuration(seconds: number): string {
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`
}
</script>

<template>
  <div class="p-6 rounded-3xl bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 flex flex-col items-center justify-center text-center transition-colors duration-200">
    <div class="mb-4">
      <div
        :class="[
          'w-16 h-16 rounded-full flex items-center justify-center transition-all shadow-lg',
          isRecording
            ? 'bg-rose-500 text-white animate-pulse shadow-rose-500/30'
            : audioBlob
            ? 'bg-emerald-100 dark:bg-emerald-500/20 text-emerald-700 dark:text-emerald-400 border border-emerald-300 dark:border-emerald-500/40'
            : 'bg-slate-200 dark:bg-slate-800 text-slate-600 dark:text-slate-300'
        ]"
      >
        <Mic class="w-7 h-7" />
      </div>
    </div>

    <!-- Timer -->
    <div class="text-2xl font-mono font-bold text-slate-900 dark:text-white mb-2">
      {{ formatDuration(durationSeconds) }}
    </div>

    <p class="text-xs text-slate-600 dark:text-slate-400 max-w-xs mb-6 font-medium">
      {{ isRecording ? 'Recording your Senior engineering response... Speak clearly.' : 'Record your spoken answer. Evaluated multimodal by Gemini Flash.' }}
    </p>

    <!-- Error message -->
    <div v-if="error" class="text-xs text-rose-700 dark:text-rose-400 bg-rose-50 dark:bg-rose-950/40 p-2.5 rounded-xl border border-rose-200 dark:border-rose-900 mb-4">
      {{ error }}
    </div>

    <!-- Controls -->
    <div class="flex items-center gap-3">
      <button
        v-if="!isRecording && !audioBlob"
        @click="startRecording"
        class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-sm shadow-md transition-colors"
      >
        <Mic class="w-4 h-4" />
        <span>Start Recording</span>
      </button>

      <button
        v-else-if="isRecording"
        @click="handleStop"
        class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-rose-600 hover:bg-rose-500 text-white font-bold text-sm shadow-md transition-colors animate-pulse"
      >
        <Square class="w-4 h-4 fill-white" />
        <span>Finish Recording</span>
      </button>

      <div v-if="audioBlob && !isRecording" class="flex flex-col items-center gap-3 w-full">
        <div class="flex items-center gap-2 text-xs font-bold text-emerald-700 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-950/40 px-3 py-1.5 rounded-xl border border-emerald-200 dark:border-emerald-800">
          <CheckCircle2 class="w-3.5 h-3.5" />
          <span>Audio Recorded ({{ formatDuration(durationSeconds) }})</span>
        </div>

        <audio v-if="audioUrl" :src="audioUrl" controls class="h-8 max-w-[260px]"></audio>

        <button
          @click="handleReset"
          class="flex items-center gap-1 text-xs font-semibold text-rose-600 dark:text-rose-400 hover:underline"
        >
          <Trash2 class="w-3 h-3" />
          <span>Re-record Answer</span>
        </button>
      </div>
    </div>
  </div>
</template>
