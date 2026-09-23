<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import {
  Settings as SettingsIcon,
  Globe,
  Bell,
  Loader2,
  Clock,
  Compass,
  AlertCircle,
  CheckCircle2,
  Send,
  User,
  Shield,
  Briefcase,
  Save,
  Eye,
  EyeOff,
  Lock,
  Target,
  Palette
} from 'lucide-vue-next'
import AppSelect from '~/components/common/AppSelect.vue'
import AppTimePicker from '~/components/common/AppTimePicker.vue'
import MasterDetailLayout from '~/components/layout/MasterDetailLayout.vue'
import { useProfileStore } from '~/stores/useProfileStore'
import { useInterviewQuizStore } from '~/stores/useInterviewQuizStore'
import { useAuthStore } from '~/stores/useAuthStore'
import { useWebPush } from '~/composables/useWebPush'
import { ApiError } from '~/composables/useApiClient'
import { useApiError } from '~/composables/useApiError'
import { useStorage } from '@vueuse/core'

const route = useRoute()
const router = useRouter()
const { t, locale, setLocale } = useI18n()
const colorMode = useColorMode()
const toast = useToast()
const { formatError } = useApiError()

const profileStore = useProfileStore()
const quizStore = useInterviewQuizStore()
const authStore = useAuthStore()

export type SettingsTab = 'general' | 'notifications' | 'security'
const validTabs: SettingsTab[] = ['general', 'notifications', 'security']

const activeTab = ref<SettingsTab>('general')

function setTab(tab: SettingsTab | 'profile') {
  const targetTab: SettingsTab = tab === 'profile' ? 'general' : tab
  activeTab.value = targetTab
  router.replace({
    query: {
      ...route.query,
      tab: targetTab
    }
  })
}

// 1. General & Learning Preferences State
const name = ref('')
const targetRole = ref('Senior Engineer')
const dailyGoalMinutes = ref(10)
const autoAdvanceCards = useStorage('techdaily-auto-advance-cards', true)
const isSaving = ref(false)

const difficultyOptions = computed(() => [
  { value: 'Senior Engineer', label: t('settings.role_senior') },
  { value: 'Staff Engineer', label: t('settings.role_staff') },
  { value: 'Principal Architect', label: t('settings.role_principal') },
  { value: 'Tech Lead', label: t('settings.role_tech_lead') },
  { value: 'Mid-Level Engineer', label: t('settings.role_mid') },
  { value: 'Junior Engineer', label: t('settings.role_junior') }
])

const dailyGoalOptions = computed(() => [
  { value: 5, label: `10 ${t('settings.cards_per_day')} (5m)` },
  { value: 10, label: `20 ${t('settings.cards_per_day')} (10m)` },
  { value: 15, label: `30 ${t('settings.cards_per_day')} (15m)` },
  { value: 30, label: `50 ${t('settings.cards_per_day')} (30m)` }
])

const themeOptions = computed(() => [
  { value: 'dark', label: t('settings.theme_dark') },
  { value: 'light', label: t('settings.theme_light') },
  { value: 'system', label: t('settings.theme_system') }
])

const currentTheme = computed({
  get: () => colorMode.preference || 'dark',
  set: (val: string) => {
    colorMode.preference = val
  }
})

const languageOptions = [
  { value: 'en', label: 'English (US)' },
  { value: 'vi', label: 'Tiếng Việt (VN)' }
]

const currentLanguage = computed({
  get: () => locale.value,
  set: (val: string) => {
    setLocale(val as 'en' | 'vi')
  }
})

// 2. Schedule & Timezone State
const preferredStudyTime = ref('08:00')
const streakAlertTime = ref('20:00')
const timeZone = ref('UTC')

const defaultTimezones = [
  { value: 'UTC', label: 'UTC (Coordinated Universal Time)' },
  { value: 'Asia/Ho_Chi_Minh', label: 'Asia/Ho_Chi_Minh (GMT+7)' },
  { value: 'Asia/Tokyo', label: 'Asia/Tokyo (GMT+9)' },
  { value: 'Asia/Singapore', label: 'Asia/Singapore (GMT+8)' },
  { value: 'Europe/London', label: 'Europe/London (GMT+0)' },
  { value: 'Europe/Paris', label: 'Europe/Paris (GMT+1)' },
  { value: 'America/New_York', label: 'America/New_York (GMT-5)' },
  { value: 'America/Chicago', label: 'America/Chicago (GMT-6)' },
  { value: 'America/Denver', label: 'America/Denver (GMT-7)' },
  { value: 'America/Los_Angeles', label: 'America/Los_Angeles (GMT-8)' }
]

const commonTimezones = ref([...defaultTimezones])

function registerTimezone(tz: string, labelSuffix: string) {
  if (!tz) return
  if (!commonTimezones.value.some(item => item.value === tz)) {
    commonTimezones.value.unshift({ value: tz, label: `${tz} (${labelSuffix})` })
  }
}

// 3. Password & Security State
const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const showCurrentPassword = ref(false)
const showNewPassword = ref(false)
const showConfirmPassword = ref(false)

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

// 4. Web Push State
const {
  isPushSupported,
  isSubscribed,
  isLoading: isPushLoading,
  checkSubscriptionStatus,
  subscribeUser,
  unsubscribeUser,
  sendTestPush
} = useWebPush()

const showBraveGuide = ref(false)
const isSendingTest = ref(false)

onMounted(async () => {
  // 1. Resolve deep-link query tab
  const queryTab = route.query.tab as string
  if (queryTab === 'profile' || queryTab === 'general') {
    activeTab.value = 'general'
  } else if (queryTab && validTabs.includes(queryTab as SettingsTab)) {
    activeTab.value = queryTab as SettingsTab
  }

  // 2. Auto-detect browser timezone
  try {
    const detectedTz = Intl.DateTimeFormat().resolvedOptions().timeZone
    if (detectedTz) {
      timeZone.value = detectedTz
      registerTimezone(detectedTz, 'Local')
    }
  } catch {
    // fallback to UTC
  }

  // 3. Fetch profile and stats
  const [profileData] = await Promise.all([
    profileStore.fetchProfile(),
    quizStore.fetchStats()
  ])

  if (profileData?.user) {
    name.value = profileData.user.name || ''
    targetRole.value = profileData.user.targetRole || 'Senior Engineer'
    dailyGoalMinutes.value = profileData.user.dailyGoalMinutes || 10

    if (profileData.user.preferredStudyTime) {
      preferredStudyTime.value = profileData.user.preferredStudyTime
    }
    if (profileData.user.streakAlertTime) {
      streakAlertTime.value = profileData.user.streakAlertTime
    }
    if (profileData.user.timeZone) {
      timeZone.value = profileData.user.timeZone
      registerTimezone(profileData.user.timeZone, 'Saved')
    }
  }

  // 4. Check push subscription status
  await checkSubscriptionStatus()

  // 5. Brave push check
  if (typeof navigator !== 'undefined' && (navigator as any).brave && typeof (navigator as any).brave.isBrave === 'function') {
    (navigator as any).brave.isBrave().then((isB: boolean) => {
      if (isB && !isSubscribed.value) {
        showBraveGuide.value = true
      }
    }).catch(() => {})
  }
})

// Watch query for browser back/forward
watch(
  () => route.query.tab,
  (newTab) => {
    if (newTab === 'profile' || newTab === 'general') {
      activeTab.value = 'general'
    } else if (newTab && validTabs.includes(newTab as SettingsTab)) {
      activeTab.value = newTab as SettingsTab
    }
  }
)

const isSavingSchedule = ref(false)

async function handleSaveSchedule() {
  isSavingSchedule.value = true
  try {
    await profileStore.updateProfile({
      preferredStudyTime: preferredStudyTime.value,
      streakAlertTime: streakAlertTime.value,
      timeZone: timeZone.value
    })
    toast.success(t('settings.schedule_saved_success'))
  } catch (err: unknown) {
    toast.error(formatError(err, 'settings.schedule_saved_failed'))
  } finally {
    isSavingSchedule.value = false
  }
}

async function handleProfileSave() {
  isSaving.value = true
  try {
    await profileStore.updateProfile({
      name: name.value.trim(),
      targetRole: targetRole.value,
      dailyGoalMinutes: dailyGoalMinutes.value,
      timeZone: timeZone.value
    })
    toast.success(t('profile.save_success'))
  } catch (err: unknown) {
    toast.error(formatError(err, 'profile.save_failed'))
  } finally {
    isSaving.value = false
  }
}

async function handlePasswordChange() {
  if (newPassword.value.length < 8) {
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

async function handleTogglePush() {
  try {
    if (isSubscribed.value) {
      await unsubscribeUser()
      await profileStore.updateProfile({ isPushEnabled: false })
      toast.success(t('settings.web_push_disabled_desc'))
    } else {
      await subscribeUser()
      await profileStore.updateProfile({ isPushEnabled: true, timeZone: timeZone.value })
      toast.success(t('settings.web_push_enabled_desc'))
    }
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : 'Push notification error'
    if (msg === 'settings.brave_push_service_blocked' || msg.toLowerCase().includes('push service error') || msg.toLowerCase().includes('brave')) {
      showBraveGuide.value = true
      toast.error(t('settings.brave_push_service_blocked'), 8000)
    } else {
      toast.error(msg)
    }
  }
}

async function handleSendTestPush() {
  isSendingTest.value = true
  try {
    const res = await sendTestPush()
    if (res.sent > 0) {
      toast.success(t('settings.web_push_test_success'))
    } else {
      toast.warning(t('settings.web_push_test_zero_sent'))
    }
  } catch (err: unknown) {
    const isExpired =
      (err instanceof ApiError && err.code === 'PUSH_SUBSCRIPTION_EXPIRED') ||
      (err instanceof Error && err.message?.toLowerCase().includes('expired'))

    if (isExpired) {
      isSubscribed.value = false
      toast.error(t('settings.web_push_test_expired'), 8000)
    } else {
      toast.error(formatError(err, 'settings.web_push_test_error'))
    }
  } finally {
    isSendingTest.value = false
  }
}

async function handleTimezoneChange(newTz: string | number) {
  const tzStr = String(newTz)
  timeZone.value = tzStr
  try {
    await profileStore.updateProfile({ timeZone: tzStr })
    toast.success(t('settings.schedule_saved_success'))
  } catch (err: unknown) {
    toast.error(formatError(err, 'settings.schedule_saved_failed'))
  }
}
</script>

<template>
  <div class="py-4 sm:py-6 px-4 sm:px-6 lg:px-8 bg-slate-50 dark:bg-canvas min-h-[calc(100vh-3.5rem)] sm:min-h-[calc(100vh-3.75rem)] transition-colors duration-200">
    <div class="max-w-5xl mx-auto">
      <MasterDetailLayout class="w-full">
        <!-- Header -->
        <template #header>
          <div class="flex items-center gap-2.5">
            <div class="w-7 h-7 rounded-lg bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0">
              <SettingsIcon class="w-4 h-4" :stroke-width="1.5" />
            </div>
            <h1 class="text-sm sm:text-base font-bold text-slate-900 dark:text-white tracking-tight">
              {{ $t('settings.title') }}
            </h1>
          </div>
        </template>

        <!-- Nav Rail: 3 Consolidated Categories matching Image #1 -->
        <template #nav>
          <div class="space-y-1">
            <!-- 1. General (General & Profile Preferences) -->
            <button
              type="button"
              @click="setTab('general')"
              :class="[
                'w-full flex items-center justify-between gap-2.5 px-3 py-2.5 rounded-xl text-xs sm:text-sm font-semibold transition-colors duration-200 text-left border',
                activeTab === 'general'
                  ? 'bg-brand-500/15 text-brand-600 dark:text-brand-400 font-bold border-brand-500/30 shadow-xs'
                  : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-white/[0.04] border-transparent'
              ]"
            >
              <span class="flex items-center gap-2.5">
                <SettingsIcon class="w-4 h-4 shrink-0" :stroke-width="1.5" />
                <span>{{ $t('settings.tab_general') }}</span>
              </span>
            </button>

            <!-- 2. Notifications -->
            <button
              type="button"
              @click="setTab('notifications')"
              :class="[
                'w-full flex items-center justify-between gap-2.5 px-3 py-2.5 rounded-xl text-xs sm:text-sm font-semibold transition-colors duration-200 text-left border',
                activeTab === 'notifications'
                  ? 'bg-brand-500/15 text-brand-600 dark:text-brand-400 font-bold border-brand-500/30 shadow-xs'
                  : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-white/[0.04] border-transparent'
              ]"
            >
              <span class="flex items-center gap-2.5">
                <Bell class="w-4 h-4 shrink-0" :stroke-width="1.5" />
                <span>{{ $t('settings.tab_notifications') }}</span>
              </span>
            </button>

            <!-- 3. Security -->
            <button
              type="button"
              @click="setTab('security')"
              :class="[
                'w-full flex items-center justify-between gap-2.5 px-3 py-2.5 rounded-xl text-xs sm:text-sm font-semibold transition-colors duration-200 text-left border',
                activeTab === 'security'
                  ? 'bg-brand-500/15 text-brand-600 dark:text-brand-400 font-bold border-brand-500/30 shadow-xs'
                  : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-white/[0.04] border-transparent'
              ]"
            >
              <span class="flex items-center gap-2.5">
                <Shield class="w-4 h-4 shrink-0" :stroke-width="1.5" />
                <span>{{ $t('settings.tab_security') }}</span>
              </span>
            </button>
          </div>
        </template>

        <!-- Content Panel: Clean borderless layout matching Image #1 -->
        <template #content>
          <!-- TAB 1: GENERAL (Learning Preferences & Profile) -->
          <div v-if="activeTab === 'general'" class="space-y-6 max-w-3xl">
            <!-- Section Header from Image #1 -->
            <div>
              <h2 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white tracking-tight">
                {{ $t('settings.learning_preferences') }}
              </h2>
              <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 mt-0.5">
                {{ $t('settings.learning_preferences_desc') }}
              </p>
            </div>

            <!-- Compact Inline Profile Bar (No heavy double borders) -->
            <div class="flex items-center justify-between gap-3 p-3.5 rounded-xl bg-slate-100/70 dark:bg-white/[0.03] border border-slate-200/60 dark:border-white/[0.06]">
              <div class="flex items-center gap-3 min-w-0">
                <div class="w-9 h-9 rounded-full bg-brand-500/20 text-brand-600 dark:text-brand-300 border border-brand-500/30 flex items-center justify-center font-bold text-sm shrink-0">
                  {{ (name || authStore.user?.name || 'U').charAt(0).toUpperCase() }}
                </div>
                <div class="min-w-0">
                  <div class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white truncate">
                    {{ name || authStore.user?.name || 'Developer' }}
                  </div>
                  <div class="text-[11px] text-slate-500 dark:text-slate-400 truncate">
                    {{ authStore.user?.email || 'authenticated' }}
                  </div>
                </div>
              </div>

              <div class="flex items-center gap-2 shrink-0">
                <span class="px-2 py-0.5 rounded-md text-[10px] font-bold bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20">
                  {{ targetRole }}
                </span>
              </div>
            </div>

            <form @submit.prevent="handleProfileSave" class="space-y-5">
              <!-- 2-Column Grid: Exactly matching Image #1 -->
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <!-- Row 1: Difficulty track & Daily goal -->
                <div>
                  <label class="block text-xs font-semibold text-slate-500 dark:text-slate-400 mb-1.5">
                    {{ $t('settings.difficulty_track') }}
                  </label>
                  <AppSelect
                    v-model="targetRole"
                    :options="difficultyOptions"
                    :icon="Briefcase"
                    :placeholder="$t('settings.select_track')"
                  />
                </div>

                <div>
                  <label class="block text-xs font-semibold text-slate-500 dark:text-slate-400 mb-1.5">
                    {{ $t('settings.daily_goal_label') }}
                  </label>
                  <AppSelect
                    v-model="dailyGoalMinutes"
                    :options="dailyGoalOptions"
                    :icon="Target"
                    :placeholder="$t('settings.select_goal')"
                  />
                </div>

                <!-- Row 2: Timezone & Theme -->
                <div>
                  <label class="block text-xs font-semibold text-slate-500 dark:text-slate-400 mb-1.5">
                    {{ $t('settings.timezone_label') }}
                  </label>
                  <AppSelect
                    v-model="timeZone"
                    :options="commonTimezones"
                    :icon="Compass"
                    :placeholder="$t('settings.select_timezone')"
                    @update:modelValue="handleTimezoneChange"
                  />
                </div>

                <div>
                  <label class="block text-xs font-semibold text-slate-500 dark:text-slate-400 mb-1.5">
                    {{ $t('settings.color_theme') }}
                  </label>
                  <AppSelect
                    v-model="currentTheme"
                    :options="themeOptions"
                    :icon="Palette"
                    :placeholder="$t('settings.select_theme')"
                  />
                </div>

                <!-- Row 3: Full Name & Interface Language -->
                <div>
                  <label class="block text-xs font-semibold text-slate-500 dark:text-slate-400 mb-1.5">
                    {{ $t('profile.full_name') }}
                  </label>
                  <div class="relative">
                    <User class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
                    <input
                      v-model="name"
                      required
                      type="text"
                      class="h-10 w-full pl-9 pr-3 rounded-xl bg-white dark:bg-canvas-subtle border border-slate-300 dark:border-white/[0.08] text-sm text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:border-brand-500 focus:ring-1 focus:ring-brand-500 shadow-xs transition-colors"
                      :placeholder="$t('settings.placeholder_full_name')"
                    />
                  </div>
                </div>

                <div>
                  <label class="block text-xs font-semibold text-slate-500 dark:text-slate-400 mb-1.5">
                    {{ $t('settings.interface_lang') }}
                  </label>
                  <AppSelect
                    v-model="currentLanguage"
                    :options="languageOptions"
                    :icon="Globe"
                    :placeholder="$t('settings.select_language')"
                  />
                </div>
              </div>

              <!-- Auto-advance cards toggle card (Directly matching Image #1) -->
              <div class="p-4 rounded-xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-50/50 dark:bg-white/[0.02] flex items-center justify-between gap-4">
                <div class="space-y-0.5">
                  <div class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white">
                    {{ $t('settings.auto_advance') }}
                  </div>
                  <div class="text-[11px] sm:text-xs text-slate-500 dark:text-slate-400">
                    {{ $t('settings.auto_advance_desc') }}
                  </div>
                </div>

                <button
                  type="button"
                  @click="autoAdvanceCards = !autoAdvanceCards"
                  class="relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-2"
                  :class="autoAdvanceCards ? 'bg-brand-600' : 'bg-slate-300 dark:bg-white/10'"
                  role="switch"
                  :aria-checked="autoAdvanceCards"
                >
                  <span
                    class="pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow-sm ring-0 transition duration-200 ease-in-out"
                    :class="autoAdvanceCards ? 'translate-x-5' : 'translate-x-0'"
                  />
                </button>
              </div>
              <!-- Form Submit Action -->
              <div class="flex justify-end pt-2">
                <button
                  type="submit"
                  :disabled="isSaving"
                  class="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-md transition-all active:scale-[0.98] disabled:opacity-50 cursor-pointer"
                >
                  <Loader2 v-if="isSaving" class="w-3.5 h-3.5 animate-spin" />
                  <Save v-else class="w-3.5 h-3.5 shrink-0" :stroke-width="1.5" />
                  <span>{{ isSaving ? $t('profile.saving') : $t('settings.save_changes') }}</span>
                </button>
              </div>

            </form>
          </div>

          <!-- TAB 2: NOTIFICATIONS -->
          <div v-else-if="activeTab === 'notifications'" class="space-y-6 max-w-3xl">
            <div>
              <h2 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white tracking-tight">
                {{ $t('settings.web_push_title') }}
              </h2>
              <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 mt-0.5">
                {{ $t('settings.web_push_desc') }}
              </p>
            </div>

            <!-- Brave Alert Callout -->
            <div
              v-if="showBraveGuide"
              class="p-4 rounded-xl bg-amber-500/10 border border-amber-500/20 space-y-1.5 text-xs text-amber-700 dark:text-amber-300"
            >
              <div class="flex items-center gap-2 font-bold">
                <AlertCircle class="w-4 h-4 shrink-0 text-amber-500" />
                <span>Brave Browser Push Service Required</span>
              </div>
              <p class="leading-relaxed">
                {{ $t('settings.brave_push_service_blocked') }}
              </p>
            </div>

            <!-- Push Toggle Card (Borderless style) -->
            <div class="p-4 rounded-xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-50/50 dark:bg-white/[0.02] flex items-center justify-between gap-4">
              <div class="space-y-0.5">
                <div class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white">
                  {{ $t('settings.web_push_toggle') }}
                </div>
                <div class="text-[11px] sm:text-xs text-slate-500 dark:text-slate-400">
                  {{ isSubscribed ? $t('settings.web_push_enabled_desc') : $t('settings.web_push_disabled_desc') }}
                </div>
              </div>

              <div class="flex items-center gap-3 shrink-0">
                <div v-if="!isPushSupported" class="text-xs text-amber-500 font-semibold flex items-center gap-1">
                  <AlertCircle class="w-4 h-4" />
                  <span>{{ $t('settings.web_push_not_supported') }}</span>
                </div>

                <button
                  v-else
                  @click="handleTogglePush"
                  :disabled="isPushLoading"
                  type="button"
                  class="relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-2 disabled:opacity-50"
                  :class="isSubscribed ? 'bg-brand-600' : 'bg-slate-300 dark:bg-white/10'"
                  role="switch"
                  :aria-checked="isSubscribed"
                >
                  <span
                    class="pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow-sm ring-0 transition duration-200 ease-in-out"
                    :class="isSubscribed ? 'translate-x-5' : 'translate-x-0'"
                  />
                </button>
              </div>
            </div>

            <!-- Active Endpoint Banner & Send Test Push -->
            <div
              v-if="isSubscribed"
              class="p-4 rounded-xl border border-emerald-500/20 bg-emerald-500/5 flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs"
            >
              <div class="flex items-center gap-2 text-emerald-600 dark:text-emerald-400 font-semibold">
                <CheckCircle2 class="w-4 h-4" />
                <span>{{ $t('settings.web_push_endpoint_active') }}</span>
              </div>

              <button
                @click="handleSendTestPush"
                :disabled="isSendingTest"
                type="button"
                class="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-brand-600 hover:bg-brand-500 text-white font-bold transition-all disabled:opacity-50 cursor-pointer text-xs shadow-xs"
              >
                <Loader2 v-if="isSendingTest" class="w-3.5 h-3.5 animate-spin" />
                <Send v-else class="w-3.5 h-3.5" :stroke-width="1.5" />
                <span>{{ isSendingTest ? $t('settings.web_push_test_sending') : $t('settings.web_push_test_btn') }}</span>
              </button>
            </div>

            <!-- Notification Schedules -->
            <div class="space-y-3 pt-2">
              <h3 class="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                {{ $t('settings.schedule_title') }}
              </h3>

              <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div class="p-4 rounded-xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-50/50 dark:bg-white/[0.02] space-y-2">
                  <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300">
                    {{ $t('settings.preferred_study_time') }}
                  </label>
                  <AppTimePicker v-model="preferredStudyTime" />
                  <p class="text-[11px] text-slate-500">{{ $t('settings.preferred_study_time_desc') }}</p>
                </div>

                <div class="p-4 rounded-xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-50/50 dark:bg-white/[0.02] space-y-2">
                  <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300">
                    {{ $t('settings.streak_alert_time') }}
                  </label>
                  <AppTimePicker v-model="streakAlertTime" />
                  <p class="text-[11px] text-slate-500">{{ $t('settings.streak_alert_time_desc') }}</p>
                </div>
              </div>
            </div>
            <!-- Save Schedule Action -->
            <div class="flex justify-end pt-2">
              <button
                type="button"
                @click="handleSaveSchedule"
                :disabled="isSavingSchedule"
                class="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-md transition-all active:scale-[0.98] disabled:opacity-50 cursor-pointer"
              >
                <Loader2 v-if="isSavingSchedule" class="w-3.5 h-3.5 animate-spin" />
                <Save v-else class="w-3.5 h-3.5 shrink-0" :stroke-width="1.5" />
                <span>{{ isSavingSchedule ? $t('profile.saving') : $t('settings.btn_save_schedule') }}</span>
              </button>
            </div>
          </div>

          <!-- TAB 3: SECURITY (Account & Password) -->
          <div v-else-if="activeTab === 'security'" class="space-y-6 max-w-2xl">
            <div>
              <h2 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white tracking-tight">
                {{ $t('profile.tab_security') }}
              </h2>
              <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 mt-0.5">
                {{ $t('settings.security_desc') }}
              </p>
            </div>

            <!-- Google OAuth Banner -->
            <div
              v-if="profileStore.profile?.isGoogleLinked && !profileStore.profile?.hasPassword"
              class="p-4 rounded-xl bg-blue-50/70 dark:bg-blue-950/30 border border-blue-200/80 dark:border-blue-500/20 flex items-center gap-2.5 text-xs text-blue-700 dark:text-blue-300"
            >
              <Shield class="w-4 h-4 shrink-0 text-blue-500" />
              <span>{{ $t('profile.google_password_hint') }}</span>
            </div>

            <form @submit.prevent="handlePasswordChange" class="space-y-4">
              <!-- Current Password -->
              <div v-if="profileStore.profile?.hasPassword">
                <label class="block text-xs font-semibold text-slate-500 dark:text-slate-400 mb-1.5">
                  {{ $t('profile.current_password') }}
                </label>
                <div class="relative">
                  <Lock class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
                  <input
                    v-model="currentPassword"
                    required
                    :type="showCurrentPassword ? 'text' : 'password'"
                    placeholder="••••••••"
                    class="h-10 w-full pl-9 pr-10 rounded-xl bg-white dark:bg-canvas-subtle border border-slate-300 dark:border-white/[0.08] text-sm text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:border-brand-500 focus:ring-1 focus:ring-brand-500 shadow-xs transition-colors"
                  />
                  <button
                    type="button"
                    @click="showCurrentPassword = !showCurrentPassword"
                    class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 cursor-pointer"
                  >
                    <EyeOff v-if="showCurrentPassword" class="w-4 h-4" />
                    <Eye v-else class="w-4 h-4" />
                  </button>
                </div>
              </div>

              <!-- New Password -->
              <div>
                <label class="block text-xs font-semibold text-slate-500 dark:text-slate-400 mb-1.5">
                  {{ $t('profile.new_password') }}
                </label>
                <div class="relative">
                  <Lock class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
                  <input
                    v-model="newPassword"
                    required
                    :type="showNewPassword ? 'text' : 'password'"
                    minlength="8"
                    placeholder="••••••••"
                    class="h-10 w-full pl-9 pr-10 rounded-xl bg-white dark:bg-canvas-subtle border border-slate-300 dark:border-white/[0.08] text-sm text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:border-brand-500 focus:ring-1 focus:ring-brand-500 shadow-xs transition-colors"
                  />
                  <button
                    type="button"
                    @click="showNewPassword = !showNewPassword"
                    class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 cursor-pointer"
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
                    />
                  </div>
                  <div class="text-[11px] font-semibold text-right" :class="pwdAnalysis.color.split(' ')[1]">
                    {{ pwdAnalysis.label }}
                  </div>
                </div>
              </div>

              <!-- Confirm Password -->
              <div>
                <label class="block text-xs font-semibold text-slate-500 dark:text-slate-400 mb-1.5">
                  {{ $t('profile.confirm_password') }}
                </label>
                <div class="relative">
                  <Lock class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
                  <input
                    v-model="confirmPassword"
                    required
                    :type="showConfirmPassword ? 'text' : 'password'"
                    minlength="8"
                    placeholder="••••••••"
                    class="h-10 w-full pl-9 pr-10 rounded-xl bg-white dark:bg-canvas-subtle border border-slate-300 dark:border-white/[0.08] text-sm text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:border-brand-500 focus:ring-1 focus:ring-brand-500 shadow-xs transition-colors"
                  />
                  <button
                    type="button"
                    @click="showConfirmPassword = !showConfirmPassword"
                    class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 cursor-pointer"
                  >
                    <EyeOff v-if="showConfirmPassword" class="w-4 h-4" />
                    <Eye v-else class="w-4 h-4" />
                  </button>
                </div>
              </div>

              <!-- Submit Password Button -->
              <div class="flex justify-end pt-2">
                <button
                  type="submit"
                  :disabled="profileStore.isUpdating"
                  class="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-sm transition-all active:scale-[0.98] disabled:opacity-50 cursor-pointer"
                >
                  <span v-if="profileStore.isUpdating" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                  <span>{{ profileStore.profile?.hasPassword ? $t('profile.update_password_btn') : $t('profile.set_password_btn') }}</span>
                </button>
              </div>
            </form>
          </div>
        </template>
      </MasterDetailLayout>
    </div>
  </div>
</template>
