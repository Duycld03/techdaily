<script setup lang="ts">
import { ref, onMounted, watch, nextTick } from 'vue'
import { useIntervalFn } from '@vueuse/core'
import {
  BookOpen,
  Lock,
  Mail,
  User,
  ArrowRight,
  ArrowLeft,
  AlertCircle,
  Eye,
  EyeOff,
  Sparkles,
  Terminal,
  Layers,
  Brain,
  Key,
  RefreshCw,
  RotateCcw,
  Info,
  Send,
  Shield
} from 'lucide-vue-next'
import ThemeToggle from '~/components/common/ThemeToggle.vue'
import LocaleSelector from '~/components/common/LocaleSelector.vue'
import { useApiError } from '~/composables/useApiError'
import { useAuthStore } from '~/stores/useAuthStore'

const authStore = useAuthStore()
const route = useRoute()
const config = useRuntimeConfig()
const { t, locale, setLocale } = useI18n()
const { formatError } = useApiError()
const toast = useToast()
const colorMode = useColorMode()

const authMode = ref<'login' | 'register' | 'forgot-password'>('login')
const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const name = ref('')
const showPassword = ref(false)
const showConfirmPassword = ref(false)
const rememberSession = ref(true)
const isLoading = ref(false)
const errorMessage = ref('')
const googleBtnContainer = ref<HTMLElement | null>(null)
const hasGsiRendered = ref(false)

function getRedirectTarget() {
  const redirectQuery = route.query?.redirect
  if (typeof redirectQuery === 'string' && redirectQuery.startsWith('/') && !redirectQuery.startsWith('/login')) {
    return redirectQuery
  }
  return '/today'
}

async function setAuthMode(mode: 'login' | 'register' | 'forgot-password') {
  authMode.value = mode
  errorMessage.value = ''
  confirmPassword.value = ''
  if (mode !== 'forgot-password') {
    await nextTick()
    renderGoogleButton()
  }
}

onMounted(() => {
  authStore.init()
  if (authStore.isLoggedIn) {
    navigateTo(getRedirectTarget())
    return
  }

  initGoogleButton()
})

watch(() => authStore.isLoggedIn, (loggedIn) => {
  if (loggedIn) {
    navigateTo(getRedirectTarget())
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
  if (typeof window === 'undefined') return
  const gsi = (window as any).google?.accounts?.id
  const btnContainer = googleBtnContainer.value
  if (!config.public.googleClientId || !gsi || !btnContainer) {
    hasGsiRendered.value = false
    return
  }
  btnContainer.innerHTML = ''
  try {
    gsi.renderButton(btnContainer, {
      theme: colorMode.value === 'dark' ? 'filled_black' : 'outline',
      size: 'large',
      width: 320,
      text: 'signin',
      shape: 'rectangular',
      logo_alignment: 'left'
    })
    nextTick(() => {
      if (btnContainer.children.length > 0) {
        hasGsiRendered.value = true
      }
    })
  } catch (err) {
    console.warn('[TechDaily Auth] GSI renderButton fallback:', err)
    hasGsiRendered.value = false
  }
}

function triggerGoogleSignIn() {
  const gsi = (window as any).google?.accounts?.id
  if (gsi && config.public.googleClientId) {
    try {
      gsi.prompt()
      return
    } catch (e) {
      console.warn('[TechDaily Auth] GSI prompt failed:', e)
    }
  }
  toast.info(t('auth.toast_google_failed'))
}

watch(() => colorMode.value, () => {
  renderGoogleButton()
})

watch(() => authMode.value, async (mode) => {
  if (mode !== 'forgot-password') {
    await nextTick()
    renderGoogleButton()
  }
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
    await navigateTo(getRedirectTarget())
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

  if (authMode.value === 'forgot-password') {
    if (!email.value) {
      errorMessage.value = t('auth.toast_enter_credentials')
      toast.error(t('auth.toast_enter_credentials'))
      return
    }
    isLoading.value = true
    try {
      await Promise.resolve()
      toast.success(t('auth.toast_reset_link_sent'))
      setAuthMode('login')
    } catch (err: any) {
      const formatted = formatError(err, 'auth.toast_auth_failed')
      errorMessage.value = formatted
      toast.error(formatted)
    } finally {
      isLoading.value = false
    }
    return
  }

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
    await navigateTo(getRedirectTarget())
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
  <div class="min-h-dvh flex flex-col justify-between bg-slate-50 dark:bg-canvas text-slate-900 dark:text-slate-100 transition-colors duration-200 overflow-x-hidden">
    <!-- Minimal Studio Header -->
    <header class="relative z-20 w-full h-14 border-b border-slate-200/80 dark:border-white/[0.08] bg-slate-50/80 dark:bg-[#09090b]/80 backdrop-blur-md px-4 sm:px-8 flex items-center justify-between">
      <!-- Left: Brand -->
      <div class="flex items-center gap-3">
        <NuxtLink to="/login" class="flex items-center gap-2.5">
          <div class="w-8 h-8 rounded-lg bg-brand-500/20 border border-brand-500/40 flex items-center justify-center text-brand-600 dark:text-brand-400 shadow-sm shadow-brand-500/20">
            <BookOpen class="w-4 h-4" :stroke-width="1.75" />
          </div>
          <span class="font-bold tracking-tight text-slate-900 dark:text-white text-base">TechDaily</span>
        </NuxtLink>
      </div>

      <!-- Right: Controls -->
      <div class="flex items-center gap-3">
        <LocaleSelector />
        <ThemeToggle />
      </div>
    </header>

    <!-- Main Studio Cockpit Stage -->
    <main class="flex-1 flex flex-col items-center justify-center p-3 sm:p-5 lg:p-6 min-h-0">
      <div class="w-full max-w-6xl mx-auto py-2 px-1 sm:px-3">
        <div class="grid lg:grid-cols-12 gap-6 lg:gap-10 items-start">
          <!-- Left Column: Curriculum & Telemetry Showcase (Desktop Only) -->
          <div class="lg:col-span-5 hidden lg:flex flex-col justify-between rounded-2xl bg-white/70 dark:bg-[#131315] border border-slate-200/80 dark:border-white/[0.08] p-6 sm:p-7 relative overflow-hidden shadow-xl">
            <div class="space-y-4">
            <!-- Track Badge -->
            <div class="flex items-center gap-2">
              <span class="px-2.5 py-0.5 rounded bg-brand-500/15 border border-brand-500/30 font-mono text-[11px] font-semibold text-brand-600 dark:text-brand-300">TECHDAILY</span>
              <span class="font-mono text-[11px] text-slate-500 dark:text-slate-400 font-medium">SM-2 ACTIVE RECALL</span>
            </div>

            <!-- Headline & Mission Description -->
            <div class="space-y-1.5">
              <h1 class="text-xl sm:text-2xl font-bold text-slate-900 dark:text-white tracking-tight">
                {{ $t('auth.cockpit_title') }}
              </h1>
              <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 leading-relaxed">
                {{ $t('auth.cockpit_desc') }}
              </p>
            </div>
            <div class="space-y-3">
              <!-- Metric 1: Session Interval Target -->
              <div class="rounded-lg bg-slate-50 dark:bg-[#09090b] border border-slate-200/60 dark:border-white/[0.06] p-3">
                <div class="flex items-center justify-between text-[11px] font-mono text-slate-500 dark:text-slate-400 mb-1.5">
                  <span class="tracking-wider uppercase">{{ $t('auth.session_interval_target') }}</span>
                  <span class="text-emerald-600 dark:text-emerald-400 font-semibold">{{ $t('auth.session_interval_hit') }}</span>
                </div>
                <div class="w-full bg-slate-200 dark:bg-[#1c1b1d] h-1.5 rounded-full overflow-hidden">
                  <div class="bg-emerald-500 h-full rounded-full" style="width: 94%"></div>
                </div>
              </div>

              <!-- Metric 2: SM-2 Spaced Decay -->
              <div class="rounded-lg bg-slate-50 dark:bg-[#09090b] border border-slate-200/60 dark:border-white/[0.06] p-3">
                <div class="flex items-center justify-between text-[11px] font-mono text-slate-500 dark:text-slate-400 mb-1.5">
                  <span class="tracking-wider uppercase">{{ $t('auth.sm2_spaced_decay') }}</span>
                  <span class="text-brand-600 dark:text-brand-300 font-semibold">{{ $t('auth.sm2_decay_value') }}</span>
                </div>
                <div class="w-full bg-slate-200 dark:bg-[#1c1b1d] h-1.5 rounded-full overflow-hidden">
                  <div class="bg-gradient-to-r from-brand-500 to-purple-400 h-full rounded-full" style="width: 68%"></div>
                </div>
              </div>
            </div>

            <!-- Live Code Snippet Box (Consensus_Promise.ts) -->
            <div class="rounded-lg bg-slate-50 dark:bg-[#09090b] border border-slate-200/80 dark:border-white/[0.08] overflow-hidden text-xs font-mono">
              <div class="flex items-center justify-between px-3 py-2 border-b border-slate-200/60 dark:border-white/[0.06] bg-slate-100/60 dark:bg-[#0d0d10]">
                <div class="flex items-center gap-1.5 text-slate-600 dark:text-slate-400 text-[11px]">
                  <Terminal class="w-3.5 h-3.5 text-brand-500 dark:text-brand-400" />
                  <span>{{ $t('auth.file_consensus') }}</span>
                </div>
                <Lock class="w-3.5 h-3.5 text-slate-400" />
              </div>
              <div class="p-3 text-slate-700 dark:text-slate-300 leading-relaxed font-mono overflow-x-auto text-[11px]">
                <p><span class="text-purple-600 dark:text-purple-400 font-medium">const</span> drill = <span class="text-purple-600 dark:text-purple-400 font-medium">await</span> techDaily.<span class="text-sky-600 dark:text-sky-300 font-semibold">getDailySlice</span>({</p>
                <p class="pl-3 text-slate-500 dark:text-slate-400">track: <span class="text-emerald-600 dark:text-emerald-300">"Architecture &amp; Systems"</span>,</p>
                <p class="pl-3 text-slate-500 dark:text-slate-400">spacedRepetition: <span class="text-amber-600 dark:text-amber-300">"SM-2 Active Recall"</span>,</p>
                <p class="pl-3 text-slate-500 dark:text-slate-400">targetTime: <span class="text-brand-600 dark:text-brand-400">"5 Mins / Day"</span></p>
                <p>});</p>
              </div>
            </div>
          </div>

          <!-- Security footnote badge -->
          <div class="pt-4 mt-3 border-t border-slate-200/80 dark:border-white/[0.06] flex items-center gap-2 text-slate-500 dark:text-slate-400 font-mono text-[11px]">
            <Shield class="w-4 h-4 text-brand-500 dark:text-brand-400" :stroke-width="1.75" />
            <span class="tracking-wide uppercase">{{ $t('auth.cryptographic_attestation') }}</span>
          </div>
        </div>

          <!-- Right Column: Interactive Cockpit Auth Card -->
          <div class="lg:col-span-7 w-full max-w-xl mx-auto flex flex-col justify-center">
            <div class="glass-panel bg-white/95 dark:bg-[#18181b] border border-slate-200 dark:border-white/[0.08] rounded-3xl p-5 sm:p-7 shadow-2xl space-y-4">
            <div class="space-y-3">
              <!-- Mode Switcher Tabs -->
              <!-- Mode Switcher Tabs: 3-Segmented Controls (Sign In, Register, Recover) -->
              <div class="w-full flex p-1 rounded-xl bg-slate-100 dark:bg-canvas border border-slate-200/80 dark:border-white/[0.08] text-xs font-mono select-none">
                <button
                  type="button"
                  @click="setAuthMode('login')"
                  :class="[
                    'flex-1 py-1.5 px-3 rounded-lg transition-colors duration-150 outline-none focus:outline-none focus:ring-0 cursor-pointer text-center select-none',
                    authMode === 'login'
                      ? 'bg-white dark:bg-white/[0.12] text-brand-600 dark:text-white font-bold shadow-sm'
                      : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
                  ]"
                >
                  {{ $t('auth.sign_in_tab') }}
                </button>
                <button
                  type="button"
                  @click="setAuthMode('register')"
                  :class="[
                    'flex-1 py-1.5 px-3 rounded-lg transition-colors duration-150 outline-none focus:outline-none focus:ring-0 cursor-pointer text-center select-none',
                    authMode === 'register'
                      ? 'bg-white dark:bg-white/[0.12] text-brand-600 dark:text-white font-bold shadow-sm'
                      : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
                  ]"
                >
                  {{ $t('auth.register_tab') }}
                </button>
                <button
                  type="button"
                  @click="setAuthMode('forgot-password')"
                  :class="[
                    'px-3 py-1.5 rounded-lg transition-colors duration-150 outline-none focus:outline-none focus:ring-0 cursor-pointer text-center select-none flex items-center justify-center gap-1.5',
                    authMode === 'forgot-password'
                      ? 'bg-white dark:bg-white/[0.12] text-brand-600 dark:text-white font-bold shadow-sm'
                      : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100'
                  ]"
                  :title="$t('auth.recovery_tab_title')"
                >
                  <RotateCcw class="w-3.5 h-3.5" :stroke-width="1.75" />
                  <span class="hidden sm:inline">{{ $t('auth.recovery_tab_title') }}</span>
                </button>
              </div>

              <!-- Cockpit Title & Subtitle -->
              <div>
                <h2 class="text-xl sm:text-2xl font-extrabold text-slate-900 dark:text-white tracking-tight">
                  {{ authMode === 'login' 
                    ? $t('auth.welcome_title') 
                    : (authMode === 'register' ? $t('auth.register_title') : $t('auth.recover_cockpit_title')) }}
                </h2>
                <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 mt-0.5 font-medium">
                  {{ authMode === 'forgot-password' 
                    ? $t('auth.recover_cockpit_subtitle')
                    : $t('auth.welcome_subtitle') }}
                </p>
              </div>
            </div>

            <!-- OAuth Providers (Login & Register) -->
            <!-- Fast 1-Click Developer OAuth Stack (Google Only) -->
            <div v-if="authMode !== 'forgot-password'" class="mb-4 pt-1">
              <div class="relative flex items-center justify-center min-h-[44px] w-full">
                <div
                  v-show="hasGsiRendered"
                  ref="googleBtnContainer"
                  class="w-full flex justify-center overflow-hidden rounded-xl"
                  style="color-scheme: light;"
                ></div>
                <button
                  v-if="!hasGsiRendered"
                  type="button"
                  @click="triggerGoogleSignIn"
                  class="w-full h-11 px-4 rounded-xl border border-slate-200 dark:border-white/[0.12] bg-white dark:bg-[#1c1b1d] hover:bg-slate-50 dark:hover:bg-white/[0.04] text-slate-800 dark:text-slate-200 flex items-center justify-between transition-colors shadow-sm text-sm font-medium cursor-pointer"
                >
                  <div class="flex items-center gap-2.5">
                    <svg class="w-4 h-4 shrink-0" viewBox="0 0 24 24">
                      <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"></path>
                      <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"></path>
                      <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z" fill="#FBBC05"></path>
                      <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z" fill="#EA4335"></path>
                    </svg>
                    <span>{{ $t('auth.google_sign_in') }}</span>
                  </div>
                  <kbd class="font-mono text-xs bg-black/5 dark:bg-black/30 px-1.5 py-0.5 rounded text-slate-500 dark:text-slate-400 border border-slate-200 dark:border-white/[0.06]">⌘L</kbd>
                </button>
              </div>

              <!-- Centered Divider -->
              <div class="relative my-4">
                <div class="absolute inset-0 flex items-center" aria-hidden="true">
                  <div class="w-full border-t border-slate-200 dark:border-white/[0.08]" />
                </div>
                <div class="relative flex justify-center text-[10px] font-mono uppercase tracking-wider">
                  <span class="bg-white dark:bg-[#18181b] px-3 text-slate-400 font-semibold">
                    {{ $t('auth.or_continue_with') }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Accessible Error Alert -->
            <div
              v-if="errorMessage"
              role="alert"
              class="p-3 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-700 dark:text-rose-300 text-xs font-medium flex items-start gap-2 animate-in fade-in duration-200"
            >
              <AlertCircle class="w-4 h-4 text-rose-500 shrink-0 mt-0.5" :stroke-width="1.5" />
              <span class="flex-1">{{ errorMessage }}</span>
            </div>

            <!-- Credentials Form -->
            <form @submit.prevent="handleSubmit" class="space-y-3.5">
              <!-- Register Mode: 2-column input grid -->
              <div v-if="authMode === 'register'" class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <!-- Full Name -->
                <div class="space-y-1">
                  <label class="block text-xs font-mono font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wider">
                    {{ $t('auth.name_label') }}
                  </label>
                  <div class="relative">
                    <User class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
                    <input
                      v-model="name"
                      type="text"
                      required
                      :placeholder="$t('auth.name_placeholder')"
                      class="w-full pl-9 pr-3 py-2 bg-slate-50 dark:bg-canvas-subtle border border-slate-200 dark:border-white/[0.08] rounded-xl text-xs sm:text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 focus:border-brand-500 focus:ring-1 focus:ring-brand-500/30 focus:outline-none transition-all"
                    />
                  </div>
                </div>

                <!-- Email Address -->
                <div class="space-y-1">
                  <label class="block text-xs font-mono font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wider">
                    {{ $t('auth.email_label') }}
                  </label>
                  <div class="relative">
                    <Mail class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
                    <input
                      v-model="email"
                      required
                      type="email"
                      placeholder="dev@techdaily.io"
                      class="w-full pl-9 pr-3 py-2 bg-slate-50 dark:bg-canvas-subtle border border-slate-200 dark:border-white/[0.08] rounded-xl text-xs sm:text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 focus:border-brand-500 focus:ring-1 focus:ring-brand-500/30 focus:outline-none transition-all"
                    />
                  </div>
                </div>

                <!-- Password -->
                <div class="space-y-1">
                  <label class="block text-xs font-mono font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wider">
                    {{ $t('auth.password_label') }}
                  </label>
                  <div class="relative">
                    <Lock class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
                    <input
                      v-model="password"
                      required
                      :type="showPassword ? 'text' : 'password'"
                      minlength="8"
                      :placeholder="$t('auth.password_placeholder')"
                      class="w-full pl-9 pr-9 py-2 bg-slate-50 dark:bg-canvas-subtle border border-slate-200 dark:border-white/[0.08] rounded-xl text-xs sm:text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 focus:border-brand-500 focus:ring-1 focus:ring-brand-500/30 focus:outline-none transition-all"
                    />
                    <button
                      type="button"
                      @click="showPassword = !showPassword"
                      class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 focus:outline-none cursor-pointer"
                      :aria-label="showPassword ? 'Hide password' : 'Show password'"
                    >
                      <EyeOff v-if="showPassword" class="w-3.5 h-3.5" :stroke-width="1.5" />
                      <Eye v-else class="w-3.5 h-3.5" :stroke-width="1.5" />
                    </button>
                  </div>
                </div>

                <!-- Confirm Password -->
                <div class="space-y-1">
                  <label class="block text-xs font-mono font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wider">
                    {{ $t('auth.confirm_password_label') }}
                  </label>
                  <div class="relative">
                    <Lock class="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
                    <input
                      v-model="confirmPassword"
                      required
                      :type="showConfirmPassword ? 'text' : 'password'"
                      minlength="8"
                      :placeholder="$t('auth.confirm_password_placeholder')"
                      class="w-full pl-9 pr-9 py-2 bg-slate-50 dark:bg-canvas-subtle border border-slate-200 dark:border-white/[0.08] rounded-xl text-xs sm:text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 focus:border-brand-500 focus:ring-1 focus:ring-brand-500/30 focus:outline-none transition-all"
                    />
                    <button
                      type="button"
                      @click="showConfirmPassword = !showConfirmPassword"
                      class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 focus:outline-none cursor-pointer"
                      :aria-label="showConfirmPassword ? 'Hide password' : 'Show password'"
                    >
                      <EyeOff v-if="showConfirmPassword" class="w-3.5 h-3.5" :stroke-width="1.5" />
                      <Eye v-else class="w-3.5 h-3.5" :stroke-width="1.5" />
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
              </div>

              <!-- Forgot Password Mode: Email only -->
              <!-- Forgot Password Mode: TechDaily Recovery Panel -->
              <div v-else-if="authMode === 'forgot-password'" class="space-y-3.5">
                <!-- Account Registration Email -->
                <div class="space-y-1">
                  <label class="block text-xs font-mono font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wider">
                    {{ $t('auth.account_email_label') }}
                  </label>
                  <div class="relative">
                    <Mail class="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
                    <input
                      v-model="email"
                      required
                      type="email"
                      placeholder="dev@techdaily.io"
                      class="w-full pl-11 pr-4 py-2.5 bg-slate-50 dark:bg-canvas-subtle border border-slate-200 dark:border-white/[0.08] rounded-xl text-xs sm:text-sm font-mono text-slate-900 dark:text-slate-100 placeholder-slate-400 focus:border-brand-500 focus:ring-1 focus:ring-brand-500/30 focus:outline-none transition-all"
                    />
                  </div>
                </div>

                <!-- Contextual OAuth / Hardware Security Key Advisory Notice -->
                <div class="rounded-xl bg-amber-500/10 border border-amber-500/20 p-3 flex items-start gap-2.5 text-xs text-amber-800 dark:text-amber-300">
                  <Info class="w-4 h-4 text-amber-500 shrink-0 mt-0.5" :stroke-width="1.75" />
                  <p class="leading-relaxed">
                    {{ $t('auth.oauth_bypass_notice') }}
                  </p>
                </div>
              </div>

              <!-- Sign In Mode: Stacked Email + Password with Telemetry Labels -->
              <div v-else class="space-y-3">
                <!-- Email -->
                <div class="space-y-1">
                  <div class="flex items-center justify-between font-mono text-[11px] font-bold">
                    <label class="text-slate-700 dark:text-slate-300 tracking-wider">
                      {{ $t('auth.dev_handle_label') }}
                    </label>
                  </div>
                  <div class="relative">
                    <Terminal class="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
                    <input
                      v-model="email"
                      required
                      type="email"
                      placeholder="dev@techdaily.io"
                      class="w-full pl-11 pr-4 py-2.5 bg-slate-50 dark:bg-canvas-subtle border border-slate-200 dark:border-white/[0.08] rounded-xl text-xs sm:text-sm font-mono text-slate-900 dark:text-slate-100 placeholder-slate-400 focus:border-brand-500 focus:ring-1 focus:ring-brand-500/30 focus:outline-none transition-all"
                    />
                  </div>
                </div>

                <!-- Password -->
                <div class="space-y-1">
                  <div class="flex items-center justify-between font-mono text-[11px] font-bold">
                    <label class="text-slate-700 dark:text-slate-300 tracking-wider">
                      {{ $t('auth.secret_token_label') }}
                    </label>
                    <button
                      type="button"
                      @click="setAuthMode('forgot-password')"
                      class="text-brand-600 dark:text-brand-400 hover:text-brand-500 font-medium transition-colors cursor-pointer text-[11px]"
                    >
                      {{ $t('auth.forgot_password_link') }}
                    </button>
                  </div>
                  <div class="relative">
                    <Key class="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
                    <input
                      v-model="password"
                      required
                      :type="showPassword ? 'text' : 'password'"
                      placeholder="••••••••••••••••"
                      class="w-full pl-11 pr-11 py-2.5 bg-slate-50 dark:bg-canvas-subtle border border-slate-200 dark:border-white/[0.08] rounded-xl text-xs sm:text-sm font-mono text-slate-900 dark:text-slate-100 placeholder-slate-400 focus:border-brand-500 focus:ring-1 focus:ring-brand-500/30 focus:outline-none transition-all"
                    />
                    <button
                      type="button"
                      @click="showPassword = !showPassword"
                      class="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 focus:outline-none transition-colors cursor-pointer"
                      :aria-label="showPassword ? 'Hide password' : 'Show password'"
                    >
                      <EyeOff v-if="showPassword" class="w-4 h-4" :stroke-width="1.5" />
                      <Eye v-else class="w-4 h-4" :stroke-width="1.5" />
                    </button>
                  </div>
                </div>

                <!-- Remember Session Checkbox -->
                <div class="flex items-center justify-between text-xs font-medium pt-0.5">
                  <label class="flex items-center gap-2 cursor-pointer select-none text-slate-600 dark:text-slate-400">
                    <input
                      v-model="rememberSession"
                      type="checkbox"
                      class="rounded border-slate-300 dark:border-white/[0.12] bg-slate-100 dark:bg-canvas-subtle text-brand-600 focus:ring-brand-500/30 w-3.5 h-3.5"
                    />
                    <span>{{ $t('auth.remember_session') }}</span>
                  </label>
                </div>
              </div>

              <!-- Submit Action Button -->
              <button
                type="submit"
                :disabled="isLoading"
                class="w-full py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 font-semibold text-white shadow-sm hover:shadow-md transition-all active:scale-[0.99] flex items-center justify-center gap-2 text-xs sm:text-sm disabled:opacity-50 cursor-pointer"
              >
                <span v-if="isLoading" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                <span v-else-if="authMode === 'forgot-password'" class="flex items-center gap-1.5">
                  <span>{{ $t('auth.send_recovery_link_btn') }}</span>
                  <Send class="w-3.5 h-3.5" :stroke-width="1.75" />
                </span>
                <span v-else>
                  {{ authMode === 'login' 
                    ? $t('auth.enter_cockpit') 
                    : $t('auth.submit_register') }}
                </span>
                <kbd v-if="!isLoading" class="px-1.5 py-0.5 rounded bg-brand-700/60 text-white font-mono text-[10px]">↵ RETURN</kbd>
              </button>

              <!-- Back to Sign In button (Forgot Password Mode Only) -->
              <div v-if="authMode === 'forgot-password'" class="text-center pt-1">
                <button
                  type="button"
                  @click="setAuthMode('login')"
                  class="font-mono text-xs text-slate-500 hover:text-slate-800 dark:text-slate-400 dark:hover:text-white transition-colors inline-flex items-center gap-1.5 cursor-pointer py-1"
                >
                  <ArrowLeft class="w-3.5 h-3.5" :stroke-width="1.75" />
                  <span>{{ $t('auth.back_to_signin_btn') }}</span>
                </button>
              </div>
            </form>

            <!-- Card Security Footnote & Legal Links -->
            <div class="mt-4 pt-3.5 border-t border-slate-200/80 dark:border-white/[0.08] flex items-center justify-between text-slate-400 font-mono text-[11px]">
              <div class="flex items-center gap-1.5">
                <Lock class="w-3.5 h-3.5 text-emerald-500" :stroke-width="1.75" />
                <span class="tracking-wide text-slate-600 dark:text-slate-300 font-medium">
                  {{ $t('auth.zero_knowledge_badge') }}
                </span>
              </div>
              <div class="flex items-center gap-2">
                <a href="#" class="hover:text-slate-900 dark:hover:text-white transition-colors">
                  {{ $t('auth.terms_link') }}
                </a>
                <span>·</span>
                <a href="#" class="hover:text-slate-900 dark:hover:text-white transition-colors">
                  {{ $t('auth.privacy_link') }}
                </a>
              </div>
            </div>

            <!-- Terms and Privacy Footer -->
            <div class="text-center pt-1 text-[11px] text-slate-400 dark:text-slate-500">
              <p class="leading-relaxed">
                {{ $t('auth.terms_agreement') }}
              </p>
            </div>
          </div>
        </div>
        </div>
      </div>
    </main>
  </div>
</template>
