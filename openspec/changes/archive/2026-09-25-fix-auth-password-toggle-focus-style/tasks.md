# Tasks

## 1. Frontend (Authentication Form Styling & Focus Ring Alignment)

- [x] 1.1 Update the password visibility toggle button in `frontend/pages/login.vue` to use centered positioning and controlled focus ring styling (`absolute right-3.5 top-1/2 -translate-y-1/2 p-1 rounded-md focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500/50`).
- [x] 1.2 Update the confirm password visibility toggle button in `frontend/pages/login.vue` to adopt identical padding and focus ring classes (`p-1 rounded-md focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500/50`) for design consistency.
- [x] 1.3 Inspect and harmonize password visibility toggle buttons in `frontend/pages/settings.vue` to adhere to the same focus ring standard.

## 2. Verification & Testing

- [x] 2.1 Run frontend test suite (`npm test`) to ensure all auth store and form tests continue to pass.
- [x] 2.2 Visually verify keyboard navigation (Tab key) on `/login` in both dark and light modes to confirm the focus outline is neatly bounded and does not collide with the input container border.
