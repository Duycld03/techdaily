import { describe, it, expect, beforeEach, vi, afterEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useCatalogStore } from "~/stores/useCatalogStore";

const mockGet = vi.fn();
const mockPost = vi.fn();
const mockPut = vi.fn();

vi.mock("~/composables/useApiClient", () => ({
  useApiClient: () => ({
    get: mockGet,
    post: mockPost,
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
          category: 1,
          language: "en",
          sourceType: 0,
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
      category: 1,
      language: "en",
      sourceType: 0,
    });

    expect(mockGet).toHaveBeenCalledWith(
      expect.stringContaining("/api/v1/catalog/documents?")
    );
    const callUrl = String(mockGet.mock.calls[0]?.[0] ?? "");
    expect(callUrl).toContain("page=2");
    expect(callUrl).toContain("pageSize=10");
    expect(callUrl).toContain("search=Kubernetes");
    expect(callUrl).toContain("category=1");
    expect(callUrl).toContain("language=en");
    expect(callUrl).toContain("sourceType=0");

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
        category: 0,
        language: "en",
        sourceType: 0,
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
    expect(store.adoptionError["doc-1"]).toBeNull();
    expect(store.documents[0]?.existingBookId).toBe("adopted-book-id");
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
        category: 0,
        language: "en",
        sourceType: 0,
        attributionText: "Attr",
        permissionBasis: "Basis",
        permissionEvidence: "Evidence",
        confirmedDistributionRights: true,
        confirmedAiProcessingRights: true,
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
        category: 0,
        language: "en",
        sourceType: 0,
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
        category: 0,
        language: "en",
        sourceType: 0,
        attributionText: "Attr",
        permissionBasis: "Basis",
        permissionEvidence: "Evidence",
        confirmedDistributionRights: true,
        confirmedAiProcessingRights: true,
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
});
