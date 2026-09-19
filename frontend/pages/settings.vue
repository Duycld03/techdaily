<script setup lang="ts">
import { ref, onMounted } from 'vue'
import {
  Settings as SettingsIcon,
  Globe,
  Bell,
  Loader2,
  Clock,
  Compass,
  AlertCircle,
  CheckCircle2,
  Send
} from 'lucide-vue-next'
import ThemeToggle from '~/components/common/ThemeToggle.vue'
import LocaleSelector from '~/components/common/LocaleSelector.vue'
import AppSelect from '~/components/common/AppSelect.vue'
import { useProfileStore } from '~/stores/useProfileStore'
import { useWebPush } from '~/composables/useWebPush'
import { ApiError } from '~/composables/useApiClient'
import { useApiError } from '~/composables/useApiError'

const { t } = useI18n()
const toast = useToast()
const { formatError } = useApiError()
const profileStore = useProfileStore()
const {
  isPushSupported,
  isSubscribed,
  isLoading: isPushLoading,
  checkSubscriptionStatus,
  subscribeUser,
  unsubscribeUser,
  sendTestPush
} = useWebPush()

const preferredStudyTime = ref('08:00')
const streakAlertTime = ref('20:00')
const timeZone = ref('UTC')
const isSavingSchedule = ref(false)
const isSendingTest = ref(false)

const defaultTimezones = [
  { value: 'UTC', label: 'UTC (Coordinated Universal Time)' },
  { value: 'Asia/Ho_Chi_Minh', label: 'Asia/Ho_Chi_Minh (UTC+7)' },
  { value: 'Asia/Tokyo', label: 'Asia/Tokyo (UTC+9)' },
  { value: 'Asia/Singapore', label: 'Asia/Singapore (UTC+8)' },
  { value: 'Europe/London', label: 'Europe/London (UTC+0 / BST)' },
  { value: 'Europe/Paris', label: 'Europe/Paris (UTC+1 / CEST)' },
  { value: 'America/New_York', label: 'America/New_York (UTC-5 / EDT)' },
  { value: 'America/Chicago', label: 'America/Chicago (UTC-6 / CDT)' },
  { value: 'America/Denver', label: 'America/Denver (UTC-7 / MDT)' },
  { value: 'America/Los_Angeles', label: 'America/Los_Angeles (UTC-8 / PDT)' }
]

const commonTimezones = ref([...defaultTimezones])

function registerTimezone(tz: string, labelSuffix: string) {
  if (!tz) return
  if (!commonTimezones.value.some(item => item.value === tz)) {
    commonTimezones.value.unshift({ value: tz, label: `${tz} (${labelSuffix})` })
  }
}
onMounted(async () => {
  // Auto-detect browser timezone
  try {
    const detectedTz = Intl.DateTimeFormat().resolvedOptions().timeZone
    if (detectedTz) {
      timeZone.value = detectedTz
      registerTimezone(detectedTz, 'Local')
    }
  } catch {
    // fallback to UTC
  }

  // Fetch profile to populate existing user preferences
  await profileStore.fetchProfile()
  if (profileStore.profile) {
    if (profileStore.profile.preferredStudyTime) {
      preferredStudyTime.value = profileStore.profile.preferredStudyTime
    }
    if (profileStore.profile.streakAlertTime) {
      streakAlertTime.value = profileStore.profile.streakAlertTime
    }
    if (profileStore.profile.timeZone) {
      timeZone.value = profileStore.profile.timeZone
      registerTimezone(profileStore.profile.timeZone, 'Saved')
    }
  }

  // Check browser service worker subscription status
  await checkSubscriptionStatus()
})

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
    if (msg === 'settings.brave_push_service_blocked' || msg.toLowerCase().includes('push service error')) {
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
    const msg = err instanceof Error ? err.message : 'Failed to save schedule.'
    toast.error(msg)
  } finally {
    isSavingSchedule.value = false
  }
}
</script>

<template>
  <div class="max-w-3xl mx-auto p-4 sm:p-6 md:p-10 space-y-6 sm:space-y-8 bg-slate-50 dark:bg-canvas min-h-[calc(100vh-3.5rem)] transition-colors duration-200">
    <div>
      <h1 class="text-xl sm:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight flex items-center gap-2.5 sm:gap-3">
        <SettingsIcon class="w-6 h-6 sm:w-7 sm:h-7 text-brand-600 dark:text-brand-400" />
        <span>{{ $t('settings.title') }}</span>
      </h1>
      <p class="text-sm md:text-lg text-slate-500 dark:text-slate-400 mt-1 font-medium">{{ $t('settings.subtitle') }}</p>
    </div>

    <!-- Appearance & Language -->
    <div class="glass-card p-5 sm:p-8 rounded-2xl sm:rounded-3xl border border-slate-200/80 dark:border-white/[0.08] space-y-4 shadow-sm">
      <h2 class="text-sm sm:text-base font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300 flex items-center gap-2">
        <Globe class="w-4 h-4 text-brand-600 dark:text-brand-400" />
        <span>{{ $t('settings.lang_theme_title') }}</span>
      </h2>
      <div class="flex items-center justify-between py-3.5 border-b border-slate-100 dark:border-white/[0.06]">
        <div>
          <div class="text-sm sm:text-base md:text-lg font-bold text-slate-900 dark:text-white">{{ $t('settings.interface_lang') }}</div>
          <div class="text-xs sm:text-sm md:text-base text-slate-500">{{ $t('settings.interface_lang_desc') }}</div>
        </div>
        <LocaleSelector />
      </div>
      <div class="flex items-center justify-between py-3.5">
        <div>
          <div class="text-sm sm:text-base md:text-lg font-bold text-slate-900 dark:text-white">{{ $t('settings.color_theme') }}</div>
          <div class="text-xs sm:text-sm md:text-base text-slate-500">{{ $t('settings.color_theme_desc') }}</div>
        </div>
        <ThemeToggle />
      </div>
    </div>

    <!-- Browser Web Push Notifications -->
    <div class="glass-card p-5 sm:p-8 rounded-2xl sm:rounded-3xl border border-slate-200/80 dark:border-white/[0.08] space-y-5 shadow-sm">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div class="space-y-1">
          <h2 class="text-sm sm:text-base font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300 flex items-center gap-2">
            <Bell class="w-4 h-4 text-brand-600 dark:text-brand-400" />
            <span>{{ $t('settings.web_push_title') }}</span>
          </h2>
          <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 leading-relaxed max-w-xl">
            {{ $t('settings.web_push_desc') }}
          </p>
        </div>

        <!-- Push Toggle Switch -->
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
            :class="isSubscribed ? 'bg-brand-600 dark:bg-brand-500' : 'bg-slate-300 dark:bg-white/[0.12]'"
            role="switch"
            :aria-checked="isSubscribed"
          >
            <span
              class="pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out"
              :class="isSubscribed ? 'translate-x-5' : 'translate-x-0'"
            />
          </button>
        </div>
      </div>

      <!-- Push Active Status / Action Banner -->
      <div
        v-if="isSubscribed"
        class="glass-panel p-4 rounded-2xl border border-brand-200/80 dark:border-brand-500/20 bg-brand-50/50 dark:bg-brand-950/30 flex flex-wrap items-center justify-between gap-3 text-xs sm:text-sm"
      >
        <div class="flex items-center gap-2 text-brand-800 dark:text-brand-300 font-semibold">
          <CheckCircle2 class="w-4 h-4 text-brand-600 dark:text-brand-400 shrink-0" />
          <span>{{ $t('settings.web_push_enabled_desc') }}</span>
        </div>

        <button
          @click="handleSendTestPush"
          :disabled="isSendingTest"
          class="inline-flex items-center justify-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-white dark:bg-canvas-elevated border border-brand-300 dark:border-brand-500/30 text-brand-700 dark:text-brand-300 hover:bg-brand-50 dark:hover:bg-brand-500/10 font-bold transition-all disabled:opacity-50 shadow-sm whitespace-nowrap shrink-0"
        >
          <Loader2 v-if="isSendingTest" class="w-3.5 h-3.5 animate-spin" />
          <Send v-else class="w-3.5 h-3.5" />
          <span>{{ isSendingTest ? $t('settings.web_push_test_sending') : $t('settings.web_push_test_btn') }}</span>
        </button>
      </div>

      <!-- Notification Schedule & Timezone Settings -->
      <div class="pt-4 border-t border-slate-100 dark:border-white/[0.06] space-y-4">
        <h3 class="text-xs sm:text-sm font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300 flex items-center gap-2">
          <Clock class="w-4 h-4 text-slate-500" />
          <span>{{ $t('settings.schedule_title') }}</span>
        </h3>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <!-- Preferred Study Time -->
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-700 dark:text-slate-300">
              {{ $t('settings.preferred_study_time') }}
            </label>
            <input
              type="time"
              v-model="preferredStudyTime"
              class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle text-slate-900 dark:text-white text-xs sm:text-sm font-semibold focus:outline-none focus:border-brand-500 focus:ring-1 focus:ring-brand-500 shadow-sm transition-colors"
            />
            <p class="text-[11px] text-slate-500">{{ $t('settings.preferred_study_time_desc') }}</p>
          </div>

          <!-- Streak Warning Time -->
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-700 dark:text-slate-300">
              {{ $t('settings.streak_alert_time') }}
            </label>
            <input
              type="time"
              v-model="streakAlertTime"
              class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle text-slate-900 dark:text-white text-xs sm:text-sm font-semibold focus:outline-none focus:border-brand-500 focus:ring-1 focus:ring-brand-500 shadow-sm transition-colors"
            />
            <p class="text-[11px] text-slate-500">{{ $t('settings.streak_alert_time_desc') }}</p>
          </div>
        </div>

        <!-- Timezone Selector -->
        <div class="space-y-1.5 pt-1">
          <label class="block text-xs font-bold text-slate-700 dark:text-slate-300 flex items-center gap-1.5">
            <Compass class="w-3.5 h-3.5 text-slate-500" />
            <span>{{ $t('settings.timezone_label') }}</span>
          </label>
          <AppSelect
            :model-value="timeZone"
            :options="commonTimezones"
            :icon="Globe"
            :aria-label="$t('settings.timezone_label')"
            @update:model-value="handleTimezoneChange"
          />
          <p class="text-[11px] text-slate-500">{{ $t('settings.timezone_desc') }}</p>
        </div>

        <!-- Save Schedule Button -->
        <div class="pt-2 flex justify-end">
          <button
            @click="handleSaveSchedule"
            :disabled="isSavingSchedule"
            class="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-sm transition-all disabled:opacity-50 active:scale-[0.98]"
          >
            <Loader2 v-if="isSavingSchedule" class="w-4 h-4 animate-spin" />
            <span>{{ $t('settings.btn_save_schedule') }}</span>
          </button>
        </div>
      </div>
    </div>

  </div>
</template>
