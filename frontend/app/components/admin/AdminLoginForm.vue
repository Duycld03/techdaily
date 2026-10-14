<script setup lang="ts">
import { shallowRef } from 'vue'
import { Eye, EyeOff, ArrowRight, LoaderCircle } from 'lucide-vue-next'
const props = withDefaults(defineProps<{ submitting?: boolean; error?: string | null; initialEmail?: string; initialRemember?: boolean }>(), { initialEmail: '', initialRemember: true })
const emit = defineEmits<{ submit: [payload: { email: string; password: string; rememberMe: boolean }] }>()
const { t } = useI18n()
const email = shallowRef(props.initialEmail)
const password = shallowRef('')
const rememberMe = shallowRef(props.initialRemember)
const visible = shallowRef(false)
const validationError = shallowRef<string | null>(null)
function submit() {
  if (props.submitting) return
  validationError.value = null
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value.trim())) { validationError.value = t('admin.login.invalid_email'); return }
  if (!password.value) { validationError.value = t('admin.login.password_required'); return }
  emit('submit', { email: email.value.trim(), password: password.value, rememberMe: rememberMe.value })
}
</script>

<template>
  <form class="space-y-5" novalidate :aria-busy="submitting" @submit.prevent="submit">
    <div>
      <label for="admin-email" class="mb-2 block text-sm sm:text-base font-semibold">{{ $t('admin.login.email') }}</label>
      <input id="admin-email" v-model="email" name="email" type="email" autocomplete="username" :disabled="submitting" required
        class="min-h-12 w-full rounded-xl border border-slate-300 bg-white px-4 text-sm sm:text-base dark:border-white/[0.1] dark:bg-canvas-subtle focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20" :placeholder="$t('admin.login.email_placeholder')" />
    </div>
    <div>
      <label for="admin-password" class="mb-2 block text-sm sm:text-base font-semibold">{{ $t('admin.login.password') }}</label>
      <div class="relative">
        <input id="admin-password" v-model="password" name="password" :type="visible ? 'text' : 'password'" autocomplete="current-password" :disabled="submitting" required
          class="min-h-12 w-full rounded-xl border border-slate-300 bg-white pl-4 pr-14 text-sm sm:text-base dark:border-white/[0.1] dark:bg-canvas-subtle focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20" />
        <button type="button" class="absolute right-1 top-1/2 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-lg text-slate-500" :aria-label="visible ? $t('admin.login.hide_password') : $t('admin.login.show_password')" @click="visible = !visible">
          <component :is="visible ? EyeOff : Eye" class="h-5 w-5" aria-hidden="true" />
        </button>
      </div>
    </div>
    <label class="flex min-h-11 cursor-pointer items-center gap-3 text-sm sm:text-base text-slate-600 dark:text-slate-300">
      <input v-model="rememberMe" name="rememberMe" type="checkbox" :disabled="submitting" class="h-4 w-4 shrink-0 accent-brand-600" />{{ $t('admin.login.remember') }}
    </label>
    <p v-if="validationError || error" role="alert" class="rounded-xl border border-rose-500/20 bg-rose-500/5 p-3 text-sm sm:text-base text-rose-700 dark:text-rose-300">{{ validationError || error }}</p>
    <button type="submit" :disabled="submitting" class="flex min-h-12 w-full items-center justify-center gap-3 rounded-xl bg-brand-600 px-4 text-sm sm:text-base font-semibold text-white transition-colors hover:bg-brand-500 disabled:opacity-60 whitespace-nowrap shrink-0">
      <LoaderCircle v-if="submitting" class="h-5 w-5 animate-spin" aria-hidden="true" /><ArrowRight v-else class="h-5 w-5" aria-hidden="true" />
      {{ submitting ? $t('admin.login.signing_in') : $t('admin.login.submit') }}
    </button>
  </form>
</template>
