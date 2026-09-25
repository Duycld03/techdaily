# Proposal

## Why

TechDaily's core architectural invariant defines the platform as an open, technology-agnostic active recall engine with zero mandatory curriculum boundaries or arbitrary calendar-day deadlines. Currently, however, the platform seeds a single global, undeletable book (`30-Day Senior Fullstack Curriculum` with `CreatedByUserId = null`). This breaks user ownership: users cannot delete the default curriculum, the library mixes shared system items with user-uploaded documents, and the content imposes an artificial 30-day schedule while being tied to specific frontend frameworks (Vue 3, Nuxt 4) rather than universal engineering foundations.

Transitioning to a **User-Centric Starter Handbook** model ensures that:
1. Every newly registered user receives their own copy of the *Senior Engineering Craft Handbook*, fully owned by them (`CreatedByUserId = user.Id`).
2. Users have complete autonomy to read, track pacing, generate flashcards, or delete the handbook to achieve a clean **Empty State** for learning purely from their own uploaded PDFs and web docs.
3. The handbook is technology-agnostic and organized purely as chapters of a book rather than an artificial 30-day course, focusing on core concepts across Frontend Systems, Backend Runtimes, Database Storage, and Distributed Systems, using code snippets only as illustrative examples rather than framework-specific silos.

## What Changes

- **Technology-Agnostic Starter Handbook Content**:
  - Rewrite `curriculum-30-days.json` and seeding definitions into the *Senior Engineering Craft Handbook* (`senior-engineering-craft-handbook.json`).
  - Eliminate the artificial "30-day" construct: organize the handbook naturally into numbered chapters/slices across core engineering pillars.
  - Eliminate framework-specific lock-in (e.g. replacing Vue/Nuxt-specific titles and summaries with universal concepts such as Reactive State Propagation, Hydration & Modern Web Rendering, Browser Rendering Pipelines, and Core Web Vitals).
  - Backend runtime, database storage, and distributed systems chapters focus on universal mechanics (Generational GC, Contiguous Memory Buffers, Non-blocking Event Loops, MVCC & WAL, Isolation Anomalies, B-Tree/LSM Engines, Cache Stampede, Outbox Pattern, Distributed Throttling).
  - Code examples in TypeScript, C#, SQL, and Go are retained as concrete conceptual illustrations without branding the curriculum around specific tech stacks.

- **User-Centric Starter Book Provisioning**:
  - Upon user registration (via standard email/password or Google OAuth), the platform automatically provisions an instance of the *Senior Engineering Craft Handbook* assigned directly to that user (`CreatedByUserId = user.Id`).
  - Seeding logic for development and database initialization no longer creates an immutable global book with `CreatedByUserId = null`; instead, it seeds a reusable starter catalog template or provisions books per active user.

- **Library Ownership & Deletion Autonomy**:
  - `GET /api/v1/library/books` queries and returns only books owned by the authenticated user (`CreatedByUserId == currentUserId`).
  - `DELETE /api/v1/library/books/{id}` succeeds for the starter handbook because the book is owned by the user, cascading soft-delete to its chunks, questions, and pacers.
  - When a user deletes all books, `/library`, `/today`, and `/roadmap` render clear empty states encouraging custom document ingestion (PDF upload, URL crawl, Markdown import).

- **Decouple Hardcoded Curriculum Fallbacks**:
  - Refactor `/today` and `/roadmap` backend handlers and frontend stores to gracefully handle the zero-book state with an explicit empty state rather than falling back to an undeletable static topic set.

## Capabilities

### Modified Capabilities

- `core-platform`: Update curriculum seeding, starter handbook provisioning on registration, and technology-agnostic content invariants.
- `library`: Enforce user-scoped book listing, allow deletion of starter handbooks by their owners, and support smooth empty-state transitions.
