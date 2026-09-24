<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useAuthStore } from '~/stores/useAuthStore'
import AppHeader from '~/components/layout/AppHeader.vue'
import AppSidebar from '~/components/layout/AppSidebar.vue'
import AppToastContainer from '~/components/common/AppToastContainer.vue'
import AppCommandPalette from '~/components/app/AppCommandPalette.vue'
const route = useRoute()
const authStore = useAuthStore()

const isReaderMode = computed(() => route.path.startsWith('/read'))
const isAuthPage = computed(() => route.path === '/login')

onMounted(() => {
  authStore.init()
})
</script>

<template>
  <div class="min-h-dvh flex flex-col bg-slate-50 dark:bg-canvas text-slate-900 dark:text-slate-100 antialiased transition-colors duration-200">
    <AppHeader v-if="!isReaderMode" />
    <AppCommandPalette />
    <AppToastContainer />
    <div class="flex-1 flex overflow-hidden">
      <AppSidebar v-if="!isReaderMode && !isAuthPage" />
      <main class="flex-1 overflow-y-auto">
        <NuxtPage />
      </main>
    </div>
  </div>
</template>
