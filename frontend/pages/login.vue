<script setup lang="ts">
import { ref } from 'vue'
import { BookOpen, ShieldCheck, Zap, ArrowRight } from 'lucide-vue-next'

const authStore = useAuthStore()
const router = useRouter()
const isLoading = ref(false)
const error = ref<string | null>(null)

async function handleDevLogin() {
  isLoading.value = true
  error.value = null
  try {
    await authStore.devLogin()
    router.push('/today')
  } catch (err: any) {
    error.value = err.message || 'Login failed'
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="min-h-[calc(100vh-3.5rem)] flex items-center justify-center p-6 bg-slate-950">
    <div class="w-full max-w-md p-8 rounded-3xl bg-slate-900 border border-slate-800 shadow-2xl space-y-6">
      <!-- Logo -->
      <div class="text-center">
        <div class="w-12 h-12 rounded-2xl bg-gradient-to-tr from-brand-600 to-emerald-400 flex items-center justify-center mx-auto mb-3 shadow-lg shadow-brand-500/20">
          <BookOpen class="w-6 h-6 text-slate-950 font-bold" />
        </div>
        <h1 class="text-2xl font-extrabold text-white tracking-tight">
          {{ $t('auth.welcome_title') }}
        </h1>
        <p class="text-xs text-slate-400 mt-1">
          {{ $t('auth.welcome_subtitle') }}
        </p>
      </div>

      <div v-if="error" class="p-3 rounded-xl bg-red-950/40 border border-red-900 text-xs text-red-300 text-center">
        {{ error }}
      </div>

      <!-- Dev 1-Click Login (Recommended for rapid testing!) -->
      <div class="p-4 rounded-2xl bg-slate-950/80 border border-brand-800/40 space-y-3">
        <div class="flex items-center gap-2 text-xs font-bold text-brand-400 uppercase tracking-wider">
          <Zap class="w-4 h-4 text-brand-400" />
          <span>Local Development Mode</span>
        </div>
        <p class="text-xs text-slate-400 leading-relaxed">
          {{ $t('auth.dev_login_desc') }}
        </p>
        <button
          @click="handleDevLogin"
          :disabled="isLoading"
          class="w-full flex items-center justify-center gap-2 py-3 rounded-xl bg-brand-600 hover:bg-brand-500 text-slate-950 font-bold text-sm shadow-md transition-all active:scale-[0.98]"
        >
          <span>{{ $t('auth.dev_login_btn') }}</span>
          <ArrowRight class="w-4 h-4" />
        </button>
      </div>

      <!-- Google OAuth Sign In -->
      <div class="space-y-3">
        <div class="relative flex items-center justify-center">
          <div class="w-full border-t border-slate-800"></div>
          <span class="px-3 bg-slate-900 text-[11px] text-slate-500 font-semibold uppercase">Or continue with</span>
        </div>

        <button
          @click="handleDevLogin"
          class="w-full flex items-center justify-center gap-3 py-3 rounded-xl bg-slate-800 hover:bg-slate-700 border border-slate-700 text-white font-semibold text-sm transition-all"
        >
          <svg class="w-4 h-4" viewBox="0 0 24 24">
            <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
            <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
            <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z"/>
            <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z"/>
          </svg>
          <span>{{ $t('auth.google_sign_in') }}</span>
        </button>
      </div>

      <div class="text-center">
        <p class="text-[11px] text-slate-500">
          By signing in, you agree to TechDaily terms and privacy policy.
        </p>
      </div>
    </div>
  </div>
</template>
