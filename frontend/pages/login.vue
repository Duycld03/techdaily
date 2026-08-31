<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { BookOpen, Zap, ArrowRight } from 'lucide-vue-next'

const authStore = useAuthStore()
const router = useRouter()
const config = useRuntimeConfig()

const isLoading = ref(false)
const error = ref<string | null>(null)
const googleBtnContainer = ref<HTMLDivElement | null>(null)

const googleClientId = (config.public.googleClientId as string) || '982684500709-75solmbterlbdvut85btisallcsf83ef.apps.googleusercontent.com'

onMounted(() => {
  initGoogleAuth()
})

function initGoogleAuth() {
  if (typeof window === 'undefined') return

  const interval = setInterval(() => {
    if ((window as any).google?.accounts?.id) {
      clearInterval(interval)
      const google = (window as any).google

      google.accounts.id.initialize({
        client_id: googleClientId,
        callback: handleGoogleCredentialResponse
      })

      if (googleBtnContainer.value) {
        google.accounts.id.renderButton(googleBtnContainer.value, {
          theme: 'filled_black',
          size: 'large',
          shape: 'rectangular',
          width: 320,
          text: 'signin_with'
        })
      }
    }
  }, 200)

  setTimeout(() => clearInterval(interval), 10000)
}

async function handleGoogleCredentialResponse(response: any) {
  if (!response?.credential) return

  isLoading.value = true
  error.value = null
  try {
    await authStore.googleLogin(response.credential)
    router.push('/today')
  } catch (err: any) {
    error.value = err.message || 'Google authentication failed'
  } finally {
    isLoading.value = false
  }
}

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

      <!-- Real Google OAuth Sign-In Button Container -->
      <div class="space-y-3">
        <div class="flex flex-col items-center justify-center min-h-[44px]">
          <div ref="googleBtnContainer" class="flex justify-center"></div>
        </div>
      </div>

      <!-- Dev 1-Click Login (For rapid local fallback) -->
      <div class="p-4 rounded-2xl bg-slate-950/80 border border-slate-800/80 space-y-2.5">
        <div class="flex items-center gap-2 text-xs font-bold text-slate-400 uppercase tracking-wider">
          <Zap class="w-3.5 h-3.5 text-amber-400" />
          <span>Local Development Mode</span>
        </div>
        <p class="text-[11px] text-slate-500 leading-relaxed">
          {{ $t('auth.dev_login_desc') }}
        </p>
        <button
          @click="handleDevLogin"
          :disabled="isLoading"
          class="w-full flex items-center justify-center gap-2 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 font-semibold text-xs border border-slate-700 transition-all active:scale-[0.98]"
        >
          <span>{{ $t('auth.dev_login_btn') }}</span>
          <ArrowRight class="w-3.5 h-3.5" />
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
