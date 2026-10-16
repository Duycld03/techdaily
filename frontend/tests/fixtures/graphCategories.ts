import type { CategoryDescriptor } from '~/stores/useCategoryStore'

export const graphCategory: CategoryDescriptor = {
  id: 'a17c9c32-6acf-4cf6-9b51-098398989a21',
  slug: 'cloud-platforms',
  nameEn: 'Cloud Platforms',
  nameVi: 'Nền tảng đám mây',
  sortOrder: 10,
  isActive: false,
  version: 2
}

export const otherGraphCategory: CategoryDescriptor = {
  id: 'b17c9c32-6acf-4cf6-9b51-098398989a22',
  slug: 'data-platforms',
  nameEn: 'Data Platforms',
  nameVi: 'Nền tảng dữ liệu',
  sortOrder: 20,
  isActive: true,
  version: 1
}
