import { describe, it, expect } from 'vitest'
import {
  computeContentHash,
  extractNarrationScript,
  splitSentences
} from '~/utils/narrationScript'

describe('narrationScript', () => {
  describe('extractNarrationScript', () => {
    it('drops fenced code blocks so source code is never read aloud', () => {
      const md = 'Intro sentence.\n\n```ts\nconst x: number = 1\nconsole.log(x)\n```\n\nOutro sentence.'
      const out = extractNarrationScript(md)
      expect(out).toContain('Intro sentence.')
      expect(out).toContain('Outro sentence.')
      expect(out).not.toContain('const x')
      expect(out).not.toContain('console.log')
    })

    it('drops tilde-fenced code blocks as well', () => {
      const md = 'Before.\n\n~~~py\nprint("hi")\n~~~\n\nAfter.'
      const out = extractNarrationScript(md)
      expect(out).toContain('Before.')
      expect(out).toContain('After.')
      expect(out).not.toContain('print')
    })

    it('keeps inline code text but removes the backticks', () => {
      expect(extractNarrationScript('Use the `useFetch` composable.')).toBe('Use the useFetch composable.')
    })

    it('strips heading, list, and blockquote markers while preserving text', () => {
      const md = '# Title\n\n- first item\n- second item\n\n> a quote'
      const out = extractNarrationScript(md)
      expect(out).toContain('Title')
      expect(out).toContain('first item')
      expect(out).toContain('second item')
      expect(out).toContain('a quote')
      expect(out).not.toMatch(/^#/m)
      expect(out).not.toMatch(/^\s*-\s/m)
      expect(out).not.toMatch(/^>/m)
    })

    it('reduces links and images to their visible text', () => {
      expect(extractNarrationScript('See [the docs](https://x.dev/y) now.')).toBe('See the docs now.')
      expect(extractNarrationScript('![a diagram](/img.png)')).toBe('a diagram')
    })

    it('removes emphasis markers, table pipes, and html tags', () => {
      const md = 'This is **bold** and _italic_ and ~~gone~~.\n\n| a | b |\n\n<span>inline</span>'
      const out = extractNarrationScript(md)
      expect(out).toContain('This is bold and italic and gone.')
      expect(out).not.toContain('*')
      expect(out).not.toContain('|')
      expect(out).not.toContain('<span>')
    })

    it('returns an empty string for empty input', () => {
      expect(extractNarrationScript('')).toBe('')
    })
  })

  describe('splitSentences', () => {
    it('splits on sentence-terminating punctuation', () => {
      expect(splitSentences('One. Two! Three?')).toEqual(['One.', 'Two!', 'Three?'])
    })

    it('splits on newlines and drops empty fragments', () => {
      expect(splitSentences('Alpha.\n\nBeta.\n')).toEqual(['Alpha.', 'Beta.'])
    })

    it('handles CJK and ellipsis terminators', () => {
      expect(splitSentences('第一句。 第二句！')).toEqual(['第一句。', '第二句！'])
    })
  })

  describe('computeContentHash', () => {
    it('produces a deterministic 64-char hex digest', async () => {
      const a = await computeContentHash('hello world')
      const b = await computeContentHash('hello world')
      expect(a).toBe(b)
      expect(a).toMatch(/^[0-9a-f]{64}$/)
    })

    it('changes when the script content changes', async () => {
      const a = await computeContentHash('one')
      const b = await computeContentHash('two')
      expect(a).not.toBe(b)
    })
  })
})
