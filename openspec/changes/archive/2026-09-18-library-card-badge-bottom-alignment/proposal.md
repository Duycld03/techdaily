# Proposal: Library Card Status Badge Bottom Alignment

## Why

### Executive Summary
When browsing books and technical documents in the grid layout on `/library` (`frontend/pages/library.vue`), document cards display variable title lengths ranging from 1 to 2 lines, and may or may not include author or source URL metadata. Because the upper content container uses standard block layout without flex-stretching, the bookmark resume badge ("Đang đọc dở tại Lát cắt X"), ready badge ("Sẵn sàng đọc"), and background ingestion status indicator float immediately below the title/subtitle block. This causes bookmark and status badges across neighboring cards in the same row to sit at visibly mismatched vertical positions, producing a ragged and unpolished grid appearance.

### Problem Statement
In the multi-column card grid (`grid-cols-1 md:grid-cols-2 lg:grid-cols-3`):
- **Card 1** has a 2-line title (`"827-thoi-quen-nguyen-tu-thuviensach.vn"`). Its bookmark badge (`"Đang đọc dở tại Lát cắt 5"`) sits lower down in the card body.
- **Card 2** has a 1-line title (`"aspnet-core-aspnetcore-10.0"`). Its bookmark badge (`"Đang đọc dở tại Lát cắt 21"`) floats noticeably higher (~28px higher), leaving awkward empty space above the card footer.
- **Card 3** has a 2-line title (`"30-Day Senior Fullstack Curriculum"`). Its bookmark badge (`"Đang đọc dở tại Lát cắt 30"`) sits lower down, aligning with Card 1 but sharply misaligned with Card 2.

The user specifically requested: *"Đang đọc dở tại Lát cắt 21 đẩy xuống dưới cho cùng mấy card khác"* (push the bookmark badge down so it aligns with the other cards across the grid).

#### Technical Root Cause in `frontend/pages/library.vue`
At lines 476–515 of `frontend/pages/library.vue`:
```html
<div class="... flex flex-col justify-between space-y-4 group ...">
  <div>
    <!-- Category & Chunks Header -->
    ...
    <!-- Title (line-clamp-2) -->
    <h3 class="... line-clamp-2 leading-snug">{{ book.title }}</h3>
    <!-- Optional Subtitle -->
    <p v-if="book.authorOrSourceUrl" ...>{{ book.authorOrSourceUrl }}</p>

    <!-- Bookmark Badge / Ready Badge / Processing Indicator -->
    <div v-if="bookmarks[book.id]" ...>...</div>
    <div v-else-if="book.status === 'Ready' ..." ...>...</div>
    <div v-if="book.status === 'Processing' ..." ...>...</div>
  </div>

  <!-- Card Action Footer -->
  <div class="pt-4 border-t ...">...</div>
</div>
```
The outer card container defines `flex flex-col justify-between`. However, its upper child is a standard block `<div>` without flex expansion (`flex-1`) or flex column layout (`flex flex-col`). In CSS Grid, cards within a row automatically stretch to match the height of the tallest card. With `justify-between`, space is distributed between the upper `<div>` and the footer `<div class="pt-4 border-t ...">`. But inside the upper `<div>`, elements follow normal document flow: the bookmark or status badge is attached directly to the bottom of `<h3>` or `<p>`. When a title occupies 1 line instead of 2 lines, the badge remains anchored high up in the card, creating an uneven, ragged baseline across adjacent columns.

---

## What Changes

### Proposed Solution
We propose a lightweight, purely CSS Flexbox restructuring of the book card upper content wrapper in `frontend/pages/library.vue`:
1. **Convert Upper Content Wrapper to Flex-1 Column:**
   - Change `<div>` to `<div class="flex flex-col flex-1">`.
   - This causes the upper container to stretch and fill all available vertical space inside the card above the footer actions divider.
2. **Anchor Status and Bookmark Badges to Bottom with `mt-auto`:**
   - Wrap the status badge elements (bookmark resume badge, ready badge, and processing ingestion indicator) inside a dedicated container anchored with `mt-auto pt-3`:
     ```html
     <div class="mt-auto pt-3">
       <!-- Bookmark Badge if exists -->
       <div v-if="bookmarks[book.id]" ...>...</div>
       <!-- Ready Badge if no bookmark -->
       <div v-else-if="book.status === 'Ready' || (book.status as any) === 2" ...>...</div>
       <!-- In-Progress Ingestion Indicator -->
       <div v-if="book.status === 'Processing' || (book.status as any) === 1" ...>...</div>
     </div>
     ```
   - In a flex column, `margin-top: auto` (`mt-auto`) automatically consumes all leftover vertical space between the title/subtitle block and the badge wrapper.
   - Whether a title has 1 line or 2 lines, and whether the subtitle (`authorOrSourceUrl`) exists or is omitted, the badge container is pinned flush to the bottom, resting at an identical horizontal baseline immediately above the card footer border (`pt-4 border-t`).

### Scope and Non-Goals
- **In Scope:**
  - Refactoring the template structure of the book card in `frontend/pages/library.vue`.
  - Adding unit and integration tests in `frontend/tests/pages/library.spec.ts` asserting flex layout classes (`flex flex-col flex-1` and `mt-auto pt-3`) and baseline consistency.
- **Non-Goals:**
  - Imposing rigid, hardcoded pixel min-heights (e.g. `min-h-[56px]` on `<h3>`), which look unnatural when titles are short on mobile or when font scaling changes.
  - Modifying the card footer actions (Delete, Export to Obsidian, Continue Reading / Read Slices CTA).
  - Altering backend C# models, API payloads, or database schema.
  - Modifying badge styling, colors, or badge translation logic.

### Verification Plan
1. **Automated Vitest Suite:**
   - Add unit tests in `frontend/tests/pages/library.spec.ts` verifying that book cards render the upper content container with `flex flex-col flex-1` and the badge container with `mt-auto pt-3`.
   - Assert consistent DOM structure across books with 1-line titles, 2-line titles, with and without bookmarks, and in processing status.
   - Run `npm --prefix frontend test` to ensure 100% test pass rate without regressions.
2. **Visual Layout Verification:**
   - Inspect `/library` in light and dark modes across grid view breakpoints:
     - Desktop (3 columns): Badges align horizontally across rows.
     - Tablet (2 columns): Badges align horizontally across pairs.
     - Mobile (1 column): Badges anchor cleanly above card footer with consistent spacing.
3. **OpenSpec Standards Compliance:**
   - Run `openspec validate --strict library-card-badge-bottom-alignment` to verify complete schema conformity.

---

## Capabilities

### Modified Capabilities
- `library`: Mandates consistent horizontal baseline alignment for book card status indicators (bookmark resume badge, ready badge, and background ingestion progress indicator) across grid columns, eliminating ragged baselines caused by variable title or subtitle heights.

---

## Impact

- **Frontend Template (`frontend/pages/library.vue`):** Replaces the plain block upper wrapper with `flex flex-col flex-1` and wraps badge elements in `<div class="mt-auto pt-3">`.
- **Frontend Test Suite (`frontend/tests/pages/library.spec.ts`):** New test coverage asserting CSS classes and structure for card content stretching and bottom-pinned badge containers.
- **Backend & APIs:** Zero impact. No API contracts, data models, or endpoints are modified.
