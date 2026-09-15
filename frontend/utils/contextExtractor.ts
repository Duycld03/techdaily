const BLOCK_TAGS: Record<string, true> = {
  P: true,
  BLOCKQUOTE: true,
  LI: true,
  DIV: true,
  PRE: true,
  SECTION: true,
  ARTICLE: true,
  H1: true,
  H2: true,
  H3: true,
  H4: true,
  H5: true,
  H6: true,
};

/**
 * Extracts surrounding text context from a DOM Selection within a reader container.
 * Up to `maxChars` characters centered around the selected text, normalizing whitespace.
 */
export function extractSurroundingContext(
  selection: Selection | null,
  maxChars = 500
): string {
  if (!selection || selection.rangeCount === 0) return "";
  const range = selection.getRangeAt(0);
  let container: Node | null = range.commonAncestorContainer;

  if (container.nodeType === Node.TEXT_NODE) {
    container = container.parentElement;
  }

  // Walk up to nearest block container within the reader pane
  let blockEl = container as HTMLElement | null;
  while (
    blockEl &&
    !BLOCK_TAGS[blockEl.tagName] &&
    !blockEl.classList?.contains("doc-reader-content") &&
    !blockEl.classList?.contains("markdown-body")
  ) {
    blockEl = blockEl.parentElement;
  }

  const fullText = blockEl?.textContent?.trim().replace(/\s+/g, " ") || "";
  if (!fullText) return "";

  if (fullText.length <= maxChars) {
    return fullText;
  }

  // Window context around selected text
  const selectedStr = selection.toString().trim();
  const idx = selectedStr ? fullText.indexOf(selectedStr) : -1;
  if (idx === -1) {
    return fullText.slice(0, maxChars);
  }

  const halfWindow = Math.floor((maxChars - selectedStr.length) / 2);
  const start = Math.max(0, idx - halfWindow);
  const end = Math.min(fullText.length, idx + selectedStr.length + halfWindow);
  let snippet = fullText.slice(start, end);
  if (start > 0) snippet = "..." + snippet;
  if (end < fullText.length) snippet = snippet + "...";

  return snippet;
}
