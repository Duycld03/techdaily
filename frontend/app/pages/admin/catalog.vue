<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, type Ref } from 'vue'
import { ShieldAlert, ArrowLeft, FileText } from 'lucide-vue-next'
import MasterDetailLayout from '~/components/layout/MasterDetailLayout.vue'
import AdminCatalogHeader from '~/components/catalog/admin/AdminCatalogHeader.vue'
import AdminCatalogNavRail from '~/components/catalog/admin/AdminCatalogNavRail.vue'
import AdminCatalogDraftForm from '~/components/catalog/admin/AdminCatalogDraftForm.vue'
import AdminCatalogDocumentDetail from '~/components/catalog/admin/AdminCatalogDocumentDetail.vue'
import AdminCatalogSliceModal from '~/components/catalog/admin/AdminCatalogSliceModal.vue'
import { useCatalogStore, type AdminCatalogSliceDto } from '~/stores/useCatalogStore'
import { useApiError } from '~/composables/useApiError'

const { t } = useI18n()
const catalogStore = useCatalogStore()
const toast = useToast()
const { formatError } = useApiError()

const isAuthorized = computed(() => catalogStore.canManageCatalog)
const isCheckingAuth = ref(true)
const isCreatingNewDraft = ref(false)
const selectedDocId = ref<string | null>(null)
const selectedSlice = ref<AdminCatalogSliceDto | null>(null)
const isSliceModalOpen = ref(false)

const search = ref('')
const visibilityFilter = ref('')
const statusFilter = ref('')

const isSavingMetadata = ref(false)
const isPreparingSlices = ref(false)
const isReviewing = ref(false)
const isPublishing = ref(false)
const isHiding = ref(false)
const isRetrying = ref(false)

const runAction = async (loading: Ref<boolean>, fn: () => Promise<void>) => {
  loading.value = true
  try { await fn() }
  catch (err: unknown) { toast.error(formatError(err)) }
  finally { loading.value = false }
}

onMounted(async () => {
  isCheckingAuth.value = true
  try {
    const canManage = await catalogStore.fetchCapabilities()
    if (!canManage) {
      isCheckingAuth.value = false
      return
    }
    await catalogStore.fetchAdminDocuments()
    if (catalogStore.adminDocuments.length > 0 && !selectedDocId.value) {
      await selectDocument(catalogStore.adminDocuments[0]?.id || '')
    } else if (catalogStore.adminDocuments.length === 0) {
      isCreatingNewDraft.value = true
    }
  } catch (err: unknown) {
    toast.error(formatError(err))
  } finally {
    isCheckingAuth.value = false
  }
})

onBeforeUnmount(() => catalogStore.stopStatusPolling())

async function selectDocument(id: string) {
  selectedDocId.value = id
  isCreatingNewDraft.value = false
  try {
    const doc = await catalogStore.fetchAdminDocumentById(id)
    if (doc && (doc.processingStatus === 'Processing' || doc.processingStatus === 'Pending')) {
      catalogStore.startStatusPolling(id)
    } else {
      catalogStore.stopStatusPolling()
    }
  } catch (err: unknown) {
    toast.error(formatError(err))
  }
}

async function handleFilterChange() {
  await catalogStore.fetchAdminDocuments({
    search: search.value || undefined,
    visibility: visibilityFilter.value || null,
    status: statusFilter.value || null,
    page: 1
  })
}

async function handleSaveMetadata(payload: any) {
  if (!catalogStore.selectedAdminDocument) return
  await runAction(isSavingMetadata, async () => {
    await catalogStore.updateMetadata(catalogStore.selectedAdminDocument!.id, {
      ...payload,
      expectedConcurrencyVersion: catalogStore.selectedAdminDocument!.concurrencyVersion
    })
    toast.success(t('admin_catalog.toasts.metadata_saved'))
    await selectDocument(catalogStore.selectedAdminDocument!.id)
  })
}

async function handlePrepareSlices() {
  if (!catalogStore.selectedAdminDocument) return
  await runAction(isPreparingSlices, async () => {
    await catalogStore.prepareDocument(catalogStore.selectedAdminDocument!.id, {
      expectedRevision: catalogStore.selectedAdminDocument!.revision
    })
    toast.success(t('admin_catalog.toasts.preparation_initiated'))
    selectDocument(catalogStore.selectedAdminDocument!.id)
  })
}

async function handleReviewConfirm() {
  if (!catalogStore.selectedAdminDocument) return
  await runAction(isReviewing, async () => {
    await catalogStore.reviewDocument(catalogStore.selectedAdminDocument!.id, {
      expectedRevision: catalogStore.selectedAdminDocument!.revision,
      reviewNotes: 'Verified source content, layout integrity and initial slice summaries.'
    })
    toast.success(t('admin_catalog.toasts.review_recorded'))
    await selectDocument(catalogStore.selectedAdminDocument!.id)
  })
}

async function handlePublish() {
  if (!catalogStore.selectedAdminDocument) return
  await runAction(isPublishing, async () => {
    await catalogStore.publishDocument(catalogStore.selectedAdminDocument!.id, catalogStore.selectedAdminDocument!.revision)
    toast.success(t('admin_catalog.toasts.published_success'))
    await selectDocument(catalogStore.selectedAdminDocument!.id)
    await catalogStore.fetchAdminDocuments()
  })
}

async function handleHide() {
  if (!catalogStore.selectedAdminDocument) return
  await runAction(isHiding, async () => {
    await catalogStore.hideDocument(catalogStore.selectedAdminDocument!.id)
    toast.success(t('admin_catalog.toasts.hidden_success'))
    await selectDocument(catalogStore.selectedAdminDocument!.id)
    await catalogStore.fetchAdminDocuments()
  })
}

async function handleRetry(file?: File) {
  if (!catalogStore.selectedAdminDocument) return
  await runAction(isRetrying, async () => {
    await catalogStore.prepareDocument(catalogStore.selectedAdminDocument!.id, {
      expectedRevision: catalogStore.selectedAdminDocument!.revision,
      replacementFile: file
    })
    toast.success(t('admin_catalog.toasts.retry_started'))
    selectDocument(catalogStore.selectedAdminDocument!.id)
  })
}

async function inspectSlice(order: number) {
  if (!catalogStore.selectedAdminDocument) return
  try {
    const slice = await catalogStore.fetchAdminSlice(catalogStore.selectedAdminDocument.id, order)
    selectedSlice.value = slice
    isSliceModalOpen.value = true
  } catch (err: unknown) {
    toast.error(formatError(err))
  }
}

async function handleDraftCreated(id: string) {
  await catalogStore.fetchAdminDocuments()
  selectDocument(id)
}
</script>

<template>
  <!-- Loading Authorization Check -->
  <div v-if="isCheckingAuth" class="flex flex-col items-center justify-center min-h-[60vh] space-y-3">
    <div class="w-8 h-8 rounded-full border-2 border-brand-500 border-t-transparent animate-spin"></div>
    <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-medium">
      {{ $t('admin_catalog.verifying_permissions') }}
    </p>
  </div>

  <!-- Access Denied State for Authenticated Non-Administrators -->
  <div
    v-else-if="!isAuthorized"
    class="min-h-[calc(100vh-3.5rem)] flex items-center justify-center p-4 bg-slate-50 dark:bg-canvas"
  >
    <div class="max-w-lg w-full glass-card p-8 sm:p-10 rounded-3xl border border-slate-200/90 dark:border-white/[0.08] shadow-xl text-center space-y-5">
      <div class="w-16 h-16 rounded-2xl bg-rose-500/10 text-rose-600 dark:text-rose-400 border border-rose-500/20 flex items-center justify-center mx-auto shadow-sm">
        <ShieldAlert class="w-8 h-8" :stroke-width="1.5" />
      </div>

      <div class="space-y-2">
        <h2 class="text-xl sm:text-2xl font-black text-slate-900 dark:text-white">
          {{ $t('admin_catalog.access_denied_title') }}
        </h2>
        <p class="text-xs sm:text-sm text-slate-600 dark:text-slate-400 leading-relaxed">
          {{ $t('admin_catalog.access_denied_desc') }}
        </p>
      </div>

      <div class="pt-3">
        <NuxtLink
          to="/library"
          class="inline-flex items-center gap-2 px-6 py-2.5 rounded-xl bg-slate-900 hover:bg-slate-800 dark:bg-white dark:hover:bg-slate-100 text-white dark:text-slate-900 font-semibold text-xs sm:text-sm shadow-md transition-all active:scale-95 whitespace-nowrap shrink-0"
        >
          <ArrowLeft class="w-4 h-4" />
          <span>{{ $t('admin_catalog.back_to_library') }}</span>
        </NuxtLink>
      </div>
    </div>
  </div>

  <!-- MasterDetail Workspace for Authorized Administrators -->
  <div v-else class="h-[calc(100vh-3.5rem)] sm:h-[calc(100vh-3.75rem)] flex flex-col overflow-hidden bg-slate-50 dark:bg-canvas">
    <MasterDetailLayout flush class="flex-1 min-h-0">
      <!-- Header Slot -->
      <template #header>
        <AdminCatalogHeader
          :is-loading="catalogStore.isAdminLoading"
          @refresh="catalogStore.fetchAdminDocuments()"
          @new-draft="isCreatingNewDraft = true; selectedDocId = null"
        />
      </template>

      <!-- Nav Rail Slot -->
      <template #nav>
        <AdminCatalogNavRail
          :documents="catalogStore.adminDocuments"
          :selected-doc-id="selectedDocId"
          :is-creating-new-draft="isCreatingNewDraft"
          :is-loading="catalogStore.isAdminLoading"
          v-model:search="search"
          v-model:visibility-filter="visibilityFilter"
          v-model:status-filter="statusFilter"
          @filter-change="handleFilterChange"
          @select-document="selectDocument"
        />
      </template>

      <!-- Content Workspace Slot -->
      <template #content>
        <!-- View 1: Create New Draft Form -->
        <AdminCatalogDraftForm
          v-if="isCreatingNewDraft"
          @cancel="isCreatingNewDraft = false"
          @created="handleDraftCreated"
        />

        <!-- View 2: Document Inspection & Management Workspace -->
        <AdminCatalogDocumentDetail
          v-else-if="catalogStore.selectedAdminDocument"
          :document="catalogStore.selectedAdminDocument"
          :is-saving-metadata="isSavingMetadata"
          :is-preparing-slices="isPreparingSlices"
          :is-reviewing="isReviewing"
          :is-publishing="isPublishing"
          :is-hiding="isHiding"
          :is-retrying="isRetrying"
          @save-metadata="handleSaveMetadata"
          @prepare-slices="handlePrepareSlices"
          @confirm-review="handleReviewConfirm"
          @publish="handlePublish"
          @hide="handleHide"
          @retry-processing="handleRetry"
          @inspect-slice="inspectSlice"
        />

        <!-- Empty Selection View -->
        <div v-else class="flex flex-col items-center justify-center min-h-[40vh] text-center space-y-3">
          <FileText class="w-12 h-12 text-slate-300 dark:text-slate-600" />
          <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400">
            {{ $t('admin_catalog.document.empty_selection_prompt') }}
          </p>
        </div>
      </template>
    </MasterDetailLayout>

    <!-- Slice Inspection Modal -->
    <AdminCatalogSliceModal
      :open="isSliceModalOpen"
      :slice="selectedSlice"
      @close="isSliceModalOpen = false"
    />
  </div>
</template>
