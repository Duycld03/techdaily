<script setup lang="ts">
import { ref, computed, nextTick, type Component } from 'vue'
import { onClickOutside, useEventListener } from '@vueuse/core'
import { ChevronDown, Check } from 'lucide-vue-next'

export interface SelectOption<T = string | number> {
  value: T
  label: string
  icon?: Component
  description?: string
  disabled?: boolean
}

const props = withDefaults(
  defineProps<{
    modelValue: string | number | null | undefined
    options: SelectOption[]
    placeholder?: string
    disabled?: boolean
    icon?: Component
    ariaLabel?: string
    id?: string
    name?: string
    dropdownClass?: string
    teleport?: boolean
  }>(),
  {
    placeholder: 'Select option...',
    disabled: false,
    options: () => [],
    teleport: true
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: string | number): void
  (e: 'change', value: string | number): void
}>()

const isOpen = ref(false)
const selectRef = ref<HTMLElement | null>(null)
const triggerRef = ref<HTMLButtonElement | null>(null)
const listboxRef = ref<HTMLElement | null>(null)
const highlightedIndex = ref(-1)
const floatingStyle = ref<Record<string, string>>({})
const isFlipped = ref(false)
const selectedOption = computed(() => {
  return props.options.find(opt => String(opt.value) === String(props.modelValue))
})

const selectedLabel = computed(() => {
  return selectedOption.value?.label ?? ''
})

const componentId = computed(() => props.id || `app-select-${Math.random().toString(36).slice(2, 9)}`)
const listboxId = computed(() => `${componentId.value}-listbox`)

function updateFloatingPosition() {
  if (!triggerRef.value || typeof window === 'undefined') return
  const rect = triggerRef.value.getBoundingClientRect()
  const width = rect.width > 0 ? rect.width : 240
  const spaceBelow = window.innerHeight - rect.bottom
  const spaceAbove = rect.top
  const placeAbove = spaceBelow < 250 && spaceAbove > spaceBelow

  isFlipped.value = placeAbove
  floatingStyle.value = {
    position: 'fixed',
    left: `${rect.left}px`,
    width: `${width}px`,
    top: placeAbove ? 'auto' : `${rect.bottom + 6}px`,
    bottom: placeAbove ? `${window.innerHeight - rect.top + 6}px` : 'auto',
    zIndex: '60'
  }
}

function handleScroll(event: Event) {
  if (!isOpen.value) return
  if (listboxRef.value && listboxRef.value.contains(event.target as Node)) {
    return
  }
  updateFloatingPosition()
}

function openDropdown() {
  if (props.disabled || isOpen.value) return
  updateFloatingPosition()
  isOpen.value = true
  const selectedIdx = props.options.findIndex(opt => String(opt.value) === String(props.modelValue))
  highlightedIndex.value = selectedIdx >= 0 ? selectedIdx : 0

  nextTick(() => {
    updateFloatingPosition()
    scrollHighlightedIntoView()
  })
}

function closeDropdown(returnFocus = true) {
  if (!isOpen.value) return
  isOpen.value = false
  highlightedIndex.value = -1

  if (returnFocus) {
    triggerRef.value?.focus()
  }
}
function toggleDropdown() {
  if (isOpen.value) {
    closeDropdown()
  } else {
    openDropdown()
  }
}

function selectOption(option: SelectOption) {
  if (option.disabled || props.disabled) return
  emit('update:modelValue', option.value)
  emit('change', option.value)
  closeDropdown()
}

function scrollHighlightedIntoView() {
  if (!listboxRef.value || highlightedIndex.value < 0) return
  const optionEls = listboxRef.value.querySelectorAll('[role="option"]')
  const targetEl = optionEls[highlightedIndex.value] as HTMLElement | undefined
  if (targetEl) {
    targetEl.scrollIntoView({ block: 'nearest' })
  }
}

function handleTriggerKeydown(event: KeyboardEvent) {
  if (props.disabled) return

  switch (event.key) {
    case 'Enter':
    case ' ':
      event.preventDefault()
      if (isOpen.value && highlightedIndex.value >= 0 && highlightedIndex.value < props.options.length) {
        selectOption(props.options[highlightedIndex.value])
      } else {
        toggleDropdown()
      }
      break
    case 'ArrowDown':
      event.preventDefault()
      if (!isOpen.value) {
        openDropdown()
      } else {
        moveHighlight(1)
      }
      break
    case 'ArrowUp':
      event.preventDefault()
      if (!isOpen.value) {
        openDropdown()
      } else {
        moveHighlight(-1)
      }
      break
    case 'Escape':
      if (isOpen.value) {
        event.preventDefault()
        closeDropdown()
      }
      break
    case 'Tab':
      if (isOpen.value) {
        closeDropdown(false)
      }
      break
  }
}

function handleListboxKeydown(event: KeyboardEvent) {
  switch (event.key) {
    case 'Enter':
    case ' ':
      event.preventDefault()
      if (highlightedIndex.value >= 0 && highlightedIndex.value < props.options.length) {
        selectOption(props.options[highlightedIndex.value])
      }
      break
    case 'ArrowDown':
      event.preventDefault()
      moveHighlight(1)
      break
    case 'ArrowUp':
      event.preventDefault()
      moveHighlight(-1)
      break
    case 'Escape':
      event.preventDefault()
      closeDropdown()
      break
    case 'Tab':
      closeDropdown(false)
      break
  }
}

function moveHighlight(direction: number) {
  if (!props.options.length) return
  const total = props.options.length
  let next = (highlightedIndex.value + direction + total) % total

  let loops = 0
  while (props.options[next]?.disabled && loops < total) {
    next = (next + direction + total) % total
    loops++
  }

  highlightedIndex.value = next
  nextTick(() => {
    scrollHighlightedIntoView()
  })
}

onClickOutside(listboxRef, () => {
  if (isOpen.value) closeDropdown(false)
}, { ignore: [triggerRef] })

useEventListener(typeof window !== 'undefined' ? window : null, 'scroll', handleScroll, { capture: true, passive: true })
useEventListener(typeof window !== 'undefined' ? window : null, 'resize', () => {
  if (isOpen.value) {
    updateFloatingPosition()
  }
}, { passive: true })

useEventListener(typeof window !== 'undefined' ? window : null, 'mousedown', (event: MouseEvent) => {
  if (!isOpen.value) return
  const target = event.target as Node
  const isInsideTrigger = triggerRef.value?.contains(target)
  const isInsideListbox = listboxRef.value?.contains(target)
  if (!isInsideTrigger && !isInsideListbox) {
    closeDropdown(false)
  }
})
</script>

<template>
  <div
    ref="selectRef"
    class="relative w-full"
    :id="componentId"
  >
    <!-- Trigger Button -->
    <button
      ref="triggerRef"
      type="button"
      role="combobox"
      :id="`${componentId}-button`"
      :aria-expanded="isOpen"
      :aria-haspopup="'listbox'"
      :aria-controls="listboxId"
      :aria-label="ariaLabel || placeholder"
      :aria-activedescendant="isOpen && highlightedIndex >= 0 ? `${componentId}-opt-${highlightedIndex}` : undefined"
      :disabled="disabled"
      @click="toggleDropdown"
      @keydown="handleTriggerKeydown"
      :class="[
        'w-full flex items-center justify-between gap-2.5 px-3.5 py-2.5 rounded-xl text-left text-xs sm:text-sm font-semibold transition-all border border-slate-200/90 dark:border-white/[0.08] shadow-sm select-none',
        disabled
          ? 'bg-slate-100/60 dark:bg-canvas-subtle/40 text-slate-400 dark:text-slate-600 cursor-not-allowed border-slate-200/60 dark:border-white/[0.04]'
          : isOpen
            ? 'bg-white dark:bg-canvas-elevated text-slate-900 dark:text-white border-brand-500/50 dark:border-brand-500/50 ring-2 ring-brand-500/20'
            : 'bg-white dark:bg-canvas-elevated text-slate-800 dark:text-slate-200 hover:border-slate-300 dark:hover:border-white/[0.16] hover:bg-slate-50/50 dark:hover:bg-white/[0.02] cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500'
      ]"
      data-testid="app-select-trigger"
    >
      <div class="flex items-center gap-2.5 min-w-0 flex-1">
        <component
          :is="selectedOption?.icon || icon"
          v-if="selectedOption?.icon || icon"
          class="w-4 h-4 text-slate-400 shrink-0"
          aria-hidden="true"
        />
        <span
          class="truncate"
          :class="!selectedLabel ? 'text-slate-400 dark:text-slate-500' : ''"
        >
          {{ selectedLabel || placeholder }}
        </span>
      </div>

      <ChevronDown
        class="w-4 h-4 text-slate-400 transition-transform duration-200 shrink-0"
        :class="{ 'rotate-180': isOpen }"
        aria-hidden="true"
      />
    </button>

    <!-- Floating Listbox Dropdown -->
    <Teleport to="body" :disabled="teleport === false">
      <Transition
        enter-active-class="transition duration-150 ease-out"
        enter-from-class="transform scale-98 opacity-0"
        enter-to-class="transform scale-100 opacity-100"
        leave-active-class="transition duration-100 ease-in"
        leave-from-class="transform scale-100 opacity-100"
        leave-to-class="transform scale-98 opacity-0"
      >
        <div
          v-if="isOpen"
          ref="listboxRef"
          :id="listboxId"
          role="listbox"
          tabindex="-1"
          :aria-label="ariaLabel || placeholder"
          @keydown="handleListboxKeydown"
          :style="floatingStyle"
          :class="[
            'glass-panel dark:bg-canvas-elevated border border-slate-200/90 dark:border-white/[0.08] shadow-2xl rounded-2xl p-1.5 backdrop-blur-md max-h-60 overflow-y-auto space-y-0.5 outline-none',
            dropdownClass || ''
          ]"
          data-testid="app-select-listbox"
        >
        <div
          v-for="(option, index) in options"
          :key="option.value"
          :id="`${componentId}-opt-${index}`"
          role="option"
          :aria-selected="String(option.value) === String(modelValue)"
          :aria-disabled="option.disabled"
          @click="selectOption(option)"
          @mouseenter="highlightedIndex = index"
          :class="[
            'w-full flex items-center justify-between gap-2.5 px-3 py-2 rounded-xl text-xs sm:text-sm font-medium transition-all select-none',
            option.disabled
              ? 'text-slate-300 dark:text-slate-600 cursor-not-allowed opacity-50'
              : String(option.value) === String(modelValue)
                ? 'bg-brand-50/80 dark:bg-brand-950/40 text-brand-950 dark:text-brand-300 font-bold border border-brand-200/80 dark:border-brand-800/80 cursor-pointer'
                : highlightedIndex === index
                  ? 'bg-slate-100 dark:bg-white/[0.06] text-slate-900 dark:text-white cursor-pointer'
                  : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06] cursor-pointer'
          ]"
          :data-testid="`app-select-option-${option.value}`"
        >
          <div class="flex items-center gap-2.5 min-w-0 flex-1">
            <component
              :is="option.icon"
              v-if="option.icon"
              class="w-4 h-4 text-slate-400 shrink-0"
              aria-hidden="true"
            />
            <div class="min-w-0 flex-1">
              <div class="truncate">{{ option.label }}</div>
              <div
                v-if="option.description"
                class="text-xs text-slate-400 dark:text-slate-500 font-normal truncate mt-0.5"
              >
                {{ option.description }}
              </div>
            </div>
          </div>

          <Check
            v-if="String(option.value) === String(modelValue)"
            class="w-4 h-4 text-brand-600 dark:text-brand-400 shrink-0 ml-1.5"
            aria-hidden="true"
          />
        </div>
      </div>
    </Transition>
  </Teleport>
  </div>
</template>
