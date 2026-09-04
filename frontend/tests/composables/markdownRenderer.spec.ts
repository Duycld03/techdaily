import { describe, it, expect, beforeAll } from 'vitest'
import { useMarkdownRenderer } from '~/composables/useMarkdownRenderer'
import {
  getShikiHighlighter,
  normalizeLanguage,
  formatLanguageLabel,
  detectCodeLanguage,
  CODE_THEME,
  SUPPORTED_LANGS
} from '~/utils/shikiHighlighter'

describe('useMarkdownRenderer & shikiHighlighter', () => {
  beforeAll(async () => {
    await getShikiHighlighter()
  })

  it('standardizes on one-dark-pro theme and includes all target languages', () => {
    expect(CODE_THEME).toBe('one-dark-pro')
    expect(SUPPORTED_LANGS).toContain('typescript')
    expect(SUPPORTED_LANGS).toContain('csharp')
    expect(SUPPORTED_LANGS).toContain('javascript')
    expect(SUPPORTED_LANGS).toContain('sql')
    expect(SUPPORTED_LANGS).toContain('dockerfile')
    expect(SUPPORTED_LANGS).toContain('markdown')
  })

  it('normalizes language aliases correctly', () => {
    expect(normalizeLanguage('ts')).toBe('typescript')
    expect(normalizeLanguage('typescript')).toBe('typescript')
    expect(normalizeLanguage('js')).toBe('javascript')
    expect(normalizeLanguage('cs')).toBe('csharp')
    expect(normalizeLanguage('c#')).toBe('csharp')
    expect(normalizeLanguage('psql')).toBe('sql')
    expect(normalizeLanguage('postgres')).toBe('sql')
    expect(normalizeLanguage('py')).toBe('python')
    expect(normalizeLanguage('rs')).toBe('rust')
    expect(normalizeLanguage('golang')).toBe('go')
    expect(normalizeLanguage('docker')).toBe('dockerfile')
  })

  it('formats display labels consistently with insights page', () => {
    expect(formatLanguageLabel('ts')).toBe('TypeScript')
    expect(formatLanguageLabel('csharp')).toBe('C# / .NET 10')
    expect(formatLanguageLabel('sql')).toBe('PostgreSQL / SQL')
    expect(formatLanguageLabel('javascript')).toBe('JavaScript')
    expect(formatLanguageLabel('vue')).toBe('Vue 3 / SFC')
  })

  it('highlights TypeScript code fence with one-dark-pro and macOS 3-dot window header', () => {
    const { render } = useMarkdownRenderer()
    const tsMarkdown = `
\`\`\`ts
// Optimistic Update Pattern
const queryClient = useQueryClient();
async function toggleFavorite(itemId: string) {
  await queryClient.cancelQueries(['items']);
}
\`\`\`
`
    const html = render(tsMarkdown)

    // Verify container styling matches /insights
    expect(html).toContain('code-block-wrapper')
    expect(html).toContain('bg-slate-900')
    expect(html).toContain('border-slate-800')

    // Verify 3-dot macOS window buttons
    expect(html).toContain('bg-rose-500/80')
    expect(html).toContain('bg-amber-500/80')
    expect(html).toContain('bg-emerald-500/80')

    // Verify TypeScript label
    expect(html).toContain('TypeScript')

    // Verify Shiki one-dark-pro highlighting output
    expect(html).toContain('shiki one-dark-pro')
    expect(html).toContain('queryClient')
  })

  it('highlights C# code fence with matching one-dark-pro theme and C# / .NET 10 header', () => {
    const { render } = useMarkdownRenderer()
    const csharpMarkdown = `
\`\`\`csharp
public ValueTask<string> GetCachedDataAsync(string key) {
    if (_memoryCache.TryGetValue(key, out string val)) {
        return new ValueTask<string>(val);
    }
    return new ValueTask<string>(FetchFromDbAsync(key));
}
\`\`\`
`
    const html = render(csharpMarkdown)

    expect(html).toContain('C# / .NET 10')
    expect(html).toContain('shiki one-dark-pro')
    expect(html).toContain('bg-rose-500/80')
    expect(html).toContain('GetCachedDataAsync')
  })

  it('auto-detects TypeScript for unlabelled code blocks with frontend state signatures', () => {
    const code = `const queryClient = useQueryClient();
async function toggleFavorite(itemId: string) {
  const previous = queryClient.getQueryData(['items']);
}`
    const detected = detectCodeLanguage(code)
    expect(detected).toBe('typescript')
  })
})
