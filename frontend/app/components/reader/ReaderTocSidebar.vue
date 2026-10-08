<script setup lang="ts">
import { ref, computed } from "vue";
import {
  BookOpen,
  CheckCircle2,
  Clock,
  Download,
  X,
  Search,
} from "lucide-vue-next";
import type { BookDetail, ChunkSummary } from "~/stores/useLibraryStore";

interface Props {
  book: BookDetail | null;
  activeChunkIndex: number;
  completedSlices: Set<number>;
  totalChunks: number;
  isDesktopOpen: boolean;
  isMobileOpen: boolean;
  isExportingMarkdown?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  isExportingMarkdown: false,
});

const emit = defineEmits<{
  (e: "select-chunk", index: number): void;
  (e: "close-mobile-toc"): void;
  (e: "export-markdown"): void;
}>();

const { t } = useI18n();
const searchQuery = ref("");

interface ChunkWithIndex extends ChunkSummary {
  originalIndex: number;
}

const filteredChunks = computed<ChunkWithIndex[]>(() => {
  const chunks = props.book?.chunks || [];
  const mapped = chunks.map((c, idx) => ({ ...c, originalIndex: idx }));
  const query = searchQuery.value.trim().toLowerCase();
  if (!query) return mapped;
  return mapped.filter((c) => c.chapterTitle.toLowerCase().includes(query));
});
</script>

<template>
  <!-- Desktop Table of Contents Sidebar (Collapsible) -->
  <aside
    v-if="isDesktopOpen"
    class="hidden md:flex w-72 lg:w-80 border-r border-slate-200/80 dark:border-white/[0.08] bg-slate-50/70 dark:bg-canvas-subtle/70 flex-col shrink-0 overflow-y-auto"
  >
    <div
      class="p-4 border-b border-slate-200/80 dark:border-white/[0.06] flex items-center justify-between"
    >
      <div
        class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400"
      >
        <BookOpen class="w-3.5 h-3.5" />
        <span>{{ $t("reader.toc") }}</span>
      </div>
      <span class="text-xs font-semibold text-slate-400">
        {{
          $t("reader.done", {
            count: completedSlices.size,
            total: totalChunks,
          })
        }}
      </span>
    </div>

    <!-- Chapter Search Filter -->
    <div class="p-2 border-b border-slate-200/80 dark:border-white/[0.06]">
      <div class="relative">
        <Search
          class="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-400"
        />
        <input
          v-model="searchQuery"
          type="text"
          :placeholder="$t('reader.search_placeholder')"
          class="w-full pl-8 pr-7 py-1.5 rounded-lg bg-white dark:bg-canvas-elevated border border-slate-200/80 dark:border-white/[0.08] text-xs text-slate-800 dark:text-slate-200 placeholder:text-slate-400 focus:outline-none focus:border-brand-500 transition-colors"
        />
        <button
          v-if="searchQuery"
          type="button"
          @click="searchQuery = ''"
          class="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
        >
          <X class="w-3.5 h-3.5" />
        </button>
      </div>
    </div>

    <!-- Slices List -->
    <div class="p-2 space-y-1 flex-1 overflow-y-auto">
      <button
        v-for="chunk in filteredChunks"
        :key="chunk.id"
        type="button"
        @click="emit('select-chunk', chunk.originalIndex)"
        :class="[
          'w-full text-left p-3 rounded-xl text-xs sm:text-sm font-semibold transition-all flex items-start gap-2.5',
          activeChunkIndex === chunk.originalIndex
            ? 'bg-brand-500/10 dark:bg-brand-500/15 text-brand-900 dark:text-brand-300 font-bold border-l-4 border-brand-500 shadow-sm'
            : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-canvas-elevated/60 border-l-4 border-transparent',
        ]"
      >
        <CheckCircle2
          v-if="completedSlices.has(chunk.chunkOrder)"
          class="w-4 h-4 text-brand-600 dark:text-brand-400 shrink-0 mt-0.5"
        />
        <span
          v-else
          class="w-4 h-4 rounded-full border border-slate-300 dark:border-white/[0.12] flex items-center justify-center text-xs text-slate-500 dark:text-slate-400 shrink-0 mt-0.5"
        >
          {{ chunk.chunkOrder }}
        </span>

        <div class="flex-1 min-w-0">
          <div class="truncate">{{ chunk.chapterTitle }}</div>
          <div
            class="text-xs text-slate-400 mt-0.5 flex items-center gap-1 font-normal"
          >
            <Clock class="w-3 h-3" />
            <span>{{
              $t("reader.read_min", {
                minutes: chunk.estimatedReadMinutes || 3,
              })
            }}</span>
          </div>
        </div>
      </button>

      <div
        v-if="filteredChunks.length === 0"
        class="py-6 text-center text-xs text-slate-400"
      >
        {{ $t("reader.no_chapters_found") }}
      </div>
    </div>

    <!-- Export Markdown Footer -->
    <div
      class="p-3 border-t border-slate-200/80 dark:border-white/[0.06] mt-auto"
    >
      <button
        type="button"
        data-testid="export-markdown-btn"
        @click="emit('export-markdown')"
        :disabled="isExportingMarkdown"
        class="w-full flex items-center justify-center gap-2 py-2 px-3 rounded-xl bg-slate-200/70 dark:bg-canvas-elevated hover:bg-slate-300 dark:hover:bg-canvas-subtle border border-transparent dark:border-white/[0.06] text-slate-800 dark:text-slate-200 text-xs font-bold transition-colors disabled:opacity-50"
      >
        <Download class="w-3.5 h-3.5" />
        <span>{{
          isExportingMarkdown
            ? $t("reader.exporting")
            : $t("reader.export_obsidian")
        }}</span>
      </button>
    </div>
  </aside>

  <!-- Mobile Table of Contents Modal Drawer (Teleported to Body) -->
  <Teleport to="body">
    <div
      v-if="isMobileOpen"
      class="md:hidden fixed inset-0 z-50 bg-slate-950/75 backdrop-blur-sm flex justify-start animate-in fade-in"
      @click.self="emit('close-mobile-toc')"
    >
      <div
        class="w-4/5 max-w-xs bg-white dark:bg-canvas-subtle text-slate-900 dark:text-white h-full flex flex-col shadow-2xl border-r border-slate-200/80 dark:border-white/[0.08] pb-[max(1rem,env(safe-area-inset-bottom))] animate-in slide-in-from-left"
      >
        <div
          class="p-4 border-b border-slate-200/80 dark:border-white/[0.06] flex items-center justify-between"
        >
          <div
            class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-slate-900 dark:text-white"
          >
            <BookOpen class="w-4 h-4 text-brand-500" :stroke-width="1.5" />
            <span>{{ $t("reader.toc") }}</span>
          </div>
          <button
            type="button"
            @click="emit('close-mobile-toc')"
            class="p-1.5 rounded-lg text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-canvas-elevated transition-colors"
            :aria-label="$t('reader.close_toc')"
          >
            <X class="w-5 h-5" :stroke-width="1.5" />
          </button>
        </div>

        <!-- Mobile Search Filter -->
        <div class="p-2 border-b border-slate-200/80 dark:border-white/[0.06]">
          <div class="relative">
            <Search
              class="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-400"
            />
            <input
              v-model="searchQuery"
              type="text"
              :placeholder="$t('reader.search_placeholder')"
              class="w-full pl-8 pr-7 py-1.5 rounded-lg bg-slate-100 dark:bg-canvas-elevated border border-slate-200/80 dark:border-white/[0.08] text-xs text-slate-800 dark:text-slate-200 placeholder:text-slate-400 focus:outline-none focus:border-brand-500 transition-colors"
            />
            <button
              v-if="searchQuery"
              type="button"
              @click="searchQuery = ''"
              class="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
            >
              <X class="w-3.5 h-3.5" />
            </button>
          </div>
        </div>

        <div class="flex-1 overflow-y-auto p-2 space-y-1">
          <button
            v-for="chunk in filteredChunks"
            :key="chunk.id"
            type="button"
            @click="
              emit('select-chunk', chunk.originalIndex);
              emit('close-mobile-toc');
            "
            :class="[
              'w-full text-left p-3 rounded-xl text-xs sm:text-sm font-semibold transition-all flex items-start gap-2.5',
              activeChunkIndex === chunk.originalIndex
                ? 'bg-brand-500/10 dark:bg-brand-500/15 text-brand-900 dark:text-brand-300 font-bold border-l-4 border-brand-500 shadow-sm'
                : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-canvas-elevated/60 border-l-4 border-transparent',
            ]"
          >
            <CheckCircle2
              v-if="completedSlices.has(chunk.chunkOrder)"
              class="w-4 h-4 text-brand-600 dark:text-brand-400 shrink-0 mt-0.5"
              :stroke-width="1.5"
            />
            <span
              v-else
              class="w-4 h-4 rounded-full border border-slate-300 dark:border-white/[0.12] flex items-center justify-center text-xs text-slate-500 dark:text-slate-400 shrink-0 mt-0.5"
            >
              {{ chunk.chunkOrder }}
            </span>

            <div class="flex-1 min-w-0">
              <div class="truncate">{{ chunk.chapterTitle }}</div>
              <div
                class="text-[11px] text-slate-400 mt-0.5 flex items-center gap-1 font-normal"
              >
                <Clock class="w-3 h-3" />
                <span>{{
                  $t("reader.read_min", {
                    minutes: chunk.estimatedReadMinutes || 3,
                  })
                }}</span>
              </div>
            </div>
          </button>

          <div
            v-if="filteredChunks.length === 0"
            class="py-6 text-center text-xs text-slate-400"
          >
            {{ $t("reader.no_chapters_found") }}
          </div>
        </div>

        <div class="p-3 border-t border-slate-200/80 dark:border-white/[0.06]">
          <button
            type="button"
            @click="emit('export-markdown')"
            :disabled="isExportingMarkdown"
            class="w-full flex items-center justify-center gap-2 py-2.5 px-3 rounded-xl bg-slate-100 dark:bg-canvas-elevated hover:bg-slate-200 dark:hover:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.06] text-slate-800 dark:text-slate-200 text-xs font-bold transition-colors disabled:opacity-50"
          >
            <Download class="w-3.5 h-3.5" />
            <span>{{
              isExportingMarkdown
                ? $t("reader.exporting")
                : $t("reader.export_obsidian")
            }}</span>
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
