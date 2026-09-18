<script setup lang="ts">
import { ref, computed } from 'vue'
import {
  Briefcase,
  Mail,
  Flame,
  CheckCircle2,
  Target,
  Snowflake,
  Clock,
  Trophy
} from 'lucide-vue-next'
import type { UserProfile, UserLearningStats } from '~/stores/useProfileStore'
import type { QuizStats } from '~/stores/useInterviewQuizStore'

interface Props {
  profile: UserProfile | null
  stats: UserLearningStats | null
  quizStats: QuizStats | null
}

const props = withDefaults(defineProps<Props>(), {
  profile: null,
  stats: null,
  quizStats: null
})

const hasAvatarError = ref(false)

const userInitial = computed(() => {
  return props.profile?.name?.trim()?.charAt(0)?.toUpperCase() || 'U'
})
</script>

<template>
  <div class="space-y-6">
    <!-- Engineer Identity Card -->
    <div class="glass-card p-6 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] shadow-sm relative overflow-hidden transition-colors duration-200 glow-subtle">
      <!-- Subtle top decorative background accent -->
      <div class="absolute -top-12 -right-12 w-32 h-32 bg-brand-500/15 dark:bg-brand-500/10 rounded-full blur-2xl pointer-events-none"></div>

      <div class="flex flex-col items-center text-center sm:text-left sm:flex-row sm:items-center gap-5 relative z-10">
        <!-- Large Avatar with Status Dot -->
        <div class="relative shrink-0">
          <img
            v-if="profile?.avatarUrl && !hasAvatarError"
            :src="profile.avatarUrl"
            :alt="profile?.name || 'Engineer'"
            @error="hasAvatarError = true"
            class="w-20 h-20 sm:w-24 sm:h-24 rounded-2xl object-cover border-2 border-slate-200 dark:border-white/[0.12] shadow-sm"
          />
          <div
            v-else
            class="w-20 h-20 sm:w-24 sm:h-24 rounded-2xl bg-gradient-to-tr from-brand-600 via-indigo-600 to-emerald-500 flex items-center justify-center text-white font-black text-3xl shadow-sm tracking-tight"
          >
            {{ userInitial }}
          </div>
          <!-- Status Indicator Dot -->
          <span
            class="absolute -bottom-1 -right-1 w-4 h-4 rounded-full bg-emerald-500 border-2 border-white dark:border-slate-900 ring-2 ring-emerald-500/20 shadow-sm"
            :title="$t('profile.status_active')"
          ></span>
        </div>

        <!-- Name, Role, Email & Account Info -->
        <div class="min-w-0 flex-1 space-y-2">
          <div>
            <h2 class="text-xl sm:text-2xl font-black text-slate-900 dark:text-white tracking-tight truncate">
              {{ profile?.name || 'Engineer' }}
            </h2>
            <p class="text-xs sm:text-sm font-mono text-slate-500 dark:text-slate-400 truncate mt-0.5">
              {{ profile?.email || 'engineer@techdaily.dev' }}
            </p>
          </div>

          <!-- Badges: Role, Account Type, Target Pace -->
          <div class="flex flex-wrap items-center justify-center sm:justify-start gap-1.5 pt-1">
            <!-- Target Role Badge -->
            <span class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full bg-brand-50 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800/60 text-brand-700 dark:text-brand-300 font-bold text-xs shrink-0">
              <Briefcase class="w-3 h-3 shrink-0" />
              <span class="truncate max-w-[140px]">{{ profile?.targetRole || 'Senior Engineer' }}</span>
            </span>

            <!-- Account Type Badge -->
            <span
              v-if="profile?.isGoogleLinked"
              class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-slate-100 dark:bg-canvas-elevated border border-slate-200 dark:border-white/[0.08] text-xs font-semibold text-slate-700 dark:text-slate-300 shrink-0"
            >
              <svg class="w-3 h-3 shrink-0" viewBox="0 0 24 24">
                <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
                <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
                <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z"/>
                <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z"/>
              </svg>
              <span>{{ $t('profile.google_linked') }}</span>
            </span>
            <span
              v-else
              class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-slate-100 dark:bg-canvas-elevated border border-slate-200 dark:border-white/[0.08] text-xs font-semibold text-slate-700 dark:text-slate-300 shrink-0"
            >
              <Mail class="w-3 h-3 shrink-0 text-brand-500" />
              <span>{{ $t('profile.standard_account') }}</span>
            </span>

            <!-- Target Pace Badge -->
            <span class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full bg-emerald-50 dark:bg-emerald-950/60 border border-emerald-200 dark:border-emerald-800/60 text-emerald-700 dark:text-emerald-300 font-bold text-xs shrink-0">
              <Clock class="w-3 h-3 shrink-0" />
              <span>{{ $t('profile.target_pace', { minutes: profile?.dailyGoalMinutes || 10 }) }}</span>
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- Stacked Milestones Bento Section -->
    <div class="glass-card p-6 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] shadow-sm space-y-4 transition-colors duration-200">
      <div class="flex items-center justify-between">
        <h3 class="text-sm font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 flex items-center gap-1.5">
          <Trophy class="w-4 h-4 text-amber-500" />
          <span>{{ $t('profile.milestones_title') }}</span>
        </h3>
      </div>

      <div class="space-y-3">
        <!-- 1. Active Streak Milestone Card -->
        <div class="p-4 rounded-xl bg-slate-50/80 dark:bg-canvas-elevated/70 border border-slate-200/60 dark:border-white/[0.06] space-y-2.5 transition-all hover:border-amber-500/30">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-amber-500">
              <div class="w-7 h-7 rounded-lg bg-amber-500/10 flex items-center justify-center shrink-0">
                <Flame class="w-4 h-4 text-amber-500" />
              </div>
              <span>{{ $t('profile.active_streak') }}</span>
            </div>
            <!-- Freeze Credits Remaining Badge -->
            <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-sky-50 dark:bg-sky-950/60 border border-sky-200 dark:border-sky-800 text-sky-700 dark:text-sky-300 text-[11px] font-semibold">
              <Snowflake class="w-3 h-3 shrink-0 text-sky-500" />
              <span>{{ $t('profile.freeze_credits_badge', { count: stats?.freezeCreditsRemaining ?? 0 }) }}</span>
            </span>
          </div>

          <div class="flex items-baseline justify-between pt-0.5">
            <div class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white tracking-tight">
              {{ stats?.currentStreak ?? 0 }}
              <span class="text-xs font-normal text-slate-500 dark:text-slate-400 ml-1">{{ $t('profile.days') }}</span>
            </div>
            <!-- Longest Streak Record -->
            <span class="text-xs font-medium text-slate-500 dark:text-slate-400">
              {{ $t('profile.longest_streak_record', { days: stats?.longestStreak ?? 0 }) }}
            </span>
          </div>
        </div>

        <!-- 2. Drills Completed Milestone Card -->
        <div class="p-4 rounded-xl bg-slate-50/80 dark:bg-canvas-elevated/70 border border-slate-200/60 dark:border-white/[0.06] space-y-2 transition-all hover:border-emerald-500/30">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-emerald-500">
              <div class="w-7 h-7 rounded-lg bg-emerald-500/10 flex items-center justify-center shrink-0">
                <CheckCircle2 class="w-4 h-4 text-emerald-500" />
              </div>
              <span>{{ $t('profile.drills_completed') }}</span>
            </div>
          </div>

          <div class="flex items-baseline justify-between pt-0.5">
            <div class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white tracking-tight">
              {{ stats?.totalDrillsCompleted ?? 0 }}
            </div>
            <span v-if="stats?.averageScore" class="text-xs font-medium text-slate-500 dark:text-slate-400">
              {{ stats.averageScore }}/10
            </span>
          </div>
        </div>

        <!-- 3. Quiz Accuracy Milestone Card -->
        <div class="p-4 rounded-xl bg-slate-50/80 dark:bg-canvas-elevated/70 border border-slate-200/60 dark:border-white/[0.06] space-y-2 transition-all hover:border-brand-500/30">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-violet-500">
              <div class="w-7 h-7 rounded-lg bg-violet-500/10 flex items-center justify-center shrink-0">
                <Target class="w-4 h-4 text-violet-500" />
              </div>
              <span>{{ $t('profile.quiz_accuracy') }}</span>
            </div>
          </div>

          <div class="flex items-baseline justify-between pt-0.5">
            <div class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white tracking-tight">
              {{ quizStats?.accuracyRate ?? 0 }}%
            </div>
            <span v-if="quizStats" class="text-xs font-medium text-slate-500 dark:text-slate-400">
              {{ $t('profile.topics_mastered', { mastered: quizStats.masteredCount ?? 0, total: quizStats.totalAnswered ?? 0 }) }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
