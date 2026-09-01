<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { CheckCircle, Sparkles } from 'lucide-vue-next'
import confetti from 'canvas-confetti'
import FlashcardDeck from '~/components/review/FlashcardDeck.vue'

const reviewStore = useReviewStore()

onMounted(() => {
  reviewStore.fetchReviewDeck()
})

const currentCard = computed(() => {
  return reviewStore.cards[0] || null
})

async function onGrade(score: number) {
  if (!currentCard.value) return

  await reviewStore.gradeCard(currentCard.value.id, score)

  if (reviewStore.cards.length === 0) {
    confetti({
      particleCount: 100,
      spread: 70,
      origin: { y: 0.6 }
    })
  }
}
</script>

<template>
  <div class="min-h-[calc(100vh-3.5rem)] sm:min-h-[calc(100vh-3.75rem)] p-4 sm:p-6 md:p-10 flex flex-col items-center justify-center bg-slate-50 dark:bg-slate-950 transition-colors duration-200">
    <!-- Loading -->
    <div v-if="reviewStore.isLoading" class="flex flex-col items-center gap-3 text-slate-500 dark:text-slate-400 text-sm">
      <div class="w-8 h-8 rounded-full border-2 border-brand-500 border-t-transparent animate-spin"></div>
      <span>Loading Spaced Repetition Deck...</span>
    </div>

    <!-- Active Review Deck -->
    <div v-else-if="currentCard" class="w-full max-w-2xl">
      <div class="text-center mb-8">
        <h1 class="text-2xl md:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">
          {{ $t('review.title') }}
        </h1>
        <p class="text-sm text-slate-500 dark:text-slate-400 mt-1.5">{{ $t('review.subtitle') }}</p>
      </div>

      <FlashcardDeck
        :card="currentCard"
        :remaining-count="reviewStore.cards.length"
        @grade="onGrade"
      />
    </div>

    <!-- Empty / Completed State -->
    <div v-else class="text-center max-w-md p-8 sm:p-10 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xl dark:shadow-2xl animate-in zoom-in-95 duration-200">
      <div class="w-16 h-16 rounded-2xl bg-emerald-100 dark:bg-emerald-500/20 text-emerald-600 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-500/30 flex items-center justify-center mx-auto mb-4 shadow-sm">
        <CheckCircle class="w-8 h-8" />
      </div>

      <h2 class="text-xl sm:text-2xl font-bold text-slate-900 dark:text-white mb-2">
        {{ $t('review.no_cards') }}
      </h2>

      <p class="text-sm text-slate-600 dark:text-slate-400 leading-relaxed mb-7">
        {{ $t('review.no_cards_desc') }}
      </p>

      <NuxtLink
        to="/today"
        class="inline-flex items-center gap-2 px-6 py-3.5 rounded-2xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-sm shadow-lg shadow-brand-500/20 transition-all active:scale-95"
      >
        <Sparkles class="w-4 h-4" />
        <span>{{ $t('review.continue_drill') }}</span>
      </NuxtLink>
    </div>
  </div>
</template>
