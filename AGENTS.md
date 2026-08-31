# TechDaily — Conventions for AI Agents & Developers

Read `docs/` before writing code. The specs and invariants there are the single source of truth.

| Question | Source of Truth |
|---|---|
| What are the business rules and invariants? | `docs/domain-rules.md` |
| What endpoints exist & what are their contracts? | `docs/api-design.md` |
| What does the database schema look like? | `docs/database-design.md` |
| What is the 30-day curriculum structure? | `docs/curriculum-30-days.md` |
| What active features are being planned or built? | `openspec/changes/` |

---

## 1. Project Layering & Clean Architecture
```
Api            → Minimal APIs, DI wiring, JWT authentication (JwtBearer), RFC 7807 problem details
Application    → Pure DI use-case handlers, DTOs, FluentValidation validators, Result Pattern
Domain         → Rich domain entities, business invariants, SM-2 algorithm (No EF, No I/O)
Infrastructure → PostgreSQL DbContext (pgvector), Gemini 3.5 Flash Client, PasswordHasher (PBKDF2)
```

Dependencies point inward only: `Api → Application → Domain`, `Infrastructure → Application → Domain`.

---

## 2. Core Invariants & Rules
- **100% English Codebase:** All classes, methods, variables, database entities, code comments, commits, and unit tests MUST be in English.
- **Source Language Preservation:** Authoritative doc excerpts strictly keep their original language (English docs in English, Vietnamese docs in Vietnamese).
- **Pure Dependency Injection:** No MediatR reflection overhead. Each handler is a dedicated class registered directly in DI.
- **SM-2 Algorithm Boundaries:** `EaseFactor` is bounded to $[1.30, 2.50]$ and progression intervals ($I_1 = 1$, $I_2 = 6$, $I_n = I_{n-1} \times \text{EF}$) are encapsulated in `SpacedRepetitionCard`.
- **PBKDF2 Password Security:** Passwords are hashed with 16-byte random salt and 100,000 SHA-256 iterations via `PasswordHasher`.
- **Soft Deletes:** Use `IsDeleted` query filter and partial unique indexes (`WHERE "IsDeleted" = false`).
- **Secrets Management:** Secrets NEVER belong in committed files. Local secrets reside in `appsettings.Local.json` and `.env` (gitignored).

---

## 3. Strict Rules & Anti-Patterns to NEVER Repeat
1. **Never use Fake/Default Fallback Users:** Endpoints requiring auth must use `.RequireAuthorization()` and return `401 Unauthorized` when no valid JWT is present. Never inject hardcoded default GUIDs.
2. **Never add Dev 1-Click Bypass Auth:** Always implement real, production-ready standard email/password and OAuth flows.
3. **Never attach Global `@mouseup` Selection Listeners:** Scoped floating action toolbars must only appear on reading markdown and must NEVER trigger on quiz clicks, copy, or translate.
4. **Never skip OpenSpec on New Features:** Major capabilities or schema changes MUST be specified in `openspec/changes/` before writing code.
5. **Always test Every Invariant:** Every new handler, store, and component gets automated tests (`dotnet test` for backend, `npm test` for frontend).

---

## 4. Quick Start
Run the fullstack development environment with a single command:
```bash
./run-dev.sh
```
