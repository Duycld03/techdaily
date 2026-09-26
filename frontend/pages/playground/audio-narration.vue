<script setup lang="ts">
import ReaderAudioPlayer from '~/components/reader/ReaderAudioPlayer.vue'
import type { ChunkSummary } from '~/stores/useLibraryStore'

// Isolated visual harness for the reader audio narration control bar.
// Exercises the AI-formatted (EN + VI voice) and not-AI-formatted states, plus a
// locale toggle so the bilingual button/label layout can be inspected.
const { locale, setLocale } = useI18n()

const base: ChunkSummary = {
  id: 'demo-en',
  chunkOrder: 4,
  chapterTitle: 'Dependency Injection in .NET 10',
  summaryMarkdown: '',
  originalTextMarkdown: 'Dependency injection decouples construction from use. First sentence. Second sentence.',
  keyTakeaways: [],
  estimatedReadMinutes: 4,
  isAiFormatted: true,
  language: 'en'
}
const enChunk = base
const viChunk: ChunkSummary = { ...base, id: 'demo-vi', chapterTitle: 'Trích Đoạn Tài Liệu Gốc', language: 'vi' }
const rawChunk: ChunkSummary = { ...base, id: 'demo-raw', isAiFormatted: false }
</script>

<template>
  <div class="min-h-screen bg-slate-50 dark:bg-canvas text-slate-900 dark:text-white">
    <div class="mx-auto w-full max-w-3xl px-4 sm:px-6 py-10 space-y-8">
      <header class="space-y-3">
        <h1 class="text-xl sm:text-2xl font-extrabold tracking-tight">
          Reader Audio Narration — Playground
        </h1>
        <div class="flex items-center gap-2">
          <button
            type="button"
            class="rounded-lg border border-slate-300 dark:border-white/10 px-3 py-1.5 text-sm font-semibold"
            @click="setLocale('en')"
          >
            EN
          </button>
          <button
            type="button"
            class="rounded-lg border border-slate-300 dark:border-white/10 px-3 py-1.5 text-sm font-semibold"
            @click="setLocale('vi')"
          >
            VI
          </button>
          <span class="text-sm text-slate-500 dark:text-slate-400">active locale: {{ locale }}</span>
        </div>
      </header>

      <section class="space-y-3">
        <p class="text-sm font-bold uppercase tracking-wider text-brand-500">
          AI-formatted slice — English voice
        </p>
        <ReaderAudioPlayer :chunk="enChunk" />
      </section>

      <section class="space-y-3">
        <p class="text-sm font-bold uppercase tracking-wider text-brand-500">
          AI-formatted slice — Vietnamese voice
        </p>
        <ReaderAudioPlayer :chunk="viChunk" />
      </section>

      <section class="space-y-3">
        <p class="text-sm font-bold uppercase tracking-wider text-brand-500">
          Not AI-formatted — narration control is hidden
        </p>
        <ReaderAudioPlayer :chunk="rawChunk" />
        <p class="text-sm text-slate-500 dark:text-slate-400">
          Nothing renders above: narration is gated on AI-formatted slices.
        </p>
      </section>
    </div>
  </div>
</template>
