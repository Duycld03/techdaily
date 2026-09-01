<script setup lang="ts">
import { ref, computed } from 'vue'
import {
  BookOpen,
  LogOut,
  User,
  Menu,
  X,
  Target,
  Map,
  Layers,
  Highlighter,
  Settings
} from 'lucide-vue-next'
import StreakBadge from '~/components/common/StreakBadge.vue'
import ThemeToggle from '~/components/common/ThemeToggle.vue'
import LocaleSelector from '~/components/common/LocaleSelector.vue'

const route = useRoute()
const authStore = useAuthStore()
const focusStore = useDailyFocusStore()
const isMobileNavOpen = ref(false)

const currentStreak = computed(() => focusStore.data?.currentStreak ?? 0)
const freezeCredits = computed(() => focusStore.data?.freezeCreditsRemaining ?? 2)

const links = [
  { name: 'nav.today', path: '/today', icon: Target },
  { name: 'nav.roadmap', path: '/roadmap', icon: Map },
  { name: 'nav.review', path: '/review', icon: Layers },
  { name: 'nav.library', path: '/library', icon: BookOpen },
  { name: 'nav.notes', path: '/notes', icon: Highlighter },
  { name: 'nav.profile', path: '/profile', icon: User },
  { name: 'nav.settings', path: '/settings', icon: Settings }
]

function isLinkActive(linkPath: string): boolean {
  const currentPath = route.path
  if (linkPath === '/today') {
    return currentPath === '/today' || currentPath === '/'
  }
  if (linkPath === '/library') {
    return currentPath === '/library' || currentPath.startsWith('/read')
  }
  return currentPath === linkPath
}

function handleMobileLinkClick(event: MouseEvent, navigate: (e?: MouseEvent) => Promise<unknown>) {
  navigate(event)
  isMobileNavOpen.value = false
  if (event.currentTarget && typeof (event.currentTarget as HTMLElement).blur === 'function') {
    (event.currentTarget as HTMLElement).blur()
  }
}
</script>

<template>
  <header class="h-15 border-b border-slate-200 dark:border-slate-800/80 bg-white/95 dark:bg-slate-950/80 backdrop-blur sticky top-0 z-40 px-3 sm:px-6 flex items-center justify-between transition-colors duration-200">
    <!-- Brand & Mobile Hamburger -->
    <div class="flex items-center gap-2 sm:gap-4">
      <!-- Mobile Menu Button -->
      <button
        @click="isMobileNavOpen = true"
        class="md:hidden p-2 rounded-xl text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
        title="Open Navigation Menu"
      >
        <Menu class="w-5 h-5" />
      </button>

      <NuxtLink to="/today" class="flex items-center gap-2.5 font-bold tracking-tight hover:opacity-90 transition-opacity">
        <div class="w-8 h-8 rounded-lg bg-gradient-to-tr from-brand-600 to-emerald-400 flex items-center justify-center shadow-md shadow-brand-500/20">
          <BookOpen class="w-4 h-4 text-slate-950 font-bold" />
        </div>
        <span class="text-lg font-black tracking-tight bg-gradient-to-r from-slate-900 via-slate-800 to-brand-600 dark:from-white dark:via-slate-100 dark:to-slate-400 bg-clip-text text-transparent">TechDaily</span>
      </NuxtLink>

      <span v-if="focusStore.data?.topic" class="hidden sm:inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-slate-100 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-800 dark:text-slate-200">
        <span class="w-2 h-2 rounded-full bg-brand-500 animate-pulse"></span>
        Day {{ focusStore.data.topic.dayOrder }} / 30
      </span>
    </div>

    <!-- Header Actions -->
    <div class="flex items-center gap-2 sm:gap-3">
      <StreakBadge :streak="currentStreak" :freeze-credits="freezeCredits" />
      <LocaleSelector />
      <ThemeToggle />

      <!-- User Profile / Auth Status -->
      <div v-if="authStore.isLoggedIn && authStore.user" class="flex items-center gap-2 pl-2 sm:pl-3 border-l border-slate-200 dark:border-slate-800">
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
        class="text-xs font-semibold px-3 sm:px-4 py-2 rounded-xl bg-brand-600 hover:bg-brand-500 text-white transition-all shadow-md shadow-brand-500/20 active:scale-95"
      >
        {{ $t('nav.login') }}
      </NuxtLink>
    </div>

    <!-- Mobile Slide-Out Navigation Drawer -->
    <div
      v-if="isMobileNavOpen"
      class="md:hidden fixed inset-0 z-50 bg-slate-950/70 backdrop-blur-sm flex justify-start animate-in fade-in"
    >
      <div class="w-4/5 max-w-xs bg-white dark:bg-slate-900 h-full flex flex-col justify-between p-6 shadow-2xl animate-in slide-in-from-left">
        <div>
          <!-- Drawer Header -->
          <div class="flex items-center justify-between pb-4 border-b border-slate-200 dark:border-slate-800">
            <div class="flex items-center gap-2.5 font-bold tracking-tight">
              <div class="w-8 h-8 rounded-lg bg-gradient-to-tr from-brand-600 to-emerald-400 flex items-center justify-center shadow-md">
                <BookOpen class="w-4 h-4 text-slate-950 font-bold" />
              </div>
              <span class="text-base font-black text-slate-900 dark:text-white">TechDaily Menu</span>
            </div>
            <button
              @click="isMobileNavOpen = false"
              class="p-2 rounded-xl text-slate-400 hover:text-slate-900 dark:hover:text-white"
            >
              <X class="w-5 h-5" />
            </button>
          </div>

          <!-- Navigation Links -->
          <nav class="space-y-1 mt-6">
            <NuxtLink
              v-for="link in links"
              :key="link.path"
              :to="link.path"
              custom
              v-slot="{ navigate, href }"
            >
              <a
                :href="href"
                @click="navigate(); isMobileNavOpen = false"
                :class="[
                  'flex items-center gap-3.5 px-4 py-2.5 rounded-xl text-sm transition-colors',
                  isLinkActive(link.path)
                    ? 'bg-slate-100 dark:bg-slate-800/80 text-brand-600 dark:text-brand-400 font-bold'
                    : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100/70 dark:hover:bg-slate-800/50 font-medium'
                ]"
              >
                <component
                  :is="link.icon"
                  :class="[
                    'w-5 h-5 transition-colors',
                    isLinkActive(link.path) ? 'text-brand-600 dark:text-brand-400' : 'text-slate-400 dark:text-slate-500'
                  ]"
                />
                <span>{{ $t(link.name) }}</span>
              </a>
            </NuxtLink>
          </nav>
        </div>

        <!-- Drawer Footer -->
        <div class="pt-4 border-t border-slate-200 dark:border-slate-800 space-y-3">
          <div v-if="authStore.isLoggedIn && authStore.user" class="flex items-center justify-between p-3 rounded-2xl bg-slate-100 dark:bg-slate-800/60">
            <div class="flex items-center gap-2.5 truncate">
              <div class="w-8 h-8 rounded-full bg-brand-600 text-white flex items-center justify-center font-bold text-xs shadow-sm">
                {{ authStore.user.name.charAt(0).toUpperCase() }}
              </div>
              <span class="text-xs font-bold text-slate-900 dark:text-white truncate">
                {{ authStore.user.name }}
              </span>
            </div>
            <button
              @click="authStore.logout(); isMobileNavOpen = false"
              class="text-xs font-semibold text-rose-600 dark:text-rose-400"
            >
              Log Out
            </button>
          </div>

          <div class="text-[11px] text-slate-400 text-center">
            Senior Engineering Daily Platform
          </div>
        </div>
      </div>
    </div>
  </header>
</template>
