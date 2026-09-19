<script setup lang="ts">
import { computed } from 'vue'
import {
  Compass,
  Cpu,
  Database,
  Network,
  Layers
} from 'lucide-vue-next'

export interface TopicStatItem {
  total?: number
  correct?: number
  answeredCount?: number
  masteredCount?: number
}

interface Props {
  topicBreakdown?: Record<string, TopicStatItem> | Array<TopicStatItem & { topic?: string }>
}

const props = withDefaults(defineProps<Props>(), {
  topicBreakdown: () => ({})
})

interface PillarConfig {
  category: number
  key: string
  titleKey: string
  defaultTitle: string
  defaultTarget: number
  icon: any
  barColor: string
  trackColor: string
  textColor: string
  badgeColor: string
}

const pillars: PillarConfig[] = [
  {
    category: 1, // Category.BackendDotNet
    key: 'backend_runtime',
    titleKey: 'profile.domain_backend_runtime',
    defaultTitle: 'Backend Runtime & Concurrency',
    defaultTarget: 8,
    icon: Cpu,
    barColor: 'bg-gradient-to-r from-violet-600 to-violet-500',
    trackColor: 'bg-slate-100 dark:bg-white/[0.06]',
    textColor: 'text-violet-600 dark:text-violet-400',
    badgeColor: 'bg-violet-50 dark:bg-violet-950/60 border-violet-200 dark:border-violet-800/60 text-violet-700 dark:text-violet-300'
  },
  {
    category: 2, // Category.DatabaseStorage
    key: 'data_storage',
    titleKey: 'profile.domain_data_storage',
    defaultTitle: 'Data Storage & Persistence',
    defaultTarget: 7,
    icon: Database,
    barColor: 'bg-gradient-to-r from-sky-600 to-sky-500',
    trackColor: 'bg-slate-100 dark:bg-white/[0.06]',
    textColor: 'text-sky-600 dark:text-sky-400',
    badgeColor: 'bg-sky-50 dark:bg-sky-950/60 border-sky-200 dark:border-sky-800/60 text-sky-700 dark:text-sky-300'
  },
  {
    category: 3, // Category.SystemDesign
    key: 'system_design',
    titleKey: 'profile.domain_system_design',
    defaultTitle: 'Distributed Systems & Architecture',
    defaultTarget: 8,
    icon: Network,
    barColor: 'bg-gradient-to-r from-emerald-600 to-emerald-500',
    trackColor: 'bg-slate-100 dark:bg-white/[0.06]',
    textColor: 'text-emerald-600 dark:text-emerald-400',
    badgeColor: 'bg-emerald-50 dark:bg-emerald-950/60 border-emerald-200 dark:border-emerald-800/60 text-emerald-700 dark:text-emerald-300'
  },
  {
    category: 0, // Category.FrontendWeb
    key: 'frontend',
    titleKey: 'profile.domain_frontend',
    defaultTitle: 'Frontend & Browser Engineering',
    defaultTarget: 7,
    icon: Layers,
    barColor: 'bg-gradient-to-r from-amber-600 to-amber-500',
    trackColor: 'bg-slate-100 dark:bg-white/[0.06]',
    textColor: 'text-amber-600 dark:text-amber-400',
    badgeColor: 'bg-amber-50 dark:bg-amber-950/60 border-amber-200 dark:border-amber-800/60 text-amber-700 dark:text-amber-300'
  }
]

function matchCategory(keyOrTopic: string): number | null {
  const k = keyOrTopic.toLowerCase().trim()

  // Pillar 1: Backend Runtime & Concurrency (.NET, Node, Nest, Express, V8, Go, Java, Spring, JVM, Python, etc.)
  if (
    k === '1' || k === 'category.backenddotnet' ||
    k.includes('backend') || k.includes('runtime') || k.includes('concurrency') ||
    k.includes('dotnet') || k.includes('.net') || k.includes('c#') || k.includes('csharp') || k.includes('clr') ||
    k.includes('node') || k.includes('nest') || k.includes('express') || k.includes('v8') || k.includes('event loop') ||
    k.includes('golang') || k.includes('goroutine') || /\bgo\b/.test(k) ||
    (k.includes('java') && !k.includes('javascript')) || k.includes('spring') || k.includes('jvm') ||
    k.includes('python') || k.includes('threading') || k.includes('task') || k.includes('channel')
  ) {
    return 1
  }

  // Pillar 2: Data Storage & Persistence (Postgres, Mongo, Redis, MySQL, SQLite, Cassandra, ACID, B-Tree, LSM, etc.)
  if (
    k === '2' || k === 'category.databasestorage' ||
    k.includes('database') || k.includes('storage') || k.includes('sql') || k.includes('query') ||
    k.includes('postgres') || k.includes('mongo') || k.includes('redis') || k.includes('mysql') ||
    k.includes('sqlite') || k.includes('cassandra') || k.includes('b-tree') || k.includes('lsm') ||
    k.includes('mvcc') || k.includes('acid') || k.includes('index') || k.includes('table') || k.includes('cache')
  ) {
    return 2
  }

  // Pillar 3: Distributed Systems & Architecture (Microservices, Kafka, Rabbit, Outbox, CAP, Consensus, Saga, etc.)
  if (
    k === '3' || k === 'category.systemdesign' ||
    k.includes('system') || k.includes('distributed') || k.includes('architecture') ||
    k.includes('microservice') || k.includes('outbox') || k.includes('kafka') || k.includes('rabbit') ||
    k.includes('event-driven') || k.includes('cap') || k.includes('consensus') || k.includes('rate-limit') ||
    k.includes('resilience') || k.includes('circuit') || k.includes('saga')
  ) {
    return 3
  }

  // Pillar 4: Frontend & Browser Engineering (DOM, Rendering, Vue, React, Next, Nuxt, TypeScript, etc.)
  if (
    k === '0' || k === 'category.frontendweb' ||
    k.includes('frontend') || k.includes('browser') || k.includes('web') ||
    k.includes('react') || k.includes('vue') || k.includes('next') || k.includes('nuxt') ||
    k.includes('angular') || k.includes('svelte') || k.includes('javascript') || k.includes('typescript') ||
    (k.includes('dom') && !k.includes('domain')) || k.includes('vitals') || k.includes('css') ||
    k.includes('rendering') || k.includes('html')
  ) {
    return 0
  }

  return null
}

const domainProgressList = computed(() => {
  const categoryStats: Record<number, { completed: number; total: number; hasData: boolean }> = {
    0: { completed: 0, total: 0, hasData: false },
    1: { completed: 0, total: 0, hasData: false },
    2: { completed: 0, total: 0, hasData: false },
    3: { completed: 0, total: 0, hasData: false }
  }

  const breakdown = props.topicBreakdown
  if (breakdown) {
    if (Array.isArray(breakdown)) {
      for (const item of breakdown) {
        const cat = matchCategory(item.topic || '')
        if (cat !== null) {
          categoryStats[cat].hasData = true
          categoryStats[cat].completed += (item.masteredCount ?? item.correct ?? 0)
          categoryStats[cat].total += (item.answeredCount ?? item.total ?? 0)
        }
      }
    } else {
      for (const [key, item] of Object.entries(breakdown)) {
        const cat = matchCategory(key)
        if (cat !== null) {
          categoryStats[cat].hasData = true
          categoryStats[cat].completed += (item.correct ?? item.masteredCount ?? 0)
          categoryStats[cat].total += (item.total ?? item.answeredCount ?? 0)
        }
      }
    }
  }

  return pillars.map(pillar => {
    const stats = categoryStats[pillar.category]
    const completed = stats.completed
    const total = stats.total > 0 ? stats.total : pillar.defaultTarget
    const percentage = total > 0 ? Math.min(100, Math.round((completed / total) * 100)) : 0

    return {
      ...pillar,
      completed,
      total,
      percentage
    }
  })
})
</script>

<template>
  <div class="glass-card p-5 sm:p-6 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] shadow-sm space-y-4 transition-colors duration-200">
    <!-- Header -->
    <div class="flex items-start justify-between gap-4">
      <div class="space-y-1">
        <h3 class="text-base sm:text-lg font-black text-slate-900 dark:text-white tracking-tight flex items-center gap-2">
          <Compass class="w-5 h-5 text-brand-500 shrink-0" />
          <span>{{ $t('profile.domain_mastery') }}</span>
        </h3>
        <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400">
          {{ $t('profile.domain_mastery_subtitle') }}
        </p>
      </div>
    </div>

    <!-- 4 Pillars Progress Grid -->
    <div class="grid grid-cols-1 gap-2.5 sm:gap-3">
      <div
        v-for="domain in domainProgressList"
        :key="domain.category"
        class="p-3 sm:p-3.5 rounded-xl bg-slate-50/80 dark:bg-canvas-elevated/70 border border-slate-200/60 dark:border-white/[0.06] space-y-2 transition-all hover:border-slate-300 dark:hover:border-white/[0.12]"
      >
        <!-- Top Row: Icon + Title + Percentage -->
        <div class="flex items-center justify-between gap-2">
          <div class="flex items-center gap-2.5 min-w-0">
            <div class="w-7 h-7 rounded-lg flex items-center justify-center shrink-0 border" :class="domain.badgeColor">
              <component :is="domain.icon" class="w-4 h-4" />
            </div>
            <div class="min-w-0">
              <h4 class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white truncate">
                {{ $t(domain.titleKey) }}
              </h4>
              <p class="text-[11px] font-mono font-medium text-slate-500 dark:text-slate-400">
                {{ $t('profile.topics_mastered', { mastered: domain.completed, total: domain.total }) }}
              </p>
            </div>
          </div>

          <!-- Percentage Badge -->
          <div class="text-right shrink-0">
            <span class="text-sm sm:text-base font-black font-mono tracking-tight" :class="domain.textColor">
              {{ domain.percentage }}%
            </span>
          </div>
        </div>

        <!-- Progress Bar with rounded corners -->
        <div class="h-2 w-full rounded-full overflow-hidden" :class="domain.trackColor">
          <div
            class="h-full rounded-full transition-all duration-500 ease-out"
            :class="domain.barColor"
            :style="{ width: `${domain.percentage}%` }"
          ></div>
        </div>
      </div>
    </div>
  </div>
</template>
