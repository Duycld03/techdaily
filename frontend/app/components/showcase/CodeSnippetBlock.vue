<script setup lang="ts">
import { ref } from 'vue'
import { Copy, Check, FileCode2 } from 'lucide-vue-next'

const props = withDefaults(
  defineProps<{
    code: string
    filename?: string
    language?: string
  }>(),
  {
    filename: undefined,
    language: 'TypeScript'
  }
)

const copied = ref(false)
let resetTimer: ReturnType<typeof setTimeout> | null = null

async function copy() {
  try {
    await navigator.clipboard.writeText(props.code)
  } catch {
    // Fallback for environments without the async clipboard API
    const ta = document.createElement('textarea')
    ta.value = props.code
    ta.style.position = 'fixed'
    ta.style.opacity = '0'
    document.body.appendChild(ta)
    ta.select()
    document.execCommand('copy')
    document.body.removeChild(ta)
  }
  copied.value = true
  if (resetTimer) clearTimeout(resetTimer)
  resetTimer = setTimeout(() => (copied.value = false), 1800)
}
</script>

<template>
  <div
    class="overflow-hidden rounded-xl border border-slate-200/80 bg-[#0d0d12] dark:border-white/[0.08]"
  >
    <!-- Header bar -->
    <div
      class="flex items-center justify-between gap-3 border-b border-white/[0.08] bg-white/[0.02] px-3 py-2"
    >
      <div class="flex items-center gap-2 min-w-0">
        <FileCode2 class="h-3.5 w-3.5 shrink-0 text-slate-500" :stroke-width="1.5" />
        <span
          v-if="filename"
          class="truncate font-mono text-xs font-medium text-slate-300"
          >{{ filename }}</span
        >
        <span
          v-else
          class="rounded bg-white/[0.06] px-1.5 py-0.5 font-mono text-[11px] font-medium text-slate-400"
          >{{ language }}</span
        >
      </div>
      <button
        type="button"
        class="flex h-7 shrink-0 items-center gap-1.5 rounded-md border border-white/[0.08] px-2 text-xs font-medium text-slate-300 transition-colors hover:bg-white/[0.06] focus:outline-none focus-visible:ring-1 focus-visible:ring-brand-400"
        :aria-label="copied ? 'Copied to clipboard' : 'Copy code'"
        @click="copy"
      >
        <Check v-if="copied" class="h-3.5 w-3.5 text-emerald-400" :stroke-width="2" />
        <Copy v-else class="h-3.5 w-3.5" :stroke-width="1.5" />
        <span :class="copied ? 'text-emerald-400' : ''">{{ copied ? 'Copied!' : 'Copy' }}</span>
      </button>
    </div>

    <!-- Code body -->
    <div class="showcase-code-scroll overflow-x-auto">
      <pre
        class="px-3.5 py-3 font-mono text-xs leading-relaxed text-slate-200 sm:text-sm"
      ><code>{{ code }}</code></pre>
    </div>
  </div>
</template>

<style scoped>
.showcase-code-scroll::-webkit-scrollbar {
  height: 6px;
}
.showcase-code-scroll::-webkit-scrollbar-track {
  background: transparent;
}
.showcase-code-scroll::-webkit-scrollbar-thumb {
  background: rgba(255, 255, 255, 0.14);
  border-radius: 9999px;
}
.showcase-code-scroll::-webkit-scrollbar-thumb:hover {
  background: rgba(255, 255, 255, 0.24);
}
.showcase-code-scroll {
  scrollbar-width: thin;
  scrollbar-color: rgba(255, 255, 255, 0.14) transparent;
}
pre {
  margin: 0;
}
</style>
