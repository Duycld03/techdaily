import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import AdminLoginForm from '~/components/admin/AdminLoginForm.vue'

describe('administrator sign-in form', () => {
  it('submits exact credentials and remember-me choice without trimming the password', async () => {
    const wrapper = mount(AdminLoginForm)
    await wrapper.get('input[name="email"]').setValue(' admin@example.test ')
    await wrapper.get('input[name="password"]').setValue(' password with spaces ')
    await wrapper.get('input[name="rememberMe"]').setValue(false)
    await wrapper.get('form').trigger('submit')
    expect(wrapper.emitted('submit')).toEqual([[{ email: 'admin@example.test', password: ' password with spaces ', rememberMe: false }]])
  })
  it('blocks invalid input with an accessible validation message', async () => {
    const wrapper = mount(AdminLoginForm)
    await wrapper.get('input[name="email"]').setValue('invalid')
    await wrapper.get('form').trigger('submit')
    expect(wrapper.emitted('submit')).toBeUndefined()
    expect(wrapper.get('[role="alert"]').text()).toBe('admin.login.invalid_email')
    await wrapper.get('input[name="email"]').setValue('admin@example.test')
    await wrapper.get('form').trigger('submit')
    expect(wrapper.get('[role="alert"]').text()).toBe('admin.login.password_required')
  })
  it('prevents duplicate submissions and preserves credentials on a server error', async () => {
    const wrapper = mount(AdminLoginForm, { props: { initialEmail: 'admin@example.test', error: 'Invalid credentials' } })
    await wrapper.get('input[name="password"]').setValue('same-password')
    await wrapper.setProps({ submitting: true })
    await wrapper.get('form').trigger('submit')
    expect(wrapper.emitted('submit')).toBeUndefined()
    expect(wrapper.get('button[type="submit"]').attributes('disabled')).toBeDefined()
    expect((wrapper.get('input[name="password"]').element as HTMLInputElement).value).toBe('same-password')
    expect(wrapper.get('[role="alert"]').text()).toBe('Invalid credentials')
  })
})
