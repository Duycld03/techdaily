import { describe, it, expect, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import AdminCatalogHeader from '~/components/catalog/admin/AdminCatalogHeader.vue'
import AdminCatalogNavRail from '~/components/catalog/admin/AdminCatalogNavRail.vue'
import AdminCatalogDraftForm from '~/components/catalog/admin/AdminCatalogDraftForm.vue'
import AdminCatalogSlicesReview from '~/components/catalog/admin/AdminCatalogSlicesReview.vue'
import AdminCatalogDocumentDetail from '~/components/catalog/admin/AdminCatalogDocumentDetail.vue'
import AdminCatalogSliceModal from '~/components/catalog/admin/AdminCatalogSliceModal.vue'
import type { AdminCatalogDocumentDto, AdminCatalogSliceDto } from '~/stores/useCatalogStore'

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
})

describe('AdminCatalogSlicesReview.vue', () => {
  it('renders slices list and emits action events', async () => {
    const wrapper = mount(AdminCatalogSlicesReview, {
      props: {
        document: mockDoc,
        isPreparingSlices: false,
        isReviewing: false
      },
      global: defaultGlobal
    })

    expect(wrapper.text()).toContain('Primer on Web Performance')
    expect(wrapper.text()).toContain('TCP Optimization')

    // Find prepare and review buttons
    const buttons = wrapper.findAll('button')
    const prepareBtn = buttons.find(b => b.text().includes('admin_catalog.prepare_action'))
    expect(prepareBtn).toBeDefined()
    await prepareBtn!.trigger('click')
    expect(wrapper.emitted('prepare-slices')).toBeTruthy()

    const reviewBtn = buttons.find(b => b.text().includes('admin_catalog.review_confirm'))
    expect(reviewBtn).toBeDefined()
    await reviewBtn!.trigger('click')
    expect(wrapper.emitted('confirm-review')).toBeTruthy()

    // Inspect slice button
    const inspectBtn = wrapper.find('button[title]')
    await inspectBtn.trigger('click')
    expect(wrapper.emitted('inspect-slice')).toBeTruthy()
    expect(wrapper.emitted('inspect-slice')![0]).toEqual([1])
  })
})

describe('AdminCatalogDocumentDetail.vue', () => {
  it('renders document overview, metadata editor, and publication controls', async () => {
    const wrapper = mount(AdminCatalogDocumentDetail, {
      props: {
        document: mockDoc,
        isSavingMetadata: false,
        isPreparingSlices: false,
        isReviewing: false,
        isPublishing: false,
        isHiding: false,
        isRetrying: false
      },
      global: defaultGlobal
    })

    expect(wrapper.text()).toContain('High Performance Browser Networking')
    expect(wrapper.text()).toContain('https://hpbn.co')
    expect(wrapper.text()).toContain('Administrator review is required before publication.')

    // Save metadata
    const saveBtn = wrapper.findAll('button').find(b => b.text().includes('admin_catalog.save_changes'))
    expect(saveBtn).toBeDefined()
    await saveBtn!.trigger('click')
    expect(wrapper.emitted('save-metadata')).toBeTruthy()
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
