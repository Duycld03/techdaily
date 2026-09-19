<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import {
  User,
  Shield,
  Briefcase,
  Save,
  Eye,
  EyeOff,
  Lock
} from 'lucide-vue-next'
import EngineerIdentityPassport from '~/components/profile/EngineerIdentityPassport.vue'
import EngineerMilestonesCard from '~/components/profile/EngineerMilestonesCard.vue'
import DomainGoalTracker from '~/components/profile/DomainGoalTracker.vue'
import AppSelect from '~/components/common/AppSelect.vue'
import { useApiError } from '~/composables/useApiError'
import { useProfileStore } from '~/stores/useProfileStore'
import { useInterviewQuizStore } from '~/stores/useInterviewQuizStore'
import { useAuthStore } from '~/stores/useAuthStore'

const profileStore = useProfileStore()
const quizStore = useInterviewQuizStore()
const authStore = useAuthStore()
const toast = useToast()
const { t } = useI18n()
const { formatError } = useApiError()

const activeTab = ref<'personal' | 'security'>('personal')

// Form state
const name = ref('')
const targetRole = ref('Senior Engineer')
const dailyGoalMinutes = ref(10)
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
  }
})

async function handleProfileSave() {
  try {
    await profileStore.updateProfile({
      name: name.value.trim(),
      targetRole: targetRole.value,
      dailyGoalMinutes: dailyGoalMinutes.value
    })

    toast.success(t('profile.save_success'))
  } catch (err: unknown) {
    toast.error(formatError(err, 'profile.save_failed'))
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
    const currentPwd = profileStore.profile?.hasPassword
      ? currentPassword.value
      : null
    await profileStore.changePassword(currentPwd, newPassword.value)
    toast.success(t('profile.password_set_success'))
    currentPassword.value = ''
    newPassword.value = ''
    confirmPassword.value = ''
  } catch (err: unknown) {
    toast.error(formatError(err, 'profile.password_change_failed'))
  }
}
</script>

<template>
  <div class="min-h-[calc(100vh-4rem)] bg-slate-50 dark:bg-canvas transition-colors duration-200">
    <div class="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-6 sm:py-8 space-y-6">
      <!-- Page Header -->
      <div class="space-y-1">
        <h1 class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white tracking-tight">
          {{ $t('profile.title') }}
        </h1>
        <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400">
          {{ $t('profile.subtitle') }}
        </p>
      </div>

      <!-- Tier 1: Top Full-Width Engineer Identity Passport Banner -->
      <EngineerIdentityPassport
        :profile="profileStore.profile"
        :stats="profileStore.stats"
      />

      <!-- Tier 2: Full-Width 4-Column Milestones Telemetry Strip -->
      <EngineerMilestonesCard
        :stats="profileStore.stats"
        :quizStats="quizStore.stats"
      />

      <!-- Tier 3: Executive 2-Column Balanced Lower Grid -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 items-start">
        <!-- Left Column (Desktop 50%, Mobile order-2): Account & Security Hub -->
        <div class="order-2 lg:order-1 space-y-6">
          <div class="glass-card p-5 sm:p-7 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] shadow-sm space-y-6 transition-colors duration-200">
            <!-- Tabs Switcher -->
            <div class="grid grid-cols-2 gap-1.5 p-1 rounded-xl bg-slate-100 dark:bg-canvas-elevated/80 border border-slate-200/80 dark:border-white/[0.08] text-xs sm:text-sm font-bold">
              <button
                type="button"
                @click="activeTab = 'personal'"
                :class="[
                  'min-h-[40px] px-3 py-1.5 rounded-lg transition-colors flex items-center justify-center gap-2 outline-none',
                  activeTab === 'personal'
                    ? 'bg-white dark:bg-canvas-subtle text-brand-600 dark:text-brand-400 font-bold shadow-sm border border-transparent dark:border-white/[0.06]'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 font-medium border border-transparent'
                ]"
              >
                <User class="w-4 h-4 shrink-0" />
                <span>{{ $t('profile.tab_personal') }}</span>
              </button>

              <button
                type="button"
                @click="activeTab = 'security'"
                :class="[
                  'min-h-[40px] px-3 py-1.5 rounded-lg transition-colors flex items-center justify-center gap-2 outline-none',
                  activeTab === 'security'
                    ? 'bg-white dark:bg-canvas-subtle text-brand-600 dark:text-brand-400 font-bold shadow-sm border border-transparent dark:border-white/[0.06]'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 font-medium border border-transparent'
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
                      class="w-full pl-9 pr-3 py-2.5 bg-white dark:bg-canvas-subtle border border-slate-300 dark:border-white/[0.08] rounded-xl text-sm text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500 shadow-sm transition-colors"
                    />
                  </div>
                </div>

                <!-- Target Level -->
                <div>
                  <label class="block text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200 mb-1.5">
                    {{ $t('profile.target_role') }}
                  </label>
                  <AppSelect
                    v-model="targetRole"
                    :options="roleOptions"
                    :icon="Briefcase"
                    :aria-label="$t('profile.target_role')"
                  />
                </div>
              </div>

              <!-- Interactive Daily Goal Pace selector chips -->
              <div>
                <label class="block text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200 mb-1.5">
                  {{ $t('profile.daily_goal') }}
                </label>
                <div class="grid grid-cols-4 gap-2 max-w-sm">
                  <button
                    v-for="opt in dailyGoalOptions"
                    :key="opt.minutes"
                    type="button"
                    @click="dailyGoalMinutes = opt.minutes"
                    :class="[
                      'py-2 rounded-xl text-center border transition-all text-xs sm:text-sm font-bold',
                      dailyGoalMinutes === opt.minutes
                        ? 'bg-brand-500/15 border-brand-500 text-brand-700 dark:text-brand-300 font-bold shadow-sm ring-1 ring-brand-500/20'
                        : 'bg-white dark:bg-canvas-subtle border-slate-200/80 dark:border-white/[0.08] text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-canvas-elevated'
                    ]"
                  >
                    {{ opt.label }}
                  </button>
                </div>
              </div>

              <!-- Save Button -->
              <div class="flex justify-end pt-2">
                <button
                  type="submit"
                  :disabled="profileStore.isUpdating"
                  class="w-full sm:w-auto flex items-center justify-center gap-2 px-6 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm shadow-md transition-all active:scale-[0.98] disabled:opacity-50"
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
                v-if="profileStore.profile?.isGoogleLinked && !profileStore.profile?.hasPassword"
                class="p-3.5 rounded-xl bg-blue-50/70 dark:bg-blue-950/30 border border-blue-200/80 dark:border-blue-500/20 flex items-center gap-2.5 text-xs text-blue-700 dark:text-blue-300"
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
                    class="w-full pl-9 pr-10 py-2.5 bg-white dark:bg-canvas-subtle border border-slate-300 dark:border-white/[0.08] rounded-xl text-sm text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500 shadow-sm transition-colors"
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
                    class="w-full pl-9 pr-10 py-2.5 bg-white dark:bg-canvas-subtle border border-slate-300 dark:border-white/[0.08] rounded-xl text-sm text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500 shadow-sm transition-colors"
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

                <!-- Password Strength Bar -->
                <div v-if="newPassword" class="mt-2 space-y-1">
                  <div class="h-1.5 w-full bg-slate-200 dark:bg-white/[0.06] rounded-full overflow-hidden">
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
                    class="w-full pl-9 pr-10 py-2.5 bg-white dark:bg-canvas-subtle border border-slate-300 dark:border-white/[0.08] rounded-xl text-sm text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500 shadow-sm transition-colors"
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
                  class="w-full sm:w-auto flex items-center justify-center gap-2 px-6 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm shadow-md transition-all active:scale-[0.98] disabled:opacity-50"
                >
                  <span v-if="profileStore.isUpdating" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                  <span>{{ profileStore.profile?.hasPassword ? $t('profile.update_password_btn') : $t('profile.set_password_btn') }}</span>
                </button>
              </div>
            </form>
          </div>

        </div>

        <!-- Right Column (Desktop 50%, Mobile order-1): Domain Mastery Goal Tracker -->
        <div class="order-1 lg:order-2 space-y-6">
          <DomainGoalTracker :topic-breakdown="quizStore.stats?.topicBreakdown" />
        </div>
      </div>
    </div>
  </div>
</template>
