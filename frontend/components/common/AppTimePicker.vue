<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import { onClickOutside, useEventListener } from '@vueuse/core'
import { Clock, ChevronDown } from 'lucide-vue-next'

const props = withDefaults(
  defineProps<{
    modelValue: string | null | undefined
    id?: string
    disabled?: boolean
    ariaLabel?: string
    placeholder?: string
    teleport?: boolean
  }>(),
  {
    modelValue: '08:00',
    disabled: false,
    placeholder: '08:00 AM',
    teleport: true
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'change', value: string): void
}>()

const isOpen = ref(false)
const containerRef = ref<HTMLElement | null>(null)
const triggerRef = ref<HTMLButtonElement | null>(null)
const popoverRef = ref<HTMLElement | null>(null)
const floatingStyle = ref<Record<string, string>>({})
const isFlipped = ref(false)

const minuteOptions = [0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55]

const quickPresets = [
  { label: '07:00 AM', value: '07:00' },
  { label: '08:00 AM', value: '08:00' },
  { label: '08:00 PM', value: '20:00' },
  { label: '09:00 PM', value: '21:00' }
]

function parseTimeTo12h(timeStr: string | null | undefined) {
  const safeStr = timeStr && timeStr.includes(':') ? timeStr : '08:00'
  const [hStr, mStr] = safeStr.split(':')
  const h24 = parseInt(hStr, 10) || 0
  const m = parseInt(mStr, 10) || 0
  const period: 'AM' | 'PM' = h24 >= 12 ? 'PM' : 'AM'
  const h12 = h24 % 12 === 0 ? 12 : h24 % 12
  return { hour12: h12, minute: m, period }
}

function format12hTo24h(hour12: number, minute: number, period: 'AM' | 'PM') {
  let h = hour12 % 12
  if (period === 'PM') h += 12
  return `${String(h).padStart(2, '0')}:${String(minute).padStart(2, '0')}`
}

const parsedTime = computed(() => parseTimeTo12h(props.modelValue))
const currentHour12 = computed(() => parsedTime.value.hour12)
const currentMinute = computed(() => parsedTime.value.minute)
const currentPeriod = computed(() => parsedTime.value.period)

const formattedDisplayTime = computed(() => {
  const { hour12, minute, period } = parsedTime.value
  return `${String(hour12).padStart(2, '0')}:${String(minute).padStart(2, '0')} ${period}`
})

const componentId = computed(() => props.id || `app-time-picker-${Math.random().toString(36).slice(2, 9)}`)

function updateFloatingPosition() {
  if (!triggerRef.value || typeof window === 'undefined') return
  const rect = triggerRef.value.getBoundingClientRect()
  const width = Math.max(rect.width, 280)
  const spaceBelow = window.innerHeight - rect.bottom
  const spaceAbove = rect.top
  const placeAbove = spaceBelow < 320 && spaceAbove > spaceBelow

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

function openDropdown() {
  if (props.disabled || isOpen.value) return
  updateFloatingPosition()
  isOpen.value = true
  nextTick(() => {
    updateFloatingPosition()
  })
}

function closeDropdown(returnFocus = true) {
  if (!isOpen.value) return
  isOpen.value = false
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

function setHour(h: number) {
  const new24 = format12hTo24h(h, currentMinute.value, currentPeriod.value)
  emit('update:modelValue', new24)
  emit('change', new24)
}

function setMinute(m: number) {
  const new24 = format12hTo24h(currentHour12.value, m, currentPeriod.value)
  emit('update:modelValue', new24)
  emit('change', new24)
}

function setPeriod(p: 'AM' | 'PM') {
  const new24 = format12hTo24h(currentHour12.value, currentMinute.value, p)
  emit('update:modelValue', new24)
  emit('change', new24)
}

function selectPreset(val: string) {
  emit('update:modelValue', val)
  emit('change', val)
  closeDropdown()
}

function handleScroll(event: Event) {
  if (!isOpen.value) return
  if (popoverRef.value && popoverRef.value.contains(event.target as Node)) {
    return
  }
  updateFloatingPosition()
}

onClickOutside(
  containerRef,
  () => {
    closeDropdown(false)
  },
  { ignore: [triggerRef, popoverRef] }
)

if (typeof window !== 'undefined') {
  useEventListener(window, 'resize', () => {
    if (isOpen.value) updateFloatingPosition()
  })
  useEventListener(window, 'scroll', handleScroll, { capture: true, passive: true })
}
</script>

<template>
  <div ref="containerRef" class="relative w-full" :id="componentId">
    <!-- Trigger Button -->
    <button
      ref="triggerRef"
      type="button"
      role="combobox"
      :id="`${componentId}-trigger`"
      :aria-expanded="isOpen"
      :aria-label="ariaLabel || placeholder"
      :disabled="disabled"
      @click="toggleDropdown"
      @keydown.esc="closeDropdown"
      :class="[
        'w-full flex items-center justify-between gap-2.5 px-3.5 py-2.5 rounded-xl text-left text-xs sm:text-sm font-semibold transition-all border border-slate-200/90 dark:border-white/[0.08] shadow-sm select-none',
        disabled
          ? 'bg-slate-100/60 dark:bg-canvas-subtle/40 text-slate-400 dark:text-slate-600 cursor-not-allowed border-slate-200/60 dark:border-white/[0.04]'
          : isOpen
            ? 'bg-white dark:bg-canvas-elevated text-slate-900 dark:text-white border-brand-500/50 dark:border-brand-500/50 ring-2 ring-brand-500/20'
            : 'bg-white dark:bg-canvas-subtle text-slate-800 dark:text-slate-200 hover:border-slate-300 dark:hover:border-white/[0.16] hover:bg-slate-50/50 dark:hover:bg-white/[0.02] cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500'
      ]"
      data-testid="app-time-picker-trigger"
    >
      <div class="flex items-center gap-2.5 min-w-0 flex-1">
        <Clock class="w-4 h-4 text-slate-400 shrink-0" :stroke-width="1.5" />
        <span class="font-mono tracking-wide truncate">
          {{ formattedDisplayTime }}
        </span>
      </div>

      <ChevronDown
        class="w-4 h-4 text-slate-400 transition-transform duration-200 shrink-0"
        :class="{ 'rotate-180': isOpen }"
        :stroke-width="1.5"
      />
    </button>

    <!-- Floating Popover -->
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
          ref="popoverRef"
          :id="`${componentId}-popover`"
          role="dialog"
          :aria-label="ariaLabel || 'Time Picker'"
          :style="floatingStyle"
          @keydown.esc="closeDropdown"
          class="glass-panel dark:bg-canvas-elevated border border-slate-200/90 dark:border-white/[0.08] shadow-2xl rounded-2xl p-3.5 backdrop-blur-md space-y-3 outline-none"
          data-testid="app-time-picker-popover"
        >
          <!-- Quick Presets -->
          <div class="space-y-1.5">
            <div class="text-[10px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 px-1">
              {{ $t('settings.quick_presets') || 'Quick Presets' }}
            </div>
            <div class="grid grid-cols-2 gap-1.5">
              <button
                v-for="preset in quickPresets"
                :key="preset.value"
                type="button"
                @click="selectPreset(preset.value)"
                :class="[
                  'px-2 py-1.5 rounded-lg text-xs font-semibold font-mono transition-all text-center',
                  modelValue === preset.value
                    ? 'bg-brand-600 text-white shadow-sm'
                    : 'bg-slate-100 dark:bg-white/[0.04] text-slate-700 dark:text-slate-300 hover:bg-brand-500/10 hover:text-brand-600 dark:hover:text-brand-400 border border-slate-200/60 dark:border-white/[0.04]'
                ]"
                :data-testid="`time-preset-${preset.value}`"
              >
                {{ preset.label }}
              </button>
            </div>
          </div>

          <!-- 3 Columns (Hours, Minutes, Period) -->
          <div class="border-t border-slate-200/60 dark:border-white/[0.06] pt-2">
            <div class="grid grid-cols-3 gap-2">
              <!-- Hours Column -->
              <div class="space-y-1">
                <div class="text-[10px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 text-center">
                  {{ $t('settings.time_hour') || 'Hour' }}
                </div>
                <div class="max-h-36 overflow-y-auto space-y-1 p-1 rounded-xl bg-slate-50/70 dark:bg-black/20 border border-slate-200/60 dark:border-white/[0.04] scrollbar-thin">
                  <button
                    v-for="h in 12"
                    :key="h"
                    type="button"
                    @click="setHour(h)"
                    :class="[
                      'w-full py-1 rounded-lg text-xs font-mono font-semibold transition-all text-center cursor-pointer',
                      currentHour12 === h
                        ? 'bg-brand-600 text-white shadow-sm font-bold'
                        : 'text-slate-700 dark:text-slate-300 hover:bg-slate-200/60 dark:hover:bg-white/[0.06]'
                    ]"
                    :data-testid="`time-hour-${h}`"
                  >
                    {{ String(h).padStart(2, '0') }}
                  </button>
                </div>
              </div>

              <!-- Minutes Column -->
              <div class="space-y-1">
                <div class="text-[10px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 text-center">
                  {{ $t('settings.time_minute') || 'Min' }}
                </div>
                <div class="max-h-36 overflow-y-auto space-y-1 p-1 rounded-xl bg-slate-50/70 dark:bg-black/20 border border-slate-200/60 dark:border-white/[0.04] scrollbar-thin">
                  <button
                    v-for="m in minuteOptions"
                    :key="m"
                    type="button"
                    @click="setMinute(m)"
                    :class="[
                      'w-full py-1 rounded-lg text-xs font-mono font-semibold transition-all text-center cursor-pointer',
                      currentMinute === m
                        ? 'bg-brand-600 text-white shadow-sm font-bold'
                        : 'text-slate-700 dark:text-slate-300 hover:bg-slate-200/60 dark:hover:bg-white/[0.06]'
                    ]"
                    :data-testid="`time-minute-${m}`"
                  >
                    {{ String(m).padStart(2, '0') }}
                  </button>
                </div>
              </div>

              <!-- Period Column (AM/PM) -->
              <div class="space-y-1">
                <div class="text-[10px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 text-center">
                  {{ $t('settings.time_period') || 'Period' }}
                </div>
                <div class="flex flex-col gap-1.5 p-1 rounded-xl bg-slate-50/70 dark:bg-black/20 border border-slate-200/60 dark:border-white/[0.04]">
                  <button
                    type="button"
                    @click="setPeriod('AM')"
                    :class="[
                      'w-full py-2.5 rounded-lg text-xs font-bold transition-all text-center cursor-pointer',
                      currentPeriod === 'AM'
                        ? 'bg-brand-600 text-white shadow-sm'
                        : 'text-slate-700 dark:text-slate-300 hover:bg-slate-200/60 dark:hover:bg-white/[0.06]'
                    ]"
                    data-testid="time-period-am"
                  >
                    AM
                  </button>
                  <button
                    type="button"
                    @click="setPeriod('PM')"
                    :class="[
                      'w-full py-2.5 rounded-lg text-xs font-bold transition-all text-center cursor-pointer',
                      currentPeriod === 'PM'
                        ? 'bg-brand-600 text-white shadow-sm'
                        : 'text-slate-700 dark:text-slate-300 hover:bg-slate-200/60 dark:hover:bg-white/[0.06]'
                    ]"
                    data-testid="time-period-pm"
                  >
                    PM
                  </button>
                </div>
              </div>
            </div>
          </div>

          <!-- Bottom Footer -->
          <div class="pt-2 border-t border-slate-200/60 dark:border-white/[0.06] flex items-center justify-between">
            <span class="text-[11px] font-mono text-slate-400 dark:text-slate-500">
              {{ modelValue || '08:00' }}
            </span>
            <button
              type="button"
              @click="closeDropdown"
              class="px-3.5 py-1 rounded-lg bg-brand-600 hover:bg-brand-500 text-white text-xs font-bold transition-all shadow-sm active:scale-95 cursor-pointer"
              data-testid="time-picker-done-btn"
            >
              {{ $t('settings.btn_done') || 'Done' }}
            </button>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
