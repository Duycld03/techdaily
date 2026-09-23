<script setup lang="ts">
import { onMounted } from 'vue'

if (typeof definePageMeta === 'function') {
  definePageMeta({
    middleware: [
      () => navigateTo('/settings?tab=profile', { redirectCode: 301, replace: true })
    ]
  })
}

onMounted(() => {
  navigateTo('/settings?tab=profile', { replace: true })
})
</script>

<template>
  <div class="flex items-center justify-center min-h-[50vh]">
    <div class="w-6 h-6 border-2 border-brand-500 border-t-transparent rounded-full animate-spin" />
  </div>
</template>
