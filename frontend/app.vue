<script setup lang="ts">
import { onMounted } from 'vue'
import AppHeader from '~/components/layout/AppHeader.vue'
import AppSidebar from '~/components/layout/AppSidebar.vue'

const authStore = useAuthStore()

onMounted(() => {
  authStore.init()
})
</script>

<template>
  <div class="min-h-screen flex flex-col bg-slate-950 text-slate-100 antialiased selection:bg-brand-500/30 selection:text-brand-300">
    <AppHeader />
    <div class="flex-1 flex overflow-hidden">
      <AppSidebar />
      <main class="flex-1 overflow-y-auto">
        <NuxtPage />
      </main>
    </div>
  </div>
</template>
