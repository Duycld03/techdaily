<script setup lang="ts">
import { ref } from 'vue'
import { Copy, Check } from 'lucide-vue-next'

const props = withDefaults(
  defineProps<{
    code: string
    language?: string
  }>(),
  {
    language: 'csharp'
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
        <span class="ml-2 font-mono uppercase tracking-wider text-[11px] text-slate-500 font-semibold">{{ language }}</span>
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

    <!-- Code Body -->
    <pre class="p-4 sm:p-5 overflow-x-auto leading-relaxed text-slate-200 selection:bg-indigo-500/30"><code>{{ code }}</code></pre>
  </div>
</template>
