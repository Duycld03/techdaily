<script setup lang="ts">
import { Languages } from 'lucide-vue-next'

const { locale, setLocale } = useI18n()

function changeLocale(newLocale: string) {
  setLocale(newLocale as any)
}
</script>

<template>
  <div class="flex items-center rounded-lg bg-slate-900 border border-slate-800 p-0.5 text-xs font-medium">
    <button
      @click="changeLocale('en')"
      :class="[
        'px-2 py-1 rounded transition-colors',
        locale === 'en' ? 'bg-slate-800 text-brand-400 font-semibold shadow-sm' : 'text-slate-400 hover:text-slate-200'
      ]"
    >
      EN
    </button>
    <button
      @click="changeLocale('vi')"
      :class="[
        'px-2 py-1 rounded transition-colors',
        locale === 'vi' ? 'bg-slate-800 text-brand-400 font-semibold shadow-sm' : 'text-slate-400 hover:text-slate-200'
      ]"
    >
      VI
    </button>
  </div>
</template>
