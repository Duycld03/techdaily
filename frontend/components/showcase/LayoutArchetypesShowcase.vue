<script setup lang="ts">
import { ref, computed } from 'vue'
import {
  Layers, SlidersHorizontal, LayoutGrid,
  RotateCcw, Clock, TrendingUp, Repeat, Keyboard,
  SettingsIcon, Bell, ShieldCheck,
  Search, Plus, List, Grid2x2, ChevronLeft, ChevronRight,
  BookOpen, Hash
} from 'lucide-vue-next'
import StudioLayout from '~/components/layout/StudioLayout.vue'
import MasterDetailLayout from '~/components/layout/MasterDetailLayout.vue'
import BoardLayout from '~/components/layout/BoardLayout.vue'
import AppSelect from '~/components/common/AppSelect.vue'

type ArchetypeKey = 'studio' | 'master-detail' | 'board'

const archetypes = [
  { key: 'studio', label: 'Flashcards Studio', icon: Layers },
  { key: 'master-detail', label: 'Settings Master-Detail', icon: SlidersHorizontal },
  { key: 'board', label: 'Notes Board', icon: LayoutGrid }
] as const

const active = ref<ArchetypeKey>('studio')

/* ---------- Demo 1: Flashcards Studio ---------- */
const revealed = ref(false)
const ratings = [
  { key: 'again', label: 'Again', hint: '<1m', cls: 'bg-red-500/10 text-red-600 hover:bg-red-500/20 dark:text-red-400' },
  { key: 'hard', label: 'Hard', hint: '8m', cls: 'bg-amber-500/10 text-amber-600 hover:bg-amber-500/20 dark:text-amber-400' },
  { key: 'good', label: 'Good', hint: '1d', cls: 'bg-emerald-500/10 text-emerald-600 hover:bg-emerald-500/20 dark:text-emerald-400' },
  { key: 'easy', label: 'Easy', hint: '4d', cls: 'bg-brand-500/15 text-brand-600 hover:bg-brand-500/25 dark:text-brand-300' }
] as const
const smStats = [
  { icon: TrendingUp, label: 'EF Factor', value: '2.48' },
  { icon: Clock, label: 'Interval', value: '6d' },
  { icon: Repeat, label: 'Repetitions', value: '3' }
]
const hotkeys = [
  { keys: 'Space', action: 'Flip card' },
  { keys: '1 – 4', action: 'Rate recall' },
  { keys: 'U', action: 'Undo last' },
  { keys: 'Esc', action: 'Exit session' }
]

/* ---------- Demo 2: Settings Master-Detail ---------- */
const settingsSections = [
  { key: 'general', label: 'General', icon: SettingsIcon, badge: null },
  { key: 'notifications', label: 'Notifications', icon: Bell, badge: '3' },
  { key: 'security', label: 'Security', icon: ShieldCheck, badge: null }
] as const
const activeSection = ref<'general' | 'notifications' | 'security'>('general')
const track = ref('senior')
const dailyGoal = ref('30')
const timezone = ref('ict')
const theme = ref('obsidian')
const trackOptions = [
  { value: 'fresher', label: 'Fresher Track' },
  { value: 'mid', label: 'Mid Track' },
  { value: 'senior', label: 'Senior Track' }
]
const goalOptions = [
  { value: '15', label: '15 cards / day' },
  { value: '30', label: '30 cards / day' },
  { value: '60', label: '60 cards / day' }
]
const tzOptions = [
  { value: 'ict', label: 'Asia/Ho_Chi_Minh (GMT+7)' },
  { value: 'utc', label: 'UTC (GMT+0)' },
  { value: 'pst', label: 'America/Los_Angeles (GMT-8)' }
]
const themeOptions = [
  { value: 'obsidian', label: 'Dark Obsidian' },
  { value: 'light', label: 'Light' },
  { value: 'system', label: 'Match System' }
]

/* ---------- Demo 3: Notes Board ---------- */
const boardSearch = ref('')
const boardView = ref<'grid' | 'list'>('grid')
const activeTag = ref('all')
const tags = ['all', 'Database', 'Vue', 'Kafka', 'System Design']
const notes = [
  { id: 1, tag: 'Database', title: 'B-Tree vs LSM-Tree', excerpt: 'LSM-trees favor write throughput via sequential appends and background compaction; B-trees favor read latency with in-place updates.', src: 'DDIA · Ch. 3' },
  { id: 2, tag: 'Kafka', title: 'Exactly-once semantics', excerpt: 'Idempotent producers plus transactional writes give end-to-end EOS across consume-process-produce loops.', src: 'Kafka Docs' },
  { id: 3, tag: 'Vue', title: 'Reactivity: ref vs reactive', excerpt: 'ref wraps a value in a { value } container so primitives stay reactive; reactive proxies an object and loses reactivity on destructure.', src: 'Vue Guide' },
  { id: 4, tag: 'System Design', title: 'Consistent hashing', excerpt: 'Virtual nodes smooth key distribution and minimize rebalancing when a node joins or leaves the ring.', src: 'Grokking SD' },
  { id: 5, tag: 'Database', title: 'MVCC snapshots', excerpt: 'Each transaction reads a consistent snapshot by version, avoiding read locks while writers append new row versions.', src: 'Postgres Internals' },
  { id: 6, tag: 'Vue', title: 'Teleport for overlays', excerpt: 'Teleport renders modal/listbox content at <body> to escape overflow and stacking-context traps.', src: 'Vue Guide' }
]
const filteredNotes = computed(() => {
  const q = boardSearch.value.trim().toLowerCase()
  return notes.filter(n => {
    const tagOk = activeTag.value === 'all' || n.tag === activeTag.value
    const qOk = !q || n.title.toLowerCase().includes(q) || n.excerpt.toLowerCase().includes(q)
    return tagOk && qOk
  })
})
const tagColor: Record<string, string> = {
  Database: 'text-sky-600 dark:text-sky-400 bg-sky-500/10',
  Vue: 'text-emerald-600 dark:text-emerald-400 bg-emerald-500/10',
  Kafka: 'text-amber-600 dark:text-amber-400 bg-amber-500/10',
  'System Design': 'text-brand-600 dark:text-brand-300 bg-brand-500/15'
}
</script>

<template>
  <div class="space-y-4">
    <!-- Archetype tab switcher -->
    <div
      class="inline-flex items-center gap-1 rounded-lg border border-slate-200 bg-slate-100/80 p-1 dark:border-white/[0.08] dark:bg-white/[0.04]"
    >
      <button
        v-for="a in archetypes"
        :key="a.key"
        type="button"
        @click="active = a.key"
        :class="[
          'flex h-8 items-center gap-1.5 rounded-md px-3 text-sm font-medium transition-colors',
          active === a.key
            ? 'bg-white text-slate-900 shadow-sm dark:bg-canvas-elevated dark:text-white'
            : 'text-slate-500 hover:text-slate-800 dark:text-slate-400 dark:hover:text-slate-200'
        ]"
      >
        <component :is="a.icon" class="h-4 w-4" :stroke-width="2" />
        {{ a.label }}
      </button>
    </div>

    <!-- ============ Demo 1: Flashcards Studio ============ -->
    <StudioLayout v-if="active === 'studio'">
      <template #header>
        <div class="flex items-center gap-2 text-sm font-semibold text-slate-800 dark:text-slate-100">
          <BookOpen class="h-4 w-4 text-brand-500" :stroke-width="2" />
          Distributed Systems
        </div>
        <span class="rounded-full bg-brand-500/15 px-2.5 py-1 text-xs font-semibold tabular-nums text-brand-600 dark:text-brand-300">
          Card 4 / 20
        </span>
      </template>

      <template #main>
        <div
          class="flex min-h-[240px] flex-col rounded-xl border border-slate-200 bg-white p-6 shadow-sm dark:border-white/[0.08] dark:bg-canvas-elevated"
        >
          <span class="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
            Question
          </span>
          <p class="mt-2 text-lg font-semibold text-slate-900 dark:text-white">
            What guarantee does a quorum write (W + R &gt; N) provide in a replicated store?
          </p>
          <div class="my-4 h-px bg-slate-100 dark:bg-white/[0.06]" />
          <template v-if="revealed">
            <span class="text-xs font-semibold uppercase tracking-wide text-emerald-500">Answer</span>
            <p class="mt-2 text-sm leading-relaxed text-slate-600 dark:text-slate-300">
              It guarantees that read and write quorums overlap on at least one node, so a read always
              observes the most recent acknowledged write — providing strong consistency for that key.
            </p>
          </template>
          <button
            v-else
            type="button"
            @click="revealed = true"
            class="mt-auto flex h-9 w-full items-center justify-center gap-1.5 rounded-lg border border-dashed border-slate-300 text-sm font-medium text-slate-500 transition-colors hover:border-brand-400 hover:text-brand-600 dark:border-white/[0.12] dark:text-slate-400 dark:hover:text-brand-300"
          >
            Reveal answer · Space
          </button>
        </div>
      </template>

      <template #dock>
        <!-- Session progress -->
        <div class="rounded-lg border border-slate-200 bg-white p-3 dark:border-white/[0.08] dark:bg-canvas-elevated">
          <div class="mb-2 flex items-center justify-between text-xs font-medium text-slate-500 dark:text-slate-400">
            <span>Session progress</span>
            <span class="tabular-nums">4 / 20</span>
          </div>
          <div class="h-1.5 overflow-hidden rounded-full bg-slate-100 dark:bg-white/[0.06]">
            <div class="h-full rounded-full bg-brand-500" style="width: 20%" />
          </div>
        </div>

        <!-- SM-2 stats -->
        <div class="rounded-lg border border-slate-200 bg-white p-3 dark:border-white/[0.08] dark:bg-canvas-elevated">
          <p class="mb-2.5 text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
            SM-2 Scheduler
          </p>
          <div class="space-y-2">
            <div v-for="s in smStats" :key="s.label" class="flex items-center justify-between">
              <span class="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
                <component :is="s.icon" class="h-4 w-4 text-slate-400" :stroke-width="2" />
                {{ s.label }}
              </span>
              <span class="text-sm font-semibold tabular-nums text-slate-900 dark:text-white">{{ s.value }}</span>
            </div>
          </div>
        </div>

        <!-- Hotkeys -->
        <div class="rounded-lg border border-slate-200 bg-white p-3 dark:border-white/[0.08] dark:bg-canvas-elevated">
          <p class="mb-2.5 flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
            <Keyboard class="h-3.5 w-3.5" :stroke-width="2" />
            Hotkeys
          </p>
          <div class="space-y-1.5">
            <div v-for="h in hotkeys" :key="h.keys" class="flex items-center justify-between text-sm">
              <span class="text-slate-600 dark:text-slate-300">{{ h.action }}</span>
              <kbd class="rounded border border-slate-200 bg-slate-50 px-1.5 py-0.5 font-mono text-[11px] text-slate-500 dark:border-white/[0.08] dark:bg-white/[0.04] dark:text-slate-400">
                {{ h.keys }}
              </kbd>
            </div>
          </div>
        </div>
      </template>

      <template #footer>
        <button
          type="button"
          @click="revealed = false"
          class="flex h-9 items-center gap-1.5 rounded-lg px-3 text-sm font-medium text-slate-500 transition-colors hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-white/[0.06]"
        >
          <RotateCcw class="h-4 w-4" :stroke-width="2" />
          Reset
        </button>
        <div class="flex items-center gap-2">
          <button
            v-for="r in ratings"
            :key="r.key"
            type="button"
            @click="revealed = false"
            :disabled="!revealed"
            :class="[
              'flex h-9 flex-col items-center justify-center rounded-lg px-3 text-sm font-semibold leading-none transition-colors disabled:cursor-not-allowed disabled:opacity-40',
              r.cls
            ]"
          >
            {{ r.label }}
            <span class="mt-0.5 text-[10px] font-normal opacity-70">{{ r.hint }}</span>
          </button>
        </div>
      </template>
    </StudioLayout>

    <!-- ============ Demo 2: Settings Master-Detail ============ -->
    <MasterDetailLayout v-else-if="active === 'master-detail'">
      <template #header>
        <div class="flex items-center gap-2 text-sm font-semibold text-slate-800 dark:text-slate-100">
          <SlidersHorizontal class="h-4 w-4 text-brand-500" :stroke-width="2" />
          Settings
        </div>
        <button
          type="button"
          class="flex h-9 items-center rounded-lg bg-brand-500 px-3.5 text-sm font-semibold text-white transition-colors hover:bg-brand-600"
        >
          Save changes
        </button>
      </template>

      <template #nav>
        <button
          v-for="s in settingsSections"
          :key="s.key"
          type="button"
          @click="activeSection = s.key"
          :class="[
            'flex w-full items-center justify-between gap-2 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
            activeSection === s.key
              ? 'bg-brand-500/10 text-brand-700 dark:bg-brand-500/15 dark:text-brand-300'
              : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-white/[0.06]'
          ]"
        >
          <span class="flex items-center gap-2.5">
            <component :is="s.icon" class="h-4 w-4" :stroke-width="2" />
            {{ s.label }}
          </span>
          <span
            v-if="s.badge"
            class="rounded-full bg-brand-500 px-1.5 text-[11px] font-bold tabular-nums text-white"
          >
            {{ s.badge }}
          </span>
        </button>
      </template>

      <template #content>
        <div v-if="activeSection === 'general'" class="max-w-3xl space-y-5">
          <div>
            <h3 class="text-sm font-semibold text-slate-900 dark:text-white">Learning preferences</h3>
            <p class="text-xs text-slate-500 dark:text-slate-400">Tune your daily practice and scheduling.</p>
          </div>
          <!-- Two-column form grid — no long single-column stack -->
          <div class="grid gap-4 sm:grid-cols-2">
            <div class="space-y-1.5">
              <label class="text-xs font-medium text-slate-600 dark:text-slate-300">Difficulty track</label>
              <AppSelect v-model="track" :options="trackOptions" aria-label="Difficulty track" />
            </div>
            <div class="space-y-1.5">
              <label class="text-xs font-medium text-slate-600 dark:text-slate-300">Daily goal</label>
              <AppSelect v-model="dailyGoal" :options="goalOptions" aria-label="Daily goal" />
            </div>
            <div class="space-y-1.5">
              <label class="text-xs font-medium text-slate-600 dark:text-slate-300">Timezone</label>
              <AppSelect v-model="timezone" :options="tzOptions" aria-label="Timezone" />
            </div>
            <div class="space-y-1.5">
              <label class="text-xs font-medium text-slate-600 dark:text-slate-300">Theme</label>
              <AppSelect v-model="theme" :options="themeOptions" aria-label="Theme" />
            </div>
          </div>
          <div class="rounded-lg border border-slate-200 p-3.5 dark:border-white/[0.08]">
            <label class="flex items-center justify-between gap-3">
              <span>
                <span class="block text-sm font-medium text-slate-800 dark:text-slate-100">Auto-advance cards</span>
                <span class="block text-xs text-slate-500 dark:text-slate-400">Move to the next card after rating.</span>
              </span>
              <span class="relative inline-flex h-5 w-9 shrink-0 items-center rounded-full bg-brand-500">
                <span class="ml-auto mr-0.5 h-4 w-4 rounded-full bg-white shadow" />
              </span>
            </label>
          </div>
        </div>

        <div v-else-if="activeSection === 'notifications'" class="max-w-3xl space-y-3">
          <h3 class="text-sm font-semibold text-slate-900 dark:text-white">Notifications</h3>
          <div
            v-for="n in ['Daily review reminder', 'Streak at risk alert', 'Weekly progress digest']"
            :key="n"
            class="flex items-center justify-between rounded-lg border border-slate-200 p-3.5 dark:border-white/[0.08]"
          >
            <span class="text-sm text-slate-700 dark:text-slate-200">{{ n }}</span>
            <span class="relative inline-flex h-5 w-9 shrink-0 items-center rounded-full bg-brand-500">
              <span class="ml-auto mr-0.5 h-4 w-4 rounded-full bg-white shadow" />
            </span>
          </div>
        </div>

        <div v-else class="max-w-3xl space-y-3">
          <h3 class="text-sm font-semibold text-slate-900 dark:text-white">Security</h3>
          <div class="grid gap-4 sm:grid-cols-2">
            <div class="space-y-1.5">
              <label class="text-xs font-medium text-slate-600 dark:text-slate-300">Current password</label>
              <input type="password" value="passwordvalue" class="h-9 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-white/[0.08] dark:bg-canvas-subtle dark:text-slate-100" />
            </div>
            <div class="space-y-1.5">
              <label class="text-xs font-medium text-slate-600 dark:text-slate-300">New password</label>
              <input type="password" placeholder="••••••••" class="h-9 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm placeholder:text-slate-400 dark:border-white/[0.08] dark:bg-canvas-subtle dark:text-slate-100" />
            </div>
          </div>
        </div>
      </template>
    </MasterDetailLayout>

    <!-- ============ Demo 3: Notes Board ============ -->
    <BoardLayout v-else>
      <template #header>
        <div class="relative w-full max-w-xs">
          <Search class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400 dark:text-slate-500" :stroke-width="2" />
          <input
            v-model="boardSearch"
            type="text"
            placeholder="Search highlights..."
            class="h-9 w-full rounded-lg border border-slate-200 bg-white pl-9 pr-14 text-sm text-slate-900 placeholder:text-slate-400 focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500 dark:border-white/[0.08] dark:bg-canvas-subtle dark:text-slate-100 dark:placeholder:text-slate-500"
          />
          <kbd class="absolute right-2.5 top-1/2 -translate-y-1/2 rounded border border-slate-200 bg-slate-50 px-1.5 py-0.5 font-mono text-[11px] text-slate-400 dark:border-white/[0.08] dark:bg-white/[0.04] dark:text-slate-500">⌘K</kbd>
        </div>
        <button
          type="button"
          class="flex h-9 items-center gap-1.5 rounded-lg bg-brand-500 px-3.5 text-sm font-semibold text-white transition-colors hover:bg-brand-600"
        >
          <Plus class="h-4 w-4" :stroke-width="2" />
          New Note
        </button>
      </template>

      <template #filters>
        <button
          v-for="t in tags"
          :key="t"
          type="button"
          @click="activeTag = t"
          :class="[
            'flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium transition-colors',
            activeTag === t
              ? 'bg-brand-500/15 text-brand-600 dark:text-brand-300'
              : 'text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-white/[0.06]'
          ]"
        >
          <Hash v-if="t !== 'all'" class="h-3 w-3" :stroke-width="2" />
          {{ t === 'all' ? 'All' : t }}
        </button>
        <div class="ml-auto inline-flex items-center gap-1 rounded-lg border border-slate-200 bg-slate-100/80 p-0.5 dark:border-white/[0.08] dark:bg-white/[0.04]">
          <button
            type="button"
            aria-label="Grid view"
            @click="boardView = 'grid'"
            :class="['flex h-7 w-7 items-center justify-center rounded-md transition-colors', boardView === 'grid' ? 'bg-white text-slate-900 shadow-sm dark:bg-canvas-elevated dark:text-white' : 'text-slate-400']"
          >
            <Grid2x2 class="h-4 w-4" :stroke-width="2" />
          </button>
          <button
            type="button"
            aria-label="List view"
            @click="boardView = 'list'"
            :class="['flex h-7 w-7 items-center justify-center rounded-md transition-colors', boardView === 'list' ? 'bg-white text-slate-900 shadow-sm dark:bg-canvas-elevated dark:text-white' : 'text-slate-400']"
          >
            <List class="h-4 w-4" :stroke-width="2" />
          </button>
        </div>
      </template>

      <template #content>
        <div
          :class="boardView === 'grid'
            ? 'grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3'
            : 'flex flex-col gap-3'"
        >
          <article
            v-for="n in filteredNotes"
            :key="n.id"
            class="group flex flex-col rounded-xl border border-slate-200 bg-white p-4 shadow-sm transition-colors hover:border-brand-400/50 dark:border-white/[0.08] dark:bg-canvas-elevated dark:hover:border-brand-500/40"
          >
            <div class="mb-2 flex items-center justify-between">
              <span :class="['rounded-full px-2 py-0.5 text-[11px] font-medium', tagColor[n.tag] ?? 'bg-slate-500/10 text-slate-500']">
                {{ n.tag }}
              </span>
              <span class="text-[11px] text-slate-400 dark:text-slate-500">{{ n.src }}</span>
            </div>
            <h4 class="text-sm font-semibold text-slate-900 dark:text-white">{{ n.title }}</h4>
            <p class="mt-1 line-clamp-3 text-xs leading-relaxed text-slate-500 dark:text-slate-400">{{ n.excerpt }}</p>
          </article>
        </div>
        <p v-if="!filteredNotes.length" class="py-10 text-center text-sm text-slate-400 dark:text-slate-500">
          No highlights match your filters.
        </p>
      </template>

      <template #pagination>
        <span class="text-xs text-slate-500 dark:text-slate-400">
          Showing <span class="font-semibold tabular-nums text-slate-700 dark:text-slate-200">{{ filteredNotes.length }}</span> of {{ notes.length }}
        </span>
        <div class="flex items-center gap-1">
          <button type="button" aria-label="Previous page" class="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 text-slate-400 transition-colors hover:bg-slate-100 dark:border-white/[0.08] dark:hover:bg-white/[0.06]">
            <ChevronLeft class="h-4 w-4" :stroke-width="2" />
          </button>
          <span class="px-2 text-sm font-medium tabular-nums text-slate-700 dark:text-slate-200">1 / 1</span>
          <button type="button" aria-label="Next page" class="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 text-slate-400 transition-colors hover:bg-slate-100 dark:border-white/[0.08] dark:hover:bg-white/[0.06]">
            <ChevronRight class="h-4 w-4" :stroke-width="2" />
          </button>
        </div>
      </template>
    </BoardLayout>
  </div>
</template>
