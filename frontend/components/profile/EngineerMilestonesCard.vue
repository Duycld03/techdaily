<script setup lang="ts">
import {
  Trophy,
  Target,
  CheckCircle2,
  Layers,
  Bookmark
} from 'lucide-vue-next'
import type { UserLearningStats } from '~/stores/useProfileStore'
import type { QuizStats } from '~/stores/useInterviewQuizStore'

interface Props {
  stats: UserLearningStats | null
  quizStats: QuizStats | null
}

withDefaults(defineProps<Props>(), {
  stats: null,
  quizStats: null
})
</script>

<template>
  <div class="glass-card p-6 sm:p-7 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] shadow-sm space-y-5 transition-colors duration-200">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <h3 class="text-base sm:text-lg font-black text-slate-900 dark:text-white tracking-tight flex items-center gap-2">
        <Trophy class="w-5 h-5 text-amber-500 shrink-0" />
        <span>{{ $t('profile.milestones_title') }}</span>
      </h3>
    </div>

    <!-- 4-Cell Cumulative Bento Grid -->
    <div class="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
      <!-- 1. Architecture Drills Completed -->
      <div class="p-4 rounded-xl bg-slate-50/80 dark:bg-canvas-elevated/70 border border-slate-200/60 dark:border-white/[0.06] space-y-2.5 transition-all hover:border-violet-500/30">
        <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-violet-600 dark:text-violet-400">
          <div class="w-7 h-7 rounded-lg bg-violet-500/10 flex items-center justify-center shrink-0">
            <Target class="w-4 h-4 text-violet-500" />
          </div>
          <span class="truncate">{{ $t('profile.drills_completed') }}</span>
        </div>

        <div class="flex items-baseline justify-between gap-2 pt-0.5">
          <div class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white tracking-tight">
            {{ stats?.totalDrillsCompleted ?? 0 }}
          </div>
          <span v-if="stats?.averageScore" class="text-xs font-semibold text-slate-500 dark:text-slate-400 shrink-0">
            {{ stats.averageScore }}/10
          </span>
        </div>
      </div>

      <!-- 2. Interview Quiz Accuracy -->
      <div class="p-4 rounded-xl bg-slate-50/80 dark:bg-canvas-elevated/70 border border-slate-200/60 dark:border-white/[0.06] space-y-2.5 transition-all hover:border-emerald-500/30">
        <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-emerald-600 dark:text-emerald-400">
          <div class="w-7 h-7 rounded-lg bg-emerald-500/10 flex items-center justify-center shrink-0">
            <CheckCircle2 class="w-4 h-4 text-emerald-500" />
          </div>
          <span class="truncate">{{ $t('profile.quiz_accuracy') }}</span>
        </div>

        <div class="flex items-baseline justify-between gap-2 pt-0.5">
          <div class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white tracking-tight">
            {{ quizStats?.accuracyRate ?? 0 }}%
          </div>
          <span v-if="quizStats" class="text-xs font-semibold text-slate-500 dark:text-slate-400 truncate">
            {{ $t('profile.topics_mastered', { mastered: quizStats.masteredCount ?? 0, total: quizStats.totalAnswered ?? 0 }) }}
          </span>
        </div>
      </div>

      <!-- 3. Memory Vault (SM-2 Spaced Repetition) -->
      <div class="p-4 rounded-xl bg-slate-50/80 dark:bg-canvas-elevated/70 border border-slate-200/60 dark:border-white/[0.06] space-y-2.5 transition-all hover:border-sky-500/30">
        <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-sky-600 dark:text-sky-400">
          <div class="w-7 h-7 rounded-lg bg-sky-500/10 flex items-center justify-center shrink-0">
            <Layers class="w-4 h-4 text-sky-500" />
          </div>
          <span class="truncate">{{ $t('profile.memory_vault') }}</span>
        </div>

        <div class="flex items-baseline justify-between gap-2 pt-0.5">
          <div class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white tracking-tight">
            {{ stats?.totalCardsInDeck ?? 0 }}
          </div>
          <span class="text-xs font-medium text-slate-500 dark:text-slate-400 truncate">
            {{ $t('profile.memory_vault_desc') }}
          </span>
        </div>
      </div>

      <!-- 4. Architecture Source Highlights -->
      <div class="p-4 rounded-xl bg-slate-50/80 dark:bg-canvas-elevated/70 border border-slate-200/60 dark:border-white/[0.06] space-y-2.5 transition-all hover:border-amber-500/30">
        <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-amber-600 dark:text-amber-400">
          <div class="w-7 h-7 rounded-lg bg-amber-500/10 flex items-center justify-center shrink-0">
            <Bookmark class="w-4 h-4 text-amber-500" />
          </div>
          <span class="truncate">{{ $t('profile.highlights_vault') }}</span>
        </div>

        <div class="flex items-baseline justify-between gap-2 pt-0.5">
          <div class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white tracking-tight">
            {{ stats?.totalHighlightsSaved ?? 0 }}
          </div>
          <span class="text-xs font-medium text-slate-500 dark:text-slate-400 truncate">
            {{ $t('profile.highlights_vault_desc') }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
