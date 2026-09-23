# Tasks

## 1. Frontend — Command Palette Scoped Focus-Visible Override

- [x] 1.1 Add `<style scoped>` block with `input:focus-visible { outline: none !important; box-shadow: none !important; }` in `frontend/components/app/AppCommandPalette.vue` to override the global `main.css` `!important` focus ring via selector specificity

## 2. Verification

- [x] 2.1 Open Command Palette (⌘K / Ctrl+K) and confirm no purple focus ring appears on the search input
- [x] 2.2 Verify search input retains focus, typing works normally, and other form inputs retain global accessibility focus ring
