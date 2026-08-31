<script setup lang="ts">
import { computed } from 'vue'
import { BookOpen, LogOut, User } from 'lucide-vue-next'
import StreakBadge from '~/components/common/StreakBadge.vue'
import ThemeToggle from '~/components/common/ThemeToggle.vue'
import LocaleSelector from '~/components/common/LocaleSelector.vue'

const authStore = useAuthStore()
const focusStore = useDailyFocusStore()

const currentStreak = computed(() => focusStore.data?.currentStreak ?? 0)
const freezeCredits = computed(() => focusStore.data?.freezeCreditsRemaining ?? 2)
</script>

<template>
  <header class="h-15 border-b border-slate-200 dark:border-slate-800/80 bg-white/95 dark:bg-slate-950/80 backdrop-blur sticky top-0 z-40 px-4 md:px-6 flex items-center justify-between transition-colors duration-200">
    <!-- Brand & Day order -->
    <div class="flex items-center gap-4">
      <NuxtLink to="/today" class="flex items-center gap-2.5 font-bold tracking-tight hover:opacity-90 transition-opacity">
        <div class="w-8 h-8 rounded-lg bg-gradient-to-tr from-brand-600 to-emerald-400 flex items-center justify-center shadow-md shadow-brand-500/20">
          <BookOpen class="w-4 h-4 text-slate-950 font-bold" />
        </div>
        <span class="text-lg font-black tracking-tight bg-gradient-to-r from-slate-900 via-slate-800 to-brand-600 dark:from-white dark:via-slate-100 dark:to-slate-400 bg-clip-text text-transparent">TechDaily</span>
      </NuxtLink>

      <span v-if="focusStore.data?.topic" class="hidden sm:inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-brand-100 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800/60 text-brand-800 dark:text-brand-300">
        <span class="w-2 h-2 rounded-full bg-brand-500 dark:bg-brand-400 animate-pulse"></span>
        Day {{ focusStore.data.topic.dayOrder }} / 30
      </span>
    </div>

    <!-- Header Actions -->
    <div class="flex items-center gap-3">
      <StreakBadge :streak="currentStreak" :freeze-credits="freezeCredits" />
      <LocaleSelector />
      <ThemeToggle />

      <!-- User Profile / Auth Status -->
      <div v-if="authStore.isLoggedIn && authStore.user" class="flex items-center gap-2.5 pl-3 border-l border-slate-200 dark:border-slate-800">
        <NuxtLink
          to="/profile"
          class="flex items-center gap-2 p-1.5 rounded-xl hover:bg-slate-100 dark:hover:bg-slate-800/80 transition-colors group"
          title="View Profile"
        >
          <div class="w-8 h-8 rounded-full bg-brand-100 dark:bg-brand-950 border border-brand-200 dark:border-brand-800 flex items-center justify-center text-xs font-bold text-brand-800 dark:text-brand-300 shadow-sm group-hover:scale-105 transition-transform">
            {{ authStore.user.name.charAt(0).toUpperCase() }}
          </div>
          <span class="text-xs font-bold text-slate-800 dark:text-slate-200 hidden sm:inline group-hover:text-brand-600 dark:group-hover:text-brand-400 transition-colors">
            {{ authStore.user.name }}
          </span>
        </NuxtLink>

        <button
          @click="authStore.logout()"
          class="p-2 rounded-xl text-slate-500 dark:text-slate-400 hover:text-rose-600 dark:hover:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-950/30 transition-colors"
          title="Sign Out"
        >
          <LogOut class="w-4 h-4" />
        </button>
      </div>

      <NuxtLink
        v-else
        to="/login"
        class="text-xs font-bold px-4 py-2 rounded-xl bg-brand-600 hover:bg-brand-500 text-slate-950 transition-all shadow-md shadow-brand-500/20 active:scale-95"
      >
        {{ $t('nav.login') }}
      </NuxtLink>
    </div>
  </header>
</template>
