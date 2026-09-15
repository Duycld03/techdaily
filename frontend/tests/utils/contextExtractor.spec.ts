import { describe, it, expect } from "vitest";
import { extractSurroundingContext } from "~/utils/contextExtractor";

function createMockSelection(params: {
  text: string;
  container: Node;
}): Selection {
  const range = document.createRange();
  range.selectNodeContents(params.container);

  return {
    rangeCount: 1,
    getRangeAt: () => range,
    toString: () => params.text,
  } as unknown as Selection;
}

describe("extractSurroundingContext", () => {
  it("returns empty string when selection is null or rangeCount is 0", () => {
    expect(extractSurroundingContext(null)).toBe("");

    const emptySelection = {
      rangeCount: 0,
      getRangeAt: () => {
        throw new Error("No range");
      },
      toString: () => "",
    } as unknown as Selection;

    expect(extractSurroundingContext(emptySelection)).toBe("");
  });

  it("extracts and normalizes whitespace for short paragraph context", () => {
    const p = document.createElement("p");
    p.textContent = "   In distributed systems,   consensus ensures consistency across replicas.   ";

    const textNode = p.firstChild as Text;
    const selection = createMockSelection({
      text: "consensus",
      container: textNode,
    });

    const result = extractSurroundingContext(selection, 500);
    expect(result).toBe("In distributed systems, consensus ensures consistency across replicas.");
  });

  it("windows long text context centered around the selected term", () => {
    const p = document.createElement("p");
    // Generate text > 500 chars
    const prefix = "A".repeat(300);
    const suffix = "B".repeat(300);
    const target = "DistributedTransactions";
    p.textContent = `${prefix} ${target} ${suffix}`;

    const textNode = p.firstChild as Text;
    const selection = createMockSelection({
      text: target,
      container: textNode,
    });

    const result = extractSurroundingContext(selection, 100);
    expect(result.length).toBeLessThanOrEqual(106); // 100 + "..." + "..."
    expect(result).toContain(target);
    expect(result.startsWith("...")).toBe(true);
    expect(result.endsWith("...")).toBe(true);
  });

  it("traverses up nested inline tags to the nearest block container", () => {
    const container = document.createElement("div");
    container.className = "markdown-body";
    const blockquote = document.createElement("blockquote");
    const p = document.createElement("p");
    const strong = document.createElement("strong");
    const code = document.createElement("code");
    code.textContent = "WAL";

    strong.appendChild(code);
    p.appendChild(document.createTextNode("Before writing to disk, append to "));
    p.appendChild(strong);
    p.appendChild(document.createTextNode(" to prevent corruption."));
    blockquote.appendChild(p);
    container.appendChild(blockquote);

    const selection = createMockSelection({
      text: "WAL",
      container: code,
    });

    const result = extractSurroundingContext(selection);
    expect(result).toBe("Before writing to disk, append to WAL to prevent corruption.");
  });

  it("handles list item containers correctly", () => {
    const ul = document.createElement("ul");
    const li = document.createElement("li");
    li.textContent = "Log-Structured Merge-tree organizes data into sequential SSTables.";
    ul.appendChild(li);

    const selection = createMockSelection({
      text: "SSTables",
      container: li.firstChild as Text,
    });

    const result = extractSurroundingContext(selection);
    expect(result).toBe("Log-Structured Merge-tree organizes data into sequential SSTables.");
  });
});
