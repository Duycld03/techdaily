<script setup lang="ts">
import { ref, watch, nextTick } from "vue";
import {
  Sparkles,
  X,
  Send,
  Loader2,
  BookOpen,
  ArrowRight,
  AlertTriangle,
  Bot,
  User,
  Trash2,
} from "lucide-vue-next";
import { useLibraryStore, type BookCitation } from "~/stores/useLibraryStore";
import { useMarkdownRenderer } from "~/composables/useMarkdownRenderer";
import { useApiError } from "~/composables/useApiError";

const props = defineProps<{
  isOpen: boolean;
  bookId: string;
  bookTitle?: string;
  currentChunkId?: string;
  currentChunkOrder?: number;
}>();

const emit = defineEmits<{
  (e: "close"): void;
  (e: "jump-to-slice", order: number): void;
}>();

const { t, locale } = useI18n();
const libraryStore = useLibraryStore();
const { render: renderMarkdown } = useMarkdownRenderer();
const { formatError } = useApiError();

interface ChatMessage {
  id: string;
  role: "user" | "assistant";
  text: string;
  citations?: BookCitation[];
  timestamp: Date;
}

const messages = ref<ChatMessage[]>([]);
const questionInput = ref("");
const isAsking = ref(false);
const rateLimitError = ref<string | null>(null);
const messagesContainer = ref<HTMLElement | null>(null);

const suggestedQuestions = [
  "What are the core architecture principles explained here?",
  "What are the key trade-offs discussed in this chapter?",
  "How does error handling and failure recovery work?",
];

function scrollToBottom() {
  nextTick(() => {
    if (messagesContainer.value) {
      messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
    }
  });
}

function handleClose() {
  emit("close");
}

function handleSelectSuggested(q: string) {
  questionInput.value = q;
  handleAsk();
}

function handleCitationClick(order: number) {
  emit("jump-to-slice", order);
}

function clearChat() {
  messages.value = [];
  rateLimitError.value = null;
}

async function handleAsk() {
  const query = questionInput.value.trim();
  if (!query || isAsking.value || query.length < 3 || query.length > 300) {
    return;
  }

  rateLimitError.value = null;
  const userMsgId = `user-${Date.now()}`;
  messages.value.push({
    id: userMsgId,
    role: "user",
    text: query,
    timestamp: new Date(),
  });

  questionInput.value = "";
  isAsking.value = true;
  scrollToBottom();

  try {
    const res = await libraryStore.askBook(
      props.bookId,
      query,
      props.currentChunkId,
      locale.value,
    );

    messages.value.push({
      id: `ai-${Date.now()}`,
      role: "assistant",
      text: res.answerMarkdown,
      citations: res.citations || [],
      timestamp: new Date(),
    });
  } catch (err: any) {
    if (err?.status === 429 || err?.response?.status === 429) {
      rateLimitError.value = t("reader.ask_rate_limit_warning");
    } else {
      messages.value.push({
        id: `ai-err-${Date.now()}`,
        role: "assistant",
        text: formatError(err, "reader.ask_error_generic"),
        timestamp: new Date(),
      });
    }
  } finally {
    isAsking.value = false;
    scrollToBottom();
  }
}

watch(
  () => props.isOpen,
  (val) => {
    if (val) {
      scrollToBottom();
    }
  },
);
</script>

<template>
  <Teleport to="body">
    <div
      v-if="isOpen"
      class="fixed inset-0 z-50 flex justify-end bg-slate-950/60 backdrop-blur-sm animate-in fade-in duration-200"
      @click.self="handleClose"
      @keydown.esc="handleClose"
    >
      <div
        class="w-full max-w-lg md:max-w-xl h-full bg-white dark:bg-slate-900 border-l border-slate-200 dark:border-slate-800 shadow-2xl flex flex-col animate-in slide-in-from-right duration-250"
      >
        <!-- Header -->
        <div
          class="px-5 py-4 border-b border-slate-200 dark:border-slate-800/80 flex items-center justify-between gap-3 shrink-0 bg-slate-50/70 dark:bg-slate-900/60 backdrop-blur"
        >
          <div class="flex items-center gap-2.5 min-w-0">
            <div
              class="w-9 h-9 rounded-xl bg-brand-500/10 dark:bg-brand-500/20 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0"
            >
              <Sparkles class="w-5 h-5" />
            </div>
            <div class="min-w-0">
              <h2
                class="text-sm sm:text-base font-bold text-slate-900 dark:text-white truncate"
              >
                {{ $t("reader.ask_book_title") }}
              </h2>
              <p
                class="text-xs text-slate-500 dark:text-slate-400 truncate max-w-xs"
              >
                {{ bookTitle || $t("reader.ask_book_subtitle") }}
              </p>
            </div>
          </div>

          <div class="flex items-center gap-1">
            <button
              v-if="messages.length > 0"
              @click="clearChat"
              class="p-2 rounded-xl text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              :title="$t('reader.clear_chat')"
            >
              <Trash2 class="w-4 h-4" />
            </button>
            <button
              @click="handleClose"
              class="p-2 rounded-xl text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              :title="$t('reader.close_drawer')"
            >
              <X class="w-5 h-5" />
            </button>
          </div>
        </div>

        <!-- Chat Scroll Area -->
        <div
          ref="messagesContainer"
          class="flex-1 overflow-y-auto p-4 sm:p-5 space-y-4 selection:bg-brand-500/20"
        >
          <!-- Empty State & Suggested Prompts -->
          <div
            v-if="messages.length === 0 && !isAsking"
            class="py-8 px-2 flex flex-col items-center justify-center text-center space-y-5 my-auto"
          >
            <div
              class="w-14 h-14 rounded-2xl bg-brand-50 dark:bg-brand-950/50 border border-brand-200 dark:border-brand-800/60 flex items-center justify-center text-brand-600 dark:text-brand-400 shadow-sm"
            >
              <Bot class="w-7 h-7" />
            </div>
            <div class="space-y-1.5 max-w-sm">
              <h3
                class="text-sm sm:text-base font-bold text-slate-900 dark:text-white"
              >
                {{ $t("reader.ask_book_title") }}
              </h3>
              <p
                class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 leading-relaxed"
              >
                {{ $t("reader.ask_empty_prompt") }}
              </p>
            </div>

            <div class="w-full space-y-2 pt-2">
              <span
                class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500"
              >
                {{ $t("reader.suggested_questions") }}
              </span>
              <div class="flex flex-col gap-2">
                <button
                  v-for="(q, idx) in suggestedQuestions"
                  :key="idx"
                  @click="handleSelectSuggested(q)"
                  class="text-left text-xs sm:text-sm p-3 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-900/50 hover:bg-brand-50 dark:hover:bg-brand-950/40 hover:border-brand-300 dark:hover:border-brand-700 text-slate-700 dark:text-slate-300 transition-all flex items-center justify-between group"
                >
                  <span class="truncate pr-2">{{ q }}</span>
                  <ArrowRight
                    class="w-3.5 h-3.5 text-slate-400 group-hover:text-brand-600 dark:group-hover:text-brand-400 shrink-0 transition-transform group-hover:translate-x-0.5"
                  />
                </button>
              </div>
            </div>
          </div>

          <!-- Message Bubbles -->
          <template v-else>
            <div
              v-for="msg in messages"
              :key="msg.id"
              :class="[
                'flex gap-3',
                msg.role === 'user' ? 'justify-end' : 'justify-start',
              ]"
            >
              <!-- AI Avatar -->
              <div
                v-if="msg.role === 'assistant'"
                class="w-7 h-7 rounded-lg bg-brand-500/10 dark:bg-brand-500/20 text-brand-600 dark:text-brand-400 border border-brand-500/30 flex items-center justify-center shrink-0 mt-1"
              >
                <Bot class="w-4 h-4" />
              </div>

              <div
                :class="[
                  'max-w-[85%] rounded-2xl p-4 shadow-sm text-sm sm:text-base leading-relaxed',
                  msg.role === 'user'
                    ? 'bg-brand-600 text-white rounded-br-xs font-medium'
                    : 'bg-slate-50 dark:bg-slate-900/90 text-slate-900 dark:text-slate-100 border border-slate-200/90 dark:border-slate-800 rounded-bl-xs',
                ]"
              >
                <!-- Message Text / Markdown -->
                <div
                  v-if="msg.role === 'assistant'"
                  class="markdown-body prose prose-slate dark:prose-invert max-w-none text-xs sm:text-sm prose-p:my-1.5 prose-headings:my-2 prose-code:text-xs prose-code:px-1 prose-code:py-0.5"
                  v-html="renderMarkdown(msg.text)"
                />
                <div v-else class="whitespace-pre-wrap break-words">
                  {{ msg.text }}
                </div>

                <!-- Grounded Citations List -->
                <div
                  v-if="msg.citations && msg.citations.length > 0"
                  class="mt-3.5 pt-3 border-t border-slate-200/80 dark:border-slate-800/80 space-y-2"
                >
                  <div
                    class="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400"
                  >
                    <BookOpen class="w-3.5 h-3.5 text-brand-500" />
                    <span>{{ $t("reader.citations") }}</span>
                  </div>

                  <div class="flex flex-wrap gap-1.5">
                    <button
                      v-for="(cit, idx) in msg.citations"
                      :key="idx"
                      @click="handleCitationClick(cit.chunkOrder)"
                      class="group inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-white dark:bg-slate-800/90 border border-slate-200 dark:border-slate-700 text-xs font-semibold text-slate-700 dark:text-slate-200 hover:border-brand-500 hover:text-brand-600 dark:hover:text-brand-400 transition-all shadow-xs"
                      :title="cit.excerpt || cit.chapterTitle"
                    >
                      <span class="truncate max-w-[150px] sm:max-w-[200px]">
                        {{ $t("reader.slice_badge", { current: cit.chunkOrder, total: props.currentChunkOrder || '?' }) }}:
                        {{ cit.chapterTitle }}
                      </span>
                      <span
                        v-if="cit.relevanceScore"
                        class="px-1 py-0.2 rounded bg-brand-50 dark:bg-brand-950/60 text-brand-600 dark:text-brand-400 text-[10px] font-bold"
                      >
                        {{ Math.round(cit.relevanceScore * 100) }}%
                      </span>
                      <ArrowRight
                        class="w-3 h-3 text-slate-400 group-hover:text-brand-500 transition-transform group-hover:translate-x-0.5 shrink-0"
                      />
                    </button>
                  </div>
                </div>
              </div>

              <!-- User Avatar -->
              <div
                v-if="msg.role === 'user'"
                class="w-7 h-7 rounded-lg bg-slate-200 dark:bg-slate-800 text-slate-600 dark:text-slate-300 flex items-center justify-center shrink-0 mt-1"
              >
                <User class="w-4 h-4" />
              </div>
            </div>
          </template>

          <!-- In-Flight Loading Skeleton -->
          <div v-if="isAsking" class="flex gap-3 justify-start items-start">
            <div
              class="w-7 h-7 rounded-lg bg-brand-500/10 dark:bg-brand-500/20 text-brand-600 dark:text-brand-400 border border-brand-500/30 flex items-center justify-center shrink-0 mt-1"
            >
              <Bot class="w-4 h-4" />
            </div>
            <div
              class="rounded-2xl p-4 bg-slate-50 dark:bg-slate-900/90 border border-slate-200/90 dark:border-slate-800 rounded-bl-xs flex items-center gap-2.5 text-xs sm:text-sm text-slate-500 dark:text-slate-400"
            >
              <Loader2 class="w-4 h-4 animate-spin text-brand-500" />
              <span>{{ $t("reader.asking") }}</span>
            </div>
          </div>

          <!-- Rate Limit Warning Banner -->
          <div
            v-if="rateLimitError"
            class="p-4 rounded-2xl bg-amber-50 dark:bg-amber-950/50 border border-amber-200 dark:border-amber-800/80 text-amber-900 dark:text-amber-200 flex items-start gap-3 text-xs sm:text-sm"
          >
            <AlertTriangle class="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
            <div class="flex-1">
              <span class="font-bold block">{{ $t("reader.rate_limited_title") }}</span>
              <span class="text-xs opacity-90">{{ rateLimitError }}</span>
            </div>
          </div>
        </div>

        <!-- Footer Input Area -->
        <div
          class="p-3 sm:p-4 border-t border-slate-200 dark:border-slate-800/80 bg-white/95 dark:bg-slate-900/90 shrink-0 space-y-2"
        >
          <div class="relative flex items-center">
            <textarea
              v-model="questionInput"
              :placeholder="$t('reader.ask_placeholder')"
              :disabled="isAsking"
              maxlength="300"
              rows="2"
              class="w-full resize-none pl-3.5 pr-14 py-2.5 rounded-2xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500 text-xs sm:text-sm transition-all disabled:opacity-50"
              @keydown.enter.exact.prevent="handleAsk"
            ></textarea>

            <button
              @click="handleAsk"
              :disabled="isAsking || questionInput.trim().length < 3"
              class="absolute right-2 bottom-2.5 p-2 rounded-xl bg-brand-600 hover:bg-brand-500 text-white disabled:opacity-40 disabled:cursor-not-allowed transition-all shadow-sm active:scale-95 flex items-center justify-center shrink-0"
              :title="$t('reader.ask_btn')"
            >
              <Loader2 v-if="isAsking" class="w-4 h-4 animate-spin" />
              <Send v-else class="w-4 h-4" />
            </button>
          </div>

          <div
            class="flex items-center justify-between text-[11px] text-slate-400 px-1"
          >
            <span>{{ $t("reader.hotkey_hint") }}</span>
            <span :class="questionInput.length > 280 ? 'text-amber-500 font-bold' : ''">
              {{ questionInput.length }} / 300
            </span>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>
