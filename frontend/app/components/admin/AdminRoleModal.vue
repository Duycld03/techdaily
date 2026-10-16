<script setup lang="ts">
import { ref, computed, watch, nextTick, onUnmounted, useTemplateRef } from 'vue'
import {
  Shield,
  ShieldAlert,
  AlertTriangle,
  Loader2,
  RefreshCw,
  X
} from 'lucide-vue-next'
import AppSelect, { type SelectOption } from '~/components/common/AppSelect.vue'
import { useAdminUsersStore, type AdminAccountDetail } from '~/stores/useAdminUsersStore'
import { useAuthStore } from '~/stores/useAuthStore'

const props = defineProps<{
  open: boolean
  user: AdminAccountDetail | null
  initiatingButton?: HTMLElement | null
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'success', user: AdminAccountDetail): void
}>()

const usersStore = useAdminUsersStore()
const authStore = useAuthStore()

const { t, te, locale } = useI18n()

const dialogShellRef = useTemplateRef<HTMLElement>('dialogShellRef')
const capturedTriggerElement = ref<HTMLElement | null>(null)

const selectedRoleId = ref<string>('')
const reason = ref('')
const formSubmitted = ref(false)
const isSubmitting = ref(false)
const isReloading = ref(false)
const formError = ref<string | null>(null)
const isConflict = ref(false)
const reloadedUser = ref<AdminAccountDetail | null>(null)
const effectiveUser = computed(() => reloadedUser.value || props.user)

let currentTargetUserId: string | null = null
let currentActorId: string | null = null
let currentSessionGen = -1
let currentAdminGen = -1
let currentDialogInteractionId = 0
let currentReloadRequestId = 0

const dialogTitleId = 'admin-role-dialog-title'
const roleSelectLabelId = 'admin-role-select-label'

const roleOptions = computed<SelectOption[]>(() => {
  return (usersStore.roles || []).map(r => {
    const nameKey = `admin.roles.${r.key}.name`
    const descKey = `admin.roles.${r.key}.description`
    const label = te && te(nameKey)
      ? t(nameKey)
      : (locale.value === 'vi' ? r.nameVi : r.nameEn) || r.nameEn || r.key
    const description = te && te(descKey) ? t(descKey) : ''
    return {
      value: r.id,
      label,
      description
    }
  })
})

const selectedRoleOption = computed(() => {
  return (usersStore.roles || []).find(r => r.id === selectedRoleId.value)
})

const currentRoleName = computed(() => {
  const role = effectiveUser.value?.role
  if (!role) return ''
  const nameKey = `admin.roles.${role.key}.name`
  return te && te(nameKey)
    ? t(nameKey)
    : (locale.value === 'vi' ? role.nameVi : role.nameEn) || role.nameEn || role.key
})

const currentRoleDescription = computed(() => {
  const role = effectiveUser.value?.role
  if (!role) return ''
  const descKey = `admin.roles.${role.key}.description`
  return te && te(descKey) ? t(descKey) : ''
})

const isSameRole = computed(() => {
  const roleId = effectiveUser.value?.role?.id
  if (!roleId || !selectedRoleId.value) return false
  return selectedRoleId.value === roleId
})

const isSelfChange = computed(() => {
  return Boolean(effectiveUser.value?.id && authStore.user?.id && effectiveUser.value.id === authStore.user.id)
})
const isReasonValid = computed(() => {
  const trimmed = reason.value.trim()
  return trimmed.length >= 1 && trimmed.length <= 500
})

const canSubmit = computed(() => {
  return (
    Boolean(selectedRoleId.value) &&
    !isSameRole.value &&
    !isSelfChange.value &&
    isReasonValid.value &&
    !isSubmitting.value &&
    !isConflict.value
  )
})
function getPermissionLabel(perm: string): string {
  const permKey = `admin.permissions.${perm}`
  return te && te(permKey) ? t(permKey) : perm
}


function getFocusableElements(container: HTMLElement): HTMLElement[] {
  const selector = [
    'button:not([disabled])',
    '[href]',
    'input:not([disabled])',
    'select:not([disabled])',
    'textarea:not([disabled])',
    '[tabindex]:not([tabindex="-1"])'
  ].join(',')
  return Array.from(container.querySelectorAll<HTMLElement>(selector))
    .filter(el => el.offsetParent !== null || el.offsetWidth > 0 || el.offsetHeight > 0)
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    event.preventDefault()
    closeModal()
    return
  }

  if (event.key === 'Tab') {
    if (!dialogShellRef.value) return
    const focusable = getFocusableElements(dialogShellRef.value)
    if (focusable.length === 0) {
      event.preventDefault()
      return
    }

    const first = focusable[0]
    const last = focusable[focusable.length - 1]

    if (event.shiftKey) {
      if (document.activeElement === first || !dialogShellRef.value.contains(document.activeElement)) {
        event.preventDefault()
        last?.focus()
      }
    } else {
      if (document.activeElement === last || !dialogShellRef.value.contains(document.activeElement)) {
        event.preventDefault()
        first?.focus()
      }
    }
  }
}

function invalidateReload() {
  currentDialogInteractionId++
  isReloading.value = false
}

watch(
  [() => authStore.user?.id, () => authStore.sessionGeneration, () => authStore.adminGeneration],
  invalidateReload,
  { flush: 'sync' }
)

watch(
  [() => props.open, () => props.user?.id],
  ([isOpen, targetUserId], [wasOpen]) => {
    invalidateReload()
    if (typeof document === 'undefined') return

    if (isOpen) {
      capturedTriggerElement.value = props.initiatingButton || (document.activeElement instanceof HTMLElement ? document.activeElement : null)

      const isSameTargetAndSession =
        targetUserId === currentTargetUserId &&
        authStore.user?.id === currentActorId &&
        authStore.sessionGeneration === currentSessionGen &&
        authStore.adminGeneration === currentAdminGen

      if (!isSameTargetAndSession) {
        currentTargetUserId = targetUserId || null
        currentActorId = authStore.user?.id || null
        currentSessionGen = authStore.sessionGeneration
        currentAdminGen = authStore.adminGeneration

        selectedRoleId.value = ''
        reason.value = ''
        reloadedUser.value = null
        isConflict.value = false
        formError.value = null
        formSubmitted.value = false
      }

      if (typeof usersStore.fetchRoles === 'function' && (!usersStore.roles || usersStore.roles.length === 0) && !usersStore.isLoadingRoles) {
        usersStore.fetchRoles()
      }

      document.body.style.overflow = 'hidden'

      nextTick(() => {
        if (dialogShellRef.value) {
          const focusable = getFocusableElements(dialogShellRef.value)
          if (focusable.length > 0) {
            focusable[0]?.focus()
          }
        }
      })
    } else {
      document.body.style.overflow = ''
      const targetToFocus = capturedTriggerElement.value || props.initiatingButton
      if (wasOpen && targetToFocus && typeof targetToFocus.focus === 'function') {
        nextTick(() => {
          targetToFocus.focus()
        })
      }
    }
  },
  { immediate: true }
)
onUnmounted(() => {
  invalidateReload()
  if (typeof document !== 'undefined') {
    document.body.style.overflow = ''
  }
})


function closeModal() {
  invalidateReload()
  emit('close')
}

async function handleReloadAccount() {
  const targetId = effectiveUser.value?.id
  if (!targetId) return
  isReloading.value = true

  const interactionId = currentDialogInteractionId
  const requestId = ++currentReloadRequestId
  const expectedActorId = authStore.user?.id
  const expectedSessionGen = authStore.sessionGeneration
  const expectedAdminGen = authStore.adminGeneration

  const ownsCurrentReload = () =>
    props.open &&
    props.user?.id === targetId &&
    effectiveUser.value?.id === targetId &&
    currentDialogInteractionId === interactionId &&
    currentReloadRequestId === requestId &&
    authStore.user?.id === expectedActorId &&
    authStore.sessionGeneration === expectedSessionGen &&
    authStore.adminGeneration === expectedAdminGen

  try {
    const reloadedDetail = await usersStore.selectUser(targetId)

    if (!ownsCurrentReload()) return

    if (reloadedDetail?.id === targetId) {
      reloadedUser.value = reloadedDetail
      isConflict.value = false
      formError.value = null
    } else if (!reloadedDetail) {
      formError.value = usersStore.error || t('admin.users.reloadFailed')
    }
  } catch (err: unknown) {
    if (!ownsCurrentReload()) return
    const message = err && typeof err === 'object' && 'message' in err ? String(err.message) : 'Failed to reload account'
    formError.value = message
  } finally {
    if (ownsCurrentReload()) isReloading.value = false
  }
}
async function submitRoleAssignment() {
  formSubmitted.value = true
  const target = effectiveUser.value
  if (!target || !canSubmit.value) return

  if (isSelfChange.value) {
    formError.value = t('admin.users.roleSelfChangeError')
    return
  }

  if (isSameRole.value) {
    formError.value = t('admin.users.roleSameError')
    return
  }

  if (!reason.value.trim()) {
    formError.value = t('admin.users.roleReasonRequired')
    return
  }

  if (reason.value.trim().length > 500) {
    formError.value = t('admin.users.roleReasonTooLong')
    return
  }

  isSubmitting.value = true
  formError.value = null

  try {

    const result = await usersStore.assignRole(
      target.id,
      selectedRoleId.value,
      reason.value.trim(),
      target.adminControlVersion
    )
    if (result && result.success) {
      emit('success', usersStore.selectedUser || reloadedUser.value || target)
      currentTargetUserId = null
      reloadedUser.value = null
      isConflict.value = false
      closeModal()
    } else {
      const code = result?.errorCode
      const msg = result?.errorMessage
      if (code === 'ACCOUNT_CONCURRENCY_CONFLICT' || msg?.toLowerCase().includes('modified by another administrator') || msg?.toLowerCase().includes('concurrency')) {
        isConflict.value = true
        formError.value = t('admin.users.roleConcurrencyError')
      } else if (code === 'ACCOUNT_LAST_ADMIN_PROTECTED') {
        formError.value = t('admin.users.roleLastAdminError')
      } else if (code === 'ACCOUNT_ROLE_UNCHANGED') {
        formError.value = t('admin.users.roleSameError')
      } else if (code === 'ACCOUNT_SELF_ROLE_CHANGE_BLOCKED') {
        formError.value = t('admin.users.roleSelfChangeError')
      } else {
        formError.value = msg || usersStore.error || 'Role assignment failed'
      }
    }
  } catch (err: unknown) {
    const message = err && typeof err === 'object' && 'message' in err ? String(err.message) : 'Role assignment failed'
    formError.value = message
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition duration-150 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition duration-100 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="open"
        class="fixed inset-0 z-50 flex items-center justify-center p-3 sm:p-4"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="dialogTitleId"
        @keydown="handleKeydown"
      >
        <!-- Backdrop -->
        <div
          class="absolute inset-0 bg-slate-900/60 dark:bg-black/75 backdrop-blur-sm"
          aria-hidden="true"
          @click="closeModal"
        />

        <!-- Dialog Shell -->
        <div
          ref="dialogShellRef"
          class="relative z-10 flex max-h-[90vh] w-full max-w-lg flex-col overflow-hidden rounded-2xl sm:rounded-3xl border border-slate-200/80 dark:border-white/10 bg-white dark:bg-canvas-elevated shadow-2xl text-slate-900 dark:text-slate-100 text-sm sm:text-base"
        >
          <!-- Fixed Header -->
          <header class="flex shrink-0 items-center justify-between gap-3 border-b border-slate-200/80 dark:border-white/[0.08] px-5 sm:px-6 py-4">
            <div class="min-w-0 flex-1">
              <h2 :id="dialogTitleId" class="text-base sm:text-lg font-bold text-slate-900 dark:text-white truncate">
                {{ $t('admin.users.roleAssignmentTitle') }}
              </h2>
              <p v-if="effectiveUser" class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 truncate mt-0.5">
                {{ effectiveUser.name || effectiveUser.email }} ({{ effectiveUser.email }})
              </p>
            </div>
            <button
              type="button"
              :aria-label="$t('admin_users.cancel')"
              class="min-h-11 min-w-11 p-2 rounded-xl text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors shrink-0 cursor-pointer flex items-center justify-center whitespace-nowrap"
              @click="closeModal"
            >
              <X class="h-5 w-5 shrink-0" aria-hidden="true" />
            </button>
          </header>

          <!-- Scrollable Body -->
          <div class="max-h-[65vh] flex-1 overflow-y-auto px-5 sm:px-6 py-4 sm:py-5 space-y-5 [scrollbar-gutter:stable]">
            <!-- Current Role Display -->
            <div class="rounded-xl border border-slate-200 dark:border-white/10 bg-slate-50 dark:bg-canvas-subtle p-3.5 space-y-1.5">
              <div class="text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {{ $t('admin.users.currentRole') }}
              </div>
              <div class="flex items-center gap-2">
                <span class="inline-flex items-center gap-1.5 rounded-lg px-2.5 py-1 text-xs sm:text-sm font-semibold bg-brand-500/10 text-brand-700 dark:text-brand-300 border border-brand-500/20 whitespace-nowrap shrink-0">
                  <Shield class="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
                  {{ currentRoleName }}
                </span>
              </div>
              <p v-if="currentRoleDescription" class="text-xs text-slate-500 dark:text-slate-400 mt-1">
                {{ currentRoleDescription }}
              </p>
            </div>

            <!-- Role Selector -->
            <div class="space-y-1.5">
              <label :id="roleSelectLabelId" class="block text-xs font-semibold uppercase tracking-wider text-slate-700 dark:text-slate-300">
                {{ $t('admin.users.selectRole') }} *
              </label>

              <!-- Loading Skeleton -->
              <div
                v-if="usersStore.isLoadingRoles"
                class="h-11 w-full animate-pulse rounded-xl bg-slate-200 dark:bg-white/10 flex items-center px-4"
                aria-busy="true"
                data-testid="role-loading-skeleton"
              >
                <Loader2 class="h-4 w-4 animate-spin text-slate-400 mr-2 shrink-0" />
                <span class="text-xs text-slate-400">{{ $t('admin_users.processing') }}</span>
              </div>
              <!-- Error State & Retry -->
              <div
                v-else-if="usersStore.rolesError"
                class="rounded-xl border border-rose-300 dark:border-rose-900/50 bg-rose-50 dark:bg-rose-950/20 p-3 flex items-center justify-between text-xs text-rose-700 dark:text-rose-300"
                data-testid="role-error-state"
              >
                <span>{{ usersStore.rolesError }}</span>
                <button
                  type="button"
                  class="flex items-center gap-1 font-semibold text-rose-800 dark:text-rose-200 hover:underline shrink-0 cursor-pointer"
                  @click="usersStore.fetchRoles()"
                >
                  <RefreshCw class="h-3.5 w-3.5" />
                  {{ $t('common.retry') }}
                </button>
              </div>

              <!-- Accessible Combobox -->
              <AppSelect
                v-else
                v-model="selectedRoleId"
                :options="roleOptions"
                :placeholder="$t('admin.users.selectRole')"
                :aria-label="$t('admin.users.selectRole')"
                size="md"
                data-testid="admin-role-select"
              />
              <!-- Same Role Error Note -->
              <p v-if="isSameRole" class="text-xs text-amber-600 dark:text-amber-400 font-medium">
                {{ $t('admin.users.roleSameError') }}
              </p>
            </div>

            <!-- Supported Permissions of selected role -->
            <div
              v-if="selectedRoleOption && selectedRoleOption.permissionKeys && selectedRoleOption.permissionKeys.length > 0"
              class="space-y-2"
              data-testid="supported-permissions-block"
            >
              <div class="text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {{ $t('admin.users.supportedPermissions') }}
              </div>
              <div class="flex flex-wrap gap-1.5">
                <span
                  v-for="perm in selectedRoleOption.permissionKeys"
                  :key="perm"
                  class="rounded-md bg-slate-100 dark:bg-white/[0.08] px-2 py-0.5 text-xs font-medium text-slate-700 dark:text-slate-300 border border-slate-200 dark:border-white/10 whitespace-nowrap shrink-0"
                >
                  {{ getPermissionLabel(perm) }}
                </span>
              </div>
            </div>

            <!-- Empty grant list indicator (e.g. Learner) -->
            <div
              v-else-if="selectedRoleOption && (!selectedRoleOption.permissionKeys || selectedRoleOption.permissionKeys.length === 0)"
              class="space-y-1 text-xs text-slate-500 dark:text-slate-400 italic"
              data-testid="empty-permissions-block"
            >
              {{ $t('admin.users.noPermissions') }}
            </div>
            <!-- Reason Textarea -->
            <div class="space-y-1.5">
              <label for="role-change-reason" class="block text-xs font-semibold uppercase tracking-wider text-slate-700 dark:text-slate-300">
                {{ $t('admin.users.roleReasonLabel') }} *
              </label>
              <textarea
                id="role-change-reason"
                v-model="reason"
                rows="3"
                maxlength="500"
                :placeholder="$t('admin.users.roleReasonPlaceholder')"
                class="w-full rounded-xl border border-slate-300 dark:border-white/10 bg-white dark:bg-canvas-subtle p-3 text-sm text-slate-900 dark:text-white placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
                data-testid="admin-role-reason"
              />
              <div class="flex items-center justify-between text-xs text-slate-400">
                <span v-if="reason.length === 0 && formSubmitted" class="text-rose-500 font-medium">
                  {{ $t('admin.users.roleReasonRequired') }}
                </span>
                <span v-else-if="reason.length > 500" class="text-rose-500 font-medium">
                  {{ $t('admin.users.roleReasonTooLong') }}
                </span>
                <span v-else>{{ $t('admin_users.reason_guidance') }}</span>
                <span class="font-mono whitespace-nowrap shrink-0">{{ reason.length }} / 500</span>
              </div>
            </div>

            <!-- Session Revocation Warning Callout -->
            <div class="rounded-xl border border-amber-500/20 bg-amber-500/10 p-3.5 text-xs sm:text-sm text-amber-800 dark:text-amber-200 flex items-start gap-2.5">
              <AlertTriangle class="h-5 w-5 shrink-0 text-amber-600 dark:text-amber-400 mt-0.5" aria-hidden="true" />
              <div class="leading-relaxed">
                {{ $t('admin.users.roleAssignmentWarning') }}
              </div>
            </div>

            <!-- Error Banner & Concurrency Reload Action -->
            <div v-if="formError" class="rounded-xl border border-rose-500/20 bg-rose-500/10 p-3 text-xs sm:text-sm text-rose-600 dark:text-rose-400 space-y-2" data-testid="role-form-error">
              <div class="font-semibold flex items-center gap-1.5">
                <ShieldAlert class="h-4 w-4 shrink-0" aria-hidden="true" />
                <span>{{ formError }}</span>
              </div>
              <div v-if="isConflict" class="pt-1">
                <button
                  type="button"
                  class="inline-flex items-center gap-1.5 rounded-lg bg-rose-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-rose-700 transition-colors whitespace-nowrap shrink-0 cursor-pointer"
                  :disabled="isReloading"
                  data-testid="reload-account-btn"
                  @click="handleReloadAccount"
                >
                  <RefreshCw class="h-3.5 w-3.5 shrink-0" :class="{ 'animate-spin': isReloading }" aria-hidden="true" />
                  {{ $t('admin.users.reloadAccount') }}
                </button>
              </div>
            </div>
          </div>

          <!-- Sticky Footer -->
          <footer class="flex shrink-0 items-center justify-end gap-2.5 border-t border-slate-200/80 bg-slate-50/90 px-5 sm:px-6 py-3.5 dark:border-white/[0.08] dark:bg-canvas-subtle/80">
            <button
              type="button"
              class="h-10 rounded-xl border border-slate-300 dark:border-white/10 px-4 text-sm font-semibold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors whitespace-nowrap shrink-0 cursor-pointer"
              @click="closeModal"
            >
              {{ $t('admin_users.cancel') }}
            </button>
            <button
              type="button"
              class="h-10 rounded-xl bg-brand-600 px-4 text-sm font-semibold text-white shadow-sm hover:bg-brand-500 transition-colors disabled:opacity-50 disabled:cursor-not-allowed whitespace-nowrap shrink-0 cursor-pointer flex items-center gap-1.5"
              :disabled="!canSubmit"
              data-testid="submit-role-assign"
              @click="submitRoleAssignment"
            >
              <Loader2 v-if="isSubmitting" class="h-4 w-4 animate-spin shrink-0" aria-hidden="true" />
              <span>{{ isSubmitting ? $t('admin_users.processing') : $t('admin.users.assignRole') }}</span>
            </button>
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
