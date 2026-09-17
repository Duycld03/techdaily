import { defineStore } from "pinia";
import { ref } from "vue";
import { useApiClient } from "~/composables/useApiClient";

export interface Book {
  id: string;
  title: string;
  slug: string;
  sourceType: number;
  category: number;
  authorOrSourceUrl?: string;
  totalChunks: number;
  isPublished: boolean;
  isFeatured?: boolean;
  status?: "Pending" | "Processing" | "Ready" | "Failed" | number;
  progressPercentage?: number;
  statusMessage?: string;
  errorMessage?: string;
  createdAt: string;
}

export interface ChunkSummary {
  id: string;
  chunkOrder: number;
  chapterTitle: string;
  summaryMarkdown: string;
  originalTextMarkdown: string;
  keyTakeaways: string[];
  estimatedReadMinutes: number;
  isAiFormatted?: boolean;
}


export interface BookDetail extends Book {
  chunks: ChunkSummary[];
}

export interface BookIngestionStatus {
  id?: string;
  bookId?: string;
  title?: string;
  status: "Pending" | "Processing" | "Ready" | "Failed" | number;
  progressPercentage: number;
  statusMessage?: string;
  errorMessage?: string;
  totalChunks: number;
}

export const useLibraryStore = defineStore("library", () => {
  const books = ref<Book[]>([]);
  const selectedBook = ref<BookDetail | null>(null);
  const isLoading = ref(false);
  const isImporting = ref(false);
  const error = ref<string | null>(null);

  const currentPage = ref(1);
  const pageSize = ref(12);
  const totalCount = ref(0);
  const totalPages = ref(0);
  async function fetchBooks(
    paramsOrCategory?:
      | {
          category?: number;
          search?: string;
          page?: number;
          pageSize?: number;
        }
      | number,
    maybeSearch?: string
  ) {
    let category: number | undefined;
    let search: string | undefined;
    let page: number | undefined;
    let size: number | undefined;

    if (typeof paramsOrCategory === "object" && paramsOrCategory !== null) {
      category = paramsOrCategory.category;
      search = paramsOrCategory.search;
      page = paramsOrCategory.page;
      size = paramsOrCategory.pageSize;
    } else {
      category = paramsOrCategory;
      search = maybeSearch;
    }

    const targetPage = page ?? currentPage.value;
    const targetSize = size ?? pageSize.value;

    isLoading.value = true;
    error.value = null;
    try {
      const api = useApiClient();
      const query = new URLSearchParams();
      if (category !== undefined && category !== null)
        query.append("category", category.toString());
      if (search) query.append("search", search);
      query.append("page", targetPage.toString());
      query.append("pageSize", targetSize.toString());

      const res = await api.get<{
        books: Book[];
        totalCount?: number;
        page?: number;
        pageSize?: number;
        totalPages?: number;
      }>(`/api/v1/library/books?${query.toString()}`);

      books.value = res.books || [];
      totalCount.value = res.totalCount ?? (res.books ? res.books.length : 0);
      currentPage.value = res.page ?? targetPage;
      pageSize.value = res.pageSize ?? targetSize;
      totalPages.value =
        res.totalPages ??
        (totalCount.value > 0 ? Math.ceil(totalCount.value / pageSize.value) : 0);
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : "Failed to load books.";
    } finally {
      isLoading.value = false;
    }
  }

  async function fetchBookById(id: string) {
    isLoading.value = true;
    error.value = null;
    try {
      const api = useApiClient();
      const res = await api.get<{ book: BookDetail }>(
        `/api/v1/library/books/${id}`,
      );
      selectedBook.value = res.book;
      return res.book;
    } catch (err: any) {
      error.value = err.message || "Failed to load book details.";
      throw err;
    } finally {
      isLoading.value = false;
    }
  }

  async function importDocument(params: {
    title: string;
    markdownContent: string;
    category: number;
    sourceUrl?: string;
    language?: string;
  }) {
    isImporting.value = true;
    error.value = null;
    try {
      const api = useApiClient();
      const res = await api.post<{ book: Book }>("/api/v1/library/import", {
        title: params.title,
        markdownContent: params.markdownContent,
        category: params.category,
        sourceUrl: params.sourceUrl,
        language: params.language || "en",
      });

      books.value.unshift(res.book);
      return res.book;
    } catch (err: any) {
      error.value = err.message || "Failed to import document.";
      throw err;
    } finally {
      isImporting.value = false;
    }
  }

  async function uploadPdf(formData: FormData) {
    isImporting.value = true;
    error.value = null;
    try {
      const api = useApiClient();
      const res = await api.postRaw<any>(
        "/api/v1/library/upload-pdf",
        formData,
      );
      const book: Book = res.book || res;
      books.value.unshift(book);
      return book;
    } catch (err: any) {
      error.value = err.message || "Failed to upload and process PDF.";
      throw err;
    } finally {
      isImporting.value = false;
    }
  }

  async function getBookStatus(id: string) {
    const api = useApiClient();
    return await api.get<BookIngestionStatus>(
      `/api/v1/library/books/${id}/status`,
    );
  }

  async function crawlUrl(url: string) {
    isImporting.value = true;
    error.value = null;
    try {
      const api = useApiClient();
      const res = await api.post<{
        title: string;
        sourceUrl: string;
        markdownContent: string;
        estimatedWordCount: number;
        isPdfDetected?: boolean;
        detectedPdfUrl?: string | null;
      }>("/api/v1/library/crawl-url", { url });
      return res;
    } catch (err: any) {
      error.value = err.message || "Failed to crawl article from URL.";
      throw err;
    } finally {
      isImporting.value = false;
    }
  }
  async function importRemotePdf(payload: { pdfUrl: string; title: string; category: number; language?: string }) {
    const api = useApiClient();
    return await api.post<{ bookId: string; title: string; status: string; message: string }>(
      '/api/v1/library/import-remote-pdf',
      payload
    );
  }


  async function deleteBook(id: string) {
    isLoading.value = true;
    error.value = null;
    try {
      const api = useApiClient();
      await api.delete(`/api/v1/library/books/${id}`);
      books.value = books.value.filter((b) => b.id !== id);
      if (selectedBook.value?.id === id) {
        selectedBook.value = null;
      }
      return true;
    } catch (err: any) {
      error.value = err.message || "Failed to delete document.";
      throw err;
    } finally {
      isLoading.value = false;
    }
  }

  const inFlightSliceFetches = new Map<string, Promise<ChunkSummary | null>>();

  function fetchSlice(
    bookId: string,
    order: number,
  ): Promise<ChunkSummary | null> {
    const key = `${bookId}:${order}`;
    if (inFlightSliceFetches.has(key)) {
      return inFlightSliceFetches.get(key)!;
    }

    const promise = (async () => {
      try {
        const api = useApiClient();
        const res = await api.get<{ slice: ChunkSummary }>(
          `/api/v1/library/books/${bookId}/slices/${order}`,
        );
        if (res.slice && selectedBook.value?.chunks) {
          const idx = selectedBook.value.chunks.findIndex(
            (c) => c.chunkOrder === order,
          );
          if (idx !== -1) {
            selectedBook.value.chunks[idx] = {
              ...selectedBook.value.chunks[idx],
              ...res.slice,
            };
          }
        }
        return res.slice;
      } catch (err) {
        console.error(`Failed to fetch slice ${order} for book ${bookId}:`, err);
        throw err;
      } finally {
        inFlightSliceFetches.delete(key);
      }
    })();

    inFlightSliceFetches.set(key, promise);
    return promise;
  }

  const inFlightCurations = new Map<string, Promise<ChunkSummary | null>>();

  function curateSlice(
    bookId: string,
    order: number,
  ): Promise<ChunkSummary | null> {
    const key = `${bookId}:${order}`;
    if (inFlightCurations.has(key)) {
      return inFlightCurations.get(key)!;
    }

    const promise = (async () => {
      try {
        const api = useApiClient();
        const res = await api.post<{ chunk: ChunkSummary }>(
          `/api/v1/library/books/${bookId}/slices/${order}/curate`,
          {},
        );
        if (res.chunk && selectedBook.value?.chunks) {
          const idx = selectedBook.value.chunks.findIndex(
            (c) => c.chunkOrder === order,
          );
          if (idx !== -1) {
            selectedBook.value.chunks[idx] = {
              ...selectedBook.value.chunks[idx],
              ...res.chunk,
            };
          }
        }
        return res.chunk;
      } catch (err) {
        console.error(`Failed to curate slice ${order} for book ${bookId}:`, err);
        throw err;
      } finally {
        inFlightCurations.delete(key);
      }
    })();

    inFlightCurations.set(key, promise);
    return promise;
  }
  async function exportBookMarkdown(bookId: string, slug?: string) {
    const api = useApiClient();
    const defaultName = `${slug || 'book'}-notes.md`;
    await api.download(`/api/v1/library/books/${bookId}/export-markdown`, defaultName);
  }



  return {
    books,
    selectedBook,
    isLoading,
    isImporting,
    error,
    currentPage,
    pageSize,
    totalCount,
    totalPages,
    fetchBooks,
    fetchBookById,
    fetchSlice,
    importDocument,
    uploadPdf,
    getBookStatus,
    crawlUrl,
    importRemotePdf,
    deleteBook,
    curateSlice,
    exportBookMarkdown,
  };
});
