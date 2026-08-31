<script setup lang="ts">
import { Settings as SettingsIcon, Globe, Bell, User } from 'lucide-vue-next'
import ThemeToggle from '~/components/common/ThemeToggle.vue'
import LocaleSelector from '~/components/common/LocaleSelector.vue'

const authStore = useAuthStore()
</script>

<template>
  <div class="max-w-3xl mx-auto p-6 md:p-10 space-y-8 bg-slate-50 dark:bg-slate-950 transition-colors duration-200">
    <div>
      <h1 class="text-2xl md:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight flex items-center gap-3">
        <SettingsIcon class="w-7 h-7 text-brand-600 dark:text-brand-400" />
        <span>{{ $t('nav.settings') }}</span>
      </h1>
      <p class="text-sm text-slate-500 dark:text-slate-400 mt-1.5 font-medium">Manage your preferences, language, and notifications</p>
    </div>

    <!-- Account Details -->
    <div class="p-6 sm:p-8 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 space-y-4 shadow-sm">
      <h2 class="text-sm font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300 flex items-center gap-2">
        <User class="w-4 h-4 text-brand-600 dark:text-brand-400" />
        <span>Profile & Account</span>
      </h2>
      <div v-if="authStore.user" class="space-y-2 text-sm sm:text-base">
        <div class="flex justify-between py-2.5 border-b border-slate-100 dark:border-slate-800 text-slate-700 dark:text-slate-300">
          <span class="text-slate-500">Name</span>
          <span class="font-bold text-slate-900 dark:text-white">{{ authStore.user.name }}</span>
        </div>
        <div class="flex justify-between py-2.5 border-b border-slate-100 dark:border-slate-800 text-slate-700 dark:text-slate-300">
          <span class="text-slate-500">Email</span>
          <span class="font-mono text-slate-900 dark:text-white">{{ authStore.user.email }}</span>
        </div>
      </div>
      <div v-else class="text-sm text-slate-600 dark:text-slate-400">
        You are currently using guest mode. <NuxtLink to="/login" class="text-brand-600 dark:text-brand-400 font-bold underline">Sign In</NuxtLink> to sync your streak across devices.
      </div>
    </div>

    <!-- Appearance & Language -->
    <div class="p-6 sm:p-8 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 space-y-4 shadow-sm">
      <h2 class="text-sm font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300 flex items-center gap-2">
        <Globe class="w-4 h-4 text-brand-600 dark:text-brand-400" />
        <span>Language & Theme</span>
      </h2>
      <div class="flex items-center justify-between py-3 border-b border-slate-100 dark:border-slate-800">
        <div>
          <div class="text-sm sm:text-base font-bold text-slate-900 dark:text-white">Interface Language</div>
          <div class="text-xs sm:text-sm text-slate-500">Select your preferred app UI language</div>
        </div>
        <LocaleSelector />
      </div>
      <div class="flex items-center justify-between py-3">
        <div>
          <div class="text-sm sm:text-base font-bold text-slate-900 dark:text-white">Color Theme</div>
          <div class="text-xs sm:text-sm text-slate-500">Switch between Dark Mode and Light Mode</div>
        </div>
        <ThemeToggle />
      </div>
    </div>

    <!-- Telegram Notifications -->
    <div class="p-6 sm:p-8 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 space-y-3 shadow-sm">
      <h2 class="text-sm font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300 flex items-center gap-2">
        <Bell class="w-4 h-4 text-brand-600 dark:text-brand-400" />
        <span>Telegram Daily Dispatch & Streak Warning</span>
      </h2>
      <p class="text-sm text-slate-600 dark:text-slate-400 leading-relaxed">
        Connect your Telegram bot to receive daily 08:00 AM curriculum links and 20:00 PM streak preservation reminders.
      </p>
      <div class="p-3.5 rounded-2xl bg-slate-50 dark:bg-slate-950/60 border border-slate-200 dark:border-slate-800/80 text-xs sm:text-sm font-mono text-brand-700 dark:text-brand-300">
        Telegram Bot: @TechDailyBot (configured via backend settings)
      </div>
    </div>
  </div>
</template>
