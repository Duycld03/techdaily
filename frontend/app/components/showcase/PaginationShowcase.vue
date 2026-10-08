<script setup lang="ts">
import { ref } from 'vue'
import BasePagination from '~/components/common/BasePagination.vue'
import { useToast } from '~/composables/useToast'

const toast = useToast()

const activePage1 = ref(2)
const totalPages1 = ref(8)
const totalCount1 = ref(118)
const pageSize1 = ref(15)

const activePage2 = ref(4)
const totalPages2 = ref(12)
const totalCount2 = ref(240)
const pageSize2 = ref(20)

function onPageChange1(page: number) {
  activePage1.value = page
  toast.success(`Navigated to page ${page}`)
}

function onPageChange2(page: number) {
  activePage2.value = page
  toast.success(`Navigated to page ${page}`)
}
</script>

<template>
  <div class="space-y-4">
    <!-- Interactive Demo 1: Full Summary Variant -->
    <div class="glass-card p-4 space-y-3.5">
      <div class="flex flex-wrap items-center justify-between gap-2">
        <p class="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
          Standard 1-Based Pagination (With Page Jump & Summary)
        </p>
        <span class="inline-flex items-center gap-1.5 rounded-full bg-brand-500/10 px-2.5 py-0.5 text-xs font-bold text-brand-600 dark:text-brand-400 border border-brand-500/20 tabular-nums">
          {{ $t('showcase.interactive.selected_page', { page: activePage1, total: totalPages1 }) }}
        </span>
      </div>

      <div class="rounded-xl border border-slate-200/80 bg-slate-50/50 p-3 dark:border-white/[0.06] dark:bg-canvas-subtle">
        <BasePagination
          :current-page="activePage1"
          :total-pages="totalPages1"
          :total-count="totalCount1"
          :page-size="pageSize1"
          :show-summary="true"
          @update:current-page="(p) => (activePage1 = p)"
          @change="onPageChange1"
        />
      </div>
    </div>

    <!-- Interactive Demo 2: Compact Item Range Variant -->
    <div class="glass-card p-4 space-y-3.5">
      <div class="flex flex-wrap items-center justify-between gap-2">
        <p class="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
          Compact Variant (With Item Range Summary)
        </p>
        <span class="inline-flex items-center gap-1.5 rounded-full bg-emerald-500/10 px-2.5 py-0.5 text-xs font-bold text-emerald-600 dark:text-emerald-400 border border-emerald-500/20 tabular-nums">
          {{ $t('showcase.interactive.selected_page', { page: activePage2, total: totalPages2 }) }}
        </span>
      </div>

      <div class="rounded-xl border border-slate-200/80 bg-slate-50/50 p-3 dark:border-white/[0.06] dark:bg-canvas-subtle">
        <BasePagination
          :current-page="activePage2"
          :total-pages="totalPages2"
          :total-count="totalCount2"
          :page-size="pageSize2"
          :show-item-summary="true"
          @update:current-page="(p) => (activePage2 = p)"
          @change="onPageChange2"
        />
      </div>
    </div>
  </div>
</template>
