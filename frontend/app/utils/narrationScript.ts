// Convert a reader slice's AI-formatted markdown into a plain-text narration
// script suitable for text-to-speech. Markdown markup is removed and fenced
// code blocks are excluded so source code is not read aloud character by character.

/**
 * Strip markdown to prose. Fenced code blocks are dropped entirely; inline code
 * keeps its inner text. Headings, lists, quotes and emphasis markers are removed
 * while their text content is preserved. Whitespace is normalized.
 */
export function extractNarrationScript(markdown: string): string {
  if (!markdown) return ''
  let text = markdown

  // Drop fenced code blocks (``` ... ``` and ~~~ ... ~~~) — never read code aloud.
  text = text.replace(/```[\s\S]*?```/g, ' ')
  text = text.replace(/~~~[\s\S]*?~~~/g, ' ')

  // Inline code: keep the inner text, drop the backticks.
  text = text.replace(/`([^`]*)`/g, '$1')

  // Images ![alt](url) -> alt ; Links [text](url) -> text
  text = text.replace(/!\[([^\]]*)\]\([^)]*\)/g, '$1')
  text = text.replace(/\[([^\]]*)\]\([^)]*\)/g, '$1')

  // Alert callout markers such as [!NOTE], [!WARNING]
  text = text.replace(/\[!(?:NOTE|TIP|WARNING|IMPORTANT|CAUTION)\]/gi, ' ')

  // Headings, blockquotes, list markers
  text = text.replace(/^\s{0,3}#{1,6}\s+/gm, '')
  text = text.replace(/^\s*>\s?/gm, '')
  text = text.replace(/^\s*[-*+]\s+/gm, '')
  text = text.replace(/^\s*\d+\.\s+/gm, '')

  // Horizontal rules
  text = text.replace(/^\s*([-*_])\1{2,}\s*$/gm, ' ')

  // Emphasis / bold / strikethrough markers (keep inner text)
  text = text.replace(/(\*\*|__|~~|\*|_)/g, '')

  // Table pipes and stray HTML tags
  text = text.replace(/\|/g, ' ')
  text = text.replace(/<[^>]+>/g, ' ')

  // Normalize whitespace: collapse runs of spaces/tabs and blank lines.
  text = text.replace(/[ \t]+/g, ' ')
  text = text.replace(/\n{2,}/g, '\n')
  text = text.replace(/[ \t]*\n[ \t]*/g, '\n')

  return text.trim()
}

/** SHA-256 hex digest of the narration script, used as the cache invalidation key. */
export async function computeContentHash(script: string): Promise<string> {
  const data = new TextEncoder().encode(script)
  const digest = await globalThis.crypto.subtle.digest('SHA-256', data)
  return Array.from(new Uint8Array(digest))
    .map(b => b.toString(16).padStart(2, '0'))
    .join('')
}

/**
 * Split a narration script into sentence-sized chunks so synthesis can stream
 * (start playing the first sentence while later ones render in the background).
 */
export function splitSentences(script: string): string[] {
  return script
    .split(/\n+/)
    .flatMap(line => line.split(/(?<=[.!?…。！？])\s+/))
    .map(s => s.trim())
    .filter(s => s.length > 0)
}
