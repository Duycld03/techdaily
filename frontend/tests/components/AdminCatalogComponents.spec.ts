import { describe, it, expect, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import AdminCatalogHeader from '~/components/catalog/admin/AdminCatalogHeader.vue'
import AdminCatalogNavRail from '~/components/catalog/admin/AdminCatalogNavRail.vue'
import AdminCatalogDraftForm from '~/components/catalog/admin/AdminCatalogDraftForm.vue'
import AdminCatalogSlicesReview from '~/components/catalog/admin/AdminCatalogSlicesReview.vue'
import AdminCatalogDocumentDetail from '~/components/catalog/admin/AdminCatalogDocumentDetail.vue'
import AdminCatalogSliceModal from '~/components/catalog/admin/AdminCatalogSliceModal.vue'
import { useCatalogStore, type AdminCatalogDocument, type AdminCatalogSlice } from '~/stores/useCatalogStore'
import { useCategoryStore } from '~/stores/useCategoryStore'
import CatalogCard from '~/components/catalog/CatalogCard.vue'
import CatalogPreviewModal from '~/components/catalog/CatalogPreviewModal.vue'
const category = { id: '99999999-9999-4999-8999-999999999999', slug: 'novel-category', nameEn: 'Novel category', nameVi: 'Danh muc moi', sortOrder: 1, isActive: true, version: 1 }
const mockDoc: AdminCatalogDocument = {
  id: 'doc-1',
  title: 'High Performance Browser Networking',
  description: 'Networking',
  sourceType: 'WebDocUrl',
  categoryId: category.id,
  category,
  language: 'en',
  authorOrSourceUrl: 'https://hpbn.co',
  attributionText: 'Ilya Grigorik',
  visibility: 'Draft',
  processingStatus: 'Ready',
  processingStatusMessage: 'Ready',
  processingErrorMessage: null,
  totalSlices: 5,
  estimatedReadMinutes: 25,
  revision: 2,
  concurrencyVersion: 1,
  processingGeneration: 1,
  createdByUserId: null,
  reviewedRevision: null,
  publishedByUserId: null,
  publishedAt: null,
  hiddenByUserId: null,
  hiddenAt: null,
  reviewedByUserId: null,
  reviewedAt: null,
  isEligibleForPublication: false,
  unmetPublicationRequirements: ['Administrator review is required before publication.'],
  createdAt: '2026-10-14T00:00:00Z',
  updatedAt: '2026-10-14T00:00:00Z'
}

const mockSlice: AdminCatalogSlice = {
  id: 'slice-1',
  catalogDocumentId: 'doc-1',
  language: 'en',
  keyTakeaways: [],
  sliceOrder: 1,
  chapterTitle: 'Primer on Web Performance',
  isAiFormatted: true,
  estimatedReadMinutes: 5,
  summaryMarkdown: 'Summary 1',
  originalTextMarkdown: 'Markdown text 1'
}

const defaultGlobal = {
  stubs: {
    NuxtLink: { template: '<a><slot /></a>' },
    AppSelect: true,
    AppModal: { template: '<div><slot /><slot name="footer" /></div>' }
  },
  mocks: {
    $t: (key: string, params?: Record<string, any>) => params ? `${key}:${JSON.stringify(params)}` : key
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  const categories = useCategoryStore()
  categories.categories = [category]
  categories.hasLoaded = true
  vi.spyOn(categories, 'ensureCategories').mockResolvedValue(undefined)
})
describe('catalog real wire behavior', () => {
  it.each([CatalogCard, CatalogPreviewModal])('blocks new adoption under fresh inactive metadata but retains owned reading', async (component) => {
    useCategoryStore().categories = [{ ...category, isActive: false }]
    const document = { id: 'doc', title: 'Guide', description: '', categoryId: category.id, category, language: 'en', sourceType: 'PdfBook' as const, attributionText: '', totalSlices: 1, estimatedReadMinutes: 5, authorOrSourceUrl: null, publishedAt: null, existingBookId: null, tableOfContents: [], firstSlicePreview: null }
    const wrapper = mount(component, { props: { document, open: true }, global: defaultGlobal })
    expect(wrapper.text()).toContain('api_errors.Category.Inactive')
    const adopt = wrapper.findAll('button').find(button => button.text().includes('categories.inactive'))!
    expect(adopt.attributes('disabled')).toBeDefined()
    await adopt.trigger('click')
    expect(wrapper.emitted('adopt')).toBeUndefined()
    await wrapper.setProps({ document: { ...document, existingBookId: 'owned-copy' } })
    expect(wrapper.text()).toContain('library.discover.open_book')
    expect(wrapper.text()).not.toContain('api_errors.Category.Inactive')
  })

  it('passes the selected replacement PDF to retry processing', async () => {
    const wrapper = mount(AdminCatalogDocumentDetail, {
      props: { document: { ...mockDoc, sourceType: 'PdfBook', processingStatus: 'Failed' }, isSavingMetadata: false, isPreparingSlices: false, isPublishing: false, isHiding: false, isRetrying: false },
      global: defaultGlobal
    })
    const file = new File(['%PDF-replacement'], 'replacement.pdf', { type: 'application/pdf' })
    const input = wrapper.find('input[type="file"]')
    Object.defineProperty(input.element, 'files', { value: [file] })
    await input.trigger('change')
    await wrapper.findAll('button').find(button => button.text().includes('admin_catalog.retry_preparation'))!.trigger('click')
    expect(wrapper.emitted('retry-processing')?.[0]).toEqual([file])
  })

  it('renders the actual markdownContent URL preview', async () => {
    vi.spyOn(useCatalogStore(), 'previewUrl').mockResolvedValue({
      title: 'Preview title', sourceUrl: 'https://example.test/article',
      markdownContent: '# Actual article body', estimatedWordCount: 4,
      isPdfDetected: false, detectedPdfUrl: null
    })
    const wrapper = mount(AdminCatalogDraftForm, { global: defaultGlobal })
    await wrapper.findAll('button').find(button => button.text().includes('admin_catalog.import_url'))!.trigger('click')
    await wrapper.find('input[type="url"]').setValue('https://example.test/article')
    await wrapper.findAll('button').find(button => button.text().includes('admin_catalog.preview_url'))!.trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('# Actual article body')
  })

  it('opens the adopted owned copy using existingBookId', () => {
    const wrapper = mount(CatalogCard, {
      props: { document: { id: 'doc', title: 'Guide', description: '', categoryId: category.id, category, language: 'en', sourceType: 'PdfBook', attributionText: '', totalSlices: 1, estimatedReadMinutes: 5, authorOrSourceUrl: null, publishedAt: null, existingBookId: 'owned-book-id' } },
      global: { ...defaultGlobal, stubs: { ...defaultGlobal.stubs, NuxtLink: { props: ['to'], template: '<a :href="to.path"><slot /></a>' } } }
    })
    expect(wrapper.find('[data-testid="catalog-card-open-book-btn"]').attributes('href')).toBe('/read/owned-book-id')
    expect(wrapper.find('[data-testid="catalog-card-adopt-btn"]').exists()).toBe(false)
  })
})


describe('CatalogCard category metadata', () => {
  it('prefers fresh shared metadata by UUID and falls back to the stored descriptor', async () => {
    const categories = useCategoryStore()
    categories.categories = [{ ...category, nameEn: 'Renamed category', isActive: false }]
    const wrapper = mount(CatalogCard, {
      props: { document: { id: 'doc', title: 'Guide', description: '', categoryId: category.id, category, language: 'en', sourceType: 'PdfBook', attributionText: '', totalSlices: 1, estimatedReadMinutes: 5, authorOrSourceUrl: null, publishedAt: null, existingBookId: null } },
      global: defaultGlobal
    })
    expect(wrapper.text()).toContain('Renamed category')
    categories.categories = []
    await wrapper.vm.$nextTick()
    expect(wrapper.text()).toContain('Novel category')
  })
})

describe('Catalog category assignment rejection', () => {
  it('refreshes choices without discarding the draft or selected UUID', async () => {
    const categories = useCategoryStore()
    const refresh = vi.spyOn(categories, 'fetchCategories').mockResolvedValue([])
    vi.spyOn(useCatalogStore(), 'importUrl').mockRejectedValue({ code: 'Category.Inactive', message: 'Inactive' })
    const wrapper = mount(AdminCatalogDraftForm, { global: defaultGlobal })
    await wrapper.findAll('button').find(b => b.text().includes('admin_catalog.import_url'))!.trigger('click')
    const title = wrapper.findAll('input[type="text"]').find(i => i.attributes('placeholder') === 'admin_catalog.form.title_placeholder')!
    await title.setValue('Retained draft')
    await wrapper.find('input[type="url"]').setValue('https://example.test/guide')
    const select = wrapper.findAllComponents({ name: 'AppSelect' })[0]!
    select.vm.$emit('update:modelValue', category.id)
    await wrapper.vm.$nextTick()
    await wrapper.findAll('button').find(b => b.text().includes('admin_catalog.create_draft_btn'))!.trigger('click')
    await wrapper.vm.$nextTick()
    expect(refresh).toHaveBeenCalledWith(true)
    expect((title.element as HTMLInputElement).value).toBe('Retained draft')
    expect((wrapper.find('input[type="url"]').element as HTMLInputElement).value).toBe('https://example.test/guide')
    expect(select.props('modelValue')).toBe(category.id)
  })
})
describe('AdminCatalogHeader.vue', () => {
  it('emits refresh and new-draft events on button clicks', async () => {
    const wrapper = mount(AdminCatalogHeader, {
      props: { isLoading: false },
      global: defaultGlobal
    })

    const refreshBtn = wrapper.find('[data-testid="admin-catalog-refresh-btn"]')
    expect(refreshBtn.exists()).toBe(true)
    await refreshBtn.trigger('click')
    expect(wrapper.emitted('refresh')).toBeTruthy()

    const newDraftBtn = wrapper.find('[data-testid="admin-catalog-new-draft-btn"]')
    expect(newDraftBtn.exists()).toBe(true)
    await newDraftBtn.trigger('click')
    expect(wrapper.emitted('new-draft')).toBeTruthy()
  })
})

describe('AdminCatalogNavRail.vue', () => {
  it('renders document items and emits select-document', async () => {
    const wrapper = mount(AdminCatalogNavRail, {
      props: {
        documents: [mockDoc],
        selectedDocId: null,
        isCreatingNewDraft: false,
        isLoading: false,
        search: '',
        visibilityFilter: '',
        statusFilter: ''
      },
      global: defaultGlobal
    })

    expect(wrapper.text()).toContain('High Performance Browser Networking')
    const card = wrapper.find('button')
    await card.trigger('click')
    expect(wrapper.emitted('select-document')).toBeTruthy()
    expect(wrapper.emitted('select-document')![0]).toEqual(['doc-1'])
  })

  it('displays loading indicator and empty match messages properly', () => {
    const loadingWrapper = mount(AdminCatalogNavRail, {
      props: {
        documents: [],
        selectedDocId: null,
        isCreatingNewDraft: false,
        isLoading: true,
        search: '',
        visibilityFilter: '',
        statusFilter: ''
      },
      global: defaultGlobal
    })
    expect(loadingWrapper.text()).toContain('admin_catalog.filters.loading_catalog')

    const emptyWrapper = mount(AdminCatalogNavRail, {
      props: {
        documents: [],
        selectedDocId: null,
        isCreatingNewDraft: false,
        isLoading: false,
        search: 'non-existent',
        visibilityFilter: '',
        statusFilter: ''
      },
      global: defaultGlobal
    })
    expect(emptyWrapper.text()).toContain('admin_catalog.filters.no_documents_match')
  })
})

describe('AdminCatalogDraftForm.vue', () => {
  it('emits cancel event when cancel button is clicked', async () => {
    const wrapper = mount(AdminCatalogDraftForm, {
      global: defaultGlobal
    })

    const cancelBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.cancel'))
    expect(cancelBtn).toBeDefined()
    await cancelBtn!.trigger('click')
    expect(wrapper.emitted('cancel')).toBeTruthy()
  })

  it('submits web URL draft with correct contract payload when target URL is provided via previewUrlInput', async () => {
    const store = useCatalogStore()
    const mockImport = vi.fn().mockResolvedValue({ id: 'doc-imported-1' })
    store.importUrl = mockImport

    const wrapper = mount(AdminCatalogDraftForm, {
      global: defaultGlobal
    })

    // Switch to URL mode
    const urlModeBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.import_url'))
    expect(urlModeBtn).toBeDefined()
    await urlModeBtn!.trigger('click')

    // Set title and preview URL
    const titleInput = wrapper.findAll('input[type="text"]').find(i => i.attributes('placeholder') === 'admin_catalog.form.title_placeholder')
    expect(titleInput).toBeDefined()
    await titleInput!.setValue('Next.js Documentation')

    const urlInput = wrapper.find('input[type="url"]')
    expect(urlInput.exists()).toBe(true)
    await urlInput.setValue('https://nextjs.org/docs')
    wrapper.findAllComponents({ name: 'AppSelect' })[0]!.vm.$emit('update:modelValue', category.id)
    await wrapper.vm.$nextTick()

    // Submit
    const submitBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.create_draft_btn'))
    expect(submitBtn).toBeDefined()
    await submitBtn!.trigger('click')

    expect(mockImport).toHaveBeenCalledWith({
      url: 'https://nextjs.org/docs',
      title: 'Next.js Documentation',
      description: 'Next.js Documentation',
      categoryId: category.id,
      language: 'en',
      authorOrSourceUrl: undefined,
      attributionText: undefined
    })
    expect(wrapper.emitted('created')).toBeTruthy()
    expect(wrapper.emitted('created')![0]).toEqual(['doc-imported-1'])
  })

  it('submits web URL draft using sourceUrl and attributionText when metadata fields are filled', async () => {
    const store = useCatalogStore()
    const mockImport = vi.fn().mockResolvedValue({ id: 'doc-imported-2' })
    store.importUrl = mockImport

    const wrapper = mount(AdminCatalogDraftForm, {
      global: defaultGlobal
    })

    // Switch to URL mode
    const urlModeBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.import_url'))
    await urlModeBtn!.trigger('click')

    // Set title
    const titleInput = wrapper.findAll('input[type="text"]').find(i => i.attributes('placeholder') === 'admin_catalog.form.title_placeholder')
    await titleInput!.setValue('Next.js Documentation')

    // Set metadata sourceUrl and attribution
    const sourceUrlInput = wrapper.findAll('input[type="text"]').find(i => i.attributes('placeholder') === 'admin_catalog.form.url_placeholder')
    expect(sourceUrlInput).toBeDefined()
    await sourceUrlInput!.setValue('https://nextjs.org/docs')

    const attributionInput = wrapper.findAll('input[type="text"]').find(i => i.attributes('placeholder') === 'admin_catalog.form.attribution_placeholder')
    expect(attributionInput).toBeDefined()
    await attributionInput!.setValue('Vercel')
    wrapper.findAllComponents({ name: 'AppSelect' })[0]!.vm.$emit('update:modelValue', category.id)
    await wrapper.vm.$nextTick()

    // Submit
    const submitBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.create_draft_btn'))
    await submitBtn!.trigger('click')

    expect(mockImport).toHaveBeenCalledWith({
      url: 'https://nextjs.org/docs',
      title: 'Next.js Documentation',
      description: 'Next.js Documentation',
      categoryId: category.id,
      language: 'en',
      authorOrSourceUrl: 'https://nextjs.org/docs',
      attributionText: 'Vercel'
    })
    expect(wrapper.emitted('created')![0]).toEqual(['doc-imported-2'])
  })

  it('blocks submission when target URL is missing in web mode', async () => {
    const store = useCatalogStore()
    const mockImport = vi.fn()
    store.importUrl = mockImport

    const wrapper = mount(AdminCatalogDraftForm, {
      global: defaultGlobal
    })

    // Switch to URL mode
    const urlModeBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.import_url'))
    await urlModeBtn!.trigger('click')

    // Set title only
    const titleInput = wrapper.findAll('input[type="text"]').find(i => i.attributes('placeholder') === 'admin_catalog.form.title_placeholder')
    await titleInput!.setValue('Next.js Documentation')

    // Submit without URL
    const submitBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.create_draft_btn'))
    await submitBtn!.trigger('click')

    expect(mockImport).not.toHaveBeenCalled()
    expect(wrapper.emitted('created')).toBeFalsy()
  })

  it('submits web URL draft with active category selected from categoryStore', async () => {
    const categoryStore = useCategoryStore()
    categoryStore.categories = [
      { ...category }
    ]

    const store = useCatalogStore()
    const mockImport = vi.fn().mockResolvedValue({ id: 'doc-imported-cat' })
    store.importUrl = mockImport

    const wrapper = mount(AdminCatalogDraftForm, {
      global: defaultGlobal
    })

    // Switch to URL mode
    const urlModeBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.import_url'))
    await urlModeBtn!.trigger('click')

    const titleInput = wrapper.findAll('input[type="text"]').find(i => i.attributes('placeholder') === 'admin_catalog.form.title_placeholder')
    await titleInput!.setValue('Distributed Systems Guide')

    const urlInput = wrapper.find('input[type="url"]')
    await urlInput.setValue('https://dist-sys.com/docs')
    wrapper.findAllComponents({ name: 'AppSelect' })[0]!.vm.$emit('update:modelValue', category.id)
    await wrapper.vm.$nextTick()

    const submitBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.create_draft_btn'))
    await submitBtn!.trigger('click')

    expect(mockImport).toHaveBeenCalledWith(expect.objectContaining({
      url: 'https://dist-sys.com/docs',
      title: 'Distributed Systems Guide',
      categoryId: category.id
    }))
    expect(wrapper.emitted('created')).toBeTruthy()
  })

  it('fetches novel metadata on direct mount without selecting it implicitly', async () => {
    const categories = useCategoryStore()
    categories.categories = []
    categories.hasLoaded = false
    vi.mocked(categories.ensureCategories).mockImplementation(async () => {
      categories.categories = [category]
      categories.hasLoaded = true
    })
    const wrapper = mount(AdminCatalogDraftForm, { global: defaultGlobal })
    await wrapper.vm.$nextTick()
    expect(categories.ensureCategories).toHaveBeenCalled()
    const select = wrapper.findAllComponents({ name: 'AppSelect' })[0]!
    expect(select.props('options')).toEqual([{ value: category.id, label: 'Novel category' }])
    expect(select.props('modelValue')).toBe('')
    expect(wrapper.findAll('button').find(b => b.text().includes('admin_catalog.create_draft_btn'))!.attributes('disabled')).toBeDefined()
  })

  it.each([null, 'Unavailable'])('blocks creation with no active categories or metadata error (%s)', async (error) => {
    const categories = useCategoryStore()
    categories.categories = []
    categories.error = error
    const store = useCatalogStore()
    const create = vi.spyOn(store, 'importUrl')
    const wrapper = mount(AdminCatalogDraftForm, { global: defaultGlobal })
    await wrapper.vm.$nextTick()
    expect(wrapper.text()).toContain(error ? 'categories.metadataUnavailable' : 'categories.noActiveCategories')
    expect(wrapper.findAll('button').find(b => b.text().includes('admin_catalog.create_draft_btn'))!.attributes('disabled')).toBeDefined()
    expect(create).not.toHaveBeenCalled()
    if (error) {
      await wrapper.findAll('button').find(b => b.text().includes('categories.retry'))!.trigger('click')
      expect(categories.ensureCategories).toHaveBeenCalledTimes(2)
    }
  })
})

describe('AdminCatalogSlicesReview.vue', () => {
  it('renders slices list and slices-ready summary when slices exist', async () => {
    const wrapper = mount(AdminCatalogSlicesReview, {
      props: {
        document: mockDoc,
        isPreparingSlices: false
      },
      global: defaultGlobal
    })

    expect(wrapper.findAll('button[title]')).toHaveLength(mockDoc.totalSlices)
    expect(wrapper.text()).toContain('admin_catalog.slices_ready_summary')

    // Prepare button must NOT be rendered when document is ready or has slices
    const buttons = wrapper.findAll('button')
    const prepareBtn = buttons.find(b => b.text().includes('admin_catalog.prepare_action'))
    expect(prepareBtn).toBeUndefined()

    // Inspect slice button
    const inspectBtn = wrapper.find('button[title]')
    await inspectBtn.trigger('click')
    expect(wrapper.emitted('inspect-slice')).toBeTruthy()
    expect(wrapper.emitted('inspect-slice')![0]).toEqual([1])
  })

  it('renders prepare initial slices button when document has 0 slices and status is not Ready', async () => {
    const unextractedDoc: AdminCatalogDocument = {
      ...mockDoc,
      processingStatus: 'Pending',
      totalSlices: 0
    }
    const wrapper = mount(AdminCatalogSlicesReview, {
      props: {
        document: unextractedDoc,
        isPreparingSlices: false
      },
      global: defaultGlobal
    })

    const buttons = wrapper.findAll('button')
    const prepareBtn = buttons.find(b => b.text().includes('admin_catalog.prepare_action'))
    expect(prepareBtn).toBeDefined()
    await prepareBtn!.trigger('click')
    expect(wrapper.emitted('prepare-slices')).toBeTruthy()
  })
})

describe('AdminCatalogDocumentDetail.vue', () => {
  it('renders document overview, metadata editor, and publication controls for Draft document', async () => {
    const wrapper = mount(AdminCatalogDocumentDetail, {
      props: {
        document: mockDoc,
        isSavingMetadata: false,
        isPreparingSlices: false,
        isPublishing: false,
        isHiding: false,
        isRetrying: false
      },
      global: defaultGlobal
    })

    expect(wrapper.text()).toContain('High Performance Browser Networking')
    expect(wrapper.text()).toContain('https://hpbn.co')

    // Save metadata is enabled for draft
    const saveBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.save_changes'))
    expect(saveBtn).toBeDefined()
    expect(saveBtn!.attributes('disabled')).toBeUndefined()
    await saveBtn!.trigger('click')
    expect(wrapper.emitted('save-metadata')).toBeTruthy()

    // Publish button is rendered in Draft mode
    const publishBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.publish_action'))
    expect(publishBtn).toBeDefined()
  })

  it('retains the current inactive UUID during metadata edits', async () => {
    const docWithInactiveCategory = {
      ...mockDoc,
      category: { ...category, isActive: false }
    }
    useCategoryStore().categories = []
    const wrapper = mount(AdminCatalogDocumentDetail, {
      props: {
        document: docWithInactiveCategory,
        isSavingMetadata: false,
        isPreparingSlices: false,
        isPublishing: false,
        isHiding: false,
        isRetrying: false
      },
      global: defaultGlobal
    })

    expect(wrapper.findAllComponents({ name: 'AppSelect' })[0]!.props('options')).toEqual([
      { value: category.id, label: expect.stringContaining('Novel category') }
    ])
    const saveBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.save_changes'))
    await saveBtn!.trigger('click')
    expect(wrapper.emitted('save-metadata')![0][0]).toMatchObject({
      categoryId: category.id
    })
    expect(wrapper.emitted('save-metadata')![0][0]).not.toHaveProperty('category')
  })

  it('enforces read-only metadata state, disabled save, notice banner, and hides Publish button when Published', async () => {
    const publishedDoc: AdminCatalogDocument = {
      ...mockDoc,
      visibility: 'Published',
      isEligibleForPublication: true,
      unmetPublicationRequirements: []
    }
    const wrapper = mount(AdminCatalogDocumentDetail, {
      props: {
        document: publishedDoc,
        isSavingMetadata: false,
        isPreparingSlices: false,
        isPublishing: false,
        isHiding: false,
        isRetrying: false
      },
      global: defaultGlobal
    })

    // Notice banner is rendered
    expect(wrapper.find('[data-testid="admin-catalog-published-notice"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('admin_catalog.metadata_published_readonly')

    // Save button is disabled
    const saveBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.save_changes'))
    expect(saveBtn).toBeDefined()
    expect(saveBtn!.attributes('disabled')).toBeDefined()

    // Input fields are disabled
    const titleInput = wrapper.find('input[type="text"]')
    expect(titleInput.attributes('disabled')).toBeDefined()

    // Publish button is hidden on Published documents
    const publishBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.publish_action'))
    expect(publishBtn).toBeUndefined()

    // Hide button is rendered
    const hideBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.hide_action'))
    expect(hideBtn).toBeDefined()
  })
})

describe('AdminCatalogSliceModal.vue', () => {
  it('renders slice details and emits close event', async () => {
    const wrapper = mount(AdminCatalogSliceModal, {
      props: {
        open: true,
        slice: mockSlice
      },
      global: defaultGlobal
    })

    expect(wrapper.text()).toContain('Primer on Web Performance')
    expect(wrapper.text()).toContain('Summary 1')
    expect(wrapper.text()).toContain('Markdown text 1')

    const closeBtn = wrapper.find('button')
    await closeBtn.trigger('click')
    expect(wrapper.emitted('close')).toBeTruthy()
  })
})
