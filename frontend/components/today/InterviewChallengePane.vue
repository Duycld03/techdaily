<script setup lang="ts">
import { ref, computed, watch } from "vue";
import {
  Terminal,
  CheckCircle2,
  Sparkles,
  Lock,
  ArrowRight,
  AlertCircle,
  Check
} from "lucide-vue-next";
import confetti from "canvas-confetti";
import AISynthesisCard from "~/components/today/AISynthesisCard.vue";
import OptionCard from "~/components/ui/OptionCard.vue";
import type {
  InterviewQuestion,
  DailyDrill,
} from "~/stores/useDailyFocusStore";
import { useDailyFocusStore } from "~/stores/useDailyFocusStore";
import { useAuthStore } from "~/stores/useAuthStore";
import { useMarkdownRenderer } from "~/composables/useMarkdownRenderer";

const props = defineProps<{
  question?: InterviewQuestion | null;
  drill?: DailyDrill | null;
  chapterTitle?: string;
}>();

const authStore = useAuthStore();
const router = useRouter();
const focusStore = useDailyFocusStore();
const { locale } = useI18n();
const { render: renderMarkdown, isHighlighterReady } = useMarkdownRenderer();

const optionLetters = ["A", "B", "C", "D", "E", "F"];

// Selected option state (initialized from drill if already submitted)
const selectedOption = ref<number | null>(
  props.drill?.selectedOptionIndex ?? null,
);

// Watch for drill changes
watch(
  () => props.drill,
  (newDrill) => {
    if (
      newDrill.selectedOptionIndex !== undefined &&
      newDrill.selectedOptionIndex !== null
    ) {
      selectedOption.value = newDrill.selectedOptionIndex;
    }
  },
  { immediate: true },
);

const isReviewed = computed(
  () =>
    props.drill?.status === 2 ||
    props.drill?.status === "Reviewed" ||
    props.drill?.status === "reviewed"
);

const isCorrect = computed(() => {
  if (props.drill?.isCorrect !== undefined && props.drill?.isCorrect !== null) {
    return props.drill.isCorrect;
  }
  if (
    props.question?.correctOptionIndex !== undefined &&
    props.question?.correctOptionIndex !== null &&
    selectedOption.value !== null
  ) {
    return selectedOption.value === props.question.correctOptionIndex;
  }
  return false;
});

const renderedExplanation = computed(() => {
  const _ = isHighlighterReady.value;
  if (props.question?.explanationMarkdown) {
    return renderMarkdown(props.question.explanationMarkdown);
  }
  return "";
});

async function handleOptionSelect(index: number) {
  if (isReviewed.value) return;
  selectedOption.value = index;
}

async function handleRetryChallenge() {
  if (focusStore.data?.documentChunk?.id) {
    await focusStore.fetchChunkChallenge(focusStore.data.documentChunk.id);
  }
}

async function handleOptionSubmit() {
  if (!authStore.isLoggedIn) {
    router.push({ path: "/login", query: { redirect: "/today" } });
    return;
  }

  if (selectedOption.value === null || focusStore.isSubmitting) return;

  try {
    const res = await focusStore.submitOption(
      selectedOption.value,
      locale.value,
    );
    if (res?.isCorrect) {
      confetti({
        particleCount: 80,
        spread: 60,
        origin: { y: 0.6 },
      });
    }
  } catch (err) {
    // handled in store
  }
}
</script>

<template>
  <div
    v-if="!question || focusStore.isGeneratingQuestion"
    class="h-full p-4 sm:p-6 md:p-9"
  >
    <AISynthesisCard
      :chapter-title="
        chapterTitle || focusStore.data?.documentChunk?.chapterTitle
      "
      @retry="handleRetryChallenge"
    />
  </div>

  <div
    v-else
    class="h-full flex flex-col bg-slate-50/50 dark:bg-canvas-subtle/40 p-3.5 sm:p-4 md:p-5 overflow-y-auto space-y-3 sm:space-y-4 transition-colors duration-200"
  >
    <!-- Header -->
    <div class="space-y-2.5 sm:space-y-3">
      <div class="flex items-center justify-between gap-2">
        <div
          class="flex items-center gap-2 text-xs sm:text-sm font-bold text-brand-600 dark:text-brand-400 uppercase tracking-wider"
        >
          <Terminal class="w-4 h-4 shrink-0" :stroke-width="1.5" />
          <span>{{ $t("today.scenario_challenge") }}</span>
        </div>

        <div class="flex items-center gap-1.5 sm:gap-2 shrink-0">
          <span
            class="px-2.5 py-1 rounded-full text-xs font-semibold uppercase tracking-wider bg-brand-500/10 text-brand-400 border border-brand-500/20"
          >
            Senior Drill
          </span>
          <span
            v-if="isReviewed"
            :class="[
              'px-2.5 py-1 rounded-full text-xs font-semibold flex items-center gap-1.5 border',
              isCorrect
                ? 'bg-brand-50 dark:bg-brand-950/40 text-brand-700 dark:text-brand-300 border-brand-200 dark:border-brand-500/30'
                : 'bg-amber-50 dark:bg-amber-950/40 text-amber-800 dark:text-amber-300 border-amber-200 dark:border-amber-500/30',
            ]"
          >
            <CheckCircle2 v-if="isCorrect" class="w-3.5 h-3.5" :stroke-width="1.5" />
            <AlertCircle v-else class="w-3.5 h-3.5" :stroke-width="1.5" />
            <span>{{ isCorrect ? "+10 Pts" : "0 Pts" }}</span>
          </span>
        </div>
      </div>

      <!-- Question Text -->
      <h2
        class="text-base sm:text-lg md:text-xl font-bold text-slate-900 dark:text-white leading-snug"
      >
        {{ question.questionText }}
      </h2>
    </div>

    <!-- Scenario Multiple-Choice Interface -->
    <div class="space-y-4 flex-1 flex flex-col justify-start">
      <!-- Options List -->
      <div class="space-y-3">
        <div
          class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400"
        >
          {{ $t("today.select_option_hint") }}
        </div>

        <div class="space-y-2 sm:space-y-2.5">
          <OptionCard
            v-for="(option, index) in question.options"
            :key="index"
            :letter="optionLetters[index] || index + 1"
            :text="option"
            :state="!isReviewed ? (selectedOption === index ? 'selected' : 'default') : (index === question.correctOptionIndex ? 'correct' : (selectedOption === index ? 'incorrect' : 'default'))"
            :disabled="isReviewed || focusStore.isSubmitting"
            @select="handleOptionSelect(index)"
          >
            <template v-if="isReviewed" #trailing>
              <span
                v-if="index === question.correctOptionIndex"
                class="inline-flex items-center gap-1 px-2 py-0.5 rounded-lg text-xs font-bold bg-emerald-600 text-white shadow-sm whitespace-nowrap shrink-0"
              >
                <Check class="w-3.5 h-3.5" :stroke-width="1.5" />
                <span>{{ $t("today.optimal_choice") }}</span>
              </span>
            </template>
          </OptionCard>
        </div>
      </div>

      <!-- Guest Sign-in prompt banner if not logged in -->
      <div
        v-if="!authStore.isLoggedIn"
        class="p-3.5 sm:p-4 rounded-2xl bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900/60 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 text-xs sm:text-sm"
      >
        <div
          class="flex items-center gap-2.5 text-amber-900 dark:text-amber-200 font-semibold"
        >
          <Lock class="w-4 h-4 text-amber-600 dark:text-amber-400 shrink-0" :stroke-width="1.5" />
          <span>{{ $t("today.signin_banner_title") }}</span>
        </div>
        <NuxtLink
          to="/login"
          class="w-full sm:w-auto text-center px-4 py-2 rounded-xl bg-amber-500 hover:bg-amber-400 text-slate-950 font-bold text-xs shrink-0 shadow transition-transform active:scale-95"
        >
          {{ $t("today.signin_banner_button") }}
        </NuxtLink>
      </div>

      <!-- Submit Action Bar (Before Review) -->
      <div v-if="!isReviewed" class="flex justify-end pt-2">
        <button
          type="button"
          @click="handleOptionSubmit"
          :disabled="selectedOption === null || focusStore.isSubmitting"
          class="w-full sm:w-auto flex items-center justify-center gap-2 h-9 px-5 text-sm font-semibold rounded-xl bg-brand-600 hover:bg-brand-500 disabled:opacity-40 disabled:cursor-not-allowed text-white shadow-lg shadow-brand-500/20 transition-all active:scale-[0.98]"
        >
          <span
            v-if="focusStore.isSubmitting"
            class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"
          ></span>
          <ArrowRight v-else class="w-4 h-4" :stroke-width="1.5" />
          <span>
            {{
              focusStore.isSubmitting
                ? $t("today.option_submitting")
                : $t("today.submit_option")
            }}
          </span>
        </button>
      </div>

      <!-- Post-Submission Feedback & Deep-Dive Explanation -->
      <div
        v-else
        class="space-y-5 pt-2 animate-in fade-in slide-in-from-bottom-2 duration-300"
      >
        <!-- Result Banner -->
        <div
          :class="[
            'p-4 sm:p-5 rounded-2xl border flex items-start gap-3.5',
            isCorrect
              ? 'bg-brand-50 dark:bg-brand-950/40 border-brand-200 dark:border-brand-500/30 text-brand-950 dark:text-brand-200'
              : 'bg-amber-50 dark:bg-amber-950/40 border-amber-200 dark:border-amber-500/30 text-amber-950 dark:text-amber-200',
          ]"
        >
          <CheckCircle2
            v-if="isCorrect"
            class="w-6 h-6 text-brand-600 dark:text-brand-400 shrink-0 mt-0.5"
            :stroke-width="1.5"
          />
          <AlertCircle
            v-else
            class="w-6 h-6 text-amber-600 dark:text-amber-400 shrink-0 mt-0.5"
            :stroke-width="1.5"
          />

          <div class="space-y-1">
            <h3 class="font-bold text-sm sm:text-base">
              {{
                isCorrect
                  ? $t("today.correct_solution")
                  : $t("today.incorrect_solution")
              }}
            </h3>
            <p
              class="text-sm md:text-lg text-slate-700 dark:text-slate-300 leading-relaxed"
            >
              {{
                isCorrect
                  ? $t("today.correct_solution_desc")
                  : $t("today.scheduled_sm2")
              }}
            </p>
          </div>
        </div>

        <!-- Architectural Deep-Dive Explanation Card -->
        <div
          v-if="question.explanationMarkdown"
        class="p-5 sm:p-6 rounded-3xl bg-white dark:bg-canvas-elevated border border-slate-200 dark:border-white/[0.08] shadow-sm space-y-3"
        >
          <div
            class="flex items-center gap-2 text-xs sm:text-sm font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400"
          >
            <Sparkles class="w-4 h-4" :stroke-width="1.5" />
            <span>{{ $t("today.correct_explanation_header") }}</span>
          </div>

          <div
            class="prose dark:prose-invert max-w-none text-sm md:text-lg leading-relaxed text-slate-700 dark:text-slate-300"
            v-html="renderedExplanation"
          ></div>
        </div>
      </div>
    </div>
  </div>
</template>
