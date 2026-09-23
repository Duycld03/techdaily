<script setup lang="ts">
import { watch, onBeforeUnmount } from 'vue'
import { X } from 'lucide-vue-next'

const props = defineProps<{
  open: boolean
  title: string
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'submit'): void
}>()

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') emit('close')
}

watch(
  () => props.open,
  (isOpen) => {
    if (typeof document === 'undefined') return
    if (isOpen) {
      document.addEventListener('keydown', onKeydown)
      document.body.style.overflow = 'hidden'
    } else {
      document.removeEventListener('keydown', onKeydown)
      document.body.style.overflow = ''
    }
  }
)

onBeforeUnmount(() => {
  if (typeof document === 'undefined') return
  document.removeEventListener('keydown', onKeydown)
  document.body.style.overflow = ''
})
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition duration-150 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition duration-100 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="open"
        class="fixed inset-0 z-[100] flex items-center justify-center p-4"
        role="dialog"
        aria-modal="true"
        :aria-label="title"
      >
        <!-- Backdrop -->
        <div
          class="absolute inset-0 bg-slate-900/40 dark:bg-black/60 backdrop-blur-sm"
          @click="emit('close')"
        />

        <!-- Dialog shell: uses a flex column capped at 85vh so the header and
             footer stay pinned while only the body scrolls (fixes fold cut-off). -->
        <div
          class="relative z-10 flex max-h-[85vh] w-full max-w-lg flex-col overflow-hidden rounded-xl border border-slate-200 bg-white shadow-2xl dark:border-white/[0.08] dark:bg-canvas-elevated"
        >
          <!-- Fixed Header -->
          <header
            class="flex shrink-0 items-center justify-between gap-3 border-b border-slate-200 px-4 py-3 dark:border-white/[0.08]"
          >
            <h2 class="text-base font-semibold text-slate-900 dark:text-white">
              {{ title }}
            </h2>
            <button
              type="button"
              aria-label="Close dialog"
              class="flex h-9 w-9 items-center justify-center rounded-lg text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-900 dark:text-slate-400 dark:hover:bg-white/[0.06] dark:hover:text-white"
              @click="emit('close')"
            >
              <X class="h-4 w-4" :stroke-width="2" />
            </button>
          </header>

          <!-- Scrollable Body -->
          <div class="modal-scroll-container max-h-[60vh] flex-1 overflow-y-auto px-4 py-4">
            <slot />
          </div>

          <!-- Sticky Footer -->
          <footer
            class="flex shrink-0 items-center justify-end gap-2.5 border-t border-slate-200 bg-slate-50/80 px-4 py-3 dark:border-white/[0.08] dark:bg-canvas-subtle/60"
          >
            <slot name="footer">
              <button
                type="button"
                class="h-9 rounded-lg border border-slate-300 px-3.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100 dark:border-white/[0.08] dark:text-slate-300 dark:hover:bg-white/[0.06]"
                @click="emit('close')"
              >
                Cancel
              </button>
              <button
                type="button"
                class="h-9 rounded-lg bg-brand-500 px-3.5 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-brand-600"
                @click="emit('submit')"
              >
                Submit
              </button>
            </slot>
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
