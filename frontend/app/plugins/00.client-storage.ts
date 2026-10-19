import { CLIENT_STORAGE, isCurrentSessionToken } from '~/utils/clientStorage'
import { browserMigrationEnvironment, hasLegacySessionCookies, migrateLegacyStorage } from '~/utils/legacyStorageMigration'
import { useAuthStore } from '~/stores/useAuthStore'
import { useClientAdmission } from '~/composables/useClientAdmission'
import { loginRedirectFor } from '~/utils/adminRoutes'

export default defineNuxtPlugin(async (app) => {
  const { state } = useClientAdmission()
  if (import.meta.server) {
    const canonical = useCookie<string | null>(CLIENT_STORAGE.token).value
    const refresh = useCookie<string | null>('refreshToken').value
    const retired = useCookie(CLIENT_STORAGE.identityRetired).value
    const legacyCandidate = !canonical && !retired && hasLegacySessionCookies(key => useCookie(key).value)
    if (!isCurrentSessionToken(canonical) && (refresh || legacyCandidate)) state.value = 'pending'
    return
  }

  await migrateLegacyStorage(browserMigrationEnvironment())
  const auth = useAuthStore(app.$pinia)
  auth.init(true)
  // Cache transfer is independent of session admission and has no remote side effects.
  void import('~/utils/legacyAudioMigration').then(module => module.migrateLegacyAudio()).catch(() => {})
  const onStorage = (event: StorageEvent) => {
    if (event.key !== CLIENT_STORAGE.identityRetired || !event.newValue) return
    auth.invalidateStoredSession()
    void app.runWithContext(() => navigateTo(loginRedirectFor(app.$router.currentRoute.value.fullPath)))
  }
  window.addEventListener('storage', onStorage)
  app.vueApp.onUnmount(() => window.removeEventListener('storage', onStorage))
})
