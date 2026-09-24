<script setup lang="ts">
import { computed } from 'vue'
import { Zap, Clock, Flame, ChevronRight } from 'lucide-vue-next'

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
const { locale } = useI18n()
</script>

<template>
  <div
    class="relative overflow-hidden rounded-2xl bg-gradient-to-br from-brand-600 to-indigo-700 text-white p-5 shadow-lg shadow-brand-500/10 flex flex-col justify-between h-full min-h-[190px]"
  >
    <!-- Background decorative glow -->
    <div
      class="pointer-events-none absolute -right-10 -top-10 h-36 w-36 rounded-full bg-white/10 blur-2xl"
      aria-hidden="true"
    />

    <!-- Header & Info -->
    <div class="relative z-10 space-y-2">
      <div class="flex items-center justify-between">
        <span class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-white/15 text-white/95 backdrop-blur-sm border border-white/10">
          <Flame class="w-3.5 h-3.5 text-amber-300" />
          <span>{{ $t('review.cards_due') }}</span>
        </span>

        <span v-if="dueCount > 0" class="inline-flex items-center gap-1 text-xs text-white/80 font-medium">
          <Clock class="w-3.5 h-3.5" />
          <span>~{{ estimatedMinutes }} {{ locale === 'vi' ? 'phút' : 'min' }}</span>
        </span>
      </div>

      <div class="pt-1">
        <div class="text-3xl font-black tracking-tight text-white flex items-baseline gap-2">
          <span>{{ dueCount }}</span>
          <span class="text-xs font-semibold text-white/80 lowercase">
            {{ $t('review.card_urgency_due') }}
          </span>
        </div>
        <p class="text-xs text-white/85 leading-relaxed mt-1">
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
    <div class="relative z-10 pt-3">
      <button
        type="button"
        @click="emit('startReview')"
        class="w-full h-9 px-4 rounded-xl bg-white text-brand-700 hover:bg-slate-100 font-bold text-xs sm:text-sm flex items-center justify-center gap-2 shadow-md transition-all active:scale-[0.98] cursor-pointer"
      >
        <Zap class="w-4 h-4 fill-brand-600 text-brand-600" />
        <span>{{ $t('review.start_review_btn') }}</span>
        <ChevronRight class="w-4 h-4" />
      </button>
    </div>
  </div>
</template>
