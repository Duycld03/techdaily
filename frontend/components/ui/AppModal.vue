<script setup lang="ts">
import { watch, onBeforeUnmount } from 'vue'
import { X } from 'lucide-vue-next'

const props = withDefaults(
  defineProps<{
    open: boolean
    title: string
    maxWidth?: string
  }>(),
  {
    maxWidth: 'max-w-2xl'
  }
)

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
        class="fixed inset-0 z-50 flex items-center justify-center p-3 sm:p-4"
        role="dialog"
        aria-modal="true"
        :aria-label="title"
      >
        <!-- Backdrop -->
        <div
          class="absolute inset-0 bg-slate-900/60 dark:bg-black/75 backdrop-blur-sm"
          @click="emit('close')"
        />

        <!-- Dialog Shell: 3-tier flex column capped at 85vh -->
        <div
          :class="[
            'relative z-10 flex max-h-[85vh] w-full flex-col overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-2xl dark:border-white/[0.08] dark:bg-canvas-elevated',
            maxWidth
          ]"
        >
          <!-- Fixed Header -->
          <header
            class="flex shrink-0 items-center justify-between gap-3 border-b border-slate-200/80 px-4 sm:px-6 py-3.5 dark:border-white/[0.08]"
          >
            <div class="min-w-0 flex-1">
              <slot name="header">
                <h2 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white truncate">
                  {{ title }}
                </h2>
              </slot>
            </div>
            <button
              type="button"
              aria-label="Close dialog"
              class="flex h-8 w-8 items-center justify-center rounded-lg text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-900 dark:hover:bg-white/[0.06] dark:hover:text-white shrink-0 cursor-pointer"
              @click="emit('close')"
            >
              <X class="h-4 w-4" :stroke-width="1.5" />
            </button>
          </header>

          <!-- Scrollable Body -->
          <div class="modal-scroll-container max-h-[60vh] flex-1 overflow-y-auto px-4 sm:px-6 py-4 space-y-4 [scrollbar-gutter:stable]">
            <slot />
          </div>

          <!-- Sticky Footer -->
          <footer
            class="flex shrink-0 items-center justify-end gap-3 border-t border-slate-200/80 bg-slate-50/90 px-4 sm:px-6 py-3 dark:border-white/[0.08] dark:bg-canvas-subtle/80"
          >
            <slot name="footer">
              <button
                type="button"
                class="h-9 rounded-xl border border-slate-300 dark:border-white/[0.08] px-4 text-sm font-semibold text-slate-700 dark:text-slate-300 transition-colors hover:bg-slate-100 dark:hover:bg-white/[0.06] cursor-pointer"
                @click="emit('close')"
              >
                Cancel
              </button>
              <button
                type="button"
                class="h-9 rounded-xl bg-brand-600 px-4 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-brand-500 cursor-pointer"
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
