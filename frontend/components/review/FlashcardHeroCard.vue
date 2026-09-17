<script setup lang="ts">
import { computed } from 'vue'
import { Zap, Clock, Sparkles } from 'lucide-vue-next'

const props = defineProps<{
  dueCount: number
}>()

const emit = defineEmits<{
  (e: 'startReview'): void
}>()

const estimatedMinutes = computed(() => {
  if (props.dueCount <= 0) return 0
  return Math.max(1, Math.ceil(props.dueCount * 0.5))
})
</script>

<template>
  <div
    class="relative overflow-hidden rounded-3xl bg-gradient-to-br from-brand-600 to-indigo-700 text-white p-5 sm:p-6 shadow-lg shadow-brand-500/10 flex flex-col justify-between h-full min-h-[200px]"
  >
    <!-- Subtle background decorative glow -->
    <div
      class="pointer-events-none absolute -right-10 -top-10 h-40 w-40 rounded-full bg-white/10 blur-2xl"
      aria-hidden="true"
    />
    <div
      class="pointer-events-none absolute -left-10 -bottom-10 h-32 w-32 rounded-full bg-indigo-400/10 blur-xl"
      aria-hidden="true"
    />

    <!-- Header & Info -->
    <div class="relative z-10 space-y-2">
      <div class="flex items-center justify-between">
        <span class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-white/15 text-white/95 backdrop-blur-sm border border-white/10">
          <Sparkles class="w-3.5 h-3.5 text-amber-300" />
          <span>{{ $t('review.cards_due') }}</span>
        </span>

        <span v-if="dueCount > 0" class="inline-flex items-center gap-1 text-xs text-white/80 font-medium">
          <Clock class="w-3.5 h-3.5" />
          <span>~{{ estimatedMinutes }} {{ $t('roadmap.days') ? 'min' : 'min' }}</span>
        </span>
      </div>

      <div class="pt-1">
        <div class="text-3xl sm:text-4xl font-black tracking-tight text-white flex items-baseline gap-2">
          <span>{{ dueCount }}</span>
          <span class="text-sm font-semibold text-white/80 lowercase">
            {{ $t('review.forecast_cards_count', { count: '' }).trim() || 'cards' }}
          </span>
        </div>
        <p class="text-xs sm:text-sm text-white/85 leading-relaxed mt-1">
          <template v-if="dueCount > 0">
            {{ $t('review.hero_desc', { count: dueCount, minutes: estimatedMinutes }) }}
          </template>
          <template v-else>
            {{ $t('review.hero_desc_zero') }}
          </template>
        </p>
      </div>
    </div>

    <!-- 1-Click CTA Button -->
    <div class="relative z-10 pt-4">
      <button
        @click="emit('startReview')"
        class="w-full inline-flex items-center justify-center gap-2 px-4 py-3 rounded-2xl bg-white text-brand-700 hover:bg-slate-50 active:scale-[0.99] font-bold text-xs sm:text-sm transition-all shadow-md shadow-black/10 hover:shadow-lg disabled:opacity-50 cursor-pointer"
      >
        <Zap class="w-4 h-4 text-amber-500 fill-amber-500" />
        <span>{{ $t('review.start_review_btn') }}</span>
      </button>
    </div>
  </div>
</template>
