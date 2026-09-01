<script setup lang="ts">
import { onMounted } from 'vue'
import { 
  Map, 
  CheckCircle2, 
  Flame, 
  Lock, 
  ArrowRight, 
  Eye, 
  Layers, 
  Cpu, 
  Database, 
  Network, 
  Sparkles,
  Calendar,
  Award
} from 'lucide-vue-next'
import { useRoadmapStore } from '~/stores/useRoadmapStore'
import { useAuthStore } from '~/stores/useAuthStore'

const roadmapStore = useRoadmapStore()
const authStore = useAuthStore()
const router = useRouter()

onMounted(async () => {
  await roadmapStore.fetchRoadmap()
})

const categoryIcons = [
  Layers,    // 0: FrontendWeb
  Cpu,       // 1: BackendDotNet
  Database,  // 2: DatabaseStorage
  Network    // 3: SystemDesign
]

function getModuleIcon(category: number) {
  return categoryIcons[category] || Layers
}

function getDifficultyLabel(diff: number) {
  switch (diff) {
    case 0: return 'Intermediate'
    case 1: return 'Senior'
    case 2: return 'Lead Architect'
    default: return 'Senior'
  }
}

function getDifficultyColor(diff: number) {
  switch (diff) {
    case 0: return 'bg-sky-50 dark:bg-sky-950/40 text-sky-700 dark:text-sky-300 border-sky-200 dark:border-sky-800'
    case 1: return 'bg-brand-50 dark:bg-brand-950/40 text-brand-700 dark:text-brand-300 border-brand-200 dark:border-brand-800'
    case 2: return 'bg-purple-50 dark:bg-purple-950/40 text-purple-700 dark:text-purple-300 border-purple-200 dark:border-purple-800'
    default: return 'bg-slate-50 dark:bg-slate-900 text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-800'
  }
}

function navigateToDay(dayOrder: number) {
  router.push(`/today?day=${dayOrder}`)
}
</script>

<template>
  <div class="max-w-6xl mx-auto px-3 sm:px-6 py-5 sm:py-8 space-y-6 sm:space-y-8 animate-in fade-in duration-300">
    <!-- Header Banner -->
    <div class="p-4 sm:p-8 rounded-3xl bg-gradient-to-br from-indigo-50/80 via-white to-brand-50/50 dark:from-slate-900 dark:via-slate-900 dark:to-brand-950 border border-slate-200/90 dark:border-slate-800 text-slate-900 dark:text-white shadow-md dark:shadow-xl relative overflow-hidden transition-all duration-300">
      <div class="absolute -right-10 -bottom-10 w-64 h-64 bg-brand-500/10 rounded-full blur-3xl pointer-events-none"></div>
      
      <div class="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-5 sm:gap-6">
        <div class="space-y-2 max-w-2xl">
          <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-brand-50 dark:bg-brand-500/20 border border-brand-200 dark:border-brand-500/30 text-brand-700 dark:text-brand-300 text-xs font-bold tracking-wide uppercase">
            <Map class="w-3.5 h-3.5" />
            <span>{{ $t('roadmap.badge') }}</span>
          </div>
          <h1 class="text-xl sm:text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white">
            {{ $t('roadmap.title') }}
          </h1>
          <p class="text-slate-600 dark:text-slate-300 text-xs sm:text-base leading-relaxed">
            {{ $t('roadmap.subtitle') }}
          </p>
        </div>

        <!-- Metric Counter Card -->
        <div class="flex items-center gap-3.5 sm:gap-4 bg-white/90 dark:bg-slate-800/80 backdrop-blur-md p-3.5 sm:p-5 rounded-2xl border border-slate-200/80 dark:border-slate-700/80 shadow-sm shrink-0">
          <div class="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-brand-50 dark:bg-brand-500/20 border border-brand-200 dark:border-brand-500/30 flex items-center justify-center text-brand-600 dark:text-brand-400 shrink-0">
            <Award class="w-5 h-5 sm:w-6 sm:h-6" />
          </div>
          <div>
            <div class="text-[11px] sm:text-xs text-slate-500 dark:text-slate-400 uppercase tracking-wider font-semibold">
              {{ $t('roadmap.progress') }}
            </div>
            <div class="text-xl sm:text-2xl font-black text-slate-900 dark:text-white flex items-baseline gap-1.5">
              <span>{{ roadmapStore.roadmapData?.completedDaysCount ?? 0 }}</span>
              <span class="text-xs text-slate-500 dark:text-slate-400 font-medium">/ 30 {{ $t('roadmap.days') }}</span>
            </div>
            <div class="text-xs text-brand-600 dark:text-brand-400 font-bold mt-0.5">
              {{ roadmapStore.roadmapData?.overallProgressPercentage ?? 0 }}% {{ $t('roadmap.completed') }}
            </div>
          </div>
        </div>
      </div>

      <!-- Global Progress Bar -->
      <div class="mt-5 sm:mt-6 space-y-1.5">
        <div class="w-full h-2.5 bg-slate-200/80 dark:bg-slate-800 rounded-full overflow-hidden p-0.5 border border-slate-200 dark:border-slate-700/50">
          <div
            class="h-full bg-gradient-to-r from-brand-500 to-emerald-400 rounded-full transition-all duration-500 shadow-sm"
            :style="{ width: `${roadmapStore.roadmapData?.overallProgressPercentage ?? 0}%` }"
          ></div>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="roadmapStore.isLoading" class="flex flex-col items-center justify-center py-20 space-y-4">
      <div class="w-10 h-10 border-4 border-brand-500 border-t-transparent rounded-full animate-spin"></div>
      <p class="text-slate-500 dark:text-slate-400 text-sm font-medium">{{ $t('roadmap.loading') }}</p>
    </div>

    <!-- Modules List -->
    <div v-else-if="roadmapStore.roadmapData" class="space-y-8 sm:space-y-10">
      <section
        v-for="module in roadmapStore.roadmapData.modules"
        :key="module.category"
        class="space-y-4 sm:space-y-5"
      >
        <!-- Module Header Card -->
        <div class="flex flex-col sm:flex-row sm:items-center justify-between p-4 sm:p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm gap-3 sm:gap-4">
          <div class="flex items-center gap-3 sm:gap-3.5">
            <div class="w-9 h-9 sm:w-10 sm:h-10 rounded-xl bg-brand-50 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800 flex items-center justify-center text-brand-600 dark:text-brand-400 shrink-0">
              <component :is="getModuleIcon(module.category)" class="w-5 h-5" />
            </div>
            <div>
              <div class="flex items-center gap-2 flex-wrap">
                <h2 class="text-sm sm:text-lg font-bold text-slate-900 dark:text-slate-100">
                  {{ module.moduleTitle }}
                </h2>
                <span class="text-[11px] sm:text-xs px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 font-semibold">
                  Days {{ module.startDay }}–{{ module.endDay }}
                </span>
              </div>
              <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 line-clamp-1 mt-0.5">
                {{ module.description }}
              </p>
            </div>
          </div>

          <!-- Module Progress -->
          <div class="flex items-center gap-3 shrink-0 self-end sm:self-auto">
            <span class="text-xs font-bold text-slate-600 dark:text-slate-400">
              {{ module.completedCount }}/{{ module.totalCount }} {{ $t('roadmap.completed') }}
            </span>
            <div class="w-16 sm:w-20 h-2 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
              <div
                class="h-full bg-brand-500 rounded-full transition-all duration-300"
                :style="{ width: `${(module.completedCount / module.totalCount) * 100}%` }"
              ></div>
            </div>
          </div>
        </div>

        <!-- Day Nodes Grid -->
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <div
            v-for="day in module.days"
            :key="day.dayOrder"
            @click="navigateToDay(day.dayOrder)"
            :class="[
              'p-4 sm:p-5 rounded-2xl border transition-all duration-200 cursor-pointer flex flex-col justify-between group relative overflow-hidden select-none',
              day.isActiveToday
                ? 'bg-amber-500/5 dark:bg-amber-500/10 border-amber-500 dark:border-amber-400/80 shadow-md ring-2 ring-amber-500/20'
                : day.isCompleted
                  ? 'bg-white dark:bg-slate-900/90 border-emerald-500/30 dark:border-emerald-500/30 hover:border-emerald-500 hover:shadow-sm'
                  : day.isUnlocked
                    ? 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 hover:border-brand-500 hover:shadow-sm'
                    : 'bg-slate-50/70 dark:bg-slate-950/40 border-slate-200/80 dark:border-slate-800/60 opacity-75 hover:opacity-100'
            ]"
          >
            <!-- Top indicator & Badges -->
            <div class="flex items-start justify-between gap-2 mb-3">
              <div class="flex items-center gap-2">
                <span
                  :class="[
                    'w-7 h-7 rounded-lg text-xs font-black flex items-center justify-center shrink-0 transition-colors',
                    day.isActiveToday
                      ? 'bg-amber-500 text-white shadow-sm'
                      : day.isCompleted
                        ? 'bg-emerald-500 text-white'
                        : 'bg-slate-200 dark:bg-slate-800 text-slate-700 dark:text-slate-300'
                  ]"
                >
                  {{ day.dayOrder }}
                </span>

                <span
                  :class="[
                    'text-[10px] px-2 py-0.5 rounded-md font-bold uppercase tracking-wider border',
                    getDifficultyColor(day.difficulty)
                  ]"
                >
                  {{ getDifficultyLabel(day.difficulty) }}
                </span>
              </div>

              <!-- Status Icon Badge -->
              <div>
                <span
                  v-if="day.isActiveToday"
                  class="flex items-center gap-1 text-[11px] font-bold text-amber-600 dark:text-amber-400 bg-amber-100 dark:bg-amber-950/60 px-2 py-0.5 rounded-full border border-amber-300 dark:border-amber-700 animate-pulse"
                >
                  <Flame class="w-3.5 h-3.5 text-amber-500" />
                  <span>{{ $t('roadmap.today') }}</span>
                </span>
                <span
                  v-else-if="day.isCompleted"
                  class="flex items-center gap-1 text-[11px] font-bold text-emerald-600 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-950/60 px-2 py-0.5 rounded-full border border-emerald-200 dark:border-emerald-800"
                >
                  <CheckCircle2 class="w-3.5 h-3.5 text-emerald-500" />
                  <span>{{ day.drillScore !== null ? `+${day.drillScore}` : 'Pass' }}</span>
                </span>
                <span
                  v-else-if="day.isUnlocked"
                  class="text-[11px] font-semibold text-slate-500 dark:text-slate-400 bg-slate-100 dark:bg-slate-800 px-2 py-0.5 rounded-full"
                >
                  <Eye class="w-3.5 h-3.5 inline mr-0.5" />
                  <span>{{ $t('roadmap.ready') }}</span>
                </span>
                <span
                  v-else
                  class="text-[11px] font-semibold text-slate-400 dark:text-slate-600 bg-slate-100/50 dark:bg-slate-800/40 px-2 py-0.5 rounded-full"
                >
                  <Lock class="w-3 h-3 inline mr-0.5" />
                  <span>{{ $t('roadmap.locked') }}</span>
                </span>
              </div>
            </div>

            <!-- Title & Summary -->
            <div class="space-y-1.5 mb-4">
              <h3 class="font-bold text-sm sm:text-base text-slate-900 dark:text-slate-100 group-hover:text-brand-600 dark:group-hover:text-brand-400 transition-colors line-clamp-1">
                {{ day.title }}
              </h3>
              <p class="text-xs text-slate-600 dark:text-slate-400 line-clamp-2 leading-relaxed">
                {{ day.summary }}
              </p>
            </div>

            <!-- Action Link -->
            <div class="pt-2 border-t border-slate-100 dark:border-slate-800/80 flex items-center justify-between text-xs font-semibold">
              <span
                :class="[
                  day.isActiveToday
                    ? 'text-amber-600 dark:text-amber-400 font-bold'
                    : day.isCompleted
                      ? 'text-emerald-600 dark:text-emerald-400'
                      : 'text-slate-500 dark:text-slate-400 group-hover:text-brand-600 dark:group-hover:text-brand-400'
                ]"
              >
                {{ day.isActiveToday ? $t('roadmap.start_today') : day.isCompleted ? $t('roadmap.review_day') : $t('roadmap.view_lesson') }}
              </span>
              <ArrowRight class="w-3.5 h-3.5 text-slate-400 group-hover:text-brand-500 group-hover:translate-x-0.5 transition-transform" />
            </div>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>
