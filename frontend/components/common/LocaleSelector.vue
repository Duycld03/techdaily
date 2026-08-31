<script setup lang="ts">
const { locale, setLocale } = useI18n()

function changeLocale(newLocale: string) {
  setLocale(newLocale as any)
}
</script>

<template>
  <div class="flex items-center rounded-xl bg-slate-100 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 p-0.5 text-xs font-semibold transition-colors">
    <button
      @click="changeLocale('en')"
      :class="[
        'px-2.5 py-1 rounded-lg transition-all',
        locale === 'en'
          ? 'bg-white dark:bg-slate-800 text-brand-700 dark:text-brand-400 font-bold shadow-sm'
          : 'text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
      ]"
    >
      EN
    </button>
    <button
      @click="changeLocale('vi')"
      :class="[
        'px-2.5 py-1 rounded-lg transition-all',
        locale === 'vi'
          ? 'bg-white dark:bg-slate-800 text-brand-700 dark:text-brand-400 font-bold shadow-sm'
          : 'text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
      ]"
    >
      VI
    </button>
  </div>
</template>
