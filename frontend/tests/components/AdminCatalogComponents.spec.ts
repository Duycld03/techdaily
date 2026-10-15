import { describe, it, expect, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import AdminCatalogHeader from '~/components/catalog/admin/AdminCatalogHeader.vue'
import AdminCatalogNavRail from '~/components/catalog/admin/AdminCatalogNavRail.vue'
import AdminCatalogDraftForm from '~/components/catalog/admin/AdminCatalogDraftForm.vue'
import AdminCatalogSlicesReview from '~/components/catalog/admin/AdminCatalogSlicesReview.vue'
import AdminCatalogDocumentDetail from '~/components/catalog/admin/AdminCatalogDocumentDetail.vue'
import AdminCatalogSliceModal from '~/components/catalog/admin/AdminCatalogSliceModal.vue'
import { useCatalogStore, type AdminCatalogDocumentDto, type AdminCatalogSliceDto } from '~/stores/useCatalogStore'

const mockDoc: AdminCatalogDocumentDto = {
  id: 'doc-1',
  title: 'High Performance Browser Networking',
  slug: 'high-performance-browser-networking',
  category: 1,
  language: 'en',
  authorOrSourceUrl: 'https://hpbn.co',
  attributionText: 'Ilya Grigorik',
  permissionBasis: 'CC-BY-NC-ND',
  permissionEvidence: 'Public Book',
  confirmedDistributionRights: true,
  confirmedAiProcessingRights: true,
  visibility: 'Draft',
  processingStatus: 'Ready',
  progressPercentage: 100,
  statusMessage: 'Ready',
  processingErrorMessage: null,
  totalSlices: 5,
  estimatedReadMinutes: 25,
  revision: 2,
  concurrencyVersion: 1,
  reviewedByUserId: null,
  reviewedAt: null,
  isEligibleForPublication: false,
  unmetPublicationRequirements: ['Administrator review is required before publication.'],
  slices: [
    {
      id: 'slice-1',
      sliceOrder: 1,
      chapterTitle: 'Primer on Web Performance',
      isAiFormatted: true,
      estimatedReadMinutes: 5,
      summaryMarkdown: 'Summary 1',
      originalTextMarkdown: 'Markdown text 1'
    },
    {
      id: 'slice-2',
      sliceOrder: 2,
      chapterTitle: 'TCP Optimization',
      isAiFormatted: false,
      estimatedReadMinutes: 6,
      summaryMarkdown: null,
      originalTextMarkdown: 'Markdown text 2'
    }
  ],
  createdAt: '2026-10-14T00:00:00Z',
  updatedAt: '2026-10-14T00:00:00Z'
}

const mockSlice: AdminCatalogSliceDto = {
  id: 'slice-1',
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

    // Submit
    const submitBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.create_draft_btn'))
    expect(submitBtn).toBeDefined()
    await submitBtn!.trigger('click')

    expect(mockImport).toHaveBeenCalledWith({
      url: 'https://nextjs.org/docs',
      title: 'Next.js Documentation',
      description: 'Next.js Documentation',
      category: 0,
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

    // Submit
    const submitBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.create_draft_btn'))
    await submitBtn!.trigger('click')

    expect(mockImport).toHaveBeenCalledWith({
      url: 'https://nextjs.org/docs',
      title: 'Next.js Documentation',
      description: 'Next.js Documentation',
      category: 0,
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

    expect(wrapper.text()).toContain('Primer on Web Performance')
    expect(wrapper.text()).toContain('TCP Optimization')
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
    const unextractedDoc: AdminCatalogDocumentDto = {
      ...mockDoc,
      processingStatus: 'Pending',
      slices: []
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

  it('normalizes string enum category to numeric index', async () => {
    const docWithStringEnum = {
      ...mockDoc,
      category: 'SystemDesign' as unknown as number
    }
    const wrapper = mount(AdminCatalogDocumentDetail, {
      props: {
        document: docWithStringEnum,
        isSavingMetadata: false,
        isPreparingSlices: false,
        isPublishing: false,
        isHiding: false,
        isRetrying: false
      },
      global: defaultGlobal
    })

    // Trigger save and ensure category is normalized to 3 (SystemDesign)
    const saveBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.save_changes'))
    await saveBtn!.trigger('click')
    expect(wrapper.emitted('save-metadata')![0][0]).toMatchObject({
      category: 3
    })
  })

  it('enforces read-only metadata state, disabled save, notice banner, and hides Publish button when Published', async () => {
    const publishedDoc: AdminCatalogDocumentDto = {
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
