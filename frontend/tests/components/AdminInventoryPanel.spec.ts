import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import AdminInventoryPanel from '~/components/admin/dashboard/AdminInventoryPanel.vue'
import AdminObservationRow from '~/components/admin/dashboard/AdminObservationRow.vue'
import type { AdminOverview } from '~/types/adminDashboard'

const zero: AdminOverview = { accountCount: 0, personalDocuments: { total: 0, pending: 0, processing: 0, ready: 0, failed: 0 }, catalogDocuments: { total: 0, draft: 0, published: 0, hidden: 0, processing: { total: 0, pending: 0, processing: 0, ready: 0, failed: 0 } }, observedAt: '2026-10-14T10:00:00Z' }

describe('administrator observations', () => {
  it('renders genuine zero counts separately from an unavailable inventory', async () => {
    const wrapper = mount(AdminInventoryPanel, { props: { overview: zero, loading: false, error: false, stale: false }, global: { stubs: { NuxtLink: true } } })
    expect(wrapper.text()).not.toContain('admin.dashboard.unavailable')
    expect(wrapper.findAll('article').slice(0, 3).every(card => card.text().includes('0'))).toBe(true)
    await wrapper.setProps({ overview: null, error: true })
    expect(wrapper.text()).toContain('admin.dashboard.unavailable')
    expect(wrapper.text()).toContain('admin.dashboard.overview_error')
    await wrapper.get('button').trigger('click')
    expect(wrapper.emitted('retry')).toHaveLength(1)
  })

  it('labels stale successful inventory after a refresh error', () => {
    const wrapper = mount(AdminInventoryPanel, { props: { overview: zero, loading: false, error: true, stale: true }, global: { stubs: { NuxtLink: true } } })
    expect(wrapper.text()).toContain('admin.dashboard.stale_error')
    expect(wrapper.text()).toContain('admin.dashboard.stale')
  })

  it('displays explicit unchecked, failure and stale service observations without raw provider errors', async () => {
    const wrapper = mount(AdminObservationRow, { props: { label: 'Text generation' } })
    expect(wrapper.text()).toContain('admin.dashboard.service_not_checked')
    await wrapper.setProps({ observation: { model: 'gemini-3.6-flash', status: 'unavailable', checkedAt: '2026-10-14T10:00:00Z', latencyMs: 15_000, errorCode: 'AI_TIMEOUT' } })
    expect(wrapper.text()).toContain('admin.dashboard.service_unavailable')
    expect(wrapper.text()).toContain('admin.errors.timeout')
    expect(wrapper.text()).not.toContain('AI_TIMEOUT')
    await wrapper.setProps({ stale: true })
    expect(wrapper.text()).toContain('admin.dashboard.service_stale')
  })
})
