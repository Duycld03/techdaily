<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { LoaderCircle, ShieldCheck, RefreshCw } from 'lucide-vue-next'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
import { useAuthStore } from '~/stores/useAuthStore'
import { loginRedirectFor } from '~/utils/adminRoutes'

const access = useAdminAccessStore()
const auth = useAuthStore()
const deferredRoute = useRoute()
const router = useRouter()
const route = computed(() => router.currentRoute?.value || deferredRoute)
async function retry() {
  if (!auth.isLoggedIn && !await auth.tryRefreshToken()) {
    if (auth.lastRefreshError === 503) { access.setUnavailable(); return }
    return navigateTo(loginRedirectFor(route.value.fullPath))
  }
  const allowed = await access.verify()
  if (!allowed && access.state === 'denied') {
    await navigateTo(loginRedirectFor(route.value.fullPath))
    return
  }
  if (allowed && !access.canAccessDestination(route.value.fullPath)) {
    const firstAccessible = access.getFirstAccessibleAdminRoute()
    if (firstAccessible !== '/login') {
      await navigateTo(firstAccessible)
    } else {
      await navigateTo(loginRedirectFor(route.value.fullPath))
    }
  }
}
onMounted(() => {
  if (access.state === 'pending' || (access.isAuthorized && !access.canAccessDestination(route.value.fullPath))) {
    void retry()
  }
})
</script>

<template>
  <div class="flex min-h-dvh items-center justify-center p-6">
    <div class="flex w-full max-w-md flex-col items-center gap-5 text-center" role="status" aria-live="polite" data-admin-boundary>
      <ShieldCheck class="h-12 w-12 text-brand-500" :stroke-width="1.5" aria-hidden="true" />
      <template v-if="access.state === 'unavailable'">
        <p class="text-sm sm:text-base text-slate-600 dark:text-slate-300">{{ $t('admin.access.unavailable') }}</p>
        <button type="button" class="inline-flex min-h-11 items-center gap-2 rounded-xl bg-brand-600 px-5 text-sm sm:text-base font-semibold text-white whitespace-nowrap shrink-0" @click="retry">
          <RefreshCw class="h-4 w-4" aria-hidden="true" />{{ $t('admin.access.retry') }}
        </button>
      </template>
      <template v-else>
        <LoaderCircle class="h-6 w-6 animate-spin text-brand-500" aria-hidden="true" />
        <p class="text-sm sm:text-base text-slate-600 dark:text-slate-300">{{ $t('admin.access.checking') }}</p>
      </template>
    </div>
  </div>
</template>
