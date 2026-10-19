<script setup lang="ts">
import MasteryGaugeCard from '~/components/review/MasteryGaugeCard.vue'
import ReviewForecastChart from '~/components/review/ReviewForecastChart.vue'
import AtRiskLeechCard from '~/components/review/AtRiskLeechCard.vue'
import SourceChannelRetentionCard from '~/components/review/SourceChannelRetentionCard.vue'
import EaseFactorDistributionCard from '~/components/review/EaseFactorDistributionCard.vue'
import type { ReviewCard, SourceChannelRetention } from '~/stores/useReviewStore'
import { useToast } from '~/composables/useToast'

const toast = useToast()

const today = new Date()
function getDayOffset(offset: number): string {
  const d = new Date(today.getFullYear(), today.getMonth(), today.getDate() + offset)
  return d.toISOString().slice(0, 10)
}

const forecastOffsets = [0, 0, 0, 1, 1, 2, 2, 2, 2, 3, 4, 4, 5, 6, 6]
const mockForecastCards: ReviewCard[] = forecastOffsets.map((offset, index) => ({
  id: `f-${index + 1}`,
  nextReviewDate: getDayOffset(offset),
  status: 1,
  categoryId: null,
  category: null,
  difficulty: 1,
  repetitionCount: 1,
  easeFactor: 2.5,
  intervalDays: 1
}))

const mockSources: SourceChannelRetention[] = [
  { sourceType: 'Highlight', total: 24, learning: 2, reviewing: 4, mastered: 18, averageEaseFactor: 2.38 },
  { sourceType: 'QuizMistake', total: 16, learning: 3, reviewing: 4, mastered: 9, averageEaseFactor: 1.85 },
  { sourceType: 'DocumentChunk', total: 12, learning: 0, reviewing: 1, mastered: 11, averageEaseFactor: 2.45 }
]

function onReviewAtRisk() {
  toast.success('Navigating to at-risk & overdue flashcards')
}
</script>

<template>
  <div class="space-y-4">
    <!-- Row 1: Mastery & Forecast -->
    <div class="grid grid-cols-1 lg:grid-cols-12 gap-4">
      <div class="lg:col-span-5">
        <MasteryGaugeCard :mastered-count="38" :total-count="52" />
      </div>
      <div class="lg:col-span-7">
        <ReviewForecastChart :cards="mockForecastCards" />
      </div>
    </div>

    <!-- Row 2: Diagnostics (At-Risk, Channels, Ease Distribution) -->
    <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
      <AtRiskLeechCard
        :overdue-count="4"
        :leech-count="2"
        :at-risk-count="5"
        @review="onReviewAtRisk"
      />
      <SourceChannelRetentionCard :sources="mockSources" />
      <EaseFactorDistributionCard
        :struggling="5"
        :developing="15"
        :comfortable="32"
      />
    </div>
  </div>
</template>
