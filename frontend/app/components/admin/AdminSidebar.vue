<script setup lang="ts">
import { computed, shallowRef } from 'vue'
import { LayoutDashboard, LibraryBig, FolderTree, FileCog, Users, ScrollText, GraduationCap, LogOut, ShieldCheck } from 'lucide-vue-next'
import { useAuthStore } from '~/stores/useAuthStore'
import { useAdminAccessStore } from '~/stores/useAdminAccessStore'
import { normalizeRoutePath } from '~/utils/adminRoutes'

const props = defineProps<{ mobile?: boolean }>()
const emit = defineEmits<{ navigate: [] }>()
const route = useRoute()
const auth = useAuthStore()
const access = useAdminAccessStore()
const isLoggingOut = shallowRef(false)

const allLinks = [
  { path: '/admin', label: 'admin.nav.dashboard', icon: LayoutDashboard, canShow: () => access.canViewAdminDashboard },
  { path: '/admin/catalog', label: 'admin.nav.documents', icon: LibraryBig, canShow: () => access.canManageCatalog },
  { path: '/admin/categories', label: 'admin.nav.categories', icon: FolderTree, canShow: () => access.canManageCategories },
  { path: '/admin/documents', label: 'admin.nav.operations', icon: FileCog, canShow: () => access.canManageDocumentOperations },
  { path: '/admin/users', label: 'admin.nav.users', icon: Users, canShow: () => access.canManageAccounts },
  { path: '/admin/audit', label: 'admin.nav.audit', icon: ScrollText, canShow: () => access.canReadAudit }
]
const links = computed(() => allLinks.filter(l => l.canShow()))
const activePath = computed(() => normalizeRoutePath(route.path))
function isActive(path: string): boolean {
  return activePath.value === path || (path === '/admin' && activePath.value === '/admin/dashboard') || (path !== '/admin' && activePath.value.startsWith(path + '/'))
}
async function logout() {
  if (isLoggingOut.value) return
  isLoggingOut.value = true
  emit('navigate')
  try { await auth.logout('/admin/login') } finally { isLoggingOut.value = false }
}
</script>

<template>
  <aside :class="props.mobile ? 'flex w-full' : 'hidden md:flex w-64 shrink-0 border-r border-slate-200/80 dark:border-white/[0.08]'" class="min-h-0 flex-col bg-white/60 dark:bg-canvas-subtle/50" data-admin-sidebar>
    <div class="flex items-center gap-3 p-5">
      <ShieldCheck class="h-5 w-5 shrink-0 text-brand-500" aria-hidden="true" />
      <span class="text-sm sm:text-base font-semibold">{{ $t('admin.nav.workspace') }}</span>
    </div>
    <nav class="flex flex-col gap-2 px-3 pb-5" :aria-label="$t('admin.nav.title')">
      <NuxtLink v-for="link in links" :key="link.path" :to="link.path" :aria-current="isActive(link.path) ? 'page' : undefined"
        class="flex min-h-11 items-center gap-3 rounded-xl px-4 py-3 text-sm sm:text-base font-semibold transition-colors whitespace-nowrap shrink-0"
        :class="isActive(link.path) ? 'bg-brand-500/10 text-brand-700 dark:text-brand-300 border border-brand-500/20' : 'text-slate-600 dark:text-slate-400 border border-transparent hover:bg-slate-100 dark:hover:bg-white/[0.04]'"
        @click="emit('navigate')">
        <component :is="link.icon" class="h-5 w-5 shrink-0" :stroke-width="1.5" aria-hidden="true" />{{ $t(link.label) }}
      </NuxtLink>
    </nav>
    <div class="mt-auto flex flex-col gap-2 border-t border-slate-200/80 dark:border-white/[0.08] p-3">
      <NuxtLink to="/today" class="flex min-h-11 items-center gap-3 rounded-xl px-4 py-3 text-sm sm:text-base font-semibold text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.04] whitespace-nowrap shrink-0" @click="emit('navigate')">
        <GraduationCap class="h-5 w-5 shrink-0" aria-hidden="true" />{{ $t('admin.nav.learning') }}
      </NuxtLink>
      <button type="button" :disabled="isLoggingOut" class="flex min-h-11 items-center gap-3 rounded-xl px-4 py-3 text-sm sm:text-base font-semibold text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-white/[0.04] whitespace-nowrap shrink-0 disabled:opacity-50" @click="logout">
        <LogOut class="h-5 w-5 shrink-0" aria-hidden="true" />{{ $t('admin.nav.logout') }}
      </button>
    </div>
  </aside>
</template>
