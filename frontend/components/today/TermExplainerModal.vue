<script setup lang="ts">
import { ref } from 'vue'
import { Sparkles, X, Check, Copy } from 'lucide-vue-next'

const props = defineProps<{
  term: string
  category: string
  context: string
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const focusStore = useDailyFocusStore()
const { locale } = useI18n()

const explanation = ref<string | null>(null)
const isLoading = ref(false)
const copied = ref(false)

async function loadExplanation() {
  isLoading.value = true
  try {
    const res = await focusStore.explainTerm(props.term, props.category, props.context, locale.value)
    explanation.value = res.explanation
  } catch {
    explanation.value = 'Could not load explanation at this moment.'
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  loadExplanation()
})

function copyText() {
  if (explanation.value) {
    navigator.clipboard.writeText(explanation.value)
    copied.value = true
    setTimeout(() => (copied.value = false), 2000)
  }
}
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
    <div class="w-full max-w-md bg-slate-900 border border-slate-800 rounded-2xl shadow-2xl p-5 overflow-hidden animate-in fade-in zoom-in-95 duration-200">
      <!-- Header -->
      <div class="flex items-center justify-between pb-3 mb-3 border-b border-slate-800">
        <div class="flex items-center gap-2">
          <div class="p-1.5 rounded-lg bg-brand-500/10 text-brand-400 border border-brand-500/20">
            <Sparkles class="w-4 h-4" />
          </div>
          <div>
            <span class="text-xs font-semibold uppercase tracking-wider text-brand-400">{{ category }}</span>
            <h3 class="text-base font-bold text-white leading-tight font-mono">{{ term }}</h3>
          </div>
        </div>
        <button
          @click="emit('close')"
          class="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
        >
          <X class="w-4 h-4" />
        </button>
      </div>

      <!-- Body -->
      <div class="py-2">
        <div v-if="isLoading" class="flex items-center gap-3 py-6 justify-center text-slate-400 text-sm">
          <span class="w-2 h-2 rounded-full bg-brand-400 animate-ping"></span>
          <span>Generating AI explanation...</span>
        </div>

        <div v-else class="space-y-3">
          <p class="text-sm text-slate-200 leading-relaxed bg-slate-950/50 p-4 rounded-xl border border-slate-800/80">
            {{ explanation }}
          </p>
        </div>
      </div>

      <!-- Footer -->
      <div class="flex items-center justify-between pt-3 mt-2 border-t border-slate-800 text-xs text-slate-500">
        <span>Powered by Gemini 2.5 Flash Cache</span>
        <button
          @click="copyText"
          class="flex items-center gap-1.5 px-3 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 transition-colors"
        >
          <Check v-if="copied" class="w-3.5 h-3.5 text-emerald-400" />
          <Copy v-else class="w-3.5 h-3.5" />
          <span>{{ copied ? 'Copied' : 'Copy' }}</span>
        </button>
      </div>
    </div>
  </div>
</template>
