<script setup lang="ts">
import { onMounted } from 'vue'
import { LoaderCircle, RefreshCw } from 'lucide-vue-next'
import { useAuthStore } from '~/stores/useAuthStore'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
import { useClientAdmission } from '~/composables/useClientAdmission'
import { isAdminRoute, loginRedirectFor } from '~/utils/adminRoutes'

const auth = useAuthStore()
const access = useAdminAccessStore()
const { state } = useClientAdmission()
const router = useRouter()

async function retry() {
  state.value = 'pending'
  auth.init()
  if (!auth.isLoggedIn && !await auth.tryRefreshToken()) {
    if (auth.lastRefreshError === 503) { state.value = 'unavailable'; return }
    return navigateTo(loginRedirectFor(router.currentRoute.value.fullPath))
  }
  if (isAdminRoute(router.currentRoute.value.path)) {
    const allowed = await access.verify()
    if (!allowed) {
      if (access.state === 'denied') return navigateTo(loginRedirectFor(router.currentRoute.value.fullPath))
      state.value = 'unavailable'
      return
    }
  }
  state.value = 'ready'
}
onMounted(retry)
</script>

<template>
  <div class="flex min-h-dvh items-center justify-center p-6">
    <div role="status" aria-live="polite" data-session-boundary class="flex w-full max-w-md flex-col items-center gap-5 text-center">
      <LoaderCircle v-if="state === 'pending'" class="h-7 w-7 animate-spin text-brand-500" aria-hidden="true" />
      <p class="text-sm sm:text-base text-slate-600 dark:text-slate-300">{{ $t(state === 'unavailable' ? 'auth.admission_unavailable' : 'auth.admission_checking') }}</p>
      <button v-if="state === 'unavailable'" type="button" class="inline-flex min-h-11 items-center gap-2 rounded-xl bg-brand-600 px-5 text-sm sm:text-base font-semibold text-white whitespace-nowrap shrink-0" @click="retry">
        <RefreshCw class="h-4 w-4" aria-hidden="true" />{{ $t('auth.admission_retry') }}
      </button>
    </div>
  </div>
</template>
