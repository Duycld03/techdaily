<script setup lang="ts">
import { ref, computed, watch, onMounted } from "vue";
import { useTimeoutFn } from "@vueuse/core";
import { Copy, Check } from "lucide-vue-next";
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

const detectedLanguage = computed(() => {
  return detectCodeLanguage(
    props.code,
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
  if (!props.code) {
    highlightedHtml.value = "";
    return;
  }
  const html = await highlightCode(props.code, detectedLanguage.value);
  highlightedHtml.value = html;
}

onMounted(() => {
  updateHighlighting();
});

watch(
  () => [props.code, detectedLanguage.value],
  () => {
    updateHighlighting();
  },
);

const copied = ref(false);
const { start: startResetTimer } = useTimeoutFn(() => {
  copied.value = false;
}, 2000, { immediate: false });

async function copyCode() {
  if (!props.code) return;
  try {
    if (typeof navigator !== "undefined" && navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(props.code);
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
      class="flex items-center justify-between px-4 sm:px-5 py-2.5 bg-slate-100/80 dark:bg-canvas-elevated/80 backdrop-blur-md border-b border-slate-200/80 dark:border-white/[0.06] text-xs select-none"
    >
      <div class="flex items-center gap-2">
        <span class="w-2.5 h-2.5 rounded-full bg-[#ff5f56]"></span>
        <span class="w-2.5 h-2.5 rounded-full bg-[#ffbd2e]"></span>
        <span class="w-2.5 h-2.5 rounded-full bg-[#27c93f]"></span>
        <span
          class="ml-2.5 font-mono uppercase tracking-widest text-[11px] sm:text-xs text-brand-600 dark:text-brand-400 font-bold"
          >{{ displayLabel }}</span
        >
      </div>

      <button
        @click="copyCode"
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
</style>
