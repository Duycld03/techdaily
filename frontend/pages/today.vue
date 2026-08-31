<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { BookOpen, Terminal, Sparkles } from 'lucide-vue-next'
import DocReaderPane from '~/components/today/DocReaderPane.vue'
import InterviewChallengePane from '~/components/today/InterviewChallengePane.vue'

const focusStore = useDailyFocusStore()
const { locale } = useI18n()
const activeMobileTab = ref<'reader' | 'challenge'>('reader')

onMounted(() => {
  focusStore.fetchTodayFocus(undefined, locale.value)
})

watch(locale, (newLocale) => {
  focusStore.fetchTodayFocus(undefined, newLocale)
})
</script>

<template>
  <div class="h-[calc(100vh-3.5rem)] flex flex-col overflow-hidden">
    <!-- Loading State -->
    <div v-if="focusStore.isLoading" class="flex-1 flex items-center justify-center">
      <div class="flex flex-col items-center gap-3 text-slate-400 text-sm">
        <div class="w-8 h-8 rounded-full border-2 border-brand-500 border-t-transparent animate-spin"></div>
        <span>Loading Today's Senior Focus...</span>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="focusStore.error" class="flex-1 flex items-center justify-center p-6">
      <div class="p-6 rounded-2xl bg-red-950/40 border border-red-900 text-center max-w-md">
        <p class="text-sm text-red-300 mb-4">{{ focusStore.error }}</p>
        <button
          @click="focusStore.fetchTodayFocus()"
          class="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-white text-xs font-semibold"
        >
          Retry
        </button>
      </div>
    </div>

    <!-- Main Dual-Pane Content -->
    <div v-else-if="focusStore.data" class="flex-1 flex flex-col md:flex-row overflow-hidden">
      <!-- Mobile Tab Switcher -->
      <div class="md:hidden flex border-b border-slate-800 bg-slate-950 shrink-0">
        <button
          @click="activeMobileTab = 'reader'"
          :class="[
            'flex-1 py-3 text-xs font-bold flex items-center justify-center gap-2 border-b-2 transition-colors',
            activeMobileTab === 'reader'
              ? 'border-brand-500 text-brand-400 bg-slate-900/40'
              : 'border-transparent text-slate-400'
          ]"
        >
          <BookOpen class="w-4 h-4" />
          <span>{{ $t('today.doc_reader') }}</span>
        </button>

        <button
          @click="activeMobileTab = 'challenge'"
          :class="[
            'flex-1 py-3 text-xs font-bold flex items-center justify-center gap-2 border-b-2 transition-colors',
            activeMobileTab === 'challenge'
              ? 'border-brand-500 text-brand-400 bg-slate-900/40'
              : 'border-transparent text-slate-400'
          ]"
        >
          <Terminal class="w-4 h-4" />
          <span>{{ $t('today.interview_challenge') }}</span>
        </button>
      </div>

      <!-- Left Pane: Doc Reader & Source Context (50% on Desktop) -->
      <div
        :class="[
          'md:w-1/2 md:border-r border-slate-800/80 h-full overflow-hidden',
          activeMobileTab === 'reader' ? 'flex-1 flex flex-col' : 'hidden md:flex md:flex-col'
        ]"
      >
        <DocReaderPane
          :topic="focusStore.data.topic"
          :document-chunk="focusStore.data.documentChunk"
        />
      </div>

      <!-- Right Pane: Interview Scenario Challenge & AI Evaluator (50% on Desktop) -->
      <div
        :class="[
          'md:w-1/2 h-full overflow-hidden',
          activeMobileTab === 'challenge' ? 'flex-1 flex flex-col' : 'hidden md:flex md:flex-col'
        ]"
      >
        <InterviewChallengePane
          :question="focusStore.data.question"
          :drill="focusStore.data.drill"
        />
      </div>
    </div>
  </div>
</template>
