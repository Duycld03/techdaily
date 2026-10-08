<script setup lang="ts">
import { ChevronLeft, ChevronRight, CheckCircle2 } from "lucide-vue-next";
import type { ChunkSummary } from "~/stores/useLibraryStore";

interface Props {
  prevChunk: ChunkSummary | null;
  nextChunk: ChunkSummary | null;
  activeChunkIndex: number;
  totalChunks: number;
}

defineProps<Props>();

const emit = defineEmits<{
  (e: "prev-slice"): void;
  (e: "next-slice"): void;
}>();

const { t } = useI18n();
</script>

<template>
  <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
    <!-- Left Card: Previous Slice -->
    <button
      v-if="prevChunk"
      type="button"
      @click="emit('prev-slice')"
      class="glass-card group flex flex-col items-start p-4 sm:p-5 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] hover:border-slate-300 dark:hover:border-white/[0.16] hover:bg-slate-50 dark:hover:bg-canvas-elevated transition-all text-left shadow-sm hover:shadow-md active:scale-[0.99] min-w-0"
      :title="prevChunk.chapterTitle"
    >
      <div
        class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 group-hover:text-slate-700 dark:group-hover:text-slate-200 transition-colors"
      >
        <ChevronLeft
          class="w-3.5 h-3.5 transition-transform group-hover:-translate-x-1 shrink-0"
        />
        <span class="whitespace-nowrap shrink-0">{{
          $t("reader.prev_slice_card_label")
        }}</span>
      </div>
      <div
        class="w-full text-sm sm:text-base font-bold text-slate-800 dark:text-slate-200 group-hover:text-brand-600 dark:group-hover:text-brand-400 truncate mt-1.5 transition-colors"
      >
        {{ prevChunk.chapterTitle }}
      </div>
    </button>

    <!-- Right Card: Next Slice -->
    <button
      v-if="activeChunkIndex < totalChunks - 1"
      type="button"
      @click="emit('next-slice')"
      :class="[
        'group flex flex-col items-end p-4 sm:p-5 rounded-2xl border border-brand-500/30 dark:border-brand-500/20 bg-brand-50/30 dark:bg-brand-500/10 hover:bg-brand-50/60 dark:hover:bg-brand-500/20 hover:border-brand-500/60 dark:hover:border-brand-500/40 transition-all text-right shadow-sm hover:shadow-md active:scale-[0.99] min-w-0',
        { 'sm:col-start-2': !prevChunk },
      ]"
      :title="nextChunk?.chapterTitle"
    >
      <div
        class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400 transition-colors"
      >
        <span class="whitespace-nowrap shrink-0">{{
          $t("reader.next_slice_card_label")
        }}</span>
        <ChevronRight
          class="w-3.5 h-3.5 transition-transform group-hover:translate-x-1 shrink-0"
        />
      </div>
      <div
        class="w-full text-sm sm:text-base font-bold text-slate-900 dark:text-white group-hover:text-brand-600 dark:group-hover:text-brand-400 truncate mt-1.5 transition-colors"
      >
        {{ nextChunk?.chapterTitle || $t("reader.next_slice") }}
      </div>
    </button>

    <!-- Right Card Alternative: Return to Library on Final Slice -->
    <NuxtLink
      v-else
      to="/library"
      :class="[
        'group flex flex-col items-end p-4 sm:p-5 rounded-2xl border border-brand-500/30 dark:border-brand-500/20 bg-brand-50/30 dark:bg-brand-500/10 hover:bg-brand-50/60 dark:hover:bg-brand-500/20 hover:border-brand-500/60 dark:hover:border-brand-500/40 transition-all text-right shadow-sm hover:shadow-md active:scale-[0.99] min-w-0',
        { 'sm:col-start-2': !prevChunk },
      ]"
    >
      <div
        class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400 transition-colors"
      >
        <span class="whitespace-nowrap shrink-0">{{
          $t("reader.completed_card_label")
        }}</span>
        <CheckCircle2 class="w-3.5 h-3.5 text-brand-500 shrink-0" />
      </div>
      <div
        class="w-full text-sm sm:text-base font-bold text-slate-900 dark:text-white group-hover:text-brand-600 dark:group-hover:text-brand-400 group-hover:underline truncate mt-1.5 transition-colors"
      >
        {{ $t("reader.return_library") }}
      </div>
    </NuxtLink>
  </div>
</template>
