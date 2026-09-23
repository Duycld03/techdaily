<script setup lang="ts">
import { ref, computed } from 'vue'
import {
  Briefcase,
  Mail,
  Clock,
  Calendar,
  Trophy
} from 'lucide-vue-next'
import type { UserProfile, UserLearningStats } from '~/stores/useProfileStore'

interface Props {
  profile: UserProfile | null
  stats: UserLearningStats | null
}

const props = withDefaults(defineProps<Props>(), {
  profile: null,
  stats: null
})

const hasAvatarError = ref(false)

const userInitial = computed(() => {
  return props.profile?.name?.trim()?.charAt(0)?.toUpperCase() || 'U'
})

const formattedMemberSince = computed(() => {
  if (!props.stats?.memberSince) return ''
  try {
    const date = new Date(props.stats.memberSince)
    if (isNaN(date.getTime())) return props.stats.memberSince
    return date.toLocaleDateString(undefined, { year: 'numeric', month: 'short' })
  } catch {
    return props.stats.memberSince
  }
})
</script>

<template>
  <div class="glass-card p-4 sm:p-5 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] shadow-sm relative overflow-hidden transition-colors duration-200 glow-subtle">
    <!-- Subtle top decorative background accent -->
    <div class="absolute -top-12 -right-12 w-40 h-40 bg-brand-500/15 dark:bg-brand-500/10 rounded-full blur-2xl pointer-events-none"></div>

    <div class="flex flex-col sm:flex-row items-center sm:items-center justify-between gap-6 relative z-10">
      <!-- Avatar + Identity Details -->
      <div class="flex flex-col sm:flex-row items-center sm:items-center gap-5 text-center sm:text-left min-w-0 w-full">
        <!-- Large Avatar with Status Dot -->
        <div class="relative shrink-0">
          <img
            v-if="profile?.avatarUrl && !hasAvatarError"
            :src="profile.avatarUrl"
            :alt="profile?.name || 'Engineer'"
            @error="hasAvatarError = true"
            class="w-16 h-16 sm:w-20 sm:h-20 rounded-2xl object-cover border-2 border-slate-200 dark:border-white/[0.12] shadow-sm"
          />
          <div
            v-else
            class="w-16 h-16 sm:w-20 sm:h-20 rounded-2xl bg-gradient-to-tr from-brand-600 via-indigo-600 to-emerald-500 flex items-center justify-center text-white font-black text-2xl sm:text-3xl shadow-sm tracking-tight"
          >
            {{ userInitial }}
          </div>
          <!-- Status Indicator Dot -->
          <span
            class="absolute -bottom-1 -right-1 w-4 h-4 rounded-full bg-emerald-500 border-2 border-white dark:border-slate-900 ring-2 ring-emerald-500/20 shadow-sm"
            :title="$t('profile.status_active')"
          ></span>
        </div>

        <!-- Name, Email, Member Since & Badges -->
        <div class="min-w-0 flex-1 space-y-2">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
            <div>
              <h2 class="text-xl sm:text-2xl font-black text-slate-900 dark:text-white tracking-tight truncate">
                {{ profile?.name || 'Engineer' }}
              </h2>
              <p class="text-xs sm:text-sm font-mono text-slate-500 dark:text-slate-400 truncate mt-0.5">
                {{ profile?.email || 'engineer@techdaily.dev' }}
              </p>
            </div>

            <!-- Longest Streak Trophy Badge (prominent on desktop & mobile) -->
            <div class="flex items-center justify-center sm:justify-end shrink-0">
              <span class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-amber-50 dark:bg-amber-950/60 border border-amber-200 dark:border-amber-800/60 text-amber-700 dark:text-amber-300 font-bold text-xs whitespace-nowrap shadow-xs">
                <Trophy class="w-3.5 h-3.5 shrink-0 text-amber-500" :stroke-width="1.5" />
                <span>{{ $t('profile.longest_streak_record', { days: stats?.longestStreak ?? 0 }) }}</span>
              </span>
            </div>
          </div>

          <!-- Membership Tenure -->
          <div v-if="stats?.memberSince" class="flex items-center justify-center sm:justify-start gap-1.5 text-xs text-slate-500 dark:text-slate-400">
            <Calendar class="w-3.5 h-3.5 text-slate-400 shrink-0" :stroke-width="1.5" />
            <span>{{ $t('profile.member_since', { date: formattedMemberSince }) }}</span>
          </div>

          <!-- Badges Row -->
          <div class="flex flex-wrap items-center justify-center sm:justify-start gap-1.5 pt-1">
            <!-- Target Role Badge -->
            <span class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full bg-brand-50 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800/60 text-brand-700 dark:text-brand-300 font-bold text-xs whitespace-nowrap shrink-0">
              <Briefcase class="w-3 h-3 shrink-0" :stroke-width="1.5" />
              <span class="truncate max-w-[150px]">{{ profile?.targetRole || 'Senior Engineer' }}</span>
            </span>

            <!-- Account Type Badge -->
            <span
              v-if="profile?.isGoogleLinked"
              class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-slate-100 dark:bg-canvas-elevated border border-slate-200 dark:border-white/[0.08] text-xs font-semibold text-slate-700 dark:text-slate-300 whitespace-nowrap shrink-0"
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
              class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-slate-100 dark:bg-canvas-elevated border border-slate-200 dark:border-white/[0.08] text-xs font-semibold text-slate-700 dark:text-slate-300 whitespace-nowrap shrink-0"
            >
              <Mail class="w-3 h-3 shrink-0 text-brand-500" :stroke-width="1.5" />
              <span>{{ $t('profile.standard_account') }}</span>
            </span>

            <!-- Target Pace Badge -->
            <span class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full bg-emerald-50 dark:bg-emerald-950/60 border border-emerald-200 dark:border-emerald-800/60 text-emerald-700 dark:text-emerald-300 font-bold text-xs whitespace-nowrap shrink-0">
              <Clock class="w-3 h-3 shrink-0" :stroke-width="1.5" />
              <span>{{ $t('profile.target_pace', { minutes: profile?.dailyGoalMinutes || 10 }) }}</span>
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
