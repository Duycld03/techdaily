<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { Copy, Check } from 'lucide-vue-next'
import { highlightCode } from '~/utils/shikiHighlighter'

const props = withDefaults(
  defineProps<{
    code: string
    language?: string
    category?: number
    tags?: string[]
  }>(),
  {
    language: 'auto'
  }
)

const detectedLanguage = computed(() => {
  if (props.language && props.language !== 'auto') {
    return props.language.toLowerCase()
  }

  const code = props.code || ''
  const trimmed = code.trim()

  // Heuristic 1: SQL / PostgreSQL
  if (
    props.category === 2 ||
    props.tags?.some(t => ['sql', 'postgres', 'postgresql', 'database', 'indexing'].includes(t.toLowerCase())) ||
    trimmed.startsWith('--') ||
    /\b(SELECT|INSERT|UPDATE|DELETE|CREATE TABLE|CREATE INDEX|ALTER TABLE|FILLFACTOR|EXPLAIN ANALYZE|VACUUM|INCLUDE)\b/i.test(trimmed)
  ) {
    return 'sql'
  }

  // Heuristic 2: Vue / TypeScript / JavaScript / JSX
  if (
    props.category === 0 ||
    props.tags?.some(t => ['vue', 'vue3', 'nuxt', 'react', 'javascript', 'typescript', 'frontend', 'dom'].includes(t.toLowerCase())) ||
    /\b(const |let |ref<|shallowRef|reactive|triggerRef|computed<|defineProps|import |export default|socket\.on|useState|useEffect)\b/.test(trimmed)
  ) {
    return 'typescript'
  }

  // Heuristic 3: C# / .NET
  if (
    props.category === 1 ||
    props.tags?.some(t => ['csharp', 'csharp13', 'dotnet', 'dotnet10', 'aspnet', 'efcore'].includes(t.toLowerCase())) ||
    /\b(public async Task|public void|Span<|ReadOnlySpan<|stackalloc|ArrayPool<|Channel<|using var|class |namespace |\[Fact\]|\[HttpGet\])\b/.test(trimmed)
  ) {
    return 'csharp'
  }

  // Heuristic 4: JSON
  if (trimmed.startsWith('{') && trimmed.endsWith('}') || trimmed.startsWith('[') && trimmed.endsWith(']')) {
    try {
      JSON.parse(trimmed)
      return 'json'
    } catch {
      // not strict json
    }
  }

  return 'csharp'
})

const displayLabel = computed(() => {
  switch (detectedLanguage.value) {
    case 'sql':
    case 'postgresql':
      return 'PostgreSQL / SQL'
    case 'typescript':
    case 'ts':
      return 'TypeScript'
    case 'javascript':
    case 'js':
      return 'JavaScript'
    case 'vue':
      return 'Vue 3'
    case 'csharp':
    case 'cs':
      return 'C# / .NET 10'
    case 'json':
      return 'JSON'
    case 'bash':
    case 'sh':
      return 'Bash / Shell'
    default:
      return detectedLanguage.value.toUpperCase()
  }
})

const highlightedHtml = ref('')

async function updateHighlighting() {
  if (!props.code) {
    highlightedHtml.value = ''
    return
  }
  const html = await highlightCode(props.code, detectedLanguage.value)
  highlightedHtml.value = html
}

onMounted(() => {
  updateHighlighting()
})

watch(
  () => [props.code, detectedLanguage.value],
  () => {
    updateHighlighting()
  }
)

const copied = ref(false)

async function copyCode() {
  if (!props.code) return
  try {
    await navigator.clipboard.writeText(props.code)
    copied.value = true
    setTimeout(() => {
      copied.value = false
    }, 2000)
  } catch {
    // Fallback
  }
}
</script>

<template>
  <div class="relative group rounded-2xl bg-slate-900 text-slate-100 overflow-hidden font-mono text-xs sm:text-sm border border-slate-800">
    <!-- Code Header -->
    <div class="flex items-center justify-between px-4 py-2 bg-slate-950/80 border-b border-slate-800/80 text-xs text-slate-400 select-none">
      <div class="flex items-center gap-2">
        <span class="w-2.5 h-2.5 rounded-full bg-rose-500/80"></span>
        <span class="w-2.5 h-2.5 rounded-full bg-amber-500/80"></span>
        <span class="w-2.5 h-2.5 rounded-full bg-emerald-500/80"></span>
        <span class="ml-2 font-mono uppercase tracking-wider text-[11px] text-slate-400 font-semibold">{{ displayLabel }}</span>
      </div>

      <button
        @click="copyCode"
        class="flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs transition-all active:scale-95"
        :title="copied ? 'Copied!' : 'Copy Code'"
      >
        <Check v-if="copied" class="w-3.5 h-3.5 text-emerald-400" />
        <Copy v-else class="w-3.5 h-3.5 text-slate-400" />
        <span class="text-[11px]">{{ copied ? 'Copied!' : 'Copy' }}</span>
      </button>
    </div>

    <!-- Highlighted Code Body -->
    <div
      v-if="highlightedHtml"
      class="shiki-container p-4 sm:p-5 overflow-x-auto text-xs sm:text-sm leading-relaxed"
      v-html="highlightedHtml"
    ></div>
    <pre
      v-else
      class="p-4 sm:p-5 overflow-x-auto leading-relaxed text-slate-200 selection:bg-indigo-500/30"
    ><code>{{ code }}</code></pre>
  </div>
</template>

<style>
.shiki-container pre.shiki {
  background-color: transparent !important;
  margin: 0 !important;
  padding: 0 !important;
  overflow-x: visible !important;
  font-family: inherit !important;
  font-size: inherit !important;
  line-height: inherit !important;
}
.shiki-container code {
  font-family: inherit !important;
  background-color: transparent !important;
}
</style>
