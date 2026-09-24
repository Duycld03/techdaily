<script setup lang="ts">
import { ref } from "vue";
import { onClickOutside } from "@vueuse/core";
import {
  ArrowLeft,
  List,
  HelpCircle,
  ChevronLeft,
  ChevronRight,
} from "lucide-vue-next";
import type { BookDetail, ChunkSummary } from "~/stores/useLibraryStore";
import ThemeToggle from "~/components/common/ThemeToggle.vue";
import { useReaderTypography } from "~/composables/useReaderTypography";

interface Props {
  book: BookDetail | null;
  currentChunk: ChunkSummary | null;
  activeChunkIndex: number;
  totalChunks: number;
  isTocOpen: boolean;
  progressPercentage: number;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  (e: "toggle-toc"): void;
  (e: "open-mobile-toc"): void;
  (e: "prev-slice"): void;
  (e: "next-slice"): void;
}>();

const { t } = useI18n();

const {
  typography,
  fontSizes,
  fontScalePercentages,
  currentFontSizeIndex,
  canDecreaseFontSize,
  canIncreaseFontSize,
  decreaseFontSize,
  increaseFontSize,
} = useReaderTypography();

const isTypographyOpen = ref(false);
const typographyDropdownRef = ref<HTMLElement | null>(null);

onClickOutside(typographyDropdownRef, () => {
  if (isTypographyOpen.value) {
    isTypographyOpen.value = false;
  }
});
</script>

<template>
  <header
    class="h-14 sm:h-15 px-3 sm:px-6 border-b border-slate-200/80 dark:border-white/[0.08] bg-white/90 dark:bg-canvas/90 backdrop-blur-md flex items-center justify-between shrink-0 gap-2 sm:gap-4 z-20"
  >
    <!-- Left: Back to Library & TOC Toggle -->
    <div class="flex items-center gap-1.5 sm:gap-2.5 shrink-0">
      <NuxtLink
        to="/library"
        class="flex items-center gap-1 sm:gap-1.5 px-2.5 sm:px-3 py-1.5 rounded-xl border border-slate-200/80 dark:border-white/[0.08] text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-canvas-elevated transition-colors shrink-0"
        :title="$t('reader.return_library')"
      >
        <ArrowLeft class="w-4 h-4 shrink-0" />
        <span class="hidden sm:inline">{{ $t("reader.library") }}</span>
      </NuxtLink>

      <!-- Desktop TOC Toggle -->
      <button
        type="button"
        @click="emit('toggle-toc')"
        :class="[
          'hidden md:flex items-center gap-1.5 px-3 py-1.5 rounded-xl border text-xs sm:text-sm font-semibold transition-colors shrink-0',
          isTocOpen
            ? 'border-brand-300 dark:border-brand-500/30 bg-brand-50 dark:bg-brand-500/10 text-brand-700 dark:text-brand-400 font-bold'
            : 'border-slate-200/80 dark:border-white/[0.08] text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-canvas-elevated',
        ]"
        :title="isTocOpen ? $t('reader.close_toc') : $t('reader.open_toc')"
      >
        <List class="w-4 h-4 shrink-0" />
        <span>{{ $t("reader.contents") }}</span>
      </button>

      <!-- Mobile TOC Drawer Button -->
      <button
        type="button"
        @click="emit('open-mobile-toc')"
        class="md:hidden flex items-center gap-1 px-2.5 py-1.5 rounded-xl border border-slate-200/80 dark:border-white/[0.08] text-xs font-semibold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-canvas-elevated transition-colors shrink-0"
        :title="$t('reader.open_toc')"
      >
        <List class="w-3.5 h-3.5 shrink-0" />
        <span>{{ $t("reader.contents") }}</span>
      </button>
    </div>

    <!-- Center: Book Title & Active Chapter Indicator -->
    <div class="flex-1 min-w-0 text-center px-1 sm:px-3">
      <h1
        class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white truncate max-w-[140px] sm:max-w-xs md:max-w-md mx-auto"
      >
        {{ book?.title || "Technical Document" }}
      </h1>
      <p
        v-if="currentChunk"
        class="text-[11px] sm:text-xs text-slate-500 dark:text-slate-400 truncate max-w-[180px] sm:max-w-sm mx-auto"
      >
        <span class="hidden sm:inline">{{
          $t("reader.slice_of", {
            current: currentChunk.chunkOrder,
            total: totalChunks,
            chapter: currentChunk.chapterTitle,
          })
        }}</span>
        <span
          class="sm:hidden font-semibold text-brand-600 dark:text-brand-400"
          >{{
            $t("reader.slice_badge", {
              current: currentChunk.chunkOrder,
              total: totalChunks,
            })
          }}</span
        >
      </p>
    </div>

    <!-- Right: Quiz Chapter, ThemeToggle, Progress & Quick Nav -->
    <div class="flex items-center gap-1.5 sm:gap-2.5 shrink-0">
      <!-- 1-Click Quiz Chapter Action -->
      <NuxtLink
        v-if="currentChunk"
        :to="{
          path: '/quiz',
          query: {
            topic: currentChunk.chapterTitle || book?.title,
            bookId: book?.id,
            grounded: 'true',
          },
        }"
        class="hidden sm:inline-flex items-center gap-1.5 px-2.5 sm:px-3 py-1.5 rounded-xl bg-brand-50 dark:bg-brand-500/10 border border-brand-200/80 dark:border-brand-500/20 text-brand-700 dark:text-brand-400 hover:bg-brand-100 dark:hover:bg-brand-500/20 text-xs font-bold transition-colors shrink-0"
        :title="$t('reader.quiz_chapter_btn')"
      >
        <HelpCircle class="w-3.5 h-3.5 shrink-0" />
        <span class="hidden md:inline">{{
          $t("reader.quiz_chapter_btn")
        }}</span>
      </NuxtLink>

      <!-- Novel-Style Typography Popover -->
      <div ref="typographyDropdownRef" class="relative">
        <button
          type="button"
          @click.stop="isTypographyOpen = !isTypographyOpen"
          :class="[
            'px-2.5 sm:px-3 py-1.5 rounded-xl border text-xs font-bold transition-all flex items-center gap-1.5 shrink-0',
            isTypographyOpen
              ? 'bg-brand-600 text-white border-transparent shadow-sm'
              : 'bg-white dark:bg-canvas-subtle text-slate-700 dark:text-slate-300 border-slate-200/80 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-canvas-elevated'
          ]"
          :title="$t('reader.typography_settings')"
        >
          <span class="font-serif text-sm font-black">Aa</span>
        </button>

        <!-- Typography Popover Dropdown (click-outside dismissed) -->
        <div
          v-if="isTypographyOpen"
          class="absolute right-0 mt-2 w-80 sm:w-84 max-w-[calc(100vw-1.5rem)] p-4 bg-white/95 dark:bg-canvas-elevated/95 backdrop-blur-xl rounded-2xl border border-slate-200/80 dark:border-white/[0.08] shadow-2xl z-50 space-y-4 text-xs select-none"
        >
          <!-- Section 1: Font Size -->
          <div class="space-y-2">
            <div class="flex items-center justify-between text-slate-500 dark:text-slate-400 font-semibold">
              <span>{{ $t('reader.font_size') }}</span>
              <span class="font-mono text-xs font-bold text-slate-700 dark:text-slate-200">
                {{ fontScalePercentages[typography.fontSize] }}
              </span>
            </div>
            <div class="flex items-center justify-between gap-2 p-1 bg-slate-100 dark:bg-canvas-subtle rounded-xl border border-slate-200/60 dark:border-white/[0.06]">
              <button
                type="button"
                @click="decreaseFontSize"
                :disabled="!canDecreaseFontSize"
                class="flex-1 py-1.5 px-3 rounded-lg font-serif font-bold text-xs flex items-center justify-center gap-1 transition-all disabled:opacity-30 disabled:cursor-not-allowed hover:bg-white dark:hover:bg-canvas-elevated text-slate-700 dark:text-slate-300 shadow-none hover:shadow-sm"
                :title="$t('reader.font_smaller')"
              >
                <span class="text-xs font-bold">A</span>
                <span class="text-[10px] font-mono">−</span>
              </button>
              <div class="flex items-center gap-1.5 px-2">
                <span
                  v-for="(size, idx) in fontSizes"
                  :key="size"
                  class="w-1.5 h-1.5 rounded-full transition-all"
                  :class="[
                    typography.fontSize === size
                      ? 'w-2 h-2 bg-brand-500 scale-110'
                      : (idx < currentFontSizeIndex ? 'bg-slate-400 dark:bg-slate-500' : 'bg-slate-300 dark:bg-slate-700')
                  ]"
                />
              </div>
              <button
                type="button"
                @click="increaseFontSize"
                :disabled="!canIncreaseFontSize"
                class="flex-1 py-1.5 px-3 rounded-lg font-serif font-bold text-sm flex items-center justify-center gap-1 transition-all disabled:opacity-30 disabled:cursor-not-allowed hover:bg-white dark:hover:bg-canvas-elevated text-slate-700 dark:text-slate-300 shadow-none hover:shadow-sm"
                :title="$t('reader.font_larger')"
              >
                <span class="text-sm font-black">A</span>
                <span class="text-[10px] font-mono">+</span>
              </button>
            </div>
          </div>

          <!-- Section 2: Font Family -->
          <div class="space-y-2">
            <span class="text-slate-500 dark:text-slate-400 font-semibold block">{{ $t('reader.font_family') }}</span>
            <div class="grid grid-cols-3 gap-1.5 p-1 bg-slate-100 dark:bg-canvas-subtle rounded-xl border border-slate-200/60 dark:border-white/[0.06]">
              <button
                type="button"
                @click="typography.fontFamily = 'sans'"
                class="py-2 px-2 rounded-lg font-sans font-medium text-xs transition-all text-center truncate"
                :class="[
                  typography.fontFamily === 'sans'
                    ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm border border-slate-200/80 dark:border-white/[0.12]'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white border border-transparent'
                ]"
              >
                {{ $t('reader.font_sans') }}
              </button>
              <button
                type="button"
                @click="typography.fontFamily = 'serif'"
                class="py-2 px-2 rounded-lg font-serif font-medium text-xs transition-all text-center truncate"
                :class="[
                  typography.fontFamily === 'serif'
                    ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm border border-slate-200/80 dark:border-white/[0.12]'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white border border-transparent'
                ]"
              >
                {{ $t('reader.font_serif') }}
              </button>
              <button
                type="button"
                @click="typography.fontFamily = 'mono'"
                class="py-2 px-2 rounded-lg font-mono font-medium text-xs transition-all text-center truncate"
                :class="[
                  typography.fontFamily === 'mono'
                    ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm border border-slate-200/80 dark:border-white/[0.12]'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white border border-transparent'
                ]"
              >
                {{ $t('reader.font_mono') }}
              </button>
            </div>
          </div>

          <!-- Section 3: Line Spacing -->
          <div class="space-y-2">
            <span class="text-slate-500 dark:text-slate-400 font-semibold block">{{ $t('reader.line_spacing') }}</span>
            <div class="grid grid-cols-3 gap-1.5 p-1 bg-slate-100 dark:bg-canvas-subtle rounded-xl border border-slate-200/60 dark:border-white/[0.06]">
              <button
                type="button"
                @click="typography.lineSpacing = 'normal'"
                class="py-1.5 px-2 rounded-lg font-medium text-xs transition-all text-center truncate"
                :class="[
                  typography.lineSpacing === 'normal'
                    ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm border border-slate-200/80 dark:border-white/[0.12]'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white border border-transparent'
                ]"
              >
                {{ $t('reader.spacing_normal') }}
              </button>
              <button
                type="button"
                @click="typography.lineSpacing = 'relaxed'"
                class="py-1.5 px-2 rounded-lg font-medium text-xs transition-all text-center truncate"
                :class="[
                  typography.lineSpacing === 'relaxed'
                    ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm border border-slate-200/80 dark:border-white/[0.12]'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white border border-transparent'
                ]"
              >
                {{ $t('reader.spacing_relaxed') }}
              </button>
              <button
                type="button"
                @click="typography.lineSpacing = 'loose'"
                class="py-1.5 px-2 rounded-lg font-medium text-xs transition-all text-center truncate"
                :class="[
                  typography.lineSpacing === 'loose'
                    ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm border border-slate-200/80 dark:border-white/[0.12]'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white border border-transparent'
                ]"
              >
                {{ $t('reader.spacing_loose') }}
              </button>
            </div>
          </div>

          <!-- Section 4: Reading Column Width -->
          <div class="space-y-2">
            <span class="text-slate-500 dark:text-slate-400 font-semibold block">{{ $t('reader.reading_width') }}</span>
            <div class="grid grid-cols-3 gap-1.5 p-1 bg-slate-100 dark:bg-canvas-subtle rounded-xl border border-slate-200/60 dark:border-white/[0.06]">
              <button
                type="button"
                @click="typography.readingWidth = 'standard'"
                class="py-1.5 px-2 rounded-lg font-medium text-xs transition-all text-center truncate"
                :class="[
                  typography.readingWidth === 'standard'
                    ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm border border-slate-200/80 dark:border-white/[0.12]'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white border border-transparent'
                ]"
              >
                {{ $t('reader.width_standard') }}
              </button>
              <button
                type="button"
                @click="typography.readingWidth = 'wide'"
                class="py-1.5 px-2 rounded-lg font-medium text-xs transition-all text-center truncate"
                :class="[
                  typography.readingWidth === 'wide'
                    ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm border border-slate-200/80 dark:border-white/[0.12]'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white border border-transparent'
                ]"
              >
                {{ $t('reader.width_wide') }}
              </button>
              <button
                type="button"
                @click="typography.readingWidth = 'full'"
                class="py-1.5 px-2 rounded-lg font-medium text-xs transition-all text-center truncate"
                :class="[
                  typography.readingWidth === 'full'
                    ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 font-bold shadow-sm border border-slate-200/80 dark:border-white/[0.12]'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white border border-transparent'
                ]"
              >
                {{ $t('reader.width_full') }}
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Theme Toggle -->
      <ThemeToggle />

      <!-- Progress Bar (Desktop) -->
      <div class="hidden lg:flex items-center gap-2">
        <div
          class="w-20 xl:w-28 h-2 rounded-full bg-slate-200/80 dark:bg-canvas-subtle border border-slate-300/40 dark:border-white/[0.06] overflow-hidden"
        >
          <div
            class="h-full bg-brand-500 rounded-full transition-all duration-300"
            :style="{ width: `${progressPercentage}%` }"
          ></div>
        </div>
        <span
          class="text-xs font-bold text-brand-700 dark:text-brand-400 tabular-nums"
        >
          {{ progressPercentage }}%
        </span>
      </div>

      <!-- Quick Slice Prev/Next -->
      <div class="flex items-center gap-0.5 sm:gap-1">
        <button
          type="button"
          @click="emit('prev-slice')"
          :disabled="activeChunkIndex <= 0"
          class="p-1.5 rounded-lg border border-slate-200/80 dark:border-white/[0.08] text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-canvas-elevated disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
          :title="$t('reader.prev_slice_hint')"
        >
          <ChevronLeft class="w-4 h-4" />
        </button>
        <button
          type="button"
          @click="emit('next-slice')"
          :disabled="activeChunkIndex >= totalChunks - 1"
          class="p-1.5 rounded-lg border border-slate-200/80 dark:border-white/[0.08] text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-canvas-elevated disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
          :title="$t('reader.next_slice_hint')"
        >
          <ChevronRight class="w-4 h-4" />
        </button>
      </div>
    </div>
  </header>
</template>
