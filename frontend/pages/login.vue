<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useIntervalFn } from '@vueuse/core'
import { BookOpen, Lock, Mail, User, ArrowRight, AlertCircle, Eye, EyeOff } from 'lucide-vue-next'
import { useApiError } from '~/composables/useApiError'
import { useAuthStore } from '~/stores/useAuthStore'

const authStore = useAuthStore()
const router = useRouter()
const config = useRuntimeConfig()
const { t, locale } = useI18n()
const { formatError } = useApiError()
const toast = useToast()
const colorMode = useColorMode()

const authMode = ref<'login' | 'register'>('login')
const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const name = ref('')
const showPassword = ref(false)
const showConfirmPassword = ref(false)
const isLoading = ref(false)
const errorMessage = ref('')
const googleBtnContainer = ref<HTMLElement | null>(null)

function setAuthMode(mode: 'login' | 'register') {
  authMode.value = mode
  errorMessage.value = ''
  confirmPassword.value = ''
}

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

let googleInitAttempts = 0
const { pause: stopGooglePoll, resume: startGooglePoll } = useIntervalFn(() => {
  googleInitAttempts++
  if (typeof window !== 'undefined' && (window as any).google?.accounts?.id) {
    stopGooglePoll()
    if (!config.public.googleClientId) {
      console.warn('[TechDaily Auth] Google Client ID is not configured. Google Sign-In is disabled.')
      return
    }
    try {
      ;(window as any).google.accounts.id.initialize({
        client_id: config.public.googleClientId,
        callback: handleGoogleCredentialResponse,
        auto_select: false,
        cancel_on_tap_outside: true
      })
      renderGoogleButton()
    } catch (e) {
      console.warn('Google Sign-In initialization:', e)
    }
  } else if (googleInitAttempts >= 50) {
    stopGooglePoll()
  }
}, 200, { immediate: false })

function renderGoogleButton() {
  const gsi = (window as any).google?.accounts?.id
  const btnContainer = googleBtnContainer.value
  if (!config.public.googleClientId || !gsi || !btnContainer) return
  btnContainer.innerHTML = ''
  gsi.renderButton(btnContainer, {
    theme: colorMode.value === 'dark' ? 'filled_black' : 'outline',
    size: 'large',
    width: 320,
    text: 'continue_with',
    shape: 'rectangular',
    logo_alignment: 'left'
  })
}

watch(() => colorMode.value, () => {
  renderGoogleButton()
})

function initGoogleButton() {
  if (typeof window === 'undefined') return
  googleInitAttempts = 0
  startGooglePoll()
}

async function handleGoogleCredentialResponse(response: any) {
  if (!response?.credential) return

  isLoading.value = true
  try {
    await authStore.googleLogin(response.credential)
    toast.success(t('auth.toast_google_success'))
    await navigateTo('/today')
  } catch (err: any) {
    const rawError = (err as any)?.data?.error || (err as any)?.response?._data?.error
    console.error('[TechDaily Auth] Google login failed:', err, rawError)
    const formatted = formatError(err, 'auth.toast_google_failed')
    errorMessage.value = rawError ? `${formatted} (${rawError})` : formatted
    toast.error(errorMessage.value)
  } finally {
    isLoading.value = false
  }
}

async function handleSubmit() {
  errorMessage.value = ''
  if (!email.value || !password.value) {
    errorMessage.value = t('auth.toast_enter_credentials')
    toast.error(t('auth.toast_enter_credentials'))
    return
  }
  if (authMode.value === 'register') {
    if (!name.value.trim()) {
      errorMessage.value = t('auth.toast_name_required')
      toast.error(t('auth.toast_name_required'))
      return
    }
    if (password.value.length < 8) {
      const formatted = t('api_errors.AUTH_PASSWORD_TOO_SHORT')
      errorMessage.value = formatted
      toast.error(formatted)
      return
    }
    if (password.value !== confirmPassword.value) {
      errorMessage.value = t('auth.toast_passwords_mismatch')
      toast.error(t('auth.toast_passwords_mismatch'))
      return
    }
  }
  isLoading.value = true

  try {
    if (authMode.value === 'login') {
      await authStore.login(email.value, password.value)
      toast.success(t('auth.toast_login_success'))
    } else {
      await authStore.register(email.value, password.value, name.value, locale.value)
      toast.success(t('auth.toast_register_success'))
    }
    await navigateTo('/today')
  } catch (err: any) {
    const formatted = formatError(err, 'auth.toast_auth_failed')
    errorMessage.value = formatted
    toast.error(formatted)
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="min-h-[calc(100dvh-3.5rem)] sm:min-h-[calc(100dvh-3.75rem)] flex items-center justify-center p-3.5 sm:p-6 bg-slate-50 dark:bg-canvas transition-colors duration-200">
    <div class="w-full max-w-md p-6 sm:p-8 rounded-3xl glass-panel shadow-2xl space-y-5 sm:space-y-6 animate-in zoom-in-95 duration-200">
      <!-- Brand Header -->
      <div class="text-center">
        <div class="w-12 h-12 sm:w-14 sm:h-14 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center mx-auto mb-3.5 sm:mb-4 shadow-lg shadow-brand-500/20">
          <BookOpen class="w-6 h-6 sm:w-7 sm:h-7" :stroke-width="1.5" />
        </div>
        <h1 class="text-xl sm:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">
          {{ authMode === 'login' ? $t('auth.welcome_title') : $t('auth.register_title') }}
        </h1>
        <p class="text-sm md:text-base text-slate-500 dark:text-slate-400 mt-1.5 font-medium">
          {{ $t('auth.welcome_subtitle') }}
        </p>
      </div>

      <!-- Mode Switcher Tabs -->
      <div class="flex p-1.5 rounded-2xl bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-sm md:text-base font-semibold">
        <button
          type="button"
          @click="setAuthMode('login')"
          :class="[
            'flex-1 py-2.5 rounded-xl transition-colors duration-150 outline-none focus:outline-none cursor-pointer border',
            authMode === 'login'
              ? 'bg-white dark:bg-white/[0.08] text-brand-600 dark:text-white font-bold shadow-sm border-slate-200/80 dark:border-white/[0.06]'
              : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 font-medium'
          ]"
        >
          {{ $t('auth.sign_in_tab') }}
        </button>
        <button
          type="button"
          @click="setAuthMode('register')"
          :class="[
            'flex-1 py-2.5 rounded-xl transition-colors duration-150 outline-none focus:outline-none cursor-pointer border',
            authMode === 'register'
              ? 'bg-white dark:bg-white/[0.08] text-brand-600 dark:text-white font-bold shadow-sm border-slate-200/80 dark:border-white/[0.06]'
              : 'border-transparent text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 font-medium'
          ]"
        >
          {{ $t('auth.register_tab') }}
        </button>
      </div>

      <!-- Accessible RFC 7807 Error Alert -->
      <div
        v-if="errorMessage"
        role="alert"
        class="p-3.5 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-700 dark:text-rose-300 text-xs sm:text-sm font-medium flex items-start gap-2.5 animate-in fade-in duration-200"
      >
        <AlertCircle class="w-4 h-4 text-rose-500 shrink-0 mt-0.5" :stroke-width="1.5" />
        <span class="flex-1">{{ errorMessage }}</span>
      </div>

      <!-- Email & Password Form -->
      <form @submit.prevent="handleSubmit" class="space-y-4">
        <div v-if="authMode === 'register'" class="space-y-1.5 animate-in fade-in slide-in-from-top-2 duration-200">
          <label class="block text-sm md:text-base font-bold text-slate-700 dark:text-slate-300">
            {{ $t('auth.name_label') }}
          </label>
          <div class="relative">
            <User class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
            <input
              v-model="name"
              type="text"
              required
              :placeholder="$t('auth.name_placeholder')"
              class="w-full pl-11 pr-4 py-3 bg-slate-50 dark:bg-canvas-subtle border border-slate-200 dark:border-white/[0.08] rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 focus:outline-none transition-all"
            />
          </div>
        </div>

        <div class="space-y-1.5">
          <label class="block text-sm md:text-base font-bold text-slate-700 dark:text-slate-300">
            {{ $t('auth.email_label') }}
          </label>
          <div class="relative">
            <Mail class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
            <input
              v-model="email"
              required
              type="email"
              placeholder="you@example.com"
              class="w-full pl-11 pr-4 py-3 bg-slate-50 dark:bg-canvas-subtle border border-slate-200 dark:border-white/[0.08] rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 focus:outline-none transition-all"
            />
          </div>
        </div>

        <div class="space-y-1.5">
          <label class="block text-sm md:text-base font-bold text-slate-700 dark:text-slate-300">
            {{ $t('auth.password_label') }}
          </label>
          <div class="relative">
            <Lock class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
            <input
              v-model="password"
              required
              :type="showPassword ? 'text' : 'password'"
              :minlength="authMode === 'register' ? 8 : undefined"
              :placeholder="authMode === 'register' ? $t('auth.password_placeholder') : '••••••••'"
              class="w-full pl-11 pr-11 py-3 bg-slate-50 dark:bg-canvas-subtle border border-slate-200 dark:border-white/[0.08] rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 focus:outline-none transition-all"
            />
            <button
              type="button"
              @click="showPassword = !showPassword"
              class="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 focus:outline-none transition-colors"
              :aria-label="showPassword ? 'Hide password' : 'Show password'"
            >
              <EyeOff v-if="showPassword" class="w-4 h-4" :stroke-width="1.5" />
              <Eye v-else class="w-4 h-4" :stroke-width="1.5" />
            </button>
          </div>
        </div>

        <div v-if="authMode === 'register'" class="space-y-1.5 animate-in fade-in slide-in-from-top-2 duration-200">
          <label class="block text-sm md:text-base font-bold text-slate-700 dark:text-slate-300">
            {{ $t('auth.confirm_password_label') }}
          </label>
          <div class="relative">
            <Lock class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
            <input
              v-model="confirmPassword"
              required
              :type="showConfirmPassword ? 'text' : 'password'"
              minlength="8"
              :placeholder="$t('auth.confirm_password_placeholder')"
              class="w-full pl-11 pr-11 py-3 bg-slate-50 dark:bg-canvas-subtle border rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none transition-all"
              :class="[
                confirmPassword && confirmPassword !== password
                  ? 'border-rose-500 focus:border-rose-500 focus:ring-2 focus:ring-rose-500/20'
                  : 'border-slate-200 dark:border-white/[0.08] focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20'
              ]"
            />
            <button
              type="button"
              @click="showConfirmPassword = !showConfirmPassword"
              class="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 focus:outline-none transition-colors"
              :aria-label="showConfirmPassword ? 'Hide password' : 'Show password'"
            >
              <EyeOff v-if="showConfirmPassword" class="w-4 h-4" :stroke-width="1.5" />
              <Eye v-else class="w-4 h-4" :stroke-width="1.5" />
            </button>
          </div>
          <p
            v-if="confirmPassword && confirmPassword !== password"
            class="text-xs text-rose-500 font-medium flex items-center gap-1.5 pt-0.5"
          >
            <AlertCircle class="w-3.5 h-3.5 shrink-0" />
            <span>{{ $t('auth.passwords_mismatch') }}</span>
          </p>
        </div>
        <button
          type="submit"
          :disabled="isLoading"
          class="w-full flex items-center justify-center gap-2 py-3.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-sm md:text-base shadow-lg shadow-brand-500/20 transition-all active:scale-[0.98] disabled:opacity-50 cursor-pointer"
        >
          <span v-if="isLoading" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
          <span>{{ authMode === 'login' ? $t('auth.submit_sign_in') : $t('auth.submit_register') }}</span>
          <ArrowRight v-if="!isLoading" class="w-4 h-4 shrink-0" :stroke-width="1.5" />
        </button>
      </form>

      <!-- Google OAuth Sign-In Divider -->
      <div class="space-y-4">
        <div class="relative flex items-center justify-center">
          <div class="w-full border-t border-slate-200 dark:border-white/[0.08]"></div>
          <span class="px-3 bg-white dark:bg-canvas-elevated text-xs sm:text-sm text-slate-500 font-semibold uppercase">{{ $t('auth.or_continue_with') }}</span>
        </div>
        <!-- Google OAuth Button Container -->
        <div class="flex flex-col items-center justify-center min-h-[44px]">
          <div ref="googleBtnContainer" class="flex justify-center w-full max-w-[320px]" style="color-scheme: light;"></div>
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
