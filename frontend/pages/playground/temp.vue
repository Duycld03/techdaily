<script setup lang="ts">
import { ref, computed } from 'vue'
import {
  BookOpen,
  Search,
  Plus,
  ExternalLink,
  Layers,
  X,
  FileText,
  Bookmark,
  Trash2,
  Download,
  Flame,
  Clock,
  Zap,
  Award,
  TrendingUp,
  BarChart3,
  SlidersHorizontal,
  Check,
  Sparkles,
  Shuffle,
  ChevronLeft,
  ChevronRight,
  BookmarkCheck,
  CheckCircle2,
  XCircle,
  Cpu,
  Brain,
  Lightbulb,
  FileCode2,
  Sun,
  Moon,
  Library,
  GraduationCap,
  FileUp,
  Globe,
  UploadCloud,
  Loader2
} from 'lucide-vue-next'
import BoardLayout from '~/components/layout/BoardLayout.vue'
import CommonShikiCodeBlock from '~/components/common/ShikiCodeBlock.vue'
import AppModal from '~/components/ui/AppModal.vue'
import AppSelect from '~/components/common/AppSelect.vue'
useHead({
  title: 'Phase 1 UI Preview — TechDaily Playground'
})

const colorMode = useColorMode()

function toggleDarkMode() {
  colorMode.preference = colorMode.value === 'dark' ? 'light' : 'dark'
}

// Active view in the playground
type PlaygroundView = 'library' | 'review' | 'insights'
const activeView = ref<PlaygroundView>('insights')

// =============================================================================
// VIEW 1: LIBRARY MOCK STATE
// =============================================================================
const librarySearch = ref('')
const selectedLibCategory = ref<number | null>(null)
// =============================================================================
// MODAL IMPORT DOCUMENT MOCK STATE
// =============================================================================
const isImportModalOpen = ref(false)
const modalActiveTab = ref<'markdown' | 'pdf' | 'url'>('markdown')
const importTitle = ref('Designing Data-Intensive Applications — Chapter 5')
const importCategory = ref(3)
const importSourceUrl = ref('https://github.com/ept/ddia-references')
const importContent = ref(`# Chapter 5: Replication\n\nReplication means keeping a copy of the same data on multiple machines that are connected via a network.\n\n## Leaders and Followers\n\nEach node that stores a copy of the database is called a replica. With multiple replicas, a question arises: how do we ensure that all the data ends up on all the replicas?\n\nEvery write to the database needs to be processed by every replica; otherwise, the replicas would no longer contain the same data. The most common solution for this is called *leader-based replication* (also known as *active/passive* or *master-slave* replication).`)
const pdfFile = ref<{ name: string; size: number } | null>({
  name: 'Designing_Data_Intensive_Applications.pdf',
  size: 24.5 * 1024 * 1024
})
const isDraggingPdf = ref(false)
const isProcessingPdf = ref(false)
const pdfProgress = ref(68)
const pdfTitle = ref('Designing Data-Intensive Applications (2nd Edition)')
const pdfCategory = ref(3)
const crawlUrlInput = ref('https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview')
const isCrawling = ref(false)
const isPdfDetected = ref(true)
const detectedPdfUrl = ref('https://raw.githubusercontent.com/dotnet/docs/main/docs/architecture/microservices.pdf')

const formCategoryOptions = [
  { value: 0, label: 'Frontend & Web' },
  { value: 1, label: 'Backend & Runtime' },
  { value: 2, label: 'Cơ Sở Dữ Liệu' },
  { value: 3, label: 'Thiết Kế Hệ Thống' }
]
const libraryCategories = [
  { id: null, label: 'Tất Cả' },
  { id: 0, label: 'Frontend & Web' },
  { id: 1, label: 'Backend & Phân Tán' },
  { id: 2, label: 'Cơ Sở Dữ Liệu' },
  { id: 3, label: 'Thiết Kế Hệ Thống' }
]

const mockBooks = [
  {
    id: 'book-1',
    title: 'Designing Data-Intensive Applications',
    author: 'Martin Kleppmann',
    category: 'System Design',
    totalChunks: 24,
    currentSlice: 8,
    status: 'In Progress',
    percent: 33,
    size: '1.2 MB • PDF'
  },
  {
    id: 'book-2',
    title: 'Database Internals: A Deep Dive into Distributed Systems',
    author: 'Alex Petrov',
    category: 'DatabaseStorage',
    totalChunks: 18,
    currentSlice: 0,
    status: 'Ready',
    percent: 0,
    size: '850 KB • Markdown'
  },
  {
    id: 'book-3',
    title: 'Building Microservices: Designing Fine-Grained Systems',
    author: 'Sam Newman',
    category: 'BackendRuntime',
    totalChunks: 22,
    currentSlice: 14,
    status: 'In Progress',
    percent: 64,
    size: '2.1 MB • PDF'
  },
  {
    id: 'book-4',
    title: 'Clean Architecture: A Craftsman\'s Guide to Software Structure',
    author: 'Robert C. Martin',
    category: 'Core Architecture',
    totalChunks: 15,
    currentSlice: 15,
    status: 'Completed',
    percent: 100,
    size: '980 KB • PDF'
  },
  {
    id: 'book-5',
    title: 'PostgreSQL 16 Administration Cookbook',
    author: 'Gianni Ciolli',
    category: 'DatabaseStorage',
    totalChunks: 20,
    currentSlice: 3,
    status: 'In Progress',
    percent: 15,
    size: '1.6 MB • PDF'
  },
  {
    id: 'book-6',
    title: 'Understanding Distributed Systems (2nd Edition)',
    author: 'Roberto Vitillo',
    category: 'System Design',
    totalChunks: 12,
    currentSlice: 0,
    status: 'Ready',
    percent: 0,
    size: '720 KB • PDF'
  }
]

// =============================================================================
// VIEW 2: FLASHCARD DECK MOCK STATE
// =============================================================================
const reviewSearch = ref('')
const selectedQuickFilter = ref<'all' | 'due' | 'mastered'>('all')
const mockFlashcards = [
  {
    id: 'card-1',
    category: 'Database Storage',
    tag: 'PostgreSQL',
    urgency: 'due',
    ef: '2.30',
    interval: '6d',
    question: 'Sự khác nhau cơ bản giữa Optimistic Lock và Pessimistic Lock trong PostgreSQL?',
    answer: 'Optimistic lock dùng xmin/version check khi update; Pessimistic lock dùng SELECT FOR UPDATE khóa cứng hàng dữ liệu.',
    source: 'Database Deep Dive'
  },
  {
    id: 'card-2',
    category: 'Distributed Systems',
    tag: 'Kafka',
    urgency: 'due',
    ef: '2.10',
    interval: '4d',
    question: 'Tại sao Kafka Consumer Group lại xảy ra hiện tượng Stop-the-world Rebalance?',
    answer: 'Khi Eager Rebalance Protocol kích hoạt, toàn bộ consumer phải drop partition và join group lại từ đầu. KIP-429 giải quyết bằng Cooperative Sticky Rebalance.',
    source: 'Kafka Architecture Guide'
  },
  {
    id: 'card-3',
    category: 'Backend & Runtime',
    tag: '.NET 10',
    urgency: 'mastered',
    ef: '2.50',
    interval: '28d',
    question: 'Cơ chế hoạt động của ThreadPool Work-Stealing Queue trong .NET Runtime?',
    answer: 'Mỗi thread có một Local LIFO WorkQueue, các thread nhàn rỗi sẽ dùng FIFO algorithm để steal task từ đuôi queue của thread khác.',
    source: 'CLR via C#'
  },
  {
    id: 'card-4',
    category: 'System Design',
    tag: 'Cache',
    urgency: 'due',
    ef: '2.40',
    interval: '12d',
    question: 'Chiến lược ngăn chặn Cache Stampede (Thundering Herd) hiệu quả nhất?',
    answer: 'Sử dụng Mutex Distributed Lock (Redlock) hoặc Probabilistic Early Expiration (XFetch algorithm) để tái tính toán cache trước khi hết hạn.',
    source: 'System Design Interview'
  },
  {
    id: 'card-5',
    category: 'Core Architecture',
    tag: 'DDD',
    urgency: 'mastered',
    ef: '2.45',
    interval: '21d',
    question: 'Quy tắc vàng khi thiết kế Aggregate Root trong Domain-Driven Design?',
    answer: 'Một Aggregate Root chỉ được tham chiếu đến Aggregate Root khác thông qua Identity (ID), không bao giờ giữ Direct Object Reference.',
    source: 'Domain-Driven Design Reference'
  },
  {
    id: 'card-6',
    category: 'Frontend & Web',
    tag: 'Vue 3',
    urgency: 'mastered',
    ef: '2.50',
    interval: '35d',
    question: 'Sự khác biệt cốt lõi giữa shallowRef và ref trong Vue 3 Reactivity Engine?',
    answer: 'ref bọc toàn bộ object trong recursive Reactive Proxy; shallowRef chỉ kích hoạt reactivity khi giá trị .value được gán lại, giúp tối ưu data lớn.',
    source: 'Vue 3 Deep Dive'
  }
]

const filteredFlashcards = computed(() => {
  return mockFlashcards.filter((card) => {
    if (selectedQuickFilter.value === 'due' && card.urgency !== 'due') return false
    if (selectedQuickFilter.value === 'mastered' && card.urgency !== 'mastered') return false
    if (reviewSearch.value) {
      const q = reviewSearch.value.toLowerCase()
      return card.question.toLowerCase().includes(q) || card.answer.toLowerCase().includes(q)
    }
    return true
  })
})

// =============================================================================
// VIEW 3: ARCHITECTURAL INSIGHTS MOCK STATE
// =============================================================================
const insightsViewMode = ref<'explore' | 'saved'>('explore')
const selectedInsightCat = ref<number | null>(null)
const activeCodeTab = ref<'solution' | 'problem'>('solution')
const isBookmarked = ref(false)
const bookmarkCount = ref(4)

function toggleBookmark() {
  isBookmarked.value = !isBookmarked.value
  bookmarkCount.value += isBookmarked.value ? 1 : -1
}

const solutionSql = `-- ✅ SENIOR PATTERN: Tối ưu hóa Visibility Map và Autovacuum Tunings cho Workload
-- 1. Đảm bảo tất cả tuple trong trang dữ liệu đều đã được freeze/visible cho mọi transaction
VACUUM ANALYZE users;

-- 2. Tinh chỉnh autovacuum cực đoan cho bảng có tần suất UPDATE cao
ALTER TABLE users SET (
    autovacuum_vacuum_scale_factor = 0.05,
    autovacuum_vacuum_threshold = 1000,
    autovacuum_analyze_scale_factor = 0.02
);

-- 3. Sử dụng câu lệnh truy vấn đảm bảo tận dụng Index-Only Scan 100%
EXPLAIN (ANALYZE, BUFFERS)
SELECT email, status FROM users WHERE email = 'test@example.com';`

const problemSql = `-- ❌ ANTI-PATTERN: Thiếu autovacuum tuning dẫn đến Visibility Map bẩn
-- Query này tưởng chừng là Index-Only Scan nhưng thực tế vẫn đọc ngẫu nhiên Heap Disk Blocks
-- Khi đó số lượng Heap Fetches tăng vọt, tiêu tốn Disk I/O ngẫu nhiên.
EXPLAIN (ANALYZE, BUFFERS)
SELECT email, status FROM users WHERE email = 'test@example.com';

-- Kết quả EXPLAIN:
-- Heap Fetches: 15420 (Mỗi tuple đều phải truy cập lại Heap để kiểm tra MVCC snapshot)`
</script>

<template>
  <div class="min-h-screen bg-slate-50 dark:bg-canvas text-slate-900 dark:text-slate-100 font-sans antialiased transition-colors duration-200">
    
    <!-- ======================================================================= -->
    <!-- TOP PLAYGROUND NAVIGATION TOOLBAR                                       -->
    <!-- ======================================================================= -->
    <header class="sticky top-0 z-50 bg-white/95 dark:bg-canvas-subtle/90 border-b border-slate-200/90 dark:border-white/[0.08] backdrop-blur-md px-4 py-2.5">
      <div class="max-w-7xl mx-auto flex flex-wrap items-center justify-between gap-3">
        <div class="flex items-center gap-2.5">
          <div class="w-8 h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center font-black text-sm">
            TD
          </div>
          <div>
            <div class="flex items-center gap-2">
              <span class="text-xs font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400">Playground V1</span>
              <span class="px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-slate-100 dark:bg-white/[0.06] text-slate-600 dark:text-slate-300 border border-slate-200 dark:border-white/[0.08]">
                Phase 1 Vue Preview
              </span>
            </div>
            <h1 class="text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200">
              Giao Diện Thật (100% Design System & Tokens)
            </h1>
          </div>
        </div>

        <!-- 3 Tabs Switcher -->
        <div class="flex items-center p-1 bg-slate-100 dark:bg-canvas border border-slate-200/80 dark:border-white/[0.08] rounded-xl text-xs font-semibold">
          <button
            @click="activeView = 'library'"
            :class="[
              'px-3 py-1.5 rounded-lg transition-all cursor-pointer',
              activeView === 'library'
                ? 'bg-brand-600 text-white font-bold shadow-sm'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            ]"
          >
            1. Thư Viện (Library)
          </button>
          <button
            @click="activeView = 'review'"
            :class="[
              'px-3 py-1.5 rounded-lg transition-all cursor-pointer',
              activeView === 'review'
                ? 'bg-brand-600 text-white font-bold shadow-sm'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            ]"
          >
            2. Kho Thẻ (Review Deck)
          </button>
          <button
            @click="activeView = 'insights'"
            :class="[
              'px-3 py-1.5 rounded-lg transition-all cursor-pointer',
              activeView === 'insights'
                ? 'bg-brand-600 text-white font-bold shadow-sm'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            ]"
          >
            3. Kiến Thức (Insights)
          </button>
        </div>

        <!-- Quick Toggles -->
        <div class="flex items-center gap-2">
          <button
            @click="toggleDarkMode"
            class="p-2 rounded-xl border border-slate-200 dark:border-white/[0.08] text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.04] transition-colors cursor-pointer"
            title="Đổi giao diện Sáng / Tối"
          >
            <Sun v-if="colorMode.value === 'dark'" class="w-4 h-4 text-amber-400" />
            <Moon v-else class="w-4 h-4 text-indigo-600" />
          </button>
        </div>
      </div>
    </header>

    <main class="max-w-7xl mx-auto p-4 sm:p-6 lg:p-8">

      <!-- ===================================================================== -->
      <!-- VIEW 1: LIBRARY CATALOG                                               -->
      <!-- ===================================================================== -->
      <section v-if="activeView === 'library'" class="space-y-4">
          <!-- Header Bar -->
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 w-full">
            <div class="flex items-center gap-3">
              <div class="w-9 h-9 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0">
                <BookOpen class="w-5 h-5" :stroke-width="1.5" />
              </div>
              <div>
                <h2 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white flex items-center gap-2">
                  Thư Viện Tài Liệu Kỹ Thuật
                </h2>
                <p class="text-xs text-slate-500 dark:text-slate-400 font-medium">
                  Sách kiến trúc hệ thống và tài liệu tham khảo chính hãng
                </p>
              </div>
            </div>

            <div class="flex items-center gap-3">
              <div class="relative w-full sm:w-72 shrink-0">
                <Search class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
                <input
                  v-model="librarySearch"
                  type="text"
                  placeholder="Tìm kiếm tài liệu, sách kỹ thuật..."
                  class="w-full pl-9 pr-8 py-2 bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] rounded-xl text-xs sm:text-sm text-slate-900 dark:text-slate-200 placeholder-slate-400 focus:outline-none focus:border-brand-500 transition-colors shadow-sm"
                />
              </div>

              <button
                type="button"
                @click="isImportModalOpen = true"
                class="flex items-center gap-2 px-4 py-2 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-xs sm:text-sm transition-all shadow-md shadow-brand-500/20 whitespace-nowrap shrink-0 cursor-pointer"
              >
                <Plus class="w-4 h-4" :stroke-width="2" />
                <span>Nhập Tài Liệu</span>
              </button>
            </div>
          </div>

          <!-- Filters Row -->
          <div class="flex flex-wrap items-center gap-2 w-full py-0.5">
            <button
              v-for="cat in libraryCategories"
              :key="cat.label"
              @click="selectedLibCategory = cat.id"
              :class="[
                'px-3.5 py-1.5 rounded-xl text-xs sm:text-sm border transition-all whitespace-nowrap shrink-0 cursor-pointer',
                selectedLibCategory === cat.id
                  ? 'bg-slate-100 dark:bg-canvas-elevated border-slate-300 dark:border-white/[0.12] text-brand-600 dark:text-brand-400 font-bold shadow-sm'
                  : 'bg-white dark:bg-canvas-subtle border-slate-200/80 dark:border-white/[0.08] text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 font-medium'
              ]"
            >
              {{ cat.label }}
            </button>
          </div>

          <!-- Cards Grid: 3-column directly on canvas -->
          <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
            <div
              v-for="book in mockBooks"
              :key="book.id"
              class="p-5 rounded-2xl border border-slate-200/90 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle hover:border-brand-500/40 transition-all flex flex-col justify-between space-y-4 shadow-sm hover:shadow-md"
            >
              <div class="space-y-2">
                <div class="flex items-center justify-between gap-2">
                  <span class="px-2.5 py-0.5 rounded-md text-[10px] font-bold uppercase tracking-wider bg-brand-500/10 text-brand-600 dark:text-brand-300 border border-brand-500/20 whitespace-nowrap shrink-0">
                    {{ book.category }}
                  </span>
                  <span class="text-xs text-slate-400 font-mono flex items-center gap-1">
                    <Bookmark class="w-3.5 h-3.5 text-brand-400" />
                    {{ book.totalChunks }} Lát Cắt
                  </span>
                </div>

                <h3 class="text-sm font-bold text-slate-900 dark:text-white line-clamp-2 leading-snug">
                  {{ book.title }}
                </h3>

                <p class="text-xs text-slate-500 dark:text-slate-400 font-mono">
                  {{ book.author }}
                </p>
              </div>

              <div class="space-y-2.5 pt-2 border-t border-slate-100 dark:border-white/[0.06]">
                <div class="flex items-center justify-between text-xs text-slate-500 dark:text-slate-400">
                  <span class="inline-flex items-center gap-1.5 font-semibold text-emerald-600 dark:text-emerald-400 text-[11px]">
                    <span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                    {{ book.status }}
                  </span>
                  <span class="font-mono text-[11px]">
                    Lát cắt {{ book.currentSlice }}/{{ book.totalChunks }} ({{ book.percent }}%)
                  </span>
                </div>

                <div class="w-full h-1 bg-slate-100 dark:bg-canvas-elevated rounded-full overflow-hidden">
                  <div class="h-full bg-brand-500 rounded-full transition-all" :style="{ width: `${book.percent}%` }"></div>
                </div>

                <div class="flex items-center justify-between gap-2 pt-1">
                  <span class="text-[11px] text-slate-400 font-mono">{{ book.size }}</span>
                  <a
                    href="#"
                    class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-brand-500/10 text-brand-600 dark:text-brand-300 hover:bg-brand-500/20 border border-brand-500/30 whitespace-nowrap shrink-0 transition-colors"
                  >
                    <GraduationCap class="w-3.5 h-3.5" />
                    <span>{{ book.percent > 0 ? 'Đọc Tiếp' : 'Bắt Đầu Đọc' }}</span>
                    <ChevronRight class="w-3.5 h-3.5" />
                  </a>
                </div>
              </div>
            </div>
          </div>

          <!-- Pagination Row -->
          <div class="flex items-center justify-between w-full pt-2 text-xs text-slate-500 dark:text-slate-400">
            <span>Hiển thị 1 - 6 trong 18 tài liệu</span>
            <div class="flex items-center gap-1">
              <button class="px-2.5 py-1 rounded-lg border border-slate-200 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-canvas-elevated">Trước</button>
              <button class="px-2.5 py-1 rounded-lg bg-brand-600 text-white font-bold">1</button>
              <button class="px-2.5 py-1 rounded-lg border border-slate-200 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-canvas-elevated">2</button>
              <button class="px-2.5 py-1 rounded-lg border border-slate-200 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-canvas-elevated">3</button>
              <button class="px-2.5 py-1 rounded-lg border border-slate-200 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-canvas-elevated">Sau</button>
            </div>
          </div>
      </section>

      <!-- =================================================================== -->
      <!-- PREVIEW: IMPORT DOCUMENT MODAL (AppModal max-w-2xl)                  -->
      <!-- =================================================================== -->
      <AppModal
        :open="isImportModalOpen"
        title="Nhập Tài Liệu Kỹ Thuật"
        max-width="max-w-2xl"
        @close="isImportModalOpen = false"
      >
        <!-- Custom Header with Brand Icon & Subtitle -->
        <template #header>
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0">
              <BookOpen class="w-5 h-5" :stroke-width="1.5" />
            </div>
            <div>
              <h2 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white tracking-tight flex items-center gap-2">
                Nhập Tài Liệu Kỹ Thuật
              </h2>
              <p class="text-xs text-slate-500 dark:text-slate-400 font-medium">
                Hỗ trợ định dạng Markdown, sách PDF kỹ thuật hoặc cào dữ liệu từ URL
              </p>
            </div>
          </div>
        </template>

        <!-- 3-Tab Segmented Selector (Design System Standard) -->
        <div class="p-1 bg-slate-100/90 dark:bg-canvas-subtle rounded-2xl border border-slate-200/80 dark:border-white/[0.08] flex items-center gap-1">
          <button
            type="button"
            @click="modalActiveTab = 'markdown'"
            :class="[
              'flex-1 py-2 px-3 rounded-xl text-xs sm:text-sm font-bold flex items-center justify-center gap-2 transition-all border cursor-pointer whitespace-nowrap',
              modalActiveTab === 'markdown'
                ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm border-slate-200/80 dark:border-white/[0.12]'
                : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white font-medium'
            ]"
          >
            <FileText class="w-4 h-4 shrink-0" />
            <span>Nhập Markdown</span>
          </button>

          <button
            type="button"
            @click="modalActiveTab = 'pdf'"
            :class="[
              'flex-1 py-2 px-3 rounded-xl text-xs sm:text-sm font-bold flex items-center justify-center gap-2 transition-all border cursor-pointer whitespace-nowrap',
              modalActiveTab === 'pdf'
                ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm border-slate-200/80 dark:border-white/[0.12]'
                : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white font-medium'
            ]"
          >
            <FileUp class="w-4 h-4 shrink-0" />
            <span>Tải Lên PDF</span>
          </button>

          <button
            type="button"
            @click="modalActiveTab = 'url'"
            :class="[
              'flex-1 py-2 px-3 rounded-xl text-xs sm:text-sm font-bold flex items-center justify-center gap-2 transition-all border cursor-pointer whitespace-nowrap',
              modalActiveTab === 'url'
                ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm border-slate-200/80 dark:border-white/[0.12]'
                : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white font-medium'
            ]"
          >
            <Globe class="w-4 h-4 shrink-0" />
            <span>Thu Thập URL Web</span>
          </button>
        </div>

        <!-- TAB 1: Markdown Direct Form -->
        <form v-if="modalActiveTab === 'markdown'" @submit.prevent class="space-y-4 pt-1">
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-1.5">
              Tiêu đề tài liệu
            </label>
            <input
              v-model="importTitle"
              type="text"
              placeholder="Ví dụ: Designing Data-Intensive Applications — Chapter 5"
              class="w-full px-4 py-2.5 bg-slate-50/70 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 transition-all font-sans"
            />
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
            <div>
              <label class="block text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-1.5">
                Phân loại danh mục
              </label>
              <AppSelect
                v-model="importCategory"
                :options="formCategoryOptions"
                aria-label="Phân loại danh mục"
              />
            </div>

            <div>
              <label class="block text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-1.5">
                URL Nguồn (Tùy chọn)
              </label>
              <input
                v-model="importSourceUrl"
                type="url"
                placeholder="https://github.com/..."
                class="w-full px-4 py-2.5 bg-slate-50/70 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 transition-all font-sans"
              />
            </div>
          </div>

          <div class="p-3 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-700 dark:text-amber-300 text-xs flex items-center gap-2.5 leading-relaxed">
            <Lightbulb class="w-4 h-4 shrink-0 text-amber-500" />
            <span>Hệ thống bảo lưu nguyên vẹn ngôn ngữ gốc của tài liệu để phục vụ việc trích xuất và tra cứu.</span>
          </div>

          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-1.5">
              Nội dung Markdown
            </label>
            <textarea
              v-model="importContent"
              rows="6"
              placeholder="Dán nội dung Markdown của tài liệu vào đây..."
              class="w-full p-4 bg-slate-50/70 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] rounded-xl text-xs sm:text-sm font-mono text-slate-800 dark:text-slate-200 placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 transition-all leading-relaxed resize-none"
            ></textarea>
          </div>
        </form>

        <!-- TAB 2: PDF Drag & Drop Upload Form -->
        <form v-else-if="modalActiveTab === 'pdf'" @submit.prevent class="space-y-4 pt-1">
          <!-- Quick Preview Switcher for PDF Tab (Testing Dropzone vs Progress Bar) -->
          <div class="flex items-center justify-between p-2 rounded-xl bg-slate-100/90 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-[11px]">
            <span class="text-slate-500 font-medium">Trạng thái hiển thị:</span>
            <button
              type="button"
              @click="isProcessingPdf = !isProcessingPdf"
              class="px-2.5 py-1 rounded-lg font-bold text-brand-600 dark:text-brand-400 bg-white dark:bg-canvas-elevated border border-slate-200/80 dark:border-white/[0.1] shadow-xs cursor-pointer"
            >
              {{ isProcessingPdf ? 'Đang Xử Lý Ingestion (Click đổi)' : 'Vùng Kéo Thả File (Click đổi)' }}
            </button>
          </div>

          <!-- Asynchronous Ingestion Progress Card -->
          <div v-if="isProcessingPdf" class="p-5 sm:p-6 rounded-2xl bg-brand-500/5 dark:bg-brand-950/20 border border-brand-500/20 space-y-4">
            <div class="flex items-center justify-between gap-3">
              <div class="flex items-center gap-2 text-brand-700 dark:text-brand-300 font-bold text-xs sm:text-sm">
                <Loader2 class="w-4 h-4 animate-spin text-brand-600 dark:text-brand-400 shrink-0" />
                <span>Đang phân tích và cắt lát PDF với AI...</span>
              </div>
              <span class="font-mono font-bold text-xs sm:text-sm text-brand-600 dark:text-brand-400">{{ pdfProgress }}%</span>
            </div>

            <!-- Realtime Progress Bar -->
            <div class="w-full h-2.5 bg-slate-200/80 dark:bg-canvas-subtle rounded-full overflow-hidden p-0.5 border border-slate-200/80 dark:border-white/[0.08]">
              <div
                class="h-full bg-gradient-to-r from-brand-600 to-indigo-500 rounded-full transition-all duration-300 shadow-sm"
                :style="{ width: `${pdfProgress}%` }"
              ></div>
            </div>

            <p class="text-xs sm:text-sm text-slate-600 dark:text-slate-300 leading-relaxed font-medium">
              Đang trích xuất nội dung văn bản, phân tích cấu trúc chương và tính toán vector embeddings cho 24 lát cắt.
            </p>

            <div class="flex items-center justify-between text-xs text-slate-400 dark:text-slate-500 pt-2 border-t border-brand-500/10 dark:border-brand-500/20">
              <span>Tiến trình chạy nền độc lập</span>
              <span>Không chặn trình duyệt</span>
            </div>
          </div>

          <!-- Dropzone -->
          <div
            v-show="!isProcessingPdf"
            :class="[
              'border-2 border-dashed rounded-2xl p-6 sm:p-8 text-center transition-all cursor-pointer relative',
              isDraggingPdf
                ? 'border-brand-500 bg-brand-500/10 dark:bg-brand-950/30'
                : 'border-slate-300 dark:border-white/[0.12] hover:border-brand-500/50 bg-slate-50/50 dark:bg-canvas-subtle/50'
            ]"
          >
            <div class="flex flex-col items-center justify-center space-y-2.5">
              <div class="w-12 h-12 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 flex items-center justify-center border border-brand-500/20 shadow-xs">
                <UploadCloud class="h-6 w-6" />
              </div>

              <div v-if="!pdfFile" class="space-y-1">
                <h4 class="text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200">
                  Kéo thả file PDF vào đây hoặc bấm để chọn
                </h4>
                <p class="text-xs text-slate-500 dark:text-slate-400">
                  Hỗ trợ tài liệu kiến trúc, RFC, và sách kỹ thuật PDF
                </p>
                <p class="text-xs text-brand-600 dark:text-brand-400 font-mono">
                  Tối đa 50MB mỗi tài liệu
                </p>
              </div>

              <div v-else class="space-y-1">
                <div class="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-700 dark:text-emerald-300 text-xs font-mono font-bold">
                  <CheckCircle2 class="w-4 h-4 text-emerald-500" />
                  <span>{{ pdfFile.name }} ({{ (pdfFile.size / (1024 * 1024)).toFixed(1) }} MB)</span>
                </div>
                <p class="text-[11px] text-slate-400">Bấm để thay đổi file khác</p>
              </div>
            </div>
          </div>

          <div v-show="!isProcessingPdf" class="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
            <div>
              <label class="block text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-1.5">
                Tiêu đề tài liệu
              </label>
              <input
                v-model="pdfTitle"
                type="text"
                placeholder="Tiêu đề sách hoặc tài liệu"
                class="w-full px-4 py-2.5 bg-slate-50/70 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 transition-all font-sans"
              />
            </div>

            <div>
              <label class="block text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-1.5">
                Phân loại danh mục
              </label>
              <AppSelect
                v-model="pdfCategory"
                :options="formCategoryOptions"
                aria-label="Phân loại danh mục"
              />
            </div>
          </div>
        </form>

        <!-- TAB 3: Web URL Crawler Form -->
        <div v-else-if="modalActiveTab === 'url'" class="space-y-4 pt-1">
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-1.5">
              Đường dẫn tài liệu Web (URL)
            </label>
            <div class="flex flex-col sm:flex-row gap-2">
              <input
                v-model="crawlUrlInput"
                type="url"
                placeholder="https://learn.microsoft.com/... hoặc link tài liệu RFC"
                class="flex-1 px-4 py-2.5 bg-slate-50/70 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 transition-all font-sans"
              />
              <button
                type="button"
                :disabled="!crawlUrlInput || isCrawling"
                @click="isCrawling = true; setTimeout(() => { isCrawling = false }, 1500)"
                class="flex items-center justify-center gap-2 px-5 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-md shadow-brand-500/20 transition-all active:scale-95 disabled:opacity-50 shrink-0 cursor-pointer"
              >
                <span v-if="isCrawling" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                <Globe v-else class="w-4 h-4" />
                <span>{{ isCrawling ? 'Đang Thu Thập...' : 'Thu Thập URL' }}</span>
              </button>
            </div>
          </div>

          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-1.5">
              Phân loại danh mục
            </label>
            <AppSelect
              v-model="importCategory"
              :options="formCategoryOptions"
              aria-label="Phân loại danh mục"
            />
          </div>

          <!-- Embedded PDF Preview Card -->
          <div v-if="isPdfDetected && detectedPdfUrl" class="p-4 rounded-2xl bg-indigo-500/10 border border-indigo-500/20 text-indigo-700 dark:text-indigo-300 space-y-2.5">
            <div class="flex items-center gap-2 text-indigo-700 dark:text-indigo-300 font-bold text-xs sm:text-sm">
              <FileUp class="w-4 h-4 shrink-0" />
              <span>Phát hiện tài liệu dạng PDF nhúng trong trang:</span>
            </div>
            <p class="text-xs text-slate-600 dark:text-slate-400 font-mono truncate">{{ detectedPdfUrl }}</p>
            <button
              type="button"
              class="w-full flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white font-bold text-xs sm:text-sm shadow-md shadow-indigo-500/20 transition-all active:scale-95 cursor-pointer"
            >
              <FileUp class="w-4 h-4 shrink-0" />
              <span>Nhập File PDF Tự Động Này</span>
            </button>
          </div>

          <div class="p-4 rounded-2xl bg-slate-50/80 dark:bg-canvas-subtle/80 border border-slate-200/80 dark:border-white/[0.06] text-xs text-slate-500 dark:text-slate-400 space-y-2">
            <h5 class="font-bold text-slate-700 dark:text-slate-300">Nguồn Hỗ Trợ Tốt Nhất:</h5>
            <ul class="list-disc list-inside space-y-1 leading-relaxed">
              <li><strong>Microsoft Learn & RFCs:</strong> Tự động bóc tách phiên bản runtime và bỏ qua các widget thừa.</li>
              <li><strong>GitHub Markdown:</strong> Nhận diện cấu trúc header và code block chuẩn.</li>
            </ul>
          </div>
        </div>

        <!-- Pinned Sticky Footer (Design System Standard) -->
        <template #footer>
          <button
            type="button"
            class="h-9 rounded-xl border border-slate-200/80 dark:border-white/[0.08] px-4 text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300 transition-colors hover:bg-slate-100 dark:hover:bg-white/[0.06] cursor-pointer whitespace-nowrap shrink-0"
            @click="isImportModalOpen = false"
          >
            Hủy
          </button>

          <button
            v-if="modalActiveTab === 'markdown'"
            type="button"
            class="h-9 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-md shadow-brand-500/20 transition-all active:scale-95 px-5 flex items-center justify-center gap-1.5 cursor-pointer whitespace-nowrap shrink-0"
            @click="isImportModalOpen = false"
          >
            <span>Tiến Hành Nhập Markdown</span>
          </button>

          <button
            v-else-if="modalActiveTab === 'pdf'"
            type="button"
            class="h-9 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-md shadow-brand-500/20 transition-all active:scale-95 px-5 flex items-center justify-center gap-1.5 cursor-pointer whitespace-nowrap shrink-0"
            @click="isImportModalOpen = false"
          >
            <span>Tải Lên & Cắt Lát PDF</span>
          </button>

          <button
            v-else-if="modalActiveTab === 'url'"
            type="button"
            class="h-9 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-md shadow-brand-500/20 transition-all active:scale-95 px-5 flex items-center justify-center gap-1.5 cursor-pointer whitespace-nowrap shrink-0"
            @click="isImportModalOpen = false"
          >
            <Globe class="w-4 h-4" />
            <span>Thu Thập Nội Dung Web</span>
          </button>
        </template>
      </AppModal>

      <!-- ===================================================================== -->
      <!-- VIEW 2: FLASHCARD DECK MANAGEMENT                                     -->
      <!-- ===================================================================== -->
      <section v-if="activeView === 'review'" class="space-y-5">
          <!-- 1. Bento Stat Cards Grid (Trực tiếp trên nền Canvas) -->
          <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4 w-full">
            <!-- Card 1: FlashcardHeroCard -->
            <div class="relative overflow-hidden rounded-2xl bg-gradient-to-br from-brand-600 to-indigo-700 text-white p-5 shadow-lg shadow-brand-500/10 flex flex-col justify-between h-full min-h-[190px]">
              <div class="pointer-events-none absolute -right-10 -top-10 h-36 w-36 rounded-full bg-white/10 blur-2xl"></div>
              <div class="relative z-10 space-y-2">
                <div class="flex items-center justify-between">
                  <span class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-white/15 text-white/95 backdrop-blur-sm border border-white/10">
                    <Flame class="w-3.5 h-3.5 text-amber-300" />
                    <span>Thẻ Cần Ôn Hôm Nay</span>
                  </span>
                  <span class="inline-flex items-center gap-1 text-xs text-white/80 font-medium">
                    <Clock class="w-3.5 h-3.5" />
                    <span>~7 phút</span>
                  </span>
                </div>
                <div class="pt-1">
                  <div class="text-3xl font-black tracking-tight text-white flex items-baseline gap-2">
                    <span>14</span>
                    <span class="text-xs font-semibold text-white/80 lowercase">thẻ đến hạn</span>
                  </div>
                  <p class="text-xs text-white/85 leading-relaxed mt-1">
                    Thuật toán SM-2 phát hiện 14 thẻ chuẩn bị rơi vào vùng quên. Hãy ôn tập ngay để củng cố rãnh ghi nhớ!
                  </p>
                </div>
              </div>
              <div class="relative z-10 pt-3">
                <button class="w-full h-9 px-4 rounded-xl bg-white text-brand-700 hover:bg-slate-100 font-bold text-xs sm:text-sm flex items-center justify-center gap-2 shadow-md transition-all active:scale-[0.98] cursor-pointer">
                  <Zap class="w-4 h-4 fill-brand-600" />
                  <span>Bắt Đầu Ôn Tập Ngay</span>
                  <ChevronRight class="w-4 h-4" />
                </button>
              </div>
            </div>

            <!-- Card 2: MasteryGaugeCard -->
            <div class="rounded-2xl glass-card text-slate-900 dark:text-white p-5 shadow-sm flex flex-col justify-between h-full min-h-[190px]">
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-2">
                  <div class="w-8 h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center">
                    <Award class="w-4 h-4" />
                  </div>
                  <div>
                    <h3 class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white">Tỷ Lệ Thuộc Thẻ</h3>
                    <p class="text-[11px] text-slate-500 dark:text-slate-400">Đạt ngưỡng nhớ bền vững (EF ≥ 2.4)</p>
                  </div>
                </div>
                <span class="px-2.5 py-1 rounded-full text-[11px] font-bold bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20">
                  Bậc Cao Thủ
                </span>
              </div>

              <div class="flex items-center justify-center gap-5 py-1">
                <div class="relative w-28 h-16 flex items-end justify-center">
                  <svg class="w-28 h-16 overflow-visible" viewBox="0 0 100 55">
                    <path d="M 5 50 A 45 45 0 0 1 95 50" fill="none" stroke="currentColor" stroke-width="8" stroke-linecap="round" class="text-slate-100 dark:text-white/[0.06]" />
                    <path d="M 5 50 A 45 45 0 0 1 95 50" fill="none" stroke="currentColor" stroke-width="8" stroke-linecap="round" class="text-brand-500 transition-all duration-700" stroke-dasharray="141.37" stroke-dashoffset="31.1" />
                  </svg>
                  <div class="absolute bottom-0 text-center">
                    <span class="text-xl font-black text-slate-900 dark:text-white leading-none">78%</span>
                    <span class="block text-[10px] text-slate-400 font-medium">Làm Chủ</span>
                  </div>
                </div>

                <div class="space-y-1 text-xs text-slate-500 dark:text-slate-400">
                  <div class="flex items-center gap-2">
                    <span class="w-2 h-2 rounded-full bg-brand-500"></span>
                    <span>86 Đã thuộc</span>
                  </div>
                  <div class="flex items-center gap-2">
                    <span class="w-2 h-2 rounded-full bg-slate-300 dark:bg-white/[0.2]"></span>
                    <span>24 Đang rèn luyện</span>
                  </div>
                </div>
              </div>

              <div class="pt-2 border-t border-slate-100 dark:border-white/[0.06] flex items-center justify-between text-xs text-slate-500 dark:text-slate-400">
                <span>Tổng cộng 110 thẻ trong kho</span>
                <span class="inline-flex items-center gap-1 font-mono text-emerald-500 font-bold">
                  <TrendingUp class="w-3.5 h-3.5" />
                  <span>+12% tuần này</span>
                </span>
              </div>
            </div>

            <!-- Card 3: ReviewForecastChart -->
            <div class="rounded-2xl glass-card text-slate-900 dark:text-white p-5 shadow-sm flex flex-col justify-between h-full min-h-[190px]">
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-2">
                  <div class="w-8 h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center">
                    <BarChart3 class="w-4 h-4" />
                  </div>
                  <div>
                    <h3 class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white">Dự Báo Tải Ôn Tập (7 Ngày)</h3>
                    <p class="text-[11px] text-slate-500 dark:text-slate-400">Phân bổ thẻ đến hạn theo ngày</p>
                  </div>
                </div>
                <span class="text-xs font-mono font-bold text-brand-500">42 Thẻ Sắp Tới</span>
              </div>

              <div class="flex items-end justify-between gap-1.5 h-16 pt-2 px-1">
                <div class="flex-1 flex flex-col items-center gap-1">
                  <div class="w-full bg-brand-500 rounded-t-md" style="height: 100%"></div>
                  <span class="text-[10px] font-mono text-slate-400">T5</span>
                </div>
                <div class="flex-1 flex flex-col items-center gap-1">
                  <div class="w-full bg-brand-500/70 rounded-t-md" style="height: 57%"></div>
                  <span class="text-[10px] font-mono text-slate-400">T6</span>
                </div>
                <div class="flex-1 flex flex-col items-center gap-1">
                  <div class="w-full bg-brand-500/50 rounded-t-md" style="height: 35%"></div>
                  <span class="text-[10px] font-mono text-slate-400">T7</span>
                </div>
                <div class="flex-1 flex flex-col items-center gap-1">
                  <div class="w-full bg-brand-500/40 rounded-t-md" style="height: 28%"></div>
                  <span class="text-[10px] font-mono text-slate-400">CN</span>
                </div>
                <div class="flex-1 flex flex-col items-center gap-1">
                  <div class="w-full bg-brand-500/60 rounded-t-md" style="height: 50%"></div>
                  <span class="text-[10px] font-mono text-slate-400">T2</span>
                </div>
                <div class="flex-1 flex flex-col items-center gap-1">
                  <div class="w-full bg-brand-500/30 rounded-t-md" style="height: 21%"></div>
                  <span class="text-[10px] font-mono text-slate-400">T3</span>
                </div>
                <div class="flex-1 flex flex-col items-center gap-1">
                  <div class="w-full bg-brand-500/45 rounded-t-md" style="height: 35%"></div>
                  <span class="text-[10px] font-mono text-slate-400">T4</span>
                </div>
              </div>

              <div class="pt-2 border-t border-slate-100 dark:border-white/[0.06] flex items-center justify-between text-xs text-slate-500 dark:text-slate-400">
                <span>Đỉnh điểm: 14 thẻ (Thứ 5)</span>
                <span class="font-mono text-brand-500 font-semibold">~6 thẻ/ngày</span>
              </div>
            </div>
          </div>

          <!-- 2. Filters & Search Bar (Trực tiếp trên nền Canvas) -->
          <div class="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 w-full">
            <!-- Quick Filter Chips -->
            <div class="flex items-center gap-1.5 overflow-x-auto pb-0.5 sm:pb-0">
              <button
                type="button"
                @click="selectedQuickFilter = 'all'"
                :class="[
                  'flex items-center gap-1 px-3 py-1.5 rounded-xl text-xs font-semibold border transition-all whitespace-nowrap shrink-0 cursor-pointer',
                  selectedQuickFilter === 'all'
                    ? 'bg-brand-600 text-white border-transparent shadow-sm'
                    : 'bg-white dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200/80 dark:border-white/[0.08]'
                ]"
              >
                <Layers class="w-3.5 h-3.5" />
                <span>Tất Cả Thẻ (110)</span>
              </button>
              <button
                type="button"
                @click="selectedQuickFilter = 'due'"
                :class="[
                  'flex items-center gap-1 px-3 py-1.5 rounded-xl text-xs font-semibold border transition-all whitespace-nowrap shrink-0 cursor-pointer',
                  selectedQuickFilter === 'due'
                    ? 'bg-amber-600 text-white border-transparent shadow-sm'
                    : 'bg-white dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200/80 dark:border-white/[0.08]'
                ]"
              >
                <Flame class="w-3.5 h-3.5 text-amber-500" />
                <span>Cần Ôn Hôm Nay (14)</span>
              </button>
              <button
                type="button"
                @click="selectedQuickFilter = 'mastered'"
                :class="[
                  'flex items-center gap-1 px-3 py-1.5 rounded-xl text-xs font-semibold border transition-all whitespace-nowrap shrink-0 cursor-pointer',
                  selectedQuickFilter === 'mastered'
                    ? 'bg-emerald-600 text-white border-transparent shadow-sm'
                    : 'bg-white dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200/80 dark:border-white/[0.08]'
                ]"
              >
                <Check class="w-3.5 h-3.5 text-emerald-500" />
                <span>Đã Thuộc (86)</span>
              </button>
            </div>

            <!-- Search input with ⌘K -->
            <div class="relative w-full sm:w-72 shrink-0">
              <Search class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
              <input
                v-model="reviewSearch"
                type="text"
                placeholder="Tìm kiếm câu hỏi flashcard..."
                class="w-full pl-9 pr-12 py-1.5 text-xs rounded-xl bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:border-brand-500 shadow-sm"
              />
              <span class="absolute right-2.5 top-1/2 -translate-y-1/2 px-1.5 py-0.5 rounded text-[10px] font-mono text-slate-400 bg-slate-100 dark:bg-white/[0.06] border border-slate-200 dark:border-white/[0.08]">⌘K</span>
            </div>
          </div>

          <!-- 3. Flashcard Inventory Cards (Trực tiếp trên nền Canvas) -->
          <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
            <div
              v-for="card in filteredFlashcards"
              :key="card.id"
              class="p-4 rounded-xl border border-slate-200/90 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle hover:border-brand-500/40 transition-all flex flex-col justify-between space-y-3 shadow-sm hover:shadow-md"
            >
              <div class="space-y-2">
                <div class="flex items-center justify-between gap-2">
                  <span
                    :class="[
                      'px-2 py-0.5 rounded text-[10px] font-mono font-bold whitespace-nowrap shrink-0 border',
                      card.urgency === 'due'
                        ? 'bg-rose-500/10 text-rose-500 border-rose-500/20'
                        : 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20'
                    ]"
                  >
                    {{ card.urgency === 'due' ? 'Đến Hạn Hôm Nay' : 'Đã Thuộc' }}
                  </span>
                  <span class="text-[11px] font-mono text-slate-400">EF: {{ card.ef }} • Chu kỳ: {{ card.interval }}</span>
                </div>
                <h4 class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white leading-snug">
                  {{ card.question }}
                </h4>
                <p class="text-xs text-slate-500 dark:text-slate-400 line-clamp-2">
                  {{ card.answer }}
                </p>
              </div>
              <div class="flex items-center justify-between pt-2 border-t border-slate-100 dark:border-white/[0.06] text-xs">
                <span class="text-slate-400 text-[11px] truncate max-w-[180px]">Nguồn: {{ card.source }}</span>
                <button class="text-brand-500 hover:text-brand-400 font-bold text-xs whitespace-nowrap cursor-pointer">Chi tiết →</button>
              </div>
            </div>
          </div>

          <!-- 4. Pagination (Trực tiếp trên nền Canvas) -->
          <div class="flex items-center justify-between w-full pt-1 text-xs text-slate-500 dark:text-slate-400">
            <span>Hiển thị {{ filteredFlashcards.length }} / {{ mockFlashcards.length }} thẻ flashcard</span>
            <div class="flex items-center gap-1">
              <button class="px-2.5 py-1 rounded-lg border border-slate-200 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-canvas-elevated cursor-pointer">Trước</button>
              <button class="px-2.5 py-1 rounded-lg bg-brand-600 text-white font-bold cursor-pointer">1</button>
              <button class="px-2.5 py-1 rounded-lg border border-slate-200 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-canvas-elevated cursor-pointer">2</button>
              <button class="px-2.5 py-1 rounded-lg border border-slate-200 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-canvas-elevated cursor-pointer">Sau</button>
            </div>
          </div>
      </section>

      <!-- ===================================================================== -->
      <!-- VIEW 3: ARCHITECTURAL INSIGHTS                                        -->
      <!-- ===================================================================== -->
      <section v-if="activeView === 'insights'" class="space-y-4">
        
        <!-- 1. Header Banner (Standalone glass-panel) -->
        <div class="p-5 sm:p-6 rounded-2xl glass-panel border border-slate-200/80 dark:border-white/[0.08] text-slate-900 dark:text-white shadow-sm relative overflow-hidden transition-all">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 relative z-10">
            <div class="space-y-1.5 flex-1 min-w-0">
              <div class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-brand-500/10 text-brand-600 dark:text-brand-300 border border-brand-500/20 text-xs font-bold uppercase tracking-wider">
                <Lightbulb class="w-3.5 h-3.5 text-brand-400" />
                <span>Kiến Thức Chuyên Sâu</span>
              </div>
              <h1 class="text-xl sm:text-2xl font-black text-slate-900 dark:text-white tracking-tight">
                Mẫu Kiến Thức Chuyên Sâu
              </h1>
              <p class="text-slate-500 dark:text-slate-400 text-xs sm:text-sm leading-relaxed max-w-xl">
                Các mẹo kiến trúc hệ thống, cơ chế runtime tầng sâu và kỹ thuật tối ưu hóa hiệu năng thực chiến.
              </p>
            </div>

            <div class="flex items-center gap-2 shrink-0">
              <button class="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-white dark:bg-canvas-subtle hover:bg-slate-50 dark:hover:bg-canvas-elevated text-slate-700 dark:text-slate-200 text-xs sm:text-sm font-semibold transition-all border border-slate-200/80 dark:border-white/[0.08] shadow-sm cursor-pointer">
                <Shuffle class="w-3.5 h-3.5 text-slate-400" />
                <span>Ngẫu Nhiên</span>
              </button>
              <button class="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-xs sm:text-sm font-semibold transition-all shadow-md shadow-brand-500/20 cursor-pointer">
                <Sparkles class="w-3.5 h-3.5" />
                <span>Tạo Với AI</span>
              </button>
            </div>
          </div>
        </div>

        <!-- 2. Controls & Filter Bar (Standalone row, NO outer card) -->
        <div class="flex flex-wrap items-center justify-between gap-3 pt-1">
          <div class="flex items-center p-1 bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] rounded-xl">
            <button
              @click="insightsViewMode = 'explore'"
              :class="[
                'flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg text-xs font-bold transition-all cursor-pointer',
                insightsViewMode === 'explore'
                  ? 'bg-white dark:bg-canvas-elevated text-slate-900 dark:text-white border border-slate-200/80 dark:border-white/[0.12] shadow-sm'
                  : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white border border-transparent'
              ]"
            >
              <Brain class="w-3.5 h-3.5 text-brand-400" />
              <span>Khám Phá</span>
            </button>
            <button
              @click="insightsViewMode = 'saved'"
              :class="[
                'flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg text-xs font-medium transition-all cursor-pointer',
                insightsViewMode === 'saved'
                  ? 'bg-white dark:bg-canvas-elevated text-slate-900 dark:text-white border border-slate-200/80 dark:border-white/[0.12] shadow-sm'
                  : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white border border-transparent'
              ]"
            >
              <Bookmark class="w-3.5 h-3.5 text-slate-400" />
              <span>Đã Lưu ({{ bookmarkCount }})</span>
            </button>
          </div>

          <div class="flex items-center gap-1.5 overflow-x-auto pb-0.5">
            <button
              v-for="cat in ['Tất Cả', 'Frontend & Web', 'Backend & Runtime', 'Cơ Sở Dữ Liệu', 'Hệ Thống Phân Tán']"
              :key="cat"
              class="px-3 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap shrink-0 border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-canvas-elevated cursor-pointer"
            >
              {{ cat }}
            </button>
          </div>
        </div>

        <!-- 3. DEEP-DIVE ARTICLE CARD (Standalone glass-card) -->
        <div class="rounded-2xl sm:rounded-3xl glass-card border border-slate-200/80 dark:border-white/[0.08] shadow-xl overflow-hidden transition-all duration-300">
          <!-- Card Header -->
          <div class="p-6 sm:p-8 border-b border-slate-100 dark:border-white/[0.06] space-y-4">
            <div class="flex items-center justify-between gap-3">
              <div class="flex flex-wrap items-center gap-1.5 sm:gap-2 min-w-0">
                <span class="px-2.5 py-0.5 rounded-md text-xs font-semibold bg-brand-500/10 text-brand-600 dark:text-brand-300 border border-brand-500/20 shrink-0">
                  Core Architecture
                </span>
                <span class="px-2 py-0.5 rounded-md bg-slate-100 dark:bg-canvas-subtle text-slate-500 dark:text-slate-400 border border-slate-200/60 dark:border-white/[0.06] text-xs font-mono">
                  #PostgreSQL
                </span>
                <span class="px-2 py-0.5 rounded-md bg-slate-100 dark:bg-canvas-subtle text-slate-500 dark:text-slate-400 border border-slate-200/60 dark:border-white/[0.06] text-xs font-mono">
                  #Performance
                </span>
                <span class="px-2 py-0.5 rounded-md bg-slate-100 dark:bg-canvas-subtle text-slate-500 dark:text-slate-400 border border-slate-200/60 dark:border-white/[0.06] text-xs font-mono">
                  #Indexing
                </span>
              </div>

              <button
                @click="toggleBookmark"
                class="p-2 rounded-xl border border-slate-200/80 dark:border-white/[0.08] text-slate-400 hover:text-brand-400 transition-colors flex items-center gap-1.5 cursor-pointer"
              >
                <BookmarkCheck v-if="isBookmarked" class="w-4 h-4 text-brand-400 fill-brand-400/20" />
                <Bookmark v-else class="w-4 h-4" />
                <span class="text-xs font-mono font-medium">{{ bookmarkCount }}</span>
              </button>
            </div>

            <!-- Title -->
            <h2 class="text-xl sm:text-2xl font-bold text-slate-900 dark:text-white tracking-tight leading-snug">
              PostgreSQL Visibility Map & Index-Only Scans: Preventing Hidden Heap Fetches
            </h2>

            <!-- Benchmark Metrics -->
            <div class="flex flex-wrap items-center gap-2 pt-0.5">
              <div class="inline-flex items-center gap-1.5 px-3 py-1 rounded-lg bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 text-xs font-mono font-medium">
                <Zap class="w-3.5 h-3.5 fill-emerald-500" />
                <span>Latency: 4.2ms → 0.3ms (14x faster)</span>
              </div>
              <div class="inline-flex items-center gap-1.5 px-3 py-1 rounded-lg bg-sky-500/10 border border-sky-500/20 text-sky-600 dark:text-sky-400 text-xs font-mono font-medium">
                <Zap class="w-3.5 h-3.5 fill-sky-500" />
                <span>Heap Fetches: 15,000 → 0 / 1M rows</span>
              </div>
            </div>

            <!-- Summary Prose -->
            <div class="text-sm text-slate-700 dark:text-slate-300 leading-relaxed font-normal pt-1">
              Nhiều lập trình viên tin rằng việc sử dụng <code class="px-1.5 py-0.5 rounded bg-slate-100 dark:bg-white/[0.08] font-mono text-xs text-brand-400">INCLUDE</code> clause trong covering index sẽ tự động giúp PostgreSQL thực hiện <strong>Index-Only Scan</strong> và đạt hiệu năng tối đa. Tuy nhiên, nếu <strong>Visibility Map (VM)</strong> chưa được cập nhật (do <code class="px-1.5 py-0.5 rounded bg-slate-100 dark:bg-white/[0.08] font-mono text-xs">VACUUM</code> chưa chạy hoặc transaction cũ đang chạy), PostgreSQL sẽ buộc phải thực hiện các thao tác <strong>Heap Fetch</strong> ngẫu nhiên để kiểm tra tính hiệu lực của tuple, làm mất đi lợi thế của Index-Only Scan.
            </div>
          </div>

          <!-- Unified Integrated Developer Terminal Window -->
          <div class="p-5 sm:p-7 md:p-8 bg-slate-50/50 dark:bg-canvas-subtle/40 border-b border-slate-100 dark:border-white/[0.06]">
            <div class="max-w-full">
              <CommonShikiCodeBlock
                :code="activeCodeTab === 'solution' ? solutionSql : problemSql"
                language="sql"
              >
                <template #left>
                  <div class="flex items-center gap-1.5 sm:gap-2.5 min-w-0">
                    <FileCode2 class="h-4 w-4 shrink-0 text-brand-500 dark:text-brand-400 hidden sm:block" />
                    <div class="flex items-center p-0.5 rounded-lg bg-slate-200/80 dark:bg-black/40 border border-slate-300/80 dark:border-white/[0.06] shrink-0">
                      <button
                        type="button"
                        @click="activeCodeTab = 'solution'"
                        :class="[
                          'inline-flex items-center gap-1 sm:gap-1.5 px-2 sm:px-3 py-1 rounded-md text-[11px] sm:text-xs font-bold transition-all whitespace-nowrap shrink-0 cursor-pointer',
                          activeCodeTab === 'solution'
                            ? 'bg-brand-500/20 text-brand-700 dark:text-brand-300 border border-brand-500/30 shadow-xs'
                            : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200 border border-transparent'
                        ]"
                      >
                        <CheckCircle2 class="w-3.5 h-3.5 text-brand-500 dark:text-brand-400 shrink-0" />
                        <span class="hidden sm:inline">Senior </span><span>Solution</span>
                      </button>
                      <button
                        type="button"
                        @click="activeCodeTab = 'problem'"
                        :class="[
                          'inline-flex items-center gap-1 sm:gap-1.5 px-2 sm:px-3 py-1 rounded-md text-[11px] sm:text-xs font-bold transition-all whitespace-nowrap shrink-0 cursor-pointer',
                          activeCodeTab === 'problem'
                            ? 'bg-rose-500/20 text-rose-700 dark:text-rose-300 border border-rose-500/30 shadow-xs'
                            : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200 border border-transparent'
                        ]"
                      >
                        <XCircle class="w-3.5 h-3.5 text-rose-500 dark:text-rose-400 shrink-0" />
                        <span>Anti-Pattern</span>
                      </button>
                    </div>
                  </div>
                </template>
              </CommonShikiCodeBlock>
            </div>
          </div>

          <!-- Under The Hood Mechanics -->
          <div class="p-6 sm:p-8 space-y-3.5">
            <div class="flex items-center gap-2 text-slate-900 dark:text-white font-bold text-sm sm:text-base">
              <Brain class="w-4 h-4 text-brand-400" />
              <span>Cơ Chế Hoạt Động Tầng Sâu (Under The Hood Mechanics)</span>
            </div>

            <div class="space-y-2.5 text-xs sm:text-sm text-slate-600 dark:text-slate-300 leading-relaxed">
              <div class="flex items-start gap-2.5">
                <span class="w-1.5 h-1.5 rounded-full bg-brand-500 mt-2 shrink-0"></span>
                <p><strong>MVCC Overhead:</strong> Khác với các cơ sở dữ liệu khác, PostgreSQL lưu trữ thông tin MVCC trực tiếp bên trong Heap Tuple (các trường <code class="px-1 py-0.5 rounded bg-slate-100 dark:bg-white/[0.08] font-mono text-xs">xmin</code>, <code class="px-1 py-0.5 rounded bg-slate-100 dark:bg-white/[0.08] font-mono text-xs">xmax</code>), chứ không lưu trên Index Tuple.</p>
              </div>
              <div class="flex items-start gap-2.5">
                <span class="w-1.5 h-1.5 rounded-full bg-brand-500 mt-2 shrink-0"></span>
                <p><strong>Visibility Map (VM):</strong> Là một bitmap lưu trữ trạng thái của từng Heap Page. Nếu bit tương ứng được bật (all-visible), PostgreSQL biết chắc chắn rằng mọi tuple trong trang đó đều hiển thị với mọi transaction hiện tại và tương lai, cho phép bỏ qua bước truy cập Heap (<code class="px-1 py-0.5 rounded bg-slate-100 dark:bg-white/[0.08] font-mono text-xs">Heap Fetches = 0</code>).</p>
              </div>
              <div class="flex items-start gap-2.5">
                <span class="w-1.5 h-1.5 rounded-full bg-brand-500 mt-2 shrink-0"></span>
                <p><strong>The Cost:</strong> Nếu một trang dữ liệu bị sửa đổi (ví dụ: <code class="px-1 py-0.5 rounded bg-slate-100 dark:bg-white/[0.08] font-mono text-xs">UPDATE status</code>), bit trong Visibility Map bị xóa. Index-Only Scan lúc này sẽ biến thành một dạng bão hòa I/O ngẫu nhiên nếu hệ thống không được cấu hình <code class="px-1 py-0.5 rounded bg-slate-100 dark:bg-white/[0.08] font-mono text-xs">autovacuum</code> kịp thời.</p>
              </div>
            </div>

            <!-- Official Documentation Reference -->
            <div class="pt-4 border-t border-slate-100 dark:border-white/[0.06] flex flex-wrap items-center justify-between gap-3 text-xs text-slate-500 dark:text-slate-400">
              <div class="flex items-center gap-1.5">
                <BookOpen class="w-3.5 h-3.5 text-slate-400" />
                <span>Tài liệu tham khảo chính thống:</span>
                <a href="https://www.postgresql.org/docs/current/indexes-index-only-scans.html" target="_blank" rel="noopener noreferrer" class="text-brand-500 hover:text-brand-400 font-medium inline-flex items-center gap-1">
                  PostgreSQL Index-Only Scans Documentation
                  <ExternalLink class="w-3 h-3" />
                </a>
              </div>
            </div>
          </div>

          <!-- Card Navigation Footer -->
          <div class="p-4 sm:p-6 bg-slate-50 dark:bg-canvas-subtle/70 border-t border-slate-200/80 dark:border-white/[0.06] flex flex-col sm:flex-row items-center justify-between gap-3 sm:gap-4">
            <div class="flex items-center gap-2 text-xs text-slate-500 dark:text-slate-400">
              <span class="font-bold text-slate-700 dark:text-slate-300">Mẫu 1/9</span>
              <span>•</span>
              <span>Nhấn <kbd class="px-1.5 py-0.5 rounded text-[10px] font-mono bg-white dark:bg-white/[0.06] border border-slate-200 dark:border-white/[0.08]">[Space]</kbd> hoặc <kbd class="px-1.5 py-0.5 rounded text-[10px] font-mono bg-white dark:bg-white/[0.06] border border-slate-200 dark:border-white/[0.08]">[→]</kbd> để chuyển nhanh</span>
            </div>

            <div class="flex items-center gap-2 sm:gap-3 w-full sm:w-auto justify-between sm:justify-end">
              <button class="flex-1 sm:flex-none flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle text-slate-700 dark:text-slate-200 text-xs sm:text-sm font-bold hover:bg-slate-50 dark:hover:bg-canvas-elevated transition-colors shadow-sm active:scale-95 cursor-pointer">
                <ChevronLeft class="w-4 h-4" />
                <span>Mẫu Trước</span>
              </button>

              <button class="flex-1 sm:flex-none flex items-center justify-center gap-1.5 px-5 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-xs sm:text-sm font-bold transition-all shadow-md shadow-brand-500/20 active:scale-95 cursor-pointer">
                <span>Mẫu Tiếp Theo</span>
                <ChevronRight class="w-4 h-4" />
              </button>
            </div>
          </div>
        </div>
      </section>

    </main>
  </div>
</template>
