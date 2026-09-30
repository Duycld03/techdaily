<script setup lang="ts">
import { ref } from 'vue'
import Sm2GradingButtons from '~/components/review/Sm2GradingButtons.vue'
import FlashcardBentoCard from '~/components/review/FlashcardBentoCard.vue'
import type { ReviewCard } from '~/stores/useReviewStore'
import { useToast } from '~/composables/useToast'

const toast = useToast()

const lastScore = ref<number | null>(null)
const lastLabel = ref<string | null>(null)

const scoreMap: Record<number, string> = {
  1: 'Again (< 1m)',
  3: 'Hard (8m)',
  4: 'Good (1d)',
  5: 'Easy (4d)'
}

function onGrade(score: number) {
  lastScore.value = score
  lastLabel.value = scoreMap[score] ?? `${score}`
  toast.success(`SM-2 recall graded: ${lastLabel.value}`)
}

const todayStr = new Date().toISOString().slice(0, 10)
const futureDate = new Date(Date.now() + 86400000 * 5).toISOString().slice(0, 10)

const sampleCards: ReviewCard[] = [
  {
    id: 'card-1',
    question: 'What is the primary difference between WAL (Write-Ahead Logging) and LSM-Trees?',
    answer: 'WAL enforces immediate durability by appending changes sequentially before updating memory, while LSM-trees batch writes in memory (memtable) and flush sequentially into immutable SSTables with background compaction.',
    easeFactor: 2.10,
    intervalDays: 1,
    repetitionCount: 2,
    status: 1,
    nextReviewDate: todayStr,
    topicTitle: 'Storage Engines',
    sourceType: 'Highlight'
  },
  {
    id: 'card-2',
    question: 'How does Optimistic Concurrency Control (OCC) prevent lost updates?',
    answer: 'OCC allows concurrent transactions without acquiring locks during execution, validating read-set and write-set versions at commit time and aborting transactions on conflict.',
    easeFactor: 1.65,
    intervalDays: 3,
    repetitionCount: 4,
    status: 1,
    nextReviewDate: futureDate,
    topicTitle: 'Database Transactions',
    sourceType: 'QuizMistake'
  },
  {
    id: 'card-3',
    question: 'What quorum condition must be satisfied in Paxos/Raft to commit an entry?',
    answer: 'An entry is committed when written to a strict majority of nodes (floor(N/2) + 1). Any two majorities overlap by at least one node, ensuring the committed entry persists into subsequent terms.',
    easeFactor: 2.50,
    intervalDays: 14,
    repetitionCount: 6,
    status: 2,
    nextReviewDate: futureDate,
    topicTitle: 'Distributed Systems',
    sourceType: 'DocumentChunk'
  }
]

function onCardAction(action: string, card: ReviewCard) {
  toast.success(`Action '${action}' on card: ${card.question.slice(0, 30)}...`)
}
</script>

<template>
  <div class="space-y-6">
    <!-- Interactive SM-2 Grading Buttons -->
    <div class="glass-card p-4 space-y-3.5">
      <div class="flex flex-wrap items-center justify-between gap-2">
        <p class="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
          Interactive SM-2 Recall Rating Controls (Sm2GradingButtons)
        </p>
        <span
          v-if="lastScore"
          class="inline-flex items-center gap-1.5 rounded-full bg-brand-500/10 px-2.5 py-0.5 text-xs font-bold text-brand-600 dark:text-brand-400 border border-brand-500/20 tabular-nums"
        >
          {{ $t('showcase.interactive.graded_toast', { score: lastScore, label: lastLabel }) }}
        </span>
        <span
          v-else
          class="text-xs text-slate-400 dark:text-slate-500 italic"
        >
          {{ $t('showcase.interactive.click_hint') }}
        </span>
      </div>

      <div class="rounded-xl border border-slate-200/80 bg-slate-50/50 p-4 dark:border-white/[0.06] dark:bg-canvas-subtle">
        <Sm2GradingButtons @grade="onGrade" />
      </div>
    </div>

    <!-- Flashcard Bento Cards -->
    <div class="glass-card p-4 space-y-3.5">
      <p class="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
        Flashcard Inventory Cards (Due Today, Learning, Mastered Tiers)
      </p>

      <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
        <FlashcardBentoCard
          v-for="card in sampleCards"
          :key="card.id"
          :card="card"
          @edit="onCardAction('Edit', card)"
          @reset="onCardAction('Reset', card)"
          @delete="onCardAction('Delete', card)"
        />
      </div>
    </div>
  </div>
</template>
