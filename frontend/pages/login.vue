<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { BookOpen, Lock, Mail, User, ArrowRight } from 'lucide-vue-next'

const authStore = useAuthStore()
const router = useRouter()
const config = useRuntimeConfig()
const { locale } = useI18n()
const colorMode = useColorMode()

const authMode = ref<'login' | 'register'>('login')
const email = ref('')
const password = ref('')
const name = ref('')

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
          theme: colorMode.value === 'dark' ? 'filled_black' : 'outline',
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

async function handleSubmit() {
  if (!email.value || !password.value) {
    error.value = 'Please enter your email and password.'
    return
  }

  isLoading.value = true
  error.value = null

  try {
    if (authMode.value === 'login') {
      await authStore.login(email.value, password.value)
    } else {
      await authStore.register(email.value, password.value, name.value, locale.value)
    }
    router.push('/today')
  } catch (err: any) {
    error.value = err.message || 'Authentication failed'
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="min-h-[calc(100vh-3.75rem)] flex items-center justify-center p-6 bg-slate-50 dark:bg-slate-950 transition-colors duration-200">
    <div class="w-full max-w-md p-8 sm:p-10 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xl dark:shadow-2xl space-y-6 animate-in zoom-in-95 duration-200">
      <!-- Brand Header -->
      <div class="text-center">
        <div class="w-14 h-14 rounded-2xl bg-gradient-to-tr from-brand-600 to-emerald-400 flex items-center justify-center mx-auto mb-4 shadow-lg shadow-brand-500/20">
          <BookOpen class="w-7 h-7 text-slate-950 font-bold" />
        </div>
        <h1 class="text-2xl sm:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">
          {{ authMode === 'login' ? 'Sign In to TechDaily' : 'Create Your Account' }}
        </h1>
        <p class="text-sm text-slate-500 dark:text-slate-400 mt-1.5 font-medium">
          Master Senior Software Engineering Daily
        </p>
      </div>

      <!-- Mode Switcher Tabs -->
      <div class="flex p-1.5 rounded-2xl bg-slate-100 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 text-sm font-semibold">
        <button
          type="button"
          @click="authMode = 'login'; error = null"
          :class="[
            'flex-1 py-2.5 rounded-xl transition-all',
            authMode === 'login' ? 'bg-brand-600 text-white shadow-md font-semibold' : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
          ]"
        >
          Sign In
        </button>
        <button
          type="button"
          @click="authMode = 'register'; error = null"
          :class="[
            'flex-1 py-2.5 rounded-xl transition-all',
            authMode === 'register' ? 'bg-brand-600 text-white shadow-md font-semibold' : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
          ]"
        >
          Register
        </button>
      </div>

      <!-- Error Alert -->
      <div v-if="error" class="p-4 rounded-2xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900 text-sm text-rose-700 dark:text-rose-300 text-center font-medium animate-in fade-in">
        {{ error }}
      </div>

      <!-- Email & Password Form -->
      <form @submit.prevent="handleSubmit" class="space-y-4">
        <div v-if="authMode === 'register'">
          <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">Full Name</label>
          <div class="relative">
            <User class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
            <input
              v-model="name"
              type="text"
              placeholder="Your Name (e.g. Senior Architect)"
              class="w-full pl-10 pr-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none transition-colors"
            />
          </div>
        </div>

        <div>
          <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">Email Address</label>
          <div class="relative">
            <Mail class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
            <input
              v-model="email"
              required
              type="email"
              placeholder="you@example.com"
              class="w-full pl-10 pr-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none transition-colors"
            />
          </div>
        </div>

        <div>
          <label class="block text-xs sm:text-sm font-bold text-slate-700 dark:text-slate-300 mb-1.5">Password</label>
          <div class="relative">
            <Lock class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
            <input
              v-model="password"
              required
              type="password"
              minlength="6"
              placeholder="•••••••• (at least 6 chars)"
              class="w-full pl-10 pr-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none transition-colors"
            />
          </div>
        </div>

        <button
          type="submit"
          :disabled="isLoading"
          class="w-full flex items-center justify-center gap-2 py-3.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-sm shadow-lg shadow-brand-500/20 transition-all active:scale-[0.98] disabled:opacity-50"
        >
          <span v-if="isLoading" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
          <span>{{ authMode === 'login' ? 'Sign In' : 'Create Account' }}</span>
          <ArrowRight v-if="!isLoading" class="w-4 h-4" />
        </button>
      </form>

      <!-- Google OAuth Sign-In Divider -->
      <div class="space-y-4">
        <div class="relative flex items-center justify-center">
          <div class="w-full border-t border-slate-200 dark:border-slate-800"></div>
          <span class="px-3 bg-white dark:bg-slate-900 text-xs text-slate-500 font-semibold uppercase">Or continue with</span>
        </div>

        <!-- Google OAuth Button Container -->
        <div class="flex flex-col items-center justify-center min-h-[44px]">
          <div ref="googleBtnContainer" class="flex justify-center"></div>
        </div>
      </div>

      <div class="text-center">
        <p class="text-xs text-slate-500 dark:text-slate-500">
          By continuing, you agree to TechDaily terms and privacy policy.
        </p>
      </div>
    </div>
  </div>
</template>
