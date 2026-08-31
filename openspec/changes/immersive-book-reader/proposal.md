# Proposal: Immersive Document & Book Reader Mode

## Summary
Add a dedicated, GitBook/Kindle-style reading experience (`/read/[bookId]`) that allows engineers to read multi-chapter technical books, documentation series, and imported Markdown documents seamlessly with Table of Contents navigation, Next/Previous slice buttons, reading progress tracking, and auto-resume bookmarks.

## Problem Statement
The Daily Focus Hub (`/today`) is designed for disciplined, 5–10 minute daily interview drills. However, when users have dedicated study time and want to read several chapters or slices of a technical book continuously (e.g. *Designing Data-Intensive Applications*, *Vue 3 Reactivity*, *PostgreSQL 17 Internals*), the modal reader in `/library` is cramped and lacks continuous chapter progression, progress tracking, and bookmarking.

## Proposed Solution
- Create a dedicated fullscreen reader view at `/read/[bookId]` (accessible directly from `/library` with a "Read Book" button).
- Provide a collapsible Table of Contents sidebar showing all book slices with completion checkmarks.
- Implement prominent Next / Previous Slice footer navigation buttons and keyboard shortcuts (`Shift + ArrowLeft` / `Shift + ArrowRight`).
- Track and display live Reading Progress (% of book completed and current slice indicator).
- Auto-save the last read slice (bookmark) so users resume exactly where they left off.
- Retain all core reader capabilities: Markdown rendering, Shiki syntax highlighting, floating mini-toolbar for highlights/AI explanations, and end-of-slice micro-quizzes.
