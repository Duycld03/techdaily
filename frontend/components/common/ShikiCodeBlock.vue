<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { Copy, Check } from 'lucide-vue-next'
import { highlightCode } from '~/utils/shikiHighlighter'

const props = withDefaults(
  defineProps<{
    code: string
    language?: string
    category?: number
    tags?: string[]
  }>(),
  {
    language: 'auto'
  }
)

const detectedLanguage = computed(() => {
  if (props.language && props.language !== 'auto') {
    return props.language.toLowerCase()
  }

  const code = props.code || ''
  const trimmed = code.trim()

  // 1. Language Tags Matching (precise language identifiers only, NOT generic topic tags)
  if (props.tags && props.tags.length > 0) {
    const lowerTags = props.tags.map(t => t.toLowerCase())
    if (lowerTags.some(t => ['csharp', 'c#', 'csharp13', 'dotnet', 'dotnet10', '.net', 'aspnet', 'efcore', 'linq'].includes(t))) return 'csharp'
    if (lowerTags.some(t => ['sql', 'psql', 'postgres-sql', 'postgresql-sql', 't-sql', 'plpgsql'].includes(t))) return 'sql'
    if (lowerTags.some(t => ['vue', 'vue3', 'nuxt', 'react', 'reactjs', 'javascript', 'js', 'typescript', 'ts'].includes(t))) return 'typescript'
    if (lowerTags.some(t => ['rust', 'rs', 'cargo'].includes(t))) return 'rust'
    if (lowerTags.some(t => ['go', 'golang'].includes(t))) return 'go'
    if (lowerTags.some(t => ['python', 'py'].includes(t))) return 'python'
    if (lowerTags.some(t => ['bash', 'sh', 'shell', 'zsh'].includes(t))) return 'bash'
    if (lowerTags.some(t => ['json'].includes(t))) return 'json'
  }

  // 2. Strong C# / .NET Syntax Signatures (must precede SQL since C# frequently uses LINQ/EF Core with Select/Update/Delete)
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
    /(<template>|<script\s+lang="ts">|shallowRef<|ref<|reactive<|computed<|triggerRef\(|defineProps<|defineEmits<|defineComponent|export\s+default|export\s+const|import\s+.*\s+from\s+['"]|const\s+.*=\s+ref\(|useState\(|useEffect\()/.test(trimmed)
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

  // 8. JSON Syntax
  if ((trimmed.startsWith('{') && trimmed.endsWith('}')) || (trimmed.startsWith('[') && trimmed.endsWith(']'))) {
    try {
      JSON.parse(trimmed)
      return 'json'
    } catch {
      // not strict json
    }
  }

  // 9. Bash / Shell
  if (trimmed.startsWith('#!') || /^(curl|docker|npm|dotnet|git|sudo|chmod)\s+/m.test(trimmed)) {
    return 'bash'
  }

  // 10. Category Fallback (0: FrontendWeb, 1: BackendDotNet, 2: DatabaseStorage, 3: SystemDesign)
  if (props.category === 0) return 'typescript'
  if (props.category === 1) return 'csharp'
  if (props.category === 2) return 'sql'
  if (props.category === 3) return 'csharp'

  return 'csharp'
})

const displayLabel = computed(() => {
  switch (detectedLanguage.value) {
    case 'sql':
    case 'postgresql':
      return 'PostgreSQL / SQL'
    case 'typescript':
    case 'ts':
      return 'TypeScript'
    case 'javascript':
    case 'js':
      return 'JavaScript'
    case 'vue':
      return 'Vue 3'
    case 'rust':
    case 'rs':
      return 'Rust'
    case 'go':
    case 'golang':
      return 'Go'
    case 'python':
    case 'py':
      return 'Python'
    case 'csharp':
    case 'cs':
      return 'C# / .NET 10'
    case 'json':
      return 'JSON'
    case 'bash':
    case 'sh':
      return 'Bash / Shell'
    default:
      return detectedLanguage.value.toUpperCase()
  }
})

const highlightedHtml = ref('')

async function updateHighlighting() {
  if (!props.code) {
    highlightedHtml.value = ''
    return
  }
  const html = await highlightCode(props.code, detectedLanguage.value)
  highlightedHtml.value = html
}

onMounted(() => {
  updateHighlighting()
})

watch(
  () => [props.code, detectedLanguage.value],
  () => {
    updateHighlighting()
  }
)

const copied = ref(false)

async function copyCode() {
  if (!props.code) return
  try {
    await navigator.clipboard.writeText(props.code)
    copied.value = true
    setTimeout(() => {
      copied.value = false
    }, 2000)
  } catch {
    // Fallback
  }
}
</script>

<template>
  <div class="relative group rounded-2xl bg-slate-900 text-slate-100 overflow-hidden font-mono text-xs sm:text-sm border border-slate-800">
    <!-- Code Header -->
    <div class="flex items-center justify-between px-4 py-2 bg-slate-950/80 border-b border-slate-800/80 text-xs text-slate-400 select-none">
      <div class="flex items-center gap-2">
        <span class="w-2.5 h-2.5 rounded-full bg-rose-500/80"></span>
        <span class="w-2.5 h-2.5 rounded-full bg-amber-500/80"></span>
        <span class="w-2.5 h-2.5 rounded-full bg-emerald-500/80"></span>
        <span class="ml-2 font-mono uppercase tracking-wider text-xs text-slate-400 font-semibold">{{ displayLabel }}</span>
      </div>

      <button
        @click="copyCode"
        class="flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs transition-all active:scale-95"
        :title="copied ? 'Copied!' : 'Copy Code'"
      >
        <Check v-if="copied" class="w-3.5 h-3.5 text-emerald-400" />
        <Copy v-else class="w-3.5 h-3.5 text-slate-400" />
        <span class="text-xs font-medium">{{ copied ? 'Copied!' : 'Copy' }}</span>
      </button>
    </div>

    <!-- Highlighted Code Body -->
    <div
      v-if="highlightedHtml"
      class="shiki-container p-4 sm:p-5 overflow-x-auto text-xs sm:text-sm leading-relaxed"
      v-html="highlightedHtml"
    ></div>
    <pre
      v-else
      class="p-4 sm:p-5 overflow-x-auto leading-relaxed text-slate-200 selection:bg-indigo-500/30"
    ><code>{{ code }}</code></pre>
  </div>
</template>

<style>
.shiki-container pre.shiki {
  background-color: transparent !important;
  margin: 0 !important;
  padding: 0 !important;
  overflow-x: visible !important;
  font-family: inherit !important;
  font-size: inherit !important;
  line-height: inherit !important;
}
.shiki-container code {
  font-family: inherit !important;
  background-color: transparent !important;
}
</style>
