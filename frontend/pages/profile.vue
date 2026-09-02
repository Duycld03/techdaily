<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import {
  User,
  Shield,
  Award,
  Flame,
  Layers,
  Highlighter,
  CheckCircle2,
  Lock,
  Mail,
  Briefcase,
  Clock,
  Send,
  Sparkles,
  Save,
  Globe,
  Calendar,
  Check,
  Eye,
  EyeOff,
  ExternalLink,
  Target,
  Zap,
  HelpCircle
} from 'lucide-vue-next'

const profileStore = useProfileStore()
const quizStore = useInterviewQuizStore()
const authStore = useAuthStore()
const toast = useToast()
const { t, locale, setLocale } = useI18n()

const activeTab = ref<'personal' | 'security'>('personal')

// Form state
const name = ref('')
const targetRole = ref('Senior Engineer')
const dailyGoalMinutes = ref(10)
const preferredLocale = ref('en')
const telegramChatId = ref<number | undefined>(undefined)

// Password form state
const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const showCurrentPassword = ref(false)
const showNewPassword = ref(false)
const showConfirmPassword = ref(false)

const roleOptions = [
  { value: 'Senior Engineer', label: 'Senior Software Engineer', levelBadge: 'Senior' },
  { value: 'Staff Engineer', label: 'Staff Software Engineer', levelBadge: 'Staff' },
  { value: 'Principal Architect', label: 'Principal Software Architect', levelBadge: 'Principal' },
  { value: 'Tech Lead', label: 'Engineering Tech Lead', levelBadge: 'Lead' },
  { value: 'Mid-Level Engineer', label: 'Mid-Level Software Engineer', levelBadge: 'Mid' },
  { value: 'Junior Engineer', label: 'Junior Software Engineer', levelBadge: 'Junior' },
  { value: 'Fresher Engineer', label: 'Fresher / Entry Engineer', levelBadge: 'Fresher' }
]

const dailyGoalOptions = [
  { minutes: 5, label: '5m', desc: 'Bite-sized' },
  { minutes: 10, label: '10m', desc: 'Standard' },
  { minutes: 15, label: '15m', desc: 'Deep Dive' },
  { minutes: 30, label: '30m', desc: 'Intensive' }
]

// Password strength analysis
const pwdAnalysis = computed(() => {
  const pwd = newPassword.value
  const hasMinLength = pwd.length >= 8
  const hasUpper = /[A-Z]/.test(pwd)
  const hasLower = /[a-z]/.test(pwd)
  const hasNumber = /[0-9]/.test(pwd)
  const hasSpecial = /[^A-Za-z0-9]/.test(pwd)
  const hasMixedCase = hasUpper && hasLower

  const score = (hasMinLength ? 1 : 0) + (hasMixedCase ? 1 : 0) + (hasNumber ? 1 : 0) + (hasSpecial ? 1 : 0)

  let label = t('profile.password_strength_weak')
  let color = 'bg-rose-500 text-rose-500'
  let width = 25

  if (score >= 4) {
    label = t('profile.password_strength_strong')
    color = 'bg-emerald-500 text-emerald-500'
    width = 100
  } else if (score >= 2) {
    label = t('profile.password_strength_good')
    color = 'bg-amber-500 text-amber-500'
    width = 65
  }

  return {
    score,
    label,
    color,
    width,
    hasMinLength,
    hasMixedCase,
    hasNumber,
    hasSpecial
  }
})

const memberSinceFormatted = computed(() => {
  const rawDate = profileStore.stats?.memberSince
  if (!rawDate) return ''
  try {
    const d = new Date(rawDate)
    return d.toLocaleDateString(locale.value === 'vi' ? 'vi-VN' : 'en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    })
  } catch {
    return ''
  }
})

onMounted(async () => {
  if (!authStore.isLoggedIn) {
    return navigateTo({
      path: '/login',
      query: { redirect: '/profile' }
    })
  }

  const [data] = await Promise.all([
    profileStore.fetchProfile(),
    quizStore.fetchStats()
  ])

  if (data?.user) {
    name.value = data.user.name
    targetRole.value = data.user.targetRole || 'Senior Engineer'
    dailyGoalMinutes.value = data.user.dailyGoalMinutes || 10
    preferredLocale.value = data.user.preferredLocale || locale.value || 'en'
    telegramChatId.value = data.user.telegramChatId
  }
})

async function handleProfileSave() {
  try {
    await profileStore.updateProfile({
      name: name.value.trim(),
      targetRole: targetRole.value,
      dailyGoalMinutes: dailyGoalMinutes.value,
      preferredLocale: preferredLocale.value,
      telegramChatId: telegramChatId.value
    })

    // If user changed preferred language in profile, synchronize app locale
    if (preferredLocale.value && preferredLocale.value !== locale.value) {
      setLocale(preferredLocale.value as any)
    }

    toast.success(t('profile.save_success'))
  } catch (err: any) {
    toast.error(err.message || 'Failed to save profile.')
  }
}

async function handlePasswordChange() {
  if (newPassword.value.length < 6) {
    toast.error(t('profile.password_strength_weak'))
    return
  }

  if (newPassword.value !== confirmPassword.value) {
    toast.error(t('profile.passwords_must_match'))
    return
  }

  try {
    await profileStore.changePassword(currentPassword.value, newPassword.value)
    toast.success(t('profile.password_set_success'))
    currentPassword.value = ''
    newPassword.value = ''
    confirmPassword.value = ''
  } catch (err: any) {
    toast.error(err.message || 'Failed to change password.')
  }
}
</script>

<template>
  <div class="max-w-5xl mx-auto p-4 sm:p-6 md:p-10 space-y-6 sm:space-y-8 bg-slate-50 dark:bg-slate-950 transition-colors duration-200">
    <!-- Header Hero Profile Card -->
    <div class="p-6 sm:p-8 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm dark:shadow-xl relative overflow-hidden">
      <!-- Ambient decorative gradient -->
      <div class="absolute -right-16 -top-16 w-64 h-64 bg-gradient-to-br from-brand-500/10 to-emerald-500/10 rounded-full blur-3xl pointer-events-none"></div>

      <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-5 sm:gap-8 relative z-10">
        <div class="flex items-center gap-4 sm:gap-6 min-w-0 w-full">
          <!-- Avatar (Image or Initial letter) -->
          <div class="relative shrink-0">
            <img
              v-if="profileStore.profile?.avatarUrl"
              :src="profileStore.profile.avatarUrl"
              :alt="profileStore.profile.name"
              class="w-16 h-16 sm:w-22 sm:h-22 rounded-2xl sm:rounded-3xl object-cover border-2 border-brand-500/30 shadow-lg shadow-brand-500/10"
            />
            <div
              v-else
              class="w-16 h-16 sm:w-22 sm:h-22 rounded-2xl sm:rounded-3xl bg-gradient-to-tr from-brand-600 via-brand-500 to-emerald-500 flex items-center justify-center text-white font-black text-2xl sm:text-4xl shadow-lg shadow-brand-500/20"
            >
              {{ profileStore.profile?.name?.charAt(0).toUpperCase() || 'E' }}
            </div>

            <!-- Active Online Indicator -->
            <div class="absolute -bottom-1 -right-1 w-5 h-5 rounded-full bg-emerald-500 border-2 border-white dark:border-slate-900 flex items-center justify-center shadow-sm">
              <Zap class="w-3 h-3 text-white fill-current" />
            </div>
          </div>

          <!-- User Details -->
          <div class="min-w-0 flex-1 space-y-1.5">
            <div class="flex items-center gap-2.5 flex-wrap">
              <h1 class="text-xl sm:text-3xl font-black text-slate-900 dark:text-white tracking-tight truncate">
                {{ profileStore.profile?.name || 'Engineer' }}
              </h1>
              <span class="px-3 py-1 rounded-full bg-brand-50 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800/60 text-brand-700 dark:text-brand-300 font-bold text-xs shrink-0 flex items-center gap-1.5 shadow-sm">
                <Briefcase class="w-3.5 h-3.5" />
                {{ profileStore.profile?.targetRole || 'Senior Engineer' }}
              </span>
            </div>

            <div class="flex items-center gap-3 text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-medium flex-wrap">
              <span class="flex items-center gap-1 font-mono">
                <Mail class="w-3.5 h-3.5 text-slate-400" />
                {{ profileStore.profile?.email }}
              </span>

              <span class="hidden sm:inline text-slate-300 dark:text-slate-700">•</span>

              <span v-if="memberSinceFormatted" class="flex items-center gap-1">
                <Calendar class="w-3.5 h-3.5 text-slate-400" />
                {{ $t('profile.member_since') }}: {{ memberSinceFormatted }}
              </span>
            </div>

            <div class="flex items-center gap-2 pt-0.5 flex-wrap">
              <span
                v-if="profileStore.profile?.isGoogleLinked"
                class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-lg bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 text-xs font-semibold border border-slate-200 dark:border-slate-700"
              >
                <svg class="w-3 h-3" viewBox="0 0 24 24">
                  <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
                  <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
                  <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z"/>
                  <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z"/>
                </svg>
                {{ $t('profile.google_linked') }}
              </span>

              <span
                v-else
                class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-lg bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 text-xs font-semibold border border-slate-200 dark:border-slate-700"
              >
                <Shield class="w-3 h-3 text-brand-500" />
                {{ $t('profile.standard_account') }}
              </span>

              <span class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-lg bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 text-xs font-semibold border border-emerald-200 dark:border-emerald-800/60">
                <Clock class="w-3 h-3" />
                {{ $t('profile.target_pace', { minutes: profileStore.profile?.dailyGoalMinutes || 10 }) }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 6 Learning Metrics Cards Grid -->
    <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3 sm:gap-4">
      <!-- 1. Streak Card -->
      <div class="p-4 sm:p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm flex flex-col justify-between space-y-2 hover:border-amber-500/40 transition-colors">
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold uppercase tracking-wider text-amber-500 flex items-center gap-1.5">
            <Flame class="w-4 h-4" />
            {{ $t('profile.active_streak') }}
          </span>
        </div>
        <div class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white">
          {{ profileStore.stats?.currentStreak ?? 0 }}
          <span class="text-xs font-normal text-slate-500">{{ $t('profile.days') }}</span>
        </div>
        <div class="text-xs text-slate-500 dark:text-slate-400 font-medium">
          {{ $t('profile.freeze_credits', { count: profileStore.stats?.freezeCreditsRemaining ?? 2 }) }}
        </div>
      </div>

      <!-- 2. Scenario Drills Card -->
      <div class="p-4 sm:p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm flex flex-col justify-between space-y-2 hover:border-emerald-500/40 transition-colors">
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold uppercase tracking-wider text-emerald-500 flex items-center gap-1.5">
            <CheckCircle2 class="w-4 h-4" />
            {{ $t('profile.drills_completed') }}
          </span>
        </div>
        <div class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white">
          {{ profileStore.stats?.totalDrillsCompleted ?? 0 }}
        </div>
        <div class="text-xs text-slate-500 dark:text-slate-400 font-medium">
          {{ $t('profile.interview_challenges') }}
        </div>
      </div>

      <!-- 3. AI Avg Score Card -->
      <div class="p-4 sm:p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm flex flex-col justify-between space-y-2 hover:border-brand-500/40 transition-colors">
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400 flex items-center gap-1.5">
            <Award class="w-4 h-4" />
            {{ $t('profile.avg_score') }}
          </span>
        </div>
        <div class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white">
          {{ profileStore.stats?.averageScore ?? 0 }}
          <span class="text-xs font-normal text-slate-500">/ 10</span>
        </div>
        <div class="text-xs text-slate-500 dark:text-slate-400 font-medium">
          {{ $t('profile.ai_evaluated') }}
        </div>
      </div>

      <!-- 4. Quiz Arena Mastery Card -->
      <div class="p-4 sm:p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm flex flex-col justify-between space-y-2 hover:border-violet-500/40 transition-colors">
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold uppercase tracking-wider text-violet-600 dark:text-violet-400 flex items-center gap-1.5">
            <Target class="w-4 h-4" />
            {{ $t('profile.quiz_mastery') }}
          </span>
        </div>
        <div class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white">
          {{ quizStore.stats?.accuracyRate ?? 0 }}%
        </div>
        <div class="text-xs text-slate-500 dark:text-slate-400 font-medium truncate">
          {{ quizStore.stats?.masteredCount ?? 0 }} {{ $t('profile.mastered_questions') }}
        </div>
      </div>

      <!-- 5. Spaced Repetition Cards -->
      <div class="p-4 sm:p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm flex flex-col justify-between space-y-2 hover:border-blue-500/40 transition-colors">
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold uppercase tracking-wider text-blue-500 flex items-center gap-1.5">
            <Layers class="w-4 h-4" />
            {{ $t('profile.cards_in_deck') }}
          </span>
        </div>
        <div class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white">
          {{ profileStore.stats?.totalCardsInDeck ?? 0 }}
        </div>
        <div class="text-xs text-slate-500 dark:text-slate-400 font-medium">
          {{ $t('profile.spaced_repetition') }}
        </div>
      </div>

      <!-- 6. Saved Highlights Card -->
      <div class="p-4 sm:p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm flex flex-col justify-between space-y-2 hover:border-amber-400/40 transition-colors">
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold uppercase tracking-wider text-amber-600 dark:text-amber-400 flex items-center gap-1.5">
            <Highlighter class="w-4 h-4" />
            Highlights
          </span>
        </div>
        <div class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white">
          {{ profileStore.stats?.totalHighlightsSaved ?? 0 }}
        </div>
        <div class="text-xs text-slate-500 dark:text-slate-400 font-medium">
          {{ $t('notes.tab_highlights') }}
        </div>
      </div>
    </div>

    <!-- Main Tabs Card -->
    <div class="p-5 sm:p-8 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm dark:shadow-xl space-y-6">
      <!-- Segmented Tab Switcher -->
      <div class="grid grid-cols-2 gap-2 p-1.5 rounded-2xl bg-slate-100 dark:bg-slate-950 border border-slate-200/80 dark:border-slate-800 text-xs sm:text-sm font-bold">
        <button
          type="button"
          @click="activeTab = 'personal'"
          :class="[
            'min-h-[44px] sm:min-h-[48px] px-3 sm:px-6 py-2 rounded-xl transition-all flex items-center justify-center gap-2 outline-none focus:outline-none text-center',
            activeTab === 'personal'
              ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 font-bold shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 font-medium'
          ]"
        >
          <User class="w-4 h-4 shrink-0" />
          <span class="whitespace-nowrap">{{ $t('profile.tab_personal') }}</span>
        </button>

        <button
          type="button"
          @click="activeTab = 'security'"
          :class="[
            'min-h-[44px] sm:min-h-[48px] px-3 sm:px-6 py-2 rounded-xl transition-all flex items-center justify-center gap-2 outline-none focus:outline-none text-center',
            activeTab === 'security'
              ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 font-bold shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 font-medium'
          ]"
        >
          <Shield class="w-4 h-4 shrink-0" />
          <span class="whitespace-nowrap">{{ $t('profile.tab_security') }}</span>
        </button>
      </div>

      <!-- Tab 1: Profile & Goals Form -->
      <form v-if="activeTab === 'personal'" @submit.prevent="handleProfileSave" class="space-y-6">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
          <!-- Full Name -->
          <div>
            <label class="block text-sm md:text-base font-bold text-slate-800 dark:text-slate-200 mb-2">
              {{ $t('profile.full_name') }}
            </label>
            <div class="relative">
              <User class="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
              <input
                v-model="name"
                required
                type="text"
                class="w-full pl-10 pr-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 focus:outline-none transition-all"
              />
            </div>
          </div>

          <!-- Target Engineering Role -->
          <div>
            <label class="block text-sm md:text-base font-bold text-slate-800 dark:text-slate-200 mb-2">
              {{ $t('profile.target_role') }}
            </label>
            <div class="relative">
              <Briefcase class="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
              <select
                v-model="targetRole"
                class="w-full pl-10 pr-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 focus:outline-none transition-all appearance-none"
              >
                <option v-for="r in roleOptions" :key="r.value" :value="r.value">
                  {{ r.label }}
                </option>
              </select>
            </div>
          </div>
        </div>

        <!-- Daily Goal Selector & Language -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
          <!-- Daily Goal Selector -->
          <div>
            <label class="block text-sm md:text-base font-bold text-slate-800 dark:text-slate-200 mb-2">
              {{ $t('profile.daily_goal') }}
            </label>
            <div class="grid grid-cols-4 gap-2">
              <button
                v-for="opt in dailyGoalOptions"
                :key="opt.minutes"
                type="button"
                @click="dailyGoalMinutes = opt.minutes"
                :class="[
                  'py-2.5 px-2 rounded-xl text-center border transition-all flex flex-col items-center justify-center gap-0.5',
                  dailyGoalMinutes === opt.minutes
                    ? 'bg-brand-50 dark:bg-brand-950/60 border-brand-500 dark:border-brand-400 text-brand-700 dark:text-brand-300 font-bold ring-2 ring-brand-500/20 shadow-sm'
                    : 'bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:border-slate-300 dark:hover:border-slate-700 font-medium'
                ]"
              >
                <span class="text-sm md:text-base font-black">{{ opt.label }}</span>
                <span class="text-[10px] sm:text-xs text-slate-500 dark:text-slate-400">{{ opt.desc }}</span>
              </button>
            </div>
          </div>

          <!-- Preferred Interface Language -->
          <div>
            <label class="block text-sm md:text-base font-bold text-slate-800 dark:text-slate-200 mb-2">
              {{ $t('profile.preferred_language') }}
            </label>
            <div class="grid grid-cols-2 gap-2">
              <button
                type="button"
                @click="preferredLocale = 'vi'"
                :class="[
                  'py-2.5 px-3 rounded-xl border transition-all flex items-center justify-center gap-2',
                  preferredLocale === 'vi'
                    ? 'bg-brand-50 dark:bg-brand-950/60 border-brand-500 dark:border-brand-400 text-brand-700 dark:text-brand-300 font-bold ring-2 ring-brand-500/20 shadow-sm'
                    : 'bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:border-slate-300 dark:hover:border-slate-700 font-medium'
                ]"
              >
                <span class="text-base">🇻🇳</span>
                <span class="text-sm md:text-base">Tiếng Việt</span>
              </button>

              <button
                type="button"
                @click="preferredLocale = 'en'"
                :class="[
                  'py-2.5 px-3 rounded-xl border transition-all flex items-center justify-center gap-2',
                  preferredLocale === 'en'
                    ? 'bg-brand-50 dark:bg-brand-950/60 border-brand-500 dark:border-brand-400 text-brand-700 dark:text-brand-300 font-bold ring-2 ring-brand-500/20 shadow-sm'
                    : 'bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:border-slate-300 dark:hover:border-slate-700 font-medium'
                ]"
              >
                <span class="text-base">🇬🇧</span>
                <span class="text-sm md:text-base">English</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Telegram Notifications Card -->
        <div class="p-4 sm:p-5 rounded-2xl bg-gradient-to-br from-sky-50/70 via-slate-50 to-indigo-50/40 dark:from-slate-950 dark:via-slate-950 dark:to-sky-950/30 border border-sky-200/80 dark:border-sky-900/40 space-y-3">
          <div class="flex items-center gap-2.5">
            <div class="w-8 h-8 rounded-xl bg-sky-500/10 dark:bg-sky-500/20 text-sky-600 dark:text-sky-400 flex items-center justify-center shrink-0">
              <Send class="w-4 h-4" />
            </div>
            <div>
              <label class="block text-sm md:text-base font-bold text-slate-900 dark:text-white">
                {{ $t('profile.telegram_id') }}
              </label>
              <p class="text-xs text-slate-500 dark:text-slate-400">
                {{ $t('profile.telegram_guide') }}
              </p>
            </div>
          </div>

          <div class="relative">
            <Send class="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              v-model.number="telegramChatId"
              type="number"
              :placeholder="$t('profile.telegram_placeholder')"
              class="w-full pl-10 pr-4 py-3 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20 focus:outline-none transition-all"
            />
          </div>
        </div>

        <!-- Submit Button -->
        <div class="flex justify-end pt-2">
          <button
            type="submit"
            :disabled="profileStore.isUpdating"
            class="w-full sm:w-auto flex items-center justify-center gap-2 px-8 py-3.5 rounded-2xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm md:text-base shadow-lg shadow-brand-500/20 hover:shadow-brand-500/30 transition-all active:scale-95 disabled:opacity-50"
          >
            <Save v-if="!profileStore.isUpdating" class="w-4 h-4" />
            <span v-if="profileStore.isUpdating" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            <span>{{ profileStore.isUpdating ? $t('profile.saving') : $t('profile.save_btn') }}</span>
          </button>
        </div>
      </form>

      <!-- Tab 2: Security & Password Form -->
      <form v-else @submit.prevent="handlePasswordChange" class="space-y-6">
        <!-- Google Connected Banner -->
        <div
          v-if="profileStore.profile?.isGoogleLinked"
          class="p-4 sm:p-5 rounded-2xl bg-blue-50/70 dark:bg-blue-950/40 border border-blue-200/80 dark:border-blue-900/40 flex items-start gap-3.5"
        >
          <div class="w-8 h-8 rounded-xl bg-blue-500/10 text-blue-600 dark:text-blue-400 flex items-center justify-center shrink-0 mt-0.5">
            <Shield class="w-4 h-4" />
          </div>
          <div class="space-y-1">
            <h4 class="text-sm md:text-base font-bold text-blue-900 dark:text-blue-200">
              {{ $t('profile.google_linked') }}
            </h4>
            <p class="text-xs sm:text-sm text-blue-700 dark:text-blue-300">
              {{ $t('profile.google_password_hint') }}
            </p>
          </div>
        </div>

        <!-- Current Password (if account already has password) -->
        <div v-if="profileStore.profile?.hasPassword">
          <label class="block text-sm md:text-base font-bold text-slate-800 dark:text-slate-200 mb-2">
            {{ $t('profile.current_password') }}
          </label>
          <div class="relative">
            <Lock class="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              v-model="currentPassword"
              required
              :type="showCurrentPassword ? 'text' : 'password'"
              placeholder="••••••••"
              class="w-full pl-10 pr-11 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 focus:outline-none transition-all"
            />
            <button
              type="button"
              @click="showCurrentPassword = !showCurrentPassword"
              class="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
            >
              <EyeOff v-if="showCurrentPassword" class="w-4 h-4" />
              <Eye v-else class="w-4 h-4" />
            </button>
          </div>
        </div>

        <!-- New Password -->
        <div>
          <label class="block text-sm md:text-base font-bold text-slate-800 dark:text-slate-200 mb-2">
            {{ $t('profile.new_password') }}
          </label>
          <div class="relative">
            <Lock class="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              v-model="newPassword"
              required
              :type="showNewPassword ? 'text' : 'password'"
              minlength="6"
              placeholder="••••••••"
              class="w-full pl-10 pr-11 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 focus:outline-none transition-all"
            />
            <button
              type="button"
              @click="showNewPassword = !showNewPassword"
              class="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
            >
              <EyeOff v-if="showNewPassword" class="w-4 h-4" />
              <Eye v-else class="w-4 h-4" />
            </button>
          </div>

          <!-- Password Strength Meter & Rules -->
          <div v-if="newPassword" class="mt-3 space-y-2.5">
            <div class="flex items-center justify-between text-xs">
              <span class="text-slate-500 dark:text-slate-400 font-medium">Độ an toàn mật khẩu:</span>
              <span class="font-bold" :class="pwdAnalysis.color.split(' ')[1]">
                {{ pwdAnalysis.label }}
              </span>
            </div>

            <!-- Progress bar -->
            <div class="h-2 w-full bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden">
              <div
                class="h-full transition-all duration-300 rounded-full"
                :class="pwdAnalysis.color.split(' ')[0]"
                :style="{ width: `${pwdAnalysis.width}%` }"
              ></div>
            </div>

            <!-- Validation checklist -->
            <div class="grid grid-cols-2 gap-2 text-xs pt-1">
              <div
                class="flex items-center gap-1.5 font-medium transition-colors"
                :class="pwdAnalysis.hasMinLength ? 'text-emerald-600 dark:text-emerald-400' : 'text-slate-400 dark:text-slate-600'"
              >
                <Check v-if="pwdAnalysis.hasMinLength" class="w-3.5 h-3.5 shrink-0 stroke-[3]" />
                <span v-else class="w-3.5 h-3.5 rounded-full border border-slate-400 dark:border-slate-600 shrink-0"></span>
                <span>{{ $t('profile.rule_length') }}</span>
              </div>

              <div
                class="flex items-center gap-1.5 font-medium transition-colors"
                :class="pwdAnalysis.hasMixedCase ? 'text-emerald-600 dark:text-emerald-400' : 'text-slate-400 dark:text-slate-600'"
              >
                <Check v-if="pwdAnalysis.hasMixedCase" class="w-3.5 h-3.5 shrink-0 stroke-[3]" />
                <span v-else class="w-3.5 h-3.5 rounded-full border border-slate-400 dark:border-slate-600 shrink-0"></span>
                <span>{{ $t('profile.rule_mixed') }}</span>
              </div>

              <div
                class="flex items-center gap-1.5 font-medium transition-colors"
                :class="pwdAnalysis.hasNumber ? 'text-emerald-600 dark:text-emerald-400' : 'text-slate-400 dark:text-slate-600'"
              >
                <Check v-if="pwdAnalysis.hasNumber" class="w-3.5 h-3.5 shrink-0 stroke-[3]" />
                <span v-else class="w-3.5 h-3.5 rounded-full border border-slate-400 dark:border-slate-600 shrink-0"></span>
                <span>{{ $t('profile.rule_number') }}</span>
              </div>

              <div
                class="flex items-center gap-1.5 font-medium transition-colors"
                :class="pwdAnalysis.hasSpecial ? 'text-emerald-600 dark:text-emerald-400' : 'text-slate-400 dark:text-slate-600'"
              >
                <Check v-if="pwdAnalysis.hasSpecial" class="w-3.5 h-3.5 shrink-0 stroke-[3]" />
                <span v-else class="w-3.5 h-3.5 rounded-full border border-slate-400 dark:border-slate-600 shrink-0"></span>
                <span>{{ $t('profile.rule_special') }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Confirm Password -->
        <div>
          <label class="block text-sm md:text-base font-bold text-slate-800 dark:text-slate-200 mb-2">
            {{ $t('profile.confirm_password') }}
          </label>
          <div class="relative">
            <Lock class="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              v-model="confirmPassword"
              required
              :type="showConfirmPassword ? 'text' : 'password'"
              minlength="6"
              placeholder="••••••••"
              class="w-full pl-10 pr-11 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 focus:outline-none transition-all"
            />
            <button
              type="button"
              @click="showConfirmPassword = !showConfirmPassword"
              class="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
            >
              <EyeOff v-if="showConfirmPassword" class="w-4 h-4" />
              <Eye v-else class="w-4 h-4" />
            </button>
          </div>
          <div v-if="confirmPassword && newPassword !== confirmPassword" class="text-xs text-rose-500 mt-2 font-medium">
            {{ $t('profile.passwords_must_match') }}
          </div>
        </div>

        <!-- Submit Button -->
        <div class="flex justify-end pt-2">
          <button
            type="submit"
            :disabled="profileStore.isUpdating || (confirmPassword !== '' && newPassword !== confirmPassword)"
            class="w-full sm:w-auto flex items-center justify-center gap-2 px-8 py-3.5 rounded-2xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm md:text-base shadow-lg shadow-brand-500/20 hover:shadow-brand-500/30 transition-all active:scale-95 disabled:opacity-50"
          >
            <span v-if="profileStore.isUpdating" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            <span>{{ profileStore.profile?.hasPassword ? $t('profile.update_password_btn') : $t('profile.set_password_btn') }}</span>
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

