import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BasePagination from '~/components/common/BasePagination.vue'

describe('BasePagination.vue', () => {
  it('does not render or renders clean empty state when totalPages <= 1 (unless showSummary is requested)', () => {
    const wrapperEmpty = mount(BasePagination, {
      props: {
        currentPage: 1,
        totalPages: 1
      }
    })
    expect(wrapperEmpty.find('nav').exists()).toBe(false)

    const wrapperZero = mount(BasePagination, {
      props: {
        currentPage: 1,
        totalPages: 0
      }
    })
    expect(wrapperZero.find('nav').exists()).toBe(false)

    const wrapperWithSummary = mount(BasePagination, {
      props: {
        currentPage: 1,
        totalPages: 1,
        showSummary: true,
        totalCount: 8,
        pageSize: 10
      }
    })
    expect(wrapperWithSummary.find('nav').exists()).toBe(true)
    expect(wrapperWithSummary.text()).toContain('Showing 1–8 of 8')
    expect(wrapperWithSummary.findAll('button').length).toBe(0)
  })

  it('renders correct sequence of page buttons when totalPages = 5', () => {
    const wrapper = mount(BasePagination, {
      props: {
        currentPage: 2,
        totalPages: 5
      }
    })

    const vm = wrapper.vm as unknown as { visiblePages: (number | string)[] }
    expect(vm.visiblePages).toEqual([1, 2, 3, 4, 5])

    const pageButtons = wrapper.findAll('button[data-page]')
    expect(pageButtons.length).toBe(5)
    expect(pageButtons.map((b) => b.text().trim())).toEqual(['1', '2', '3', '4', '5'])
  })

  it('calculates smart ellipsis when totalPages = 10 and currentPage = 5 ([1, "...", 4, 5, 6, "...", 10])', () => {
    const wrapper = mount(BasePagination, {
      props: {
        currentPage: 5,
        totalPages: 10
      }
    })

    const vm = wrapper.vm as unknown as { visiblePages: (number | string)[] }
    expect(vm.visiblePages).toEqual([1, '...', 4, 5, 6, '...', 10])

    const pageButtons = wrapper.findAll('button[data-page]')
    expect(pageButtons.map((b) => b.text().trim())).toEqual(['1', '4', '5', '6', '10'])

    const ellipses = wrapper.findAll('span[aria-label="More pages"]')
    expect(ellipses.length).toBe(2)
  })

  it('calculates smart ellipsis for start and end boundary positions', () => {
    const wrapperStart = mount(BasePagination, {
      props: {
        currentPage: 1,
        totalPages: 10
      }
    })
    const vmStart = wrapperStart.vm as unknown as { visiblePages: (number | string)[] }
    expect(vmStart.visiblePages).toEqual([1, 2, 3, 4, '...', 10])

    const wrapperEnd = mount(BasePagination, {
      props: {
        currentPage: 9,
        totalPages: 10
      }
    })
    const vmEnd = wrapperEnd.vm as unknown as { visiblePages: (number | string)[] }
    expect(vmEnd.visiblePages).toEqual([1, '...', 7, 8, 9, 10])
  })

  it('previous button is disabled on page 1, active on page 2', async () => {
    const wrapperPage1 = mount(BasePagination, {
      props: {
        currentPage: 1,
        totalPages: 5
      }
    })
    const prevBtn1 = wrapperPage1.find('button[aria-label="Previous"]')
    expect(prevBtn1.exists()).toBe(true)
    expect(prevBtn1.attributes('disabled')).toBeDefined()
    expect(prevBtn1.attributes('aria-disabled')).toBe('true')

    const wrapperPage2 = mount(BasePagination, {
      props: {
        currentPage: 2,
        totalPages: 5
      }
    })
    const prevBtn2 = wrapperPage2.find('button[aria-label="Previous"]')
    expect(prevBtn2.exists()).toBe(true)
    expect(prevBtn2.attributes('disabled')).toBeUndefined()
    expect(prevBtn2.attributes('aria-disabled')).toBeUndefined()
  })

  it('next button is disabled on page totalPages, active on page totalPages - 1', () => {
    const wrapperLast = mount(BasePagination, {
      props: {
        currentPage: 5,
        totalPages: 5
      }
    })
    const nextBtnLast = wrapperLast.find('button[aria-label="Next"]')
    expect(nextBtnLast.exists()).toBe(true)
    expect(nextBtnLast.attributes('disabled')).toBeDefined()
    expect(nextBtnLast.attributes('aria-disabled')).toBe('true')

    const wrapperPenultimate = mount(BasePagination, {
      props: {
        currentPage: 4,
        totalPages: 5
      }
    })
    const nextBtnPenultimate = wrapperPenultimate.find('button[aria-label="Next"]')
    expect(nextBtnPenultimate.exists()).toBe(true)
    expect(nextBtnPenultimate.attributes('disabled')).toBeUndefined()
    expect(nextBtnPenultimate.attributes('aria-disabled')).toBeUndefined()
  })

  it('clicking a page button emits both update:currentPage and change events with expected page number', async () => {
    const wrapper = mount(BasePagination, {
      props: {
        currentPage: 1,
        totalPages: 5
      }
    })

    const buttonPage3 = wrapper.find('button[data-page="3"]')
    expect(buttonPage3.exists()).toBe(true)
    await buttonPage3.trigger('click')

    expect(wrapper.emitted('update:currentPage')).toBeTruthy()
    expect(wrapper.emitted('update:currentPage')?.[0]).toEqual([3])
    expect(wrapper.emitted('change')).toBeTruthy()
    expect(wrapper.emitted('change')?.[0]).toEqual([3])
  })

  it('clicking previous and next buttons emits events with expected page number', async () => {
    const wrapper = mount(BasePagination, {
      props: {
        currentPage: 3,
        totalPages: 5
      }
    })

    const prevBtn = wrapper.find('button[aria-label="Previous"]')
    await prevBtn.trigger('click')
    expect(wrapper.emitted('update:currentPage')?.[0]).toEqual([2])
    expect(wrapper.emitted('change')?.[0]).toEqual([2])

    const nextBtn = wrapper.find('button[aria-label="Next"]')
    await nextBtn.trigger('click')
    expect(wrapper.emitted('update:currentPage')?.[1]).toEqual([4])
    expect(wrapper.emitted('change')?.[1]).toEqual([4])
  })

  it('has accessible WAI-ARIA attributes (role="navigation", aria-current="page" on active button)', () => {
    const wrapper = mount(BasePagination, {
      props: {
        currentPage: 3,
        totalPages: 5
      }
    })

    const nav = wrapper.find('nav')
    expect(nav.attributes('role')).toBe('navigation')
    expect(nav.attributes('aria-label')).toBe('Pagination Navigation')

    const activeBtn = wrapper.find('button[data-page="3"]')
    expect(activeBtn.attributes('aria-current')).toBe('page')
    const inactiveBtn = wrapper.find('button[data-page="2"]')
    expect(inactiveBtn.attributes('aria-current')).toBeUndefined()
  })

  it('does not emit events when disabled prop is true', async () => {
    const wrapper = mount(BasePagination, {
      props: {
        currentPage: 2,
        totalPages: 5,
        disabled: true
      }
    })

    const button3 = wrapper.find('button[data-page="3"]')
    await button3.trigger('click')
    expect(wrapper.emitted('change')).toBeFalsy()

    const prevBtn = wrapper.find('button[aria-label="Previous"]')
    expect(prevBtn.attributes('disabled')).toBeDefined()
    await prevBtn.trigger('click')
    expect(wrapper.emitted('change')).toBeFalsy()
  })

  it('renders summary text correctly when totalCount and pageSize are provided', () => {
    const wrapper = mount(BasePagination, {
      props: {
        currentPage: 2,
        totalPages: 4,
        totalCount: 42,
        pageSize: 12,
        showSummary: true
      }
    })

    expect(wrapper.text()).toContain('Showing 13–24 of 42')
  })
})
