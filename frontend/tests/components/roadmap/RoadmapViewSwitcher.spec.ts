import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import RoadmapViewSwitcher from '~/components/roadmap/RoadmapViewSwitcher.vue'
import {
  useRoadmapViewMode,
  ROADMAP_VIEW_MODE_STORAGE_KEY
} from '~/composables/useRoadmapViewMode'

describe('components/roadmap/RoadmapViewSwitcher.vue', () => {
  it('renders both timeline and mindmap tabs with correct accessibility attributes', () => {
    const wrapper = mount(RoadmapViewSwitcher, {
      props: {
        modelValue: 'timeline'
      }
    })

    const tablist = wrapper.find('[role="tablist"]')
    expect(tablist.exists()).toBe(true)

    const tabs = wrapper.findAll('[role="tab"]')
    expect(tabs).toHaveLength(2)

    const timelineTab = wrapper.find('#tab-timeline')
    const mindmapTab = wrapper.find('#tab-mindmap')

    expect(timelineTab.attributes('aria-selected')).toBe('true')
    expect(timelineTab.attributes('tabindex')).toBe('0')
    expect(mindmapTab.attributes('aria-selected')).toBe('false')
    expect(mindmapTab.attributes('tabindex')).toBe('-1')
  })

  it('updates aria-selected when modelValue changes to mindmap', () => {
    const wrapper = mount(RoadmapViewSwitcher, {
      props: {
        modelValue: 'mindmap'
      }
    })

    const timelineTab = wrapper.find('#tab-timeline')
    const mindmapTab = wrapper.find('#tab-mindmap')

    expect(timelineTab.attributes('aria-selected')).toBe('false')
    expect(mindmapTab.attributes('aria-selected')).toBe('true')
  })

  it('emits update:modelValue with mindmap when mindmap tab is clicked', async () => {
    const wrapper = mount(RoadmapViewSwitcher, {
      props: {
        modelValue: 'timeline'
      }
    })

    const mindmapTab = wrapper.find('#tab-mindmap')
    await mindmapTab.trigger('click')

    expect(wrapper.emitted('update:modelValue')).toBeTruthy()
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['mindmap'])
  })

  it('emits update:modelValue with timeline when timeline tab is clicked', async () => {
    const wrapper = mount(RoadmapViewSwitcher, {
      props: {
        modelValue: 'mindmap'
      }
    })

    const timelineTab = wrapper.find('#tab-timeline')
    await timelineTab.trigger('click')

    expect(wrapper.emitted('update:modelValue')).toBeTruthy()
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['timeline'])
  })

  it('does not emit update:modelValue if active tab is clicked', async () => {
    const wrapper = mount(RoadmapViewSwitcher, {
      props: {
        modelValue: 'timeline'
      }
    })

    const timelineTab = wrapper.find('#tab-timeline')
    await timelineTab.trigger('click')

    expect(wrapper.emitted('update:modelValue')).toBeFalsy()
  })

  it('handles keyboard navigation with ArrowRight and ArrowLeft', async () => {
    const wrapper = mount(RoadmapViewSwitcher, {
      props: {
        modelValue: 'timeline'
      }
    })

    const tablist = wrapper.find('[role="tablist"]')

    // ArrowRight should switch to mindmap
    await tablist.trigger('keydown', { key: 'ArrowRight' })
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['mindmap'])

    // ArrowLeft should switch to timeline
    await wrapper.setProps({ modelValue: 'mindmap' })
    await tablist.trigger('keydown', { key: 'ArrowLeft' })
    expect(wrapper.emitted('update:modelValue')?.[1]).toEqual(['timeline'])
  })

  describe('useRoadmapViewMode composable', () => {
    beforeEach(() => {
      window.localStorage.clear()
    })

    it('defaults to timeline when localStorage is empty', () => {
      const { viewMode } = useRoadmapViewMode()
      expect(viewMode.value).toBe('timeline')
    })

    it('loads stored viewMode from localStorage', () => {
      window.localStorage.setItem(ROADMAP_VIEW_MODE_STORAGE_KEY, 'mindmap')
      const { viewMode } = useRoadmapViewMode()
      expect(viewMode.value).toBe('mindmap')
    })

    it('ignores invalid values from localStorage and defaults to timeline', () => {
      window.localStorage.setItem(ROADMAP_VIEW_MODE_STORAGE_KEY, 'invalid-mode')
      const { viewMode } = useRoadmapViewMode()
      expect(viewMode.value).toBe('timeline')
    })

    it('updates viewMode and persists to localStorage on setViewMode', () => {
      const { viewMode, setViewMode } = useRoadmapViewMode()
      setViewMode('mindmap')

      expect(viewMode.value).toBe('mindmap')
      expect(window.localStorage.getItem(ROADMAP_VIEW_MODE_STORAGE_KEY)).toBe('mindmap')
    })
  })
})
