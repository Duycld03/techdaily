import { createHighlighter, type Highlighter } from 'shiki'

export const CODE_THEME = 'one-dark-pro'

export const SUPPORTED_LANGS = [
  'csharp',
  'typescript',
  'javascript',
  'sql',
  'json',
  'vue',
  'bash',
  'html',
  'css',
  'yaml',
  'rust',
  'go',
  'python',
  'markdown',
  'dockerfile'
]

let highlighterInstance: Highlighter | null = null
let highlighterPromise: Promise<Highlighter> | null = null

export function getHighlighterSync(): Highlighter | null {
  return highlighterInstance
}

export function getShikiHighlighter(): Promise<Highlighter> {
  if (highlighterInstance) return Promise.resolve(highlighterInstance)
  if (!highlighterPromise) {
    highlighterPromise = createHighlighter({
      themes: [CODE_THEME],
      langs: SUPPORTED_LANGS
    }).then((hl) => {
      highlighterInstance = hl
      return hl
    })
  }
  return highlighterPromise
}

export function normalizeLanguage(lang?: string): string {
  if (!lang) return ''
  const l = lang.toLowerCase().trim()
  const aliases: Record<string, string> = {
    ts: 'typescript',
    typescript: 'typescript',
    tsx: 'typescript',
    js: 'javascript',
    javascript: 'javascript',
    jsx: 'javascript',
    cs: 'csharp',
    csharp: 'csharp',
    'c#': 'csharp',
    dotnet: 'csharp',
    dotnet10: 'csharp',
    aspnet: 'csharp',
    efcore: 'csharp',
    linq: 'csharp',
    py: 'python',
    python: 'python',
    sql: 'sql',
    psql: 'sql',
    postgres: 'sql',
    postgresql: 'sql',
    'postgres-sql': 'sql',
    'postgresql-sql': 'sql',
    't-sql': 'sql',
    plpgsql: 'sql',
    sh: 'bash',
    bash: 'bash',
    shell: 'bash',
    zsh: 'bash',
    json: 'json',
    yaml: 'yaml',
    yml: 'yaml',
    html: 'html',
    css: 'css',
    scss: 'css',
    rust: 'rust',
    rs: 'rust',
    cargo: 'rust',
    go: 'go',
    golang: 'go',
    vue: 'vue',
    vue3: 'vue',
    docker: 'dockerfile',
    dockerfile: 'dockerfile',
    md: 'markdown',
    markdown: 'markdown'
  }
  return aliases[l] || l
}

export function formatLanguageLabel(lang: string): string {
  const normalized = normalizeLanguage(lang)
  switch (normalized) {
    case 'sql':
      return 'PostgreSQL / SQL'
    case 'csharp':
      return 'C# / .NET 10'
    case 'typescript':
      return 'TypeScript'
    case 'javascript':
      return 'JavaScript'
    case 'vue':
      return 'Vue 3 / SFC'
    case 'rust':
      return 'Rust'
    case 'go':
      return 'Go / Golang'
    case 'python':
      return 'Python'
    case 'bash':
    case 'sh':
      return 'Bash / Shell'
    case 'json':
      return 'JSON'
    case 'html':
      return 'HTML'
    case 'css':
      return 'CSS'
    case 'yaml':
      return 'YAML'
    case 'markdown':
      return 'Markdown'
    case 'dockerfile':
      return 'Dockerfile'
    default:
      return (normalized || 'code').toUpperCase()
  }
}

export function detectCodeLanguage(
  code: string,
  fallbackLang?: string,
  tags?: string[],
  category?: number
): string {
  const normalizedFallback = normalizeLanguage(fallbackLang)
  if (
    normalizedFallback &&
    normalizedFallback !== 'text' &&
    normalizedFallback !== 'plaintext' &&
    normalizedFallback !== 'auto' &&
    SUPPORTED_LANGS.includes(normalizedFallback)
  ) {
    return normalizedFallback
  }

  const trimmed = (code || '').trim()

  // 1. Language Tags Matching
  if (tags && tags.length > 0) {
    for (const tag of tags) {
      const normalizedTag = normalizeLanguage(tag)
      if (SUPPORTED_LANGS.includes(normalizedTag)) {
        return normalizedTag
      }
    }
  }

  // 2. Strong C# / .NET Syntax Signatures
  if (
    /\b(public|private|protected|internal)\s+(class|record|struct|interface|enum)\b/.test(trimmed) ||
    /\b(public|private|protected|internal)\s+(static\s+)?(async\s+)?(Task|ValueTask|void|string|int|bool|Span|ReadOnlySpan)\b/.test(trimmed) ||
    /\b(public|private|protected|internal)\s+readonly\b/.test(trimmed) ||
    /\b(IRequest|IRequestHandler|DbContext|DbSet|AsNoTracking|ToListAsync|FirstOrDefaultAsync|SaveChangesAsync)\b/.test(trimmed) ||
    /\b(ReadOnlySpan<|Span<|stackalloc\s+|ArrayPool<|MemoryExtensions|BoundedChannelOptions|Channel<)\b/.test(trimmed) ||
    /\b(using\s+var\s+|using\s+System|namespace\s+[A-Za-z0-9_.]+;?)/.test(trimmed) ||
    /\b\[(Fact|Theory|HttpGet|HttpPost|HttpPut|HttpDelete|Route|ApiController|Authorize)\]/.test(trimmed) ||
    /\b(CancellationToken\s+\w+|cancellationToken\b)/.test(trimmed) ||
    /\b(new\s+List<|new\s+Dictionary<|new\s+HashSet<|Console\.WriteLine)/.test(trimmed)
  ) {
    return 'csharp'
  }

  // 3. Strong Rust Syntax Signatures
  if (
    /\b(fn\s+\w+|pub\s+fn\s+|impl\s+|pub\s+struct\s+|pub\s+enum\s+|let\s+mut\s+|match\s+\w+\s*\{|unsafe\s*\{|println!|eprintln!|format!|&str|String::|Vec::|BufWriter|Result<|Option<)/.test(trimmed)
  ) {
    return 'rust'
  }

  // 4. Strong Go Syntax Signatures
  if (
    /\b(package\s+\w+|func\s+\(?\w*\)?\s*\w+\(|fmt\.Print|fmt\.Sprintf|chan\s+\w+|go\s+func|make\(chan|make\(map|defer\s+\w+)/.test(trimmed)
  ) {
    return 'go'
  }

  // 5. Strong Python Syntax Signatures
  if (
    /\b(def\s+\w+\(|async\s+def\s+\w+\(|class\s+\w+\s*:|import\s+sys|import\s+os|print\(|__init__|self\.\w+|elif\s+)/.test(trimmed)
  ) {
    return 'python'
  }

  // 6. Strong Vue / TypeScript / JavaScript Signatures
  if (
    /(<template>|<script\s+lang="ts">|shallowRef<|ref<|reactive<|computed<|triggerRef\(|defineProps<|defineEmits<|defineComponent|export\s+default|export\s+const|import\s+.*\s+from\s+['"]|const\s+.*=\s+ref\(|useState\(|useEffect\(|useQueryClient\(|toggleFavorite|async\s+function\s+\w+|interface\s+[A-Z]|type\s+[A-Z]|const\s+\w+\s*:\s*|let\s+\w+\s*:\s*)/.test(trimmed)
  ) {
    return 'typescript'
  }

  // 7. Strong SQL / PostgreSQL DDL & Query Signatures
  if (
    trimmed.startsWith('--') ||
    /\b(CREATE\s+TABLE|CREATE\s+INDEX|CREATE\s+UNIQUE\s+INDEX|ALTER\s+TABLE|DROP\s+TABLE|CREATE\s+OR\s+REPLACE\s+FUNCTION)\b/i.test(trimmed) ||
    /\bSELECT\s+.+\s+FROM\s+/i.test(trimmed) ||
    /\bINSERT\s+INTO\s+/i.test(trimmed) ||
    /\bUPDATE\s+\w+\s+SET\s+/i.test(trimmed) ||
    /\bDELETE\s+FROM\s+/i.test(trimmed) ||
    /\bEXPLAIN\s+ANALYZE\b/i.test(trimmed) ||
    /\b(FILLFACTOR\s*=|VACUUM\s+ANALYZE)\b/i.test(trimmed) ||
    /\bFROM\s+pg_stat_/i.test(trimmed)
  ) {
    return 'sql'
  }

  // 8. Dockerfile Signatures
  if (/\b(FROM\s+[a-z0-9]|RUN\s+|ENTRYPOINT|COPY\s+|WORKDIR)\b/i.test(trimmed)) {
    return 'dockerfile'
  }

  // 9. JSON Syntax
  if ((trimmed.startsWith('{') && trimmed.endsWith('}')) || (trimmed.startsWith('[') && trimmed.endsWith(']'))) {
    try {
      JSON.parse(trimmed)
      return 'json'
    } catch {
      // not strict json
    }
  }

  // 10. Bash / Shell
  if (trimmed.startsWith('#!') || /^(curl|docker|npm|dotnet|git|sudo|chmod)\s+/m.test(trimmed)) {
    return 'bash'
  }

  // 11. Category Fallback (0: FrontendWeb, 1: BackendDotNet, 2: DatabaseStorage, 3: SystemDesign)
  if (category === 0) return 'typescript'
  if (category === 1) return 'csharp'
  if (category === 2) return 'sql'
  if (category === 3) return 'csharp'

  return 'csharp'
}

export async function highlightCode(
  code: string,
  lang: string,
  theme = CODE_THEME
): Promise<string> {
  if (!code) return ''

  try {
    const highlighter = await getShikiHighlighter()
    const normalizedLang = normalizeLanguage(lang)
    const targetLang = SUPPORTED_LANGS.includes(normalizedLang) ? normalizedLang : 'csharp'

    return highlighter.codeToHtml(code.trimEnd(), {
      lang: targetLang,
      theme
    })
  } catch (err) {
    console.warn('Shiki syntax highlighting fallback:', err)
    return ''
  }
}
