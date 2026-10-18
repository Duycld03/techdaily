import { defineStore } from "pinia";
import { ref } from "vue";
import { useApiClient } from "~/composables/useApiClient";
import type { CategoryDescriptor } from "~/stores/useCategoryStore";

export interface Book {
  id: string;
  title: string;
  slug: string;
  sourceType: number;
  category: CategoryDescriptor;
  categoryId: string;
  authorOrSourceUrl?: string | null;
  totalChunks: number;
  isPublished: boolean;
  isFeatured?: boolean;
  status?: "Pending" | "Processing" | "Ready" | "Failed" | number;
  progressPercentage?: number;
  statusMessage?: string;
  errorMessage?: string;
  createdAt: string;
  language?: string;
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
  language?: string;
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
  let activeBooksRequestSeq = 0;
  function cancelInFlight() {
    activeBooksRequestSeq++;
  }
  async function fetchBooks(params?: {
    categoryId?: string | null;
    search?: string;
    language?: string | null;
    page?: number;
    pageSize?: number;
  }) {
    const { categoryId, search, language } = params ?? {};
    const targetPage = params?.page ?? currentPage.value;
    const targetSize = params?.pageSize ?? pageSize.value;
    const requestSeq = ++activeBooksRequestSeq;

    isLoading.value = true;
    error.value = null;
    try {
      const api = useApiClient();
      const query = new URLSearchParams();
      if (categoryId) query.append("categoryId", categoryId);
      if (search) query.append("search", search);
      if (language) query.append("language", language);
      query.append("page", targetPage.toString());
      query.append("pageSize", targetSize.toString());

      const res = await api.get<{
        books: Book[];
        totalCount?: number;
        page?: number;
        pageSize?: number;
        totalPages?: number;
      }>(`/api/v1/library/books?${query.toString()}`);
      if (requestSeq !== activeBooksRequestSeq) return;

      books.value = res.books || [];
      totalCount.value = res.totalCount ?? (res.books ? res.books.length : 0);
      currentPage.value = res.page ?? targetPage;
      pageSize.value = res.pageSize ?? targetSize;
      totalPages.value =
        res.totalPages ??
        (totalCount.value > 0 ? Math.ceil(totalCount.value / pageSize.value) : 0);
    } catch (err: unknown) {
      if (requestSeq === activeBooksRequestSeq) error.value = err instanceof Error ? err.message : "Failed to load books.";
    } finally {
      if (requestSeq === activeBooksRequestSeq) isLoading.value = false;
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
    categoryId: string;
    sourceUrl?: string;
    language?: string;
  }) {
    isImporting.value = true;
    error.value = null;
    try {
      const api = useApiClient();
      const res = await api.post<{ book: Book }>("/api/v1/library/import", {
        title: params.title,
        categoryId: params.categoryId,
        markdownContent: params.markdownContent,
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
  async function importRemotePdf(payload: { pdfUrl: string; title: string; categoryId: string; language?: string }) {
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
  async function updateBook(id: string, payload: { title: string; authorOrSourceUrl?: string | null; categoryId: string; language?: string }) {
    isLoading.value = true;
    error.value = null;
    try {
      const api = useApiClient();
      const updated = await api.patch<Book>(`/api/v1/library/books/${id}`, payload);
      const index = books.value.findIndex((b) => b.id === id);
      if (index !== -1) {
        books.value[index] = { ...books.value[index], ...updated };
      }
      if (selectedBook.value?.id === id) {
        selectedBook.value = { ...selectedBook.value, ...updated };
      }
      return updated;
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : "Failed to update book.";
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
  async function replaceBookSource(
    id: string,
    file: File,
    expectedVersion: number,
    explicitLanguage?: string
  ) {
    isLoading.value = true;
    error.value = null;
    try {
      const api = useApiClient();
      const formData = new FormData();
      formData.append('file', file);
      formData.append('expectedVersion', expectedVersion.toString());
      formData.append('requestId', crypto.randomUUID());
      if (explicitLanguage) {
        formData.append('explicitLanguage', explicitLanguage);
      }
      const res = await api.postRaw<{ attemptId: string; documentId: string; state: string; resultingVersion: number }>(
        `/api/v1/library/books/${id}/source/pdf`,
        formData
      );
      return res;
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Failed to replace book source.';
      throw err;
    } finally {
      isLoading.value = false;
    }
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
    cancelInFlight,
    fetchBooks,
    fetchBookById,
    fetchSlice,
    importDocument,
    uploadPdf,
    getBookStatus,
    crawlUrl,
    importRemotePdf,
    deleteBook,
    updateBook,
    curateSlice,
    exportBookMarkdown,
    replaceBookSource,
  };
});
