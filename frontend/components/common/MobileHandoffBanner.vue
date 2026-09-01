<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { Sparkles, Smartphone, X, ArrowRight } from 'lucide-vue-next'

const authStore = useAuthStore()
const profileStore = useProfileStore()
const isDismissed = ref(true)

onMounted(async () => {
  if (typeof window !== 'undefined') {
    const dismissed = localStorage.getItem('techdaily_dismiss_mobile_handoff')
    if (!dismissed && authStore.isLoggedIn) {
      if (!profileStore.profile) {
        await profileStore.fetchProfile()
      }
      if (profileStore.profile && !profileStore.profile.hasPassword) {
        isDismissed.value = false
      }
    }
  }
})

const shouldShow = computed(() => {
  return authStore.isLoggedIn && !isDismissed.value && profileStore.profile && !profileStore.profile.hasPassword
})

function dismissBanner() {
  isDismissed.value = true
  if (typeof window !== 'undefined') {
    localStorage.setItem('techdaily_dismiss_mobile_handoff', 'true')
  }
}
</script>

<template>
  <div
    v-if="shouldShow"
    class="w-full bg-gradient-to-r from-brand-900/90 via-slate-900/90 to-brand-950/90 border-b border-brand-500/30 text-white px-4 sm:px-6 py-2.5 sm:py-3 transition-all duration-300 flex items-center justify-between gap-3 shadow-md shrink-0 animate-in fade-in slide-in-from-top-2"
  >
    <div class="flex items-center gap-3 min-w-0">
      <div class="p-1.5 rounded-lg bg-brand-500/20 border border-brand-400/30 shrink-0 text-brand-400">
        <Smartphone class="w-4 h-4" />
      </div>
      <div class="min-w-0">
        <span class="text-xs sm:text-sm font-bold text-brand-200 mr-2">{{ $t('profile.mobile_handoff_banner_title') }}:</span>
        <span class="text-xs sm:text-sm text-slate-300 truncate hidden md:inline">{{ $t('profile.mobile_handoff_banner_desc') }}</span>
      </div>
    </div>

    <div class="flex items-center gap-2 sm:gap-3 shrink-0">
      <NuxtLink
        to="/profile"
        class="inline-flex items-center gap-1 px-3 py-1 rounded-lg bg-brand-500 hover:bg-brand-400 text-slate-950 text-xs sm:text-sm font-bold shadow-sm transition-all active:scale-95"
      >
        <span>{{ $t('profile.mobile_handoff_banner_btn') }}</span>
        <ArrowRight class="w-3.5 h-3.5" />
      </NuxtLink>
      <button
        @click="dismissBanner"
        aria-label="Dismiss banner"
        class="p-1 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
      >
        <X class="w-4 h-4" />
      </button>
    </div>
  </div>
</template>
