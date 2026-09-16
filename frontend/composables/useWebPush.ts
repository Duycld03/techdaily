import { ref, computed } from 'vue'
import { useApiClient } from '~/composables/useApiClient'

export function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4)
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')
  const rawData = window.atob(base64)
  const outputArray = new Uint8Array(rawData.length)
  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i)
  }
  return outputArray
}

export function useWebPush() {
  const api = useApiClient()
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const isSubscribed = ref(false)

  const isPushSupported = computed(() => {
    if (typeof window === 'undefined') return false
    return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window
  })

  const permissionState = ref<NotificationPermission>(
    typeof window !== 'undefined' && 'Notification' in window
      ? Notification.permission
      : 'default'
  )

  async function checkSubscriptionStatus(): Promise<boolean> {
    if (!isPushSupported.value) return false
    try {
      const reg = await navigator.serviceWorker.getRegistration('/sw.js')
      if (!reg) {
        isSubscribed.value = false
        return false
      }
      const sub = await reg.pushManager.getSubscription()
      isSubscribed.value = !!sub
      return !!sub
    } catch {
      isSubscribed.value = false
      return false
    }
  }

  async function subscribeUser(): Promise<boolean> {
    if (!isPushSupported.value) {
      error.value = 'Push notifications are not supported by this browser.'
      return false
    }

    isLoading.value = true
    error.value = null

    try {
      const permission = await Notification.requestPermission()
      permissionState.value = permission
      if (permission !== 'granted') {
        error.value = 'Notification permission was denied.'
        return false
      }

      // 1. Register service worker
      const reg = await navigator.serviceWorker.register('/sw.js')
      await navigator.serviceWorker.ready

      // 2. Fetch VAPID public key
      const { publicKey } = await api.get<{ publicKey: string }>('/api/v1/notifications/push/vapid-public-key')
      if (!publicKey) {
        throw new Error('VAPID public key not found on server.')
      }

      // 3. Subscribe to push manager
      const applicationServerKey = urlBase64ToUint8Array(publicKey)
      let sub: PushSubscription
      try {
        sub = await reg.pushManager.subscribe({
          userVisibleOnly: true,
          applicationServerKey
        })
      } catch (err: unknown) {
        const nav = typeof navigator !== 'undefined' ? (navigator as unknown as { brave?: { isBrave?: () => unknown } }) : null
        const isBrave = Boolean(nav?.brave && typeof nav.brave.isBrave === 'function')
        const errMsg = err instanceof Error ? err.message : String(err)
        if (
          errMsg.includes('push service error') ||
          errMsg.includes('Registration failed') ||
          isBrave
        ) {
          error.value = 'settings.brave_push_service_blocked'
          throw new Error('settings.brave_push_service_blocked')
        }
        throw err
      }

      // 4. Extract keys and send to backend
      const json = sub.toJSON()
      if (!json.endpoint || !json.keys?.p256dh || !json.keys?.auth) {
        throw new Error('Incomplete push subscription payload from browser.')
      }

      const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC'

      await api.post('/api/v1/notifications/push/subscribe', {
        endpoint: json.endpoint,
        keys: {
          p256dh: json.keys.p256dh,
          auth: json.keys.auth
        },
        userAgent: navigator.userAgent,
        timeZone
      })
      isSubscribed.value = true
      return true
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to subscribe to push notifications.'
      error.value = msg
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function unsubscribeUser(): Promise<boolean> {
    if (!isPushSupported.value) return false
    isLoading.value = true
    error.value = null

    try {
      const reg = await navigator.serviceWorker.getRegistration('/sw.js')
      if (reg) {
        const sub = await reg.pushManager.getSubscription()
        if (sub) {
          const endpoint = sub.endpoint
          await sub.unsubscribe()
          await api.post('/api/v1/notifications/push/unsubscribe', { endpoint })
        }
      }
      isSubscribed.value = false
      return true
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to unsubscribe.'
      error.value = msg
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function sendTestPush(): Promise<{ success: boolean; sent: number }> {
    isLoading.value = true
    error.value = null
    try {
      const res = await api.post<{ success: boolean; sent: number; total: number }>(
        '/api/v1/notifications/push/test'
      )
      return res
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to send test push notification.'
      error.value = msg
      throw err
    } finally {
      isLoading.value = false
    }
  }

  return {
    isPushSupported,
    permissionState,
    isSubscribed,
    isLoading,
    error,
    checkSubscriptionStatus,
    subscribeUser,
    unsubscribeUser,
    sendTestPush
  }
}
