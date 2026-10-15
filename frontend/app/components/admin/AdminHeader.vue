<script setup lang="ts">
import { Menu, ShieldCheck } from 'lucide-vue-next'
import ThemeToggle from '~/components/common/ThemeToggle.vue'
import LocaleSelector from '~/components/common/LocaleSelector.vue'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'

defineEmits<{ menu: [] }>()
const access = useAdminAccessStore()
</script>

<template>
  <header class="flex h-14 sm:h-15 shrink-0 items-center justify-between gap-2 sm:gap-4 border-b border-slate-200/80 bg-white/95 px-3 sm:px-6 dark:border-white/[0.08] dark:bg-canvas/80 backdrop-blur-md" data-admin-header>
    <div class="flex min-w-0 items-center gap-2 sm:gap-3">
      <button type="button" class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl md:hidden hover:bg-slate-100 dark:hover:bg-white/[0.06]" :aria-label="$t('admin.nav.open')" @click="$emit('menu')"><Menu class="h-5 w-5" aria-hidden="true" /></button>
      <NuxtLink :to="access.canViewAdminDashboard ? '/admin' : access.getFirstAccessibleAdminRoute()" class="flex items-center gap-2 font-bold text-base sm:text-lg whitespace-nowrap shrink-0"><img src="/favicon.svg" alt="" class="h-7 w-7" />DeepPace</NuxtLink>
      <span class="hidden sm:inline-flex items-center gap-1.5 rounded-lg border border-brand-500/20 bg-brand-500/10 px-2.5 py-1 text-xs font-semibold text-brand-600 dark:text-brand-300 whitespace-nowrap shrink-0"><ShieldCheck class="h-3.5 w-3.5" aria-hidden="true" />{{ $t('admin.nav.admin') }}</span>
    </div>
    <div class="flex items-center gap-1.5 sm:gap-3 shrink-0">
      <LocaleSelector />
      <ThemeToggle :light-label="$t('admin.preferences.light')" :dark-label="$t('admin.preferences.dark')" :toggle-label="$t('admin.preferences.theme')" />
    </div>
  </header>
</template>
