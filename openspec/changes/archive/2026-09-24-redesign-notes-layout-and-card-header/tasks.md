# Tasks: Redesign Notes Layout & Card Header

## 1. Frontend - Layout Archetype & Open Canvas

- [x] 1.1 In `frontend/components/layout/BoardLayout.vue`, add the `flat?: boolean` prop and conditional styling to omit the enclosing `.glass-card` shell when `flat` is true
- [x] 1.2 In `frontend/pages/notes.vue`, pass `flat` to `<BoardLayout flat class="w-full">`, removing the outer wrapper card and allowing page sections to flow on the open canvas

## 2. Frontend - Note Card Structure & Header Fix

- [x] 2.1 In `frontend/pages/notes.vue`, refactor the note card header to isolate the source context: display `BookOpen` icon, `item.bookTitle`, and `item.chapterTitle` with full horizontal width, and place only the compact delete icon button on the far right
- [x] 2.2 In `frontend/pages/notes.vue`, move the primary action buttons ("Edit Note" / "Chỉnh sửa ghi chú" and "Flashcard SM-2" / "Đã Trong SM-2") from the header into a dedicated card footer separated by a top border divider (`pt-3 border-t`)
- [x] 2.3 In `frontend/pages/notes.vue`, align tag badges and action buttons in the card footer with responsive flex wrapping, ensuring clean click/tap targets across mobile and desktop

## 3. Testing & Verification

- [x] 3.1 In `frontend/tests/components/LayoutArchetypes.spec.ts`, add a unit test asserting that `BoardLayout.vue` renders in flat open-canvas mode without `.glass-card` when `flat` is passed
- [x] 3.2 In `frontend/tests/pages/notes.spec.ts`, update or add unit tests verifying the note card header contains book/chapter titles, delete button, and footer contains edit and flashcard action buttons
- [x] 3.3 Execute full frontend test suite (`npm test`) and production build (`npm run build`) to ensure zero regressions
- [x] 3.4 Capture browser preview screenshots at 1920x1080 (Desktop) and 390x844 (Mobile) in Dark and Light modes verifying removal of the outer card, legible document titles, and balanced footer controls
