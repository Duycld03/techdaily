import { defineStore } from "pinia";
import { ref } from "vue";
import { useApiClient } from "~/composables/useApiClient";

export interface LearnerCatalogDocument {
  id: string;
  title: string;
  description: string;
  category: number;
  language: string;
  sourceType: number;
  authorOrSourceUrl?: string;
  attributionText: string;
  totalSlices: number;
  estimatedReadMinutes: number;
  publishedAt?: string;
  existingBookId?: string | null;
}

export interface CatalogTocItem {
  sliceOrder: number;
  chapterTitle: string;
  estimatedReadMinutes: number;
}

export interface CatalogFirstSlicePreview {
  sliceOrder: number;
  chapterTitle: string;
  originalTextMarkdown: string;
  summaryMarkdown: string;
  keyTakeaways: string[];
  estimatedReadMinutes: number;
}

export interface LearnerCatalogDetail extends LearnerCatalogDocument {
  tableOfContents: CatalogTocItem[];
  firstSlicePreview?: CatalogFirstSlicePreview | null;
}

export interface CatalogAdoptionResult {
  bookId: string;
  alreadyAdded: boolean;
}

export interface AdminCatalogDocument {
  id: string;
  title: string;
  description: string;
  category: number;
  language: string;
  sourceType: number;
  authorOrSourceUrl?: string;
  attributionText: string;
  permissionBasis: string;
  permissionEvidence: string;
  confirmedDistributionRights: boolean;
  confirmedAiProcessingRights: boolean;
  visibility: "Draft" | "Published" | "Hidden" | number;
  processingStatus: "Pending" | "Processing" | "Ready" | "Failed" | number;
  totalSlices: number;
  estimatedReadMinutes: number;
  revision: number;
  concurrencyVersion: number;
  processingGeneration: number;
  processingStatusMessage?: string;
  processingErrorMessage?: string;
  isEligibleForPublication: boolean;
  unmetPublicationRequirements: string[];
  createdAt: string;
  updatedAt?: string;
}

export interface AdminCatalogSlice {
  id: string;
  catalogDocumentId: string;
  sliceOrder: number;
  chapterTitle: string;
  originalTextMarkdown: string;
  summaryMarkdown: string;
  keyTakeaways: string[];
  language: string;
  estimatedReadMinutes: number;
  isAiFormatted: boolean;
}

export interface AdminPreviewUrlResult {
  title: string;
  markdownContent: string;
  sourceUrl: string;
  estimatedWordCount: number;
  isPdfDetected: boolean;
}

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
  const category = ref<number | null>(null);
  const language = ref<string | null>(null);
  const sourceType = ref<number | null>(null);
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
  const adminCategory = ref<number | null>(null);
  const isAdminLoading = ref(false);
  const adminError = ref<string | null>(null);
  const selectedAdminDoc = ref<AdminCatalogDocument | null>(null);
  const selectedAdminSlice = ref<AdminCatalogSlice | null>(null);

  // Race condition protection for admin
  let activeAdminRequestSeq = 0;

  // Polling handle
  let pollingTimer: ReturnType<typeof setInterval> | null = null;

  async function fetchCapabilities(): Promise<boolean> {
    isCheckingCapability.value = true;
    try {
      const response = await apiClient.get<{ canManageCatalog: boolean }>(
        "/api/v1/catalog/capabilities"
      );
      canManageCatalog.value = response?.canManageCatalog ?? false;
      return canManageCatalog.value;
    } catch {
      canManageCatalog.value = false;
      return false;
    } finally {
      isCheckingCapability.value = false;
    }
  }

  async function fetchPublishedDocuments(params?: {
    search?: string;
    category?: number | null;
    language?: string | null;
    sourceType?: number | null;
    page?: number;
    pageSize?: number;
  }) {
    if (params?.page !== undefined) page.value = params.page;
    if (params?.pageSize !== undefined) pageSize.value = params.pageSize;
    if (params?.search !== undefined) search.value = params.search;
    if (params?.category !== undefined) category.value = params.category;
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
      if (category.value !== null && category.value !== undefined) queryParams.set("category", category.value.toString());
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

      // Update local state: mark adopted
      if (result?.bookId || result?.alreadyAdded) {
        const item = documents.value.find((d) => d.id === id);
        if (item) {
          if (result.bookId) item.existingBookId = result.bookId;
          item.alreadyAdded = true;
        }
        if (selectedDocument.value && selectedDocument.value.id === id) {
          if (result.bookId) selectedDocument.value.existingBookId = result.bookId;
          selectedDocument.value.alreadyAdded = true;
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
    category?: number | null;
    page?: number;
    pageSize?: number;
  }) {
    if (params?.page !== undefined) adminPage.value = params.page;
    if (params?.pageSize !== undefined) adminPageSize.value = params.pageSize;
    if (params?.search !== undefined) adminSearch.value = params.search;
    if (params?.visibility !== undefined) adminVisibility.value = params.visibility;
    if (params?.status !== undefined) adminStatus.value = params.status;
    if (params?.category !== undefined) adminCategory.value = params.category;

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
      if (adminCategory.value !== null && adminCategory.value !== undefined) {
        queryParams.set("category", adminCategory.value.toString());
      }

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
          cleanupAdmin();
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
    isAdminLoading.value = true;
    adminError.value = null;
    try {
      const doc = await apiClient.get<AdminCatalogDocument>(`/api/v1/admin/catalog/documents/${id}`);
      selectedAdminDoc.value = doc;
      return doc;
    } catch (err: unknown) {
      if (isForbiddenError(err)) {
        cleanupAdmin();
      } else {
        adminError.value = extractErrorMessage(err, "Failed to load document details");
      }
      return null;
    } finally {
      isAdminLoading.value = false;
    }
  }

  async function fetchAdminSlice(docId: string, order: number): Promise<AdminCatalogSlice | null> {
    isAdminLoading.value = true;
    adminError.value = null;
    try {
      const slice = await apiClient.get<AdminCatalogSlice>(
        `/api/v1/admin/catalog/documents/${docId}/slices/${order}`
      );
      selectedAdminSlice.value = slice;
      return slice;
    } catch (err: unknown) {
      if (isForbiddenError(err)) {
        cleanupAdmin();
      } else {
        adminError.value = extractErrorMessage(err, "Failed to load slice");
      }
      return null;
    } finally {
      isAdminLoading.value = false;
    }
  }

  async function previewUrl(url: string): Promise<AdminPreviewUrlResult> {
    return await apiClient.post<AdminPreviewUrlResult>("/api/v1/admin/catalog/preview-url", { url });
  }

  async function importUrl(payload: {
    url: string;
    title: string;
    description: string;
    category: number;
    language: string;
    authorOrSourceUrl?: string;
    attributionText: string;
    permissionBasis: string;
    permissionEvidence: string;
    confirmedDistributionRights: boolean;
    confirmedAiProcessingRights: boolean;
  }): Promise<AdminCatalogDocument> {
    const doc = await apiClient.post<AdminCatalogDocument>(
      "/api/v1/admin/catalog/documents/import-url",
      payload
    );
    await fetchAdminDocuments();
    return doc;
  }

  async function uploadPdf(formData: FormData): Promise<AdminCatalogDocument> {
    const doc = await apiClient.post<AdminCatalogDocument>(
      "/api/v1/admin/catalog/documents/upload-pdf",
      formData
    );
    await fetchAdminDocuments();
    return doc;
  }

  async function updateMetadata(
    id: string,
    payload: {
      title: string;
      description: string;
      category: number;
      language: string;
      authorOrSourceUrl?: string;
      attributionText: string;
      permissionBasis: string;
      permissionEvidence: string;
      confirmedDistributionRights: boolean;
      confirmedAiProcessingRights: boolean;
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
      if (isForbiddenError(err)) cleanupAdmin();
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
      if (isForbiddenError(err)) cleanupAdmin();
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
      if (isForbiddenError(err)) cleanupAdmin();
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
      if (isForbiddenError(err)) cleanupAdmin();
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
      if (isForbiddenError(err)) cleanupAdmin();
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

  function cleanupAdmin() {
    canManageCatalog.value = false;
    adminDocuments.value = [];
    selectedAdminDoc.value = null;
    selectedAdminSlice.value = null;
    adminTotalCount.value = 0;
    stopStatusPolling();
  }

  function cleanup() {
    cleanupAdmin();
    documents.value = [];
    selectedDocument.value = null;
    totalCount.value = 0;
    isAdopting.value = {};
    adoptionError.value = {};
    error.value = null;
    adminError.value = null;
  }

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
    category,
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
    adminCategory,
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
