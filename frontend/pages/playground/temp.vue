<script setup lang="ts">
import { ref } from 'vue'
import {
  HelpCircle,
  Sparkles,
  RotateCcw,
  BarChart3,
  CheckCircle2,
  XCircle,
  ArrowRight,
  Flame,
  Award,
  BookOpen,
  Clock,
  Layers,
  Check,
  Eye,
  EyeOff,
  Sun,
  Moon,
  Key,
  Activity,
  BrainCircuit,
  FileCode,
  Tag
} from 'lucide-vue-next'
import OptionCard, { type OptionState } from '~/components/ui/OptionCard.vue'

// --- View Switcher State ---
type PlaygroundView = 'quiz-arena' | 'flashcard-studio'
const activeView = ref<PlaygroundView>('flashcard-studio') // default to flashcard for user inspection

// --- Theme State ---
const colorMode = useColorMode()
function toggleTheme() {
  const next = colorMode.value === 'dark' ? 'light' : 'dark'
  colorMode.preference = next
  if (typeof document !== 'undefined') {
    document.documentElement.classList.remove('light', 'dark')
    document.documentElement.classList.add(next)
  }
}

// --- Locale Simulation State ---
const currentLocale = ref<'en' | 'vi'>('en')
function toggleLocale() {
  currentLocale.value = currentLocale.value === 'en' ? 'vi' : 'en'
}

// =============================================================================
// MOCK DATA: VIEW A (QUIZ ARENA STUDIO)
// =============================================================================
const mockQuiz = {
  currentQuestion: 3,
  totalQuestions: 10,
  topic: 'ASP.NET Core Performance & Memory Management',
  seniority: 'Senior / Staff',
  timeRemainingSeconds: 42,
  timeTotalSeconds: 60,
  streak: 4,
  multiplier: '2.5x',
  score: 1850,
  question: {
    title: 'Why should ReadOnlySequence<T> and PipeReader be preferred over byte[] and MemoryStream for high-throughput network stream parsing in Kestrel?',
    codeSnippet: `// High-performance streaming pipeline
public async ValueTask ProcessRequestAsync(PipeReader reader)
{
    while (true)
    {
        ReadResult result = await reader.ReadAsync();
        ReadOnlySequence<byte> buffer = result.Buffer;
        
        while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
        {
            ProcessLine(line); // Zero LOH allocations
        }
        
        reader.AdvanceTo(buffer.Start, buffer.End);
        if (result.IsCompleted) break;
    }
}`,
    options: [
      {
        id: 0,
        text: 'PipeReader allocates buffer segments on the stack while MemoryStream allocates directly on the CPU L1 cache.'
      },
      {
        id: 1,
        text: 'ReadOnlySequence<T> pools discontinuous multi-segment memory buffers via MemoryPool<byte>.Shared, avoiding Gen-2 Large Object Heap (LOH) fragmentation and eliminating synchronous GC blocking pauses.'
      },
      {
        id: 2,
        text: 'MemoryStream requires pinning managed pointers with GCHandle.Alloc while PipeReader executes entirely in unmanaged OS kernel memory.'
      },
      {
        id: 3,
        text: 'ReadOnlySequence<T> disables thread synchronization locks across ASP.NET Core ThreadPool worker threads.'
      }
    ],
    correctIndex: 1,
    explanation: '`ReadOnlySequence<byte>` allows Kestrel and high-performance protocols to represent multi-segment memory buffers loaned directly from `MemoryPool<byte>.Shared`. Unlike `MemoryStream` which requires continuous array resizes that easily end up on the Large Object Heap (LOH > 85,000 bytes) and cause expensive Gen 2 GCs, `PipeReader` manages backpressure, zero-copy parsing, and non-contiguous buffer segments with optimal memory density.'
  }
}

const selectedQuizOption = ref<number | null>(1)
const isQuizSubmitted = ref(false)
const quizStateMode = ref<'answering' | 'submitted-correct' | 'submitted-incorrect'>('answering')

function getOptionState(idx: number): OptionState {
  if (quizStateMode.value === 'answering') {
    return selectedQuizOption.value === idx ? 'selected' : 'default'
  }
  if (quizStateMode.value === 'submitted-correct') {
    if (idx === mockQuiz.question.correctIndex) return 'correct'
    if (idx === selectedQuizOption.value) return 'incorrect'
    return 'default'
  }
  if (quizStateMode.value === 'submitted-incorrect') {
    if (idx === 0) return 'incorrect'
    if (idx === mockQuiz.question.correctIndex) return 'correct'
    return 'default'
  }
  return 'default'
}

function selectQuizOption(idx: number) {
  if (isQuizSubmitted.value) return
  selectedQuizOption.value = idx
}

function submitQuizAnswer() {
  isQuizSubmitted.value = true
  quizStateMode.value = selectedQuizOption.value === mockQuiz.question.correctIndex ? 'submitted-correct' : 'submitted-incorrect'
}

function resetQuizDemo() {
  selectedQuizOption.value = 1
  isQuizSubmitted.value = false
  quizStateMode.value = 'answering'
}

const questionMap = [
  { index: 1, status: 'correct' },
  { index: 2, status: 'correct' },
  { index: 3, status: 'current' },
  { index: 4, status: 'unvisited' },
  { index: 5, status: 'unvisited' },
  { index: 6, status: 'unvisited' },
  { index: 7, status: 'unvisited' },
  { index: 8, status: 'unvisited' },
  { index: 9, status: 'unvisited' },
  { index: 10, status: 'unvisited' }
]

// =============================================================================
// MOCK DATA: VIEW B (FLASHCARD 3D PRACTICE STUDIO)
// =============================================================================
const isCardFlipped = ref(false)
const mockCard = {
  id: 'card-101',
  topicTitle: 'PostgreSQL MVCC & VACUUM Internals',
  sourceBook: 'Designing Data-Intensive Applications',
  sourceContext: 'Chapter 7: Transactions & Snapshot Isolation',
  repetitionCount: 4,
  easeFactor: 2.45,
  intervalDays: 14,
  dueToday: 8,
  reviewedCount: 12,
  totalSessionCards: 20,
  front: {
    question: 'How does PostgreSQL implement Multi-Version Concurrency Control (MVCC) without relying on traditional rollback segments like Oracle or MySQL InnoDB?',
    category: 'Database Internals',
    seniority: 'Senior / Staff',
    conceptTags: ['MVCC', 'Heap Tuples', 'VACUUM', 'Snapshot Isolation'],
    sourceExcerpt: 'In PostgreSQL, when an UPDATE occurs, a new row version is written to the heap with xmin set to current txid, while the old version xmax is set to the same txid. No Undo tablespace exists.'
  },
  back: {
    summary: 'PostgreSQL stores all tuple versions directly inside table data pages (Heap) instead of a separate Undo tablespace, tracking visibility via tuple header stamps.',
    deepDive: [
      { term: 'Heap Tuples with xmin & xmax', desc: 'Every row header contains 4-byte transaction IDs. xmin records the creator txid; xmax records deleting or updating txid.' },
      { term: 'Updates as Insert + Invalidate', desc: 'An UPDATE inserts a new row version with current txid as xmin and points old row t_ctid to the new location.' },
      { term: 'HOT (Heap-Only Tuples)', desc: 'If updated tuple fits on the same page and no indexed columns change, PostgreSQL updates index-free without index bloat.' },
      { term: 'VACUUM & Autovacuum', desc: 'Dead tuples remain visible until autovacuum freezes old xmin values and marks space reusable in the Free Space Map (FSM).' }
    ],
    codeSnippet: `-- Inspect tuple header metadata in page buffer
SELECT ctid, xmin, xmax, t_ctid, val 
FROM heap_page_items(get_raw_page('orders', 0))
LIMIT 3;`
  }
}

const sm2Forecast = [
  { grade: 1, label: 'Blackout', interval: '1d', ef: '2.25', color: 'border-rose-500/40 text-rose-600 dark:text-rose-400 bg-rose-500/10 hover:bg-rose-500/20' },
  { grade: 2, label: 'Hard', interval: '6d', ef: '2.35', color: 'border-amber-500/40 text-amber-600 dark:text-amber-400 bg-amber-500/10 hover:bg-amber-500/20' },
  { grade: 3, label: 'Good', interval: '14d', ef: '2.45', color: 'border-brand-500/40 text-brand-600 dark:text-brand-400 bg-brand-500/10 hover:bg-brand-500/20' },
  { grade: 4, label: 'Easy', interval: '21d', ef: '2.50', color: 'border-emerald-500/40 text-emerald-600 dark:text-emerald-400 bg-emerald-500/10 hover:bg-emerald-500/20' }
]

function toggleFlip() {
  isCardFlipped.value = !isCardFlipped.value
}
</script>

<template>
  <div class="min-h-screen bg-slate-50 dark:bg-canvas text-slate-900 dark:text-slate-100 font-sans antialiased transition-colors duration-200 pb-12">

    <!-- ======================================================================= -->
    <!-- PLAYGROUND MASTER CONTROL BAR                                           -->
    <!-- ======================================================================= -->
    <header class="sticky top-0 z-50 bg-white/95 dark:bg-canvas-subtle/90 border-b border-slate-200/90 dark:border-white/[0.08] backdrop-blur-md px-4 py-2.5">
      <div class="max-w-7xl mx-auto flex flex-wrap items-center justify-between gap-3">
        
        <!-- Left: Brand & Phase 2 Indicator -->
        <div class="flex items-center gap-2.5">
          <div class="w-8 h-8 rounded-xl bg-brand-600 text-white flex items-center justify-center font-black shadow-md shadow-brand-500/25 text-sm">
            TD
          </div>
          <div>
            <div class="flex items-center gap-2">
              <span class="font-bold text-slate-900 dark:text-white text-sm">Phase 2 Unboxed Practice</span>
              <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-brand-500/15 text-brand-600 dark:text-brand-400 border border-brand-500/30">
                Direct Canvas (Zero Card-in-Card)
              </span>
            </div>
            <p class="text-[11px] text-slate-500 dark:text-slate-400 hidden sm:block">
              Single-Card Focus on Mobile • 68/32 Unboxed Cockpit on Desktop
            </p>
          </div>
        </div>

        <!-- Center: Dual View Switcher (Arena vs Flashcard) -->
        <div class="flex items-center p-1 bg-slate-100 dark:bg-canvas border border-slate-200/80 dark:border-white/[0.08] rounded-2xl text-xs font-semibold">
          <button
            @click="activeView = 'flashcard-studio'"
            :class="[
              'px-3.5 py-1.5 rounded-xl transition-all flex items-center gap-1.5 cursor-pointer',
              activeView === 'flashcard-studio'
                ? 'bg-white dark:bg-white/[0.1] text-brand-600 dark:text-white shadow-sm font-bold'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            ]"
          >
            <Layers class="w-3.5 h-3.5 text-brand-500" />
            <span>View B: Flashcard 3D Studio</span>
          </button>

          <button
            @click="activeView = 'quiz-arena'"
            :class="[
              'px-3.5 py-1.5 rounded-xl transition-all flex items-center gap-1.5 cursor-pointer',
              activeView === 'quiz-arena'
                ? 'bg-white dark:bg-white/[0.1] text-brand-600 dark:text-white shadow-sm font-bold'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            ]"
          >
            <BrainCircuit class="w-3.5 h-3.5 text-brand-500" />
            <span>View A: Quiz Arena Studio</span>
          </button>
        </div>

        <!-- Right: Utility Toggles -->
        <div class="flex items-center gap-2">
          <!-- Locale Toggle -->
          <button
            @click="toggleLocale"
            class="px-2.5 py-1.5 rounded-xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle text-xs font-mono font-bold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.04] transition-colors cursor-pointer"
          >
            🌐 {{ currentLocale.toUpperCase() }}
          </button>

          <!-- Theme Toggle -->
          <button
            @click="toggleTheme"
            class="p-2 rounded-xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.04] transition-colors cursor-pointer"
            title="Toggle Light / Dark Obsidian Theme"
          >
            <Sun v-if="colorMode.value === 'dark'" class="w-4 h-4 text-amber-400" />
            <Moon v-else class="w-4 h-4 text-slate-600" />
          </button>
        </div>

      </div>
    </header>

    <!-- ======================================================================= -->
    <!-- PLAYGROUND CONTENT STAGE: UNBOXED DIRECT-CANVAS                         -->
    <!-- ======================================================================= -->
    <main class="max-w-7xl mx-auto p-3 sm:p-5 lg:p-6 space-y-4">

      <!-- ===================================================================== -->
      <!-- VIEW B: FLASHCARD 3D PRACTICE STUDIO (UNBOXED DIRECT-CANVAS)           -->
      <!-- ===================================================================== -->
      <section v-if="activeView === 'flashcard-studio'" class="space-y-4">
        
        <!-- 1. Interactive Simulation Toolbar (Direct on Canvas) -->
        <div class="flex flex-wrap items-center justify-between gap-3 p-3 bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] rounded-2xl shadow-sm text-xs">
          <div class="flex items-center gap-2">
            <span class="font-bold text-slate-500 uppercase tracking-wider text-[11px]">Card Face:</span>
            <button
              @click="isCardFlipped = false"
              :class="[
                'px-2.5 py-1 rounded-lg border font-semibold cursor-pointer transition-colors',
                !isCardFlipped
                  ? 'bg-brand-500/10 border-brand-500/40 text-brand-600 dark:text-brand-300'
                  : 'border-slate-200 dark:border-white/[0.08] text-slate-600 dark:text-slate-400'
              ]"
            >
              Front (Prompt)
            </button>
            <button
              @click="isCardFlipped = true"
              :class="[
                'px-2.5 py-1 rounded-lg border font-semibold cursor-pointer transition-colors',
                isCardFlipped
                  ? 'bg-brand-500/10 border-brand-500/40 text-brand-600 dark:text-brand-300'
                  : 'border-slate-200 dark:border-white/[0.08] text-slate-600 dark:text-slate-400'
              ]"
            >
              Back (Solution & SM-2)
            </button>
          </div>

          <div class="flex items-center gap-2">
            <span class="text-xs text-slate-500 dark:text-slate-400 font-mono">
              EF: {{ mockCard.easeFactor }} • {{ mockCard.intervalDays }}d interval
            </span>
          </div>
        </div>

        <!-- 2. Clean Breadcrumb & Status Header (Direct on Canvas, NO ENCLOSING CARD!) -->
        <div class="flex flex-wrap items-center justify-between gap-2 px-1">
          <div class="flex items-center gap-2.5 min-w-0">
            <div class="w-7 h-7 sm:w-8 sm:h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0">
              <Layers class="w-4 h-4" />
            </div>
            <div class="min-w-0">
              <h1 class="text-sm sm:text-base font-bold text-slate-900 dark:text-white truncate">
                {{ mockCard.topicTitle }}
              </h1>
              <p class="text-[11px] sm:text-xs text-slate-500 dark:text-slate-400 truncate">
                {{ mockCard.sourceBook }} • {{ mockCard.sourceContext }}
              </p>
            </div>
          </div>

          <div class="flex items-center gap-2 shrink-0">
            <span class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 shadow-sm">
              <span class="w-1.5 h-1.5 rounded-full bg-brand-500 animate-pulse"></span>
              <span>{{ mockCard.dueToday }} {{ currentLocale === 'en' ? 'due today' : 'đến hạn hôm nay' }}</span>
            </span>
          </div>
        </div>

        <!-- 3. Unboxed 2-Column Cockpit Layout (Stage 68% + Dock 32%) -->
        <div class="flex flex-col lg:flex-row gap-4 items-start">
          
          <!-- LEFT: Flashcard Hero (The ONLY Card!) -->
          <div class="w-full lg:w-[68%]">
            <div class="perspective-box w-full">
              <div
                :class="[
                  'card-flipper w-full rounded-2xl border transition-all duration-300 relative shadow-md dark:shadow-xl overflow-hidden',
                  isCardFlipped
                    ? 'border-brand-500/30 bg-white dark:bg-canvas-subtle'
                    : 'border-slate-200/90 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle'
                ]"
              >
                
                <!-- FRONT FACE: Active Recall Challenge -->
                <div v-if="!isCardFlipped" class="p-5 sm:p-7 space-y-4 sm:space-y-5">
                  <!-- Category & Seniority Tags -->
                  <div class="flex items-center justify-between gap-2 pb-3 border-b border-slate-100 dark:border-white/[0.06]">
                    <div class="flex items-center gap-2">
                      <span class="px-2.5 py-1 rounded-xl text-xs font-bold bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20">
                        {{ mockCard.front.category }}
                      </span>
                      <span class="px-2 py-0.5 rounded-lg bg-slate-100 dark:bg-white/[0.06] text-slate-600 dark:text-slate-400 text-xs font-semibold">
                        {{ mockCard.front.seniority }}
                      </span>
                    </div>
                    <span class="text-xs font-mono text-slate-400">
                      Repetition #{{ mockCard.repetitionCount }}
                    </span>
                  </div>

                  <!-- Prompt Heading -->
                  <div class="space-y-2">
                    <div class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
                      <HelpCircle class="w-3.5 h-3.5 text-brand-500" />
                      <span>Active Recall Challenge</span>
                    </div>

                    <h2 class="text-base sm:text-lg md:text-xl font-bold text-slate-900 dark:text-white leading-relaxed tracking-tight">
                      {{ mockCard.front.question }}
                    </h2>
                  </div>

                  <!-- Core Concepts -->
                  <div class="flex flex-wrap items-center gap-1.5">
                    <span class="text-[11px] text-slate-400 flex items-center gap-1 mr-1">
                      <Tag class="w-3 h-3" /> Core Concepts:
                    </span>
                    <span
                      v-for="tag in mockCard.front.conceptTags"
                      :key="tag"
                      class="px-2 py-0.5 rounded-md text-[11px] font-mono bg-slate-100 dark:bg-white/[0.04] text-slate-600 dark:text-slate-300 border border-slate-200/80 dark:border-white/[0.06]"
                    >
                      {{ tag }}
                    </span>
                  </div>

                  <!-- Source Excerpt Box -->
                  <div class="p-3 sm:p-3.5 rounded-xl bg-slate-50 dark:bg-canvas border border-slate-200/80 dark:border-white/[0.06] space-y-1">
                    <div class="flex items-center gap-1.5 text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
                      <BookOpen class="w-3 h-3 text-brand-500" />
                      <span>Original Documentation Context</span>
                    </div>
                    <p class="text-xs text-slate-600 dark:text-slate-300 italic leading-relaxed">
                      "{{ mockCard.front.sourceExcerpt }}"
                    </p>
                  </div>

                  <!-- Flip CTA Button -->
                  <div class="pt-3 border-t border-slate-100 dark:border-white/[0.06] flex items-center justify-center">
                    <button
                      @click="toggleFlip"
                      type="button"
                      class="group inline-flex items-center justify-center gap-2 px-8 py-3 rounded-2xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm shadow-md shadow-brand-500/25 transition-all cursor-pointer w-full sm:w-auto"
                    >
                      <Eye class="w-4 h-4 transition-transform group-hover:scale-110" />
                      <span>{{ currentLocale === 'en' ? 'Show Architectural Solution' : 'Xem Lời Giải Kiến Trúc' }}</span>
                      <kbd class="hidden sm:inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-mono bg-white/20 text-white">Space</kbd>
                    </button>
                  </div>
                </div>

                <!-- BACK FACE: Architectural Solution & SM-2 Rating -->
                <div v-else class="p-5 sm:p-7 space-y-4">
                  <!-- Solution Header -->
                  <div class="flex items-center justify-between gap-2 pb-2.5 border-b border-slate-100 dark:border-white/[0.06]">
                    <span class="px-2.5 py-0.5 rounded-lg text-xs font-bold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20 flex items-center gap-1">
                      <CheckCircle2 class="w-3.5 h-3.5 text-emerald-500" />
                      Verified Architecture
                    </span>
                    <button
                      @click="toggleFlip"
                      class="inline-flex items-center gap-1.5 text-xs text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 transition-colors cursor-pointer"
                    >
                      <EyeOff class="w-3.5 h-3.5" />
                      <span>Hide Answer</span>
                      <kbd class="px-1 rounded text-[10px] font-mono bg-slate-100 dark:bg-white/10">Esc</kbd>
                    </button>
                  </div>

                  <!-- Summary -->
                  <p class="text-xs sm:text-sm font-semibold text-slate-800 dark:text-slate-200 leading-relaxed">
                    {{ mockCard.back.summary }}
                  </p>

                  <!-- Technical Mechanics -->
                  <div class="space-y-2 p-3 sm:p-3.5 rounded-xl bg-slate-50 dark:bg-canvas border border-slate-200/80 dark:border-white/[0.06]">
                    <div
                      v-for="(item, idx) in mockCard.back.deepDive"
                      :key="idx"
                      class="text-xs leading-relaxed"
                    >
                      <span class="font-bold text-slate-900 dark:text-slate-100 font-mono">• {{ item.term }}:</span>
                      <span class="text-slate-600 dark:text-slate-300 ml-1">{{ item.desc }}</span>
                    </div>
                  </div>

                  <!-- Code Excerpt -->
                  <div class="rounded-xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-900 dark:bg-black/60 overflow-hidden text-xs font-mono text-slate-200 p-3">
                    <pre><code>{{ mockCard.back.codeSnippet }}</code></pre>
                  </div>

                  <!-- SM-2 Grading Pill Bar -->
                  <div class="pt-3 border-t border-slate-100 dark:border-white/[0.06] space-y-2">
                    <div class="flex items-center justify-between text-[11px] text-slate-400 px-1 font-semibold">
                      <span>Rate your recall effort (SM-2):</span>
                      <span class="font-mono">Keys [1] - [4]</span>
                    </div>

                    <div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
                      <button
                        v-for="g in sm2Forecast"
                        :key="g.grade"
                        type="button"
                        class="p-2 sm:p-2.5 rounded-xl border flex flex-col items-center justify-center transition-all cursor-pointer"
                        :class="g.color"
                      >
                        <span class="text-xs font-black">[{{ g.grade }}] {{ g.label }}</span>
                        <span class="text-[10px] opacity-80 font-mono mt-0.5">+{{ g.interval }}</span>
                      </button>
                    </div>
                  </div>
                </div>

              </div>
            </div>
          </div>

          <!-- RIGHT: Telemetry Dock (Cards sit directly on canvas) -->
          <aside class="w-full lg:w-[32%] space-y-3 sm:space-y-4">
            
            <!-- 1. Session Progress -->
            <div class="rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle p-4 shadow-sm space-y-2.5">
              <div class="flex items-center justify-between text-xs font-semibold text-slate-500 dark:text-slate-400">
                <span class="flex items-center gap-1.5">
                  <Clock class="w-3.5 h-3.5 text-brand-500" />
                  <span>{{ currentLocale === 'en' ? 'Session Progress' : 'Tiến Độ Phiên Ôn Tập' }}</span>
                </span>
                <span class="font-mono text-xs font-bold text-slate-700 dark:text-slate-300">
                  {{ mockCard.reviewedCount }} / {{ mockCard.totalSessionCards }}
                </span>
              </div>

              <div class="h-2 w-full bg-slate-100 dark:bg-white/[0.06] rounded-full overflow-hidden">
                <div
                  class="h-full bg-gradient-to-r from-brand-600 to-brand-400 transition-all duration-300"
                  :style="{ width: `${(mockCard.reviewedCount / mockCard.totalSessionCards) * 100}%` }"
                ></div>
              </div>

              <div class="flex items-center justify-between text-[11px] text-slate-400">
                <span>60% complete</span>
                <span>{{ mockCard.dueToday }} remaining</span>
              </div>
            </div>

            <!-- 2. SM-2 Algorithm Telemetry Readout -->
            <div class="rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle p-4 shadow-sm space-y-3">
              <div class="flex items-center justify-between text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
                <span class="flex items-center gap-1.5">
                  <Activity class="w-3.5 h-3.5 text-brand-500" />
                  <span>SM-2 Memory Metrics</span>
                </span>
              </div>

              <div class="grid grid-cols-2 gap-2 text-xs">
                <div class="p-2.5 rounded-xl bg-slate-50 dark:bg-white/[0.02] border border-slate-100 dark:border-white/[0.04]">
                  <span class="text-[11px] text-slate-400 block">Ease Factor</span>
                  <span class="font-mono font-bold text-slate-800 dark:text-slate-200 text-sm">
                    {{ mockCard.easeFactor.toFixed(2) }}
                  </span>
                </div>
                <div class="p-2.5 rounded-xl bg-slate-50 dark:bg-white/[0.02] border border-slate-100 dark:border-white/[0.04]">
                  <span class="text-[11px] text-slate-400 block">Current Interval</span>
                  <span class="font-mono font-bold text-slate-800 dark:text-slate-200 text-sm">
                    {{ mockCard.intervalDays }} Days
                  </span>
                </div>
              </div>

              <p class="text-[11px] text-slate-400 leading-relaxed">
                Formula: $I_n = I_{n-1} \times \text{EF}$. Next interval scales from 6d up to 21d.
              </p>
            </div>

            <!-- 3. Keyboard Shortcuts Guide -->
            <div class="rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle p-4 shadow-sm space-y-2">
              <div class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
                <Key class="w-3.5 h-3.5 text-brand-500" />
                <span>Interactive Hotkeys</span>
              </div>
              <div class="space-y-1.5 text-xs text-slate-600 dark:text-slate-400">
                <div class="flex items-center justify-between">
                  <span>Flip / Unflip Card</span>
                  <span class="font-mono text-[11px] font-bold px-1.5 py-0.5 rounded bg-slate-100 dark:bg-white/[0.06] border border-slate-200 dark:border-white/[0.08]">Space</span>
                </div>
                <div class="flex items-center justify-between">
                  <span>Grade SM-2 (1-4)</span>
                  <span class="font-mono text-[11px] font-bold px-1.5 py-0.5 rounded bg-slate-100 dark:bg-white/[0.06] border border-slate-200 dark:border-white/[0.08]">1, 2, 3, 4</span>
                </div>
                <div class="flex items-center justify-between">
                  <span>Edit Card Content</span>
                  <span class="font-mono text-[11px] font-bold px-1.5 py-0.5 rounded bg-slate-100 dark:bg-white/[0.06] border border-slate-200 dark:border-white/[0.08]">E</span>
                </div>
              </div>
            </div>

          </aside>

        </div>

      </section>

      <!-- ===================================================================== -->
      <!-- VIEW A: QUIZ ARENA STUDIO (UNBOXED DIRECT-CANVAS)                     -->
      <!-- ===================================================================== -->
      <section v-if="activeView === 'quiz-arena'" class="space-y-4">
        
        <!-- Interactive State Simulation Bar -->
        <div class="flex flex-wrap items-center justify-between gap-3 p-3 bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] rounded-2xl shadow-sm text-xs">
          <div class="flex items-center gap-2">
            <span class="font-bold text-slate-500 uppercase tracking-wider text-[11px]">Simulate Quiz State:</span>
            <button
              @click="quizStateMode = 'answering'; isQuizSubmitted = false"
              :class="[
                'px-2.5 py-1 rounded-lg border font-semibold cursor-pointer',
                quizStateMode === 'answering'
                  ? 'bg-brand-500/10 border-brand-500/40 text-brand-600 dark:text-brand-300'
                  : 'border-slate-200 dark:border-white/[0.08] text-slate-600 dark:text-slate-400'
              ]"
            >
              Answering
            </button>
            <button
              @click="quizStateMode = 'submitted-correct'; isQuizSubmitted = true; selectedQuizOption = 1"
              :class="[
                'px-2.5 py-1 rounded-lg border font-semibold cursor-pointer',
                quizStateMode === 'submitted-correct'
                  ? 'bg-emerald-500/10 border-emerald-500/40 text-emerald-600 dark:text-emerald-300'
                  : 'border-slate-200 dark:border-white/[0.08] text-slate-600 dark:text-slate-400'
              ]"
            >
              Correct
            </button>
            <button
              @click="quizStateMode = 'submitted-incorrect'; isQuizSubmitted = true; selectedQuizOption = 0"
              :class="[
                'px-2.5 py-1 rounded-lg border font-semibold cursor-pointer',
                quizStateMode === 'submitted-incorrect'
                  ? 'bg-rose-500/10 border-rose-500/40 text-rose-600 dark:text-rose-300'
                  : 'border-slate-200 dark:border-white/[0.08] text-slate-600 dark:text-slate-400'
              ]"
            >
              Incorrect
            </button>
          </div>

          <div class="flex items-center gap-2">
            <button
              @click="resetQuizDemo"
              class="px-2.5 py-1 rounded-lg border border-slate-200 dark:border-white/[0.08] text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white flex items-center gap-1 cursor-pointer"
            >
              <RotateCcw class="w-3.5 h-3.5" />
              Reset Demo
            </button>
          </div>
        </div>

        <!-- Clean Breadcrumb Bar (Direct on Canvas) -->
        <div class="flex flex-wrap items-center justify-between gap-2 px-1">
          <div class="flex items-center gap-2.5 min-w-0">
            <span class="px-2.5 py-1 rounded-lg text-xs font-black uppercase tracking-wider bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 shrink-0">
              Question {{ mockQuiz.currentQuestion }} / {{ mockQuiz.totalQuestions }}
            </span>
            <h2 class="text-sm sm:text-base font-bold text-slate-900 dark:text-white truncate">
              {{ mockQuiz.topic }}
            </h2>
          </div>

          <div class="flex items-center gap-2 shrink-0">
            <span class="px-2.5 py-1 rounded-lg text-xs font-bold bg-slate-100 dark:bg-white/[0.06] text-slate-700 dark:text-slate-300 border border-slate-200/60 dark:border-white/[0.06]">
              {{ mockQuiz.seniority }}
            </span>
          </div>
        </div>

        <!-- Unboxed 2-Column Cockpit Layout (Stage 68% + Dock 32%) -->
        <div class="flex flex-col lg:flex-row gap-4 items-start">
          
          <!-- LEFT: Quiz Action Card (The Primary Card!) -->
          <div class="w-full lg:w-[68%] rounded-2xl border border-slate-200/90 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle p-5 sm:p-7 space-y-5 shadow-sm">
            
            <!-- Question Heading -->
            <div class="space-y-2">
              <div class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
                <HelpCircle class="w-3.5 h-3.5 text-brand-500" />
                <span>Technical Interview Challenge</span>
              </div>
              <h1 class="text-base sm:text-lg md:text-xl font-bold text-slate-900 dark:text-white leading-relaxed tracking-tight">
                {{ mockQuiz.question.title }}
              </h1>
            </div>

            <!-- High-Performance Shiki Code Block Preview -->
            <div class="rounded-xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-900 dark:bg-black/60 overflow-hidden shadow-inner">
              <div class="flex items-center justify-between px-3.5 py-1.5 bg-slate-800/80 dark:bg-white/[0.04] border-b border-white/[0.06] text-xs font-mono text-slate-400">
                <span class="flex items-center gap-1.5">
                  <span class="w-2 h-2 rounded-full bg-emerald-400"></span>
                  <span>C# 13 • Kestrel.Pipes.cs</span>
                </span>
                <span class="text-[11px] text-slate-500">ReadOnlySequence&lt;T&gt;</span>
              </div>
              <pre class="p-4 text-xs font-mono text-slate-200 leading-relaxed overflow-x-auto"><code>{{ mockQuiz.question.codeSnippet }}</code></pre>
            </div>

            <!-- Option Choices Grid (Uniform tactile geometry) -->
            <div class="space-y-2.5">
              <div class="flex items-center justify-between text-xs text-slate-500 dark:text-slate-400 mb-1">
                <span>{{ currentLocale === 'en' ? 'Select the optimal answer:' : 'Chọn câu trả lời tối ưu nhất:' }}</span>
                <span class="text-[11px] font-mono">Keys [1] - [4]</span>
              </div>

              <div class="grid grid-cols-1 gap-2.5">
                <OptionCard
                  v-for="(opt, idx) in mockQuiz.question.options"
                  :key="opt.id"
                  :letter="['A', 'B', 'C', 'D'][idx]"
                  :text="opt.text"
                  :state="getOptionState(idx)"
                  :disabled="isQuizSubmitted"
                  @select="selectQuizOption(idx)"
                />
              </div>
            </div>

            <!-- Action Bar / Submit Button -->
            <div v-if="!isQuizSubmitted" class="pt-2 flex items-center justify-between">
              <span class="text-xs text-slate-500 dark:text-slate-400">
                Press <kbd class="px-1.5 py-0.5 rounded bg-slate-200 dark:bg-white/10 font-mono text-[10px] font-bold">Enter</kbd> to submit
              </span>

              <button
                @click="submitQuizAnswer"
                :disabled="selectedQuizOption === null"
                class="h-10 px-6 text-sm font-bold rounded-xl bg-brand-600 hover:bg-brand-500 text-white shadow-md shadow-brand-500/25 disabled:opacity-40 disabled:cursor-not-allowed flex items-center gap-2 transition-all cursor-pointer whitespace-nowrap shrink-0"
              >
                <Check class="w-4 h-4" :stroke-width="2.5" />
                <span>{{ currentLocale === 'en' ? 'Submit Choice' : 'Gửi Câu Trả Lời' }}</span>
              </button>
            </div>

            <!-- Explanation Box (Post-Submit) -->
            <div v-if="isQuizSubmitted" class="space-y-4 pt-4 border-t border-slate-200 dark:border-white/[0.08] animate-in fade-in duration-200">
              <div
                :class="[
                  'p-4 rounded-xl border flex items-center gap-3',
                  quizStateMode === 'submitted-correct'
                    ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-700 dark:text-emerald-300'
                    : 'bg-rose-500/10 border-rose-500/30 text-rose-700 dark:text-rose-300'
                ]"
              >
                <CheckCircle2 v-if="quizStateMode === 'submitted-correct'" class="w-5 h-5 text-emerald-500 shrink-0" :stroke-width="2" />
                <XCircle v-else class="w-5 h-5 text-rose-500 shrink-0" :stroke-width="2" />
                <div class="flex-1">
                  <p class="font-bold text-sm sm:text-base">
                    {{ quizStateMode === 'submitted-correct' ? 'Correct Choice! +180 XP' : 'Incorrect Choice — Added to Review Queue' }}
                  </p>
                  <p class="text-xs opacity-90">
                    {{ quizStateMode === 'submitted-correct' ? 'Mastered in 1st attempt. Spaced repetition card updated.' : 'Scheduled for high-frequency spaced recall today.' }}
                  </p>
                </div>
              </div>

              <div class="p-4 sm:p-5 rounded-xl border border-slate-200/80 dark:border-white/[0.06] bg-slate-50 dark:bg-canvas space-y-2">
                <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                  Architectural Deep-Dive
                </h3>
                <p class="text-xs sm:text-sm text-slate-700 dark:text-slate-300 leading-relaxed">
                  {{ mockQuiz.question.explanation }}
                </p>
              </div>

              <div class="flex justify-end pt-1">
                <button
                  @click="resetQuizDemo"
                  class="h-10 px-6 text-sm font-bold rounded-xl bg-brand-600 hover:bg-brand-500 text-white shadow-md shadow-brand-500/25 flex items-center gap-2 cursor-pointer transition-all"
                >
                  <span>Next Question</span>
                  <ArrowRight class="w-4 h-4" />
                </button>
              </div>
            </div>

          </div>

          <!-- RIGHT: Telemetry Dock -->
          <aside class="w-full lg:w-[32%] space-y-3 sm:space-y-4">
            
            <!-- 1. Live Countdown Timer -->
            <div class="rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle p-4 shadow-sm space-y-3">
              <div class="flex items-center justify-between text-xs font-semibold text-slate-500 dark:text-slate-400">
                <span class="flex items-center gap-1.5">
                  <Clock class="w-3.5 h-3.5 text-brand-500" />
                  <span>Time Remaining</span>
                </span>
                <span class="font-mono text-xs font-bold text-brand-600 dark:text-brand-400">
                  {{ mockQuiz.timeRemainingSeconds }}s
                </span>
              </div>

              <div class="h-2 w-full bg-slate-100 dark:bg-white/[0.06] rounded-full overflow-hidden">
                <div
                  class="h-full bg-gradient-to-r from-brand-600 to-amber-500 transition-all duration-300"
                  :style="{ width: `${(mockQuiz.timeRemainingSeconds / mockQuiz.timeTotalSeconds) * 100}%` }"
                ></div>
              </div>

              <div class="flex items-center justify-between text-[11px] text-slate-400">
                <span>Standard 60s arena clock</span>
                <span class="font-mono">Limit: 1 min</span>
              </div>
            </div>

            <!-- 2. Streak & Multiplier Card -->
            <div class="rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle p-4 shadow-sm flex items-center justify-between">
              <div class="flex items-center gap-3">
                <div class="w-10 h-10 rounded-xl bg-amber-500/10 text-amber-500 border border-amber-500/20 flex items-center justify-center font-bold">
                  <Flame class="w-5 h-5 fill-amber-500/20" />
                </div>
                <div>
                  <div class="flex items-center gap-1.5">
                    <span class="text-sm font-black text-slate-900 dark:text-white">Streak {{ mockQuiz.streak }}</span>
                    <span class="px-1.5 py-0.5 rounded text-[10px] font-black bg-amber-500/15 text-amber-600 dark:text-amber-400 border border-amber-500/30">
                      {{ mockQuiz.multiplier }}
                    </span>
                  </div>
                  <p class="text-[11px] text-slate-500 dark:text-slate-400">Combo Multiplier Active</p>
                </div>
              </div>
              <div class="text-right">
                <span class="text-xs font-mono font-bold text-slate-700 dark:text-slate-300">{{ mockQuiz.score }}</span>
                <p class="text-[10px] text-slate-400">Total XP</p>
              </div>
            </div>

            <!-- 3. Question Map -->
            <div class="rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle p-4 shadow-sm space-y-2.5">
              <div class="flex items-center justify-between text-xs font-semibold text-slate-500 dark:text-slate-400">
                <span class="flex items-center gap-1.5">
                  <BarChart3 class="w-3.5 h-3.5 text-brand-500" />
                  <span>Question Map</span>
                </span>
                <span class="text-[11px] font-mono text-slate-400">3/10 Answered</span>
              </div>

              <div class="grid grid-cols-5 gap-2">
                <div
                  v-for="q in questionMap"
                  :key="q.index"
                  :class="[
                    'h-8 rounded-lg flex items-center justify-center text-xs font-mono font-bold border transition-colors',
                    q.status === 'correct'
                      ? 'bg-emerald-500/15 border-emerald-500/30 text-emerald-600 dark:text-emerald-400'
                      : q.status === 'current'
                      ? 'bg-brand-500 text-white border-brand-600 shadow-sm ring-2 ring-brand-500/20'
                      : 'bg-slate-50 dark:bg-white/[0.02] border-slate-200/80 dark:border-white/[0.06] text-slate-400'
                  ]"
                >
                  {{ q.index }}
                </div>
              </div>
            </div>

            <!-- 4. Keyboard Shortcuts -->
            <div class="rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle p-4 shadow-sm space-y-2">
              <div class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
                <Key class="w-3.5 h-3.5 text-brand-500" />
                <span>Keyboard Controls</span>
              </div>
              <div class="space-y-1.5 text-xs text-slate-600 dark:text-slate-400">
                <div class="flex items-center justify-between">
                  <span>Select Options</span>
                  <span class="font-mono text-[11px] font-bold px-1.5 py-0.5 rounded bg-slate-100 dark:bg-white/[0.06] border border-slate-200 dark:border-white/[0.08]">1, 2, 3, 4</span>
                </div>
                <div class="flex items-center justify-between">
                  <span>Submit Choice</span>
                  <span class="font-mono text-[11px] font-bold px-1.5 py-0.5 rounded bg-slate-100 dark:bg-white/[0.06] border border-slate-200 dark:border-white/[0.08]">Enter</span>
                </div>
                <div class="flex items-center justify-between">
                  <span>Next Challenge</span>
                  <span class="font-mono text-[11px] font-bold px-1.5 py-0.5 rounded bg-slate-100 dark:bg-white/[0.06] border border-slate-200 dark:border-white/[0.08]">Space</span>
                </div>
              </div>
            </div>

          </aside>

        </div>

      </section>

    </main>
  </div>
</template>

<style scoped>
.perspective-box {
  perspective: 1200px;
}
</style>
