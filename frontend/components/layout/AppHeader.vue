<script setup lang="ts">
import { BookOpen, LogOut, User as UserIcon } from 'lucide-vue-next'
import StreakBadge from '~/components/common/StreakBadge.vue'
import ThemeToggle from '~/components/common/ThemeToggle.vue'
import LocaleSelector from '~/components/common/LocaleSelector.vue'

const authStore = useAuthStore()
const focusStore = useDailyFocusStore()

const currentStreak = computed(() => focusStore.data?.currentStreak ?? 0)
const freezeCredits = computed(() => focusStore.data?.freezeCreditsRemaining ?? 2)
</script>

<template>
  <header class="h-14 border-b border-slate-800/80 bg-slate-950/80 backdrop-blur sticky top-0 z-40 px-4 md:px-6 flex items-center justify-between">
    <!-- Brand & Day order -->
    <div class="flex items-center gap-4">
      <NuxtLink to="/today" class="flex items-center gap-2.5 font-bold tracking-tight text-white hover:opacity-90 transition-opacity">
        <div class="w-8 h-8 rounded-lg bg-gradient-to-tr from-brand-600 to-emerald-400 flex items-center justify-center shadow-lg shadow-brand-500/20">
          <BookOpen class="w-4 h-4 text-slate-950 font-bold" />
        </div>
        <span class="text-base tracking-tight font-black bg-gradient-to-r from-white via-slate-100 to-slate-400 bg-clip-text text-transparent">TechDaily</span>
      </NuxtLink>

      <span v-if="focusStore.data?.topic" class="hidden sm:inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-medium bg-brand-950/60 border border-brand-800/60 text-brand-300">
        <span class="w-1.5 h-1.5 rounded-full bg-brand-400 animate-pulse"></span>
        Day {{ focusStore.data.topic.dayOrder }} / 30
      </span>
    </div>

    <!-- Header Actions -->
    <div class="flex items-center gap-3">
      <StreakBadge :streak="currentStreak" :freeze-credits="freezeCredits" />
      <LocaleSelector />
      <ThemeToggle />

      <!-- User Profile / Auth Status -->
      <div v-if="authStore.isLoggedIn && authStore.user" class="flex items-center gap-2 pl-2 border-l border-slate-800">
        <div class="w-7 h-7 rounded-full bg-slate-800 border border-slate-700 flex items-center justify-center text-xs font-semibold text-slate-300">
          {{ authStore.user.name.charAt(0).toUpperCase() }}
        </div>
        <button
          @click="authStore.logout()"
          class="p-1.5 rounded-lg text-slate-400 hover:text-red-400 transition-colors"
          title="Sign Out"
        >
          <LogOut class="w-4 h-4" />
        </button>
      </div>

      <NuxtLink
        v-else
        to="/login"
        class="text-xs font-medium px-3 py-1.5 rounded-lg bg-brand-600 hover:bg-brand-500 text-slate-950 font-semibold transition-colors shadow-sm"
      >
        {{ $t('nav.login') }}
      </NuxtLink>
    </div>
  </header>
</template>
