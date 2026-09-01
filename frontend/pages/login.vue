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
const googleBtnContainer = ref<HTMLElement | null>(null)

onMounted(() => {
  authStore.init()
  if (authStore.isLoggedIn) {
    navigateTo('/today')
    return
  }

  initGoogleButton()
})

watch(() => authStore.isLoggedIn, (loggedIn) => {
  if (loggedIn) {
    navigateTo('/today')
  }
})

function initGoogleButton() {
  if (typeof window === 'undefined') return

  const interval = setInterval(() => {
    if ((window as any).google?.accounts?.id) {
      clearInterval(interval)
      try {
        ;(window as any).google.accounts.id.initialize({
          client_id: config.public.googleClientId,
          callback: handleGoogleCredentialResponse,
          auto_select: false,
          cancel_on_tap_outside: true
        })
        const btnContainer = googleBtnContainer.value || document.getElementById('google-signin-btn')
        if (btnContainer) {
          ;(window as any).google.accounts.id.renderButton(btnContainer, {
            theme: colorMode.value === 'dark' ? 'filled_black' : 'outline',
            size: 'large',
            width: '100%',
            text: 'continue_with',
            shape: 'rectangular',
            logo_alignment: 'left'
          })
        }
      } catch (e) {
        console.warn('Google Sign-In initialization:', e)
      }
    }
  }, 200)

  setTimeout(() => clearInterval(interval), 10000)
}

async function handleGoogleCredentialResponse(response: any) {
  if (!response?.credential) return

  isLoading.value = true
  try {
    await authStore.googleLogin(response.credential)
    toast.success('Đăng nhập Google thành công!')
    await navigateTo('/today')
  } catch (err: any) {
    toast.error(err.message || 'Google authentication failed')
  } finally {
    isLoading.value = false
  }
}

async function handleSubmit() {
  if (!email.value || !password.value) {
    toast.error('Please enter your email and password.')
    return
  }

  isLoading.value = true

  try {
    if (authMode.value === 'login') {
      await authStore.login(email.value, password.value)
      toast.success('Đăng nhập thành công!')
    } else {
      await authStore.register(email.value, password.value, name.value, locale.value)
      toast.success('Đăng ký tài khoản thành công!')
    }
    await navigateTo('/today')
  } catch (err: any) {
    toast.error(err.message || 'Authentication failed')
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="min-h-[calc(100vh-3.5rem)] sm:min-h-[calc(100vh-3.75rem)] flex items-center justify-center p-3.5 sm:p-6 bg-slate-50 dark:bg-slate-950 transition-colors duration-200">
    <div class="w-full max-w-md p-5 sm:p-10 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xl dark:shadow-2xl space-y-5 sm:space-y-6 animate-in zoom-in-95 duration-200">
      <!-- Brand Header -->
      <div class="text-center">
        <div class="w-12 h-12 sm:w-14 sm:h-14 rounded-2xl bg-gradient-to-tr from-brand-600 to-emerald-400 flex items-center justify-center mx-auto mb-3.5 sm:mb-4 shadow-lg shadow-brand-500/20">
          <BookOpen class="w-6 h-6 sm:w-7 sm:h-7 text-slate-950 font-bold" />
        </div>
        <h1 class="text-xl sm:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">
          {{ authMode === 'login' ? 'Sign In to TechDaily' : 'Create Your Account' }}
        </h1>
        <p class="text-sm md:text-base text-slate-500 dark:text-slate-400 mt-1.5 font-medium">
          Master Senior Software Engineering Daily
        </p>
      </div>

      <!-- Mode Switcher Tabs -->
      <div class="flex p-1.5 rounded-2xl bg-slate-100 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 text-sm md:text-base font-semibold">
        <button
          type="button"
          @click="authMode = 'login'"
          :class="[
            'flex-1 py-2.5 rounded-xl transition-all outline-none focus:outline-none',
            authMode === 'login'
              ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 font-bold shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 font-medium'
          ]"
        >
          {{ $t('auth.sign_in_tab') }}
        </button>
        <button
          type="button"
          @click="authMode = 'register'"
          :class="[
            'flex-1 py-2.5 rounded-xl transition-all outline-none focus:outline-none',
            authMode === 'register'
              ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 font-bold shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 font-medium'
          ]"
        >
          {{ $t('auth.register_tab') }}
        </button>
      </div>

      <!-- Email & Password Form -->
      <form @submit.prevent="handleSubmit" class="space-y-4">
        <div v-if="authMode === 'register'">
          <label class="block text-sm md:text-base font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('auth.name_label') }}</label>
          <div class="relative">
            <User class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
            <input
              v-model="name"
              type="text"
              class="w-full pl-10 pr-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none transition-colors"
            />
          </div>
        </div>

        <div>
          <label class="block text-sm md:text-base font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('auth.email_label') }}</label>
          <div class="relative">
            <Mail class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
            <input
              v-model="email"
              required
              type="email"
              placeholder="you@example.com"
              class="w-full pl-10 pr-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none transition-colors"
            />
          </div>
        </div>

        <div>
          <label class="block text-sm md:text-base font-bold text-slate-700 dark:text-slate-300 mb-1.5">{{ $t('auth.password_label') }}</label>
          <div class="relative">
            <Lock class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
            <input
              v-model="password"
              required
              type="password"
              minlength="6"
              placeholder="••••••••"
              class="w-full pl-10 pr-4 py-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-600 focus:border-brand-500 focus:outline-none transition-colors"
            />
          </div>
        </div>

        <button
          type="submit"
          :disabled="isLoading"
          class="w-full flex items-center justify-center gap-2 py-3.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-sm md:text-base shadow-lg shadow-brand-500/20 transition-all active:scale-[0.98] disabled:opacity-50"
        >
          <span v-if="isLoading" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
          <span>{{ authMode === 'login' ? $t('auth.submit_sign_in') : $t('auth.submit_register') }}</span>
          <ArrowRight v-if="!isLoading" class="w-4 h-4" />
        </button>
      </form>

      <!-- Google OAuth Sign-In Divider -->
      <div class="space-y-4">
        <div class="relative flex items-center justify-center">
          <div class="w-full border-t border-slate-200 dark:border-slate-800"></div>
          <span class="px-3 bg-white dark:bg-slate-900 text-xs sm:text-sm text-slate-500 font-semibold uppercase">{{ $t('auth.or_continue_with') }}</span>
        </div>

        <!-- Google OAuth Button Container -->
        <div class="flex flex-col items-center justify-center min-h-[44px]">
          <div ref="googleBtnContainer" class="flex justify-center"></div>
        </div>
      </div>

      <div class="text-center">
        <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 leading-relaxed">
          {{ $t('auth.terms_agreement') }}
        </p>
      </div>
    </div>
  </div>
</template>
