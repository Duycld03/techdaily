import { beforeEach, afterEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useCatalogStore } from '~/stores/useCatalogStore'

vi.mock('~/stores/useAuthStore', () => ({
  useAuthStore: () => ({ init: vi.fn(), token: null, user: null, sessionGeneration: 0, adminGeneration: 0 })
}))

describe('catalog multipart wire contract', () => {
  beforeEach(() => setActivePinia(createPinia()))
  afterEach(() => vi.unstubAllGlobals())

  it('delivers the PDF bytes and all UUID metadata through the real API transport', async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(new Response(JSON.stringify({ id: 'created' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ items: [], totalCount: 0 }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)
    const file = new File(['%PDF-content'], 'guide.pdf', { type: 'application/pdf' })
    await useCatalogStore().uploadPdf({ file, title: 'Guide', description: 'Description', categoryId: '99999999-9999-4999-8999-999999999999', language: 'vi', authorOrSourceUrl: 'Author', attributionText: 'License' })
    const [url, request] = fetchMock.mock.calls[0]!
    expect(url).toContain('/api/v1/admin/catalog/documents/upload-pdf')
    expect(request.body).toBeInstanceOf(FormData)
    expect(request.body.get('file')).toBe(file)
    expect(Object.fromEntries([...request.body.entries()].filter(([key]) => key !== 'file'))).toEqual({ title: 'Guide', description: 'Description', categoryId: '99999999-9999-4999-8999-999999999999', language: 'vi', authorOrSourceUrl: 'Author', attributionText: 'License' })
    expect(request.headers['Content-Type']).toBeUndefined()
  })
})
