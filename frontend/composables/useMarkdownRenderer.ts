import { ref } from "vue";
import MarkdownIt from "markdown-it";
import {
  getShikiHighlighter,
  getHighlighterSync,
  detectCodeLanguage,
  formatLanguageLabel,
  normalizeLanguage,
  SUPPORTED_LANGS,
  CODE_THEME,
} from "~/utils/shikiHighlighter";
declare global {
  interface Window {
    __copyCode?: (btn: HTMLElement) => void;
  }
}

interface AlertCalloutConfig {
  label: string;
  borderClass: string;
  bgClass: string;
  titleClass: string;
  iconSvg: string;
}

const ALERT_CONFIGS: Record<string, AlertCalloutConfig> = {
  NOTE: {
    label: "Note",
    borderClass: "border-sky-500",
    bgClass: "bg-sky-50/70 dark:bg-sky-950/20",
    titleClass: "text-sky-700 dark:text-sky-400",
    iconSvg:
      '<svg class="w-4 h-4 shrink-0 text-sky-500" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="M12 16v-4"/><path d="M12 8h.01"/></svg>',
  },
  TIP: {
    label: "Tip",
    borderClass: "border-emerald-500",
    bgClass: "bg-emerald-50/70 dark:bg-emerald-950/20",
    titleClass: "text-emerald-700 dark:text-emerald-400",
    iconSvg:
      '<svg class="w-4 h-4 shrink-0 text-emerald-500" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M15 14c.2-1 .7-1.7 1.5-2.5 1-.9 1.5-2.2 1.5-3.5A6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.7.7 1.3 1.5 1.5 2.5"/><path d="M9 18h6"/><path d="M10 22h4"/></svg>',
  },
  IMPORTANT: {
    label: "Important",
    borderClass: "border-indigo-500",
    bgClass: "bg-indigo-50/70 dark:bg-indigo-950/20",
    titleClass: "text-indigo-700 dark:text-indigo-400",
    iconSvg:
      '<svg class="w-4 h-4 shrink-0 text-indigo-500" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" x2="12" y1="8" y2="12"/><line x1="12" x2="12.01" y1="16" y2="16"/></svg>',
  },
  WARNING: {
    label: "Warning",
    borderClass: "border-amber-500",
    bgClass: "bg-amber-50/70 dark:bg-amber-950/20",
    titleClass: "text-amber-700 dark:text-amber-400",
    iconSvg:
      '<svg class="w-4 h-4 shrink-0 text-amber-500" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"/><line x1="12" x2="12" y1="9" y2="13"/><line x1="12" x2="12.01" y1="17" y2="17"/></svg>',
  },
  CAUTION: {
    label: "Caution",
    borderClass: "border-rose-500",
    bgClass: "bg-rose-50/70 dark:bg-rose-950/20",
    titleClass: "text-rose-700 dark:text-rose-400",
    iconSvg:
      '<svg class="w-4 h-4 shrink-0 text-rose-500" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="7.86 2 16.14 2 22 7.86 22 16.14 16.14 22 7.86 22 2 16.14 2 7.86 7.86 2"/><line x1="12" x2="12" y1="8" y2="12"/><line x1="12" x2="12.01" y1="16" y2="16"/></svg>',
  },
};

export function useMarkdownRenderer() {
  const isHighlighterReady = ref(getHighlighterSync() !== null);

  // Initialize highlighter in browser & register copy helper
  if (import.meta.client) {
    if (typeof window !== "undefined" && !window.__copyCode) {
      window.__copyCode = (btn: HTMLElement) => {
        const rawCode = btn.getAttribute("data-code");
        const code = rawCode ? decodeURIComponent(rawCode) : "";
        if (code) {
          navigator.clipboard.writeText(code);
          const span = btn.querySelector("span");
          const svg = btn.querySelector("svg");
          if (span) {
            const old = span.textContent;
            span.textContent = "Copied!";
            if (svg) {
              svg.innerHTML = '<polyline points="20 6 9 17 4 12"></polyline>';
              svg.classList.remove("text-slate-400", "text-slate-500");
              svg.classList.add("text-emerald-500", "dark:text-emerald-400");
            }
            setTimeout(() => {
              span.textContent = old;
              if (svg) {
                svg.innerHTML =
                  '<rect width="14" height="14" x="8" y="8" rx="2" ry="2"/><path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/>';
                svg.classList.remove("text-emerald-500", "dark:text-emerald-400");
                svg.classList.add("text-slate-500", "dark:text-slate-400");
              }
            }, 2000);
          }
        }
      };
    }

    if (!isHighlighterReady.value) {
      getShikiHighlighter()
        .then(() => {
          isHighlighterReady.value = true;
        })
        .catch((err) => {
          console.warn("Failed to initialize Shiki highlighter:", err);
        });
    }
  }

  function createMarkdownInstance(): MarkdownIt {
    const md = new MarkdownIt({
      html: false,
      linkify: true,
      typographer: true,
      breaks: false,
    });

    // Disable fuzzy linking so technology names like ASP.NET, System.Net, Vue.js are not mistakenly autolinked into web domains
    md.linkify.set({ fuzzyLink: false });

    // Disable legacy 4-space indented code blocks so accidental whitespace doesn't create spurious code fences
    md.disable("code");

    // Custom Link Renderer: Open external links in new tab with rel="noopener noreferrer",
    // resolve relative links against baseUrl if provided, and preserve in-page anchors within page.
    const defaultLinkOpen =
      md.renderer.rules.link_open ||
      function (tokens, idx, options, env, self) {
        return self.renderToken(tokens, idx, options);
      };

    md.renderer.rules.link_open = (tokens, idx, options, env, self) => {
      const token = tokens[idx];
      if (!token) {
        return defaultLinkOpen(tokens, idx, options, env, self);
      }
      const hrefIndex = token.attrIndex("href");

      if (hrefIndex >= 0) {
        const href = token.attrs?.[hrefIndex]?.[1] ?? "";

        if (/^https?:\/\//i.test(href) || href.startsWith("//")) {
          token.attrSet("target", "_blank");
          token.attrSet("rel", "noopener noreferrer");
          token.attrJoin(
            "class",
            "external-link hover:underline text-emerald-600 dark:text-emerald-400",
          );
        } else if (href.startsWith("#")) {
          // In-page anchor fragment - preserve in current tab
        } else if (
          env?.baseUrl &&
          /^https?:\/\//i.test(env.baseUrl) &&
          !href.startsWith("javascript:") &&
          !href.startsWith("mailto:") &&
          !href.startsWith("tel:")
        ) {
          try {
            const resolved = new URL(href, env.baseUrl).href;
            token.attrSet("href", resolved);
            token.attrSet("target", "_blank");
            token.attrSet("rel", "noopener noreferrer");
            token.attrJoin(
              "class",
              "external-link hover:underline text-emerald-600 dark:text-emerald-400",
            );
          } catch {
            // Malformed URL, leave unchanged
          }
        }
      }

      return defaultLinkOpen(tokens, idx, options, env, self);
    };

    // Custom Code Block (Fence) Renderer with Copy Button & Language Badge
    const defaultFence =
      md.renderer.rules.fence ||
      function (tokens, idx, options, env, self) {
        return self.renderToken(tokens, idx, options);
      };

    md.renderer.rules.fence = (tokens, idx, options, env, self) => {
      const token = tokens[idx];
      if (!token) {
        return defaultFence(tokens, idx, options, env, self);
      }
      const info = token.info ? token.info.trim() : "";
      const rawLang = info.split(/\s+/)[0]?.toLowerCase() ?? "";
      const code = token.content;
      const effectiveLang = detectCodeLanguage(code, rawLang);
      const targetLang = normalizeLanguage(effectiveLang);
      const langDisplay = formatLanguageLabel(targetLang);

      const highlighter = getHighlighterSync();
      let highlightedHtml = "";
      if (highlighter && SUPPORTED_LANGS.includes(targetLang)) {
        try {
          highlightedHtml = highlighter.codeToHtml(code.trimEnd(), {
            lang: targetLang,
            theme: CODE_THEME,
          });
        } catch {
          highlightedHtml = "";
        }
      }

      if (!highlightedHtml) {
        const escaped = md.utils.escapeHtml(code.trimEnd());
        highlightedHtml = `<pre class="shiki vitesse-dark font-mono text-sm sm:text-[14.5px] p-4 sm:p-5 overflow-x-auto max-w-full text-slate-800 dark:text-slate-200"><code>${escaped}</code></pre>`;
      }

      const encodedCode = encodeURIComponent(code.trimEnd());

      return `
        <div class="code-block-wrapper relative group my-6 sm:my-8 rounded-2xl overflow-hidden border border-slate-200/80 dark:border-white/[0.08] bg-slate-50/90 dark:bg-canvas-subtle shadow-lg dark:shadow-2xl max-w-full w-full min-w-0 font-mono text-sm sm:text-[14.5px]">
          <div class="flex items-center justify-between px-4 sm:px-5 py-2.5 bg-slate-100/80 dark:bg-canvas-elevated/80 backdrop-blur-md border-b border-slate-200/80 dark:border-white/[0.06] text-xs select-none">
            <div class="flex items-center gap-2">
              <span class="w-2.5 h-2.5 rounded-full bg-[#ff5f56]"></span>
              <span class="w-2.5 h-2.5 rounded-full bg-[#ffbd2e]"></span>
              <span class="w-2.5 h-2.5 rounded-full bg-[#27c93f]"></span>
              <span class="ml-2.5 font-mono uppercase tracking-widest text-[11px] sm:text-xs text-brand-600 dark:text-brand-400 font-bold">${langDisplay}</span>
            </div>
            <button
              type="button"
              class="copy-code-btn flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-white/80 dark:bg-white/[0.06] hover:bg-white dark:hover:bg-white/[0.12] border border-slate-200/80 dark:border-white/[0.08] text-slate-700 dark:text-slate-200 text-xs font-semibold shadow-xs transition-all active:scale-95 cursor-pointer"
              data-code="${encodedCode}"
              onclick="window.__copyCode && window.__copyCode(this)"
              title="Copy Code"
            >
              <svg class="copy-icon w-3.5 h-3.5 text-slate-500 dark:text-slate-400 shrink-0" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="14" height="14" x="8" y="8" rx="2" ry="2"/><path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/></svg>
              <span class="text-xs font-medium">Copy</span>
            </button>
          </div>
          <div class="code-content markdown-code-content text-sm sm:text-[14.5px] leading-relaxed overflow-x-auto max-w-full w-full">
            ${highlightedHtml}
          </div>
        </div>
      `;
    };

    // MarkdownIt Core Rule for Alert Callout Extraction
    md.core.ruler.after("block", "callouts", (state) => {
      const tokens = state.tokens;
      for (let i = 0; i < tokens.length; i++) {
        const currentToken = tokens[i];
        if (!currentToken || currentToken.type !== "blockquote_open") continue;

        // Find matching blockquote_close
        let level = 1;
        let closeIdx = -1;
        for (let j = i + 1; j < tokens.length; j++) {
          const innerToken = tokens[j];
          if (!innerToken) continue;
          if (innerToken.type === "blockquote_open") level++;
          else if (innerToken.type === "blockquote_close") {
            level--;
            if (level === 0) {
              closeIdx = j;
              break;
            }
          }
        }
        if (closeIdx === -1) continue;

        const nextToken = tokens[i + 1];
        const inlineToken = tokens[i + 2];
        if (
          nextToken?.type === "paragraph_open" &&
          inlineToken &&
          inlineToken.type === "inline"
        ) {
          const markerMatch = inlineToken.content.match(
            /^\s*(?:\[!(NOTE|TIP|IMPORTANT|WARNING|CAUTION|DANGER)\]|\*{0,2}\[(NOTE|TIP|IMPORTANT|WARNING|CAUTION|DANGER)\]\*{0,2})(?:\s*|\n|$)/i,
          );

          if (markerMatch) {
            const matchedType = markerMatch[1] ?? markerMatch[2] ?? "";
            const rawType = matchedType.toUpperCase();
            const alertType = rawType === "DANGER" ? "CAUTION" : rawType;
            currentToken.meta = { alertType };
            const closeToken = tokens[closeIdx];
            if (closeToken) {
              closeToken.meta = { alertType };
            }

            // Remaining content in the first inline token
            let remaining = inlineToken.content
              .slice(markerMatch[0].length)
              .trim();
            if (remaining.toLowerCase() === alertType.toLowerCase()) {
              remaining = "";
            } else if (
              remaining.toLowerCase().startsWith(alertType.toLowerCase() + "\n")
            ) {
              remaining = remaining.slice(alertType.length).trim();
            }

            if (!remaining) {
              // The first paragraph only contained the marker and/or duplicate title
              tokens.splice(i + 1, 3);
              closeIdx -= 3;

              // Check if the next paragraph is purely the duplicate title
              const nextPara = tokens[i + 1];
              const nextInline = tokens[i + 2];
              if (
                nextPara?.type === "paragraph_open" &&
                nextInline &&
                nextInline.type === "inline"
              ) {
                if (
                  nextInline.content.trim().toLowerCase() ===
                  alertType.toLowerCase()
                ) {
                  tokens.splice(i + 1, 3);
                  closeIdx -= 3;
                }
              }
            } else {
              inlineToken.content = remaining;
              inlineToken.children = [];
            }
          }
        }
      }
    });

    // Custom Blockquote Renderer for Alerts & Callouts
    md.renderer.rules.blockquote_open = (tokens, idx) => {
      const token = tokens[idx];
      const alertType = token?.meta?.alertType as string | undefined;
      const cfg = alertType ? ALERT_CONFIGS[alertType] : undefined;
      if (!alertType || !cfg) {
        return '<blockquote class="my-4 pl-4 border-l-4 border-slate-300 dark:border-slate-700 bg-slate-50/50 dark:bg-slate-900/40 py-2.5 px-4 rounded-r-xl text-slate-700 dark:text-slate-300 not-italic">';
      }
      return `<div class="callout-box callout-${alertType.toLowerCase()} my-5 p-4 sm:p-5 rounded-2xl border-l-4 ${cfg.borderClass} ${cfg.bgClass} shadow-sm not-italic"><div class="flex items-center gap-2 font-bold ${cfg.titleClass} text-xs sm:text-sm uppercase tracking-wider mb-2 select-none">${cfg.iconSvg}<span>${cfg.label}</span></div><div class="callout-content text-slate-800 dark:text-slate-200 text-sm sm:text-base leading-relaxed prose-p:my-1.5 prose-p:leading-relaxed">`;
    };

    md.renderer.rules.blockquote_close = (tokens, idx) => {
      const token = tokens[idx];
      if (token?.meta?.alertType) {
        return "</div></div>";
      }
      return "</blockquote>";
    };

    // Custom Table Renderer
    md.renderer.rules.table_open = () => {
      return '<div class="table-container my-6 overflow-x-auto rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm"><table class="w-full text-left text-sm border-collapse">';
    };
    md.renderer.rules.table_close = () => {
      return "</table></div>";
    };
    md.renderer.rules.thead_open = () => {
      return '<thead class="bg-slate-100 dark:bg-slate-900 text-slate-900 dark:text-slate-100 font-semibold border-b border-slate-200 dark:border-slate-800">';
    };
    md.renderer.rules.th_open = () => {
      return '<th class="p-3 font-semibold text-slate-800 dark:text-slate-200">';
    };
    md.renderer.rules.td_open = () => {
      return '<td class="p-3 border-t border-slate-100 dark:border-slate-800/60 text-slate-700 dark:text-slate-300">';
    };

    return md;
  }

  function cleanLatexSymbols(text: string): string {
    if (!text) return "";
    return text
      .replace(/\$\\rightarrow\$/g, "→")
      .replace(/\$\\leftarrow\$/g, "←")
      .replace(/\$\\Rightarrow\$/g, "⇒")
      .replace(/\$\\Leftarrow\$/g, "⇐")
      .replace(/\$\\leftrightarrow\$/g, "↔")
      .replace(/\$\\ge\$/g, "≥")
      .replace(/\$\\le\$/g, "≤")
      .replace(/\$\\geq\$/g, "≥")
      .replace(/\$\\leq\$/g, "≤")
      .replace(/\$\\neq\$/g, "≠")
      .replace(/\$\\approx\$/g, "≈")
      .replace(/\$\\times\$/g, "×")
      .replace(/\$\\pm\$/g, "±")
      .replace(/\$\\cdot\$/g, "·")
      .replace(/\\rightarrow/g, "→")
      .replace(/\\leftarrow/g, "←")
      .replace(/\\Rightarrow/g, "⇒");
  }

  function sanitizeScraperArtifacts(text: string): string {
    if (!text) return "";
    let cleaned = text;
    if (cleaned.includes("\\n")) {
      cleaned = cleaned
        .replace(/\\r\\n/g, "\n")
        .replace(/\\n/g, "\n")
        .replace(/\\t/g, "\t");
    }
    return cleaned
      .replace(/^Read in English\s+\[Edit\]\(.*?\)\s*$/gim, "")
      .replace(/(\n\s*\* \* \*\s*){2,}/g, "\n\n* * *\n\n")
      .replace(/(\n\s*---\s*){2,}/g, "\n\n---\n\n")
      .replace(
        /^>\s*["“']\s*(\[!(?:NOTE|TIP|IMPORTANT|WARNING|CAUTION|DANGER)\]|\*{0,2}\[(?:NOTE|TIP|IMPORTANT|WARNING|CAUTION|DANGER)\]\*{0,2})/gim,
        "> $1",
      );
  }

  function render(markdown: string, baseUrl?: string): string {
    if (!markdown) return "";
    const sanitized = sanitizeScraperArtifacts(markdown);
    const cleaned = cleanLatexSymbols(sanitized);
    const md = createMarkdownInstance();
    return md.render(cleaned, { baseUrl });
  }

  return {
    render,
    isHighlighterReady,
    initHighlighter: getShikiHighlighter,
  };
}
