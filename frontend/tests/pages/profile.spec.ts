import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest'
import { mount } from '@vue/test-utils'
import ProfilePage from '~/pages/profile.vue'

describe('profile.vue (Redirect to Unified Settings)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('redirects to /settings?tab=profile on mount to preserve backward compatibility', () => {
    mount(ProfilePage)
    const globalObj = globalThis as unknown as { navigateTo: Mock }
    expect(globalObj.navigateTo).toHaveBeenCalledWith(
      '/settings?tab=profile',
      { replace: true }
    )
  })
})
