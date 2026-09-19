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

  it('standardizes on vitesse-dark theme and includes all target languages', () => {
    expect(CODE_THEME).toBe('vitesse-dark')
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
    expect(normalizeLanguage('txt')).toBe('text')
    expect(normalizeLanguage('text')).toBe('text')
    expect(normalizeLanguage('plaintext')).toBe('text')
    expect(normalizeLanguage('output')).toBe('text')
    expect(normalizeLanguage('console')).toBe('text')
  })

  it('formats display labels consistently with insights page', () => {
    expect(formatLanguageLabel('ts')).toBe('TypeScript')
    expect(formatLanguageLabel('csharp')).toBe('C# / .NET 10')
    expect(formatLanguageLabel('sql')).toBe('PostgreSQL / SQL')
    expect(formatLanguageLabel('javascript')).toBe('JavaScript')
    expect(formatLanguageLabel('vue')).toBe('Vue 3 / SFC')
    expect(formatLanguageLabel('text')).toBe('Output')
    expect(formatLanguageLabel('txt')).toBe('Output')
    expect(formatLanguageLabel('plaintext')).toBe('Output')
  })

  it('highlights TypeScript code fence with vitesse-dark and macOS 3-dot window header', () => {
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

    // Verify container styling matches Dev-Learning Studio
    expect(html).toContain('code-block-wrapper')
    expect(html).toContain('dark:bg-canvas-subtle')
    expect(html).toContain('dark:border-white/[0.08]')

    // Verify 3-dot macOS window buttons
    expect(html).toContain('bg-[#ff5f56]')
    expect(html).toContain('bg-[#ffbd2e]')
    expect(html).toContain('bg-[#27c93f]')
    // Verify TypeScript label
    expect(html).toContain('TypeScript')

    // Verify Shiki vitesse-dark highlighting output
    expect(html).toContain('shiki vitesse-dark')
    expect(html).toContain('queryClient')
  })

  it('highlights C# code fence with matching vitesse-dark theme and C# / .NET 10 header', () => {
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
    expect(html).toContain('shiki vitesse-dark')
    expect(html).toContain('bg-[#ff5f56]')
    expect(html).toContain('GetCachedDataAsync')
  })

  it('highlights txt / output code fence with Output label and spacious margins', () => {
    const { render } = useMarkdownRenderer()
    const txtMarkdown = `
\`\`\`txt
1. Endpoint: (null)
2. Endpoint: Hello
3. Endpoint: Hello
\`\`\`
`
    const html = render(txtMarkdown)

    expect(html).toContain('Output')
    expect(html).not.toContain('C# / .NET 10')
    expect(html).toContain('code-block-wrapper')
    expect(html).toContain('my-6 sm:my-8')
    expect(html).toContain('text-sm sm:text-[14.5px]')
    expect(html).toContain('1. Endpoint: (null)')
  })

  it('auto-detects text/output for console logs without programming keywords', () => {
    const consoleOutput = `1. Endpoint: (null)
2. Endpoint: Hello
3. Endpoint: Hello`
    const detected = detectCodeLanguage(consoleOutput)
    expect(detected).toBe('text')
  })

  it('auto-detects TypeScript for unlabelled code blocks with frontend state signatures', () => {
    const code = `const queryClient = useQueryClient();
async function toggleFavorite(itemId: string) {
  const previous = queryClient.getQueryData(['items']);
}`
    const detected = detectCodeLanguage(code)
    expect(detected).toBe('typescript')
  })

  it('escapes raw HTML and script tags to prevent XSS execution', () => {
    const { render } = useMarkdownRenderer()
    const maliciousInput = '# Hello <script>alert("xss")</script><img src="x" onerror="steal()">'
    const output = render(maliciousInput)
    expect(output).not.toContain('<script>')
    expect(output).not.toContain('<img src="x"')
    expect(output).toContain('&lt;script&gt;')
  })

  it('renders GitHub alert callouts with distinct semantic colors and titles', () => {
    const { render } = useMarkdownRenderer()
    const input = `
> [!WARNING]
> When using System.Text.RegularExpressions, pass a timeout.
`
    const output = render(input)
    expect(output).toContain('callout-warning')
    expect(output).toContain('border-amber-500')
    expect(output).toContain('bg-amber-50/70')
    expect(output).toContain('Warning')
    expect(output).toContain('When using System.Text.RegularExpressions, pass a timeout.')
  })

  it('deduplicates repetitive alert headers from scraped documentation', () => {
    const { render } = useMarkdownRenderer()
    const input = `
> **[WARNING]**
>
> Warning
>
> When using System.Text.RegularExpressions, pass a timeout.
`
    const output = render(input)
    expect(output).toContain('callout-warning')
    expect(output).toContain('Warning</span>')
    // Ensure the word "Warning" is not rendered multiple times in the body
    const matches = output.match(/Warning/g)
    expect(matches?.length).toBe(1)
    expect(output).not.toContain('**[WARNING]**')
    expect(output).toContain('When using System.Text.RegularExpressions, pass a timeout.')
  })

  it('renders Note, Tip, Important, and Caution callouts correctly', () => {
    const { render } = useMarkdownRenderer()
    const noteOutput = render('> [!NOTE]\n> Note content')
    expect(noteOutput).toContain('callout-note')
    expect(noteOutput).toContain('border-sky-500')
    expect(noteOutput).toContain('Note content')

    const tipOutput = render('> [!TIP]\n> Tip content')
    expect(tipOutput).toContain('callout-tip')
    expect(tipOutput).toContain('border-emerald-500')
    expect(tipOutput).toContain('Tip content')

    const impOutput = render('> [!IMPORTANT]\n> Important content')
    expect(impOutput).toContain('callout-important')
    expect(impOutput).toContain('border-indigo-500')

    const cautionOutput = render('> [!CAUTION]\n> Caution content')
    expect(cautionOutput).toContain('callout-caution')
    expect(cautionOutput).toContain('border-rose-500')
  })

  it('renders standard blockquotes with neutral styling when no alert marker is present', () => {
    const { render } = useMarkdownRenderer()
    const input = `
> This is a normal quote from Martin Fowler.
`
    const output = render(input)
    expect(output).toContain('<blockquote')
    expect(output).toContain('border-slate-300')
    expect(output).toContain('This is a normal quote from Martin Fowler.')
    expect(output).not.toContain('callout-box')
  })

  it('renders external links with target="_blank" and rel="noopener noreferrer"', () => {
    const { render } = useMarkdownRenderer()
    const input = 'Read the [ASP.NET Core Docs](https://learn.microsoft.com/en-us/aspnet/core/) for details.'
    const output = render(input)

    expect(output).toContain('href="https://learn.microsoft.com/en-us/aspnet/core/"')
    expect(output).toContain('target="_blank"')
    expect(output).toContain('rel="noopener noreferrer"')
    expect(output).toContain('external-link')
  })

  it('preserves in-page fragment anchors without target="_blank"', () => {
    const { render } = useMarkdownRenderer()
    const input = 'Jump to [Routing Basics](#routing-basics) section.'
    const output = render(input)

    expect(output).toContain('href="#routing-basics"')
    expect(output).not.toContain('target="_blank"')
    expect(output).not.toContain('rel="noopener noreferrer"')
    expect(output).not.toContain('external-link')
  })

  it('resolves relative URLs against baseUrl and attaches target="_blank" when baseUrl is provided', () => {
    const { render } = useMarkdownRenderer()
    const input = 'See [Dependency Injection](dependency-injection?view=aspnetcore-10.0) topic.'
    const baseUrl = 'https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-10.0'
    const output = render(input, baseUrl)

    expect(output).toContain('href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0"')
    expect(output).toContain('target="_blank"')
    expect(output).toContain('rel="noopener noreferrer"')
  })

  it('preserves relative URLs without target="_blank" when baseUrl is not a URL or missing', () => {
    const { render } = useMarkdownRenderer()
    const input = 'See [Chapter 2](ch02.html).'
    const outputWithoutBase = render(input)
    expect(outputWithoutBase).toContain('href="ch02.html"')
    expect(outputWithoutBase).not.toContain('target="_blank"')

    const outputWithAuthor = render(input, 'Martin Fowler')
    expect(outputWithAuthor).toContain('href="ch02.html"')
    expect(outputWithAuthor).not.toContain('target="_blank"')
  })

  it('does not mistakenly autolink programming terms like ASP.NET, System.Net or Vue.js', () => {
    const { render } = useMarkdownRenderer()
    const input = 'Learn ASP.NET Core, System.Net.Sockets, and Vue.js reactivity without external links. But visit https://learn.microsoft.com for docs.'
    const output = render(input)

    expect(output).not.toContain('href="http://ASP.NET"')
    expect(output).not.toContain('href="http://System.Net"')
    expect(output).not.toContain('href="http://Vue.js"')
    expect(output).toContain('ASP.NET Core')
    expect(output).toContain('System.Net.Sockets')
    expect(output).toContain('Vue.js')
    expect(output).toContain('href="https://learn.microsoft.com"')
  })

  it('parses double newlines into separate semantic paragraph tags without collapsing', () => {
    const { render } = useMarkdownRenderer()
    const input = 'First paragraph with some text.\n\nSecond paragraph following double newline.'
    const output = render(input)
    expect(output).toContain('<p>First paragraph with some text.</p>')
    expect(output).toContain('<p>Second paragraph following double newline.</p>')
  })
})
