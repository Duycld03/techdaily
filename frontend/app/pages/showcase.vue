<script setup lang="ts">
import { ref } from 'vue'
import { Component, Inbox, WifiOff } from 'lucide-vue-next'
import ShowcaseSection from '~/components/showcase/ShowcaseSection.vue'
import PrimitivesShowcase from '~/components/showcase/PrimitivesShowcase.vue'
import PaginationShowcase from '~/components/showcase/PaginationShowcase.vue'
import Sm2AssessmentShowcase from '~/components/showcase/Sm2AssessmentShowcase.vue'
import RetentionAnalyticsShowcase from '~/components/showcase/RetentionAnalyticsShowcase.vue'
import QuizOptionCard from '~/components/showcase/QuizOptionCard.vue'
import ProductionModal from '~/components/showcase/ProductionModal.vue'
import CodeSnippetBlock from '~/components/showcase/CodeSnippetBlock.vue'
import SkeletonShimmer from '~/components/showcase/SkeletonShimmer.vue'
import EmptyStateCard from '~/components/showcase/EmptyStateCard.vue'
import FloatingSelectionToolbar from '~/components/showcase/FloatingSelectionToolbar.vue'
import LayoutArchetypesShowcase from '~/components/showcase/LayoutArchetypesShowcase.vue'
import IconShowcase from '~/components/showcase/IconShowcase.vue'
import { useToast } from '~/composables/useToast'

if (typeof useHead === 'function') {
  useHead({
    title: 'Design System — TechDaily',
    link: [
      {
        rel: 'stylesheet',
        href: 'https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;500;600&display=swap'
      }
    ]
  })
}

const toast = useToast()

// Interactive MCQ demo state
const selected = ref<string | null>('b')
const quizOptions = [
  { letter: 'A', text: 'Vertically shard the write path by tenant ID', state: 'default' as const },
  { letter: 'B', text: 'Introduce a read-through cache with TTL invalidation', state: 'selected' as const },
  { letter: 'C', text: 'Add a CDN in front of the origin', state: 'correct' as const },
  { letter: 'D', text: 'Increase the connection pool size', state: 'incorrect' as const }
]

const modalOpen = ref(false)

function handleSubmit() {
  modalOpen.value = false
  toast.success('Configuration saved')
}

const csharpSnippet = `public sealed class RateLimiter
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();

    public bool TryAcquire(string clientId, int cost = 1)
    {
        var bucket = _buckets.GetOrAdd(clientId, _ => new TokenBucket(capacity: 100, refillPerSecond: 10));
        return bucket.TryConsume(cost);
    }
}`

const tsSnippet = `export function useDebouncedRef<T>(value: T, delayMs = 250) {
  const state = ref(value) as Ref<T>
  let timer: ReturnType<typeof setTimeout> | null = null
  watch(() => value, (next) => {
    if (timer) clearTimeout(timer)
    timer = setTimeout(() => (state.value = next), delayMs)
  })
  return readonly(state)
}`

function onToolbarAction(action: string) {
  toast.success(`Action: ${action}`)
}
</script>

<template>
  <div class="max-w-7xl mx-auto space-y-8 px-4 sm:px-6 py-6">
    <!-- Platform-Standard Page Header Banner -->
    <header class="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-200/80 dark:border-white/[0.08] pb-6">
      <div class="flex items-center gap-3">
        <!-- 40x40 Icon Badge -->
        <div class="w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0 shadow-sm">
          <Component class="w-5 h-5 sm:w-6 sm:h-6" :stroke-width="1.5" />
        </div>
        <div class="space-y-0.5">
          <div class="flex items-center gap-2">
            <h1 class="text-xl sm:text-2xl font-black tracking-tight text-slate-900 dark:text-white">
              {{ $t('showcase.title') }}
            </h1>
            <span class="rounded-md bg-brand-500/15 px-2 py-0.5 font-mono text-[11px] font-semibold text-brand-600 dark:text-brand-300">
              {{ $t('showcase.version_badge') }}
            </span>
          </div>
          <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-normal">
            {{ $t('showcase.subtitle') }}
          </p>
        </div>
      </div>

      <!-- Quick-Jump Anchor Navigation Pills -->
      <nav aria-label="Showcase quick jump" class="flex items-center gap-1 overflow-x-auto whitespace-nowrap rounded-2xl border border-slate-200/80 bg-slate-100 p-1 dark:border-white/[0.08] dark:bg-canvas-subtle shrink-0">
        <a href="#primitives" class="rounded-xl px-2.5 sm:px-3 py-1 text-xs font-semibold text-slate-600 transition-colors hover:text-slate-900 dark:text-slate-400 dark:hover:text-white">
          {{ $t('showcase.anchors.primitives') }}
        </a>
        <a href="#pagination" class="rounded-xl px-2.5 sm:px-3 py-1 text-xs font-semibold text-slate-600 transition-colors hover:text-slate-900 dark:text-slate-400 dark:hover:text-white">
          {{ $t('showcase.anchors.pagination') }}
        </a>
        <a href="#sm2" class="rounded-xl px-2.5 sm:px-3 py-1 text-xs font-semibold text-slate-600 transition-colors hover:text-slate-900 dark:text-slate-400 dark:hover:text-white">
          {{ $t('showcase.anchors.sm2') }}
        </a>
        <a href="#retention" class="rounded-xl px-2.5 sm:px-3 py-1 text-xs font-semibold text-slate-600 transition-colors hover:text-slate-900 dark:text-slate-400 dark:hover:text-white">
          {{ $t('showcase.anchors.retention') }}
        </a>
        <a href="#archetypes" class="rounded-xl px-2.5 sm:px-3 py-1 text-xs font-semibold text-slate-600 transition-colors hover:text-slate-900 dark:text-slate-400 dark:hover:text-white">
          {{ $t('showcase.anchors.archetypes') }}
        </a>
        <a href="#feedback" class="rounded-xl px-2.5 sm:px-3 py-1 text-xs font-semibold text-slate-600 transition-colors hover:text-slate-900 dark:text-slate-400 dark:hover:text-white">
          {{ $t('showcase.anchors.feedback') }}
        </a>
        <a href="#icons" class="rounded-xl px-2.5 sm:px-3 py-1 text-xs font-semibold text-slate-600 transition-colors hover:text-slate-900 dark:text-slate-400 dark:hover:text-white">
          {{ $t('showcase.anchors.icons') }}
        </a>
      </nav>
    </header>

    <div class="space-y-10">
      <!-- 01. Base UI Primitives -->
      <section id="primitives">
        <ShowcaseSection
          index="01"
          :title="$t('showcase.sections.primitives_title')"
          :subtitle="$t('showcase.sections.primitives_sub')"
        >
          <PrimitivesShowcase />
        </ShowcaseSection>
      </section>

      <!-- 02. Universal Pagination (BasePagination) -->
      <section id="pagination">
        <ShowcaseSection
          index="02"
          :title="$t('showcase.sections.pagination_title')"
          :subtitle="$t('showcase.sections.pagination_sub')"
        >
          <PaginationShowcase />
        </ShowcaseSection>
      </section>

      <!-- 03. Spaced Repetition (SM-2) & Recall Assessment -->
      <section id="sm2">
        <ShowcaseSection
          index="03"
          :title="$t('showcase.sections.sm2_title')"
          :subtitle="$t('showcase.sections.sm2_sub')"
        >
          <Sm2AssessmentShowcase />
        </ShowcaseSection>
      </section>

      <!-- 04. Retention Analytics & Memory Health -->
      <section id="retention">
        <ShowcaseSection
          index="04"
          :title="$t('showcase.sections.retention_title')"
          :subtitle="$t('showcase.sections.retention_sub')"
        >
          <RetentionAnalyticsShowcase />
        </ShowcaseSection>
      </section>

      <!-- 05. System Layout Archetypes -->
      <section id="archetypes">
        <ShowcaseSection
          index="05"
          :title="$t('showcase.sections.archetypes_title')"
          :subtitle="$t('showcase.sections.archetypes_sub')"
        >
          <LayoutArchetypesShowcase />
        </ShowcaseSection>
      </section>

      <!-- 06. Interactive Option Cards -->
      <ShowcaseSection
        index="06"
        title="Interactive Option Cards"
        subtitle="Compact multiple-choice answer rows with selection and validation states."
      >
        <div class="grid gap-4 lg:grid-cols-2">
          <!-- Live interactive set -->
          <div class="glass-card p-4">
            <p class="mb-3 text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
              Interactive (click to select)
            </p>
            <div class="space-y-2">
              <QuizOptionCard
                v-for="opt in quizOptions"
                :key="opt.letter"
                :letter="opt.letter"
                :text="opt.text"
                :state="selected === opt.letter.toLowerCase() ? 'selected' : 'default'"
                @select="selected = opt.letter.toLowerCase()"
              />
            </div>
          </div>

          <!-- States reference -->
          <div class="glass-card p-4">
            <p class="mb-3 text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
              States Reference
            </p>
            <div class="space-y-2">
              <QuizOptionCard letter="A" text="Default / resting state" state="default" />
              <QuizOptionCard letter="B" text="Selected by candidate" state="selected" />
              <QuizOptionCard letter="C" text="Correct answer revealed" state="correct" />
              <QuizOptionCard letter="D" text="Incorrect selection" state="incorrect" />
            </div>
          </div>
        </div>
      </ShowcaseSection>

      <!-- 07. Production Modal Dialog Shell -->
      <section id="feedback">
        <div class="space-y-10">
          <ShowcaseSection
            index="07"
            :title="$t('showcase.sections.modal_title')"
            :subtitle="$t('showcase.sections.modal_sub')"
          >
            <div class="glass-card p-4">
              <div class="flex flex-wrap items-center justify-between gap-3">
                <p class="text-sm text-slate-600 dark:text-slate-300">
                  Footer actions stay pinned even when the body overflows on short viewports.
                </p>
                <button
                  type="button"
                  class="h-9 rounded-lg bg-brand-500 px-3.5 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-brand-600"
                  @click="modalOpen = true"
                >
                  Open Modal
                </button>
              </div>
            </div>

            <ProductionModal
              :open="modalOpen"
              title="Scenario Submission Settings"
              @close="modalOpen = false"
              @submit="handleSubmit"
            >
              <div class="space-y-3.5 text-sm text-slate-700 dark:text-slate-300">
                <p>
                  Configure how your scenario answer is evaluated. This body scrolls independently while the
                  header and footer remain fixed.
                </p>
                <div
                  v-for="n in 10"
                  :key="n"
                  class="rounded-lg border border-slate-200/80 bg-slate-50/60 p-3 dark:border-white/[0.06] dark:bg-white/[0.02]"
                >
                  <p class="text-sm font-medium text-slate-800 dark:text-slate-200">Evaluation criterion {{ n }}</p>
                  <p class="mt-0.5 text-xs text-slate-500 dark:text-slate-400">
                    Filler row to force the body to overflow and prove the sticky footer behaviour.
                  </p>
                </div>
              </div>
            </ProductionModal>
          </ShowcaseSection>

          <!-- 08. Code Snippet Block -->
          <ShowcaseSection
            index="08"
            :title="$t('showcase.sections.code_title')"
            :subtitle="$t('showcase.sections.code_sub')"
          >
            <div class="grid gap-3.5 lg:grid-cols-2">
              <CodeSnippetBlock filename="RateLimiter.cs" :code="csharpSnippet" />
              <CodeSnippetBlock language="TypeScript" :code="tsSnippet" />
            </div>
          </ShowcaseSection>

          <!-- 09. Skeleton Loading States -->
          <ShowcaseSection
            index="09"
            :title="$t('showcase.sections.skeletons_title')"
            :subtitle="$t('showcase.sections.skeletons_sub')"
          >
            <div class="grid gap-3.5 lg:grid-cols-3">
              <SkeletonShimmer preset="card" />
              <SkeletonShimmer preset="quiz" />
              <SkeletonShimmer preset="metric" />
            </div>
          </ShowcaseSection>

          <!-- 10. Empty & Error States -->
          <ShowcaseSection
            index="10"
            :title="$t('showcase.sections.empty_title')"
            :subtitle="$t('showcase.sections.empty_sub')"
          >
            <div class="grid gap-3.5 sm:grid-cols-2">
              <EmptyStateCard
                :icon="Inbox"
                accent="brand"
                heading="No documents yet"
                description="Import your first source document to start generating drills and flashcards."
                cta="Import First Document"
                @action="toast.success('Import started')"
              />
              <EmptyStateCard
                :icon="WifiOff"
                accent="red"
                heading="Connection lost"
                description="We couldn't reach the evaluation service. Check your network and try again."
                cta="Retry Connection"
                @action="toast.success('Retrying…')"
              />
            </div>
          </ShowcaseSection>

          <!-- 11. Reader Selection Toolbar -->
          <ShowcaseSection
            index="11"
            :title="$t('showcase.sections.reader_title')"
            :subtitle="$t('showcase.sections.reader_sub')"
          >
            <div class="glass-card flex flex-col items-center gap-4 p-6">
              <p class="max-w-lg text-center text-sm leading-relaxed text-slate-600 dark:text-slate-300">
                Distributed consensus relies on
                <mark class="rounded bg-brand-500/20 px-1 text-brand-700 dark:text-brand-200">quorum</mark>
                to guarantee agreement across replicas — highlight a term to reveal contextual actions.
              </p>
              <FloatingSelectionToolbar
                @explain="onToolbarAction('Explain Term')"
                @translate="onToolbarAction('Translate')"
                @copy="onToolbarAction('Copy')"
              />
            </div>
          </ShowcaseSection>
        </div>
      </section>

      <!-- 12. Engineering Iconography System -->
      <section id="icons">
        <ShowcaseSection
          index="12"
          :title="$t('showcase.sections.icons_title')"
          :subtitle="$t('showcase.sections.icons_sub')"
        >
          <IconShowcase />
        </ShowcaseSection>
      </section>
    </div>
  </div>
</template>
