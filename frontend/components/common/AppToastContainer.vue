<script setup lang="ts">
import { CheckCircle2, AlertCircle, Info, AlertTriangle, X } from 'lucide-vue-next'
import { useToast } from '~/composables/useToast'

const { toasts, remove } = useToast()
</script>

<template>
  <div class="fixed top-4 right-4 sm:top-5 sm:right-5 z-[9999] flex flex-col gap-2.5 max-w-[calc(100vw-2rem)] sm:max-w-md w-full pointer-events-none">
    <TransitionGroup
      enter-active-class="transition-all duration-300 ease-out"
      enter-from-class="opacity-0 translate-x-8 scale-95"
      enter-to-class="opacity-100 translate-x-0 scale-100"
      leave-active-class="transition-all duration-200 ease-in"
      leave-from-class="opacity-100 translate-x-0 scale-100"
      leave-to-class="opacity-0 translate-x-8 scale-95"
    >
      <div
        v-for="toast in toasts"
        :key="toast.id"
        :class="[
          'pointer-events-auto flex items-start gap-3 p-4 rounded-2xl shadow-2xl border backdrop-blur-md transition-all duration-200',
          toast.type === 'success'
            ? 'bg-emerald-950/90 border-emerald-500/50 text-emerald-100 shadow-emerald-950/40'
            : toast.type === 'error'
              ? 'bg-rose-950/90 border-rose-500/50 text-rose-100 shadow-rose-950/40'
              : toast.type === 'warning'
                ? 'bg-amber-950/90 border-amber-500/50 text-amber-100 shadow-amber-950/40'
                : 'bg-slate-900/90 border-brand-500/40 text-slate-100 shadow-brand-950/40'
        ]"
      >
        <div class="shrink-0 mt-0.5">
          <CheckCircle2 v-if="toast.type === 'success'" class="w-5 h-5 text-emerald-400" />
          <AlertCircle v-else-if="toast.type === 'error'" class="w-5 h-5 text-rose-400" />
          <AlertTriangle v-else-if="toast.type === 'warning'" class="w-5 h-5 text-amber-400" />
          <Info v-else class="w-5 h-5 text-brand-400" />
        </div>

        <div class="flex-1 text-sm font-semibold leading-snug break-words">
          {{ toast.message }}
        </div>

        <button
          @click="remove(toast.id)"
          type="button"
          aria-label="Close notification"
          class="shrink-0 p-1 -mr-1 -mt-1 rounded-lg text-slate-400 hover:text-white hover:bg-white/10 transition-colors"
        >
          <X class="w-4 h-4" />
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>
