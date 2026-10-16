import { defineStore } from "pinia";
import { ref, watch } from "vue";
import { useApiClient } from "~/composables/useApiClient";
import { useAuthStore } from "~/stores/useAuthStore";
import type { components } from "~/types/api.generated";

export type LearnerCatalogDocument = components["schemas"]["LearnerCatalogDocumentDto"];
export type CatalogTocItem = components["schemas"]["CatalogTocItemDto"];
export type CatalogFirstSlicePreview = components["schemas"]["CatalogFirstSlicePreviewDto"];
export type LearnerCatalogDetail = components["schemas"]["LearnerCatalogDetailDto"];
export type CatalogAdoptionResult = components["schemas"]["CatalogAdoptionResult"];
export type AdminCatalogDocument = components["schemas"]["AdminCatalogDocumentDto"];
export type AdminCatalogSlice = components["schemas"]["AdminCatalogSliceDto"];
export type AdminPreviewUrlResult = components["schemas"]["AdminPreviewCatalogUrlResponse"];

function extractErrorMessage(err: unknown, fallback: string): string {
  if (err && typeof err === "object" && "message" in err) {
    const msg = err.message;
    if (typeof msg === "string") {
      return msg;
    }
  }
  return fallback;
}

function isForbiddenError(err: unknown): boolean {
  if (err && typeof err === "object" && "status" in err) {
    return err.status === 403;
  }
  return false;
}

export const useCatalogStore = defineStore("catalog", () => {
  const apiClient = useApiClient();
  const auth = useAuthStore();
  let adminDataGeneration = 0;

  // Capability
  const canManageCatalog = ref(false);
  const isCheckingCapability = ref(false);

  // Learner Discovery State
  const documents = ref<LearnerCatalogDocument[]>([]);
  const selectedDocument = ref<LearnerCatalogDetail | null>(null);
  const totalCount = ref(0);
  const page = ref(1);
  const pageSize = ref(12);
  const search = ref("");
  const categoryId = ref<string | null>(null);
  const language = ref<string | null>(null);
  const sourceType = ref<components["schemas"]["SourceType"] | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  // Race condition protection for discovery
  let activeDiscoveryRequestSeq = 0;

  // Adoption state with race protection and per-document retry tracking
  const isAdopting = ref<Record<string, boolean>>({});
  const adoptionError = ref<Record<string, string | null>>({});

  // Admin Catalog State
  const adminDocuments = ref<AdminCatalogDocument[]>([]);
  const adminTotalCount = ref(0);
  const adminPage = ref(1);
  const adminPageSize = ref(12);
  const adminSearch = ref("");
  const adminVisibility = ref<string | null>(null);
  const adminStatus = ref<string | null>(null);
  const adminCategoryId = ref<string | null>(null);
  const isAdminLoading = ref(false);
  const adminError = ref<string | null>(null);
  const selectedAdminDoc = ref<AdminCatalogDocument | null>(null);
  const selectedAdminSlice = ref<AdminCatalogSlice | null>(null);

  // Race condition protection for admin
  let activeAdminRequestSeq = 0;

  // Polling handle
  let pollingTimer: ReturnType<typeof setInterval> | null = null;

  async function fetchCapabilities(): Promise<boolean> {
    const generation = adminDataGeneration;
    isCheckingCapability.value = true;
    try {
      const response = await apiClient.get<{ canManageCatalog: boolean }>(
        "/api/v1/catalog/capabilities"
      );
      if (generation !== adminDataGeneration) return false;
      canManageCatalog.value = response?.canManageCatalog ?? false;
      return canManageCatalog.value;
    } catch {
      if (generation === adminDataGeneration) canManageCatalog.value = false;
      return false;
    } finally {
      if (generation === adminDataGeneration) isCheckingCapability.value = false;
    }
  }

  async function fetchPublishedDocuments(params?: {
    search?: string;
    categoryId?: string | null;
    language?: string | null;
    sourceType?: components["schemas"]["SourceType"] | null;
    page?: number;
    pageSize?: number;
  }) {
    if (params?.page !== undefined) page.value = params.page;
    if (params?.pageSize !== undefined) pageSize.value = params.pageSize;
    if (params?.search !== undefined) search.value = params.search;
    if (params?.categoryId !== undefined) categoryId.value = params.categoryId;
    if (params?.language !== undefined) language.value = params.language;
    if (params?.sourceType !== undefined) sourceType.value = params.sourceType;
    isLoading.value = true;
    error.value = null;

    const requestSeq = ++activeDiscoveryRequestSeq;

    try {
      const queryParams = new URLSearchParams();
      queryParams.set("page", page.value.toString());
      queryParams.set("pageSize", pageSize.value.toString());
      if (search.value.trim()) queryParams.set("search", search.value.trim());
      if (categoryId.value) queryParams.set("categoryId", categoryId.value);
      if (language.value) queryParams.set("language", language.value);
      if (sourceType.value !== null && sourceType.value !== undefined) queryParams.set("sourceType", sourceType.value.toString());
      const response = await apiClient.get<{
        items: LearnerCatalogDocument[];
        totalCount: number;
        page: number;
        pageSize: number;
      }>(`/api/v1/catalog/documents?${queryParams.toString()}`);

      // Discard stale responses if a newer request was dispatched
      if (requestSeq !== activeDiscoveryRequestSeq) {
        return;
      }

      documents.value = response.items || [];
      totalCount.value = response.totalCount || 0;
    } catch (err: unknown) {
      if (requestSeq === activeDiscoveryRequestSeq) {
        error.value = extractErrorMessage(err, "Failed to load discovery catalog");
      }
    } finally {
      if (requestSeq === activeDiscoveryRequestSeq) {
        isLoading.value = false;
      }
    }
  }

  async function fetchPublishedDocumentById(id: string): Promise<LearnerCatalogDetail | null> {
    isLoading.value = true;
    error.value = null;
    try {
      const detail = await apiClient.get<LearnerCatalogDetail>(`/api/v1/catalog/documents/${id}`);
      selectedDocument.value = detail;
      return detail;
    } catch (err: unknown) {
      error.value = extractErrorMessage(err, "Failed to fetch document preview");
      selectedDocument.value = null;
      return null;
    } finally {
      isLoading.value = false;
    }
  }

  async function adoptDocument(id: string): Promise<CatalogAdoptionResult | null> {
    // Duplicate submit protection
    if (isAdopting.value[id]) {
      return null;
    }

    isAdopting.value[id] = true;
    adoptionError.value[id] = null;

    try {
      const result = await apiClient.post<CatalogAdoptionResult>(
        `/api/v1/catalog/documents/${id}/adopt`,
        {}
      );

      if (result?.bookId) {
        documents.value = documents.value.map(document =>
          document.id === id ? { ...document, existingBookId: result.bookId } : document
        );
        if (selectedDocument.value?.id === id) {
          selectedDocument.value = { ...selectedDocument.value, existingBookId: result.bookId };
        }
      }

      return result;
    } catch (err: unknown) {
      adoptionError.value[id] = extractErrorMessage(err, "Adoption failed. Please retry.");
      throw err;
    } finally {
      isAdopting.value[id] = false;
    }
  }

  async function fetchAdminDocuments(params?: {
    search?: string;
    visibility?: string | null;
    status?: string | null;
    categoryId?: string | null;
    page?: number;
    pageSize?: number;
  }) {
    if (params?.page !== undefined) adminPage.value = params.page;
    if (params?.pageSize !== undefined) adminPageSize.value = params.pageSize;
    if (params?.search !== undefined) adminSearch.value = params.search;
    if (params?.visibility !== undefined) adminVisibility.value = params.visibility;
    if (params?.status !== undefined) adminStatus.value = params.status;
    if (params?.categoryId !== undefined) adminCategoryId.value = params.categoryId;

    isAdminLoading.value = true;
    adminError.value = null;

    const requestSeq = ++activeAdminRequestSeq;

    try {
      const queryParams = new URLSearchParams();
      queryParams.set("page", adminPage.value.toString());
      queryParams.set("pageSize", adminPageSize.value.toString());
      if (adminSearch.value.trim()) queryParams.set("search", adminSearch.value.trim());
      if (adminVisibility.value) queryParams.set("visibility", adminVisibility.value);
      if (adminStatus.value) queryParams.set("status", adminStatus.value);
      if (adminCategoryId.value) queryParams.set("categoryId", adminCategoryId.value);
      const response = await apiClient.get<{
        items: AdminCatalogDocument[];
        totalCount: number;
        page: number;
        pageSize: number;
      }>(`/api/v1/admin/catalog/documents?${queryParams.toString()}`);

      if (requestSeq !== activeAdminRequestSeq) {
        return;
      }

      adminDocuments.value = response.items || [];
      adminTotalCount.value = response.totalCount || 0;
    } catch (err: unknown) {
      if (requestSeq === activeAdminRequestSeq) {
        if (isForbiddenError(err)) {
          handleForbidden();
        } else {
          adminError.value = extractErrorMessage(err, "Failed to load admin documents");
        }
      }
    } finally {
      if (requestSeq === activeAdminRequestSeq) {
        isAdminLoading.value = false;
      }
    }
  }

  async function fetchAdminDocumentById(id: string): Promise<AdminCatalogDocument | null> {
    const generation = adminDataGeneration;
    isAdminLoading.value = true;
    adminError.value = null;
    try {
      const doc = await apiClient.get<AdminCatalogDocument>(`/api/v1/admin/catalog/documents/${id}`);
      if (generation !== adminDataGeneration) return null;
      selectedAdminDoc.value = doc;
      return doc;
    } catch (err: unknown) {
      if (generation !== adminDataGeneration) return null;
      if (isForbiddenError(err)) {
        handleForbidden();
      } else {
        adminError.value = extractErrorMessage(err, "Failed to load document details");
      }
      return null;
    } finally {
      if (generation === adminDataGeneration) isAdminLoading.value = false;
    }
  }

  async function fetchAdminSlice(docId: string, order: number): Promise<AdminCatalogSlice | null> {
    const generation = adminDataGeneration;
    isAdminLoading.value = true;
    adminError.value = null;
    try {
      const slice = await apiClient.get<AdminCatalogSlice>(
        `/api/v1/admin/catalog/documents/${docId}/slices/${order}`
      );
      if (generation !== adminDataGeneration) return null;
      selectedAdminSlice.value = slice;
      return slice;
    } catch (err: unknown) {
      if (generation !== adminDataGeneration) return null;
      if (isForbiddenError(err)) {
        handleForbidden();
      } else {
        adminError.value = extractErrorMessage(err, "Failed to load slice");
      }
      return null;
    } finally {
      if (generation === adminDataGeneration) isAdminLoading.value = false;
    }
  }

  async function previewUrl(url: string): Promise<AdminPreviewUrlResult> {
    return await apiClient.post<AdminPreviewUrlResult>("/api/v1/admin/catalog/preview-url", { url });
  }

  async function importUrl(payload: {
    url: string;
    title: string;
    description: string;
    categoryId: string;
    language: string;
    authorOrSourceUrl?: string;
    attributionText?: string;
  }): Promise<AdminCatalogDocument> {
    const doc = await apiClient.post<AdminCatalogDocument>(
      "/api/v1/admin/catalog/documents/import-url",
      payload
    );
    await fetchAdminDocuments();
    return doc;
  }

  async function uploadPdf(
    input: {
      file: File;
      title: string;
      description?: string;
      categoryId: string;
      language: string;
      authorOrSourceUrl?: string;
      attributionText?: string;
    }
  ): Promise<AdminCatalogDocument> {
    const body = new FormData();
    body.append("file", input.file);
    body.append("title", input.title);
    body.append("description", input.description || input.title);
    body.append("categoryId", input.categoryId);
    body.append("language", input.language);
    if (input.authorOrSourceUrl) body.append("authorOrSourceUrl", input.authorOrSourceUrl);
    if (input.attributionText) body.append("attributionText", input.attributionText);
    const doc = await apiClient.postRaw<AdminCatalogDocument>(
      "/api/v1/admin/catalog/documents/upload-pdf",
      body
    );
    await fetchAdminDocuments();
    return doc;
  }

  async function updateMetadata(
    id: string,
    payload: {
      title: string;
      description: string;
      categoryId: string;
      language: string;
      authorOrSourceUrl?: string;
      attributionText?: string;
      expectedVersion: number;
    }
  ): Promise<AdminCatalogDocument> {
    try {
      const updated = await apiClient.put<AdminCatalogDocument>(
        `/api/v1/admin/catalog/documents/${id}`,
        payload
      );
      selectedAdminDoc.value = updated;
      const index = adminDocuments.value.findIndex((d) => d.id === id);
      if (index !== -1) {
        adminDocuments.value[index] = updated;
      }
      return updated;
    } catch (err: unknown) {
      if (isForbiddenError(err)) handleForbidden();
      throw err;
    }
  }

  async function prepareDocument(
    id: string,
    payload?: { expectedRevision?: number; replacementFile?: File | null }
  ): Promise<AdminCatalogDocument> {
    try {
      const doc = await apiClient.post<AdminCatalogDocument>(
        `/api/v1/admin/catalog/documents/${id}/prepare`,
        { expectedRevision: payload?.expectedRevision }
      );
      selectedAdminDoc.value = doc;
      return doc;
    } catch (err: unknown) {
      if (isForbiddenError(err)) handleForbidden();
      throw err;
    }
  }

  async function reviewSlice(id: string, order: number, revision: number, notes?: string): Promise<AdminCatalogDocument> {
    try {
      const doc = await apiClient.post<AdminCatalogDocument>(
        `/api/v1/admin/catalog/documents/${id}/slices/${order}/review`,
        { expectedRevision: revision, reviewNotes: notes }
      );
      selectedAdminDoc.value = doc;
      return doc;
    } catch (err: unknown) {
      if (isForbiddenError(err)) handleForbidden();
      throw err;
    }
  }
  async function reviewDocument(id: string, payload?: { expectedRevision?: number; reviewNotes?: string }): Promise<AdminCatalogDocument> {
    const rev = payload?.expectedRevision ?? selectedAdminDoc.value?.revision ?? 1;
    return await reviewSlice(id, 1, rev, payload?.reviewNotes);
  }

  async function publishDocument(id: string, revision: number, notes?: string): Promise<AdminCatalogDocument> {
    try {
      const doc = await apiClient.post<AdminCatalogDocument>(
        `/api/v1/admin/catalog/documents/${id}/publish`,
        { expectedRevision: revision, reviewNotes: notes }
      );
      selectedAdminDoc.value = doc;
      const index = adminDocuments.value.findIndex((d) => d.id === id);
      if (index !== -1) {
        adminDocuments.value[index] = doc;
      }
      return doc;
    } catch (err: unknown) {
      if (isForbiddenError(err)) handleForbidden();
      throw err;
    }
  }

  async function hideDocument(id: string, expectedVersion?: number): Promise<AdminCatalogDocument> {
    try {
      const ver = expectedVersion ?? selectedAdminDoc.value?.concurrencyVersion ?? 1;
      const doc = await apiClient.post<AdminCatalogDocument>(
        `/api/v1/admin/catalog/documents/${id}/hide`,
        { expectedConcurrencyVersion: ver }
      );
      selectedAdminDoc.value = doc;
      const index = adminDocuments.value.findIndex((d) => d.id === id);
      if (index !== -1) {
        adminDocuments.value[index] = doc;
      }
      return doc;
    } catch (err: unknown) {
      if (isForbiddenError(err)) handleForbidden();
      throw err;
    }
  }

  function startStatusPolling(id: string, onUpdate?: (doc: AdminCatalogDocument) => void) {
    stopStatusPolling();
    pollingTimer = setInterval(async () => {
      const doc = await fetchAdminDocumentById(id);
      if (doc) {
        onUpdate?.(doc);
        const status = typeof doc.processingStatus === "string" ? doc.processingStatus : String(doc.processingStatus);
        if (status === "Ready" || status === "Failed" || status === "3" || status === "4") {
          stopStatusPolling();
        }
      }
    }, 2000);
  }

  function stopStatusPolling() {
    if (pollingTimer) {
      clearInterval(pollingTimer);
      pollingTimer = null;
    }
  }

  function handleForbidden() {
    cleanupAdmin();
    auth.invalidateAdminAccess();
  }

  function cleanupAdmin() {
    adminDataGeneration++;
    activeAdminRequestSeq++;
    isAdminLoading.value = false;
    isCheckingCapability.value = false;
    adminError.value = null;
    canManageCatalog.value = false;
    adminDocuments.value = [];
    selectedAdminDoc.value = null;
    selectedAdminSlice.value = null;
    adminTotalCount.value = 0;
    stopStatusPolling();
  }

  function cleanup() {
    activeDiscoveryRequestSeq++;
    cleanupAdmin();
    documents.value = [];
    selectedDocument.value = null;
    totalCount.value = 0;
    isAdopting.value = {};
    adoptionError.value = {};
    error.value = null;
    adminError.value = null;
  }

  watch([() => auth.user?.id, () => auth.sessionGeneration], cleanup, { flush: 'sync' });
  watch(() => auth.adminGeneration, cleanupAdmin, { flush: 'sync' });

  return {
    // Capability
    canManageCatalog,
    isCheckingCapability,
    fetchCapabilities,

    // Learner Discovery
    documents,
    selectedDocument,
    totalCount,
    page,
    pageSize,
    search,
    categoryId,
    language,
    sourceType,
    isLoading,
    error,
    isAdopting,
    adoptionError,
    fetchPublishedDocuments,
    fetchPublishedDocumentById,
    adoptDocument,

    // Admin
    adminDocuments,
    adminTotalCount,
    adminPage,
    adminPageSize,
    adminSearch,
    adminVisibility,
    adminStatus,
    adminCategoryId,
    isAdminLoading,
    adminError,
    selectedAdminDoc,
    selectedAdminDocument: selectedAdminDoc,
    selectedAdminSlice,
    fetchAdminDocuments,
    fetchAdminDocumentById,
    fetchAdminSlice,
    previewUrl,
    importUrl,
    uploadPdf,
    updateMetadata,
    prepareDocument,
    reviewSlice,
    reviewDocument,
    publishDocument,
    hideDocument,
    startStatusPolling,
    stopStatusPolling,
    cleanupAdmin,
    cleanup,
  };
});
