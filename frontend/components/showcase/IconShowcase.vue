<script setup lang="ts">
import {
  GraduationCap,
  BookOpen,
  Library,
  Brain,
  Lightbulb,
  Target,
  Trophy,
  Award,
  Flame,
  Zap,
  Timer,
  Clock,
  Calendar,
  Repeat,
  Bookmark,
  FileText,
  Pencil,
  Clipboard,
  Play,
  BarChart3,
  TrendingUp,
  Rocket,
  Sparkles,
  Check,
} from 'lucide-vue-next'
import { ref } from 'vue'

interface IconDef {
  name: string
  icon: typeof GraduationCap
  vn: string
}

const iconCategories: { group: string; icons: IconDef[] }[] = [
  {
    group: 'Learning & Reading',
    icons: [
      { name: 'GraduationCap', icon: GraduationCap, vn: 'Học tập' },
      { name: 'BookOpen', icon: BookOpen, vn: 'Đọc' },
      { name: 'Library', icon: Library, vn: 'Thư viện' },
      { name: 'Brain', icon: Brain, vn: 'Tư duy' },
      { name: 'Lightbulb', icon: Lightbulb, vn: 'Insight' },
    ],
  },
  {
    group: 'Goals & Mastery',
    icons: [
      { name: 'Target', icon: Target, vn: 'Mục tiêu' },
      { name: 'Trophy', icon: Trophy, vn: 'Thành tựu' },
      { name: 'Award', icon: Award, vn: 'Huy hiệu' },
      { name: 'Flame', icon: Flame, vn: 'Streak' },
      { name: 'Zap', icon: Zap, vn: 'Năng lượng' },
    ],
  },
  {
    group: 'Time & Review',
    icons: [
      { name: 'Timer', icon: Timer, vn: 'Tập trung' },
      { name: 'Clock', icon: Clock, vn: 'Thời gian' },
      { name: 'Calendar', icon: Calendar, vn: 'Lịch' },
      { name: 'Repeat', icon: Repeat, vn: 'Ôn tập SM-2' },
      { name: 'Bookmark', icon: Bookmark, vn: 'Lưu trữ' },
    ],
  },
  {
    group: 'Notes & Code',
    icons: [
      { name: 'FileText', icon: FileText, vn: 'Ghi chú' },
      { name: 'Pencil', icon: Pencil, vn: 'Viết' },
      { name: 'Clipboard', icon: Clipboard, vn: 'Nhiệm vụ' },
      { name: 'Play', icon: Play, vn: 'Bắt đầu' },
    ],
  },
  {
    group: 'Telemetry & AI',
    icons: [
      { name: 'BarChart3', icon: BarChart3, vn: 'Tiến độ' },
      { name: 'TrendingUp', icon: TrendingUp, vn: 'Phát triển' },
      { name: 'Rocket', icon: Rocket, vn: 'Khởi động' },
      { name: 'Sparkles', icon: Sparkles, vn: 'AI Curation' },
      { name: 'Check', icon: Check, vn: 'Hoàn thành' },
    ],
  },
]

const totalIcons = iconCategories.reduce((sum, cat) => sum + cat.icons.length, 0)

const showToast = ref(false)
let toastTimer: ReturnType<typeof setTimeout> | undefined

function copyToken(value: string) {
  if (typeof navigator !== 'undefined' && navigator.clipboard) {
    navigator.clipboard.writeText(value)
  }
  showToast.value = true
  clearTimeout(toastTimer)
  toastTimer = setTimeout(() => (showToast.value = false), 1800)
}

function handleCopy(name: string) {
  copyToken(`<${name} :size="20" :stroke-width="1.5" />`)
}
</script>

<template>
  <div class="icon-showcase space-y-5">
    <!-- Spec bar -->
    <div class="flex flex-wrap items-center gap-3 sm:gap-4 p-3.5 sm:p-4 rounded-xl bg-slate-100 dark:bg-canvas-elevated border border-slate-200/80 dark:border-white/[0.08] text-xs text-slate-600 dark:text-slate-400">
      <div class="flex items-center gap-1.5"><strong class="text-slate-900 dark:text-white">Library:</strong> Lucide Vue Next</div>
      <div class="w-px h-3.5 bg-slate-300 dark:bg-white/[0.1] hidden sm:block" />
      <div class="flex items-center gap-1.5"><strong class="text-slate-900 dark:text-white">Size:</strong> <code class="px-1.5 py-0.5 rounded font-mono text-[11px] bg-brand-500/10 text-brand-600 dark:text-brand-300 border border-brand-500/20">20px / 18px</code></div>
      <div class="w-px h-3.5 bg-slate-300 dark:bg-white/[0.1] hidden sm:block" />
      <div class="flex items-center gap-1.5"><strong class="text-slate-900 dark:text-white">Stroke:</strong> <code class="px-1.5 py-0.5 rounded font-mono text-[11px] bg-brand-500/10 text-brand-600 dark:text-brand-300 border border-brand-500/20">1.5px round</code></div>
      <div class="w-px h-3.5 bg-slate-300 dark:bg-white/[0.1] hidden sm:block" />
      <div class="flex items-center gap-1.5"><strong class="text-slate-900 dark:text-white">Color:</strong> <code class="px-1.5 py-0.5 rounded font-mono text-[11px] bg-brand-500/10 text-brand-600 dark:text-brand-300 border border-brand-500/20">text-brand-400</code></div>
      <div class="w-px h-3.5 bg-slate-300 dark:bg-white/[0.1] hidden sm:block" />
      <div class="flex items-center gap-1.5"><strong class="text-slate-900 dark:text-white">Total:</strong> {{ totalIcons }} icons</div>
    </div>

    <!-- Category groups -->
    <div v-for="cat in iconCategories" :key="cat.group" class="space-y-3">
      <div class="flex items-center gap-2.5">
        <h4 class="text-xs sm:text-sm font-semibold text-slate-800 dark:text-slate-200">{{ cat.group }}</h4>
        <span class="px-1.5 py-0.5 rounded text-[10px] font-mono text-slate-500 dark:text-slate-400 bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08]">{{ cat.icons.length }}</span>
        <div class="flex-1 h-px bg-slate-200/80 dark:border-white/[0.08]" />
      </div>

      <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-2.5">
        <button
          v-for="item in cat.icons"
          :key="item.name"
          type="button"
          class="flex flex-col items-center gap-2 p-3 sm:p-4 rounded-xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle hover:border-brand-500/40 hover:shadow-md hover:shadow-brand-500/5 transition-all text-center group cursor-pointer"
          :title="`Click to copy <${item.name} />`"
          @click="handleCopy(item.name)"
        >
          <div class="w-9 h-9 rounded-xl bg-brand-500/10 dark:bg-brand-500/15 border border-brand-500/20 text-brand-600 dark:text-brand-400 flex items-center justify-center group-hover:scale-110 transition-transform">
            <component :is="item.icon" :size="20" :stroke-width="1.5" />
          </div>
          <span class="font-mono text-xs font-semibold text-slate-800 dark:text-slate-200 group-hover:text-brand-400 transition-colors">{{ item.name }}</span>
          <span class="text-[11px] text-slate-400">{{ item.vn }}</span>
        </button>
      </div>
    </div>

    <!-- Toast notification -->
    <Transition name="fade">
      <div v-if="showToast" class="fixed bottom-6 left-1/2 -translate-x-1/2 z-50 flex items-center gap-2 px-4 py-2 rounded-xl bg-slate-900 text-white text-xs font-semibold shadow-2xl border border-white/10">
        <Check :size="16" :stroke-width="1.5" class="text-emerald-400" />
        <span>Đã copy code component vào clipboard!</span>
      </div>
    </Transition>
  </div>
</template>
