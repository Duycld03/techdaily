<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import {
  Users,
  Search,
  RefreshCw,
  Shield,
  ShieldAlert,
  LogOut,
  UserCheck,
  UserX,
  KeyRound,
  Calendar,
  Globe,
  Loader2
} from 'lucide-vue-next'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
import { useAdminUsersStore, type AdminAccountSummary } from '~/stores/useAdminUsersStore'
import { useAuthStore } from '~/stores/useAuthStore'
import { useToast } from '~/composables/useToast'
import MasterDetailLayout from '~/components/layout/MasterDetailLayout.vue'
import AppModal from '~/components/ui/AppModal.vue'
import AdminRoleModal from '~/components/admin/AdminRoleModal.vue'

definePageMeta({
  layout: 'admin'
})

const access = useAdminAccessStore()
const store = useAdminUsersStore()
const auth = useAuthStore()
const toast = useToast()
const { t, te, locale } = useI18n()

const searchInput = ref('')
const selectedStatus = ref<'Active' | 'Suspended' | ''>('')
const selectedId = ref<string | null>(null)

// Modal states
const activeModal = ref<'suspend' | 'reactivate' | 'revoke' | null>(null)
const actionReason = ref('')
const formError = ref<string | null>(null)
const isRoleModalOpen = ref(false)
const changeRoleButtonRef = ref<HTMLButtonElement | null>(null)

onMounted(async () => {
  if (access.canManageAccounts) {
    await store.fetchUsers()
    if (store.users.length > 0 && !selectedId.value) {
      const user = store.users[0]
      if (!user) return
      handleSelect(user)
    }
  }
})
watch(
  () => access.canManageAccounts,
  async (canManage) => {
    if (canManage && store.users.length === 0) {
      await store.fetchUsers()
      if (store.users.length > 0 && !selectedId.value) {
        const user = store.users[0]
        if (!user) return
        handleSelect(user)
      }
    }
  }
)


function handleSearch() {
  store.search = searchInput.value
  store.page = 1
  store.fetchUsers()
}

function handleStatusFilter(status: 'Active' | 'Suspended' | '') {
  selectedStatus.value = status
  store.statusFilter = status || null
  store.page = 1
  store.fetchUsers()
}

function handlePageChange(newPage: number) {
  store.page = newPage
  store.fetchUsers()
}

async function handleSelect(user: AdminAccountSummary | undefined) {
  if (!user) return
  selectedId.value = user.id
  await store.selectUser(user.id)
}

function openModal(type: 'suspend' | 'reactivate' | 'revoke') {
  activeModal.value = type
  actionReason.value = ''
  formError.value = null
}

function closeModal() {
  activeModal.value = null
  actionReason.value = ''
  formError.value = null
}

async function submitAction() {
  if (!store.selectedUser) return
  if (!actionReason.value.trim()) {
    formError.value = 'Reason is required'
    return
  }

  formError.value = null
  let ok = false

  if (activeModal.value === 'suspend') {
    ok = await store.suspendUser(
      store.selectedUser.id,
      actionReason.value.trim(),
      store.selectedUser.adminControlVersion
    )
  } else if (activeModal.value === 'reactivate') {
    ok = await store.reactivateUser(
      store.selectedUser.id,
      actionReason.value.trim(),
      store.selectedUser.adminControlVersion
    )
  } else if (activeModal.value === 'revoke') {
    ok = await store.revokeSessions(
      store.selectedUser.id,
      actionReason.value.trim(),
      store.selectedUser.adminControlVersion
    )
  }

  if (ok) {
    closeModal()
  } else {
    formError.value = store.error || 'Action failed'
  }
}
function getRoleBadgeLabel(role?: { key: string; nameEn?: string; nameVi?: string }) {
  if (!role) return ''
  const nameKey = `admin.roles.${role.key}.name`
  return te && te(nameKey) ? t(nameKey) : (locale.value === 'vi' ? role.nameVi : role.nameEn) || role.nameEn || role.key
}

function getRoleDescription(role?: { key: string }) {
  if (!role) return ''
  const descKey = `admin.roles.${role.key}.description`
  return te && te(descKey) ? t(descKey) : ''
}

function openRoleModal() {
  isRoleModalOpen.value = true
}

function handleRoleSuccess() {
  toast.success(t('admin.users.roleAssignSuccess'))
}


const totalPages = computed(() => Math.ceil(store.totalCount / store.pageSize) || 1)
</script>

<template>
  <div v-if="access.canManageAccounts" class="h-[calc(100vh-3.5rem)] sm:h-[calc(100vh-3.75rem)] flex flex-col overflow-hidden bg-slate-50 dark:bg-canvas">
    <MasterDetailLayout flush nav-class="md:w-84 lg:w-96" class="flex-1 min-h-0" :nav-label="$t('admin_users.nav_label')" :content-label="$t('admin_users.title')">
      <!-- Header -->
      <template #header>
        <div class="flex items-center justify-between w-full">
          <div class="flex items-center gap-2.5">
            <Users class="h-5 w-5 text-brand-600 dark:text-brand-400" aria-hidden="true" />
            <h1 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white">
              {{ $t('admin_users.title') }}
            </h1>
            <span class="rounded-full bg-slate-200 dark:bg-white/10 px-2.5 py-0.5 text-xs font-semibold text-slate-600 dark:text-slate-300">
              {{ store.totalCount }}
            </span>
          </div>
          <button
            type="button"
            class="flex items-center gap-1.5 rounded-xl border border-slate-300 dark:border-white/10 px-3 py-1.5 text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors"
            :disabled="store.isLoading"
            @click="store.fetchUsers()"
          >
            <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': store.isLoading }" aria-hidden="true" />
            <span class="hidden sm:inline">{{ $t('admin_users.refresh') }}</span>
          </button>
        </div>
      </template>

      <!-- Nav Rail (Account Directory) -->
      <template #nav>
        <div class="flex flex-col gap-3 h-full">
          <!-- Search & Filter bar -->
          <div class="space-y-2">
            <div class="relative">
              <input
                v-model="searchInput"
                type="text"
                :placeholder="$t('admin_users.search_placeholder')"
                class="w-full rounded-xl border border-slate-300 dark:border-white/10 bg-white dark:bg-canvas-subtle/50 pl-9 pr-3 py-2 text-sm text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500/20"
                @keydown.enter="handleSearch"
              />
              <Search class="absolute left-3 top-2.5 h-4 w-4 text-slate-400" aria-hidden="true" />
            </div>

            <!-- Status Filter Pills -->
            <div class="flex gap-1.5">
              <button
                type="button"
                class="px-2.5 py-1 text-xs rounded-lg font-semibold transition-colors"
                :class="selectedStatus === '' ? 'bg-brand-500 text-white' : 'bg-slate-200 dark:bg-white/10 text-slate-600 dark:text-slate-400'"
                @click="handleStatusFilter('')"
              >
                {{ $t('admin_users.filter_all') }}
              </button>
              <button
                type="button"
                class="px-2.5 py-1 text-xs rounded-lg font-semibold transition-colors"
                :class="selectedStatus === 'Active' ? 'bg-emerald-600 text-white' : 'bg-slate-200 dark:bg-white/10 text-slate-600 dark:text-slate-400'"
                @click="handleStatusFilter('Active')"
              >
                {{ $t('admin_users.status_active') }}
              </button>
              <button
                type="button"
                class="px-2.5 py-1 text-xs rounded-lg font-semibold transition-colors"
                :class="selectedStatus === 'Suspended' ? 'bg-rose-600 text-white' : 'bg-slate-200 dark:bg-white/10 text-slate-600 dark:text-slate-400'"
                @click="handleStatusFilter('Suspended')"
              >
                {{ $t('admin_users.status_suspended') }}
              </button>
            </div>
          </div>

          <!-- Account List -->
          <div class="flex-1 overflow-y-auto space-y-1.5 min-h-0">
            <div v-if="store.isLoading && store.users.length === 0" class="flex justify-center py-8">
              <Loader2 class="h-6 w-6 animate-spin text-brand-500" />
            </div>

            <div v-else-if="store.users.length === 0" class="text-center py-8 text-sm text-slate-500 dark:text-slate-400">
              {{ $t('admin_users.empty_list') }}
            </div>

            <div
              v-for="user in store.users"
              :key="user.id"
              class="cursor-pointer rounded-xl p-3 border transition-colors"
              :class="selectedId === user.id ? 'bg-brand-500/10 border-brand-500/30' : 'bg-white dark:bg-canvas-subtle/30 border-slate-200/80 dark:border-white/[0.06] hover:bg-slate-100 dark:hover:bg-white/[0.04]'"
              @click="handleSelect(user)"
            >
              <div class="flex items-center justify-between gap-2">
                <div class="font-semibold text-sm text-slate-900 dark:text-white truncate">
                  {{ user.name || user.email }}
                </div>
                <div class="flex items-center gap-1.5 shrink-0">
                  <span
                    v-if="user.role"
                    class="px-2 py-0.5 text-[11px] font-semibold rounded-md bg-brand-500/10 text-brand-700 dark:text-brand-300 border border-brand-500/20 whitespace-nowrap shrink-0"
                    data-testid="user-role-badge"
                  >
                    {{ getRoleBadgeLabel(user.role) }}
                  </span>
                  <span
                    class="px-2 py-0.5 text-[11px] font-semibold rounded-md whitespace-nowrap shrink-0"
                    :class="user.status === 'Active' ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400' : 'bg-rose-500/10 text-rose-600 dark:text-rose-400'"
                  >
                    {{ user.status === 'Active' ? $t('admin_users.status_active') : $t('admin_users.status_suspended') }}
                  </span>
                </div>
              </div>
              <div class="text-xs text-slate-500 dark:text-slate-400 truncate mt-0.5">
                {{ user.email }}
              </div>
            </div>
          </div>

          <!-- Pagination footer -->
          <div v-if="totalPages > 1" class="flex items-center justify-between pt-2 border-t border-slate-200 dark:border-white/10 text-xs text-slate-500">
            <button
              type="button"
              class="px-2.5 py-1 rounded border border-slate-300 dark:border-white/10 disabled:opacity-40"
              :disabled="store.page <= 1 || store.isLoading"
              @click="handlePageChange(store.page - 1)"
            >
              {{ $t('admin_users.prev') }}
            </button>
            <span>{{ store.page }} / {{ totalPages }}</span>
            <button
              type="button"
              class="px-2.5 py-1 rounded border border-slate-300 dark:border-white/10 disabled:opacity-40"
              :disabled="store.page >= totalPages || store.isLoading"
              @click="handlePageChange(store.page + 1)"
            >
              {{ $t('admin_users.next') }}
            </button>
          </div>
        </div>
      </template>

      <!-- Content Panel (Account Detail) -->
      <template #content>
        <div v-if="store.selectedUser" class="space-y-6 max-w-4xl">
          <!-- Identity Summary Card -->
          <div class="rounded-2xl border border-slate-200/80 dark:border-white/10 bg-white dark:bg-canvas-subtle/50 p-5 sm:p-6 shadow-sm">
            <div class="flex flex-wrap items-start justify-between gap-4">
              <div class="flex items-center gap-4">
                <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 font-bold text-lg">
                  {{ (store.selectedUser.name || store.selectedUser.email)[0]?.toUpperCase() }}
                </div>
                <div>
                  <h2 class="text-lg sm:text-xl font-bold text-slate-900 dark:text-white">
                    {{ store.selectedUser.name }}
                  </h2>
                  <p class="text-sm text-slate-500 dark:text-slate-400 font-mono">
                    {{ store.selectedUser.email }}
                  </p>
                  <div v-if="store.selectedUser.role" class="flex flex-wrap items-center gap-2 mt-2">
                    <span class="inline-flex items-center gap-1.5 rounded-lg px-2.5 py-1 text-xs font-semibold bg-brand-500/10 text-brand-700 dark:text-brand-300 border border-brand-500/20 whitespace-nowrap shrink-0" data-testid="detail-role-badge">
                      <Shield class="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
                      {{ getRoleBadgeLabel(store.selectedUser.role) }}
                    </span>
                    <span v-if="getRoleDescription(store.selectedUser.role)" class="text-xs text-slate-500 dark:text-slate-400">
                      {{ getRoleDescription(store.selectedUser.role) }}
                    </span>
                  </div>
                </div>
              </div>

              <!-- Status Badge -->
              <div>
                <span
                  class="inline-flex items-center gap-1.5 rounded-xl px-3 py-1.5 text-xs font-semibold"
                  :class="store.selectedUser.status === 'Active' ? 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-300 border border-emerald-500/20' : 'bg-rose-500/10 text-rose-700 dark:text-rose-300 border border-rose-500/20'"
                >
                  <component :is="store.selectedUser.status === 'Active' ? UserCheck : UserX" class="h-4 w-4" aria-hidden="true" />
                  {{ store.selectedUser.status === 'Active' ? $t('admin_users.status_active') : $t('admin_users.status_suspended') }}
                </span>
              </div>
            </div>

            <!-- Metadata grid -->
            <div class="mt-6 grid grid-cols-1 sm:grid-cols-3 gap-4 border-t border-slate-200 dark:border-white/10 pt-5 text-sm">
              <div class="flex items-center gap-2 text-slate-600 dark:text-slate-300">
                <Calendar class="h-4 w-4 text-slate-400" />
                <span>{{ $t('admin_users.created_at') }}: {{ new Date(store.selectedUser.createdAt).toLocaleDateString() }}</span>
              </div>
              <div class="flex items-center gap-2 text-slate-600 dark:text-slate-300">
                <Globe class="h-4 w-4 text-slate-400" />
                <span>{{ $t('admin_users.locale') }}: {{ store.selectedUser.preferredLocale }}</span>
              </div>
              <div class="flex items-center gap-2 text-slate-600 dark:text-slate-300">
                <KeyRound class="h-4 w-4 text-slate-400" />
                <span>
                  {{ store.selectedUser.hasPassword ? 'Password' : '' }}
                  {{ store.selectedUser.hasPassword && store.selectedUser.hasGoogleAuth ? ' • ' : '' }}
                  {{ store.selectedUser.hasGoogleAuth ? 'Google' : '' }}
                </span>
              </div>
            </div>
          </div>

          <!-- Suspension Info Banner (If Suspended) -->
          <div v-if="store.selectedUser.status === 'Suspended'" class="rounded-2xl border border-amber-500/20 bg-amber-500/10 p-5 text-amber-800 dark:text-amber-200">
            <div class="flex items-start gap-3">
              <ShieldAlert class="h-5 w-5 shrink-0 text-amber-600 dark:text-amber-400 mt-0.5" />
              <div>
                <h3 class="font-bold text-sm sm:text-base">{{ $t('admin_users.suspension_title') }}</h3>
                <p class="text-sm mt-1">
                  <strong>{{ $t('admin_users.reason_label') }}:</strong> {{ store.selectedUser.suspensionReason }}
                </p>
                <p v-if="store.selectedUser.suspendedAt" class="text-xs text-amber-700 dark:text-amber-300 mt-1">
                  {{ new Date(store.selectedUser.suspendedAt).toUTCString() }}
                </p>
              </div>
            </div>
          </div>

          <!-- Access Control Actions -->
          <div class="rounded-2xl border border-slate-200/80 dark:border-white/10 bg-white dark:bg-canvas-subtle/50 p-5 sm:p-6 shadow-sm">
            <h3 class="font-bold text-base text-slate-900 dark:text-white mb-4">
              {{ $t('admin_users.actions_heading') }}
            </h3>

            <div class="flex flex-wrap gap-3">
              <!-- Change Role Action -->
              <button
                v-if="access.canManageRoles && store.selectedUser.id !== auth.user?.id"
                ref="changeRoleButtonRef"
                type="button"
                class="inline-flex items-center gap-2 rounded-xl bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 transition-colors whitespace-nowrap shrink-0 cursor-pointer"
                data-testid="change-role-btn"
                @click="openRoleModal"
              >
                <Shield class="h-4 w-4 shrink-0" aria-hidden="true" />
                {{ $t('admin.users.changeRole') }}
              </button>

              <!-- Suspend Action (Active only) -->
              <button
                v-if="store.selectedUser.status === 'Active'"
                type="button"
                class="inline-flex items-center gap-2 rounded-xl bg-rose-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-rose-700 transition-colors"
                @click="openModal('suspend')"
              >
                <UserX class="h-4 w-4" />
                {{ $t('admin_users.action_suspend') }}
              </button>

              <!-- Reactivate Action (Suspended only) -->
              <button
                v-if="store.selectedUser.status === 'Suspended'"
                type="button"
                class="inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-emerald-700 transition-colors"
                @click="openModal('reactivate')"
              >
                <UserCheck class="h-4 w-4" />
                {{ $t('admin_users.action_reactivate') }}
              </button>

              <!-- Revoke Sessions Action (Active only) -->
              <button
                v-if="store.selectedUser.status === 'Active'"
                type="button"
                class="inline-flex items-center gap-2 rounded-xl border border-slate-300 dark:border-white/10 px-4 py-2.5 text-sm font-semibold text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors"
                @click="openModal('revoke')"
              >
                <LogOut class="h-4 w-4" />
                {{ $t('admin_users.action_revoke_sessions') }}
              </button>
            </div>
          </div>
        </div>

        <div v-else class="flex flex-col items-center justify-center h-full text-center p-8 text-slate-500">
          <Users class="h-12 w-12 text-slate-300 dark:text-slate-600 mb-3" />
          <p class="text-base font-semibold">{{ $t('admin_users.no_selection') }}</p>
        </div>
      </template>
    </MasterDetailLayout>

    <!-- Confirmation Modal -->
    <AppModal
      :open="Boolean(activeModal)"
      :title="activeModal === 'suspend' ? $t('admin_users.modal_suspend_title') : activeModal === 'reactivate' ? $t('admin_users.modal_reactivate_title') : $t('admin_users.modal_revoke_title')"
      @close="closeModal"
    >
      <div class="space-y-4">
        <p class="text-sm text-slate-600 dark:text-slate-300">
          <span v-if="activeModal === 'suspend'">{{ $t('admin_users.modal_suspend_desc') }}</span>
          <span v-else-if="activeModal === 'reactivate'">{{ $t('admin_users.modal_reactivate_desc') }}</span>
          <span v-else>{{ $t('admin_users.modal_revoke_desc') }}</span>
        </p>

        <!-- Reason Input -->
        <div class="space-y-1.5">
          <label for="admin-action-reason" class="block text-xs font-semibold uppercase tracking-wider text-slate-700 dark:text-slate-300">
            {{ $t('admin_users.reason_label') }} *
          </label>
          <textarea
            id="admin-action-reason"
            v-model="actionReason"
            rows="3"
            maxlength="500"
            :placeholder="$t('admin_users.reason_placeholder')"
            class="w-full rounded-xl border border-slate-300 dark:border-white/10 bg-white dark:bg-canvas-subtle p-3 text-sm text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500/20"
          />
          <div class="flex justify-between text-xs text-slate-400">
            <span>{{ $t('admin_users.reason_guidance') }}</span>
            <span>{{ actionReason.length }} / 500</span>
          </div>
        </div>

        <div v-if="formError" class="rounded-xl border border-rose-500/20 bg-rose-500/10 p-3 text-xs text-rose-600 dark:text-rose-400 font-semibold">
          {{ formError }}
        </div>
      </div>

      <template #footer>
        <div class="flex justify-end gap-2.5">
          <button
            type="button"
            class="px-4 py-2 rounded-xl border border-slate-300 dark:border-white/10 text-sm font-semibold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06]"
            @click="closeModal"
          >
            {{ $t('admin_users.cancel') }}
          </button>
          <button
            type="button"
            class="px-4 py-2 rounded-xl text-sm font-semibold text-white transition-colors disabled:opacity-50"
            :class="activeModal === 'suspend' ? 'bg-rose-600 hover:bg-rose-700' : 'bg-brand-600 hover:bg-brand-700'"
            :disabled="!actionReason.trim() || store.isActionInFlight"
            @click="submitAction"
          >
            <span v-if="store.isActionInFlight" class="flex items-center gap-1.5">
              <Loader2 class="h-4 w-4 animate-spin" />
              {{ $t('admin_users.processing') }}
            </span>
            <span v-else>{{ $t('admin_users.confirm') }}</span>
          </button>
        </div>
      </template>
    </AppModal>

    <!-- Role Assignment Modal -->
    <AdminRoleModal
      :open="isRoleModalOpen"
      :user="store.selectedUser"
      :initiating-button="changeRoleButtonRef"
      @close="isRoleModalOpen = false"
      @success="handleRoleSuccess"
    />
  </div>
</template>
