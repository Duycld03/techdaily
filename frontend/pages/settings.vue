<script setup lang="ts">
import { Settings as SettingsIcon, Globe, Bell } from 'lucide-vue-next'
import ThemeToggle from '~/components/common/ThemeToggle.vue'
import LocaleSelector from '~/components/common/LocaleSelector.vue'
</script>

<template>
  <div class="max-w-3xl mx-auto p-4 sm:p-6 md:p-10 space-y-6 sm:space-y-8 bg-slate-50 dark:bg-slate-950 transition-colors duration-200">
    <div>
      <h1 class="text-xl sm:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight flex items-center gap-2.5 sm:gap-3">
        <SettingsIcon class="w-6 h-6 sm:w-7 sm:h-7 text-brand-600 dark:text-brand-400" />
        <span>{{ $t('settings.title') }}</span>
      </h1>
      <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 mt-1 font-medium">{{ $t('settings.subtitle') }}</p>
    </div>

    <!-- Appearance & Language -->
    <div class="p-5 sm:p-8 rounded-2xl sm:rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 space-y-4 shadow-sm">
      <h2 class="text-sm font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300 flex items-center gap-2">
        <Globe class="w-4 h-4 text-brand-600 dark:text-brand-400" />
        <span>{{ $t('settings.lang_theme_title') }}</span>
      </h2>
      <div class="flex items-center justify-between py-3 border-b border-slate-100 dark:border-slate-800">
        <div>
          <div class="text-sm sm:text-base font-bold text-slate-900 dark:text-white">{{ $t('settings.interface_lang') }}</div>
          <div class="text-xs sm:text-sm text-slate-500">{{ $t('settings.interface_lang_desc') }}</div>
        </div>
        <LocaleSelector />
      </div>
      <div class="flex items-center justify-between py-3">
        <div>
          <div class="text-sm sm:text-base font-bold text-slate-900 dark:text-white">{{ $t('settings.color_theme') }}</div>
          <div class="text-xs sm:text-sm text-slate-500">{{ $t('settings.color_theme_desc') }}</div>
        </div>
        <ThemeToggle />
      </div>
    </div>

    <!-- Telegram Notifications -->
    <div class="p-6 sm:p-8 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 space-y-3 shadow-sm">
      <h2 class="text-sm font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300 flex items-center gap-2">
        <Bell class="w-4 h-4 text-brand-600 dark:text-brand-400" />
        <span>{{ $t('settings.telegram_title') }}</span>
      </h2>
      <p class="text-sm text-slate-600 dark:text-slate-400 leading-relaxed">
        {{ $t('settings.telegram_desc') }}
      </p>
      <div class="p-3.5 rounded-2xl bg-slate-50 dark:bg-slate-950/60 border border-slate-200 dark:border-slate-800/80 text-xs sm:text-sm font-mono text-brand-700 dark:text-brand-300">
        {{ $t('settings.telegram_bot_info') }}
      </div>
    </div>
  </div>
</template>
