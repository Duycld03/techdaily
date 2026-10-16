import { describe, it, expect, beforeEach, vi, afterEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useCatalogStore } from "~/stores/useCatalogStore";
const category = { id: '99999999-9999-4999-8999-999999999999', slug: 'novel', nameEn: 'Novel', nameVi: 'Moi', sortOrder: 1, isActive: true, version: 1 };
const nullableLearnerFields = { authorOrSourceUrl: null, publishedAt: null, existingBookId: null };
const nullableAdminFields = { authorOrSourceUrl: null, processingStatusMessage: null, processingErrorMessage: null, createdByUserId: null, reviewedByUserId: null, reviewedRevision: null, reviewedAt: null, publishedByUserId: null, publishedAt: null, hiddenByUserId: null, hiddenAt: null, updatedAt: null };

const mockGet = vi.fn();
const mockPost = vi.fn();
const mockPut = vi.fn();

vi.mock("~/composables/useApiClient", () => ({
  useApiClient: () => ({
    get: mockGet,
    post: mockPost,
    postRaw: mockPost,
    put: mockPut,
  }),
}));

describe("useCatalogStore", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("fetches capabilities and updates canManageCatalog", async () => {
    mockGet.mockResolvedValueOnce({ canManageCatalog: true });

    const store = useCatalogStore();
    const canManage = await store.fetchCapabilities();

    expect(mockGet).toHaveBeenCalledWith("/api/v1/catalog/capabilities");
    expect(canManage).toBe(true);
    expect(store.canManageCatalog).toBe(true);
  });

  it("handles capabilities failure with default false", async () => {
    mockGet.mockRejectedValueOnce(new Error("Network error"));

    const store = useCatalogStore();
    const canManage = await store.fetchCapabilities();

    expect(canManage).toBe(false);
    expect(store.canManageCatalog).toBe(false);
  });

  it("fetches published documents with exact query params", async () => {
    mockGet.mockResolvedValueOnce({
      items: [
        {
          id: "doc-1",
          title: "Kubernetes Guide",
          description: "Cloud native docs",
          category,
          categoryId: category.id,
          language: "en",
          sourceType: 'PdfBook',
          attributionText: "Linux Foundation",
          totalSlices: 10,
          estimatedReadMinutes: 45,
          publishedAt: "2026-10-14T00:00:00Z",
          existingBookId: null,
        },
      ],
      totalCount: 1,
      page: 2,
      pageSize: 10,
    });

    const store = useCatalogStore();
    await store.fetchPublishedDocuments({
      page: 2,
      pageSize: 10,
      search: "Kubernetes",
      categoryId: category.id,
      language: "en",
      sourceType: 'PdfBook',
    });

    expect(mockGet).toHaveBeenCalledWith(
      expect.stringContaining("/api/v1/catalog/documents?")
    );
    const callUrl = String(mockGet.mock.calls[0]?.[0] ?? "");
    expect(callUrl).toContain("page=2");
    expect(callUrl).toContain("pageSize=10");
    expect(callUrl).toContain("search=Kubernetes");
    expect(callUrl).toContain(`categoryId=${category.id}`);
    expect(new URL(callUrl, 'https://example.test').searchParams.has('category')).toBe(false);
    expect(callUrl).toContain("language=en");
    expect(callUrl).toContain("sourceType=PdfBook");

    expect(store.documents).toHaveLength(1);
    expect(store.documents[0]?.title).toBe("Kubernetes Guide");
    expect(store.totalCount).toBe(1);
  });

  it("discards stale discovery responses when requests race", async () => {
    const store = useCatalogStore();

    const firstResolvers = Promise.withResolvers<unknown>();
    const secondResolvers = Promise.withResolvers<unknown>();

    mockGet
      .mockReturnValueOnce(firstResolvers.promise)
      .mockReturnValueOnce(secondResolvers.promise);

    // Dispatch first request (e.g. search "old")
    const req1 = store.fetchPublishedDocuments({ search: "old" });
    // Dispatch second request before first resolves (e.g. search "new")
    const req2 = store.fetchPublishedDocuments({ search: "new" });

    // Second request finishes first
    secondResolvers.resolve({
      items: [{ id: "doc-new", title: "New Result" }],
      totalCount: 1,
    });
    await req2;

    expect(store.documents[0]?.id).toBe("doc-new");

    // First request finishes late
    firstResolvers.resolve({
      items: [{ id: "doc-old", title: "Old Result" }],
      totalCount: 5,
    });
    await req1;

    // Stale response must be discarded; store retains "doc-new"
    expect(store.documents[0]?.id).toBe("doc-new");
    expect(store.totalCount).toBe(1);
  });

  it("prevents duplicate adoption submissions for the same document", async () => {
    const store = useCatalogStore();

    const adoptionResolvers = Promise.withResolvers<unknown>();
    mockPost.mockReturnValueOnce(adoptionResolvers.promise);

    const call1 = store.adoptDocument("doc-1");
    // Duplicate submit while call1 is in-flight
    const call2 = store.adoptDocument("doc-1");

    expect(await call2).toBeNull();
    expect(mockPost).toHaveBeenCalledTimes(1);

    adoptionResolvers.resolve({ bookId: "personal-book-1", alreadyAdded: false });
    const result1 = await call1;
    expect(result1?.bookId).toBe("personal-book-1");
    expect(store.isAdopting["doc-1"]).toBe(false);
  });

  it("allows adoption retry after a failure and clears error on success", async () => {
    const store = useCatalogStore();
    store.documents = [
      {
        id: "doc-1",
        title: "Test",
        description: "Desc",
        category,
        categoryId: category.id,
        language: "en",
        sourceType: 'PdfBook',
        ...nullableLearnerFields,
        attributionText: "Attr",
        totalSlices: 1,
        estimatedReadMinutes: 5,
        existingBookId: null,
      },
    ];

    mockPost.mockRejectedValueOnce(new Error("Network timeout"));

    // First attempt fails
    await expect(store.adoptDocument("doc-1")).rejects.toThrow("Network timeout");
    expect(store.adoptionError["doc-1"]).toBe("Network timeout");
    expect(store.isAdopting["doc-1"]).toBe(false);

    // Second attempt succeeds
    mockPost.mockResolvedValueOnce({ bookId: "adopted-book-id", alreadyAdded: false });
    const result = await store.adoptDocument("doc-1");

    expect(result?.bookId).toBe("adopted-book-id");
    expect(store.documents[0]?.existingBookId).toBe('adopted-book-id');
    expect(store.documents[0]).not.toHaveProperty('alreadyAdded');
    expect(store.adoptionError["doc-1"]).toBeNull();
  });

  it("maintains separate learner discovery and admin document states", async () => {
    const store = useCatalogStore();

    mockGet
      .mockResolvedValueOnce({
        items: [{ id: "learner-doc-1", title: "Published Doc" }],
        totalCount: 1,
      })
      .mockResolvedValueOnce({
        items: [{ id: "admin-doc-1", title: "Draft Doc", visibility: "Draft" }],
        totalCount: 5,
      });

    await store.fetchPublishedDocuments();
    await store.fetchAdminDocuments();

    expect(store.documents).toHaveLength(1);
    expect(store.documents[0]?.id).toBe("learner-doc-1");

    expect(store.adminDocuments).toHaveLength(1);
    expect(store.adminDocuments[0]?.id).toBe("admin-doc-1");
    expect(store.totalCount).toBe(1);
    expect(store.adminTotalCount).toBe(5);
  });

  it("cleans up admin state on 403 Forbidden response", async () => {
    const store = useCatalogStore();
    store.canManageCatalog = true;
    store.adminDocuments = [
      {
        id: "admin-doc-1",
        title: "Draft",
        description: "Desc",
        category,
        categoryId: category.id,
        language: "en",
        sourceType: 'PdfBook',
        ...nullableAdminFields,
        attributionText: "Attr",
        visibility: "Draft",
        processingStatus: "Pending",
        totalSlices: 0,
        estimatedReadMinutes: 0,
        revision: 1,
        concurrencyVersion: 1,
        processingGeneration: 1,
        isEligibleForPublication: false,
        unmetPublicationRequirements: [],
        createdAt: "2026-10-14T00:00:00Z",
      },
    ];

    const forbiddenError = { status: 403, message: "Forbidden" };
    mockGet.mockRejectedValueOnce(forbiddenError);

    await store.fetchAdminDocuments();

    expect(store.canManageCatalog).toBe(false);
    expect(store.adminDocuments).toHaveLength(0);
  });

  it("cleans up all state on full cleanup()", () => {
    const store = useCatalogStore();
    store.canManageCatalog = true;
    store.documents = [
      {
        id: "doc-1",
        title: "Title",
        description: "Desc",
        category,
        categoryId: category.id,
        language: "en",
        sourceType: 'PdfBook',
        ...nullableLearnerFields,
        attributionText: "Attr",
        totalSlices: 1,
        estimatedReadMinutes: 1,
      },
    ];
    store.adminDocuments = [
      {
        id: "admin-1",
        title: "Title",
        description: "Desc",
        category,
        categoryId: category.id,
        language: "en",
        sourceType: 'PdfBook',
        ...nullableAdminFields,
        attributionText: "Attr",
        visibility: "Draft",
        processingStatus: "Pending",
        totalSlices: 0,
        estimatedReadMinutes: 0,
        revision: 1,
        concurrencyVersion: 1,
        processingGeneration: 1,
        isEligibleForPublication: false,
        unmetPublicationRequirements: [],
        createdAt: "2026-10-14T00:00:00Z",
      },
    ];
    store.totalCount = 10;
    store.isAdopting = { "doc-1": true };
    store.adoptionError = { "doc-1": "Error" };

    store.cleanup();

    expect(store.canManageCatalog).toBe(false);
    expect(store.documents).toHaveLength(0);
    expect(store.adminDocuments).toHaveLength(0);
    expect(store.totalCount).toBe(0);
    expect(Object.keys(store.isAdopting)).toHaveLength(0);
    expect(Object.keys(store.adoptionError)).toHaveLength(0);
  });

  it("handles status polling until processing reaches terminal state", async () => {
    vi.useFakeTimers();
    const store = useCatalogStore();

    mockGet
      .mockResolvedValueOnce({
        id: "poll-doc",
        processingStatus: "Processing",
      })
      .mockResolvedValueOnce({
        id: "poll-doc",
        processingStatus: "Ready",
      });

    const updateCallback = vi.fn();
    store.startStatusPolling("poll-doc", updateCallback);

    await vi.advanceTimersByTimeAsync(2000);
    expect(updateCallback).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(2000);
    expect(updateCallback).toHaveBeenCalledTimes(2);

    // Polling should have stopped after Ready
    await vi.advanceTimersByTimeAsync(2000);
    expect(updateCallback).toHaveBeenCalledTimes(2);
  });

  it("supports categoryId in fetchPublishedDocuments query", async () => {
    mockGet.mockResolvedValueOnce({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 12
    });

    const store = useCatalogStore();
    await store.fetchPublishedDocuments({ categoryId: "44444444-4444-4000-8000-444444444444" });

    expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("categoryId=44444444-4444-4000-8000-444444444444"));
  });

  it("supports categoryId in fetchAdminDocuments query", async () => {
    mockGet.mockResolvedValueOnce({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20
    });

    const store = useCatalogStore();
    await store.fetchAdminDocuments({ categoryId: "55555555-5555-4000-8000-555555555555" });

    expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("categoryId=55555555-5555-4000-8000-555555555555"));
  });

  it("persists and clears admin category UUID filters between refreshes", async () => {
    mockGet.mockResolvedValue({ items: [], totalCount: 0 });
    const store = useCatalogStore();
    await store.fetchAdminDocuments({ categoryId: category.id });
    await store.fetchAdminDocuments();
    expect(mockGet.mock.lastCall?.[0]).toContain(`categoryId=${category.id}`);
    await store.fetchAdminDocuments({ categoryId: null });
    expect(mockGet.mock.lastCall?.[0]).not.toContain('category');
  });

  it("uploads PDF multipart with only categoryId", async () => {
    mockPost.mockResolvedValue({ id: 'created' });
    mockGet.mockResolvedValue({ items: [], totalCount: 0 });
    const store = useCatalogStore();
    const file = new File(['%PDF-actual-content'], 'guide.pdf', { type: 'application/pdf' });
    await store.uploadPdf({ file, title: 'Guide', description: 'Full description', categoryId: category.id, language: 'vi', authorOrSourceUrl: 'Author', attributionText: 'Licensed' });
    const body = mockPost.mock.calls[0]![1] as FormData;
    expect(body.get('file')).toBe(file);
    expect(body.get('description')).toBe('Full description');
    expect(body.get('language')).toBe('vi');
    expect(body.get('authorOrSourceUrl')).toBe('Author');
    expect(body.get('attributionText')).toBe('Licensed');
    expect(body.get('categoryId')).toBe(category.id);
    expect(body.has('category')).toBe(false);
  });

  it("sends URL import and metadata updates with categoryId only", async () => {
    mockPost.mockResolvedValue({ id: 'created' });
    mockPut.mockResolvedValue({ id: 'created' });
    mockGet.mockResolvedValue({ items: [], totalCount: 0 });
    const store = useCatalogStore();
    await store.importUrl({ url: 'https://example.test/guide', title: 'Guide', description: 'Guide', categoryId: category.id, language: 'en' });
    expect(mockPost.mock.calls[0]![1]).toMatchObject({ categoryId: category.id });
    expect(mockPost.mock.calls[0]![1]).not.toHaveProperty('category');
    await store.updateMetadata('created', { title: 'Renamed', description: 'Guide', categoryId: category.id, language: 'en', expectedVersion: 1 });
    expect(mockPut.mock.calls[0]![1]).toMatchObject({ categoryId: category.id });
    expect(mockPut.mock.calls[0]![1]).not.toHaveProperty('category');
  });
});
