// @env node
import withNuxt from './.nuxt/eslint.config.mjs'

export default withNuxt(
  {
    ignores: [
      'e2e/**',
      'public/**',
      '.nuxt/**',
      '.output/**',
      'dist/**',
      '**/*.d.ts'
    ]
  },
  {
    rules: {
      'vue/no-v-html': 'warn',
      'vue/no-template-target-blank': 'error',
      'vue/multi-word-component-names': 'off',
      'vue/html-self-closing': 'off',
      'vue/first-attribute-linebreak': 'off',
      'vue/attributes-order': 'off',
      'vue/attribute-hyphenation': 'warn',
      'vue/require-default-prop': 'off',
      'vue/no-required-prop-with-default': 'off',
      '@typescript-eslint/no-explicit-any': 'warn',
      '@typescript-eslint/no-unused-vars': ['warn', { argsIgnorePattern: '^_', varsIgnorePattern: '^_' }],
      '@typescript-eslint/unified-signatures': 'off',
      '@typescript-eslint/no-empty-object-type': 'off',
      '@typescript-eslint/no-invalid-void-type': 'off',
      '@typescript-eslint/no-this-alias': 'off',
      'import/first': 'off',
      'import/no-duplicates': 'warn',
      'no-console': ['warn', { allow: ['warn', 'error'] }],
      'no-control-regex': 'off',
      'no-useless-catch': 'warn',
      'no-useless-assignment': 'warn',
      'prefer-const': 'warn'
    }
  },
  {
    files: ['tests/**/*', '**/*.spec.ts', '**/*.test.ts'],
    rules: {
      '@typescript-eslint/no-explicit-any': 'off',
      '@typescript-eslint/no-unused-vars': 'off',
      '@typescript-eslint/no-empty-object-type': 'off',
      '@typescript-eslint/no-invalid-void-type': 'off',
      '@typescript-eslint/no-this-alias': 'off',
      'no-console': 'off'
    }
  }
)
