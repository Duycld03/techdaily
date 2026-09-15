import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { ref, nextTick } from "vue";
import { mount, flushPromises } from "@vue/test-utils";
import { setActivePinia, createPinia } from "pinia";
import TermExplainerModal from "~/components/today/TermExplainerModal.vue";
import enTranslations from "~/i18n/locales/en.json";
import viTranslations from "~/i18n/locales/vi.json";

const mockExplainTerm = vi.fn();

vi.mock("~/stores/useDailyFocusStore", () => ({
  useDailyFocusStore: () => ({
    explainTerm: mockExplainTerm,
  }),
}));

vi.mock("~/composables/useMarkdownRenderer", () => ({
  useMarkdownRenderer: () => ({
    render: (md: string) => `<p>${md}</p>`,
    isHighlighterReady: ref(true),
  }),
}));

function getTranslation(translations: Record<string, unknown>, key: string): string {
  const parts = key.split(".");
  let current: unknown = translations;
  for (const part of parts) {
    if (current && typeof current === "object" && part in current) {
      current = (current as Record<string, unknown>)[part];
    } else {
      return key;
    }
  }
  return typeof current === "string" ? current : key;
}

interface TermExplanationResponse {
  term: string;
  explanation: string;
  isFromCache: boolean;
  locale?: string;
}

describe("TermExplainerModal.vue", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    mockExplainTerm.mockResolvedValue({
      term: "Write-Ahead Log",
      explanation: "WAL ensures data durability before committing to storage.",
      isFromCache: true,
      locale: "en",
    });

    Object.defineProperty(navigator, "clipboard", {
      value: {
        writeText: vi.fn().mockResolvedValue(undefined),
      },
      writable: true,
      configurable: true,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  function createWrapper(props: Record<string, unknown> = {}, locale = "en") {
    const translations = (locale === "vi" ? viTranslations : enTranslations) as Record<string, unknown>;
    return mount(TermExplainerModal, {
      props: {
        term: "Write-Ahead Log",
        category: "Storage Engine",
        context: "Append-only log structure for crash recovery.",
        ...props,
      },
      global: {
        mocks: {
          $t: (key: string) => getTranslation(translations, key),
        },
      },
    });
  }

  it("renders responsive header layout with truncate and flex constraints", async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Outer flex header
    const header = wrapper.find(".border-b");
    expect(header.classes()).toContain("flex");
    expect(header.classes()).toContain("items-center");
    expect(header.classes()).toContain("justify-between");
    expect(header.classes()).toContain("gap-3");

    // Icon container has shrink-0
    const iconWrapper = wrapper.find(".bg-brand-100");
    expect(iconWrapper.classes()).toContain("shrink-0");

    // Title and category container has min-w-0 flex-1
    const textContainer = wrapper.find(".min-w-0.flex-1");
    expect(textContainer.exists()).toBe(true);

    // Category row has flex-wrap sm:flex-nowrap
    const categoryRow = textContainer.find(".flex.items-center.gap-2");
    expect(categoryRow.classes()).toContain("flex-wrap");
    expect(categoryRow.classes()).toContain("sm:flex-nowrap");

    // Category span has max-w and truncate
    const categorySpan = categoryRow.find("span");
    expect(categorySpan.classes()).toContain("truncate");
    expect(categorySpan.classes()).toContain("max-w-[180px]");
    expect(categorySpan.attributes("title")).toBe("Storage Engine");

    // Term heading has truncate and title
    const termHeading = wrapper.find("h3");
    expect(termHeading.classes()).toContain("truncate");
    expect(termHeading.attributes("title")).toBe("Write-Ahead Log");

    // Close button has shrink-0
    const closeBtn = wrapper.find("button[aria-label]");
    expect(closeBtn.classes()).toContain("shrink-0");
  });

  it("renders Instant Cache badge with whitespace-nowrap and shrink-0 when isFromCache is true", async () => {
    mockExplainTerm.mockResolvedValue({
      term: "Write-Ahead Log",
      explanation: "WAL ensures data durability.",
      isFromCache: true,
      locale: "en",
    });

    const wrapper = createWrapper();
    await flushPromises();

    const badge = wrapper.find(".bg-amber-500\\/15");
    expect(badge.exists()).toBe(true);
    expect(badge.classes()).toContain("whitespace-nowrap");
    expect(badge.classes()).toContain("shrink-0");
    expect(badge.text()).toContain("Instant Cache");
  });

  it("does not render Instant Cache badge when isFromCache is false", async () => {
    mockExplainTerm.mockResolvedValue({
      term: "Write-Ahead Log",
      explanation: "WAL ensures data durability.",
      isFromCache: false,
      locale: "en",
    });

    const wrapper = createWrapper();
    await flushPromises();

    const badge = wrapper.find(".bg-amber-500\\/15");
    expect(badge.exists()).toBe(false);
  });

  it("renders English localized strings correctly", async () => {
    const wrapper = createWrapper({}, "en");
    await flushPromises();

    // Instant cache badge
    const badge = wrapper.find(".bg-amber-500\\/15");
    expect(badge.text()).toBe("⚡ Instant Cache");

    // Powered by Google Gemini footer
    const footerSpan = wrapper.find(".border-t span.font-medium");
    expect(footerSpan.text()).toBe("Powered by Google Gemini");

    // Copy explanation button
    const copyBtn = wrapper.find(".border-t button");
    expect(copyBtn.text()).toBe("Copy Explanation");
  });

  it("renders Vietnamese localized strings correctly", async () => {
    const wrapper = createWrapper({}, "vi");
    await flushPromises();

    // Instant cache badge
    const badge = wrapper.find(".bg-amber-500\\/15");
    expect(badge.text()).toBe("⚡ Bộ nhớ tức thì");

    // Powered by Google Gemini footer
    const footerSpan = wrapper.find(".border-t span.font-medium");
    expect(footerSpan.text()).toBe("Được hỗ trợ bởi Google Gemini");

    // Copy explanation button
    const copyBtn = wrapper.find(".border-t button");
    expect(copyBtn.text()).toBe("Sao chép giải thích");
  });

  it("renders localized loading indicator while fetching", async () => {
    const { promise, resolve } = Promise.withResolvers<TermExplanationResponse>();
    mockExplainTerm.mockReturnValue(promise);

    const wrapper = createWrapper({}, "vi");
    await nextTick();
    // Before promise resolves, loading should be displayed
    const loadingText = wrapper.find(".animate-ping + span");
    expect(loadingText.text()).toBe("Đang phân tích thuật ngữ với Google Gemini...");

    resolve({
      term: "Write-Ahead Log",
      explanation: "Giải thích...",
      isFromCache: false,
    });
    await flushPromises();

    expect(wrapper.find(".animate-ping").exists()).toBe(false);
  });

  it("handles copy button interaction and switches to copied state", async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const copyBtn = wrapper.find(".border-t button");
    expect(copyBtn.text()).toBe("Copy Explanation");

    await copyBtn.trigger("click");
    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(
      "WAL ensures data durability before committing to storage."
    );

    expect(copyBtn.text()).toBe("Copied");
  });

  it("emits close event when close button or background overlay is clicked", async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const closeBtn = wrapper.find("button[aria-label]");
    await closeBtn.trigger("click");
    expect(wrapper.emitted("close")).toBeTruthy();

    const overlay = wrapper.find(".fixed.inset-0");
    await overlay.trigger("click");
    expect(wrapper.emitted("close")?.length).toBe(2);
  });
});
