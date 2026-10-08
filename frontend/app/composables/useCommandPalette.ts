import { ref } from 'vue'

const fallbackState = ref(false)

export const useCommandPalette = () => {
  const isOpen = typeof useState === 'function'
    ? useState<boolean>('app_command_palette_open', () => false)
    : fallbackState

  const open = () => {
    isOpen.value = true
  }

  const close = () => {
    isOpen.value = false
  }

  const toggle = () => {
    isOpen.value = !isOpen.value
  }

  return {
    isOpen,
    open,
    close,
    toggle
  }
}
