<script setup lang="ts">
import { shallowRef, useTemplateRef, watch, nextTick } from 'vue'
import { onKeyStroke } from '@vueuse/core'
import AdminHeader from './AdminHeader.vue'
import AdminSidebar from './AdminSidebar.vue'
import AppModal from '~/components/ui/AppModal.vue'

const route = useRoute()
const mobileOpen = shallowRef(false)
const drawerBody = useTemplateRef<HTMLDivElement>('drawerBody')
let previousFocus: HTMLElement | null = null
const focusable = () => Array.from(drawerBody.value?.closest('[role="dialog"]')?.querySelectorAll<HTMLElement>('a[href], button:not([disabled]), [tabindex="0"]') || [])
watch(() => route.fullPath, () => { mobileOpen.value = false })
watch(mobileOpen, async open => {
  if (open) { previousFocus = document.activeElement as HTMLElement; await nextTick(); focusable()[0]?.focus() }
  else { previousFocus?.focus() }
})
onKeyStroke('Tab', event => {
  if (!mobileOpen.value) return
  const elements = focusable()
  const first = elements[0]
  const last = elements.at(-1)
  if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus() }
  else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus() }
})
</script>

<template>
  <div class="flex h-dvh min-h-0 flex-col overflow-hidden">
    <AdminHeader @menu="mobileOpen = true" />
    <div class="flex min-h-0 flex-1">
      <AdminSidebar />
      <main id="admin-main" class="min-w-0 flex-1 overflow-y-auto"><slot /></main>
    </div>
    <AppModal :open="mobileOpen" :title="$t('admin.nav.title')" :close-label="$t('admin.nav.close')" max-width="max-w-sm" @close="mobileOpen = false">
      <div ref="drawerBody"><AdminSidebar mobile @navigate="mobileOpen = false" /></div>
      <template #footer><button type="button" class="min-h-11 rounded-xl border border-slate-300 dark:border-white/[0.08] px-4 text-sm sm:text-base font-semibold whitespace-nowrap shrink-0" @click="mobileOpen = false">{{ $t('admin.nav.close') }}</button></template>
    </AppModal>
  </div>
</template>
