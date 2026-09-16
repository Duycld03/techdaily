import { describe, it, expect, beforeEach, vi } from 'vitest'
import { useWebPush, urlBase64ToUint8Array } from '~/composables/useWebPush'

const mockPost = vi.fn()
const mockGet = vi.fn()
const mockRequestPermission = vi.fn()
vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    get: mockGet,
    post: mockPost
  })
}))

describe('useWebPush (Browser Web Push & VAPID)', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    // Mock Notification
    mockRequestPermission.mockResolvedValue('granted')
    globalThis.Notification = {
      permission: 'default',
      requestPermission: mockRequestPermission
    } as unknown as typeof Notification

    // Mock PushSubscription
    const mockSubscription = {
      endpoint: 'https://push.example.com/test-ep',
      toJSON: () => ({
        endpoint: 'https://push.example.com/test-ep',
        keys: {
          p256dh: 'test-p256dh',
          auth: 'test-auth'
        }
      }),
      unsubscribe: vi.fn().mockResolvedValue(true)
    }

    // Mock ServiceWorkerRegistration
    const mockRegistration = {
      scope: '/',
      pushManager: {
        getSubscription: vi.fn().mockResolvedValue(mockSubscription),
        subscribe: vi.fn().mockResolvedValue(mockSubscription)
      }
    }

    // Mock navigator.serviceWorker
    Object.defineProperty(global.navigator, 'serviceWorker', {
      value: {
        register: vi.fn().mockResolvedValue(mockRegistration),
        getRegistration: vi.fn().mockResolvedValue(mockRegistration),
        ready: Promise.resolve(mockRegistration)
      },
      writable: true,
      configurable: true
    })

    // Mock PushManager on window
    Object.defineProperty(globalThis, 'PushManager', {
      value: {},
      writable: true,
      configurable: true
    })
  })

  it('urlBase64ToUint8Array converts valid base64url string to Uint8Array', () => {
    const base64 = 'BIk1JIgP1yE25O8mFA3cw4VV1R73s78sKQlnqN9X4LOqnFFW4auAtwtqh8W9XTpmrwWVGSKTb15lNhkyQ5rJnK0'
    const arr = urlBase64ToUint8Array(base64)
    expect(arr).toBeInstanceOf(Uint8Array)
    expect(arr.length).toBeGreaterThan(0)
  })

  it('detects push support accurately in browser environment', () => {
    const { isPushSupported } = useWebPush()
    expect(isPushSupported.value).toBe(true)
  })

  it('checks subscription status from service worker registration', async () => {
    const { checkSubscriptionStatus, isSubscribed } = useWebPush()
    const active = await checkSubscriptionStatus()
    expect(active).toBe(true)
    expect(isSubscribed.value).toBe(true)
  })

  it('successfully subscribes user by fetching VAPID key and posting subscription', async () => {
    mockGet.mockResolvedValueOnce({ publicKey: 'BIk1JIgP1yE25O8mFA3cw4VV1R73s78sKQlnqN9X4LOqnFFW4auAtwtqh8W9XTpmrwWVGSKTb15lNhkyQ5rJnK0' })
    mockPost.mockResolvedValueOnce({ success: true })

    const { subscribeUser, isSubscribed, error } = useWebPush()
    const result = await subscribeUser()

    expect(result).toBe(true)
    expect(isSubscribed.value).toBe(true)
    expect(error.value).toBeNull()

    expect(mockGet).toHaveBeenCalledWith('/api/v1/notifications/push/vapid-public-key')
    expect(mockPost).toHaveBeenCalledWith('/api/v1/notifications/push/subscribe', {
      endpoint: 'https://push.example.com/test-ep',
      keys: {
        p256dh: 'test-p256dh',
        auth: 'test-auth'
      },
      userAgent: expect.any(String)
    })
  })

  it('handles permission denial gracefully without subscribing', async () => {
    mockRequestPermission.mockResolvedValueOnce('denied')

    const { subscribeUser, isSubscribed, error } = useWebPush()
    const result = await subscribeUser()

    expect(result).toBe(false)
    expect(isSubscribed.value).toBe(false)
    expect(error.value).toContain('denied')
    expect(mockGet).not.toHaveBeenCalled()
    expect(mockPost).not.toHaveBeenCalled()
  })

  it('successfully unsubscribes user and notifies backend', async () => {
    mockPost.mockResolvedValueOnce({})

    const { unsubscribeUser, isSubscribed } = useWebPush()
    const result = await unsubscribeUser()

    expect(result).toBe(true)
    expect(isSubscribed.value).toBe(false)
    expect(mockPost).toHaveBeenCalledWith('/api/v1/notifications/push/unsubscribe', {
      endpoint: 'https://push.example.com/test-ep'
    })
  })

  it('sends test push notification via API client', async () => {
    mockPost.mockResolvedValueOnce({ success: true, sent: 1, total: 1 })

    const { sendTestPush } = useWebPush()
    const result = await sendTestPush()

    expect(result.success).toBe(true)
    expect(result.sent).toBe(1)
    expect(mockPost).toHaveBeenCalledWith('/api/v1/notifications/push/test')
  })
})
