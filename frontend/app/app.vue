<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useAuthStore } from '~/stores/useAuthStore'
import AppHeader from '~/components/layout/AppHeader.vue'
import AppSidebar from '~/components/layout/AppSidebar.vue'
import AppToastContainer from '~/components/common/AppToastContainer.vue'
import AppCommandPalette from '~/components/app/AppCommandPalette.vue'
import AdminShell from '~/components/admin/AdminShell.vue'
import AdminAuthorizationBoundary from '~/components/admin/AdminAuthorizationBoundary.vue'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
import { isAdminRoute, isAdminLogin } from '~/utils/adminRoutes'
const deferredRoute = useRoute()
const router = useRouter()
// Shell transitions must follow the committed router location even when the
// authorization boundary temporarily replaces NuxtPage and delays page finish.
const route = computed(() => router.currentRoute?.value || deferredRoute)
const authStore = useAuthStore()
const adminAccess = useAdminAccessStore()
const isAdminPage = computed(() => isAdminRoute(route.value.path) && !isAdminLogin(route.value.path))

const isReaderMode = computed(() => route.value.path.startsWith('/read'))
const isAuthPage = computed(() => {
  const normalizedPath = route.value.path.replace(/\/+$/, '') || '/'
  return (
    normalizedPath === '/login' ||
    normalizedPath === '/register' ||
    normalizedPath === '/forgot-password' ||
    normalizedPath === '/reset-password' ||
    isAdminLogin(normalizedPath)
  )
})

onMounted(() => {
  authStore.init()
})
</script>

<template>
  <div class="min-h-dvh flex flex-col bg-slate-50 dark:bg-canvas text-slate-900 dark:text-slate-100 antialiased transition-colors duration-200">
    <AppToastContainer />

    <!-- Auth Cockpit Shell: Isolated from internal navigation chrome & persistent containers -->
    <template v-if="isAuthPage">
      <NuxtPage :page-key="route => route.fullPath" />
    </template>

    <template v-else-if="isAdminPage">
      <AdminShell v-if="adminAccess.isAuthorized"><NuxtPage :page-key="route => route.fullPath" /></AdminShell>
      <AdminAuthorizationBoundary v-else />
    </template>

    <!-- Standard Application Shell -->
    <template v-else>
      <AppHeader v-if="!isReaderMode" />
      <AppCommandPalette />
      <div class="flex-1 flex overflow-hidden">
        <AppSidebar v-if="!isReaderMode" />
        <main class="flex-1 overflow-y-auto">
          <NuxtPage :page-key="route => route.fullPath" />
        </main>
      </div>
    </template>
  </div>
</template>
