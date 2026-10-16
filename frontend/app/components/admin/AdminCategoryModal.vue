<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import { X, AlertCircle, RefreshCw, Loader2, FolderTree, Check } from 'lucide-vue-next'
import { useCategoryStore, type CategoryDescriptor } from '~/stores/useCategoryStore'
import { useToast } from '~/composables/useToast'

const props = defineProps<{
  open: boolean
  category?: CategoryDescriptor | null
  initiatingButton?: HTMLElement | null
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'success', category: CategoryDescriptor): void
}>()

const categoryStore = useCategoryStore()
const toast = useToast()
const { t } = useI18n()

const dialogShellRef = ref<HTMLElement | null>(null)
const capturedTriggerElement = ref<HTMLElement | null>(null)

const slug = ref('')
const nameEn = ref('')
const nameVi = ref('')
const sortOrder = ref(0)
const isActive = ref(true)
const currentVersion = ref(1)

const isSubmitting = ref(false)
const isReloading = ref(false)
const formError = ref<string | null>(null)
const fieldErrors = ref<Record<string, string>>({})
const isConflict = ref(false)
const refreshedCategory = ref<CategoryDescriptor | null>(null)
const confirmRefreshedVersion = ref(false)
let dialogGeneration = 0

const isEdit = computed(() => Boolean(props.category))
const dialogTitleId = 'admin-category-modal-title'

watch(
  [() => props.open, () => props.category?.id],
  ([isOpen]) => {
    dialogGeneration++
    isReloading.value = false
    isSubmitting.value = false
    refreshedCategory.value = null
    confirmRefreshedVersion.value = false
    if (typeof document === 'undefined') return
    if (isOpen) {
      capturedTriggerElement.value = props.initiatingButton || (document.activeElement instanceof HTMLElement ? document.activeElement : null)
      formError.value = null
      fieldErrors.value = {}
      isConflict.value = false

      if (props.category) {
        slug.value = props.category.slug
        nameEn.value = props.category.nameEn
        nameVi.value = props.category.nameVi
        sortOrder.value = props.category.sortOrder
        isActive.value = props.category.isActive
        currentVersion.value = props.category.version
      } else {
        slug.value = ''
        nameEn.value = ''
        nameVi.value = ''
        sortOrder.value = 0
        isActive.value = true
        currentVersion.value = 1
      }

      document.body.style.overflow = 'hidden'
      nextTick(() => {
        if (dialogShellRef.value) {
          const focusable = getFocusableElements(dialogShellRef.value)
          if (focusable.length > 0) {
            focusable[0]?.focus()
          }
        }
      })
    } else {
      document.body.style.overflow = ''
      const target = capturedTriggerElement.value || props.initiatingButton
      if (target && typeof target.focus === 'function') {
        nextTick(() => target.focus())
      }
    }
  },
  { immediate: true }
)

function getFocusableElements(container: HTMLElement): HTMLElement[] {
  const selector = [
    'button:not([disabled])',
    '[href]',
    'input:not([disabled])',
    'textarea:not([disabled])',
    '[tabindex]:not([tabindex="-1"])'
  ].join(',')
  return Array.from(container.querySelectorAll<HTMLElement>(selector))
    .filter(el => el.offsetParent !== null || el.offsetWidth > 0 || el.offsetHeight > 0)
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    event.preventDefault()
    emit('close')
    return
  }

  if (event.key === 'Tab') {
    if (!dialogShellRef.value) return
    const focusable = getFocusableElements(dialogShellRef.value)
    if (focusable.length === 0) {
      event.preventDefault()
      return
    }

    const first = focusable[0]
    const last = focusable[focusable.length - 1]

    if (event.shiftKey) {
      if (document.activeElement === first || !dialogShellRef.value.contains(document.activeElement)) {
        event.preventDefault()
        last?.focus()
      }
    } else {
      if (document.activeElement === last || !dialogShellRef.value.contains(document.activeElement)) {
        event.preventDefault()
        first?.focus()
      }
    }
  }
}

function validate(): boolean {
  const errors: Record<string, string> = {}

  if (!isEdit.value) {
    const trimmedSlug = slug.value.trim()
    if (!trimmedSlug) {
      errors.slug = 'Slug is required.'
    } else if (trimmedSlug.length > 100) {
      errors.slug = 'Slug cannot exceed 100 characters.'
    } else if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(trimmedSlug)) {
      errors.slug = 'Slug must be 1-100 lowercase alphanumeric characters with single hyphens (e.g. system-design).'
    }
  }

  const trimmedEn = nameEn.value.trim()
  if (!trimmedEn) {
    errors.nameEn = 'English name is required.'
  } else if (trimmedEn.length > 100) {
    errors.nameEn = 'English name cannot exceed 100 characters.'
  }

  const trimmedVi = nameVi.value.trim()
  if (!trimmedVi) {
    errors.nameVi = 'Vietnamese name is required.'
  } else if (trimmedVi.length > 100) {
    errors.nameVi = 'Vietnamese name cannot exceed 100 characters.'
  }

  const orderNum = Number(sortOrder.value)
  if (isNaN(orderNum) || !Number.isInteger(orderNum) || orderNum < 0 || orderNum > 10000) {
    errors.sortOrder = 'Sort order must be an integer between 0 and 10000.'
  }

  fieldErrors.value = errors
  return Object.keys(errors).length === 0
}

function isCurrentDialog(generation: number, categoryId: string | undefined): boolean {
  return props.open && generation === dialogGeneration && props.category?.id === categoryId
}

async function handleSubmit() {
  if (isSubmitting.value || isReloading.value || isConflict.value
    || (refreshedCategory.value && !confirmRefreshedVersion.value) || !validate()) return
  const generation = dialogGeneration
  const categoryId = props.category?.id
  isSubmitting.value = true
  formError.value = null
  isConflict.value = false

  try {
    if (isEdit.value && props.category) {
      const updated = await categoryStore.updateCategory(props.category.id, {
        nameEn: nameEn.value.trim(),
        nameVi: nameVi.value.trim(),
        sortOrder: Number(sortOrder.value),
        isActive: isActive.value,
        expectedVersion: currentVersion.value
      })
      if (!isCurrentDialog(generation, categoryId)) return
      toast.success(t('admin.categories.save') + ' OK')
      emit('success', updated)
      emit('close')
    } else {
      const created = await categoryStore.createCategory({
        slug: slug.value.trim().toLowerCase(),
        nameEn: nameEn.value.trim(),
        nameVi: nameVi.value.trim(),
        sortOrder: Number(sortOrder.value),
        isActive: isActive.value
      })
      if (!isCurrentDialog(generation, categoryId)) return
      toast.success(t('admin.categories.create') + ' OK')
      emit('success', created)
      emit('close')
    }
  } catch (err: unknown) {
    if (!isCurrentDialog(generation, categoryId)) return
    const errorObj = err as Record<string, unknown>
    const code = (errorObj?.code as string) || (errorObj?.data as Record<string, unknown>)?.code || ''
    const status = errorObj?.status || (errorObj?.data as Record<string, unknown>)?.status
    const message = (errorObj?.message as string) || ''

    if (code === 'Category.SlugConflict' || message.includes('SlugConflict') || (status === 409 && !isEdit.value)) {
      fieldErrors.value.slug = t('admin.categories.errors.Category.SlugConflict')
      formError.value = t('admin.categories.errors.Category.SlugConflict')
    } else if (code === 'Category.ConcurrencyConflict' || message.includes('ConcurrencyConflict') || (status === 409 && isEdit.value)) {
      isConflict.value = true
      formError.value = t('admin.categories.errors.Category.ConcurrencyConflict')
    } else {
      formError.value = message || 'Failed to save category.'
    }
  } finally {
    if (isCurrentDialog(generation, categoryId)) isSubmitting.value = false
  }
}

async function handleReload() {
  if (!props.category?.id || isReloading.value) return
  const categoryId = props.category.id
  const generation = dialogGeneration
  isReloading.value = true
  formError.value = null
  try {
    const fresh = await categoryStore.fetchCategoryDetail(categoryId)
    if (!isCurrentDialog(generation, categoryId)) return
    refreshedCategory.value = fresh
    currentVersion.value = fresh.version
    confirmRefreshedVersion.value = false
    isConflict.value = false
  } catch {
    if (isCurrentDialog(generation, categoryId)) formError.value = t('admin.categories.conflict.reloadFailed')
  } finally {
    if (isCurrentDialog(generation, categoryId)) isReloading.value = false
  }
}
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition duration-150 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition duration-100 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="open"
        class="fixed inset-0 z-50 flex items-center justify-center p-3 sm:p-4"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="dialogTitleId"
        @keydown="handleKeydown"
      >
        <!-- Backdrop -->
        <div
          class="absolute inset-0 bg-slate-900/60 dark:bg-black/75 backdrop-blur-sm"
          aria-hidden="true"
          @click="emit('close')"
        />

        <!-- Dialog Shell -->
        <div
          ref="dialogShellRef"
          class="relative z-10 flex max-h-[90vh] w-full max-w-lg flex-col overflow-hidden rounded-2xl sm:rounded-3xl border border-slate-200/80 dark:border-white/10 bg-white dark:bg-canvas-elevated shadow-2xl text-slate-900 dark:text-slate-100 text-sm sm:text-base"
        >
          <!-- Header -->
          <header class="flex shrink-0 items-center justify-between gap-3 border-b border-slate-200/80 dark:border-white/[0.08] px-5 sm:px-6 py-4">
            <div class="flex items-center gap-2.5 min-w-0">
              <FolderTree class="h-5 w-5 text-brand-600 dark:text-brand-400 shrink-0" aria-hidden="true" />
              <h2 :id="dialogTitleId" class="text-base sm:text-lg font-bold text-slate-900 dark:text-white truncate">
                {{ isEdit ? $t('admin.categories.edit') : $t('admin.categories.newCategory') }}
              </h2>
            </div>
            <button
              type="button"
              class="rounded-lg p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-white/10 dark:hover:text-slate-200 whitespace-nowrap shrink-0 transition-colors"
              aria-label="Close dialog"
              @click="emit('close')"
            >
              <X class="h-5 w-5" aria-hidden="true" />
            </button>
          </header>

          <!-- Body -->
          <div class="flex-1 overflow-y-auto px-5 sm:px-6 py-4 space-y-4">
            <!-- Global / Concurrency Error Alert -->
            <div
              v-if="formError"
              class="rounded-xl border p-3 text-sm flex items-start gap-2.5"
              :class="isConflict ? 'border-amber-500/30 bg-amber-500/10 text-amber-800 dark:text-amber-200' : 'border-rose-500/30 bg-rose-500/10 text-rose-800 dark:text-rose-200'"
              role="alert"
            >
              <AlertCircle class="h-5 w-5 shrink-0 mt-0.5 text-rose-500 dark:text-rose-400" aria-hidden="true" />
              <div class="flex-1 space-y-1">
                <p>{{ formError }}</p>
                <div v-if="isConflict" class="pt-1.5">
                  <button
                    type="button"
                    class="inline-flex items-center gap-1.5 rounded-lg bg-amber-600 px-3 py-1 text-xs font-semibold text-white hover:bg-amber-700 transition-colors whitespace-nowrap shrink-0"
                    :disabled="isReloading"
                    @click="handleReload"
                  >
                    <RefreshCw class="h-3.5 w-3.5" :class="{ 'animate-spin': isReloading }" aria-hidden="true" />
                    <span>{{ $t('admin.categories.refresh') }}</span>
                  </button>
                </div>
              </div>
            </div>

            <section v-if="refreshedCategory" role="status" class="rounded-xl border border-amber-500/30 p-4 space-y-3 text-sm sm:text-base">
              <h3 class="font-semibold">{{ $t('admin.categories.conflict.currentVersion', { version: refreshedCategory.version }) }}</h3>
              <dl class="space-y-2 break-words">
                <div><dt class="font-semibold">{{ $t('admin.categories.fields.nameEn') }}</dt><dd>{{ refreshedCategory.nameEn }}</dd></div>
                <div><dt class="font-semibold">{{ $t('admin.categories.fields.nameVi') }}</dt><dd>{{ refreshedCategory.nameVi }}</dd></div>
                <div><dt class="font-semibold">{{ $t('admin.categories.fields.sortOrder') }}</dt><dd>{{ refreshedCategory.sortOrder }}</dd></div>
                <div><dt class="font-semibold">{{ $t('admin.categories.fields.status') }}</dt><dd>{{ refreshedCategory.isActive ? $t('admin.categories.status.active') : $t('admin.categories.status.inactive') }}</dd></div>
              </dl>
              <label class="flex items-start gap-3">
                <input id="category-confirm-refreshed" v-model="confirmRefreshedVersion" type="checkbox" class="mt-1 shrink-0" />
                <span>{{ $t('admin.categories.conflict.confirmDraft') }}</span>
              </label>
            </section>

            <!-- Slug Input -->
            <div class="space-y-1.5">
              <label for="category-slug-input" class="block font-semibold text-xs sm:text-sm text-slate-700 dark:text-slate-300">
                {{ $t('admin.categories.fields.slug') }}
                <span v-if="!isEdit" class="text-rose-500">*</span>
              </label>
              <input
                id="category-slug-input"
                v-model="slug"
                type="text"
                :disabled="isEdit || isSubmitting"
                :placeholder="$t('admin.categories.fields.slugPlaceholder')"
                class="w-full rounded-xl border px-3.5 py-2 text-sm focus:outline-none focus:ring-2 font-mono"
                :class="[
                  isEdit ? 'bg-slate-100 dark:bg-white/[0.04] text-slate-500 dark:text-slate-400 border-slate-300 dark:border-white/10 cursor-not-allowed' : 'bg-white dark:bg-canvas-subtle/50 text-slate-900 dark:text-white',
                  fieldErrors.slug ? 'border-rose-500 focus:ring-rose-500/20' : 'border-slate-300 dark:border-white/10 focus:ring-brand-500/20'
                ]"
              />
              <p v-if="fieldErrors.slug" class="text-xs text-rose-600 dark:text-rose-400">{{ fieldErrors.slug }}</p>
              <p v-else class="text-xs text-slate-500 dark:text-slate-400">
                {{ isEdit ? 'Slug is immutable after category creation.' : $t('admin.categories.fields.slugHelp') }}
              </p>
            </div>

            <!-- English Name Input -->
            <div class="space-y-1.5">
              <label for="category-name-en-input" class="block font-semibold text-xs sm:text-sm text-slate-700 dark:text-slate-300">
                {{ $t('admin.categories.fields.nameEn') }}
                <span class="text-rose-500">*</span>
              </label>
              <input
                id="category-name-en-input"
                v-model="nameEn"
                type="text"
                maxlength="100"
                :disabled="isSubmitting"
                :placeholder="$t('admin.categories.fields.nameEnPlaceholder')"
                class="w-full rounded-xl border bg-white dark:bg-canvas-subtle/50 px-3.5 py-2 text-sm text-slate-900 dark:text-white focus:outline-none focus:ring-2"
                :class="fieldErrors.nameEn ? 'border-rose-500 focus:ring-rose-500/20' : 'border-slate-300 dark:border-white/10 focus:ring-brand-500/20'"
              />
              <p v-if="fieldErrors.nameEn" class="text-xs text-rose-600 dark:text-rose-400">{{ fieldErrors.nameEn }}</p>
            </div>

            <!-- Vietnamese Name Input -->
            <div class="space-y-1.5">
              <label for="category-name-vi-input" class="block font-semibold text-xs sm:text-sm text-slate-700 dark:text-slate-300">
                {{ $t('admin.categories.fields.nameVi') }}
                <span class="text-rose-500">*</span>
              </label>
              <input
                id="category-name-vi-input"
                v-model="nameVi"
                type="text"
                maxlength="100"
                :disabled="isSubmitting"
                :placeholder="$t('admin.categories.fields.nameViPlaceholder')"
                class="w-full rounded-xl border bg-white dark:bg-canvas-subtle/50 px-3.5 py-2 text-sm text-slate-900 dark:text-white focus:outline-none focus:ring-2"
                :class="fieldErrors.nameVi ? 'border-rose-500 focus:ring-rose-500/20' : 'border-slate-300 dark:border-white/10 focus:ring-brand-500/20'"
              />
              <p v-if="fieldErrors.nameVi" class="text-xs text-rose-600 dark:text-rose-400">{{ fieldErrors.nameVi }}</p>
            </div>

            <!-- Sort Order Input -->
            <div class="space-y-1.5">
              <label for="category-sort-order-input" class="block font-semibold text-xs sm:text-sm text-slate-700 dark:text-slate-300">
                {{ $t('admin.categories.fields.sortOrder') }}
              </label>
              <input
                id="category-sort-order-input"
                v-model.number="sortOrder"
                type="number"
                min="0"
                max="10000"
                step="1"
                :disabled="isSubmitting"
                class="w-full rounded-xl border bg-white dark:bg-canvas-subtle/50 px-3.5 py-2 text-sm text-slate-900 dark:text-white focus:outline-none focus:ring-2"
                :class="fieldErrors.sortOrder ? 'border-rose-500 focus:ring-rose-500/20' : 'border-slate-300 dark:border-white/10 focus:ring-brand-500/20'"
              />
              <p v-if="fieldErrors.sortOrder" class="text-xs text-rose-600 dark:text-rose-400">{{ fieldErrors.sortOrder }}</p>
              <p v-else class="text-xs text-slate-500 dark:text-slate-400">{{ $t('admin.categories.fields.sortOrderHelp') }}</p>
            </div>

            <!-- Status Checkbox / Switch -->
            <div class="pt-2">
              <label class="flex items-center gap-3 cursor-pointer select-none">
                <input
                  v-model="isActive"
                  type="checkbox"
                  :disabled="isSubmitting"
                  class="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
                />
                <span class="text-sm font-semibold text-slate-800 dark:text-slate-200">
                  {{ isActive ? $t('admin.categories.status.active') : $t('admin.categories.status.inactive') }}
                </span>
              </label>
            </div>
          </div>

          <!-- Footer -->
          <footer class="flex shrink-0 items-center justify-end gap-3 border-t border-slate-200/80 dark:border-white/[0.08] px-5 sm:px-6 py-4 bg-slate-50/50 dark:bg-white/[0.02]">
            <button
              type="button"
              class="rounded-xl border border-slate-300 dark:border-white/10 px-4 py-2 text-xs sm:text-sm font-semibold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors whitespace-nowrap shrink-0"
              :disabled="isSubmitting"
              @click="emit('close')"
            >
              {{ $t('admin.categories.cancel') }}
            </button>
            <button
              type="button"
              class="flex items-center gap-2 rounded-xl bg-brand-600 px-5 py-2 text-xs sm:text-sm font-semibold text-white hover:bg-brand-500 transition-colors disabled:opacity-50 whitespace-nowrap shrink-0 shadow-sm"
              :disabled="isSubmitting || isReloading || (isConflict && isEdit) || (!!refreshedCategory && !confirmRefreshedVersion)"
              @click="handleSubmit"
            >
              <Loader2 v-if="isSubmitting" class="h-4 w-4 animate-spin shrink-0" aria-hidden="true" />
              <Check v-else class="h-4 w-4 shrink-0" aria-hidden="true" />
              <span>{{ isEdit ? $t('admin.categories.save') : $t('admin.categories.create') }}</span>
            </button>
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
