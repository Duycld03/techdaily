import { ref } from 'vue'

export type ToastType = 'success' | 'error' | 'info' | 'warning'

export interface ToastItem {
  id: string
  type: ToastType
  message: string
  duration?: number
}

const toasts = ref<ToastItem[]>([])

export function useToast() {
  function show(message: string, type: ToastType = 'info', duration = 3500): string {
    const id = `${Date.now()}-${Math.random().toString(36).substring(2, 9)}`
    const toast: ToastItem = { id, message, type, duration }
    toasts.value.push(toast)

    if (duration > 0 && typeof window !== 'undefined') {
      setTimeout(() => {
        remove(id)
      }, duration)
    }
    return id
  }

  function success(message: string, duration = 3500): string {
    return show(message, 'success', duration)
  }

  function error(message: string, duration = 4000): string {
    return show(message, 'error', duration)
  }

  function info(message: string, duration = 3500): string {
    return show(message, 'info', duration)
  }

  function warning(message: string, duration = 4000): string {
    return show(message, 'warning', duration)
  }

  function remove(id: string): void {
    toasts.value = toasts.value.filter(t => t.id !== id)
  }

  function clear(): void {
    toasts.value = []
  }

  return {
    toasts,
    show,
    success,
    error,
    info,
    warning,
    remove,
    clear
  }
}
