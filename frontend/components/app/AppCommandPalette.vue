<script setup lang="ts">
import { ref, computed, watch, nextTick, onMounted, onUnmounted } from 'vue'
import { useCommandPalette } from '~/composables/useCommandPalette'
import {
  Search,
  LayoutGrid,
  Target,
  Map,
  HelpCircle,
  Layers,
  Sparkles,
  BookOpen,
  Highlighter,
  Network,
  User,
  Settings,
  CornerDownLeft,
  X
} from 'lucide-vue-next'

const router = useRouter()
const { isOpen, close } = useCommandPalette()

const searchQuery = ref('')
const searchInputRef = ref<HTMLInputElement | null>(null)
const selectedIndex = ref(0)

interface CommandItem {
  id: string
  titleKey: string
  descKey: string
  path: string
  icon: any
  category: 'practice' | 'knowledge' | 'account'
  keywords: string[]
}

const commandItems: CommandItem[] = [
  {
    id: 'dashboard',
    titleKey: 'command_palette.action_dashboard',
    descKey: 'command_palette.action_dashboard_desc',
    path: '/',
    icon: LayoutGrid,
    category: 'practice',
    keywords: ['dashboard', 'home', 'overview', 'trang chu', 'tong quan', 'metrics']
  },
  {
    id: 'today',
    titleKey: 'command_palette.action_today',
    descKey: 'command_palette.action_today_desc',
    path: '/today',
    icon: Target,
    category: 'practice',
    keywords: ['today', 'studio', 'slice', 'reading', 'focus', 'daily', 'bai doc', 'hom nay']
  },
  {
    id: 'roadmap',
    titleKey: 'command_palette.action_roadmap',
    descKey: 'command_palette.action_roadmap_desc',
    path: '/roadmap',
    icon: Map,
    category: 'practice',
    keywords: ['roadmap', 'mindmap', 'track', 'pillars', 'lo trinh', 'so do']
  },
  {
    id: 'quiz',
    titleKey: 'command_palette.action_quiz',
    descKey: 'command_palette.action_quiz_desc',
    path: '/quiz',
    icon: HelpCircle,
    category: 'practice',
    keywords: ['quiz', 'interview', 'scenario', 'questions', 'trac nghiem', 'phong van']
  },
  {
    id: 'review',
    titleKey: 'command_palette.action_review',
    descKey: 'command_palette.action_review_desc',
    path: '/review',
    icon: Layers,
    category: 'practice',
    keywords: ['review', 'flashcard', 'sm2', 'retention', 'memory', 'the ghi nho', 'on tap']
  },
  {
    id: 'insights',
    titleKey: 'nav.insights',
    descKey: 'command_palette.action_graph_desc',
    path: '/insights',
    icon: Sparkles,
    category: 'knowledge',
    keywords: ['insights', 'radar', 'stats', 'thong ke']
  },
  {
    id: 'library',
    titleKey: 'command_palette.action_library',
    descKey: 'command_palette.action_library_desc',
    path: '/library',
    icon: BookOpen,
    category: 'knowledge',
    keywords: ['library', 'books', 'documents', 'crawler', 'pdf', 'thu vien', 'sach']
  },
  {
    id: 'notes',
    titleKey: 'command_palette.action_notes',
    descKey: 'command_palette.action_notes_desc',
    path: '/notes',
    icon: Highlighter,
    category: 'knowledge',
    keywords: ['notes', 'highlights', 'bookmarks', 'tags', 'ghi chu', 'trich doan']
  },
  {
    id: 'graph',
    titleKey: 'command_palette.action_graph',
    descKey: 'command_palette.action_graph_desc',
    path: '/graph',
    icon: Network,
    category: 'knowledge',
    keywords: ['graph', '3d', '2d', 'cosmos', 'network', 'so do tri thuc', 'do thi']
  },
  {
    id: 'profile',
    titleKey: 'command_palette.action_profile',
    descKey: 'command_palette.action_profile_desc',
    path: '/profile',
    icon: User,
    category: 'account',
    keywords: ['profile', 'user', 'streak', 'goal', 'ho so', 'ca nhan']
  },
  {
    id: 'settings',
    titleKey: 'command_palette.action_settings',
    descKey: 'command_palette.action_settings_desc',
    path: '/settings',
    icon: Settings,
    category: 'account',
    keywords: ['settings', 'notifications', 'timezone', 'theme', 'cai dat', 'thong bao']
  }
]

const { t } = useI18n()

const filteredItems = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()
  if (!query) return commandItems

  return commandItems.filter(item => {
    const title = t(item.titleKey).toLowerCase()
    const desc = t(item.descKey).toLowerCase()
    const matchTitle = title.includes(query)
    const matchDesc = desc.includes(query)
    const matchKeyword = item.keywords.some(k => k.toLowerCase().includes(query))
    return matchTitle || matchDesc || matchKeyword
  })
})

function selectItem(item: CommandItem) {
  close()
  router.push(item.path)
}

function handleKeydown(e: KeyboardEvent) {
  if (!isOpen.value) {
    if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
      e.preventDefault()
      isOpen.value = true
    }
    return
  }

  if (e.key === 'Escape') {
    e.preventDefault()
    close()
    return
  }

  if (e.key === 'ArrowDown') {
    e.preventDefault()
    if (filteredItems.value.length > 0) {
      selectedIndex.value = (selectedIndex.value + 1) % filteredItems.value.length
    }
    return
  }

  if (e.key === 'ArrowUp') {
    e.preventDefault()
    if (filteredItems.value.length > 0) {
      selectedIndex.value = (selectedIndex.value - 1 + filteredItems.value.length) % filteredItems.value.length
    }
    return
  }

  if (e.key === 'Enter') {
    e.preventDefault()
    const item = filteredItems.value[selectedIndex.value]
    if (item) {
      selectItem(item)
    }
  }
}

watch(isOpen, async (val) => {
  if (val) {
    searchQuery.value = ''
    selectedIndex.value = 0
    if (typeof document !== 'undefined') {
      document.body.style.overflow = 'hidden'
    }
    await nextTick()
    searchInputRef.value?.focus()
  } else {
    if (typeof document !== 'undefined') {
      document.body.style.overflow = ''
    }
  }
})

watch(filteredItems, () => {
  selectedIndex.value = 0
})

onMounted(() => {
  if (typeof window !== 'undefined') {
    window.addEventListener('keydown', handleKeydown)
  }
})

onUnmounted(() => {
  if (typeof window !== 'undefined') {
    window.removeEventListener('keydown', handleKeydown)
    if (typeof document !== 'undefined') {
      document.body.style.overflow = ''
    }
  }
})
</script>

<template>
  <Teleport to="body">
    <div
      v-if="isOpen"
      class="fixed inset-0 z-60 flex items-start justify-center pt-16 sm:pt-24 px-4 bg-slate-950/60 dark:bg-black/80 backdrop-blur-sm transition-opacity"
      @click.self="close"
    >
      <div
        class="w-full max-w-2xl bg-white dark:bg-canvas-elevated border border-slate-200 dark:border-white/[0.1] rounded-2xl shadow-2xl overflow-hidden flex flex-col max-h-[80vh] animate-in fade-in zoom-in-95 duration-150"
      >
        <!-- Search Input Bar -->
        <div class="flex items-center gap-3 px-4 py-3.5 border-b border-slate-200 dark:border-white/[0.08] bg-slate-50/50 dark:bg-canvas-subtle/50">
          <Search class="w-5 h-5 text-brand-500 shrink-0" />
          <input
            ref="searchInputRef"
            v-model="searchQuery"
            type="text"
            :placeholder="$t('command_palette.search_placeholder')"
            class="flex-1 bg-transparent text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 text-base focus:outline-none"
            aria-label="Command search"
          />
          <button
            v-if="searchQuery"
            @click="searchQuery = ''"
            class="p-1 rounded-md text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
            title="Clear search"
          >
            <X class="w-4 h-4" />
          </button>
          <kbd class="hidden sm:inline-flex items-center gap-0.5 px-2 py-0.5 rounded text-[11px] font-mono font-medium text-slate-500 dark:text-slate-400 bg-slate-200/80 dark:bg-white/10 border border-slate-300 dark:border-white/[0.08]">
            ESC
          </kbd>
        </div>

        <!-- Results List -->
        <div class="overflow-y-auto p-2 space-y-1 divide-y divide-transparent">
          <div v-if="filteredItems.length === 0" class="py-12 text-center text-slate-500 dark:text-slate-400 text-sm">
            {{ $t('command_palette.no_results') }}
          </div>

          <div
            v-for="(item, idx) in filteredItems"
            :key="item.id"
            @click="selectItem(item)"
            @mouseenter="selectedIndex = idx"
            :class="[
              'flex items-center justify-between gap-3 px-3.5 py-2.5 rounded-xl cursor-pointer transition-all text-sm',
              selectedIndex === idx
                ? 'bg-brand-500/10 dark:bg-brand-500/15 text-brand-600 dark:text-brand-300'
                : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.04]'
            ]"
          >
            <div class="flex items-center gap-3 min-w-0">
              <div
                :class="[
                  'w-8 h-8 rounded-lg flex items-center justify-center shrink-0 transition-colors',
                  selectedIndex === idx
                    ? 'bg-brand-500 text-white shadow-sm shadow-brand-500/30'
                    : 'bg-slate-100 dark:bg-white/[0.06] text-slate-500 dark:text-slate-400'
                ]"
              >
                <component :is="item.icon" class="w-4 h-4" />
              </div>
              <div class="min-w-0">
                <div class="font-semibold truncate">{{ $t(item.titleKey) }}</div>
                <div class="text-xs text-slate-500 dark:text-slate-400 truncate">{{ $t(item.descKey) }}</div>
              </div>
            </div>

            <div class="flex items-center gap-2 shrink-0">
              <span
                v-if="selectedIndex === idx"
                class="hidden sm:inline-flex items-center gap-1 text-xs font-medium text-brand-600 dark:text-brand-400"
              >
                {{ $t('command_palette.hint_select') }}
                <CornerDownLeft class="w-3.5 h-3.5" />
              </span>
            </div>
          </div>
        </div>

        <!-- Footer Navigation Hints -->
        <div class="px-4 py-2.5 border-t border-slate-200 dark:border-white/[0.08] bg-slate-50/70 dark:bg-canvas-subtle/80 flex items-center justify-between text-xs text-slate-500 dark:text-slate-400 select-none">
          <div class="flex items-center gap-4">
            <span class="inline-flex items-center gap-1">
              <kbd class="px-1.5 py-0.5 rounded font-mono bg-slate-200 dark:bg-white/10 text-[10px]">↑↓</kbd>
              {{ $t('command_palette.hint_navigate') }}
            </span>
            <span class="inline-flex items-center gap-1">
              <kbd class="px-1.5 py-0.5 rounded font-mono bg-slate-200 dark:bg-white/10 text-[10px]">↵</kbd>
              {{ $t('command_palette.hint_select') }}
            </span>
            <span class="inline-flex items-center gap-1">
              <kbd class="px-1.5 py-0.5 rounded font-mono bg-slate-200 dark:bg-white/10 text-[10px]">ESC</kbd>
              {{ $t('command_palette.hint_close') }}
            </span>
          </div>

          <div class="flex items-center gap-1.5 font-semibold text-brand-500">
            <span class="w-1.5 h-1.5 rounded-full bg-brand-500 animate-pulse"></span>
            TechDaily Studio
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>
