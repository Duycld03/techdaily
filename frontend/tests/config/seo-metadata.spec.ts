import { describe, it, expect } from 'vitest'
import nuxtConfig from '../../nuxt.config'

describe('SEO Metadata & Structured Data Configuration', () => {
  const head = (nuxtConfig as any).app?.head
  const meta: Array<{ name?: string; property?: string; content?: string }> = head?.meta || []
  const link: Array<{ rel?: string; href?: string }> = head?.link || []
  const script: Array<{ type?: string; children?: string }> = head?.script || []

  describe('Canonical and Link Tags', () => {
    it('defines a canonical link pointing to the primary domain', () => {
      const canonical = link.find(l => l.rel === 'canonical')
      expect(canonical).toBeDefined()
      expect(canonical?.href).toBe('https://deeppace.duckdns.org')
    })

    it('declares the dark theme color meta tag', () => {
      const themeColor = meta.find(m => m.name === 'theme-color')
      expect(themeColor).toBeDefined()
      expect(themeColor?.content).toBe('#09090b')
    })
  })

  describe('Open Graph Protocol Tags', () => {
    it('configures complete Open Graph metadata', () => {
      const ogByProperty = new Map(meta.filter(m => m.property).map(m => [m.property!, m.content]))

      expect(ogByProperty.get('og:site_name')).toBe('DeepPace')
      expect(ogByProperty.get('og:type')).toBe('website')
      expect(ogByProperty.get('og:title')).toContain('DeepPace')
      expect(ogByProperty.get('og:description')).toBeDefined()
      expect(ogByProperty.get('og:url')).toBe('https://deeppace.duckdns.org')
      expect(ogByProperty.get('og:image')).toBeDefined()
      expect(ogByProperty.get('og:locale')).toBe('en_US')
      expect(ogByProperty.get('og:locale:alternate')).toBe('vi_VN')
    })
  })

  describe('Twitter Card Tags', () => {
    it('configures summary_large_image Twitter Card metadata', () => {
      const twitterByName = new Map(meta.filter(m => m.name).map(m => [m.name!, m.content]))

      expect(twitterByName.get('twitter:card')).toBe('summary_large_image')
      expect(twitterByName.get('twitter:title')).toContain('DeepPace')
      expect(twitterByName.get('twitter:description')).toBeDefined()
      expect(twitterByName.get('twitter:image')).toBeDefined()
    })
  })

  describe('Schema.org JSON-LD Structured Data', () => {
    it('injects valid and parseable JSON-LD WebApplication schema', () => {
      const jsonLdScript = script.find(s => s.type === 'application/ld+json')
      expect(jsonLdScript).toBeDefined()
      expect(jsonLdScript?.children).toBeDefined()

      const parsed = JSON.parse(jsonLdScript!.children!)
      expect(parsed['@context']).toBe('https://schema.org')
      expect(parsed['@type']).toEqual(expect.arrayContaining(['WebApplication', 'EducationalApplication']))
      expect(parsed.name).toBe('DeepPace')
      expect(parsed.alternateName).toBe('TechDaily')
      expect(parsed.url).toBe('https://deeppace.duckdns.org')
      expect(parsed.applicationCategory).toBe('EducationalApplication')
      expect(parsed.operatingSystem).toBe('All')
      expect(parsed.offers).toMatchObject({
        price: '0',
        priceCurrency: 'USD'
      })
    })
  })
})
