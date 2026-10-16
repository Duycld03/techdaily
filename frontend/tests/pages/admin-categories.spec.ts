import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import AdminCategoriesPage from '../../app/pages/admin/categories.vue'
import AdminCategoryModal from '../../app/components/admin/AdminCategoryModal.vue'
import { useAdminAccessStore } from '../../app/stores/useAdminAccessStore'
import {
  useCategoryStore,
  type CategoryDescriptor,
  type AdminCategoryDetail
} from '../../app/stores/useCategoryStore'
import { useAuthStore, type AuthUser } from '../../app/stores/useAuthStore'

describe('AdminCategoriesPage (categories.vue)', () => {
  const mockCategories: CategoryDescriptor[] = [
    {
      id: '10000000-0000-4000-8000-000000000000',
      slug: 'frontend-web',
      nameEn: 'Frontend & Web',
      nameVi: 'Frontend & Web',
      sortOrder: 0,
      isActive: true,
      version: 1
    },
    {
      id: '10000000-0000-4000-8000-000000000001',
      slug: 'backend-runtime',
      nameEn: 'Backend & Runtime',
      nameVi: 'Backend & Runtime',
      sortOrder: 1,
      isActive: true,
      version: 2
    },
    {
      id: '10000000-0000-4000-8000-000000000002',
      slug: 'database-storage',
      nameEn: 'Database & Storage',
      nameVi: 'Cơ Sở Dữ Liệu & Lưu Trữ',
      sortOrder: 2,
      isActive: false,
      version: 3
    }
  ]

  const mockDetail: AdminCategoryDetail = {
    ...mockCategories[0],
    canDelete: true,
    usageCount: 0
  }

  const mockInUseDetail: AdminCategoryDetail = {
    ...mockCategories[1],
    canDelete: false,
    usageCount: 15
  }

  beforeEach(() => {
    setActivePinia(createPinia())
    const authStore = useAuthStore()
    authStore.user = {
      id: 'admin-caller-id',
      email: 'admin@example.com',
      name: 'Admin User'
    } as unknown as AuthUser

    const accessStore = useAdminAccessStore()
    accessStore.capabilities = {
      canViewAdminDashboard: true,
      canManageCatalog: true,
      canManageAccounts: true,
      canReadAudit: true,
      canManageDocumentOperations: true,
      canManageRoles: true,
      canManageCategories: true
    }

    const categoryStore = useCategoryStore()
    categoryStore.categories = [...mockCategories]
    categoryStore.fetchCategories = vi.fn().mockImplementation(async () => {
      categoryStore.categories = [...mockCategories]
      return categoryStore.categories
    })
    categoryStore.fetchCategoryDetail = vi.fn().mockImplementation(async (id: string) => {
      if (id === mockCategories[1].id) return mockInUseDetail
      return { ...mockDetail, id }
    })
    categoryStore.createCategory = vi.fn().mockImplementation(async (payload) => {
      const created: CategoryDescriptor = {
        ...payload,
        id: 'new-created-id',
        version: 1
      }
      categoryStore.categories = [...categoryStore.categories, created]
      return created
    })
    categoryStore.updateCategory = vi.fn().mockImplementation(async (id, payload) => {
      const existing = categoryStore.getCategoryById(id)
      const updated: CategoryDescriptor = {
        id,
        slug: existing ? existing.slug : 'unknown',
        nameEn: payload.nameEn,
        nameVi: payload.nameVi,
        sortOrder: payload.sortOrder,
        isActive: payload.isActive,
        version: payload.expectedVersion + 1
      }
      categoryStore.categories = categoryStore.categories.map(c => c.id === id ? updated : c)
      return updated
    })
    categoryStore.deleteCategory = vi.fn().mockImplementation(async (id) => {
      categoryStore.categories = categoryStore.categories.filter(c => c.id !== id)
    })
  })

  function createWrapper() {
    return mount(AdminCategoriesPage, {
      global: {
        stubs: {
          Teleport: true,
          NuxtLink: {
            template: '<a><slot /></a>'
          }
        }
      }
    })
  }

  it('renders categories list and loads the first category into detail view by default', async () => {
    const wrapper = createWrapper()
    await flushPromises()

    const listItems = wrapper.findAll('[data-testid="category-list-item"]')
    expect(listItems.length).toBe(3)
    expect(wrapper.text()).toContain('frontend-web')
    expect(wrapper.text()).toContain('backend-runtime')
    expect(wrapper.text()).toContain('database-storage')

    // Detail panel displays selected first category
    expect(wrapper.text()).toContain('Frontend & Web')
    expect(wrapper.text()).toContain('v1')
    wrapper.unmount()
  })

  it('filters category list by search query matching name or slug', async () => {
    const wrapper = createWrapper()
    await flushPromises()

    const searchInput = wrapper.find('input[placeholder="Search categories..."]')
    await searchInput.setValue('backend')
    await flushPromises()

    const listItems = wrapper.findAll('[data-testid="category-list-item"]')
    expect(listItems.length).toBe(1)
    expect(listItems[0].text()).toContain('backend-runtime')
    expect(listItems.some(i => i.text().includes('frontend-web'))).toBe(false)
    wrapper.unmount()
  })

  it('filters category list by active and inactive status chips', async () => {
    const wrapper = createWrapper()
    await flushPromises()

    // Click Inactive filter
    const statusButtons = wrapper.findAll('div[role="group"] button')
    const inactiveBtn = statusButtons.find(b => b.text().includes('admin.categories.status.inactive') || b.text().includes('Inactive'))
    expect(inactiveBtn).toBeDefined()
    await inactiveBtn!.trigger('click')
    await flushPromises()

    const listItems = wrapper.findAll('[data-testid="category-list-item"]')
    expect(listItems.length).toBe(1)
    expect(listItems[0].text()).toContain('database-storage')
    expect(listItems.some(i => i.text().includes('frontend-web'))).toBe(false)
    wrapper.unmount()
  })

  it('switches selection and fetches detail when another category is clicked', async () => {
    const categoryStore = useCategoryStore()
    const wrapper = createWrapper()
    await flushPromises()

    const listItems = wrapper.findAll('[data-testid="category-list-item"]')
    await listItems[1].trigger('click')
    await flushPromises()

    expect(categoryStore.fetchCategoryDetail).toHaveBeenCalledWith('10000000-0000-4000-8000-000000000001')
    expect(wrapper.text()).toContain('Backend & Runtime')
    expect(wrapper.text()).toContain('v2')
    wrapper.unmount()
  })

  it('exposes category selection as a native button with its selected state', async () => {
    const wrapper = createWrapper()
    await flushPromises()
    const choice = wrapper.find('button[data-testid="category-list-item"]:nth-of-type(2)')

    expect(choice.exists()).toBe(true)
    await choice.trigger('click')
    await flushPromises()

    expect(choice.attributes('aria-pressed')).toBe('true')
    expect(wrapper.find('h2').text()).toBe('Backend & Runtime')
    wrapper.unmount()
  })

  it('does not enable deletion when category usage detail is unavailable', async () => {
    useCategoryStore().fetchCategoryDetail = vi.fn().mockRejectedValue(new Error('Unavailable'))
    const wrapper = createWrapper()
    await flushPromises()

    const deleteButton = wrapper.findAll('button')
      .find(button => button.text().includes('admin.categories.actions.delete'))
    expect(deleteButton?.attributes('disabled')).toBeDefined()
    wrapper.unmount()
  })

  it('discards an earlier detail response after selecting another category', async () => {
    let resolveEarlier!: (detail: AdminCategoryDetail) => void
    const earlier = new Promise<AdminCategoryDetail>(resolve => { resolveEarlier = resolve })
    const store = useCategoryStore()
    store.fetchCategoryDetail = vi.fn().mockImplementation((id: string) =>
      id === mockCategories[0].id ? earlier : Promise.resolve(mockInUseDetail))
    const wrapper = createWrapper()
    await flushPromises()
    await wrapper.findAll('[data-testid="category-list-item"]')[1].trigger('click')
    await flushPromises()

    resolveEarlier(mockDetail)
    await flushPromises()

    const deleteButton = wrapper.findAll('button')
      .find(button => button.text().includes('admin.categories.actions.delete'))
    expect(wrapper.find('h2').text()).toBe('Backend & Runtime')
    expect(deleteButton?.attributes('disabled')).toBeDefined()
    wrapper.unmount()
  })

  it('disables delete button and shows in-use warning when canDelete is false', async () => {
    const wrapper = createWrapper()
    await flushPromises()

    // Select second category (mockInUseDetail has canDelete: false)
    const listItems = wrapper.findAll('[data-testid="category-list-item"]')
    await listItems[1].trigger('click')
    await flushPromises()

    const deleteBtn = wrapper.findAll('button').find(b => b.text().includes('admin.categories.actions.delete'))
    expect(deleteBtn).toBeDefined()
    expect(deleteBtn!.attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('15') // usageCount
    wrapper.unmount()
  })

  it('opens create modal on clicking New Category and validates inputs', async () => {
    const wrapper = createWrapper()
    await flushPromises()

    const newBtn = wrapper.findAll('button').find(b => b.text().includes('admin.categories.newCategory'))
    expect(newBtn).toBeDefined()
    await newBtn!.trigger('click')
    await flushPromises()

    // Modal dialog is open
    const modal = wrapper.find('[role="dialog"]')
    expect(modal.exists()).toBe(true)

    // Submit without filling fields -> triggers validation
    const submitBtn = modal.findAll('button').find(b => b.text().includes('admin.categories.create'))
    await submitBtn!.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Slug is required.')
    expect(wrapper.text()).toContain('English name is required.')
    expect(wrapper.text()).toContain('Vietnamese name is required.')
    wrapper.unmount()
  })

  it('submits valid create category payload', async () => {
    const categoryStore = useCategoryStore()
    const wrapper = createWrapper()
    await flushPromises()

    const newBtn = wrapper.findAll('button').find(b => b.text().includes('admin.categories.newCategory'))
    await newBtn!.trigger('click')
    await flushPromises()

    await wrapper.find('#category-slug-input').setValue('distributed-systems')
    await wrapper.find('#category-name-en-input').setValue('Distributed Systems')
    await wrapper.find('#category-name-vi-input').setValue('Hệ Thống Phân Tán')
    await wrapper.find('#category-sort-order-input').setValue('3')

    const submitBtn = wrapper.find('[role="dialog"]').findAll('button').find(b => b.text().includes('admin.categories.create'))
    await submitBtn!.trigger('click')
    await flushPromises()

    expect(categoryStore.createCategory).toHaveBeenCalledWith({
      slug: 'distributed-systems',
      nameEn: 'Distributed Systems',
      nameVi: 'Hệ Thống Phân Tán',
      sortOrder: 3,
      isActive: true
    })
    wrapper.unmount()
  })

  it('displays error message on slug conflict during creation', async () => {
    const categoryStore = useCategoryStore()
    categoryStore.createCategory = vi.fn().mockRejectedValue({
      code: 'Category.SlugConflict',
      status: 409,
      message: 'A category with this slug already exists.'
    })

    const wrapper = createWrapper()
    await flushPromises()

    const newBtn = wrapper.findAll('button').find(b => b.text().includes('admin.categories.newCategory'))
    await newBtn!.trigger('click')
    await flushPromises()

    await wrapper.find('#category-slug-input').setValue('frontend-web')
    await wrapper.find('#category-name-en-input').setValue('Frontend')
    await wrapper.find('#category-name-vi-input').setValue('Frontend')
    await wrapper.find('#category-sort-order-input').setValue('0')

    const submitBtn = wrapper.find('[role="dialog"]').findAll('button').find(b => b.text().includes('admin.categories.create'))
    await submitBtn!.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('admin.categories.errors.Category.SlugConflict')
    wrapper.unmount()
  })

  it('submits edit with expectedVersion and locks slug input as disabled', async () => {
    const categoryStore = useCategoryStore()
    const wrapper = createWrapper()
    await flushPromises()

    const editBtn = wrapper.findAll('button').find(b => b.text().includes('admin.categories.actions.edit'))
    expect(editBtn).toBeDefined()
    await editBtn!.trigger('click')
    await flushPromises()

    const slugInput = wrapper.find('#category-slug-input')
    expect(slugInput.attributes('disabled')).toBeDefined()

    await wrapper.find('#category-name-en-input').setValue('Frontend & Web Engineering')
    const saveBtn = wrapper.find('[role="dialog"]').findAll('button').find(b => b.text().includes('admin.categories.save'))
    await saveBtn!.trigger('click')
    await flushPromises()

    expect(categoryStore.updateCategory).toHaveBeenCalledWith('10000000-0000-4000-8000-000000000000', {
      nameEn: 'Frontend & Web Engineering',
      nameVi: 'Frontend & Web',
      sortOrder: 0,
      isActive: true,
      expectedVersion: 1
    })
    wrapper.unmount()
  })

  it('handles concurrency conflict on edit, preserves user input, and offers reload', async () => {
    const categoryStore = useCategoryStore()
    categoryStore.updateCategory = vi.fn().mockRejectedValue({
      code: 'Category.ConcurrencyConflict',
      status: 409,
      message: 'Category was modified by another administrator.'
    })

    const wrapper = createWrapper()
    await flushPromises()

    const editBtn = wrapper.findAll('button').find(b => b.text().includes('admin.categories.actions.edit'))
    await editBtn!.trigger('click')
    await flushPromises()

    await wrapper.find('#category-name-en-input').setValue('Modified by me')
    const saveBtn = wrapper.find('[role="dialog"]').findAll('button').find(b => b.text().includes('admin.categories.save'))
    await saveBtn!.trigger('click')
    await flushPromises()

    // Concurrency conflict message appears
    expect(wrapper.text()).toContain('admin.categories.errors.Category.ConcurrencyConflict')
    // User's input is preserved in the form
    const nameInput = wrapper.find<HTMLInputElement>('#category-name-en-input')
    expect(nameInput.element.value).toBe('Modified by me')

    // Reload button is available
    const reloadBtn = wrapper.find('[role="dialog"]').findAll('button').find(b => b.text().includes('admin.categories.refresh'))
    expect(reloadBtn).toBeDefined()

    // Click reload
    categoryStore.fetchCategoryDetail = vi.fn().mockResolvedValue({
      ...mockCategories[0],
      nameEn: 'Latest server name',
      nameVi: 'Tên mới nhất',
      sortOrder: 9,
      isActive: false,
      version: 5,
      canDelete: true
    })
    await reloadBtn!.trigger('click')
    await flushPromises()

    expect(categoryStore.fetchCategoryDetail).toHaveBeenCalledWith('10000000-0000-4000-8000-000000000000')
    expect(wrapper.find<HTMLInputElement>('#category-name-en-input').element.value).toBe('Modified by me')
    expect(wrapper.find<HTMLInputElement>('#category-name-vi-input').element.value).toBe('Frontend & Web')
    expect(wrapper.text()).toContain('Latest server name')
    expect(wrapper.text()).toContain('Tên mới nhất')
    expect(saveBtn!.attributes('disabled')).toBeDefined()
    await wrapper.find('#category-confirm-refreshed').setValue(true)
    categoryStore.updateCategory = vi.fn().mockResolvedValue({ ...mockCategories[0], version: 6 })
    await saveBtn!.trigger('click')
    await flushPromises()
    expect(categoryStore.updateCategory).toHaveBeenCalledWith(mockCategories[0].id, {
      nameEn: 'Modified by me',
      nameVi: 'Frontend & Web',
      sortOrder: 0,
      isActive: true,
      expectedVersion: 5
    })
    wrapper.unmount()
  })

  it('does not apply a stale conflict refresh to another category dialog', async () => {
    const store = useCategoryStore()
    store.updateCategory = vi.fn().mockRejectedValue({ code: 'Category.ConcurrencyConflict', status: 409 })
    const wrapper = mount(AdminCategoryModal, {
      props: { open: false, category: mockCategories[0] },
      global: { stubs: { Teleport: true } }
    })
    await wrapper.setProps({ open: true })
    await wrapper.findAll('button').find(button => button.text().includes('admin.categories.save'))!.trigger('click')
    await flushPromises()
    let resolveRefresh!: (detail: AdminCategoryDetail) => void
    store.fetchCategoryDetail = vi.fn().mockReturnValue(new Promise<AdminCategoryDetail>(resolve => { resolveRefresh = resolve }))
    await wrapper.findAll('button').find(button => button.text().includes('admin.categories.refresh'))!.trigger('click')

    await wrapper.setProps({ open: false })
    await wrapper.setProps({ open: true, category: mockCategories[1] })
    resolveRefresh({ ...mockCategories[0], nameEn: 'Obsolete first dialog response', version: 99, canDelete: true })
    await flushPromises()

    expect(wrapper.find<HTMLInputElement>('#category-name-en-input').element.value).toBe(mockCategories[1].nameEn)
    await wrapper.findAll('button').find(button => button.text().includes('admin.categories.save'))!.trigger('click')
    await flushPromises()
    expect(store.updateCategory).toHaveBeenLastCalledWith(mockCategories[1].id, expect.objectContaining({
      nameEn: mockCategories[1].nameEn,
      expectedVersion: mockCategories[1].version
    }))
    wrapper.unmount()
  })

  it('opens delete confirmation and executes deletion on confirm', async () => {
    const categoryStore = useCategoryStore()
    const wrapper = createWrapper()
    await flushPromises()

    const deleteBtn = wrapper.findAll('button').find(b => b.text().includes('admin.categories.actions.delete'))
    expect(deleteBtn).toBeDefined()
    await deleteBtn!.trigger('click')
    await flushPromises()

    // Delete confirmation modal is open
    expect(wrapper.text()).toContain('admin.categories.modals.deleteConfirm')
    expect(wrapper.text()).toContain('admin.categories.modals.deleteWarning')

    // Confirm deletion
    const confirmBtn = wrapper.find('[aria-label="admin.categories.modals.deleteTitle"]').findAll('button').find(b => b.text().includes('admin.categories.actions.delete'))
    expect(confirmBtn).toBeDefined()
    await confirmBtn!.trigger('click')
    await flushPromises()

    expect(categoryStore.deleteCategory).toHaveBeenCalledWith('10000000-0000-4000-8000-000000000000', 1)
    wrapper.unmount()
  })
})
