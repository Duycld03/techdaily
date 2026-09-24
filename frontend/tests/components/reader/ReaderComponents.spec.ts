import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import ReaderHeaderBar from "~/components/reader/ReaderHeaderBar.vue";
import ReaderTocSidebar from "~/components/reader/ReaderTocSidebar.vue";
import ReaderNavigationCards from "~/components/reader/ReaderNavigationCards.vue";
import type { BookDetail } from "~/stores/useLibraryStore";

const mockBook: BookDetail = {
  id: "book-1",
  title: "PostgreSQL 17 Internals",
  slug: "postgres-17",
  sourceType: 0,
  category: 2,
  authorOrSourceUrl: "Egor Rogov",
  totalChunks: 3,
  isPublished: true,
  createdAt: "2026-09-01T00:00:00Z",
  chunks: [
    {
      id: "chunk-1",
      chunkOrder: 1,
      chapterTitle: "Chapter 1: Architecture & Process Memory",
      summaryMarkdown: "Summary 1",
      originalTextMarkdown: "Original 1",
      keyTakeaways: ["Key 1"],
      estimatedReadMinutes: 4,
      isAiFormatted: true,
    },
    {
      id: "chunk-2",
      chunkOrder: 2,
      chapterTitle: "Chapter 2: WAL & Checkpointing Mechanisms",
      summaryMarkdown: "Summary 2",
      originalTextMarkdown: "Original 2",
      keyTakeaways: ["Key 2"],
      estimatedReadMinutes: 5,
      isAiFormatted: true,
    },
    {
      id: "chunk-3",
      chunkOrder: 3,
      chapterTitle: "Chapter 3: Query Optimization & Vector Indexing",
      summaryMarkdown: "Summary 3",
      originalTextMarkdown: "Original 3",
      keyTakeaways: ["Key 3"],
      estimatedReadMinutes: 6,
      isAiFormatted: true,
    },
  ],
};

const globalMocks = {
  $t: (key: string, params?: Record<string, any>) => {
    if (params) {
      return `${key}:${JSON.stringify(params)}`;
    }
    return key;
  },
  locale: "en",
};

describe("ReaderHeaderBar.vue", () => {
  it("renders book title and active chapter indicator", () => {
    const wrapper = mount(ReaderHeaderBar, {
      props: {
        book: mockBook,
        currentChunk: mockBook.chunks[0],
        activeChunkIndex: 0,
        totalChunks: 3,
        isTocOpen: true,
        progressPercentage: 33,
      },
      global: {
        mocks: globalMocks,
        stubs: {
          NuxtLink: {
            template: "<a><slot /></a>",
          },
          ThemeToggle: {
            template: "<button class='theme-toggle-stub' />",
          },
        },
      },
    });

    expect(wrapper.text()).toContain("PostgreSQL 17 Internals");
    expect(wrapper.text()).toContain("33%");
  });

  it("emits toggle-toc and open-mobile-toc on contents button click", async () => {
    const wrapper = mount(ReaderHeaderBar, {
      props: {
        book: mockBook,
        currentChunk: mockBook.chunks[0],
        activeChunkIndex: 0,
        totalChunks: 3,
        isTocOpen: false,
        progressPercentage: 33,
      },
      global: {
        mocks: globalMocks,
        stubs: {
          NuxtLink: { template: "<a><slot /></a>" },
          ThemeToggle: { template: "<button />" },
        },
      },
    });

    const tocButtons = wrapper.findAll("button").filter((b) => b.attributes("title") === "reader.open_toc");
    expect(tocButtons.length).toBeGreaterThanOrEqual(2);
    await tocButtons[0]?.trigger("click");
    expect(wrapper.emitted("toggle-toc")).toHaveLength(1);

    await tocButtons[1]?.trigger("click");
    expect(wrapper.emitted("open-mobile-toc")).toHaveLength(1);
  });

  it("emits prev-slice and next-slice events correctly", async () => {
    const wrapper = mount(ReaderHeaderBar, {
      props: {
        book: mockBook,
        currentChunk: mockBook.chunks[1],
        activeChunkIndex: 1,
        totalChunks: 3,
        isTocOpen: false,
        progressPercentage: 66,
      },
      global: {
        mocks: globalMocks,
        stubs: {
          NuxtLink: { template: "<a><slot /></a>" },
          ThemeToggle: { template: "<button />" },
        },
      },
    });

    const prevBtn = wrapper.find('button[title="reader.prev_slice_hint"]');
    const nextBtn = wrapper.find('button[title="reader.next_slice_hint"]');

    expect(prevBtn.attributes("disabled")).toBeUndefined();
    expect(nextBtn.attributes("disabled")).toBeUndefined();

    await prevBtn.trigger("click");
    expect(wrapper.emitted("prev-slice")).toHaveLength(1);

    await nextBtn.trigger("click");
    expect(wrapper.emitted("next-slice")).toHaveLength(1);
  });
});

describe("ReaderTocSidebar.vue", () => {
  it("renders chapter titles and completed indicators", () => {
    const completedSet = new Set([1]);
    const wrapper = mount(ReaderTocSidebar, {
      props: {
        book: mockBook,
        activeChunkIndex: 0,
        completedSlices: completedSet,
        totalChunks: 3,
        isDesktopOpen: true,
        isMobileOpen: false,
        isExportingMarkdown: false,
      },
      global: {
        mocks: globalMocks,
      },
    });

    expect(wrapper.text()).toContain("Chapter 1: Architecture & Process Memory");
    expect(wrapper.text()).toContain("Chapter 2: WAL & Checkpointing Mechanisms");
    expect(wrapper.text()).toContain("Chapter 3: Query Optimization & Vector Indexing");
  });

  it("filters chapters based on search query input", async () => {
    const wrapper = mount(ReaderTocSidebar, {
      props: {
        book: mockBook,
        activeChunkIndex: 0,
        completedSlices: new Set(),
        totalChunks: 3,
        isDesktopOpen: true,
        isMobileOpen: false,
      },
      global: {
        mocks: globalMocks,
      },
    });

    const input = wrapper.find("input[type='text']");
    await input.setValue("WAL");

    expect(wrapper.text()).toContain("Chapter 2: WAL & Checkpointing Mechanisms");
    expect(wrapper.text()).not.toContain("Chapter 1: Architecture");
    expect(wrapper.text()).not.toContain("Chapter 3: Query Optimization");
  });

  it("emits select-chunk with original index when a chapter button is clicked", async () => {
    const wrapper = mount(ReaderTocSidebar, {
      props: {
        book: mockBook,
        activeChunkIndex: 0,
        completedSlices: new Set(),
        totalChunks: 3,
        isDesktopOpen: true,
        isMobileOpen: false,
      },
      global: {
        mocks: globalMocks,
      },
    });

    const chunkButtons = wrapper.findAll("button").filter((b) =>
      b.text().includes("Chapter")
    );
    expect(chunkButtons.length).toBeGreaterThanOrEqual(3);

    const targetBtn = chunkButtons[1];
    expect(targetBtn).toBeDefined();
    await targetBtn?.trigger("click");
    expect(wrapper.emitted("select-chunk")).toEqual([[1]]);
  });

  it("emits export-markdown on export button click", async () => {
    const wrapper = mount(ReaderTocSidebar, {
      props: {
        book: mockBook,
        activeChunkIndex: 0,
        completedSlices: new Set(),
        totalChunks: 3,
        isDesktopOpen: true,
        isMobileOpen: false,
      },
      global: {
        mocks: globalMocks,
      },
    });

    const exportBtn = wrapper.find('[data-testid="export-markdown-btn"]');
    expect(exportBtn.exists()).toBe(true);
    await exportBtn.trigger("click");
    expect(wrapper.emitted("export-markdown")).toHaveLength(1);
  });
});

describe("ReaderNavigationCards.vue", () => {
  it("renders prev and next cards when between first and last slice", async () => {
    const wrapper = mount(ReaderNavigationCards, {
      props: {
        prevChunk: mockBook.chunks[0],
        nextChunk: mockBook.chunks[2],
        activeChunkIndex: 1,
        totalChunks: 3,
      },
      global: {
        mocks: globalMocks,
        stubs: {
          NuxtLink: { template: "<a><slot /></a>" },
        },
      },
    });

    expect(wrapper.text()).toContain("Chapter 1: Architecture & Process Memory");
    expect(wrapper.text()).toContain("Chapter 3: Query Optimization & Vector Indexing");

    const buttons = wrapper.findAll("button");
    expect(buttons).toHaveLength(2);

    const prevBtn = buttons[0];
    const nextBtn = buttons[1];
    expect(prevBtn).toBeDefined();
    expect(nextBtn).toBeDefined();

    await prevBtn?.trigger("click");
    expect(wrapper.emitted("prev-slice")).toHaveLength(1);

    await nextBtn?.trigger("click");
    expect(wrapper.emitted("next-slice")).toHaveLength(1);
  });

  it("renders return to library link on final slice", () => {
    const wrapper = mount(ReaderNavigationCards, {
      props: {
        prevChunk: mockBook.chunks[1],
        nextChunk: null,
        activeChunkIndex: 2,
        totalChunks: 3,
      },
      global: {
        mocks: globalMocks,
        stubs: {
          NuxtLink: { template: "<a class='library-link'><slot /></a>" },
        },
      },
    });

    expect(wrapper.text()).toContain("reader.completed_card_label");
    expect(wrapper.text()).toContain("reader.return_library");
    expect(wrapper.find(".library-link").exists()).toBe(true);
  });
});
