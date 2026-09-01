import { describe, it, expect, beforeEach } from 'vitest'
import { useToast } from '~/composables/useToast'

describe('useToast', () => {
  beforeEach(() => {
    const toast = useToast()
    toast.clear()
  })

  it('adds and removes toast messages correctly', () => {
    const toast = useToast()
    expect(toast.toasts.value.length).toBe(0)

    const id1 = toast.success('Operation succeeded!')
    expect(toast.toasts.value.length).toBe(1)
    expect(toast.toasts.value[0].message).toBe('Operation succeeded!')
    expect(toast.toasts.value[0].type).toBe('success')

    const id2 = toast.error('Something went wrong!')
    expect(toast.toasts.value.length).toBe(2)
    expect(toast.toasts.value[1].type).toBe('error')

    toast.remove(id1)
    expect(toast.toasts.value.length).toBe(1)
    expect(toast.toasts.value[0].id).toBe(id2)
  })

  it('supports info and warning toasts', () => {
    const toast = useToast()
    toast.info('New tip available')
    toast.warning('Check your connection')

    expect(toast.toasts.value.length).toBe(2)
    expect(toast.toasts.value[0].type).toBe('info')
    expect(toast.toasts.value[1].type).toBe('warning')
  })
})
