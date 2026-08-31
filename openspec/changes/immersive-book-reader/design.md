# Design: Immersive Document & Book Reader Mode

## Architecture & Component Hierarchy

```
frontend/pages/read/[bookId].vue
├── ReaderHeader (Book Title, Progress Bar, Bookmark Indicator, Sidebar Toggle)
├── ReaderBody
│   ├── TableOfContentsSidebar (Desktop sticky sidebar & Mobile drawer)
│   └── ReadingArticlePane
│       ├── ChapterHeader (Estimated read time, Category badge, Chapter title)
│       ├── MarkdownBody (Rendered doc slice content)
│       ├── KeyTakeaways
│       ├── MicroQuizCard (Active recall test)
│       └── SliceFooterNav (Previous Slice Button, Next Slice Button)
└── FloatingToolbar (Scoped text selection menu: Explain with AI + Copy)
```

## User Flow
1. User clicks **"Read Book"** on a book card in `/library`.
2. Router navigates to `/read/:bookId`.
3. The page loads `BookDetail` (with `chunks: ChunkSummary[]`) from `useLibraryStore.fetchBookById(bookId)`.
4. The page checks `localStorage.getItem('techdaily_bookmark_' + bookId)`:
   - If a bookmark exists (e.g. slice 4), it opens slice 4.
   - Otherwise, it defaults to slice 1 (`chunks[0]`).
5. When the user finishes reading slice 1, they click **"Next Slice: #2 Chapter Title →"**:
   - Updates active slice index to 1.
   - Smoothly scrolls reader pane to `scrollTop: 0`.
   - Saves bookmark `slice: 2` to `localStorage`.
   - Updates progress bar (e.g. `2 / 12 (17%)`).
6. When reaching the last slice, the button changes to **"🎉 Finish Document / Return to Library"**.

## Keyboard Shortcuts
- `Shift + ArrowRight`: Next slice.
- `Shift + ArrowLeft`: Previous slice.
- `Escape`: Return to `/library`.
