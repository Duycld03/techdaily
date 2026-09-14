# Proposal: Doc Pacer Engine & Asynchronous Large PDF Ingestion

## Executive Summary
TechDaily is advancing from a static, hardcoded 30-day curriculum into an active, long-form literature mastery platform: **The Doc Pacer Model**. Under this paradigm, engineers study authoritative, large-scale technical books (e.g. *Designing Data-Intensive Applications*, *CLR via C#*, *PostgreSQL 17 Internals*, and comprehensive multi-thousand-page guides like *Microsoft Learn ASP.NET Core 10*) at a sustainable daily pace.

This change delivers three foundational architectural capabilities:
1. **Asynchronous Large PDF Ingestion Pipeline:** Replaces synchronous HTTP processing with non-blocking, zero-LOH disk streaming (`bufferSize = 80KB`), supporting technical books up to **300 MB** and **8,000+ pages**. Ingestion runs on an in-memory `System.Threading.Channels` background worker with realtime progress reporting (0% to 100%).
2. **PDF Bookmarks & Chapter-Aware Structuring:** Reads native PDF Outline/Bookmarks trees to extract exact chapter boundaries and page offsets while automatically pruning publisher front-matter (dedications, roman numeral prefaces, copyright) and back-matter (indexes, bibliographies).
3. **Look-Ahead JIT Pre-Generation Buffer:** Eliminates the trade-off between upfront batch generation overload and read-time latency. Slices maintain a sliding buffer of 3 pre-generated Senior Trade-off Challenges. If a user rapidly navigates forward, an intelligent non-blocking AI Synthesis card appears with immediate priority queue promotion, ensuring 0ms typical latency and zero user-facing AI failures.
4. **Doc Pacer Navigation & Roadmap Synchronization:** Replaces the static `Day 1..30` selector on `/today` and `/roadmap` with a dynamic **Active Book Pacer**. Features seamless 1-click book switching, auto-selection of a pre-seeded featured book for zero-state visitors, and chapter-based milestone tracking.

## Problem Statement & Motivation
- **The Large Document Ingestion Wall:** Real-world technical books are massive (often 40MB–250MB, containing thousands of pages). Synchronous HTTP ingestion triggers Nginx 504 timeouts, browser disconnects, and Large Object Heap (LOH) memory spikes that crash low-resource cloud servers.
- **TOC Noise & Naive Chunking:** Naive word-count segmentation cuts across sentences and pollutes the curriculum with prefaces, publisher dedications, and indexes. True technical depth requires preserving the author's intentional chapter structure.
- **The AI Generation Dilemma:** Generating AI scenario drills for an entire 8,000-page document upfront requires hundreds of API calls (exhausting daily rate limits and wasting 90% of tokens on unread chapters), while generating drills strictly on-demand introduces disruptive 3–5s latency and exposes users to runtime AI failures.
- **Static Curriculum Rigidity:** The existing 30-day fixed curriculum cannot adapt to what an engineer is actively studying in their professional work (e.g. preparing for a distributed systems interview vs mastering database internals).

## Scope of Change
- **Backend (.NET 10 / ASP.NET Core):**
  - Increase upload size limit to 300 MB.
  - Implement `FileStream` disk-spooling temporary ingestion service (Zero-LOH).
  - Scaffold `System.Threading.Channels` queue and `IHostedService` background worker.
  - Enhance `IPdfExtractor` / `PdfPigExtractor` to parse PDF Bookmarks / Outline tree.
  - Add `UserBookPacer` domain entity and migration.
  - Implement Look-Ahead generation background service with Priority Channel.
  - Add API endpoints for pacer status, active book switching, and ingestion progress polling.
- **Frontend (Nuxt 4 / Vue 3):**
  - Refactor `/today` top bar: replace day dropdown with Pacer Bar (Active Book, Chapter, Slice, Progress %, 1-click Switcher).
  - Add AI Synthesis Skeleton card in `InterviewChallengePane.vue` for slices whose drills are actively generating.
  - Update `/roadmap` to render chapter-based milestones of the active book.
  - Update `/library` upload modal to display asynchronous progress bar.
