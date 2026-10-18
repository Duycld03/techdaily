import { describe, it, expect, beforeEach, vi } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useLibraryStore } from "~/stores/useLibraryStore";

const categoryId = "33333333-3333-4000-8000-333333333333";
const category = { id: categoryId, slug: "novel", nameEn: "Novel subject", nameVi: "Chủ đề mới", isActive: true, sortOrder: 0, version: 1 };
const mockBooks = [
  {
    id: "b-1",
    title: "Designing Data-Intensive Applications",
    slug: "ddia",
    sourceType: 0,
    category,
    categoryId,
    authorOrSourceUrl: "Martin Kleppmann",
    totalChunks: 12,
    isPublished: true,
    createdAt: "2026-08-31T00:00:00Z",
  },
  {
    id: "b-2",
    title: "Vue 3 Core Architecture",
    slug: "vue3-core",
    sourceType: 0,
    category,
    categoryId,
    authorOrSourceUrl: "Evan You",
    totalChunks: 8,
    isPublished: true,
    createdAt: "2026-08-31T00:00:00Z",
  },
];

const mockPost = vi.fn(async (url: string, body: Record<string, unknown>) => {
  if (url.includes("/import-remote-pdf")) {
    return {
      bookId: "b-remote-1",
      title: body.title,
      status: "Processing",
      message: "Remote PDF queued for ingestion"
    };
  }
  if (url.includes("/import")) {
    return {
      book: {
        id: "b-3",
        title: body.title,
        slug: "sre-book",
        sourceType: 1,
        category,
        categoryId: body.categoryId,
        totalChunks: 5,
        isPublished: true,
        createdAt: "2026-08-31T00:00:00Z",
      },
    };
  }
  if (url.includes("/curate")) {
    return {
      chunk: {
        id: "chk-1",
        chunkOrder: 1,
        chapterTitle: "Reliability, Scalability, and Maintainability",
        summaryMarkdown: "Curated summary",
        originalTextMarkdown: "# Curated markdown",
        keyTakeaways: ["Takeaway 1", "Takeaway 2"],
        estimatedReadMinutes: 5,
        isAiFormatted: true,
      },
    };
  }
  throw new Error("Not found");
});
const mockDownload = vi.fn(async (url: string, defaultName: string) => {
  return;
});
const mockPatch = vi.fn(async (url: string, body: Record<string, unknown>) => {
  return {
    id: "b-1",
    title: body.title,
    category,
    categoryId: body.categoryId,
    authorOrSourceUrl: body.authorOrSourceUrl,
    totalChunks: 12,
    isPublished: true,
    createdAt: "2026-08-31T00:00:00Z",
    language: (body.language as string) ?? "en"
  };
});


const mockGet = vi.fn(async (url: string) => {
  if (url.includes("/slices/")) {
    return {
      slice: {
        id: "chk-1",
        chunkOrder: 1,
        chapterTitle: "Reliability, Scalability, and Maintainability",
        summaryMarkdown: "Core qualities of data systems.",
        originalTextMarkdown: "Systems must maintain performance...",
        keyTakeaways: ["High availability", "Fault tolerance"],
        estimatedReadMinutes: 4,
        isAiFormatted: true,
      },
    };
  }
  if (url.includes("/books/b-1")) {
    return {
      book: {
        ...mockBooks[0],
        chunks: [
          {
            id: "chk-1",
            chunkOrder: 1,
            chapterTitle: "Reliability, Scalability, and Maintainability",
            summaryMarkdown: "Core qualities of data systems.",
            originalTextMarkdown: "Systems must maintain performance...",
            keyTakeaways: ["High availability", "Fault tolerance"],
            estimatedReadMinutes: 4,
          },
        ],
      },
    };
  }
  if (url.includes("/books")) {
    const urlObj = new URL("http://localhost" + url);
    const pageParam = parseInt(urlObj.searchParams.get("page") || "1", 10);
    const pageSizeParam = parseInt(urlObj.searchParams.get("pageSize") || "12", 10);
    return {
      books: [...mockBooks],
      totalCount: 24,
      page: pageParam,
      pageSize: pageSizeParam,
      totalPages: 2
    };
  }
  throw new Error("Not found");
});

vi.mock("~/composables/useApiClient", () => ({
  useApiClient: () => ({
    get: mockGet,
    post: mockPost,
    patch: mockPatch,
    download: mockDownload,
  }),
}));

describe("useLibraryStore", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it("preserves exact Markdown and source URL while sending only the UUID assignment", async () => {
    const library = useLibraryStore();
    const payload = {
      title: "Novel topic",
      markdownContent: "# Original\n\n  unchanged prose\n",
      sourceUrl: "https://example.com/original?q=1",
      categoryId: "33333333-3333-4000-8000-333333333333",
      language: "vi",
    };
    await library.importDocument(payload);
    expect(mockPost).toHaveBeenCalledWith("/api/v1/library/import", payload);
    expect(mockPost.mock.calls[0]?.[1]).not.toHaveProperty("category");
  });

  it("round trips a UUID filter without a legacy query key", async () => {
    const library = useLibraryStore();
    const id = "33333333-3333-4000-8000-333333333333";
    await library.fetchBooks({ categoryId: id });
    const url = new URL(mockGet.mock.calls[0]![0], "https://example.com");
    expect(url.searchParams.get("categoryId")).toBe(id);
    expect(url.searchParams.has("category")).toBe(false);
  });

  it("combines personal language with category, search and paging in the request", async () => {
    const library = useLibraryStore();
    await library.fetchBooks({ categoryId, search: "architecture", language: "vi", page: 2 });
    const query = new URL(String(mockGet.mock.calls.at(-1)?.[0]), "https://example.test").searchParams;
    expect(query.get("language")).toBe("vi");
    expect(query.get("categoryId")).toBe(categoryId);
    expect(query.get("search")).toBe("architecture");
    expect(query.get("page")).toBe("2");
  });

  it("keeps the newer personal query result when an earlier request finishes late", async () => {
    type ListResponse = { books: typeof mockBooks; totalCount: number; page: number; pageSize: number; totalPages: number };
    const older = Promise.withResolvers<ListResponse>();
    const newer = Promise.withResolvers<ListResponse>();
    mockGet.mockImplementationOnce(() => older.promise).mockImplementationOnce(() => newer.promise);
    const library = useLibraryStore();
    const oldRequest = library.fetchBooks({ search: "older", page: 1 });
    const newRequest = library.fetchBooks({ search: "current", page: 2 });
    newer.resolve({ books: mockBooks.map(book => ({ ...book, title: "Current result" })), totalCount: 20, page: 2, pageSize: 12, totalPages: 2 });
    await newRequest;
    older.resolve({ books: mockBooks.map(book => ({ ...book, title: "Stale result" })), totalCount: 30, page: 1, pageSize: 12, totalPages: 3 });
    await oldRequest;

    expect(library.books[0]?.title).toBe("Current result");
    expect(library.currentPage).toBe(2);
    expect(library.totalCount).toBe(20);
  });

  it("fetches books list with categories and search filtering", async () => {
    const library = useLibraryStore();
    expect(library.books).toHaveLength(0);

    await library.fetchBooks();
    expect(library.books).toHaveLength(2);
    expect(library.books[0]?.title).toBe(
      "Designing Data-Intensive Applications",
    );
  });

  it("fetches books list with pagination state", async () => {
    const library = useLibraryStore();
    expect(library.currentPage).toBe(1);
    expect(library.pageSize).toBe(12);

    await library.fetchBooks({ page: 2, pageSize: 12 });
    expect(library.books).toHaveLength(2);
    expect(library.currentPage).toBe(2);
    expect(library.pageSize).toBe(12);
    expect(library.totalCount).toBe(24);
    expect(library.totalPages).toBe(2);
    expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("page=2"));
    expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("pageSize=12"));
  });

  it("fetches specific book details with chunks", async () => {
    const library = useLibraryStore();
    const book = await library.fetchBookById("b-1");

    expect(book.id).toBe("b-1");
    expect(book.chunks).toHaveLength(1);
    expect(book.chunks[0]?.chapterTitle).toBe(
      "Reliability, Scalability, and Maintainability",
    );
  });

  it("imports new document and triggers chunking", async () => {
    const library = useLibraryStore();
    const result = await library.importDocument({
      title: "Site Reliability Engineering",
      markdownContent: "# Chapter 1\nSRE principles...",
      categoryId,
    });

    expect(result.id).toBe("b-3");
    expect(result.title).toBe("Site Reliability Engineering");
    expect(result.totalChunks).toBe(5);
    expect(library.books).toHaveLength(1);
  });

  it("imports remote PDF via importRemotePdf action", async () => {
    const library = useLibraryStore();
    const res = await library.importRemotePdf({
      pdfUrl: "https://example.com/books/atomic-habits.pdf",
      title: "Atomic Habits",
      categoryId,
      language: "vi"
    });

    expect(mockPost).toHaveBeenCalledWith(
      "/api/v1/library/import-remote-pdf",
      expect.objectContaining({
        pdfUrl: "https://example.com/books/atomic-habits.pdf",
        title: "Atomic Habits",
        categoryId,
      })
    );
    expect(res.bookId).toBe("b-remote-1");
    expect(res.status).toBe("Processing");
  });

  it("deduplicates concurrent curateSlice calls for the same slice", async () => {
    const library = useLibraryStore();

    const p1 = library.curateSlice("b-1", 1);
    const p2 = library.curateSlice("b-1", 1);

    const [res1, res2] = await Promise.all([p1, p2]);
    expect(res1).toEqual(res2);
    expect(res1?.isAiFormatted).toBe(true);

    // Verify mockPost was called only once despite two concurrent invocations
    expect(mockPost).toHaveBeenCalledTimes(1);
  });

  it("fetches single slice on demand and deduplicates in-flight requests", async () => {
    const library = useLibraryStore();

    const p1 = library.fetchSlice("b-1", 1);
    const p2 = library.fetchSlice("b-1", 1);

    const [res1, res2] = await Promise.all([p1, p2]);
    expect(res1).toEqual(res2);
    expect(res1?.chapterTitle).toBe("Reliability, Scalability, and Maintainability");
    expect(res1?.originalTextMarkdown).toBe("Systems must maintain performance...");
  });

  it("exports book markdown notes via download API", async () => {
    const library = useLibraryStore();
    await library.exportBookMarkdown("b-1", "ddia");

    expect(mockDownload).toHaveBeenCalledWith(
      "/api/v1/library/books/b-1/export-markdown",
      "ddia-notes.md"
    );
  });

  it("re-throws error when curateSlice API call fails", async () => {
    const library = useLibraryStore();
    mockPost.mockRejectedValueOnce(new Error("AI curation failed"));

    await expect(library.curateSlice("b-1", 1)).rejects.toThrow("AI curation failed");
  });

  it("re-throws error when fetchSlice API call fails", async () => {
    const library = useLibraryStore();
    mockGet.mockRejectedValueOnce(new Error("Slice fetch failed"));

    await expect(library.fetchSlice("b-1", 1)).rejects.toThrow("Slice fetch failed");
  });

  it("updates book metadata via patch and reflects change in store", async () => {
    const library = useLibraryStore();
    await library.fetchBooks();

    const updated = await library.updateBook("b-1", {
      title: "Updated Title",
      authorOrSourceUrl: "New Author",
      categoryId,
      language: "vi"
    });

    expect(mockPatch).toHaveBeenCalledWith(
      "/api/v1/library/books/b-1",
      {
        title: "Updated Title",
        authorOrSourceUrl: "New Author",
        categoryId,
        language: "vi"
      }
    );
    expect(updated.title).toBe("Updated Title");
    expect(updated.language).toBe("vi");
    expect(library.books.find(b => b.id === "b-1")?.title).toBe("Updated Title");
    expect(library.books.find(b => b.id === "b-1")?.categoryId).toBe(categoryId);
    expect(library.books.find(b => b.id === "b-1")?.language).toBe("vi");
  });

  it("supports categoryId filter in fetchBooks query parameters", async () => {
    const library = useLibraryStore();
    await library.fetchBooks({ categoryId: "11111111-1111-4000-8000-111111111111" });
    expect(library.books.length).toBe(2);
  });

  it("updates book with categoryId via patch", async () => {
    const library = useLibraryStore();
    await library.fetchBooks();

    await library.updateBook("b-1", {
      title: "Category UUID Title",
      categoryId: "11111111-1111-4000-8000-111111111111"
    });

    expect(mockPatch).toHaveBeenCalledWith(
      "/api/v1/library/books/b-1",
      expect.objectContaining({
        categoryId: "11111111-1111-4000-8000-111111111111"
      })
    );
  });
});
