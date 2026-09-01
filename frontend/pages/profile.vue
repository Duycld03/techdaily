<script setup lang="ts">
import { ref, onMounted } from 'vue'
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
  Save
} from 'lucide-vue-next'

const profileStore = useProfileStore()
const { t } = useI18n()

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
const passwordError = ref<string | null>(null)

onMounted(async () => {
  const data = await profileStore.fetchProfile()
  if (data?.user) {
    name.value = data.user.name
    targetRole.value = data.user.targetRole || 'Senior Engineer'
    dailyGoalMinutes.value = data.user.dailyGoalMinutes || 10
    preferredLocale.value = data.user.preferredLocale || 'en'
    telegramChatId.value = data.user.telegramChatId
  }
})

async function handleProfileSave() {
  try {
    await profileStore.updateProfile({
      name: name.value,
      targetRole: targetRole.value,
      dailyGoalMinutes: dailyGoalMinutes.value,
      preferredLocale: preferredLocale.value,
      telegramChatId: telegramChatId.value
    })
  } catch {
    // handled in store
  }
}

async function handlePasswordChange() {
  passwordError.value = null
  if (newPassword.value.length < 6) {
    passwordError.value = 'New password must be at least 6 characters.'
    return
  }

  if (newPassword.value !== confirmPassword.value) {
    passwordError.value = 'Passwords do not match.'
    return
  }

  try {
    await profileStore.changePassword(currentPassword.value, newPassword.value)
    currentPassword.value = ''
    newPassword.value = ''
    confirmPassword.value = ''
  } catch (err: any) {
    passwordError.value = err.message || 'Failed to change password.'
  }
}
</script>

<template>
  <div class="max-w-4xl mx-auto p-4 sm:p-6 md:p-10 space-y-6 sm:space-y-8 bg-slate-50 dark:bg-slate-950 transition-colors duration-200">
    <!-- Header Banner -->
    <div class="p-5 sm:p-8 rounded-2xl sm:rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-md dark:shadow-xl flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 sm:gap-6">
      <div class="flex items-center gap-3.5 sm:gap-5 min-w-0 w-full">
        <div class="w-14 h-14 sm:w-20 sm:h-20 rounded-2xl sm:rounded-3xl bg-gradient-to-tr from-brand-600 to-emerald-500 flex items-center justify-center text-white font-black text-xl sm:text-3xl shadow-lg shadow-brand-500/20 shrink-0">
          {{ profileStore.profile?.name?.charAt(0).toUpperCase() || 'U' }}
        </div>

        <div class="min-w-0 flex-1">
          <div class="flex items-center gap-2 flex-wrap">
            <h1 class="text-xl sm:text-3xl font-black text-slate-900 dark:text-white tracking-tight truncate">
              {{ profileStore.profile?.name || 'Engineer' }}
            </h1>
            <span class="px-2.5 py-0.5 sm:px-3 sm:py-1 rounded-xl bg-brand-100 dark:bg-brand-950 border border-brand-200 dark:border-brand-800 text-brand-800 dark:text-brand-300 font-bold text-xs shrink-0">
              {{ profileStore.profile?.targetRole || 'Senior Engineer' }}
            </span>
          </div>

          <p class="text-xs sm:text-sm md:text-base font-mono text-slate-500 dark:text-slate-400 mt-0.5 sm:mt-1 truncate">
            {{ profileStore.profile?.email }}
          </p>

          <p class="text-xs sm:text-sm md:text-base text-slate-500 dark:text-slate-400 mt-0.5 sm:mt-1 font-medium">
            {{ $t('profile.target_pace', { minutes: profileStore.profile?.dailyGoalMinutes || 10 }) }}
          </p>
        </div>
      </div>
    </div>

    <!-- Learning Metrics Cards -->
    <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 sm:gap-4">
      <div class="p-3.5 sm:p-5 rounded-2xl sm:rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-1">
        <div class="flex items-center gap-1.5 sm:gap-2 text-xs font-bold uppercase tracking-wider text-amber-500">
          <Flame class="w-4 h-4" />
          <span>{{ $t('profile.active_streak') }}</span>
        </div>
        <div class="text-xl sm:text-3xl font-black text-slate-900 dark:text-white">
          {{ profileStore.stats?.currentStreak ?? 0 }} <span class="text-xs font-normal text-slate-500">{{ $t('profile.days') }}</span>
        </div>
        <div class="text-xs text-slate-500 dark:text-slate-400">
          {{ $t('profile.freeze_credits', { count: profileStore.stats?.freezeCreditsRemaining ?? 2 }) }}
        </div>
      </div>

      <div class="p-3.5 sm:p-5 rounded-2xl sm:rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-1">
        <div class="flex items-center gap-1.5 sm:gap-2 text-xs font-bold uppercase tracking-wider text-emerald-500">
          <CheckCircle2 class="w-4 h-4" />
          <span>{{ $t('profile.drills_completed') }}</span>
        </div>
        <div class="text-xl sm:text-3xl font-black text-slate-900 dark:text-white">
          {{ profileStore.stats?.totalDrillsCompleted ?? 0 }}
        </div>
        <div class="text-xs text-slate-500 dark:text-slate-400">
          {{ $t('profile.interview_challenges') }}
        </div>
      </div>

      <div class="p-3.5 sm:p-5 rounded-2xl sm:rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-1">
        <div class="flex items-center gap-1.5 sm:gap-2 text-xs font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400">
          <Award class="w-4 h-4" />
          <span>{{ $t('profile.avg_score') }}</span>
        </div>
        <div class="text-xl sm:text-3xl font-black text-slate-900 dark:text-white">
          {{ profileStore.stats?.averageScore ?? 0 }} <span class="text-xs font-normal text-slate-500">/ 10</span>
        </div>
        <div class="text-xs text-slate-500 dark:text-slate-400">
          {{ $t('profile.ai_evaluated') }}
        </div>
      </div>

      <div class="p-3.5 sm:p-5 rounded-2xl sm:rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-1">
        <div class="flex items-center gap-1.5 sm:gap-2 text-xs font-bold uppercase tracking-wider text-blue-500">
          <Layers class="w-4 h-4" />
          <span>{{ $t('profile.cards_in_deck') }}</span>
        </div>
        <div class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white">
          {{ profileStore.stats?.totalCardsInDeck ?? 0 }}
        </div>
        <div class="text-xs text-slate-500 dark:text-slate-400">
          {{ $t('profile.spaced_repetition') }}
        </div>
      </div>
    </div>

    <!-- Alert / Feedback Message -->
    <div v-if="profileStore.successMessage" class="p-4 rounded-2xl bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-900 text-sm text-emerald-800 dark:text-emerald-300 font-semibold text-center animate-in fade-in">
      {{ profileStore.successMessage }}
    </div>

    <div v-if="profileStore.error" class="p-4 rounded-2xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900 text-sm text-rose-800 dark:text-rose-300 font-semibold text-center animate-in fade-in">
      {{ profileStore.error }}
    </div>

    <!-- Tabs Container -->
    <div class="p-4 sm:p-6 md:p-8 rounded-2xl sm:rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-md dark:shadow-xl space-y-6">
      <!-- Tab Headers -->
      <div class="grid grid-cols-2 gap-1.5 p-1 sm:p-1.5 rounded-2xl bg-slate-100 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 text-xs sm:text-sm font-bold">
        <button
          type="button"
          @click="activeTab = 'personal'"
          :class="[
            'min-h-[42px] sm:min-h-[46px] px-1.5 sm:px-4 py-2 rounded-xl transition-all flex items-center justify-center gap-1.5 sm:gap-2 outline-none focus:outline-none text-center',
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
            'min-h-[42px] sm:min-h-[46px] px-1.5 sm:px-4 py-2 rounded-xl transition-all flex items-center justify-center gap-1.5 sm:gap-2 outline-none focus:outline-none text-center',
            activeTab === 'security'
              ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 font-bold shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 font-medium'
          ]"
        >
          <Shield class="w-4 h-4 shrink-0" />
          <span class="whitespace-nowrap">{{ $t('profile.tab_security') }}</span>
        </button>
      </div>

      <!-- Tab 1: Profile & Goals -->
      <form v-if="activeTab === 'personal'" @submit.prevent="handleProfileSave" class="space-y-5">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
          <div>
            <label class="block text-sm md:text-base font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('profile.full_name') }}</label>
            <div class="relative">
              <User class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
              <input
                v-model="name"
                required
                type="text"
                class="w-full pl-10 pr-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none transition-colors"
              />
            </div>
          </div>

          <div>
            <label class="block text-sm md:text-base font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('profile.target_role') }}</label>
            <div class="relative">
              <Briefcase class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
              <select
                v-model="targetRole"
                class="w-full pl-10 pr-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none transition-colors"
              >
                <option value="Senior Engineer">Senior Software Engineer</option>
                <option value="Principal Architect">Principal Software Architect</option>
                <option value="Staff Engineer">Staff Software Engineer</option>
                <option value="Tech Lead">Engineering Tech Lead</option>
                <option value="Mid-Level Engineer">Mid-Level Software Engineer</option>
              </select>
            </div>
          </div>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
          <div>
            <label class="block text-sm md:text-base font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('profile.daily_goal') }}</label>
            <div class="relative">
              <Clock class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
              <select
                v-model="dailyGoalMinutes"
                class="w-full pl-10 pr-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none transition-colors"
              >
                <option :value="5">5 Minutes</option>
                <option :value="10">10 Minutes</option>
                <option :value="15">15 Minutes</option>
                <option :value="30">30 Minutes</option>
              </select>
            </div>
          </div>

          <div>
            <label class="block text-sm md:text-base font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('profile.telegram_id') }}</label>
            <div class="relative">
              <Send class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
              <input
                v-model.number="telegramChatId"
                type="number"
                :placeholder="$t('profile.telegram_placeholder')"
                class="w-full pl-10 pr-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none transition-colors"
              />
            </div>
          </div>
        </div>

        <div class="flex justify-end pt-3">
          <button
            type="submit"
            :disabled="profileStore.isUpdating"
            class="flex items-center gap-2 px-6 py-3 rounded-2xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-sm md:text-base shadow-md transition-all active:scale-95 disabled:opacity-50"
          >
            <Save v-if="!profileStore.isUpdating" class="w-4 h-4" />
            <span v-if="profileStore.isUpdating" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            <span>{{ profileStore.isUpdating ? $t('profile.saving') : $t('profile.save_btn') }}</span>
          </button>
        </div>
      </form>

      <!-- Tab 2: Security & Password -->
      <form v-else @submit.prevent="handlePasswordChange" class="space-y-5">
        <div v-if="passwordError" class="p-3.5 rounded-xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900 text-sm text-rose-800 dark:text-rose-300 text-center font-semibold">
          {{ passwordError }}
        </div>

        <div v-if="profileStore.profile?.hasPassword">
          <label class="block text-sm md:text-base font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('profile.current_password') }}</label>
          <div class="relative">
            <Lock class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
            <input
              v-model="currentPassword"
              required
              type="password"
              placeholder="••••••••"
              class="w-full pl-10 pr-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none transition-colors"
            />
          </div>
        </div>

        <div>
          <label class="block text-sm md:text-base font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('profile.new_password') }}</label>
          <div class="relative">
            <Lock class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
            <input
              v-model="newPassword"
              required
              type="password"
              minlength="6"
              placeholder="••••••••"
              class="w-full pl-10 pr-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none transition-colors"
            />
          </div>
        </div>

        <div>
          <label class="block text-sm md:text-base font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('profile.confirm_password') }}</label>
          <div class="relative">
            <Lock class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
            <input
              v-model="confirmPassword"
              required
              type="password"
              minlength="6"
              placeholder="••••••••"
              class="w-full pl-10 pr-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none transition-colors"
            />
          </div>
        </div>

        <div class="flex justify-end pt-3">
          <button
            type="submit"
            :disabled="profileStore.isUpdating"
            class="flex items-center gap-2 px-6 py-3 rounded-2xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-sm md:text-base shadow-md transition-all active:scale-95 disabled:opacity-50"
          >
            <span v-if="profileStore.isUpdating" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            <span>{{ profileStore.profile?.hasPassword ? $t('profile.update_password_btn') : $t('profile.set_password_btn') }}</span>
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
