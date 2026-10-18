import { nextTick, onScopeDispose, watch, type Ref } from 'vue'
import { useEventListener } from '@vueuse/core'

export function useLibraryFilterDialog(open: () => boolean, dialog: Ref<HTMLElement | null>, close: () => void) {
  let trigger: HTMLElement | null = null
  let previousOverflow = ''
  let active = false

  function restore() {
    if (!active) return
    active = false
    document.body.style.overflow = previousOverflow
    if (trigger?.isConnected) trigger.focus()
  }

  watch(open, async isOpen => {
    if (typeof document === 'undefined') return
    if (!isOpen) {
      restore()
      return
    }
    trigger = document.activeElement instanceof HTMLElement ? document.activeElement : null
    previousOverflow = document.body.style.overflow
    active = true
    document.body.style.overflow = 'hidden'
    await nextTick()
    if (open()) dialog.value?.querySelector<HTMLButtonElement>('button:not([disabled])')?.focus()
  }, { immediate: true, flush: 'post' })

  useEventListener(dialog, 'keydown', event => {
    if (event.key === 'Escape') {
      event.preventDefault()
      close()
      return
    }
    if (event.key !== 'Tab') return
    const controls = dialog.value?.querySelectorAll<HTMLElement>('button:not([disabled]), [href], input:not([disabled]), [tabindex="0"]')
    const first = controls?.[0]
    const last = controls?.[controls.length - 1]
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault()
      last?.focus()
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault()
      first?.focus()
    }
  })
  onScopeDispose(restore)
}
