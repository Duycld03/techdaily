<script setup lang="ts">
const { locale, setLocale } = useI18n()

async function changeLocale(newLocale: string) {
  await setLocale(newLocale as any)
}
</script>

<template>
  <div class="flex items-center rounded-xl bg-slate-100 dark:bg-canvas-elevated border border-slate-200/80 dark:border-white/[0.08] p-0.5 text-xs font-semibold transition-colors">
    <button
      @click="changeLocale('en')"
      :class="[
        'px-2.5 py-1 rounded-lg transition-colors outline-none focus:outline-none',
        locale === 'en'
          ? 'bg-white dark:bg-canvas-subtle text-brand-700 dark:text-brand-400 font-bold shadow-sm border border-transparent dark:border-white/[0.06]'
          : 'text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200 border border-transparent'
      ]"
    >
      EN
    </button>
    <button
      @click="changeLocale('vi')"
      :class="[
        'px-2.5 py-1 rounded-lg transition-colors outline-none focus:outline-none',
        locale === 'vi'
          ? 'bg-white dark:bg-canvas-subtle text-brand-700 dark:text-brand-400 font-bold shadow-sm border border-transparent dark:border-white/[0.06]'
          : 'text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200 border border-transparent'
      ]"
    >
      VI
    </button>
  </div>
</template>
