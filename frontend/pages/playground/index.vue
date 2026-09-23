<script setup lang="ts">
import { FlaskConical, LayoutGrid, SlidersHorizontal, Layers, ArrowRight } from 'lucide-vue-next'

useHead({
  title: 'Playground Sandbox — TechDaily'
})

const prototypes = [
  {
    title: 'Bento Dashboard Modernization',
    route: '/playground/dashboard',
    icon: LayoutGrid,
    desc: 'BentoDashboardLayout prototype with equalized columns, compact typography, and zero dead margins.'
  }
]
</script>

<template>
  <div class="max-w-4xl mx-auto px-4 py-8 space-y-6">
    <div class="flex items-center gap-3">
      <div class="w-10 h-10 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center">
        <FlaskConical class="w-5 h-5" />
      </div>
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">UI Playground Sandbox</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400">Isolated prototypes for visual inspection before production cutover.</p>
      </div>
    </div>

    <div class="grid gap-4 sm:grid-cols-2">
      <NuxtLink
        v-for="p in prototypes"
        :key="p.route"
        :to="p.route"
        class="glass-card p-4 hover:border-brand-500/30 transition-all flex flex-col justify-between group"
      >
        <div>
          <div class="w-8 h-8 rounded-lg bg-slate-100 dark:bg-white/[0.04] text-slate-600 dark:text-slate-300 flex items-center justify-center mb-3">
            <component :is="p.icon" class="w-4 h-4" />
          </div>
          <h2 class="text-sm font-semibold text-slate-900 dark:text-white group-hover:text-brand-400 transition-colors">{{ p.title }}</h2>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">{{ p.desc }}</p>
        </div>
        <div class="flex items-center gap-1.5 text-xs text-brand-500 font-medium mt-4">
          <span>Open Prototype</span>
          <ArrowRight class="w-3.5 h-3.5 group-hover:translate-x-1 transition-transform" />
        </div>
      </NuxtLink>
    </div>
  </div>
</template>
