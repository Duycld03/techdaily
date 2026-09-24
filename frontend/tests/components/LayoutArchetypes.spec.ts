import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import StudioLayout from '~/components/layout/StudioLayout.vue'
import MasterDetailLayout from '~/components/layout/MasterDetailLayout.vue'
import BoardLayout from '~/components/layout/BoardLayout.vue'

describe('StudioLayout.vue', () => {
  it('renders default slots (#main, #header, #dock, #footer)', () => {
    const wrapper = mount(StudioLayout, {
      slots: {
        header: '<div data-testid="studio-header">Header Content</div>',
        main: '<div data-testid="studio-main">Main Practice Content</div>',
        dock: '<div data-testid="studio-dock">Telemetry Dock Content</div>',
        footer: '<div data-testid="studio-footer">Footer Content</div>'
      }
    })

    expect(wrapper.find('header').exists()).toBe(true)
    expect(wrapper.find('[data-testid="studio-header"]').text()).toBe('Header Content')

    expect(wrapper.find('section[aria-label="Practice stage"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="studio-main"]').text()).toBe('Main Practice Content')

    expect(wrapper.find('aside[aria-label="Session telemetry"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="studio-dock"]').text()).toBe('Telemetry Dock Content')

    expect(wrapper.find('footer').exists()).toBe(true)
    expect(wrapper.find('[data-testid="studio-footer"]').text()).toBe('Footer Content')
  })

  it('conditionally omits header, dock, and footer when slots are not provided', () => {
    const wrapper = mount(StudioLayout, {
      slots: {
        main: '<div data-testid="studio-main">Solo Main Content</div>'
      }
    })

    expect(wrapper.find('header').exists()).toBe(false)
    expect(wrapper.find('aside[aria-label="Session telemetry"]').exists()).toBe(false)
    expect(wrapper.find('footer').exists()).toBe(false)
    expect(wrapper.find('[data-testid="studio-main"]').text()).toBe('Solo Main Content')
  })

  it('applies custom maxHeight style when prop is passed', () => {
    const wrapper = mount(StudioLayout, {
      props: {
        maxHeight: '750px'
      }
    })

    expect((wrapper.element as HTMLElement).style.height).toBe('750px')
  })

  it('renders natural height when maxHeight is not provided', () => {
    const wrapper = mount(StudioLayout)
    expect((wrapper.element as HTMLElement).style.height).toBe('')
  })

  it('renders dock slot inside aside container', () => {
    const wrapper = mount(StudioLayout, {
      slots: {
        dock: '<div data-testid="dock-content">Telemetry Stats</div>'
      }
    })

    const dock = wrapper.find('aside[aria-label="Session telemetry"]')
    expect(dock.exists()).toBe(true)
    expect(dock.find('[data-testid="dock-content"]').text()).toBe('Telemetry Stats')
  })
})

describe('MasterDetailLayout.vue', () => {
  it('renders #header, #nav, and #content slots', () => {
    const wrapper = mount(MasterDetailLayout, {
      slots: {
        header: '<div data-testid="md-header">Settings Header</div>',
        nav: '<nav data-testid="md-nav">Sidebar Nav Items</nav>',
        content: '<div data-testid="md-content">Settings Detail Panel</div>'
      }
    })

    expect(wrapper.find('header').exists()).toBe(true)
    expect(wrapper.find('[data-testid="md-header"]').text()).toBe('Settings Header')

    expect(wrapper.find('nav[aria-label="Settings sections"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="md-nav"]').text()).toBe('Sidebar Nav Items')

    expect(wrapper.find('section[aria-label="Settings content"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="md-content"]').text()).toBe('Settings Detail Panel')
  })

  it('conditionally omits header when slot is not provided', () => {
    const wrapper = mount(MasterDetailLayout, {
      slots: {
        nav: '<div>Nav</div>',
        content: '<div>Content</div>'
      }
    })

    expect(wrapper.find('header').exists()).toBe(false)
    expect(wrapper.find('nav[aria-label="Settings sections"]').exists()).toBe(true)
    expect(wrapper.find('section[aria-label="Settings content"]').exists()).toBe(true)
  })

  it('applies custom maxHeight style when prop is passed', () => {
    const wrapper = mount(MasterDetailLayout, {
      props: {
        maxHeight: '520px'
      }
    })

    expect((wrapper.element as HTMLElement).style.height).toBe('520px')
  })

  it('renders natural height when maxHeight is not provided', () => {
    const wrapper = mount(MasterDetailLayout)
    expect((wrapper.element as HTMLElement).style.height).toBe('')
  })
})

describe('BoardLayout.vue', () => {
  it('renders #header, #filters, #content, and #pagination slots', () => {
    const wrapper = mount(BoardLayout, {
      slots: {
        header: '<div data-testid="board-header">Board Header Bar</div>',
        filters: '<div data-testid="board-filters">Category Filter Badges</div>',
        content: '<div data-testid="board-content">Card Grid Items</div>',
        pagination: '<div data-testid="board-pagination">Page Controls</div>'
      }
    })

    expect(wrapper.find('header').exists()).toBe(true)
    expect(wrapper.find('[data-testid="board-header"]').text()).toBe('Board Header Bar')

    expect(wrapper.find('[data-testid="board-filters"]').text()).toBe('Category Filter Badges')

    expect(wrapper.find('[data-testid="board-content"]').text()).toBe('Card Grid Items')

    expect(wrapper.find('footer').exists()).toBe(true)
    expect(wrapper.find('[data-testid="board-pagination"]').text()).toBe('Page Controls')
  })

  it('conditionally omits header, filters, and pagination when slots are not provided', () => {
    const wrapper = mount(BoardLayout, {
      slots: {
        content: '<div data-testid="board-content">Grid Only</div>'
      }
    })

    expect(wrapper.find('header').exists()).toBe(false)
    expect(wrapper.find('footer').exists()).toBe(false)
    expect(wrapper.find('[data-testid="board-content"]').text()).toBe('Grid Only')
  })

  it('applies custom maxHeight style when prop is passed', () => {
    const wrapper = mount(BoardLayout, {
      props: {
        maxHeight: '680px'
      }
    })

    expect((wrapper.element as HTMLElement).style.height).toBe('680px')
  })

  it('renders natural height when maxHeight is not provided', () => {
    const wrapper = mount(BoardLayout)
    expect((wrapper.element as HTMLElement).style.height).toBe('')
  })
})
