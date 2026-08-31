<script setup lang="ts">
import { ref, onMounted } from 'vue'
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
  <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm" @click.self="emit('close')">
    <div class="w-full max-w-lg bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-3xl shadow-2xl p-6 sm:p-7 overflow-hidden animate-in fade-in zoom-in-95 duration-200 space-y-4 transition-colors">
      <!-- Header -->
      <div class="flex items-center justify-between pb-3 border-b border-slate-200 dark:border-slate-800">
        <div class="flex items-center gap-3">
          <div class="p-2 rounded-xl bg-brand-100 dark:bg-brand-500/10 text-brand-700 dark:text-brand-400 border border-brand-200 dark:border-brand-500/20">
            <Sparkles class="w-5 h-5" />
          </div>
          <div>
            <span class="text-xs font-bold uppercase tracking-wider text-brand-700 dark:text-brand-400">{{ category }}</span>
            <h3 class="text-lg font-bold text-slate-900 dark:text-white leading-tight font-mono">{{ term }}</h3>
          </div>
        </div>
        <button
          @click="emit('close')"
          class="p-2 rounded-xl text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
        >
          <X class="w-5 h-5" />
        </button>
      </div>

      <!-- Body -->
      <div class="py-2">
        <div v-if="isLoading" class="flex items-center gap-3 py-8 justify-center text-slate-500 dark:text-slate-400 text-sm">
          <span class="w-3 h-3 rounded-full bg-brand-500 animate-ping"></span>
          <span>Analyzing term with Gemini 3.5 Flash-Lite...</span>
        </div>

        <div v-else class="space-y-3">
          <p class="text-sm sm:text-base text-slate-800 dark:text-slate-200 leading-relaxed bg-slate-50 dark:bg-slate-950/80 p-5 rounded-2xl border border-slate-200 dark:border-slate-800/80">
            {{ explanation }}
          </p>
        </div>
      </div>

      <!-- Footer -->
      <div class="flex items-center justify-between pt-3 border-t border-slate-200 dark:border-slate-800 text-xs text-slate-500">
        <span class="font-medium">Powered by Gemini 3.5 Flash-Lite</span>
        <button
          @click="copyText"
          class="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 font-semibold text-xs transition-colors shadow-sm"
        >
          <Check v-if="copied" class="w-3.5 h-3.5 text-emerald-500" />
          <Copy v-else class="w-3.5 h-3.5" />
          <span>{{ copied ? 'Copied' : 'Copy Explanation' }}</span>
        </button>
      </div>
    </div>
  </div>
</template>
