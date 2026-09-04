import { ref } from 'vue'
import MarkdownIt from 'markdown-it'
import {
  getShikiHighlighter,
  getHighlighterSync,
  detectCodeLanguage,
  formatLanguageLabel,
  normalizeLanguage,
  SUPPORTED_LANGS,
  CODE_THEME
} from '~/utils/shikiHighlighter'
declare global {
  interface Window {
    __copyCode?: (btn: HTMLElement) => void
  }
}


export function useMarkdownRenderer() {
  const isHighlighterReady = ref(getHighlighterSync() !== null)

  // Initialize highlighter in browser & register copy helper
  if (import.meta.client) {
    if (typeof window !== 'undefined' && !window.__copyCode) {
      window.__copyCode = (btn: HTMLElement) => {
        if (code) {
          navigator.clipboard.writeText(code)
          const span = btn.querySelector('span')
          const svg = btn.querySelector('svg')
          if (span) {
            const old = span.textContent
            span.textContent = 'Copied!'
            if (svg) {
              svg.innerHTML = '<polyline points="20 6 9 17 4 12"></polyline>'
              svg.classList.remove('text-slate-400')
              svg.classList.add('text-emerald-400')
            }
            setTimeout(() => {
              span.textContent = old
              if (svg) {
                svg.innerHTML = '<rect width="14" height="14" x="8" y="8" rx="2" ry="2"/><path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/>'
                svg.classList.remove('text-emerald-400')
                svg.classList.add('text-slate-400')
              }
            }, 2000)
          }
        }
      }
    }

    if (!isHighlighterReady.value) {
      getShikiHighlighter().then(() => {
        isHighlighterReady.value = true
      }).catch((err) => {
        console.warn('Failed to initialize Shiki highlighter:', err)
      })
    }
  }

  function createMarkdownInstance(): MarkdownIt {
    const md = new MarkdownIt({
      html: true,
      linkify: true,
      typographer: true,
      breaks: false
    })

    // Custom Code Block (Fence) Renderer with Copy Button & Language Badge
    const defaultFence = md.renderer.rules.fence || function (tokens, idx, options, env, self) {
      return self.renderToken(tokens, idx, options)
    }

    md.renderer.rules.fence = (tokens, idx, options, env, self) => {
      const token = tokens[idx]
      const info = token.info ? token.info.trim() : ''
      const rawLang = info.split(/\s+/)[0].toLowerCase() || ''
      const code = token.content

      const effectiveLang = detectCodeLanguage(code, rawLang)
      const targetLang = normalizeLanguage(effectiveLang)
      const langDisplay = formatLanguageLabel(targetLang)

      const highlighter = getHighlighterSync()
      let highlightedHtml = ''
      if (highlighter && SUPPORTED_LANGS.includes(targetLang)) {
        try {
          highlightedHtml = highlighter.codeToHtml(code.trimEnd(), {
            lang: targetLang,
            theme: CODE_THEME
          })
        } catch {
          highlightedHtml = ''
        }
      }

      if (!highlightedHtml) {
        const escaped = md.utils.escapeHtml(code.trimEnd())
        highlightedHtml = `<pre class="shiki one-dark-pro font-mono text-xs sm:text-sm p-4 sm:p-5 overflow-x-auto max-w-full text-slate-200"><code>${escaped}</code></pre>`
      }

      const encodedCode = encodeURIComponent(code.trimEnd())

      return `
        <div class="code-block-wrapper relative group my-5 rounded-2xl overflow-hidden border border-slate-800 bg-slate-900 shadow-lg max-w-full w-full min-w-0 font-mono text-xs sm:text-sm">
          <div class="flex items-center justify-between px-4 py-2 bg-slate-950/80 border-b border-slate-800/80 text-xs text-slate-400 select-none">
            <div class="flex items-center gap-2">
              <span class="w-2.5 h-2.5 rounded-full bg-rose-500/80"></span>
              <span class="w-2.5 h-2.5 rounded-full bg-amber-500/80"></span>
              <span class="w-2.5 h-2.5 rounded-full bg-emerald-500/80"></span>
              <span class="ml-2 font-mono uppercase tracking-wider text-xs text-slate-400 font-semibold">${langDisplay}</span>
            </div>
            <button
              type="button"
              class="copy-code-btn flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs transition-all active:scale-95 cursor-pointer"
              data-code="${encodedCode}"
              onclick="window.__copyCode && window.__copyCode(this)"
              title="Copy Code"
            >
              <svg class="copy-icon w-3.5 h-3.5 text-slate-400 shrink-0" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="14" height="14" x="8" y="8" rx="2" ry="2"/><path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/></svg>
              <span class="text-xs font-medium">Copy</span>
            </button>
          </div>
          <div class="code-content shiki-container text-xs sm:text-sm leading-relaxed overflow-x-auto max-w-full w-full">
            ${highlightedHtml}
          </div>
        </div>
      `
    }

    // Custom Blockquote Renderer for GitHub-style callouts
    const defaultBlockquoteOpen = md.renderer.rules.blockquote_open || function (tokens, idx, options, env, self) {
      return self.renderToken(tokens, idx, options)
    }

    md.renderer.rules.blockquote_open = (tokens, idx, options, env, self) => {
      return '<blockquote class="my-4 pl-4 border-l-4 border-emerald-500 bg-emerald-500/5 dark:bg-emerald-950/20 py-2.5 px-4 rounded-r-xl text-slate-700 dark:text-slate-300">'
    }

    // Custom Table Renderer
    md.renderer.rules.table_open = () => {
      return '<div class="table-container my-6 overflow-x-auto rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm"><table class="w-full text-left text-sm border-collapse">'
    }
    md.renderer.rules.table_close = () => {
      return '</table></div>'
    }
    md.renderer.rules.thead_open = () => {
      return '<thead class="bg-slate-100 dark:bg-slate-900 text-slate-900 dark:text-slate-100 font-semibold border-b border-slate-200 dark:border-slate-800">'
    }
    md.renderer.rules.th_open = () => {
      return '<th class="p-3 font-semibold text-slate-800 dark:text-slate-200">'
    }
    md.renderer.rules.td_open = () => {
      return '<td class="p-3 border-t border-slate-100 dark:border-slate-800/60 text-slate-700 dark:text-slate-300">'
    }

    return md
  }

  function cleanLatexSymbols(text: string): string {
    if (!text) return ''
    return text
      .replace(/\$\\rightarrow\$/g, '→')
      .replace(/\$\\leftarrow\$/g, '←')
      .replace(/\$\\Rightarrow\$/g, '⇒')
      .replace(/\$\\Leftarrow\$/g, '⇐')
      .replace(/\$\\leftrightarrow\$/g, '↔')
      .replace(/\$\\ge\$/g, '≥')
      .replace(/\$\\le\$/g, '≤')
      .replace(/\$\\geq\$/g, '≥')
      .replace(/\$\\leq\$/g, '≤')
      .replace(/\$\\neq\$/g, '≠')
      .replace(/\$\\approx\$/g, '≈')
      .replace(/\$\\times\$/g, '×')
      .replace(/\$\\pm\$/g, '±')
      .replace(/\$\\cdot\$/g, '·')
      .replace(/\\rightarrow/g, '→')
      .replace(/\\leftarrow/g, '←')
      .replace(/\\Rightarrow/g, '⇒')
  }

  function render(markdown: string): string {
    if (!markdown) return ''
    const cleaned = cleanLatexSymbols(markdown)
    const md = createMarkdownInstance()
    return md.render(cleaned)
  }

  return {
    render,
    isHighlighterReady,
    initHighlighter: getShikiHighlighter
  }
}

