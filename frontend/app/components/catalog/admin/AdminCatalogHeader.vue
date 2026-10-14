<script setup lang="ts">
import { ShieldCheck, RefreshCw, Plus } from 'lucide-vue-next'

defineProps<{
  isLoading?: boolean
  isCreatingDraft?: boolean
}>()

defineEmits<{
  (e: 'refresh'): void
  (e: 'new-draft'): void
}>()
</script>

<template>
  <div class="flex w-full flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
    <div class="flex min-w-0 flex-1 items-start gap-3">
      <div class="w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0">
        <ShieldCheck class="w-5 h-5 sm:w-6 sm:h-6" :stroke-width="1.5" />
      </div>
      <div class="min-w-0">
        <h1 class="text-lg sm:text-xl font-black tracking-tight text-slate-900 dark:text-white">
          {{ $t('admin_catalog.title') }}
        </h1>
        <p class="mt-1 text-sm sm:text-base leading-relaxed text-slate-500 dark:text-slate-400">
          {{ $t('admin_catalog.subtitle') }}
        </p>
      </div>
    </div>

    <div class="flex w-full items-center justify-end gap-2.5 lg:w-auto shrink-0">
      <button
        type="button"
        data-testid="admin-catalog-refresh-btn"
        @click="$emit('refresh')"
        class="inline-flex min-h-11 items-center gap-1.5 px-3 py-2 text-sm sm:text-base font-semibold rounded-xl text-slate-600 dark:text-slate-400 hover:bg-slate-200/60 dark:hover:bg-white/[0.06] border border-slate-200/80 dark:border-white/[0.08] transition-colors whitespace-nowrap shrink-0 cursor-pointer"
        :title="$t('admin_catalog.refresh_title')"
        :aria-label="$t('admin_catalog.refresh_title')"
      >
        <RefreshCw :class="['w-4 h-4 shrink-0', isLoading && 'animate-spin']" />
        <span class="whitespace-nowrap">{{ $t('admin_catalog.refresh') }}</span>
      </button>

      <button
        v-if="!isCreatingDraft"
        type="button"
        data-testid="admin-catalog-new-draft-btn"
        @click="$emit('new-draft')"
        class="inline-flex min-h-11 items-center gap-2 px-3.5 sm:px-4 py-2 text-sm sm:text-base font-semibold rounded-xl bg-brand-600 hover:bg-brand-500 text-white shadow-md shadow-brand-500/20 transition-all active:scale-95 whitespace-nowrap shrink-0 cursor-pointer"
      >
        <Plus class="w-4 h-4 shrink-0" :stroke-width="2" />
        <span class="whitespace-nowrap">{{ $t('admin_catalog.new_draft') }}</span>
      </button>
    </div>
  </div>
</template>
