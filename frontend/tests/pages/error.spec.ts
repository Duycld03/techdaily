import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import type { NuxtError } from '#app'
import ErrorPage from '~/error.vue'

describe('error.vue', () => {
  it('renders 404 status code badge and not found copy', () => {
    const error = {
      statusCode: 404,
      statusMessage: 'Page Not Found',
      message: 'Page not found'
    } as unknown as NuxtError

    const wrapper = mount(ErrorPage, {
      props: { error }
    })

    expect(wrapper.text()).toContain('404')
    expect(wrapper.text()).toContain('error.not_found_title')
    expect(wrapper.text()).toContain('error.not_found_desc')
    expect(wrapper.text()).toContain('error.btn_home')
    // Retry button should not be displayed on 404
    expect(wrapper.text()).not.toContain('error.btn_retry')
  })

  it('renders 500 status code badge and retry button on server error', () => {
    const error = {
      statusCode: 500,
      statusMessage: 'Internal Server Error',
      message: 'Internal server error'
    } as unknown as NuxtError

    const wrapper = mount(ErrorPage, {
      props: { error }
    })

    expect(wrapper.text()).toContain('500')
    expect(wrapper.text()).toContain('error.server_error_title')
    expect(wrapper.text()).toContain('error.server_error_desc')
    expect(wrapper.text()).toContain('error.btn_retry')
  })

  it('navigates to /today when home button is clicked', async () => {
    const clearErrorMock = vi.fn()
    Reflect.set(globalThis, 'clearError', clearErrorMock)

    const error = {
      statusCode: 404,
      statusMessage: 'Not Found'
    } as unknown as NuxtError

    const wrapper = mount(ErrorPage, {
      props: { error }
    })

    const homeButton = wrapper.findAll('button')[0]
    await homeButton.trigger('click')

    expect(clearErrorMock).toHaveBeenCalledWith({ redirect: '/today' })
  })

  it('invokes clearError without redirect when retry button is clicked', async () => {
    const clearErrorMock = vi.fn()
    Reflect.set(globalThis, 'clearError', clearErrorMock)

    const error = {
      statusCode: 500,
      statusMessage: 'Server Error'
    } as unknown as NuxtError

    const wrapper = mount(ErrorPage, {
      props: { error }
    })

    const buttons = wrapper.findAll('button')
    const retryButton = buttons[1]
    await retryButton.trigger('click')

    expect(clearErrorMock).toHaveBeenCalledWith()
  })
})
