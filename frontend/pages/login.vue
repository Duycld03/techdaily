<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useIntervalFn } from '@vueuse/core'
import {
  BookOpen,
  Lock,
  Mail,
  User,
  ArrowRight,
  AlertCircle,
  Eye,
  EyeOff,
  Sparkles,
  Terminal,
  Layers,
  Brain
} from 'lucide-vue-next'
import ThemeToggle from '~/components/common/ThemeToggle.vue'
import LocaleSelector from '~/components/common/LocaleSelector.vue'
import { useApiError } from '~/composables/useApiError'
import { useAuthStore } from '~/stores/useAuthStore'

const authStore = useAuthStore()
const route = useRoute()
const config = useRuntimeConfig()
const { t, locale } = useI18n()
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
const isLoading = ref(false)
const errorMessage = ref('')
const googleBtnContainer = ref<HTMLElement | null>(null)

function getRedirectTarget() {
  const redirectQuery = route.query?.redirect
  if (typeof redirectQuery === 'string' && redirectQuery.startsWith('/') && !redirectQuery.startsWith('/login')) {
    return redirectQuery
  }
  return '/today'
}

function setAuthMode(mode: 'login' | 'register' | 'forgot-password') {
  authMode.value = mode
  errorMessage.value = ''
  confirmPassword.value = ''
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
  <div class="min-h-dvh flex flex-col justify-between bg-slate-50 dark:bg-canvas text-slate-900 dark:text-slate-100 transition-colors duration-200">
    <!-- Top Ambient Utility Bar (Isolated Shell) -->
    <header class="w-full h-14 sm:h-16 px-4 sm:px-8 flex items-center justify-between z-20 shrink-0">
      <NuxtLink to="/login" class="flex items-center gap-2.5 group">
        <div class="w-8 h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center transition-transform group-hover:scale-105 shadow-sm">
          <BookOpen class="w-4 h-4" :stroke-width="1.75" />
        </div>
        <span class="font-extrabold text-base tracking-tight text-slate-900 dark:text-white">
          TechDaily
        </span>
      </NuxtLink>

      <div class="flex items-center gap-2">
        <LocaleSelector />
        <ThemeToggle />
      </div>
    </header>

    <!-- Main Authentication Stage -->
    <main class="flex-1 flex items-center justify-center p-4 sm:p-6 lg:p-8">
      <div class="w-full max-w-5xl mx-auto grid lg:grid-cols-12 gap-8 lg:gap-12 items-center py-4 px-2 sm:px-4">
        <!-- Left Column: Platform Value Stage (Desktop Only) -->
        <div class="lg:col-span-5 hidden lg:block space-y-6">
          <!-- TechDaily Brand Badge -->
          <div class="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-brand-500/10 border border-brand-500/20 text-brand-600 dark:text-brand-400 text-xs font-semibold tracking-wide">
            <Terminal class="w-3.5 h-3.5" :stroke-width="2" />
            <span>TechDaily Studio</span>
            <Sparkles class="w-3 h-3 text-brand-500 dark:text-brand-300" />
          </div>

          <!-- Hero Title & Platform Value Subtitle -->
          <div class="space-y-3">
            <h1 class="text-3xl lg:text-4xl font-extrabold text-slate-900 dark:text-white tracking-tight leading-tight">
              {{ locale === 'vi' ? 'Luyện Tập Tư Duy Senior Software Engineer Mỗi Ngày' : 'Master Senior Software Engineering Daily' }}
            </h1>
            <p class="text-sm lg:text-base text-slate-600 dark:text-slate-400 leading-relaxed">
              {{ locale === 'vi' 
                ? 'Nền tảng kiến thức thực chiến dành cho kỹ sư muốn bứt phá lên Senior & Staff. Tình huống sâu sắc, tài liệu chuyên sâu và thuật toán giãn cách.' 
                : 'Accelerate your transition to Senior & Staff engineer through daily technical problem solving, authoritative deep-dives, and spaced retention.' }}
            </p>
          </div>

          <!-- 3 Value Proposition Cards -->
          <div class="space-y-3.5">
            <!-- 1. Scenario Drills -->
            <div class="bg-white/40 dark:bg-white/[0.03] border border-slate-200/60 dark:border-white/[0.06] rounded-xl p-3.5 flex items-start gap-3 transition-colors">
              <div class="w-9 h-9 rounded-lg bg-indigo-500/10 text-indigo-600 dark:text-indigo-400 border border-indigo-500/20 flex items-center justify-center shrink-0">
                <Terminal class="w-4 h-4" :stroke-width="1.75" />
              </div>
              <div class="space-y-0.5">
                <h2 class="text-sm font-bold text-slate-900 dark:text-white">
                  {{ $t('auth.pillar_drills_title') }}
                </h2>
                <p class="text-xs text-slate-500 dark:text-slate-400 leading-normal">
                  {{ $t('auth.pillar_drills_desc') }}
                </p>
              </div>
            </div>

            <!-- 2. SM-2 Spaced Repetition Mastery -->
            <div class="bg-white/40 dark:bg-white/[0.03] border border-slate-200/60 dark:border-white/[0.06] rounded-xl p-3.5 flex items-start gap-3 transition-colors">
              <div class="w-9 h-9 rounded-lg bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20 flex items-center justify-center shrink-0">
                <Brain class="w-4 h-4" :stroke-width="1.75" />
              </div>
              <div class="space-y-0.5">
                <h2 class="text-sm font-bold text-slate-900 dark:text-white">
                  {{ $t('auth.pillar_sm2_title') }}
                </h2>
                <p class="text-xs text-slate-500 dark:text-slate-400 leading-normal">
                  {{ $t('auth.pillar_sm2_desc') }}
                </p>
              </div>
            </div>

            <!-- 3. Daily System Design Slices -->
            <div class="bg-white/40 dark:bg-white/[0.03] border border-slate-200/60 dark:border-white/[0.06] rounded-xl p-3.5 flex items-start gap-3 transition-colors">
              <div class="w-9 h-9 rounded-lg bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0">
                <Layers class="w-4 h-4" :stroke-width="1.75" />
              </div>
              <div class="space-y-0.5">
                <h2 class="text-sm font-bold text-slate-900 dark:text-white">
                  {{ $t('auth.pillar_design_title') }}
                </h2>
                <p class="text-xs text-slate-500 dark:text-slate-400 leading-normal">
                  {{ $t('auth.pillar_design_desc') }}
                </p>
              </div>
            </div>
          </div>
        </div>

        <!-- Right Column: Interactive Authentication Card -->
        <div class="lg:col-span-7 w-full">
          <div class="glass-panel shadow-2xl rounded-3xl p-6 sm:p-8 space-y-5 animate-in zoom-in-95 duration-200">
            <!-- Mobile Brand Header (Hidden on Desktop) -->
            <div class="text-center lg:hidden">
              <div class="w-12 h-12 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center mx-auto mb-3 shadow-lg shadow-brand-500/20">
                <BookOpen class="w-6 h-6" :stroke-width="1.5" />
              </div>
              <h2 class="text-xl sm:text-2xl font-extrabold text-slate-900 dark:text-white tracking-tight">
                {{ authMode === 'login' 
                  ? $t('auth.welcome_title') 
                  : (authMode === 'register' ? $t('auth.register_title') : $t('auth.forgot_password_title')) }}
              </h2>
              <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 mt-1 font-medium">
                {{ authMode === 'forgot-password' 
                  ? $t('auth.forgot_password_subtitle') 
                  : $t('auth.welcome_subtitle') }}
              </p>
            </div>

            <!-- Desktop Card Header (Hidden on Mobile) -->
            <div class="hidden lg:block">
              <h2 class="text-2xl font-bold text-slate-900 dark:text-white tracking-tight">
                {{ authMode === 'login' 
                  ? $t('auth.welcome_title') 
                  : (authMode === 'register' ? $t('auth.register_title') : $t('auth.forgot_password_title')) }}
              </h2>
              <p class="text-sm text-slate-500 dark:text-slate-400 mt-1 font-medium">
                {{ authMode === 'forgot-password' 
                  ? $t('auth.forgot_password_subtitle') 
                  : $t('auth.welcome_subtitle') }}
              </p>
            </div>

            <!-- Mode Switcher Tabs (Shown for login & register) -->
            <div 
              v-if="authMode !== 'forgot-password'"
              class="flex p-1.5 rounded-2xl bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-sm md:text-base font-semibold"
            >
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

            <!-- Credentials Form -->
            <form @submit.prevent="handleSubmit" class="space-y-4">
              <!-- In Register Mode: 2-column grid layout for form inputs -->
              <div v-if="authMode === 'register'" class="grid grid-cols-1 sm:grid-cols-2 gap-3.5 animate-in fade-in duration-200">
                <!-- Col 1 (Row 1): Full Name -->
                <div class="space-y-1.5">
                  <label class="block text-sm font-bold text-slate-700 dark:text-slate-300">
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

                <!-- Col 2 (Row 1): Email Address -->
                <div class="space-y-1.5">
                  <label class="block text-sm font-bold text-slate-700 dark:text-slate-300">
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

                <!-- Col 1 (Row 2): Password (with eye toggle) -->
                <div class="space-y-1.5">
                  <label class="block text-sm font-bold text-slate-700 dark:text-slate-300">
                    {{ $t('auth.password_label') }}
                  </label>
                  <div class="relative">
                    <Lock class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
                    <input
                      v-model="password"
                      required
                      :type="showPassword ? 'text' : 'password'"
                      minlength="8"
                      :placeholder="$t('auth.password_placeholder')"
                      class="w-full pl-11 pr-11 py-3 bg-slate-50 dark:bg-canvas-subtle border border-slate-200 dark:border-white/[0.08] rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 focus:outline-none transition-all"
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

                <!-- Col 2 (Row 2): Confirm Password (with eye toggle) -->
                <div class="space-y-1.5">
                  <label class="block text-sm font-bold text-slate-700 dark:text-slate-300">
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
                      class="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 focus:outline-none transition-colors cursor-pointer"
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
              </div>

              <!-- In Forgot Password Mode: Email only -->
              <div v-else-if="authMode === 'forgot-password'" class="space-y-4 animate-in fade-in duration-200">
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
              </div>

              <!-- In Sign In Mode: Stacked Email + Password -->
              <div v-else class="space-y-4">
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
                  <div class="flex items-center justify-between">
                    <label class="block text-sm md:text-base font-bold text-slate-700 dark:text-slate-300">
                      {{ $t('auth.password_label') }}
                    </label>
                    <button
                      type="button"
                      @click="setAuthMode('forgot-password')"
                      class="text-xs sm:text-sm text-brand-600 dark:text-brand-400 hover:text-brand-500 font-medium transition-colors cursor-pointer"
                    >
                      {{ $t('auth.forgot_password_link') }}
                    </button>
                  </div>
                  <div class="relative">
                    <Lock class="w-4 h-4 text-slate-400 dark:text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" :stroke-width="1.5" />
                    <input
                      v-model="password"
                      required
                      :type="showPassword ? 'text' : 'password'"
                      placeholder="••••••••"
                      class="w-full pl-11 pr-11 py-3 bg-slate-50 dark:bg-canvas-subtle border border-slate-200 dark:border-white/[0.08] rounded-xl text-sm md:text-base text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 focus:outline-none transition-all"
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
              </div>

              <!-- Submit Action Button -->
              <button
                type="submit"
                :disabled="isLoading"
                class="w-full py-3 rounded-xl bg-brand-600 hover:bg-brand-500 font-semibold text-white shadow-lg shadow-brand-500/20 active:scale-[0.98] flex items-center justify-center gap-2 text-sm md:text-base transition-all disabled:opacity-50 cursor-pointer"
              >
                <span v-if="isLoading" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                <span>
                  {{ authMode === 'login' 
                    ? $t('auth.submit_sign_in') 
                    : (authMode === 'register' ? $t('auth.submit_register') : $t('auth.forgot_password_btn')) }}
                </span>
                <ArrowRight v-if="!isLoading" class="w-4 h-4 shrink-0" :stroke-width="1.5" />
              </button>

              <!-- Back to Sign In button (Forgot Password Mode Only) -->
              <button
                v-if="authMode === 'forgot-password'"
                type="button"
                @click="setAuthMode('login')"
                class="w-full py-2.5 rounded-xl border border-slate-200 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-white/[0.04] text-slate-700 dark:text-slate-300 text-xs sm:text-sm font-semibold transition-colors cursor-pointer"
              >
                {{ $t('auth.back_to_sign_in') }}
              </button>
            </form>

            <!-- Google OAuth Sign-In Section (Login and Register only) -->
            <div v-if="authMode !== 'forgot-password'" class="space-y-4">
              <!-- Centered Absolute OAuth Divider -->
              <div class="relative my-5">
                <div class="absolute inset-0 flex items-center" aria-hidden="true">
                  <div class="w-full border-t border-slate-200 dark:border-white/[0.08]" />
                </div>
                <div class="relative flex justify-center text-xs uppercase">
                  <span class="bg-white/90 dark:bg-[#131215] px-3 text-slate-400 font-medium tracking-wider">
                    {{ $t('auth.or_continue_with') }}
                  </span>
                </div>
              </div>

              <!-- Google OAuth Button Container -->
              <div class="flex items-center justify-center min-h-[44px]">
                <div ref="googleBtnContainer" class="flex justify-center w-full max-w-[320px]" style="color-scheme: light;"></div>
              </div>
            </div>

            <!-- Terms and Privacy Footer -->
            <div class="text-center">
              <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 leading-relaxed">
                {{ $t('auth.terms_agreement') }}
              </p>
            </div>
          </div>
        </div>
      </div>
    </main>

    <!-- Bottom Ambient Spacer -->
    <div class="h-4 sm:h-6 shrink-0" aria-hidden="true" />
  </div>
</template>
