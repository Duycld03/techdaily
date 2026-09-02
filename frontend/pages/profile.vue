<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import {
  User,
  Shield,
  Flame,
  CheckCircle2,
  Lock,
  Mail,
  Briefcase,
  Send,
  Save,
  Eye,
  EyeOff,
  Target
} from 'lucide-vue-next'

const profileStore = useProfileStore()
const quizStore = useInterviewQuizStore()
const authStore = useAuthStore()
const toast = useToast()
const { t } = useI18n()

const activeTab = ref<'personal' | 'security'>('personal')
const hasAvatarError = ref(false)

// Form state
const name = ref('')
const targetRole = ref('Senior Engineer')
const dailyGoalMinutes = ref(10)
const telegramChatId = ref<number | undefined>(undefined)

// Password form state
const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const showCurrentPassword = ref(false)
const showNewPassword = ref(false)
const showConfirmPassword = ref(false)

const roleOptions = [
  { value: 'Senior Engineer', label: 'Senior Software Engineer' },
  { value: 'Staff Engineer', label: 'Staff Software Engineer' },
  { value: 'Principal Architect', label: 'Principal Software Architect' },
  { value: 'Tech Lead', label: 'Engineering Tech Lead' },
  { value: 'Mid-Level Engineer', label: 'Mid-Level Software Engineer' },
  { value: 'Junior Engineer', label: 'Junior Software Engineer' },
  { value: 'Fresher Engineer', label: 'Fresher / Entry Engineer' }
]

const dailyGoalOptions = [
  { minutes: 5, label: '5m' },
  { minutes: 10, label: '10m' },
  { minutes: 15, label: '15m' },
  { minutes: 30, label: '30m' }
]

// Password strength analysis
const pwdAnalysis = computed(() => {
  const pwd = newPassword.value
  if (!pwd) return { label: '', color: '', width: 0 }
  const hasMinLength = pwd.length >= 8
  const hasUpper = /[A-Z]/.test(pwd)
  const hasLower = /[a-z]/.test(pwd)
  const hasNumber = /[0-9]/.test(pwd)
  const hasSpecial = /[^A-Za-z0-9]/.test(pwd)
  const score = (hasMinLength ? 1 : 0) + ((hasUpper && hasLower) ? 1 : 0) + (hasNumber ? 1 : 0) + (hasSpecial ? 1 : 0)

  if (score >= 3 && pwd.length >= 8) {
    return { label: t('profile.password_strength_strong'), color: 'bg-emerald-500 text-emerald-500', width: 100 }
  } else if (score >= 2) {
    return { label: t('profile.password_strength_good'), color: 'bg-amber-500 text-amber-500', width: 65 }
  }
  return { label: t('profile.password_strength_weak'), color: 'bg-rose-500 text-rose-500', width: 30 }
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
    telegramChatId.value = data.user.telegramChatId
  }
})

async function handleProfileSave() {
  try {
    await profileStore.updateProfile({
      name: name.value.trim(),
      targetRole: targetRole.value,
      dailyGoalMinutes: dailyGoalMinutes.value,
      telegramChatId: telegramChatId.value
    })

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
  <div class="max-w-3xl mx-auto p-4 sm:p-6 md:p-8 space-y-6 bg-slate-50 dark:bg-slate-950 transition-colors duration-200">
    <!-- Clean Top Hero Card -->
    <div class="p-6 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm flex flex-col sm:flex-row items-center sm:items-center justify-between gap-5">
      <div class="flex items-center gap-4 min-w-0 w-full">
        <!-- Avatar -->
        <div class="shrink-0">
          <img
            v-if="profileStore.profile?.avatarUrl && !hasAvatarError"
            :src="profileStore.profile.avatarUrl"
            :alt="profileStore.profile.name"
            @error="hasAvatarError = true"
            class="w-16 h-16 rounded-2xl object-cover border border-slate-200 dark:border-slate-700 shadow-sm"
          />
          <div
            v-else
            class="w-16 h-16 rounded-2xl bg-gradient-to-tr from-brand-600 to-emerald-500 flex items-center justify-center text-white font-black text-2xl shadow-sm"
          >
            {{ profileStore.profile?.name?.charAt(0).toUpperCase() || 'U' }}
          </div>
        </div>

        <!-- Name & Status -->
        <div class="min-w-0 flex-1 space-y-1">
          <div class="flex items-center gap-2 flex-wrap">
            <h1 class="text-xl sm:text-2xl font-black text-slate-900 dark:text-white tracking-tight truncate">
              {{ profileStore.profile?.name || 'Engineer' }}
            </h1>
            <span class="px-2.5 py-0.5 rounded-full bg-brand-50 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800/60 text-brand-700 dark:text-brand-300 font-bold text-xs shrink-0 flex items-center gap-1">
              <Briefcase class="w-3 h-3" />
              {{ profileStore.profile?.targetRole || 'Senior Engineer' }}
            </span>
          </div>

          <p class="text-xs sm:text-sm font-mono text-slate-500 dark:text-slate-400 truncate">
            {{ profileStore.profile?.email }}
          </p>

          <div class="flex items-center gap-2 pt-1 text-xs font-semibold text-slate-600 dark:text-slate-400">
            <span
              v-if="profileStore.profile?.isGoogleLinked"
              class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-md bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-xs"
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
              class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-md bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-xs"
            >
              <Mail class="w-3 h-3 text-brand-500" />
              {{ $t('profile.standard_account') }}
            </span>

            <span>•</span>
            <span>{{ $t('profile.target_pace', { minutes: profileStore.profile?.dailyGoalMinutes || 10 }) }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- 3 Clean Metrics Cards -->
    <div class="grid grid-cols-3 gap-2.5 sm:gap-4">
      <!-- 1. Streak -->
      <div class="px-2 py-3.5 sm:p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm text-center space-y-1">
        <div class="text-[11px] sm:text-xs font-bold uppercase tracking-wider text-amber-500 flex items-center justify-center gap-1 whitespace-nowrap">
          <Flame class="w-3.5 h-3.5 shrink-0" />
          <span>{{ $t('profile.active_streak') }}</span>
        </div>
        <div class="text-xl sm:text-3xl font-black text-slate-900 dark:text-white">
          {{ profileStore.stats?.currentStreak ?? 0 }}
          <span class="text-xs font-normal text-slate-500">{{ $t('profile.days') }}</span>
        </div>
      </div>

      <!-- 2. Drills -->
      <div class="px-2 py-3.5 sm:p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm text-center space-y-1">
        <div class="text-[11px] sm:text-xs font-bold uppercase tracking-wider text-emerald-500 flex items-center justify-center gap-1 whitespace-nowrap">
          <CheckCircle2 class="w-3.5 h-3.5 shrink-0" />
          <span>{{ $t('profile.drills_completed') }}</span>
        </div>
        <div class="text-xl sm:text-3xl font-black text-slate-900 dark:text-white">
          {{ profileStore.stats?.totalDrillsCompleted ?? 0 }}
        </div>
      </div>

      <!-- 3. Quiz -->
      <div class="px-2 py-3.5 sm:p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm text-center space-y-1">
        <div class="text-[11px] sm:text-xs font-bold uppercase tracking-wider text-violet-500 flex items-center justify-center gap-1 whitespace-nowrap">
          <Target class="w-3.5 h-3.5 shrink-0" />
          <span>{{ $t('profile.quiz_mastery') }}</span>
        </div>
        <div class="text-xl sm:text-3xl font-black text-slate-900 dark:text-white">
          {{ quizStore.stats?.accuracyRate ?? 0 }}%
        </div>
      </div>
    </div>

    <!-- Main Clean Tab Container -->
    <div class="p-5 sm:p-7 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm space-y-6">
      <!-- Tabs Switcher -->
      <div class="grid grid-cols-2 gap-1.5 p-1 rounded-xl bg-slate-100 dark:bg-slate-950 border border-slate-200/80 dark:border-slate-800 text-xs sm:text-sm font-bold">
        <button
          type="button"
          @click="activeTab = 'personal'"
          :class="[
            'min-h-[40px] px-3 py-1.5 rounded-lg transition-all flex items-center justify-center gap-2 outline-none',
            activeTab === 'personal'
              ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 font-bold shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 font-medium'
          ]"
        >
          <User class="w-4 h-4 shrink-0" />
          <span>{{ $t('profile.tab_personal') }}</span>
        </button>

        <button
          type="button"
          @click="activeTab = 'security'"
          :class="[
            'min-h-[40px] px-3 py-1.5 rounded-lg transition-all flex items-center justify-center gap-2 outline-none',
            activeTab === 'security'
              ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 font-bold shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 font-medium'
          ]"
        >
          <Shield class="w-4 h-4 shrink-0" />
          <span>{{ $t('profile.tab_security') }}</span>
        </button>
      </div>

      <!-- Tab 1: Personal Info Form -->
      <form v-if="activeTab === 'personal'" @submit.prevent="handleProfileSave" class="space-y-5">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <!-- Full Name -->
          <div>
            <label class="block text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200 mb-1.5">
              {{ $t('profile.full_name') }}
            </label>
            <div class="relative">
              <User class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
              <input
                v-model="name"
                required
                type="text"
                class="w-full pl-9 pr-3 py-2.5 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none transition-all"
              />
            </div>
          </div>

          <!-- Target Level -->
          <div>
            <label class="block text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200 mb-1.5">
              {{ $t('profile.target_role') }}
            </label>
            <div class="relative">
              <Briefcase class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
              <select
                v-model="targetRole"
                class="w-full pl-9 pr-3 py-2.5 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none transition-all"
              >
                <option v-for="r in roleOptions" :key="r.value" :value="r.value">
                  {{ r.label }}
                </option>
              </select>
            </div>
          </div>
        </div>

        <!-- Daily Goal & Telegram Chat ID -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <!-- Goal Selection -->
          <div>
            <label class="block text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200 mb-1.5">
              {{ $t('profile.daily_goal') }}
            </label>
            <div class="grid grid-cols-4 gap-1.5">
              <button
                v-for="opt in dailyGoalOptions"
                :key="opt.minutes"
                type="button"
                @click="dailyGoalMinutes = opt.minutes"
                :class="[
                  'py-2 rounded-xl text-center border transition-all text-xs sm:text-sm font-bold',
                  dailyGoalMinutes === opt.minutes
                    ? 'bg-brand-50 dark:bg-brand-950/60 border-brand-500 text-brand-700 dark:text-brand-300 ring-1 ring-brand-500/20'
                    : 'bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300'
                ]"
              >
                {{ opt.label }}
              </button>
            </div>
          </div>

          <!-- Telegram Chat ID -->
          <div>
            <label class="block text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200 mb-1.5">
              {{ $t('profile.telegram_id') }}
            </label>
            <div class="relative">
              <Send class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
              <input
                v-model.number="telegramChatId"
                type="number"
                :placeholder="$t('profile.telegram_placeholder')"
                class="w-full pl-9 pr-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none transition-all"
              />
            </div>
          </div>
        </div>

        <!-- Save Button -->
        <div class="flex justify-end pt-2">
          <button
            type="submit"
            :disabled="profileStore.isUpdating"
            class="w-full sm:w-auto flex items-center justify-center gap-2 px-6 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm shadow-md transition-all active:scale-95 disabled:opacity-50"
          >
            <Save v-if="!profileStore.isUpdating" class="w-4 h-4" />
            <span v-if="profileStore.isUpdating" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            <span>{{ profileStore.isUpdating ? $t('profile.saving') : $t('profile.save_btn') }}</span>
          </button>
        </div>
      </form>

      <!-- Tab 2: Security & Password Form -->
      <form v-else @submit.prevent="handlePasswordChange" class="space-y-4">
        <!-- Google Connected Banner -->
        <div
          v-if="profileStore.profile?.isGoogleLinked"
          class="p-3.5 rounded-xl bg-blue-50/70 dark:bg-blue-950/40 border border-blue-200/80 dark:border-blue-900/40 flex items-center gap-2.5 text-xs text-blue-700 dark:text-blue-300"
        >
          <Shield class="w-4 h-4 shrink-0 text-blue-500" />
          <span>{{ $t('profile.google_password_hint') }}</span>
        </div>

        <!-- Current Password -->
        <div v-if="profileStore.profile?.hasPassword">
          <label class="block text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200 mb-1.5">
            {{ $t('profile.current_password') }}
          </label>
          <div class="relative">
            <Lock class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              v-model="currentPassword"
              required
              :type="showCurrentPassword ? 'text' : 'password'"
              placeholder="••••••••"
              class="w-full pl-9 pr-10 py-2.5 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none transition-all"
            />
            <button
              type="button"
              @click="showCurrentPassword = !showCurrentPassword"
              class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
            >
              <EyeOff v-if="showCurrentPassword" class="w-4 h-4" />
              <Eye v-else class="w-4 h-4" />
            </button>
          </div>
        </div>

        <!-- New Password -->
        <div>
          <label class="block text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200 mb-1.5">
            {{ $t('profile.new_password') }}
          </label>
          <div class="relative">
            <Lock class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              v-model="newPassword"
              required
              :type="showNewPassword ? 'text' : 'password'"
              minlength="6"
              placeholder="••••••••"
              class="w-full pl-9 pr-10 py-2.5 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none transition-all"
            />
            <button
              type="button"
              @click="showNewPassword = !showNewPassword"
              class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
            >
              <EyeOff v-if="showNewPassword" class="w-4 h-4" />
              <Eye v-else class="w-4 h-4" />
            </button>
          </div>

          <!-- Strength Bar -->
          <div v-if="newPassword" class="mt-2 space-y-1">
            <div class="h-1.5 w-full bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden">
              <div
                class="h-full transition-all duration-300 rounded-full"
                :class="pwdAnalysis.color.split(' ')[0]"
                :style="{ width: `${pwdAnalysis.width}%` }"
              ></div>
            </div>
            <div class="text-[11px] font-semibold text-right" :class="pwdAnalysis.color.split(' ')[1]">
              {{ pwdAnalysis.label }}
            </div>
          </div>
        </div>

        <!-- Confirm Password -->
        <div>
          <label class="block text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200 mb-1.5">
            {{ $t('profile.confirm_password') }}
          </label>
          <div class="relative">
            <Lock class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              v-model="confirmPassword"
              required
              :type="showConfirmPassword ? 'text' : 'password'"
              minlength="6"
              placeholder="••••••••"
              class="w-full pl-9 pr-10 py-2.5 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none transition-all"
            />
            <button
              type="button"
              @click="showConfirmPassword = !showConfirmPassword"
              class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
            >
              <EyeOff v-if="showConfirmPassword" class="w-4 h-4" />
              <Eye v-else class="w-4 h-4" />
            </button>
          </div>
          <div v-if="confirmPassword && newPassword !== confirmPassword" class="text-xs text-rose-500 mt-1 font-medium">
            {{ $t('profile.passwords_must_match') }}
          </div>
        </div>

        <!-- Submit Button -->
        <div class="flex justify-end pt-2">
          <button
            type="submit"
            :disabled="profileStore.isUpdating || (confirmPassword !== '' && newPassword !== confirmPassword)"
            class="w-full sm:w-auto flex items-center justify-center gap-2 px-6 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm shadow-md transition-all active:scale-95 disabled:opacity-50"
          >
            <span v-if="profileStore.isUpdating" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            <span>{{ profileStore.profile?.hasPassword ? $t('profile.update_password_btn') : $t('profile.set_password_btn') }}</span>
          </button>
        </div>
      </form>
    </div>
  </div>
</template>


