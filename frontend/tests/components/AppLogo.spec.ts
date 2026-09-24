import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import AppLogo from '~/components/common/AppLogo.vue'

describe('AppLogo.vue', () => {
  it('renders SVG with default dimensions and accessibility attributes', () => {
    const wrapper = mount(AppLogo)
    const svg = wrapper.find('svg')
    expect(svg.exists()).toBe(true)
    expect(svg.attributes('role')).toBe('img')
    expect(svg.attributes('aria-label')).toBe('TechDaily')
    expect(svg.attributes('width')).toBe('32px')
    expect(svg.attributes('height')).toBe('32px')
  })

  it('renders squircle canvas, hexagon, code brackets, and core nucleus', () => {
    const wrapper = mount(AppLogo)
    // Squircle canvas rect
    const rect = wrapper.find('rect')
    expect(rect.exists()).toBe(true)
    expect(rect.attributes('rx')).toBe('104')

    // Hexagon polygons
    const polygons = wrapper.findAll('polygon')
    expect(polygons.length).toBe(2) // Outer hexagon ring + inner architectural hairline hexagon

    // Code brackets < >
    const paths = wrapper.findAll('path')
    expect(paths.length).toBe(2) // Left bracket < + Right bracket >

    // Core nucleus and accent circles
    const circles = wrapper.findAll('circle')
    expect(circles.length).toBeGreaterThanOrEqual(4)
  })

  it('supports size prop presets (xs, sm, md, lg, xl)', () => {
    const wrapperSm = mount(AppLogo, { props: { size: 'sm' } })
    expect(wrapperSm.find('svg').attributes('width')).toBe('28px')

    const wrapperLg = mount(AppLogo, { props: { size: 'lg' } })
    expect(wrapperLg.find('svg').attributes('width')).toBe('40px')

    const wrapperXl = mount(AppLogo, { props: { size: 'xl' } })
    expect(wrapperXl.find('svg').attributes('width')).toBe('48px')
  })

  it('supports custom numeric size prop', () => {
    const wrapper = mount(AppLogo, { props: { size: 64 } })
    expect(wrapper.find('svg').attributes('width')).toBe('64px')
    expect(wrapper.find('svg').attributes('height')).toBe('64px')
  })

  it('scopes gradient and filter IDs with custom idPrefix', () => {
    const wrapper = mount(AppLogo, { props: { idPrefix: 'custom-brand' } })
    const defs = wrapper.find('defs')
    expect(defs.html()).toContain('id="custom-brand-bgGrad"')
    expect(defs.html()).toContain('id="custom-brand-symmHexGrad"')
    expect(wrapper.find('rect').attributes('fill')).toContain('#custom-brand-bgGrad')
  })
})
