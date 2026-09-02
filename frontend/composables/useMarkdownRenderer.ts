import MarkdownIt from 'markdown-it'
import { createHighlighter, type Highlighter } from 'shiki'

let highlighterInstance: Highlighter | null = null
let highlighterInitPromise: Promise<Highlighter> | null = null

const SUPPORTED_LANGS = [
  'csharp',
  'typescript',
  'javascript',
  'json',
  'html',
  'css',
  'sql',
  'bash',
  'sh',
  'python',
  'go',
  'markdown',
  'yaml',
  'dockerfile'
]

async function initHighlighter(): Promise<Highlighter> {
  if (highlighterInstance) return highlighterInstance
  if (highlighterInitPromise) return highlighterInitPromise

  highlighterInitPromise = createHighlighter({
    themes: ['github-dark-dimmed', 'github-light'],
    langs: SUPPORTED_LANGS
  }).then((hl) => {
    highlighterInstance = hl
    return hl
  })

  return highlighterInitPromise
}

export function useMarkdownRenderer() {
  const isHighlighterReady = ref(false)

  // Initialize highlighter in browser
  if (import.meta.client) {
    initHighlighter().then(() => {
      isHighlighterReady.value = true
    }).catch((err) => {
      console.warn('Failed to initialize Shiki highlighter:', err)
    })
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

      const effectiveLang = detectLanguage(code, rawLang)
      const langDisplay = formatLangName(effectiveLang)

      let highlightedHtml = ''
      if (highlighterInstance && SUPPORTED_LANGS.includes(effectiveLang)) {
        try {
          highlightedHtml = highlighterInstance.codeToHtml(code.trimEnd(), {
            lang: effectiveLang,
            theme: 'github-dark-dimmed'
          })
        } catch {
          highlightedHtml = ''
        }
      }

      if (!highlightedHtml) {
        const escaped = md.utils.escapeHtml(code.trimEnd())
        highlightedHtml = `<pre class="shiki github-dark-dimmed font-mono text-sm p-4 overflow-x-auto text-slate-200"><code>${escaped}</code></pre>`
      }

      const encodedCode = encodeURIComponent(code.trimEnd())

      return `
        <div class="code-block-wrapper relative group my-5 rounded-2xl overflow-hidden border border-slate-700/60 bg-[#22272e] shadow-lg">
          <div class="flex items-center justify-between px-4 py-2 bg-slate-800/80 border-b border-slate-700/50 text-xs font-mono text-slate-300 select-none">
            <span class="flex items-center gap-1.5 font-semibold text-emerald-400">
              <span class="w-2.5 h-2.5 rounded-full bg-emerald-500/80 inline-block"></span>
              ${langDisplay}
            </span>
            <button
              type="button"
              class="copy-code-btn px-2.5 py-1 rounded-md bg-slate-700/70 hover:bg-emerald-600 hover:text-white text-slate-300 transition-all flex items-center gap-1 cursor-pointer"
              data-code="${encodedCode}"
              onclick="window.__copyCode && window.__copyCode(this)"
            >
              <span>Copy</span>
            </button>
          </div>
          <div class="code-content text-sm leading-relaxed overflow-x-auto">
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
    initHighlighter
  }
}

function detectLanguage(code: string, fallbackLang: string): string {
  if (fallbackLang && fallbackLang !== 'text' && fallbackLang !== 'plaintext') {
    return fallbackLang === 'cs' ? 'csharp' : fallbackLang
  }
  const sample = code.trim()
  if (/\b(public\s+(interface|class|record|struct|enum|static|async|void)|using\s+System|builder\.Services|AddKeyed|IActionResult)\b/.test(sample) || /^\s*(public|private|protected|internal)\s+/m.test(sample)) {
    return 'csharp'
  }
  if (/\b(import\s+React|export\s+default|interface\s+Props|const\s+|let\s+|async\s+function|type\s+[A-Z])\b/.test(sample)) {
    return 'typescript'
  }
  if (/\b(SELECT\s+|FROM\s+|WHERE\s+|INSERT\s+INTO|CREATE\s+TABLE|ALTER\s+TABLE)\b/i.test(sample)) {
    return 'sql'
  }
  if (/\b(def\s+|import\s+numpy|import\s+pandas|print\(|__init__)\b/.test(sample)) {
    return 'python'
  }
  if (/\b(docker|FROM\s+[a-z0-9]|RUN\s+|ENTRYPOINT|COPY\s+)\b/i.test(sample)) {
    return 'dockerfile'
  }
  if (sample.startsWith('{') || sample.startsWith('[')) {
    try { JSON.parse(sample); return 'json' } catch {}
  }
  return 'csharp'
}

function formatLangName(lang: string): string {
  const map: Record<string, string> = {
    csharp: 'C#',
    cs: 'C#',
    ts: 'TypeScript',
    typescript: 'TypeScript',
    js: 'JavaScript',
    javascript: 'JavaScript',
    py: 'Python',
    python: 'Python',
    sql: 'SQL',
    json: 'JSON',
    bash: 'Bash',
    sh: 'Shell',
    html: 'HTML',
    css: 'CSS',
    yaml: 'YAML',
    dockerfile: 'Dockerfile'
  }
  return map[lang] || lang.toUpperCase()
}
