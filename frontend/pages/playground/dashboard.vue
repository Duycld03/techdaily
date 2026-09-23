<script setup lang="ts">
import { ref } from 'vue'
import {
  BookOpen,
  Terminal,
  ArrowRight,
  Flame,
  Shield,
  Clock,
  CheckCircle2,
  Sparkles,
  Layers,
  Brain,
  TrendingUp
} from 'lucide-vue-next'
import BentoDashboardLayout from '~/components/layout/BentoDashboardLayout.vue'

useHead({
  title: 'Dashboard Prototype (Bento Layout) — Playground'
})

// Self-contained Mock Data
const user = {
  name: 'Duy',
  targetRole: 'Senior Fullstack Engineer',
  streak: 12,
  freezeCredits: 2
}

const pacer = {
  bookTitle: 'Designing Data-Intensive Applications',
  currentChunkOrder: 4,
  totalChunks: 30,
  estimatedMinutes: 5
}

const activeTopic = {
  title: 'PostgreSQL MVCC & WAL Buffers Under High Concurrent Writes',
  summary: 'Explore how multi-version concurrency control isolates transactions without read locks, and analyze the write-ahead log write amplification trade-offs.',
  tags: ['Database', 'Concurrency', 'PostgreSQL']
}

const scenario = {
  title: 'Design an Idempotent Payment Webhook Consumer for Stripe',
  context: 'Mitigate duplicate network delivery and concurrent retry race conditions under network partition.',
  difficulty: 'Senior Architect',
  estimatedMinutes: 8
}

const heatmapDays = [
  { day: 'Mon', completed: true, isToday: false },
  { day: 'Tue', completed: true, isToday: false },
  { day: 'Wed', completed: true, isToday: false },
  { day: 'Thu', completed: true, isToday: false },
  { day: 'Fri', completed: true, isToday: false },
  { day: 'Sat', completed: true, isToday: false },
  { day: 'Sun', completed: false, isToday: true }
]

const dueCardsCount = 6
</script>

<template>
  <div class="h-full flex flex-col bg-slate-50 dark:bg-canvas transition-colors duration-200">
    <!-- Sandbox Navigation Banner -->
    <div class="bg-amber-500/10 border-b border-amber-500/20 px-4 py-2 flex items-center justify-between text-xs text-amber-600 dark:text-amber-400">
      <div class="flex items-center gap-2">
        <span class="px-2 py-0.5 rounded bg-amber-500/20 font-bold uppercase tracking-wider text-[10px]">Playground Prototype</span>
        <span>Testing BentoDashboardLayout with equalized card bounds & zero dead margins.</span>
      </div>
      <NuxtLink to="/playground" class="font-bold underline hover:text-amber-300">Exit Playground</NuxtLink>
    </div>

    <!-- Main Bento Dashboard Shell -->
    <div class="flex-1 overflow-y-auto lg:overflow-hidden flex flex-col">
      <BentoDashboardLayout>
        <!-- Slot: Header (Orientation Banner) -->
        <template #header>
          <div class="glass-card px-4 py-3 sm:px-5 sm:py-3.5 relative overflow-hidden border border-slate-200/80 dark:border-white/[0.06]">
            <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 relative z-10">
              <div class="space-y-1">
                <div class="flex items-center gap-2">
                  <span class="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-slate-100 dark:bg-white/[0.06] text-slate-700 dark:text-slate-300 border border-slate-200/60 dark:border-white/[0.08]">
                    Slice {{ pacer.currentChunkOrder }} / {{ pacer.totalChunks }}
                  </span>
                  <span class="text-xs text-slate-500 dark:text-slate-400">• {{ user.targetRole }}</span>
                </div>
                <h1 class="text-lg sm:text-xl lg:text-2xl font-black text-slate-900 dark:text-white tracking-tight">
                  Welcome Back, {{ user.name }}! 👋
                </h1>
                <p class="text-xs sm:text-sm text-slate-600 dark:text-slate-300 max-w-2xl">
                  Pick up your 15-minute senior engineering focus for today.
                </p>
              </div>

              <!-- Streak & Freeze Badges -->
              <div class="flex items-center gap-2 self-end sm:self-center shrink-0">
                <div class="px-3 py-1.5 rounded-xl bg-amber-500/10 border border-amber-500/20 flex items-center gap-2 text-amber-600 dark:text-amber-400 text-xs font-bold">
                  <Flame class="w-4 h-4 text-amber-500" />
                  <span>{{ user.streak }} Day Streak</span>
                </div>
                <div class="px-3 py-1.5 rounded-xl bg-brand-500/10 border border-brand-500/20 flex items-center gap-2 text-brand-600 dark:text-brand-400 text-xs font-bold">
                  <Shield class="w-4 h-4 text-brand-500" />
                  <span>{{ user.freezeCredits }} Freezes</span>
                </div>
              </div>
            </div>
          </div>
        </template>

        <!-- Slot: Action Stage (Card A & Card B) -->
        <template #action-stage>
          <!-- Card A: Daily Reading Slice -->
          <div class="glass-card p-4 sm:p-5 flex flex-col justify-between group hover:border-brand-500/30 transition-all border border-slate-200/80 dark:border-white/[0.06]">
            <div>
              <div class="flex items-center justify-between gap-3 mb-2.5">
                <div class="flex items-center gap-2">
                  <div class="w-7 h-7 rounded-lg bg-brand-500/10 text-brand-600 dark:text-brand-400 flex items-center justify-center shrink-0">
                    <BookOpen class="w-3.5 h-3.5" :stroke-width="2" />
                  </div>
                  <span class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                    Active Reading Slice
                  </span>
                </div>
                <span class="text-xs text-slate-500 dark:text-slate-400 flex items-center gap-1">
                  <Clock class="w-3.5 h-3.5" />
                  {{ pacer.estimatedMinutes }} mins read
                </span>
              </div>

              <h2 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white group-hover:text-brand-400 transition-colors">
                {{ activeTopic.title }}
              </h2>
              <p class="text-xs sm:text-sm text-slate-600 dark:text-slate-400 mt-1 line-clamp-2 leading-relaxed">
                {{ activeTopic.summary }}
              </p>
            </div>

            <div class="flex items-center justify-between gap-3 mt-4 pt-3 border-t border-slate-100 dark:border-white/[0.04]">
              <div class="flex items-center gap-1.5 flex-wrap">
                <span v-for="tag in activeTopic.tags" :key="tag" class="px-2 py-0.5 rounded text-[11px] font-medium bg-slate-100 dark:bg-white/[0.04] text-slate-600 dark:text-slate-300">
                  #{{ tag }}
                </span>
              </div>
              <button class="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-xs transition-all shadow-sm shrink-0">
                <span>Start Reading</span>
                <ArrowRight class="w-3.5 h-3.5" />
              </button>
            </div>
          </div>

          <!-- Card B: Daily Scenario Challenge -->
          <div class="glass-card p-4 sm:p-5 flex flex-col justify-between group hover:border-emerald-500/30 transition-all border border-slate-200/80 dark:border-white/[0.06]">
            <div>
              <div class="flex items-center justify-between gap-3 mb-2.5">
                <div class="flex items-center gap-2">
                  <div class="w-7 h-7 rounded-lg bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shrink-0">
                    <Terminal class="w-3.5 h-3.5" :stroke-width="2" />
                  </div>
                  <span class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                    System Design Scenario
                  </span>
                </div>
                <span class="px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20">
                  {{ scenario.difficulty }}
                </span>
              </div>

              <h2 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white group-hover:text-emerald-400 transition-colors">
                {{ scenario.title }}
              </h2>
              <p class="text-xs sm:text-sm text-slate-600 dark:text-slate-400 mt-1 line-clamp-2 leading-relaxed">
                {{ scenario.context }}
              </p>
            </div>

            <div class="flex items-center justify-between gap-3 mt-4 pt-3 border-t border-slate-100 dark:border-white/[0.04]">
              <span class="text-xs text-slate-500 dark:text-slate-400 flex items-center gap-1">
                <Clock class="w-3.5 h-3.5" />
                {{ scenario.estimatedMinutes }} mins challenge
              </span>
              <button class="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-slate-900 dark:bg-white text-white dark:text-slate-900 hover:bg-slate-800 dark:hover:bg-slate-100 font-semibold text-xs transition-all shadow-sm shrink-0">
                <span>Solve Challenge</span>
                <ArrowRight class="w-3.5 h-3.5" />
              </button>
            </div>
          </div>
        </template>

        <!-- Slot: Telemetry Dock (Card C & Card D) -->
        <template #telemetry-dock>
          <!-- Card C: 7-Day Consistency & SM-2 Retention -->
          <div class="glass-card p-4 sm:p-5 flex flex-col justify-between border border-slate-200/80 dark:border-white/[0.06]">
            <div>
              <div class="flex items-center justify-between mb-3">
                <span class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 flex items-center gap-1.5">
                  <TrendingUp class="w-3.5 h-3.5 text-brand-500" />
                  Study Consistency
                </span>
                <span class="text-xs font-semibold text-emerald-500">6 / 7 Days</span>
              </div>

              <!-- 7-Day Heatmap row -->
              <div class="grid grid-cols-7 gap-1.5 text-center">
                <div v-for="d in heatmapDays" :key="d.day" class="flex flex-col items-center gap-1">
                  <span class="text-[10px] text-slate-400">{{ d.day }}</span>
                  <div
                    :class="[
                      'w-7 h-7 rounded-lg flex items-center justify-center text-xs font-bold transition-all',
                      d.completed
                        ? 'bg-emerald-500/20 text-emerald-500 border border-emerald-500/30'
                        : d.isToday
                          ? 'border border-dashed border-brand-500 text-brand-400 animate-pulse'
                          : 'bg-slate-100 dark:bg-white/[0.02] text-slate-400'
                    ]"
                  >
                    <CheckCircle2 v-if="d.completed" class="w-3.5 h-3.5" />
                    <span v-else>•</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- SM-2 Quick Review Pill -->
            <NuxtLink
              to="/review"
              class="mt-4 p-3 rounded-xl bg-brand-500/10 hover:bg-brand-500/15 border border-brand-500/20 flex items-center justify-between text-xs transition-all"
            >
              <div class="flex items-center gap-2">
                <Layers class="w-4 h-4 text-brand-500" />
                <span class="font-semibold text-brand-600 dark:text-brand-300">Spaced Review Queue</span>
              </div>
              <span class="px-2 py-0.5 rounded-full bg-brand-500 text-white font-bold text-[10px]">
                {{ dueCardsCount }} Due
              </span>
            </NuxtLink>
          </div>

          <!-- Card D: Knowledge Constellation Preview -->
          <div class="glass-card p-4 sm:p-5 flex flex-col justify-between border border-slate-200/80 dark:border-white/[0.06]">
            <div>
              <div class="flex items-center justify-between mb-2.5">
                <span class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 flex items-center gap-1.5">
                  <Brain class="w-3.5 h-3.5 text-brand-500" />
                  Knowledge Cosmos
                </span>
                <span class="text-[11px] font-mono text-slate-400">42 / 120 Nodes</span>
              </div>
              <p class="text-xs text-slate-600 dark:text-slate-400">
                Your neural mastery graph has formed 18 cross-domain architectural links.
              </p>
            </div>

            <NuxtLink
              to="/graph"
              class="mt-4 inline-flex items-center justify-center gap-1.5 py-2 px-3 rounded-xl bg-slate-100 dark:bg-white/[0.04] hover:bg-slate-200 dark:hover:bg-white/[0.08] text-xs font-semibold text-slate-700 dark:text-slate-200 border border-slate-200/80 dark:border-white/[0.08] transition-all"
            >
              <span>Explore Knowledge Cosmos</span>
              <ArrowRight class="w-3.5 h-3.5" />
            </NuxtLink>
          </div>
        </template>
      </BentoDashboardLayout>
    </div>
  </div>
</template>
