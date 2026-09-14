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
  microQuiz?: any;
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

  async function fetchBooks(category?: number, search?: string) {
    isLoading.value = true;
    error.value = null;
    try {
      const api = useApiClient();
      const query = new URLSearchParams();
      if (category !== undefined && category !== null)
        query.append("category", category.toString());
      if (search) query.append("search", search);

      const res = await api.get<{ books: Book[] }>(
        `/api/v1/library/books?${query.toString()}`,
      );
      books.value = res.books;
    } catch (err: any) {
      error.value = err.message || "Failed to load books.";
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
      }>("/api/v1/library/crawl-url", { url });
      return res;
    } catch (err: any) {
      error.value = err.message || "Failed to crawl article from URL.";
      throw err;
    } finally {
      isImporting.value = false;
    }
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
        return res.chunk;
      } catch {
        return null;
      } finally {
        inFlightCurations.delete(key);
      }
    })();

    inFlightCurations.set(key, promise);
    return promise;
  }

  return {
    books,
    selectedBook,
    isLoading,
    isImporting,
    error,
    fetchBooks,
    fetchBookById,
    importDocument,
    uploadPdf,
    getBookStatus,
    crawlUrl,
    deleteBook,
    curateSlice,
  };
});
