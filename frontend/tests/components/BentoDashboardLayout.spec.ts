import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BentoDashboardLayout from '~/components/layout/BentoDashboardLayout.vue'

describe('BentoDashboardLayout', () => {
  it('renders default action-stage slot content', () => {
    const wrapper = mount(BentoDashboardLayout, {
      slots: {
        'action-stage': '<div data-testid="action-content">Action Stage Tiles</div>'
      }
    })

    expect(wrapper.find('[data-testid="action-content"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('Action Stage Tiles')
  })

  it('renders all slots when provided', () => {
    const wrapper = mount(BentoDashboardLayout, {
      slots: {
        header: '<div data-testid="bento-header">Welcome Header</div>',
        'action-stage': '<div data-testid="bento-action">Core Action Cards</div>',
        'telemetry-dock': '<div data-testid="bento-telemetry">Telemetry Metrics</div>',
        footer: '<div data-testid="bento-footer">System Status</div>'
      }
    })

    expect(wrapper.find('[data-testid="bento-header"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="bento-action"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="bento-telemetry"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="bento-footer"]').exists()).toBe(true)
  })

  it('omits optional header, telemetry-dock, and footer when slots are not provided', () => {
    const wrapper = mount(BentoDashboardLayout, {
      slots: {
        'action-stage': '<div>Only Action Content</div>'
      }
    })

    expect(wrapper.find('header').exists()).toBe(false)
    expect(wrapper.find('aside').exists()).toBe(false)
    expect(wrapper.find('footer').exists()).toBe(false)
  })

  it('applies custom maxHeight style when prop is passed', () => {
    const wrapper = mount(BentoDashboardLayout, {
      props: {
        maxHeight: '750px'
      },
      slots: {
        'action-stage': '<div>Content</div>'
      }
    })

    expect(wrapper.attributes('style')).toContain('height: 750px')
  })

  it('renders action stage and telemetry dock semantic elements', () => {
    const wrapper = mount(BentoDashboardLayout, {
      slots: {
        'action-stage': '<div>Action Cards</div>',
        'telemetry-dock': '<div>Telemetry Cards</div>'
      }
    })

    const actionSection = wrapper.find('section[aria-label="Core action stage"]')
    const telemetryAside = wrapper.find('aside[aria-label="Telemetry and constellation dock"]')

    expect(actionSection.exists()).toBe(true)
    expect(telemetryAside.exists()).toBe(true)
  })
})
