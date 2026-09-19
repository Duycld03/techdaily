<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { Sparkles, X, Check, Copy, AlertCircle, RotateCcw } from "lucide-vue-next";

const props = withDefaults(
  defineProps<{
    term: string;
    category?: string;
    context?: string;
  }>(),
  {
    category: "Software Architecture",
    context: "",
  },
);

const emit = defineEmits<{
  (e: "close"): void;
}>();

import { useDailyFocusStore } from "~/stores/useDailyFocusStore";
import { useMarkdownRenderer } from "~/composables/useMarkdownRenderer";
import { useApiError } from "~/composables/useApiError";

const focusStore = useDailyFocusStore();
const { locale } = useI18n();
const { formatError } = useApiError();
const { render: renderMarkdown, isHighlighterReady } = useMarkdownRenderer();

const explanation = ref<string | null>(null);
const errorMessage = ref<string | null>(null);
const isFromCache = ref(false);
const isLoading = ref(false);
const copied = ref(false);

const renderedExplanation = computed(() => {
  const _ = isHighlighterReady.value;
  if (!explanation.value) return "";
  return renderMarkdown(explanation.value);
});

async function loadExplanation() {
  isLoading.value = true;
  errorMessage.value = null;
  explanation.value = null;
  try {
    const res = await focusStore.explainTerm(
      props.term,
      props.category || "Software Architecture",
      props.context || "",
      locale.value,
    );
    explanation.value = res.explanation;
    isFromCache.value = !!res.isFromCache;
  } catch (err: any) {
    errorMessage.value = formatError(err, "today.explain_error");
    explanation.value = "";
  } finally {
    isLoading.value = false;
  }
}

onMounted(() => {
  loadExplanation();
});

function copyText() {
  if (explanation.value && !errorMessage.value) {
    navigator.clipboard.writeText(explanation.value);
    copied.value = true;
    setTimeout(() => (copied.value = false), 2000);
  }
}
</script>

<template>
  <div
    class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm"
    @click.self="emit('close')"
  >
    <div
      class="w-full max-w-lg bg-white dark:bg-canvas-elevated border border-slate-200 dark:border-white/[0.08] rounded-3xl shadow-2xl p-6 sm:p-7 overflow-hidden animate-in fade-in zoom-in-95 duration-200 space-y-4 transition-colors"
    >
      <!-- Header -->
      <div
        class="flex items-center justify-between gap-3 pb-3 border-b border-slate-200 dark:border-white/[0.06]"
      >
        <div class="flex items-center gap-3 min-w-0 flex-1">
          <div
            class="p-2 rounded-xl bg-brand-100 dark:bg-brand-500/10 text-brand-700 dark:text-brand-400 border border-brand-200 dark:border-brand-500/20 shrink-0"
          >
            <Sparkles class="w-5 h-5" />
          </div>
          <div class="min-w-0 flex-1">
            <div class="flex items-center gap-2 flex-wrap sm:flex-nowrap">
              <span
                class="text-xs font-bold uppercase tracking-wider text-brand-700 dark:text-brand-400 truncate max-w-[180px] sm:max-w-xs"
                :title="category"
              >
                {{ category }}
              </span>
              <span
                v-if="isFromCache"
                class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-500/15 text-amber-600 dark:text-amber-400 border border-amber-500/30 shadow-xs whitespace-nowrap shrink-0"
              >
                ⚡ {{ $t('reader.instant_cache') }}
              </span>
            </div>
            <h3
              class="text-lg font-bold text-slate-900 dark:text-white leading-tight font-mono mt-0.5 truncate"
              :title="term"
            >
              {{ term }}
            </h3>
          </div>
        </div>
        <button
          @click="emit('close')"
          class="p-2 rounded-xl text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-white/[0.08] transition-colors shrink-0"
          :aria-label="$t('common.close') || 'Close'"
        >
          <X class="w-5 h-5" />
        </button>
      </div>

      <!-- Body -->
      <div class="py-2">
        <div
          v-if="isLoading"
          class="flex items-center gap-3 py-8 justify-center text-slate-500 dark:text-slate-400 text-sm"
        >
          <span class="w-3 h-3 rounded-full bg-brand-500 animate-ping"></span>
          <span>{{ $t('reader.term_explainer_loading') }}</span>
        </div>

        <!-- Error State -->
        <div
          v-else-if="errorMessage"
          class="p-5 rounded-2xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800/60 flex flex-col items-start gap-3.5 text-rose-900 dark:text-rose-200"
        >
          <div class="flex items-center gap-2.5 font-bold text-sm">
            <AlertCircle class="w-5 h-5 text-rose-600 dark:text-rose-400 shrink-0" />
            <span>{{ $t("today.explain_error_title") || $t("common.error") || "Error" }}</span>
          </div>
          <p class="text-xs sm:text-sm leading-relaxed text-rose-800 dark:text-rose-300">
            {{ errorMessage }}
          </p>
          <button
            type="button"
            @click="loadExplanation"
            class="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-rose-600 hover:bg-rose-500 text-white font-semibold text-xs transition-colors shadow-sm active:scale-95"
          >
            <RotateCcw class="w-3.5 h-3.5" />
            <span>{{ $t("today.retry") || $t("common.retry") || "Retry" }}</span>
          </button>
        </div>

        <!-- Success Explanation -->
        <div v-else-if="explanation" class="space-y-3">
          <div
            class="prose prose-slate dark:prose-invert max-w-none text-sm sm:text-base text-slate-800 dark:text-slate-200 leading-relaxed bg-slate-50 dark:bg-canvas-subtle p-5 rounded-2xl border border-slate-200 dark:border-white/[0.08]"
            v-html="renderedExplanation"
          />
        </div>
      </div>

      <!-- Footer -->
      <div
        class="flex items-center justify-between pt-3 border-t border-slate-200 dark:border-white/[0.06] text-xs text-slate-500"
      >
        <span class="font-medium">{{ $t('reader.term_explainer_powered_by') }}</span>
        <button
          @click="copyText"
          :disabled="!explanation || !!errorMessage"
          class="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-slate-100 dark:bg-white/[0.06] hover:bg-slate-200 dark:hover:bg-white/[0.12] border border-transparent dark:border-white/[0.08] text-slate-700 dark:text-slate-200 font-semibold text-xs transition-colors shadow-sm disabled:opacity-40 disabled:cursor-not-allowed"
        >
          <Check v-if="copied" class="w-3.5 h-3.5 text-emerald-500" />
          <Copy v-else class="w-3.5 h-3.5" />
          <span>{{ copied ? $t('reader.term_explainer_copied') : $t('reader.term_explainer_copy') }}</span>
        </button>
      </div>
    </div>
  </div>
</template>
