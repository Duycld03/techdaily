import { createHighlighter, type Highlighter } from 'shiki'

let highlighterPromise: Promise<Highlighter> | null = null

export function getShikiHighlighter(): Promise<Highlighter> {
  if (!highlighterPromise) {
    highlighterPromise = createHighlighter({
      themes: ['one-dark-pro'],
      langs: [
        'csharp',
        'typescript',
        'javascript',
        'sql',
        'json',
        'vue',
        'bash',
        'html',
        'css',
        'yaml'
      ]
    })
  }
  return highlighterPromise
}

export async function highlightCode(
  code: string,
  lang: string,
  theme = 'one-dark-pro'
): Promise<string> {
  if (!code) return ''

  try {
    const highlighter = await getShikiHighlighter()
    const supportedLangs = [
      'csharp',
      'typescript',
      'javascript',
      'sql',
      'json',
      'vue',
      'bash',
      'html',
      'css',
      'yaml'
    ]

    const normalizedLang = lang.toLowerCase()
    const targetLang = supportedLangs.includes(normalizedLang)
      ? normalizedLang
      : normalizedLang === 'ts'
      ? 'typescript'
      : normalizedLang === 'js'
      ? 'javascript'
      : normalizedLang === 'cs'
      ? 'csharp'
      : normalizedLang === 'postgresql'
      ? 'sql'
      : 'csharp'

    return highlighter.codeToHtml(code, {
      lang: targetLang,
      theme
    })
  } catch (err) {
    console.warn('Shiki syntax highlighting fallback:', err)
    return ''
  }
}
