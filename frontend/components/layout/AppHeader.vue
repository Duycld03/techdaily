<script setup lang="ts">
import { ref, computed, watch, onUnmounted } from 'vue'
import {
  BookOpen,
  LogOut,
  User,
  Menu,
  X,
  Target,
  Map,
  Sparkles,
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
  { name: 'nav.insights', path: '/insights', icon: Sparkles },
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

watch(isMobileNavOpen, (open) => {
  if (typeof document !== 'undefined') {
    if (open) {
      document.body.style.overflow = 'hidden'
      document.body.style.touchAction = 'none'
    } else {
      document.body.style.overflow = ''
      document.body.style.touchAction = ''
    }
  }
})

onUnmounted(() => {
  if (typeof document !== 'undefined') {
    document.body.style.overflow = ''
    document.body.style.touchAction = ''
  }
})
</script>

<template>
  <header class="h-14 sm:h-15 border-b border-slate-200 dark:border-slate-800/80 bg-white/95 dark:bg-slate-950/80 backdrop-blur sticky top-0 z-40 px-3 sm:px-6 flex items-center justify-between transition-colors duration-200">
    <!-- Brand & Mobile Hamburger -->
    <div class="flex items-center gap-2 sm:gap-4 shrink-0">
      <!-- Mobile Menu Button -->
      <button
        @click="isMobileNavOpen = true"
        class="md:hidden p-1.5 sm:p-2 rounded-xl text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
        title="Open Navigation Menu"
        aria-label="Open Navigation Menu"
      >
        <Menu class="w-5 h-5" />
      </button>

      <NuxtLink to="/today" class="flex items-center gap-2 font-bold tracking-tight hover:opacity-90 transition-opacity">
        <div class="w-7 h-7 sm:w-8 sm:h-8 rounded-lg bg-gradient-to-tr from-brand-600 to-emerald-400 flex items-center justify-center shadow-md shadow-brand-500/20 shrink-0">
          <BookOpen class="w-3.5 h-3.5 sm:w-4 sm:h-4 text-slate-950 font-bold" />
        </div>
        <span class="hidden sm:inline text-base sm:text-lg font-black tracking-tight bg-gradient-to-r from-slate-900 via-slate-800 to-brand-600 dark:from-white dark:via-slate-100 dark:to-slate-400 bg-clip-text text-transparent">TechDaily</span>
      </NuxtLink>

      <span v-if="focusStore.data?.topic" class="hidden lg:inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-slate-100 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-800 dark:text-slate-200">
        <span class="w-2 h-2 rounded-full bg-brand-500 animate-pulse"></span>
        Day {{ focusStore.data.topic.dayOrder }} / 30
      </span>
    </div>

    <!-- Header Actions -->
    <div class="flex items-center gap-1.5 sm:gap-3 shrink-0">
      <StreakBadge :streak="currentStreak" :freeze-credits="freezeCredits" />
      <LocaleSelector />
      <ThemeToggle />

      <!-- User Profile / Auth Status -->
      <div v-if="authStore.isLoggedIn && authStore.user" class="flex items-center gap-1 sm:gap-2 pl-1.5 sm:pl-3 border-l border-slate-200 dark:border-slate-800">
        <NuxtLink
          to="/profile"
          class="flex items-center gap-2 p-1 rounded-xl hover:bg-slate-100 dark:hover:bg-slate-800/80 transition-colors group"
          title="View Profile"
        >
          <div class="w-7 h-7 sm:w-8 sm:h-8 rounded-full bg-brand-100 dark:bg-brand-950 border border-brand-200 dark:border-brand-800 flex items-center justify-center text-xs font-bold text-brand-800 dark:text-brand-300 shadow-sm group-hover:scale-105 transition-transform">
            {{ authStore.user.name.charAt(0).toUpperCase() }}
          </div>
          <span class="text-xs font-bold text-slate-800 dark:text-slate-200 hidden md:inline group-hover:text-brand-600 dark:group-hover:text-brand-400 transition-colors max-w-[120px] truncate">
            {{ authStore.user.name }}
          </span>
        </NuxtLink>

        <button
          @click="authStore.logout()"
          class="hidden sm:inline-flex p-2 rounded-xl text-slate-500 dark:text-slate-400 hover:text-rose-600 dark:hover:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-950/30 transition-colors"
          title="Sign Out"
        >
          <LogOut class="w-4 h-4" />
        </button>
      </div>

      <NuxtLink
        v-else
        to="/login"
        class="text-xs font-semibold px-2.5 sm:px-4 py-1.5 sm:py-2 rounded-xl bg-brand-600 hover:bg-brand-500 text-white transition-all shadow-md shadow-brand-500/20 active:scale-95 shrink-0"
      >
        {{ $t('nav.login') }}
      </NuxtLink>
    </div>

    <!-- Mobile Slide-Out Navigation Drawer (Teleported to Body) -->
    <Teleport to="body">
      <div
        v-if="isMobileNavOpen"
        class="md:hidden fixed inset-0 z-50 bg-slate-950/75 backdrop-blur-sm touch-none animate-in fade-in"
        @click.self="isMobileNavOpen = false"
      >
        <div class="fixed inset-y-0 left-0 z-50 w-[85%] max-w-xs bg-white dark:bg-slate-900 h-full min-h-[100dvh] flex flex-col justify-between p-5 sm:p-6 shadow-2xl border-r border-slate-200 dark:border-slate-800 animate-in slide-in-from-left">
          <!-- Drawer Header -->
          <div class="shrink-0 flex items-center justify-between pb-4 border-b border-slate-200 dark:border-slate-800">
            <div class="flex items-center gap-3 font-bold tracking-tight">
              <div class="w-9 h-9 rounded-xl bg-gradient-to-tr from-brand-600 to-emerald-400 flex items-center justify-center shadow-md shrink-0">
                <BookOpen class="w-5 h-5 text-slate-950 font-bold" />
              </div>
              <span class="text-base font-black text-slate-900 dark:text-white">TechDaily Menu</span>
            </div>
            <button
              @click="isMobileNavOpen = false"
              class="p-2 rounded-xl text-slate-400 hover:text-slate-900 dark:hover:text-white"
              aria-label="Close menu"
            >
              <X class="w-5 h-5" />
            </button>
          </div>

          <!-- Navigation Links (Scrollable) -->
          <div class="flex-1 overflow-y-auto min-h-0 py-4 -mx-1 px-1 overscroll-contain">
            <nav class="space-y-1.5">
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
                    'flex items-center gap-3.5 px-4 py-3 rounded-xl text-sm transition-colors',
                    isLinkActive(link.path)
                      ? 'bg-slate-100 dark:bg-slate-800/80 text-brand-600 dark:text-brand-400 font-bold'
                      : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100/70 dark:hover:bg-slate-800/50 font-medium'
                  ]"
                >
                  <component
                    :is="link.icon"
                    :class="[
                      'w-5 h-5 transition-colors shrink-0',
                      isLinkActive(link.path) ? 'text-brand-600 dark:text-brand-400' : 'text-slate-400 dark:text-slate-500'
                    ]"
                  />
                  <span>{{ $t(link.name) }}</span>
                </a>
              </NuxtLink>
            </nav>
          </div>

          <!-- Drawer Footer (Fixed Pinned) -->
          <div class="shrink-0 pt-4 border-t border-slate-200 dark:border-slate-800 space-y-3 pb-[max(1rem,env(safe-area-inset-bottom))]">
            <div v-if="authStore.isLoggedIn && authStore.user" class="flex items-center justify-between p-3 rounded-2xl bg-slate-100 dark:bg-slate-800/60">
              <div class="flex items-center gap-3 min-w-0 pr-2">
                <div class="w-8 h-8 rounded-full bg-brand-600 text-white flex items-center justify-center font-bold text-xs shadow-sm shrink-0">
                  {{ authStore.user.name.charAt(0).toUpperCase() }}
                </div>
                <span class="text-xs font-bold text-slate-900 dark:text-white truncate">
                  {{ authStore.user.name }}
                </span>
              </div>
              <button
                @click="authStore.logout(); isMobileNavOpen = false"
                class="text-xs font-semibold text-rose-600 dark:text-rose-400 shrink-0 hover:underline"
              >
                Log Out
              </button>
            </div>

            <NuxtLink
              v-else
              to="/login"
              @click="isMobileNavOpen = false"
              class="flex items-center justify-center gap-2 w-full py-3 rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-xs font-bold shadow-md shadow-brand-500/20 active:scale-95"
            >
              {{ $t('nav.login') }}
            </NuxtLink>

            <div class="text-[11px] text-slate-400 text-center">
              Senior Engineering Daily Platform
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </header>
</template>
