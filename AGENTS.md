# TechDaily — Conventions for AI Agents & Developers

## 1. Project Vision & Architecture
TechDaily transforms 5–10 daily minutes into Senior & Principal software engineering mastery through curated authoritative documentation slices, scenario interview drills with multimodal AI evaluation, and SM-2 spaced repetition flashcards.

```
Api            → Minimal APIs, DI wiring, JWT authentication, exception handling (RFC 7807)
Application    → Pure DI use-case handlers, DTOs, FluentValidation validators, Result Pattern
Domain         → Rich domain entities, business invariants, SM-2 algorithm, streak rules (No EF, No I/O)
Infrastructure → PostgreSQL DbContext (pgvector), Gemini 3.5 Flash Client, PasswordHasher (PBKDF2)
```

## 2. Core Invariants & Rules
- **100% English Codebase:** All classes, methods, variables, database entities, code comments, commits, and unit tests MUST be in English.
- **Source Language Preservation:** Authoritative doc excerpts strictly keep their original language (English docs in English, Vietnamese docs in Vietnamese).
- **Pure Dependency Injection:** No MediatR reflection overhead. Each handler is a dedicated class registered directly in DI.
- **SM-2 Algorithm Boundaries:** `EaseFactor` is bounded to $[1.30, 2.50]$ and progression intervals ($I_1 = 1$, $I_2 = 6$, $I_n = I_{n-1} \times \text{EF}$) are encapsulated in `SpacedRepetitionCard`.
- **PBKDF2 Password Security:** Passwords are hashed with 16-byte random salt and 100,000 SHA-256 iterations via `PasswordHasher`.
- **Soft Deletes:** Use `IsDeleted` query filter and partial unique indexes (`WHERE "IsDeleted" = false`).
- **Secrets Management:** Secrets NEVER belong in committed files. Local secrets reside in `appsettings.Local.json` and `.env` (gitignored).

## 3. Quick Start
Run the fullstack development environment with a single command:
```bash
./run-dev.sh
```
