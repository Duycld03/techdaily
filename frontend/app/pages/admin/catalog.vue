<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, type Ref } from 'vue'
import { ShieldAlert, ArrowLeft, FileText } from 'lucide-vue-next'
import MasterDetailLayout from '~/components/layout/MasterDetailLayout.vue'
import AdminCatalogHeader from '~/components/catalog/admin/AdminCatalogHeader.vue'
import AdminCatalogNavRail from '~/components/catalog/admin/AdminCatalogNavRail.vue'
import AdminCatalogDraftForm from '~/components/catalog/admin/AdminCatalogDraftForm.vue'
import AdminCatalogDocumentDetail from '~/components/catalog/admin/AdminCatalogDocumentDetail.vue'
import AdminCatalogSliceModal from '~/components/catalog/admin/AdminCatalogSliceModal.vue'
import { useCatalogStore, type AdminCatalogSlice } from '~/stores/useCatalogStore'
import { useApiError } from '~/composables/useApiError'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
import { useCategoryStore } from '~/stores/useCategoryStore'

const { t } = useI18n()
const catalogStore = useCatalogStore()
const categoryStore = useCategoryStore()
const toast = useToast()
const { formatError } = useApiError()

const access = useAdminAccessStore()
const isCreatingNewDraft = ref(false)
const selectedDocId = ref<string | null>(null)
const selectedSlice = ref<AdminCatalogSlice | null>(null)
const isSliceModalOpen = ref(false)

const search = ref('')
const visibilityFilter = ref('')
const statusFilter = ref('')

const isSavingMetadata = ref(false)
const isPreparingSlices = ref(false)
const isPublishing = ref(false)
const isHiding = ref(false)
const isRetrying = ref(false)

const runAction = async (loading: Ref<boolean>, fn: () => Promise<void>) => {
  loading.value = true
  try { await fn() }
  catch (err: unknown) {
    if (err && typeof err === 'object' && 'code' in err && typeof err.code === 'string' && err.code.startsWith('Category.')) {
      await categoryStore.fetchCategories(true)
    }
    toast.error(formatError(err))
  }
  finally { loading.value = false }
}

onMounted(async () => {
  if (!access.isAuthorized) return
  void categoryStore.ensureCategories()
  try {
    await catalogStore.fetchAdminDocuments()
    if (catalogStore.adminDocuments.length > 0 && !selectedDocId.value) {
      await selectDocument(catalogStore.adminDocuments[0]?.id || '')
    } else if (catalogStore.adminDocuments.length === 0) {
      isCreatingNewDraft.value = true
    }
  } catch (err: unknown) { toast.error(formatError(err)) }
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
      title: payload.title,
      description: catalogStore.selectedAdminDocument!.description || payload.title,
      categoryId: payload.categoryId,
      language: payload.language,
      authorOrSourceUrl: payload.authorOrSourceUrl,
      attributionText: payload.attributionText,
      expectedVersion: catalogStore.selectedAdminDocument!.revision
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
  <div v-if="access.isAuthorized" class="h-[calc(100vh-3.5rem)] sm:h-[calc(100vh-3.75rem)] flex flex-col overflow-hidden bg-slate-50 dark:bg-canvas">
    <!-- MasterDetail Workspace for Authorized Administrators -->
    <MasterDetailLayout flush nav-class="md:w-84 lg:w-96" class="flex-1 min-h-0" :nav-label="$t('admin.nav.documents')" :content-label="$t('admin_catalog.title')">
      <!-- Header Slot -->
      <template #header>
        <AdminCatalogHeader
          :is-loading="catalogStore.isAdminLoading"
          :is-creating-draft="isCreatingNewDraft"
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
          :is-publishing="isPublishing"
          :is-hiding="isHiding"
          :is-retrying="isRetrying"
          @save-metadata="handleSaveMetadata"
          @prepare-slices="handlePrepareSlices"
          @publish="handlePublish"
          @hide="handleHide"
          @retry-processing="handleRetry"
          @inspect-slice="inspectSlice"
        />

        <!-- Empty Selection View -->
        <div v-else class="flex flex-col items-center justify-center min-h-[40vh] text-center space-y-3">
          <FileText class="w-12 h-12 text-slate-300 dark:text-slate-600" />
          <p class="text-sm sm:text-base text-slate-500 dark:text-slate-400">
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
