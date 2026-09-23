# Tasks

## 1. Frontend — Fix z-index

- [x] 1.1 Replace `z-60` with `z-[60]` (Tailwind arbitrary value) in `AppCommandPalette.vue` — Tailwind config numeric key extension was not generating CSS; arbitrary syntax works reliably

## 2. Verification

- [x] 2.1 Open Command Palette (⌘K) on login page and confirm search overlay renders above all page content (z-index: 60 confirmed via computed styles)
