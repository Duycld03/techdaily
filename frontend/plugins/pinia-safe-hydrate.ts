import { shouldHydrate } from 'pinia'

/**
 * Fixes Nuxt SSR hydration crash when payload contains prototype-less objects (Object.create(null)),
 * where Pinia's shouldHydrate calls `obj.hasOwnProperty()` directly and throws:
 * "TypeError: obj.hasOwnProperty is not a function".
 */
export default definePayloadPlugin(() => {
  definePayloadReducer(
    'skipHydrate',
    (data: unknown) => {
      try {
        return !shouldHydrate(data) && 1
      } catch {
        return false
      }
    }
  )
  definePayloadReviver('skipHydrate', () => void 0)
})
