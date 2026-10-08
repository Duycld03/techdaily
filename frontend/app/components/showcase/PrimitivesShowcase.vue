<script setup lang="ts">
import { ref } from 'vue'
import { Search, Plus, Trash2, Settings2, Flame } from 'lucide-vue-next'
import AppSelect from '~/components/common/AppSelect.vue'
import AppTimePicker from '~/components/common/AppTimePicker.vue'

const searchValue = ref('')
const selectValue = ref('senior')
const timeValue = ref('08:30')
const trackOptions = [
  { value: 'fresher', label: 'Fresher Track' },
  { value: 'mid', label: 'Mid Track' },
  { value: 'senior', label: 'Senior Track' }
]
const activeTab = ref<'drills' | 'flashcards' | 'scenarios'>('drills')

const tabs = [
  { key: 'drills', label: 'Drills' },
  { key: 'flashcards', label: 'Flashcards' },
  { key: 'scenarios', label: 'Scenarios' }
] as const
</script>

<template>
  <div class="space-y-4">
    <!-- Buttons -->
    <div class="glass-card p-4 space-y-3.5">
      <p class="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
        Buttons
      </p>
      <div class="flex flex-wrap items-center gap-2.5">
        <button
          type="button"
          class="flex h-9 items-center gap-1.5 rounded-lg bg-brand-500 px-3.5 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-brand-600"
        >
          <Plus class="h-4 w-4" :stroke-width="2" />
          Primary
        </button>
        <button
          type="button"
          class="flex h-9 items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3.5 text-sm font-medium text-slate-700 shadow-sm transition-colors hover:bg-slate-50 dark:border-white/[0.08] dark:bg-canvas-elevated dark:text-slate-200 dark:hover:bg-white/[0.06]"
        >
          Secondary
        </button>
        <button
          type="button"
          class="flex h-9 items-center gap-1.5 rounded-lg px-3.5 text-sm font-medium text-slate-600 transition-colors hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-white/[0.06]"
        >
          Ghost
        </button>
        <button
          type="button"
          class="flex h-9 items-center gap-1.5 rounded-lg bg-red-500/10 px-3.5 text-sm font-semibold text-red-600 transition-colors hover:bg-red-500/20 dark:text-red-400"
        >
          <Trash2 class="h-4 w-4" :stroke-width="2" />
          Danger
        </button>
        <button
          type="button"
          aria-label="Settings"
          class="flex h-9 w-9 items-center justify-center rounded-lg border border-slate-200 bg-white text-slate-500 shadow-sm transition-colors hover:bg-slate-50 hover:text-slate-900 dark:border-white/[0.08] dark:bg-canvas-elevated dark:text-slate-400 dark:hover:bg-white/[0.06] dark:hover:text-white"
        >
          <Settings2 class="h-4 w-4" :stroke-width="2" />
        </button>
      </div>
    </div>

    <!-- Inputs, Select & TimePicker -->
    <div class="glass-card p-4 space-y-3.5">
      <p class="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
        Inputs, Select &amp; TimePicker
      </p>
      <div class="grid gap-3 sm:grid-cols-3">
        <div class="relative">
          <Search
            class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400 dark:text-slate-500"
            :stroke-width="2"
          />
          <input
            v-model="searchValue"
            type="text"
            placeholder="Search topics, docs, slices..."
            class="h-9 w-full rounded-lg border border-slate-200 bg-white pl-9 pr-16 text-sm text-slate-900 placeholder:text-slate-400 focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500 dark:border-white/[0.08] dark:bg-canvas-subtle dark:text-slate-100 dark:placeholder:text-slate-500"
          />
          <kbd
            class="absolute right-2.5 top-1/2 -translate-y-1/2 rounded border border-slate-200 bg-slate-50 px-1.5 py-0.5 font-mono text-[11px] font-medium text-slate-400 dark:border-white/[0.08] dark:bg-white/[0.04] dark:text-slate-500"
          >
            ⌘K
          </kbd>
        </div>
        <AppSelect
          v-model="selectValue"
          :options="trackOptions"
          aria-label="Difficulty track"
        />
        <AppTimePicker
          v-model="timeValue"
          aria-label="Demo time picker"
        />
      </div>
    </div>

    <!-- Badges & Pills -->
    <div class="glass-card p-4 space-y-3.5">
      <p class="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
        Badges &amp; Pills
      </p>
      <div class="flex flex-wrap items-center gap-2">
        <span
          class="inline-flex items-center gap-1 rounded-full bg-emerald-500/10 px-2.5 py-1 text-xs font-medium text-emerald-600 dark:text-emerald-400"
        >
          <span class="h-1.5 w-1.5 rounded-full bg-emerald-500" />
          Active
        </span>
        <span
          class="inline-flex items-center gap-1 rounded-full bg-amber-500/10 px-2.5 py-1 text-xs font-medium text-amber-600 dark:text-amber-400"
        >
          <span class="h-1.5 w-1.5 rounded-full bg-amber-500" />
          Pending
        </span>
        <span class="rounded-full bg-sky-500/10 px-2.5 py-1 text-xs font-medium text-sky-600 dark:text-sky-400">
          Fresher
        </span>
        <span class="rounded-full bg-violet-500/10 px-2.5 py-1 text-xs font-medium text-violet-600 dark:text-violet-400">
          Mid
        </span>
        <span class="rounded-full bg-brand-500/15 px-2.5 py-1 text-xs font-semibold text-brand-600 dark:text-brand-300">
          Senior
        </span>
        <span
          class="inline-flex items-center gap-1 rounded-full border border-streak-amber/30 bg-streak-amber/10 px-2.5 py-1 text-xs font-semibold tabular-nums text-streak-amber"
        >
          <Flame class="h-3.5 w-3.5" :stroke-width="2" />
          +10 Pts
        </span>
      </div>
    </div>

    <!-- Segmented Tab Switcher -->
    <div class="glass-card p-4 space-y-3.5">
      <p class="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
        Segmented Switcher
      </p>
      <div
        class="inline-flex items-center gap-1 rounded-lg border border-slate-200 bg-slate-100/80 p-1 dark:border-white/[0.08] dark:bg-white/[0.04]"
      >
        <button
          v-for="tab in tabs"
          :key="tab.key"
          type="button"
          @click="activeTab = tab.key"
          :class="[
            'segmented-control-btn h-7 rounded-md px-3 text-sm font-medium',
            activeTab === tab.key
              ? 'bg-white text-slate-900 shadow-sm dark:bg-canvas-elevated dark:text-white'
              : 'text-slate-500 hover:text-slate-800 dark:text-slate-400 dark:hover:text-slate-200'
          ]"
        >
          {{ tab.label }}
        </button>
      </div>
    </div>
  </div>
</template>
