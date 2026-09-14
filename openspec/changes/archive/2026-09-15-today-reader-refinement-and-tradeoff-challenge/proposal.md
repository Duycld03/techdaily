# Proposal: Today Reader Refinement & Architectural Trade-off Challenge

## Executive Summary
TechDaily is shifting from a static, hardcoded 30-day curriculum model to a dynamic **Document Pacer model (PDF / Book Pacer)** where users master long-form authoritative engineering literature chapter-by-chapter.

As the foundational first phase of this transformation, we refine the core daily focus experience on `/today`:
1. **Uninterrupted Deep Reading Sanctuary:** Eliminates the superficial, inline `MicroQuiz` from the bottom of the left reading column (`DocReaderPane.vue`), restoring focused, distraction-free reading of architectural deep dives.
2. **Elevation to Architectural Trade-off Challenge:** Upgrades the right column (`InterviewChallengePane.vue`) from an elementary multiple-choice quiz (1 correct answer vs 3 naive distractors) into a real-world **Senior Architecture Trade-off Challenge** featuring production constraints (traffic, latency SLA, budget, consistency), comparative architecture proposals (with Pros, Cons, and Failure Modes), and Gemini-powered Principal-level architectural reviews.
3. **Pacer Foundation:** Prepares the layout and state contracts on `/today` to seamlessly bind to user-selected books and active reading pacers from `/library` in subsequent phases.

## Problem Statement & Motivation
- **UX Dilution (The "Two Quiz" Anti-Pattern):** The current `/today` page places two independent A/B/C/D quizzes side-by-side: `MicroQuizCard` at the bottom of the reader pane, and `InterviewChallengePane` across the right half of the screen. This dual-quiz layout cheapens the user experience, making TechDaily look like an elementary high-school test prep app rather than a high-caliber Senior engineering platform.
- **Interrupted Reading Flow:** Senior engineers studying complex topics (e.g. LSM-Trees, Write-Ahead Logs, Distributed Consensus) need uninterrupted immersion. Injecting an elementary quiz into the text stream abruptly breaks concentration.
- **Oversimplified Interview Challenges:** Real-world architectural interviews do not have a single "magically correct" choice. Architecture is entirely about **trade-offs under concrete constraints** (e.g. 30k RPS, p99 < 20ms, zero data loss, limited hardware). Presenting options without analyzing failure thresholds fails to prepare engineers for Staff/Principal-level interviews.

## Scope of Phase 1
- **Included:**
  - Remove `MicroQuizCard` component and references from `DocReaderPane.vue`.
  - Verify and maintain clean text-selection toolbar functionality (AI Explainer modal, Highlight note creation, Copy).
  - Define the data model and UI specification for the new **Architectural Trade-off Challenge**.
  - Update `InterviewChallengePane.vue` design to showcase scenario constraints, proposal trade-offs, and Principal review explanations.
  - Maintain 100% test coverage across existing frontend and backend suites.
- **Deferred to Phase 2:**
  - Asynchronous background PDF ingestion worker and PDF Bookmarks (TOC) parsing.
  - Replacing the top `Day 1..30` selector with the active Book Pacer progress indicator.
