<script setup lang="ts">
import { shallowRef, onMounted } from 'vue'
import { ShieldCheck, LibraryBig, LayoutDashboard, GraduationCap } from 'lucide-vue-next'
import StudioLayout from '~/components/layout/StudioLayout.vue'
import AdminLoginForm from '~/components/admin/AdminLoginForm.vue'
import ThemeToggle from '~/components/common/ThemeToggle.vue'
import LocaleSelector from '~/components/common/LocaleSelector.vue'
import { useAuthStore } from '~/stores/useAuthStore'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
import { useApiError } from '~/composables/useApiError'
import { safeAdminReturnPath } from '~/utils/adminRoutes'

const auth = useAuthStore()
const access = useAdminAccessStore()
const route = useRoute()
const { t } = useI18n()
const { formatError } = useApiError()
const submitting = shallowRef(false)
const error = shallowRef<string | null>(null)
async function verifyAndEnter() {
  const allowed = await access.verify()
  if (allowed) await navigateTo(safeAdminReturnPath(route.query.redirect))
}
async function submit(payload: { email: string; password: string; rememberMe: boolean }) {
  if (submitting.value) return
  submitting.value = true
  error.value = null
  try { await auth.login(payload.email, payload.password, payload.rememberMe); await verifyAndEnter() }
  catch (failure: unknown) { error.value = formatError(failure) }
  finally { submitting.value = false }
}
onMounted(() => { if (auth.isLoggedIn && access.state === 'pending') void verifyAndEnter() })
</script>

<template>
  <div class="flex min-h-dvh flex-col gap-4 p-3 sm:p-6 lg:p-8" data-admin-login>
    <header class="flex items-center justify-between gap-3">
      <NuxtLink to="/admin/login" class="flex items-center gap-2 text-lg font-bold whitespace-nowrap shrink-0"><img src="/favicon.svg" alt="" class="h-8 w-8" />DeepPace</NuxtLink>
      <div class="flex items-center gap-2 shrink-0"><LocaleSelector touch-targets /><ThemeToggle :light-label="t('admin.preferences.light')" :dark-label="t('admin.preferences.dark')" :toggle-label="t('admin.preferences.theme')" class="min-h-11 min-w-11" /></div>
    </header>
    <StudioLayout class="flex-1" :main-label="$t('admin.login.title')" :dock-label="$t('admin.login.workspace_title')">
      <template #main>
        <div class="mx-auto w-full max-w-md space-y-6 py-5 sm:py-8">
          <div class="space-y-3">
            <div class="flex h-12 w-12 items-center justify-center rounded-2xl border border-brand-500/20 bg-brand-500/10 text-brand-600 dark:text-brand-300"><ShieldCheck class="h-6 w-6" :stroke-width="1.5" aria-hidden="true" /></div>
            <h1 class="text-2xl sm:text-3xl font-bold tracking-tight">{{ $t('admin.login.title') }}</h1>
            <p class="text-sm sm:text-base leading-relaxed text-slate-600 dark:text-slate-400">{{ $t('admin.login.description') }}</p>
          </div>
          <p v-if="access.state === 'denied'" role="alert" class="rounded-xl border border-amber-500/20 bg-amber-500/5 p-4 text-sm sm:text-base text-amber-800 dark:text-amber-200">{{ $t('admin.login.denied') }}</p>
          <div v-if="access.state === 'unavailable'" class="space-y-3 rounded-xl border border-amber-500/20 bg-amber-500/5 p-4">
            <p role="status" class="text-sm sm:text-base text-amber-800 dark:text-amber-200">{{ $t('admin.access.unavailable') }}</p>
            <button type="button" class="min-h-11 rounded-xl bg-brand-600 px-4 text-sm sm:text-base font-semibold text-white whitespace-nowrap shrink-0" :disabled="access.state === 'checking'" @click="verifyAndEnter">{{ $t('admin.access.retry') }}</button>
          </div>
          <AdminLoginForm :submitting="submitting" :error="error" :initial-email="auth.user?.email" :initial-remember="auth.isPersistentSession" @submit="submit" />
        </div>
      </template>
      <template #dock>
        <div class="flex flex-1 flex-col justify-center gap-6 py-3 sm:py-6">
          <div><p class="mb-2 text-sm sm:text-base font-semibold text-brand-600 dark:text-brand-300">{{ $t('admin.nav.workspace') }}</p><h2 class="text-xl sm:text-2xl font-bold">{{ $t('admin.login.workspace_title') }}</h2></div>
          <div v-for="item in [{ icon: LayoutDashboard, key: 'overview' }, { icon: LibraryBig, key: 'catalog' }, { icon: ShieldCheck, key: 'permission' }]" :key="item.key" class="flex items-start gap-3">
            <component :is="item.icon" class="mt-1 h-5 w-5 shrink-0 text-brand-500" :stroke-width="1.5" aria-hidden="true" />
            <p class="text-sm sm:text-base leading-relaxed text-slate-600 dark:text-slate-300">{{ $t(`admin.login.context_${item.key}`) }}</p>
          </div>
        </div>
      </template>
      <template #footer>
        <div class="flex w-full flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <p class="text-sm sm:text-base text-slate-500 dark:text-slate-400">{{ $t('admin.login.shared_session') }}</p>
          <NuxtLink to="/today" class="inline-flex min-h-11 items-center justify-center gap-2 rounded-xl border border-slate-300 dark:border-white/[0.08] px-4 text-sm sm:text-base font-semibold whitespace-nowrap shrink-0"><GraduationCap class="h-5 w-5" aria-hidden="true" />{{ $t('admin.nav.learning') }}</NuxtLink>
        </div>
      </template>
    </StudioLayout>
  </div>
</template>
