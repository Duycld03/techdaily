<script setup lang="ts">
import { ref } from 'vue'
import { Flame, Timer, Target, Brain, Inbox, WifiOff } from 'lucide-vue-next'
import ShowcaseSection from '~/components/showcase/ShowcaseSection.vue'
import PrimitivesShowcase from '~/components/showcase/PrimitivesShowcase.vue'
import QuizOptionCard from '~/components/showcase/QuizOptionCard.vue'
import ProductionModal from '~/components/showcase/ProductionModal.vue'
import BentoMetricCard from '~/components/showcase/BentoMetricCard.vue'
import CodeSnippetBlock from '~/components/showcase/CodeSnippetBlock.vue'
import SkeletonShimmer from '~/components/showcase/SkeletonShimmer.vue'
import EmptyStateCard from '~/components/showcase/EmptyStateCard.vue'
import FloatingSelectionToolbar from '~/components/showcase/FloatingSelectionToolbar.vue'
import LayoutArchetypesShowcase from '~/components/showcase/LayoutArchetypesShowcase.vue'
import IconShowcase from '~/components/showcase/IconShowcase.vue'
import { useToast } from '~/composables/useToast'

useHead({
  title: 'Design System — TechDaily',
  link: [
    {
      rel: 'stylesheet',
      href: 'https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;500;600&display=swap'
    }
  ]
})

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
  <div class="mx-auto max-w-7xl px-4 py-4 sm:px-6">
    <!-- Page header -->
    <header class="mb-5 flex flex-wrap items-end justify-between gap-3">
      <div class="space-y-1">
        <div class="flex items-center gap-2">
          <span
            class="rounded-md bg-brand-500/15 px-2 py-0.5 font-mono text-[11px] font-semibold text-brand-600 dark:text-brand-300"
            >v1.0</span
          >
          <span class="text-xs font-medium text-slate-400 dark:text-slate-500">Engineering Cockpit · Density 8/10</span>
        </div>
        <h1 class="text-xl font-bold tracking-tight text-slate-900 dark:text-white">
          TechDaily Design System
        </h1>
        <p class="text-sm text-slate-500 dark:text-slate-400">
          Primitives, quiz cards, modal shell, metrics, code blocks, skeletons, empty states &amp; reader toolbar.
        </p>
      </div>
    </header>

    <div class="space-y-8">
      <!-- 1. Base UI Primitives -->
      <ShowcaseSection
        index="01"
        title="Base UI Primitives"
        subtitle="Buttons, inputs, badges & segmented switcher — all h-9 controls."
      >
        <PrimitivesShowcase />
      </ShowcaseSection>

      <!-- 2. Multiple-Choice Option Card -->
      <ShowcaseSection
        index="02"
        title="Interactive Option Card"
        subtitle="Compact ~50px rows — four options fit inside 250px of vertical space."
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
              States
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

      <!-- 3. Production Modal Dialog Shell -->
      <ShowcaseSection
        index="03"
        title="Production Modal Shell"
        subtitle="Fixed header + scrollable body (max-h-60vh) + sticky footer — no fold cut-off."
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

      <!-- 4. Bento Metric Cards -->
      <ShowcaseSection
        index="04"
        title="Bento Metric Cards"
        subtitle="Compact stats with tabular numbers, accent icon & mini progress bar."
      >
        <div class="grid gap-3.5 sm:grid-cols-2 lg:grid-cols-4">
          <BentoMetricCard
            :icon="Flame"
            label="Current Streak"
            value="42"
            unit="days"
            accent="amber"
            :progress="70"
            delta="+3"
            delta-trend="up"
          />
          <BentoMetricCard
            :icon="Timer"
            label="Focus Today"
            value="28"
            unit="min"
            accent="brand"
            :progress="93"
            delta="+12%"
            delta-trend="up"
          />
          <BentoMetricCard
            :icon="Target"
            label="Drill Accuracy"
            value="87"
            unit="%"
            accent="emerald"
            :progress="87"
            delta="+4%"
            delta-trend="up"
          />
          <BentoMetricCard
            :icon="Brain"
            label="Cards Due"
            value="16"
            unit="cards"
            accent="cyber"
            :progress="34"
            delta="-8"
            delta-trend="down"
          />
        </div>
      </ShowcaseSection>

      <!-- 5. Code Snippet Block -->
      <ShowcaseSection
        index="05"
        title="Code Snippet Block"
        subtitle="File/language header, copy-to-clipboard feedback & compact horizontal scrollbar."
      >
        <div class="grid gap-3.5 lg:grid-cols-2">
          <CodeSnippetBlock filename="RateLimiter.cs" :code="csharpSnippet" />
          <CodeSnippetBlock language="TypeScript" :code="tsSnippet" />
        </div>
      </ShowcaseSection>

      <!-- 6. Skeleton Loading States -->
      <ShowcaseSection
        index="06"
        title="Skeleton Loading States"
        subtitle="Subtle animate-pulse placeholders for card, quiz & metric layouts."
      >
        <div class="grid gap-3.5 lg:grid-cols-3">
          <SkeletonShimmer preset="card" />
          <SkeletonShimmer preset="quiz" />
          <SkeletonShimmer preset="metric" />
        </div>
      </ShowcaseSection>

      <!-- 7. Empty & Error States -->
      <ShowcaseSection
        index="07"
        title="Empty & Error States"
        subtitle="Compact containers (< 220px) with icon, copy & a single primary action."
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

      <!-- 8. Floating Selection Toolbar -->
      <ShowcaseSection
        index="08"
        title="Reader Selection Toolbar"
        subtitle="Floating pill (~34px, rounded-full) surfaced on text selection in the reader."
      >
        <div
          class="glass-card flex flex-col items-center gap-4 p-6"
        >
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

      <!-- 9. System Layout Archetypes -->
      <ShowcaseSection
        index="09"
        title="System Layout Archetypes"
        subtitle="Full-page templates that fill the 1680px working width — no black voids, no stretched phone columns."
      >
        <LayoutArchetypesShowcase />
      </ShowcaseSection>

      <!-- 10. Engineering Iconography System -->
      <ShowcaseSection
        index="10"
        title="Engineering Iconography"
        subtitle="Standardized 24-icon kit for technical learning, mastery tracking, and telemetry."
      >
        <IconShowcase />
      </ShowcaseSection>
    </div>
  </div>
</template>
