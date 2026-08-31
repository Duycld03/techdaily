<script setup lang="ts">
import { Mic, Square, Play, Trash2, Volume2 } from 'lucide-vue-next'
import { useAudioRecorder } from '~/composables/useAudioRecorder'

const props = defineProps<{
  disabled?: boolean
}>()

const emit = defineEmits<{
  (e: 'update:audio', blob: Blob | null, base64: string | null): void
}>()

const {
  isRecording,
  durationSeconds,
  audioBlob,
  audioUrl,
  error,
  startRecording,
  stopRecording,
  reset,
  blobToBase64
} = useAudioRecorder()

async function handleStop() {
  stopRecording()
  // Wait a tick for blob to be generated
  setTimeout(async () => {
    if (audioBlob.value) {
      const b64 = await blobToBase64(audioBlob.value)
      emit('update:audio', audioBlob.value, b64)
    }
  }, 100)
}

function handleReset() {
  reset()
  emit('update:audio', null, null)
}

function formatDuration(seconds: number): string {
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`
}
</script>

<template>
  <div class="p-6 rounded-2xl bg-slate-900 border border-slate-800 flex flex-col items-center justify-center text-center">
    <div class="mb-4">
      <div
        :class="[
          'w-16 h-16 rounded-full flex items-center justify-center transition-all shadow-lg',
          isRecording
            ? 'bg-red-500 text-white animate-pulse shadow-red-500/30'
            : audioBlob
            ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/40'
            : 'bg-slate-800 text-slate-300'
        ]"
      >
        <Mic class="w-7 h-7" />
      </div>
    </div>

    <!-- Timer -->
    <div class="text-2xl font-mono font-bold text-white mb-2">
      {{ formatDuration(durationSeconds) }}
    </div>

    <p class="text-xs text-slate-400 max-w-xs mb-6">
      {{ isRecording ? 'Recording your Senior engineering response... Speak clearly.' : 'Record your spoken answer. Evaluated multimodal by Gemini Flash.' }}
    </p>

    <!-- Error message -->
    <div v-if="error" class="text-xs text-red-400 bg-red-950/40 p-2.5 rounded-xl border border-red-900 mb-4">
      {{ error }}
    </div>

    <!-- Controls -->
    <div class="flex items-center gap-3">
      <button
        v-if="!isRecording && !audioBlob"
        @click="startRecording"
        :disabled="disabled"
        class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-slate-950 font-bold text-sm shadow-md transition-colors"
      >
        <Mic class="w-4 h-4" />
        <span>Start Recording</span>
      </button>

      <button
        v-else-if="isRecording"
        @click="handleStop"
        class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-red-600 hover:bg-red-500 text-white font-bold text-sm shadow-md transition-colors animate-pulse"
      >
        <Square class="w-4 h-4 fill-white" />
        <span>Finish Recording</span>
      </button>

      <div v-if="audioBlob && !isRecording" class="flex flex-col items-center gap-3 w-full">
        <audio v-if="audioUrl" :src="audioUrl" controls class="w-full max-w-sm rounded-lg" />
        <button
          @click="handleReset"
          class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold text-slate-400 hover:text-red-400 hover:bg-slate-800 transition-colors"
        >
          <Trash2 class="w-3.5 h-3.5" />
          <span>Re-record</span>
        </button>
      </div>
    </div>
  </div>
</template>
