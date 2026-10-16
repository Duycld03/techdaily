<script setup lang="ts">
import { ref, computed, onMounted, watch, nextTick } from 'vue'
import {
  FolderTree,
  Search,
  RefreshCw,
  Plus,
  Pencil,
  Trash2,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Loader2,
  Info,
  Power
} from 'lucide-vue-next'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
import { useCategoryStore, type CategoryDescriptor, type AdminCategoryDetail } from '~/stores/useCategoryStore'
import { useToast } from '~/composables/useToast'
import MasterDetailLayout from '~/components/layout/MasterDetailLayout.vue'
import AppModal from '~/components/ui/AppModal.vue'
import AdminCategoryModal from '~/components/admin/AdminCategoryModal.vue'

definePageMeta({
  layout: 'admin'
})

const access = useAdminAccessStore()
const categoryStore = useCategoryStore()
const toast = useToast()
const { t, locale } = useI18n()

const searchInput = ref('')
const selectedStatus = ref<'all' | 'active' | 'inactive'>('all')
const selectedId = ref<string | null>(null)
const selectedDetail = ref<AdminCategoryDetail | null>(null)
const isLoadingDetail = ref(false)
const detailError = ref<string | null>(null)
const categoryListRef = ref<HTMLElement | null>(null)
const createButtonRef = ref<HTMLButtonElement | null>(null)
let detailRequest = 0

// Modals
const isCategoryModalOpen = ref(false)
const categoryToEdit = ref<CategoryDescriptor | null>(null)
const isDeactivateModalOpen = ref(false)
const isDeleteModalOpen = ref(false)
const isActionSubmitting = ref(false)
const actionError = ref<string | null>(null)

const categoryModalTrigger = ref<HTMLElement | null>(null)
const deactivateModalTrigger = ref<HTMLElement | null>(null)
const deleteModalTrigger = ref<HTMLElement | null>(null)

onMounted(async () => {
  if (access.canManageCategories) {
    await categoryStore.fetchCategories()
    const firstCategory = categoryStore.allCategories[0]
    if (firstCategory && !selectedId.value) {
      handleSelect(firstCategory)
    }
  }
})

watch(
  () => access.canManageCategories,
  async (canManage) => {
    if (canManage && categoryStore.categories.length === 0) {
      await categoryStore.fetchCategories()
      const firstCategory = categoryStore.allCategories[0]
      if (firstCategory && !selectedId.value) {
        handleSelect(firstCategory)
      }
    }
  }
)

const filteredCategories = computed<CategoryDescriptor[]>(() => {
  let list = categoryStore.allCategories

  if (selectedStatus.value === 'active') {
    list = list.filter(c => c.isActive)
  } else if (selectedStatus.value === 'inactive') {
    list = list.filter(c => !c.isActive)
  }

  const query = searchInput.value.trim().toLowerCase()
  if (query) {
    list = list.filter(c =>
      c.nameEn.toLowerCase().includes(query) ||
      c.nameVi.toLowerCase().includes(query) ||
      c.slug.toLowerCase().includes(query)
    )
  }

  return list
})

const selectedCategory = computed<CategoryDescriptor | undefined>(() => {
  if (!selectedId.value) return undefined
  return categoryStore.getCategoryById(selectedId.value)
})

async function handleSelect(category: CategoryDescriptor) {
  const request = ++detailRequest
  selectedId.value = category.id
  selectedDetail.value = null
  detailError.value = null
  isLoadingDetail.value = true
  try {
    const detail = await categoryStore.fetchCategoryDetail(category.id)
    if (request === detailRequest && selectedId.value === category.id) selectedDetail.value = detail
  } catch {
    if (request === detailRequest) detailError.value = t('categories.metadataUnavailable')
  } finally {
    if (request === detailRequest) isLoadingDetail.value = false
  }
}

function openCreateModal(event?: MouseEvent) {
  categoryToEdit.value = null
  categoryModalTrigger.value = (event?.currentTarget as HTMLElement) || null
  isCategoryModalOpen.value = true
}

function openEditModal(event?: MouseEvent) {
  if (!selectedCategory.value) return
  categoryToEdit.value = selectedCategory.value
  categoryModalTrigger.value = (event?.currentTarget as HTMLElement) || null
  isCategoryModalOpen.value = true
}

function onCategoryModalSuccess(saved: CategoryDescriptor) {
  selectedId.value = saved.id
  handleSelect(saved)
}

function openDeactivateModal(event?: MouseEvent) {
  actionError.value = null
  deactivateModalTrigger.value = (event?.currentTarget as HTMLElement) || null
  isDeactivateModalOpen.value = true
}

async function handleConfirmDeactivate() {
  if (!selectedCategory.value) return
  isActionSubmitting.value = true
  actionError.value = null
  try {
    const cat = selectedCategory.value
    await categoryStore.updateCategory(cat.id, {
      nameEn: cat.nameEn,
      nameVi: cat.nameVi,
      sortOrder: cat.sortOrder,
      isActive: false,
      expectedVersion: cat.version
    })
    isDeactivateModalOpen.value = false
    toast.success(t('admin.categories.actions.deactivate') + ' OK')
    const fresh = categoryStore.getCategoryById(cat.id)
    if (fresh) handleSelect(fresh)
  } catch (err: unknown) {
    const errorObj = err as Record<string, unknown>
    actionError.value = (errorObj?.message as string) || 'Failed to deactivate category.'
  } finally {
    isActionSubmitting.value = false
  }
}

async function handleActivate() {
  if (!selectedCategory.value) return
  isActionSubmitting.value = true
  try {
    const cat = selectedCategory.value
    await categoryStore.updateCategory(cat.id, {
      nameEn: cat.nameEn,
      nameVi: cat.nameVi,
      sortOrder: cat.sortOrder,
      isActive: true,
      expectedVersion: cat.version
    })
    toast.success(t('admin.categories.actions.activate') + ' OK')
    const fresh = categoryStore.getCategoryById(cat.id)
    if (fresh) handleSelect(fresh)
  } catch (err: unknown) {
    const errorObj = err as Record<string, unknown>
    toast.error((errorObj?.message as string) || 'Failed to activate category.')
  } finally {
    isActionSubmitting.value = false
  }
}

function openDeleteModal(event?: MouseEvent) {
  actionError.value = null
  deleteModalTrigger.value = (event?.currentTarget as HTMLElement) || null
  isDeleteModalOpen.value = true
}

async function handleConfirmDelete() {
  if (!selectedCategory.value) return
  isActionSubmitting.value = true
  actionError.value = null
  try {
    const cat = selectedCategory.value
    await categoryStore.deleteCategory(cat.id, cat.version)
    isDeleteModalOpen.value = false
    toast.success(t('admin.categories.actions.delete') + ' OK')

    const nextCategory = filteredCategories.value[0]
    if (nextCategory) {
      void handleSelect(nextCategory)
    } else {
      detailRequest++
      selectedId.value = null
      selectedDetail.value = null
    }
    await nextTick()
    const nextChoice = categoryListRef.value?.querySelector<HTMLButtonElement>('button[aria-pressed="true"]')
    ;(nextChoice ?? createButtonRef.value)?.focus()
  } catch (err: unknown) {
    const errorObj = err as Record<string, unknown>
    const code = (errorObj?.code as string) || ''
    if (code === 'Category.InUse' || (errorObj?.message as string)?.includes('InUse')) {
      actionError.value = t('admin.categories.errors.Category.InUse')
    } else if (code === 'Category.ConcurrencyConflict' || (errorObj?.message as string)?.includes('ConcurrencyConflict')) {
      actionError.value = t('admin.categories.errors.Category.ConcurrencyConflict')
    } else {
      actionError.value = (errorObj?.message as string) || 'Failed to delete category.'
    }
  } finally {
    isActionSubmitting.value = false
  }
}
</script>

<template>
  <div v-if="access.canManageCategories" class="h-[calc(100vh-3.5rem)] sm:h-[calc(100vh-3.75rem)] flex flex-col overflow-hidden bg-slate-50 dark:bg-canvas">
    <MasterDetailLayout
      flush
      nav-class="md:w-84 lg:w-96"
      class="flex-1 min-h-0"
      :nav-label="$t('admin.categories.title')"
      :content-label="$t('admin.categories.manage')"
    >
      <!-- Header -->
      <template #header>
        <div class="flex items-center justify-between w-full">
          <div class="flex items-center gap-2.5">
            <FolderTree class="h-5 w-5 text-brand-600 dark:text-brand-400" aria-hidden="true" />
            <h1 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white">
              {{ $t('admin.categories.title') }}
            </h1>
            <span class="rounded-full bg-slate-200 dark:bg-white/10 px-2.5 py-0.5 text-xs font-semibold text-slate-600 dark:text-slate-300">
              {{ categoryStore.categories.length }}
            </span>
          </div>
          <div class="flex items-center gap-2">
            <button
              type="button"
              class="flex items-center gap-1.5 rounded-xl border border-slate-300 dark:border-white/10 px-3 py-1.5 text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors whitespace-nowrap shrink-0"
              :disabled="categoryStore.isLoading"
              @click="categoryStore.fetchCategories(true)"
            >
              <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': categoryStore.isLoading }" aria-hidden="true" />
              <span class="hidden sm:inline">{{ $t('admin.categories.refresh') }}</span>
            </button>
            <button
              ref="createButtonRef"
              type="button"
              class="flex items-center gap-1.5 rounded-xl bg-brand-600 px-3.5 py-1.5 text-xs sm:text-sm font-semibold text-white hover:bg-brand-500 transition-colors whitespace-nowrap shrink-0 shadow-sm"
              @click="openCreateModal"
            >
              <Plus class="h-4 w-4" aria-hidden="true" />
              <span>{{ $t('admin.categories.newCategory') }}</span>
            </button>
          </div>
        </div>
      </template>

      <!-- Nav Rail (Category List) -->
      <template #nav>
        <div class="flex flex-col gap-3 h-full">
          <!-- Search & Filter bar -->
          <div class="space-y-2">
            <div class="relative">
              <input
                v-model="searchInput"
                type="text"
                placeholder="Search categories..."
                class="w-full rounded-xl border border-slate-300 dark:border-white/10 bg-white dark:bg-canvas-subtle/50 pl-9 pr-3 py-2 text-sm text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500/20"
              />
              <Search class="absolute left-3 top-2.5 h-4 w-4 text-slate-400" aria-hidden="true" />
            </div>

            <!-- Status Filter Pills -->
            <div class="flex gap-1.5" role="group" aria-label="Status filter">
              <button
                type="button"
                :aria-pressed="selectedStatus === 'all'"
                class="px-2.5 py-1 text-xs rounded-lg font-semibold transition-colors whitespace-nowrap shrink-0"
                :class="selectedStatus === 'all' ? 'bg-brand-500 text-white' : 'bg-slate-200 dark:bg-white/10 text-slate-600 dark:text-slate-400'"
                @click="selectedStatus = 'all'"
              >
                All
              </button>
              <button
                type="button"
                :aria-pressed="selectedStatus === 'active'"
                class="px-2.5 py-1 text-xs rounded-lg font-semibold transition-colors whitespace-nowrap shrink-0"
                :class="selectedStatus === 'active' ? 'bg-emerald-600 text-white' : 'bg-slate-200 dark:bg-white/10 text-slate-600 dark:text-slate-400'"
                @click="selectedStatus = 'active'"
              >
                {{ $t('admin.categories.status.active') }}
              </button>
              <button
                type="button"
                :aria-pressed="selectedStatus === 'inactive'"
                class="px-2.5 py-1 text-xs rounded-lg font-semibold transition-colors whitespace-nowrap shrink-0"
                :class="selectedStatus === 'inactive' ? 'bg-slate-600 text-white' : 'bg-slate-200 dark:bg-white/10 text-slate-600 dark:text-slate-400'"
                @click="selectedStatus = 'inactive'"
              >
                {{ $t('admin.categories.status.inactive') }}
              </button>
            </div>
          </div>

          <!-- Category List -->
          <div ref="categoryListRef" class="flex-1 overflow-y-auto space-y-1.5 min-h-0">
            <div v-if="categoryStore.isLoading && categoryStore.categories.length === 0" class="flex justify-center py-8">
              <Loader2 class="h-6 w-6 animate-spin text-brand-500" />
            </div>

            <div v-else-if="filteredCategories.length === 0" class="text-center py-8 text-sm text-slate-500 dark:text-slate-400">
              {{ $t('admin.categories.empty') }}
            </div>

            <button
              v-for="cat in filteredCategories"
              :key="cat.id"
              type="button"
              :aria-pressed="selectedId === cat.id"
              class="w-full text-left rounded-xl p-3 border transition-colors select-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
              :class="selectedId === cat.id ? 'bg-brand-500/10 border-brand-500/30' : 'bg-white dark:bg-canvas-subtle/30 border-slate-200/80 dark:border-white/[0.06] hover:bg-slate-100 dark:hover:bg-white/[0.04]'"
              data-testid="category-list-item"
              @click="handleSelect(cat)"
            >
              <span class="flex items-center justify-between gap-2">
                <span class="font-semibold text-sm sm:text-base text-slate-900 dark:text-white truncate">
                  {{ categoryStore.getCategoryName(cat, locale) }}
                </span>
                <span class="flex items-center gap-1.5 whitespace-nowrap shrink-0">
                  <span class="rounded bg-slate-100 dark:bg-white/10 px-1.5 py-0.5 text-[11px] font-mono text-slate-600 dark:text-slate-300">
                    #{{ cat.sortOrder }}
                  </span>
                  <span
                    class="rounded-full px-2 py-0.5 text-[11px] font-semibold"
                    :class="cat.isActive ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400' : 'bg-slate-500/10 text-slate-500 dark:text-slate-400'"
                  >
                    {{ cat.isActive ? $t('admin.categories.status.active') : $t('admin.categories.status.inactive') }}
                  </span>
                </span>
              </span>

              <span class="mt-1 flex items-center justify-between gap-2 text-xs text-slate-500 dark:text-slate-400">
                <span class="font-mono text-[11px] text-brand-600 dark:text-brand-400 truncate">{{ cat.slug }}</span>
                <span v-if="cat.nameEn !== cat.nameVi" class="text-[11px] truncate max-w-[140px] text-slate-400">
                  {{ cat.nameEn }}
                </span>
              </span>
            </button>
          </div>
        </div>
      </template>

      <!-- Content Panel (Category Details) -->
      <template #content>
        <div v-if="selectedCategory" class="max-w-3xl space-y-6">
          <!-- Detail Header Card -->
          <div class="rounded-2xl border border-slate-200/80 dark:border-white/10 bg-white dark:bg-canvas-subtle/40 p-5 sm:p-6 shadow-sm">
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
              <div class="space-y-1 min-w-0">
                <div class="flex items-center gap-2.5 flex-wrap">
                  <h2 class="text-xl sm:text-2xl font-bold text-slate-900 dark:text-white truncate">
                    {{ categoryStore.getCategoryName(selectedCategory, locale) }}
                  </h2>
                  <span
                    class="rounded-full px-2.5 py-0.5 text-xs font-semibold"
                    :class="selectedCategory.isActive ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400' : 'bg-slate-500/10 text-slate-500 dark:text-slate-400'"
                  >
                    {{ selectedCategory.isActive ? $t('admin.categories.status.active') : $t('admin.categories.status.inactive') }}
                  </span>
                  <span class="rounded bg-slate-100 dark:bg-white/10 px-2 py-0.5 text-xs font-mono text-slate-600 dark:text-slate-300">
                    v{{ selectedCategory.version }}
                  </span>
                </div>
                <p class="font-mono text-sm text-brand-600 dark:text-brand-400">
                  {{ selectedCategory.slug }}
                </p>
              </div>

              <!-- Actions Toolbar -->
              <div class="flex items-center gap-2 flex-wrap">
                <button
                  type="button"
                  class="inline-flex items-center gap-1.5 rounded-xl border border-slate-300 dark:border-white/10 bg-white dark:bg-canvas-subtle px-3.5 py-2 text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-white/[0.04] transition-colors whitespace-nowrap shrink-0 shadow-sm"
                  @click="openEditModal"
                >
                  <Pencil class="h-4 w-4" aria-hidden="true" />
                  <span>{{ $t('admin.categories.actions.edit') }}</span>
                </button>

                <!-- Deactivate or Activate -->
                <button
                  v-if="selectedCategory.isActive"
                  type="button"
                  class="inline-flex items-center gap-1.5 rounded-xl border border-amber-300 dark:border-amber-500/30 bg-amber-50 dark:bg-amber-500/10 px-3.5 py-2 text-xs sm:text-sm font-semibold text-amber-700 dark:text-amber-300 hover:bg-amber-100 dark:hover:bg-amber-500/20 transition-colors whitespace-nowrap shrink-0"
                  :disabled="isActionSubmitting"
                  @click="openDeactivateModal"
                >
                  <Power class="h-4 w-4" aria-hidden="true" />
                  <span>{{ $t('admin.categories.actions.deactivate') }}</span>
                </button>
                <button
                  v-else
                  type="button"
                  class="inline-flex items-center gap-1.5 rounded-xl border border-emerald-300 dark:border-emerald-500/30 bg-emerald-50 dark:bg-emerald-500/10 px-3.5 py-2 text-xs sm:text-sm font-semibold text-emerald-700 dark:text-emerald-300 hover:bg-emerald-100 dark:hover:bg-emerald-500/20 transition-colors whitespace-nowrap shrink-0"
                  :disabled="isActionSubmitting"
                  @click="handleActivate"
                >
                  <CheckCircle2 class="h-4 w-4" aria-hidden="true" />
                  <span>{{ $t('admin.categories.actions.activate') }}</span>
                </button>

                <!-- Delete Category Button (Gated by canDelete) -->
                <button
                  type="button"
                  class="inline-flex items-center gap-1.5 rounded-xl border border-rose-300 dark:border-rose-500/30 bg-rose-50 dark:bg-rose-500/10 px-3.5 py-2 text-xs sm:text-sm font-semibold text-rose-700 dark:text-rose-300 hover:bg-rose-100 dark:hover:bg-rose-500/20 transition-colors disabled:opacity-40 disabled:cursor-not-allowed whitespace-nowrap shrink-0"
                  :disabled="selectedDetail?.canDelete !== true || isLoadingDetail || isActionSubmitting"
                  :title="selectedDetail?.canDelete === false ? $t('admin.categories.modals.deleteWarning') : undefined"
                  @click="openDeleteModal"
                >
                  <Trash2 class="h-4 w-4" aria-hidden="true" />
                  <span>{{ $t('admin.categories.actions.delete') }}</span>
                </button>
              </div>
            </div>
          </div>

          <div v-if="detailError" role="alert" class="rounded-xl border border-rose-500/20 p-4 text-sm sm:text-base text-rose-700 dark:text-rose-300">
            <p>{{ detailError }}</p>
            <button type="button" class="mt-2 font-semibold whitespace-nowrap shrink-0" @click="handleSelect(selectedCategory)">
              {{ $t('categories.retry') }}
            </button>
          </div>
          <!-- Metadata Properties Card -->
          <div class="rounded-2xl border border-slate-200/80 dark:border-white/10 bg-white dark:bg-canvas-subtle/40 p-5 sm:p-6 shadow-sm space-y-4">
            <h3 class="text-sm font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              Metadata Properties
            </h3>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div class="space-y-1">
                <span class="text-xs text-slate-500 dark:text-slate-400">{{ $t('admin.categories.fields.nameEn') }}</span>
                <p class="font-semibold text-sm text-slate-900 dark:text-white">{{ selectedCategory.nameEn }}</p>
              </div>

              <div class="space-y-1">
                <span class="text-xs text-slate-500 dark:text-slate-400">{{ $t('admin.categories.fields.nameVi') }}</span>
                <p class="font-semibold text-sm text-slate-900 dark:text-white">{{ selectedCategory.nameVi }}</p>
              </div>

              <div class="space-y-1">
                <span class="text-xs text-slate-500 dark:text-slate-400">{{ $t('admin.categories.fields.slug') }}</span>
                <p class="font-mono text-sm text-brand-600 dark:text-brand-400">{{ selectedCategory.slug }}</p>
                <p class="text-[11px] text-slate-400">Immutable identifier across all versions and references.</p>
              </div>

              <div class="space-y-1">
                <span class="text-xs text-slate-500 dark:text-slate-400">{{ $t('admin.categories.fields.sortOrder') }}</span>
                <p class="font-semibold text-sm text-slate-900 dark:text-white">{{ selectedCategory.sortOrder }}</p>
              </div>

              <div class="space-y-1">
                <span class="text-xs text-slate-500 dark:text-slate-400">UUID</span>
                <p class="font-mono text-xs text-slate-600 dark:text-slate-300 break-all">{{ selectedCategory.id }}</p>
              </div>

              <div v-if="selectedDetail?.usageCount !== undefined" class="space-y-1">
                <span class="text-xs text-slate-500 dark:text-slate-400">Referenced Documents</span>
                <p class="font-semibold text-sm text-slate-900 dark:text-white">{{ selectedDetail.usageCount }}</p>
              </div>
            </div>

            <!-- In-use notice if cannot delete -->
            <div
              v-if="selectedDetail?.canDelete === false"
              class="rounded-xl border border-amber-500/20 bg-amber-500/5 p-3.5 text-xs text-amber-800 dark:text-amber-300 flex items-start gap-2.5"
            >
              <Info class="h-4 w-4 shrink-0 mt-0.5 text-amber-600 dark:text-amber-400" aria-hidden="true" />
              <p>{{ $t('admin.categories.modals.deleteWarning') }}</p>
            </div>
          </div>
        </div>

        <!-- Empty state when no selection -->
        <div v-else class="flex flex-col items-center justify-center py-16 text-center text-slate-400">
          <FolderTree class="h-12 w-12 text-slate-300 dark:text-slate-600 mb-3" aria-hidden="true" />
          <p class="text-sm font-semibold">Select a category to view details and administration actions.</p>
        </div>
      </template>
    </MasterDetailLayout>

    <!-- Create / Edit Modal -->
    <AdminCategoryModal
      :open="isCategoryModalOpen"
      :category="categoryToEdit"
      :initiating-button="categoryModalTrigger"
      @close="isCategoryModalOpen = false"
      @success="onCategoryModalSuccess"
    />

    <!-- Deactivate Confirmation Dialog -->
    <AppModal
      :open="isDeactivateModalOpen"
      :title="$t('admin.categories.modals.deactivateTitle')"
      max-width="max-w-md"
      @close="isDeactivateModalOpen = false"
    >
      <div class="space-y-4">
        <p class="text-sm text-slate-700 dark:text-slate-300">
          {{ $t('admin.categories.modals.deactivateConfirm', { name: selectedCategory?.nameEn || '' }) }}
        </p>

        <div v-if="actionError" class="rounded-xl border border-rose-500/30 bg-rose-500/10 p-3 text-xs text-rose-800 dark:text-rose-200">
          {{ actionError }}
        </div>
      </div>

      <template #footer>
        <button
          type="button"
          class="rounded-xl border border-slate-300 dark:border-white/10 px-4 py-2 text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors whitespace-nowrap shrink-0"
          :disabled="isActionSubmitting"
          @click="isDeactivateModalOpen = false"
        >
          {{ $t('admin.categories.cancel') }}
        </button>
        <button
          type="button"
          class="flex items-center gap-1.5 rounded-xl bg-amber-600 px-4 py-2 text-xs sm:text-sm font-semibold text-white hover:bg-amber-500 transition-colors disabled:opacity-50 whitespace-nowrap shrink-0 shadow-sm"
          :disabled="isActionSubmitting"
          @click="handleConfirmDeactivate"
        >
          <Loader2 v-if="isActionSubmitting" class="h-4 w-4 animate-spin shrink-0" aria-hidden="true" />
          <span>{{ $t('admin.categories.actions.deactivate') }}</span>
        </button>
      </template>
    </AppModal>

    <!-- Delete Confirmation Dialog -->
    <AppModal
      :open="isDeleteModalOpen"
      :title="$t('admin.categories.modals.deleteTitle')"
      max-width="max-w-md"
      @close="isDeleteModalOpen = false"
    >
      <div class="space-y-4">
        <p class="text-sm text-slate-700 dark:text-slate-300">
          {{ $t('admin.categories.modals.deleteConfirm', { name: selectedCategory?.nameEn || '' }) }}
        </p>

        <div class="rounded-xl border border-amber-500/30 bg-amber-500/10 p-3 text-xs text-amber-800 dark:text-amber-200">
          {{ $t('admin.categories.modals.deleteWarning') }}
        </div>

        <div v-if="actionError" class="rounded-xl border border-rose-500/30 bg-rose-500/10 p-3 text-xs text-rose-800 dark:text-rose-200">
          {{ actionError }}
        </div>
      </div>

      <template #footer>
        <button
          type="button"
          class="rounded-xl border border-slate-300 dark:border-white/10 px-4 py-2 text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors whitespace-nowrap shrink-0"
          :disabled="isActionSubmitting"
          @click="isDeleteModalOpen = false"
        >
          {{ $t('admin.categories.cancel') }}
        </button>
        <button
          type="button"
          class="flex items-center gap-1.5 rounded-xl bg-rose-600 px-4 py-2 text-xs sm:text-sm font-semibold text-white hover:bg-rose-500 transition-colors disabled:opacity-50 whitespace-nowrap shrink-0 shadow-sm"
          :disabled="isActionSubmitting"
          @click="handleConfirmDelete"
        >
          <Loader2 v-if="isActionSubmitting" class="h-4 w-4 animate-spin shrink-0" aria-hidden="true" />
          <span>{{ $t('admin.categories.actions.delete') }}</span>
        </button>
      </template>
    </AppModal>
  </div>
</template>
