<script setup lang="ts">
import { ref, onMounted, watch, nextTick } from 'vue'
import { useIntervalFn, useEventListener, useDebounceFn } from '@vueuse/core'
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
  Shield,
  Zap
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
  if (mode === 'login') {
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

useEventListener('resize', useDebounceFn(() => {
  if (hasGsiRendered.value) {
    renderGoogleButton()
  }
}, 150))

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
    const containerWidth = Math.min(Math.max(btnContainer.clientWidth || 400, 200), 400)
    gsi.renderButton(btnContainer, {
      theme: colorMode.value === 'dark' ? 'filled_black' : 'outline',
      size: 'large',
      width: containerWidth,
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
  const btnContainer = googleBtnContainer.value
  const googleBtn = btnContainer?.querySelector('div[role="button"]') as HTMLElement | null
  if (googleBtn) {
    googleBtn.click()
    return
  }

  const gsi = (window as any).google?.accounts?.id
  if (gsi && config.public.googleClientId) {
    try {
      gsi.prompt((notification: any) => {
        if (notification.isNotDisplayed?.() || notification.isSkippedMoment?.()) {
          console.warn('[TechDaily Auth] GSI prompt not displayed:', notification.getNotDisplayedReason?.() || notification.getSkippedReason?.())
        }
      })
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
  if (mode === 'login') {
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
  <div class="min-h-dvh flex flex-col justify-between bg-slate-50 dark:bg-[#070709] text-slate-900 dark:text-zinc-200 font-sans antialiased transition-colors duration-200 relative overflow-x-hidden">
    <!-- BEGIN: Ambient Background Layers (Eliminates Empty Space) -->
    <div class="fixed inset-0 pointer-events-none z-0">
      <!-- Subtle engineering dot-matrix grid -->
      <div class="absolute inset-0 bg-grid-dots opacity-70"></div>
      <!-- Deep Iris Violet Radial Ambient Glow behind hero split card -->
      <div class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[900px] h-[650px] bg-brand-600/15 dark:bg-[#7c3aed]/15 rounded-full blur-[140px] pointer-events-none"></div>
      <div class="absolute top-0 right-1/4 w-[400px] h-[300px] bg-emerald-500/5 rounded-full blur-[120px] pointer-events-none"></div>
      <div class="absolute bottom-10 left-10 w-[450px] h-[350px] bg-blue-600/10 rounded-full blur-[130px] pointer-events-none"></div>
      <!-- Tech Baseline hairline decorations -->
      <div class="absolute left-0 right-0 top-16 h-px bg-gradient-to-r from-transparent via-slate-200 dark:via-white/[0.08] to-transparent"></div>
      <div class="absolute left-12 top-0 bottom-0 w-px bg-gradient-to-b from-transparent via-slate-200/50 dark:via-white/[0.03] to-transparent hidden 2xl:block"></div>
      <div class="absolute right-12 top-0 bottom-0 w-px bg-gradient-to-b from-transparent via-slate-200/50 dark:via-white/[0.03] to-transparent hidden 2xl:block"></div>
    </div>
    <!-- END: Ambient Background Layers -->

    <!-- BEGIN: TopHeader -->
    <header class="relative z-10 w-full border-b border-slate-200/80 dark:border-white/[0.06] bg-white/70 dark:bg-[#0c0c0e]/60 backdrop-blur-xl px-4 sm:px-8 py-3.5 transition-all">
      <div class="max-w-7xl mx-auto flex items-center justify-between">
        <!-- Brand -->
        <NuxtLink to="/login" class="flex items-center gap-2.5 group">
          <!-- Logo Icon -->
          <div class="w-9 h-9 rounded-lg bg-gradient-to-br from-brand-600 to-indigo-700 p-0.5 shadow-lg shadow-brand-600/20 group-hover:scale-105 transition-transform duration-200">
            <div class="w-full h-full bg-white dark:bg-[#0c0c0e] rounded-[7px] flex items-center justify-center">
              <BookOpen class="w-4 h-4 text-brand-500 dark:text-brand-400 group-hover:text-brand-600 dark:group-hover:text-white transition-colors" :stroke-width="2" />
            </div>
          </div>
          <!-- Brand Title -->
          <span class="text-base font-bold tracking-tight text-slate-900 dark:text-white group-hover:text-brand-500 transition-colors">TechDaily</span>
        </NuxtLink>

        <!-- Language Switcher & Theme Control -->
        <div class="flex items-center gap-2">
          <LocaleSelector />
          <ThemeToggle />
        </div>
      </div>
    </header>
    <!-- END: TopHeader -->

    <!-- BEGIN: MainContentArea -->
    <main class="relative z-10 flex-1 flex items-center justify-center px-4 sm:px-6 lg:px-8 py-8 sm:py-12">
      <!-- Balanced Hero Split Grid -->
      <div class="w-full max-w-6xl mx-auto grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-10 items-center">
        <!-- =============================================== -->
        <!-- LEFT COLUMN: Value Proposition & Live Telemetry -->
        <!-- =============================================== -->
        <section class="lg:col-span-6 flex flex-col justify-center space-y-6">
          <!-- Platform Badge -->
          <div class="flex items-center gap-2.5">
            <div class="inline-flex items-center gap-2 px-3 py-1 rounded-md bg-brand-500/10 border border-brand-500/25 text-brand-600 dark:text-brand-300 font-mono text-xs tracking-wide uppercase">
              <span class="w-1.5 h-1.5 rounded-full bg-brand-500 dark:bg-brand-400"></span>
              <span>TECHDAILY</span>
              <span class="text-slate-300 dark:text-zinc-600">|</span>
              <span class="text-brand-600 dark:text-brand-400">SM-2 ACTIVE RECALL</span>
            </div>
          </div>

          <!-- Main Headline & Subtitle -->
          <div class="space-y-3">
            <h1 class="text-3xl sm:text-4xl lg:text-[40px] font-bold tracking-tight text-slate-900 dark:text-white leading-tight">
              {{ $t('auth.cockpit_title') }}
            </h1>
            <p class="text-sm sm:text-base text-slate-600 dark:text-zinc-400 leading-relaxed max-w-xl">
              {{ $t('auth.cockpit_desc') }}
            </p>
          </div>

          <!-- Progress Slices Card -->
          <div class="p-4 sm:p-5 rounded-xl bg-white/80 dark:bg-[#0c0c0e]/90 border border-slate-200 dark:border-white/[0.08] shadow-xl backdrop-blur-md space-y-4">
            <!-- Daily Goal Metric -->
            <div>
              <div class="flex justify-between items-center text-xs font-mono mb-2">
                <span class="text-slate-600 dark:text-zinc-400 tracking-wider flex items-center gap-1.5">
                  <span class="w-1.5 h-1.5 rounded-full bg-emerald-500 dark:bg-emerald-400"></span>
                  {{ $t('auth.session_interval_target') }}
                </span>
                <span class="text-emerald-600 dark:text-emerald-400 font-semibold">{{ $t('auth.session_interval_hit') }}</span>
              </div>
              <!-- Progress Track -->
              <div class="w-full h-2 rounded-full bg-slate-200 dark:bg-zinc-800/80 overflow-hidden p-0.5 border border-slate-200/50 dark:border-white/5">
                <div class="h-full rounded-full bg-gradient-to-r from-emerald-500 to-teal-400 w-[94%] shadow-[0_0_12px_rgba(16,185,129,0.5)]"></div>
              </div>
            </div>

            <!-- Spaced Repetition Metric -->
            <div>
              <div class="flex justify-between items-center text-xs font-mono mb-2">
                <span class="text-slate-600 dark:text-zinc-400 tracking-wider flex items-center gap-1.5">
                  <span class="w-1.5 h-1.5 rounded-full bg-brand-500 dark:bg-brand-400"></span>
                  {{ $t('auth.sm2_spaced_decay') }}
                </span>
                <span class="text-brand-600 dark:text-brand-400 font-semibold tracking-wide">{{ $t('auth.sm2_decay_value') }}</span>
              </div>
              <!-- Progress Track -->
              <div class="w-full h-2 rounded-full bg-slate-200 dark:bg-zinc-800/80 overflow-hidden p-0.5 border border-slate-200/50 dark:border-white/5">
                <div class="h-full rounded-full bg-gradient-to-r from-brand-600 to-purple-400 w-[68%] shadow-[0_0_12px_rgba(124,58,237,0.5)]"></div>
              </div>
            </div>
          </div>

          <!-- Code Snippet Simulation -->
          <div class="rounded-xl border border-slate-200 dark:border-white/[0.08] bg-white/80 dark:bg-[#0c0c0e]/90 shadow-2xl overflow-hidden backdrop-blur-md">
            <!-- Editor Titlebar -->
            <div class="px-4 py-2.5 bg-slate-100 dark:bg-[#111115] border-b border-slate-200 dark:border-white/[0.06] flex items-center justify-between">
              <div class="flex items-center gap-2">
                <span class="w-2.5 h-2.5 rounded-full bg-rose-500/80"></span>
                <span class="w-2.5 h-2.5 rounded-full bg-amber-500/80"></span>
                <span class="w-2.5 h-2.5 rounded-full bg-emerald-500/80"></span>
                <span class="ml-2 font-mono text-xs text-slate-600 dark:text-zinc-400 flex items-center gap-1.5">
                  <span class="text-brand-500 font-bold">&gt;_</span> {{ $t('auth.file_consensus') }}
                </span>
              </div>
              <Lock class="w-3.5 h-3.5 text-slate-400 dark:text-zinc-500" :stroke-width="1.5" />
            </div>
            <!-- Code View -->
            <div class="p-4 font-mono text-[13px] leading-relaxed text-slate-800 dark:text-zinc-300 overflow-x-auto bg-slate-50 dark:bg-[#0d0d11]">
              <p><span class="text-purple-600 dark:text-purple-400 font-semibold">const</span> <span class="text-blue-600 dark:text-blue-300">drill</span> = <span class="text-purple-600 dark:text-purple-400 font-semibold">await</span> techDaily.<span class="text-amber-600 dark:text-amber-300">getDailySlice</span>({</p>
              <p class="pl-4"><span class="text-slate-500 dark:text-zinc-400">track</span>: <span class="text-emerald-600 dark:text-emerald-400">"Architecture &amp; Systems"</span>,</p>
              <p class="pl-4"><span class="text-slate-500 dark:text-zinc-400">spacedRepetition</span>: <span class="text-emerald-600 dark:text-emerald-400">"SM-2 Active Recall"</span>,</p>
              <p class="pl-4"><span class="text-slate-500 dark:text-zinc-400">targetTime</span>: <span class="text-emerald-600 dark:text-emerald-400">"5 Mins / Day"</span></p>
              <p>});</p>
            </div>
          </div>
        </section>

        <!-- =============================================== -->
        <!-- RIGHT COLUMN: Elevated Glass Cockpit Card       -->
        <!-- =============================================== -->
        <section class="lg:col-span-6 flex justify-center">
          <div class="glass-panel w-full max-w-md bg-white/95 dark:bg-[#131317]/90 backdrop-blur-2xl rounded-2xl border border-slate-200 dark:border-white/[0.09] p-6 sm:p-8 shadow-2xl tech-border-glow relative">
            <!-- Segmented Tab Switcher (Sign In, Register, Recover) -->
            <div class="w-full bg-slate-100 dark:bg-[#0a0a0d] p-1 rounded-xl border border-slate-200/80 dark:border-white/[0.07] grid grid-cols-3 gap-1 mb-6 text-xs font-medium">
              <button 
                type="button"
                @click="setAuthMode('login')"
                :class="[
                  'py-2 rounded-lg transition font-semibold cursor-pointer text-center',
                  authMode === 'login'
                    ? 'bg-white dark:bg-zinc-800 text-slate-900 dark:text-white shadow-sm'
                    : 'text-slate-600 dark:text-zinc-400 hover:text-slate-900 dark:hover:text-zinc-200'
                ]"
              >
                {{ $t('auth.sign_in_tab') }}
              </button>
              <button 
                type="button"
                @click="setAuthMode('register')"
                :class="[
                  'py-2 rounded-lg transition font-semibold cursor-pointer text-center',
                  authMode === 'register'
                    ? 'bg-white dark:bg-zinc-800 text-slate-900 dark:text-white shadow-sm'
                    : 'text-slate-600 dark:text-zinc-400 hover:text-slate-900 dark:hover:text-zinc-200'
                ]"
              >
                {{ $t('auth.register_tab') }}
              </button>
              <button 
                type="button"
                @click="setAuthMode('forgot-password')"
                :class="[
                  'py-2 rounded-lg transition font-semibold cursor-pointer flex items-center justify-center gap-1',
                  authMode === 'forgot-password'
                    ? 'bg-white dark:bg-zinc-800 text-slate-900 dark:text-white shadow-sm'
                    : 'text-slate-600 dark:text-zinc-400 hover:text-slate-900 dark:hover:text-zinc-200'
                ]"
              >
                <RotateCcw class="w-3 h-3 text-slate-500 dark:text-zinc-500" />
                <span>{{ $t('auth.recovery_tab_title') }}</span>
              </button>
            </div>

            <!-- Form Header -->
            <div class="mb-6 space-y-1.5 text-left">
              <h2 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">
                {{ authMode === 'login' ? $t('auth.welcome_title') : (authMode === 'register' ? $t('auth.register_title') : $t('auth.recover_cockpit_title')) }}
              </h2>
              <p class="text-xs text-slate-500 dark:text-zinc-400 leading-normal">
                {{ authMode === 'forgot-password' ? $t('auth.recover_cockpit_subtitle') : $t('auth.welcome_subtitle') }}
              </p>
            </div>

            <!-- OAuth Provider (Google SSO) -->
            <div v-if="authMode === 'login'" class="mb-5">
              <!-- Interactive Wrapper with Invisible Native GSI Overlay (Guarantees Native Flow & Zero Overflow) -->
              <div class="relative w-full overflow-hidden rounded-xl group">
                <!-- Visual Custom Google Button -->
                <button 
                  type="button"
                  @click="triggerGoogleSignIn"
                  class="w-full flex items-center justify-center gap-3 py-2.5 px-4 rounded-xl bg-slate-100 dark:bg-[#202024] group-hover:bg-slate-200 dark:group-hover:bg-zinc-800 text-sm font-medium text-slate-800 dark:text-zinc-200 border border-slate-200 dark:border-white/[0.08] transition shadow-sm group-hover:border-slate-300 dark:group-hover:border-white/15 cursor-pointer"
                >
                  <!-- Official Google Icon SVG with Transparent Background -->
                  <svg class="w-4 h-4 shrink-0" viewBox="0 0 24 24">
                    <path d="M23.745 12.27c0-.7-.06-1.4-.19-2.07H12v4.51h6.6c-.29 1.52-1.14 2.82-2.4 3.68v3.05h3.88c2.27-2.09 3.665-5.17 3.665-9.17z" fill="#4285F4"></path>
                    <path d="M12 24c3.24 0 5.95-1.08 7.93-2.91l-3.88-3.05c-1.08.72-2.45 1.16-4.05 1.16-3.12 0-5.77-2.1-6.72-4.93H1.25v3.15C3.26 21.36 7.33 24 12 24z" fill="#34A853"></path>
                    <path d="M5.28 14.27c-.25-.72-.38-1.49-.38-2.27s.13-1.55.38-2.27V6.58H1.25C.45 8.18 0 9.99 0 12s.45 3.82 1.25 5.42l4.03-3.15z" fill="#FBBC05"></path>
                    <path d="M12 4.75c1.77 0 3.35.61 4.6 1.8l3.42-3.42C17.95 1.19 15.24 0 12 0 7.33 0 3.26 2.64 1.25 6.58l4.03 3.15c.95-2.83 3.6-4.98 6.72-4.98z" fill="#EA4335"></path>
                  </svg>
                  <span>{{ $t('auth.google_sign_in_with') }}</span>
                </button>

                <!-- Transparent GSI Native Target Overlay -->
                <div
                  ref="googleBtnContainer"
                  class="absolute inset-0 opacity-0 cursor-pointer overflow-hidden flex items-center justify-center pointer-events-auto"
                  style="z-index: 10;"
                ></div>
              </div>

              <!-- Divider -->
              <div class="relative my-5 flex items-center justify-center">
                <div class="absolute inset-0 flex items-center" aria-hidden="true">
                  <div class="w-full border-t border-slate-200 dark:border-white/[0.08]"></div>
                </div>
                <div class="relative flex justify-center text-[10px] font-mono uppercase tracking-widest whitespace-nowrap">
                  <span class="bg-white dark:bg-[#131317] px-3 text-slate-400 dark:text-zinc-500">
                    {{ $t('auth.or_continue_with') }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Accessible Error Alert -->
            <div
              v-if="errorMessage"
              role="alert"
              class="p-3 mb-4 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-700 dark:text-rose-300 text-xs font-medium flex items-start gap-2 animate-in fade-in duration-200"
            >
              <AlertCircle class="w-4 h-4 text-rose-500 shrink-0 mt-0.5" :stroke-width="1.5" />
              <span class="flex-1">{{ errorMessage }}</span>
            </div>

            <!-- Auth Form Fields -->
            <form @submit.prevent="handleSubmit" class="space-y-4">
              <!-- Register Mode: Additional Name Field -->
              <div v-if="authMode === 'register'" class="space-y-1.5 text-left">
                <label class="block font-mono text-[11px] font-medium tracking-wider text-slate-600 dark:text-zinc-400" for="name">
                  {{ $t('auth.name_label') }}
                </label>
                <div class="relative rounded-xl border border-slate-200 dark:border-white/[0.09] bg-slate-50 dark:bg-[#070709] focus-within:border-brand-500 focus-within:ring-1 focus-within:ring-brand-500 transition">
                  <span class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400 dark:text-zinc-500">
                    <User class="w-4 h-4" :stroke-width="1.5" />
                  </span>
                  <input
                    id="name"
                    v-model="name"
                    required
                    type="text"
                    :placeholder="$t('auth.name_placeholder')"
                    class="w-full pl-10 pr-4 py-2.5 bg-transparent border-0 text-sm font-mono text-slate-900 dark:text-zinc-200 placeholder-slate-400 dark:placeholder-zinc-600 focus:ring-0 focus:outline-none"
                  />
                </div>
              </div>

              <!-- Email / Username Input -->
              <div class="space-y-1.5 text-left">
                <label class="block font-mono text-[11px] font-medium tracking-wider text-slate-600 dark:text-zinc-400" for="email">
                  {{ authMode === 'forgot-password' ? $t('auth.account_email_label') : $t('auth.dev_handle_label') }}
                </label>
                <div class="relative rounded-xl border border-slate-200 dark:border-white/[0.09] bg-slate-50 dark:bg-[#070709] focus-within:border-brand-500 focus-within:ring-1 focus-within:ring-brand-500 transition">
                  <span class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none font-mono text-xs text-slate-400 dark:text-zinc-500">
                    &gt;_
                  </span>
                  <input
                    id="email"
                    v-model="email"
                    required
                    type="email"
                    placeholder="dev@techdaily.io"
                    class="w-full pl-9 pr-4 py-2.5 bg-transparent border-0 text-sm font-mono text-slate-900 dark:text-zinc-200 placeholder-slate-400 dark:placeholder-zinc-600 focus:ring-0 focus:outline-none"
                  />
                </div>
              </div>

              <!-- Contextual OAuth / Hardware Security Key Advisory Notice (Forgot Password Mode) -->
              <div v-if="authMode === 'forgot-password'" class="rounded-xl bg-amber-500/10 border border-amber-500/20 p-3 flex items-start gap-2.5 text-xs text-amber-800 dark:text-amber-300">
                <Info class="w-4 h-4 text-amber-500 shrink-0 mt-0.5" :stroke-width="1.75" />
                <p class="leading-relaxed">
                  {{ $t('auth.oauth_bypass_notice') }}
                </p>
              </div>

              <!-- Password Input with Forgot link -->
              <div v-if="authMode !== 'forgot-password'" class="space-y-1.5 text-left">
                <div class="flex items-center justify-between">
                  <label class="block font-mono text-[11px] font-medium tracking-wider text-slate-600 dark:text-zinc-400" for="password">
                    {{ $t('auth.secret_token_label') }}
                  </label>
                  <button
                    v-if="authMode === 'login'"
                    type="button"
                    @click="setAuthMode('forgot-password')"
                    class="text-xs text-brand-600 dark:text-brand-400 hover:text-brand-500 transition cursor-pointer"
                  >
                    {{ $t('auth.forgot_password_link') }}
                  </button>
                </div>
                <div class="relative rounded-xl border border-slate-200 dark:border-white/[0.09] bg-slate-50 dark:bg-[#070709] focus-within:border-brand-500 focus-within:ring-1 focus-within:ring-brand-500 transition">
                  <span class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400 dark:text-zinc-500">
                    <Key class="w-4 h-4" :stroke-width="1.5" />
                  </span>
                  <input
                    id="password"
                    v-model="password"
                    required
                    :type="showPassword ? 'text' : 'password'"
                    minlength="8"
                    placeholder="••••••••••••"
                    class="w-full pl-10 pr-10 py-2.5 bg-transparent border-0 text-sm font-mono text-slate-900 dark:text-zinc-200 tracking-wider placeholder-slate-400 dark:placeholder-zinc-600 focus:ring-0 focus:outline-none"
                  />
                  <button
                    type="button"
                    @click="showPassword = !showPassword"
                    class="absolute inset-y-0 right-0 pr-3.5 flex items-center text-slate-400 dark:text-zinc-500 hover:text-slate-600 dark:hover:text-zinc-300 transition cursor-pointer"
                    :aria-label="showPassword ? 'Hide password' : 'Show password'"
                  >
                    <EyeOff v-if="showPassword" class="w-4 h-4" :stroke-width="1.5" />
                    <Eye v-else class="w-4 h-4" :stroke-width="1.5" />
                  </button>
                </div>
              </div>

              <!-- Confirm Password (Register Mode Only) -->
              <div v-if="authMode === 'register'" class="space-y-1.5 text-left">
                <label class="block font-mono text-[11px] font-medium tracking-wider text-slate-600 dark:text-zinc-400" for="confirmPassword">
                  {{ $t('auth.confirm_password_label') }}
                </label>
                <div class="relative rounded-xl border border-slate-200 dark:border-white/[0.09] bg-slate-50 dark:bg-[#070709] focus-within:border-brand-500 focus-within:ring-1 focus-within:ring-brand-500 transition">
                  <span class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400 dark:text-zinc-500">
                    <Lock class="w-4 h-4" :stroke-width="1.5" />
                  </span>
                  <input
                    id="confirmPassword"
                    v-model="confirmPassword"
                    required
                    :type="showConfirmPassword ? 'text' : 'password'"
                    minlength="8"
                    :placeholder="$t('auth.confirm_password_placeholder')"
                    class="w-full pl-10 pr-10 py-2.5 bg-transparent border-0 text-sm font-mono text-slate-900 dark:text-zinc-200 tracking-wider placeholder-slate-400 dark:placeholder-zinc-600 focus:ring-0 focus:outline-none"
                  />
                  <button
                    type="button"
                    @click="showConfirmPassword = !showConfirmPassword"
                    class="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 focus:outline-none cursor-pointer"
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

              <!-- Remember Session Checkbox (Login Mode Only) -->
              <div v-if="authMode === 'login'" class="flex items-center pt-1">
                <label class="flex items-center gap-2 cursor-pointer text-xs text-slate-600 dark:text-zinc-400 select-none">
                  <input
                    v-model="rememberSession"
                    type="checkbox"
                    class="w-4 h-4 rounded bg-slate-100 dark:bg-[#070709] border-slate-300 dark:border-white/20 text-brand-600 focus:ring-0 focus:ring-offset-0 focus:outline-none cursor-pointer"
                  />
                  <span>{{ $t('auth.remember_session') }}</span>
                </label>
              </div>

              <!-- Primary Submit Action Button -->
              <div class="pt-2">
                <button
                  type="submit"
                  :disabled="isLoading"
                  class="w-full group relative flex items-center justify-center gap-2 py-3 px-4 rounded-xl bg-brand-600 hover:bg-brand-500 active:scale-[0.99] text-white font-semibold text-sm shadow-lg shadow-brand-600/30 transition-all duration-150 ease-out cursor-pointer disabled:opacity-60"
                >
                  <span v-if="isLoading" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
                  <span v-else-if="authMode === 'forgot-password'" class="flex items-center gap-1.5">
                    <span>{{ $t('auth.send_recovery_link_btn') }}</span>
                    <Send class="w-3.5 h-3.5" :stroke-width="1.75" />
                  </span>
                  <span v-else>
                    {{ authMode === 'register' ? $t('auth.submit_register') : $t('auth.enter_cockpit') }}
                  </span>
                  <span v-if="!isLoading" class="inline-flex items-center font-mono text-[11px] px-1.5 py-0.5 rounded bg-brand-700/60 text-brand-200 border border-brand-400/20 group-hover:bg-brand-700 transition">
                    ↵ RETURN
                  </span>
                </button>
              </div>

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

            <!-- Card Security Footer -->
            <div class="mt-6 pt-5 border-t border-slate-200/80 dark:border-white/[0.06] flex items-center justify-between text-xs text-slate-500 dark:text-zinc-500 font-mono">
              <div class="flex items-center gap-1.5 text-emerald-600 dark:text-emerald-400">
                <Shield class="w-3.5 h-3.5" :stroke-width="1.75" />
                <span class="text-[11px] tracking-wide font-medium">{{ $t('auth.zero_knowledge_badge') }}</span>
              </div>
              <div class="flex items-center gap-2">
                <a href="#" class="hover:text-slate-800 dark:hover:text-zinc-300 transition">{{ $t('auth.terms_link') }}</a>
                <span>·</span>
                <a href="#" class="hover:text-slate-800 dark:hover:text-zinc-300 transition">{{ $t('auth.privacy_link') }}</a>
              </div>
            </div>

            <!-- Micro Agreement text -->
            <p class="mt-3 text-[11px] text-center text-slate-400 dark:text-zinc-500">
              {{ $t('auth.terms_agreement') }}
            </p>
          </div>
        </section>
      </div>
    </main>
    <!-- END: MainContentArea -->
  </div>
</template>

<style scoped>
.bg-grid-dots {
  background-size: 24px 24px;
  background-image: radial-gradient(rgba(255, 255, 255, 0.07) 1px, transparent 1px);
}

.tech-border-glow {
  box-shadow: 0 0 45px -10px rgba(124, 58, 237, 0.22),
              inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

input:focus,
input:focus-visible {
  outline: none !important;
  box-shadow: none !important;
}
</style>
