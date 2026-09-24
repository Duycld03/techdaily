<script setup lang="ts">
import { ref, computed, watch, onMounted } from "vue";
import { useTimeoutFn } from "@vueuse/core";
import { Copy, Check, FileCode2 } from "lucide-vue-next";
import {
  highlightCode,
  detectCodeLanguage,
  formatLanguageLabel,
} from "~/utils/shikiHighlighter";

const props = withDefaults(
  defineProps<{
    code: string;
    language?: string;
    category?: number;
    tags?: string[];
  }>(),
  {
    language: "auto",
  },
);

const normalizedCode = computed(() => {
  return props.code ? props.code.normalize('NFC') : ''
})

const detectedLanguage = computed(() => {
  return detectCodeLanguage(
    normalizedCode.value,
    props.language,
    props.tags,
    props.category,
  );
});

const displayLabel = computed(() => {
  return formatLanguageLabel(detectedLanguage.value);
});

const highlightedHtml = ref("");

async function updateHighlighting() {
  if (!normalizedCode.value) {
    highlightedHtml.value = "";
    return;
  }
  const html = await highlightCode(normalizedCode.value, detectedLanguage.value);
  highlightedHtml.value = html;
}

onMounted(() => {
  updateHighlighting();
});

watch(
  () => [normalizedCode.value, detectedLanguage.value],
  () => {
    updateHighlighting();
  },
);

const copied = ref(false);
const { start: startResetTimer } = useTimeoutFn(() => {
  copied.value = false;
}, 2000, { immediate: false });

async function copyCode() {
  if (!normalizedCode.value) return;
  try {
    if (typeof navigator !== "undefined" && navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(normalizedCode.value);
    }
    copied.value = true;
    startResetTimer();
  } catch {
    // Fallback
  }
}
</script>

<template>
  <div
    class="code-block-wrapper relative group rounded-2xl bg-slate-50/90 dark:bg-canvas-subtle text-slate-900 dark:text-slate-100 overflow-hidden font-mono text-xs sm:text-sm border border-slate-200/80 dark:border-white/[0.08] shadow-lg dark:shadow-2xl"
  >
    <!-- Code Header -->
    <div
      class="flex items-center justify-between px-3.5 sm:px-4 py-2 bg-slate-100/90 dark:bg-canvas-elevated/90 backdrop-blur-md border-b border-slate-200/80 dark:border-white/[0.06] text-xs select-none gap-2"
    >
      <div class="flex items-center gap-2 min-w-0">
        <slot name="left">
          <FileCode2 class="h-4 w-4 shrink-0 text-brand-500 dark:text-brand-400" />
          <span
            class="font-mono uppercase tracking-wider text-[11px] sm:text-xs text-slate-700 dark:text-slate-300 font-bold truncate"
          >{{ displayLabel }}</span>
        </slot>
      </div>

      <div class="flex items-center gap-1.5 sm:gap-2 shrink-0">
        <span class="hidden sm:inline-block rounded bg-slate-200/80 dark:bg-white/[0.06] px-1.5 py-0.5 font-mono text-[11px] font-medium text-slate-600 dark:text-slate-400">
          {{ displayLabel }}
        </span>
        <button
          @click="copyCode"
          type="button"
          class="copy-code-btn flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-white/80 dark:bg-white/[0.06] hover:bg-white dark:hover:bg-white/[0.12] border border-slate-200/80 dark:border-white/[0.08] text-slate-700 dark:text-slate-200 text-xs font-semibold shadow-xs transition-all active:scale-95 cursor-pointer"
          :title="copied ? 'Copied!' : 'Copy Code'"
        >
          <Check v-if="copied" class="w-3.5 h-3.5 text-emerald-500 dark:text-emerald-400" :stroke-width="1.5" />
          <Copy v-else class="w-3.5 h-3.5 text-slate-500 dark:text-slate-400" :stroke-width="1.5" />
          <span class="text-xs font-medium">{{
            copied ? "Copied!" : "Copy"
          }}</span>
        </button>
      </div>
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
    ><code>{{ normalizedCode }}</code></pre>
  </div>
</template>

<style scoped>
:deep(.shiki-container pre.shiki),
:deep(pre.shiki) {
  background-color: transparent !important;
  margin: 0 !important;
  padding: 0 !important;
  overflow-x: visible !important;
  font-family: inherit !important;
  font-size: inherit !important;
  line-height: inherit !important;
}
:deep(.shiki-container code),
:deep(code) {
  font-family: inherit !important;
    background-color: transparent !important;
  }
  :deep(.dark .shiki),
  :deep(.dark .shiki span),
  :deep(html.dark .shiki),
  :deep(html.dark .shiki span) {
    color: var(--shiki-dark) !important;
  }
</style>
