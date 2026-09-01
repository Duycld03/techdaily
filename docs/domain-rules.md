# TechDaily — Domain Invariants & Engineering Rules

This document defines the strict non-negotiable rules, invariants, and anti-patterns for TechDaily. Violating these breaks platform stability and security.

---

## 1. Authentication & Security Invariants
1. **Zero Fake Data / No Default User Fallbacks:**
   - Handlers and Endpoints that require user context MUST use `.RequireAuthorization()`.
   - Never inject hardcoded fallback Guids (e.g. `00000000-0000-0000-0000-000000000001`) or dummy accounts into production endpoints.
   - If an unauthenticated request reaches an endpoint requiring user context, it MUST immediately return `HTTP 401 Unauthorized`.
2. **Password Security:**
   - Passwords MUST be hashed using PBKDF2 with HMAC-SHA256, a 16-byte cryptographically secure random salt, and 100,000 iterations via `PasswordHasher`.
   - Minimum password length is 6 characters.
3. **JWT Bearer Token Standards:**
   - Tokens MUST include `ClaimTypes.NameIdentifier` (`sub`), `Email`, and `Name`.
   - `Jwt:Issuer` and `Jwt:Audience` MUST be strictly synchronized across token generation (`AuthEndpoints.cs`) and validation pipeline (`Program.cs`).
4. **Secrets Management:**
   - Secrets, API keys, and private tokens MUST NEVER be committed to Git. They reside exclusively in gitignored `appsettings.Local.json` and `.env`.
5. **Hybrid Identity & Password Initialization for OAuth Accounts:**
   - Accounts created via Google OAuth (`PasswordHash == null`) MUST be permitted to establish an initial password via `PUT /api/v1/user/change-password` without requiring an existing `CurrentPassword`.
   - Once established, the account transitions to `hasPassword: true`, enabling login via both Google OAuth and standard Email/Password credentials across all devices (desktops, mobile networks, private browsers).
   - Accounts with existing passwords (`PasswordHash != null`) MUST verify `CurrentPassword` before applying any password changes.

---

## 2. Document Processing & Ingestion Invariants

### A. PDF Ingestion & Extraction (`PdfPigExtractor`)
1. **Zero Large-Object-Heap (LOH) Streaming:**
   - PDF files MUST be processed via non-buffering streams (`IFormFile.OpenReadStream()`).
   - If the incoming stream is non-seekable, it must be safely buffered into a temporary seekable stream without leaking unmanaged memory.
2. **Safety Boundaries (50-60% Gemini Free Tier Capacity):**
   - Maximum file size: **200 MB**.
   - Maximum page count: **800 pages** (~500,000 tokens). Exceeding this throws `InvalidOperationException`.
3. **Geometric Baseline Line Grouping:**
   - Words extracted via `page.GetWords()` MUST be grouped geometrically by their baseline Y-coordinate (`BoundingBox.Bottom`) with a tolerance factor (~3.5 points) to preserve natural paragraph line breaks and code indentations.
4. **PostgreSQL UTF-8 Null-Byte (`\0`) Sanitization:**
   - Raw PDF text extractions containing null bytes (`\0`, `0x00`) or control characters MUST be sanitized via `Regex.Replace(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "")` before writing to PostgreSQL text columns.
5. **Continuous Code Block Unification:**
   - Heuristics MUST detect continuous code lines, comments (`//`, `/*`), keywords, and block braces to prevent fragmented, multi-box code rendering.

### B. Web Article Crawling (`WebArticleCrawler`)
1. **Target Content Isolation:**
   - HTML documents MUST be cleaned with `HtmlAgilityPack` to isolate primary article containers (`<article>`, `<main>`, `.markdown-body`).
   - All junk tags (`<script>`, `<style>`, `<nav>`, `<footer>`, `<aside>`, ads, iframes) MUST be stripped.
2. **Code Syntax & Alert Preservation:**
   - Code classes (`lang-csharp`, `language-typescript`) MUST be mapped to standardized Markdown code fences ` ```{lang} `.
   - Documentation callouts (`<div class="NOTE">`, `<div class="TIP">`, `<div class="WARNING">`) MUST be converted to Markdown blockquotes.
3. **GitHub Raw Link Auto-Resolution:**
   - GitHub web URLs (`github.com/.../blob/...`) MUST be auto-resolved to `raw.githubusercontent.com` before fetching.

---

## 3. UI/UX, Typography & Syntax Highlighting Invariants
1. **Shiki Syntax Highlighting & Code Copying:**
   - Code blocks MUST be rendered with **Shiki** (TextMate grammar engine) matching IDE-grade syntax colors for C#, TypeScript, JavaScript, SQL, Python, Go, JSON, Bash, YAML, Dockerfile.
   - Every code block MUST feature a language header badge and an animated **1-Click Copy** button.
   - Code block language auto-detection MUST infer the correct language (e.g. C# for `.NET` constructs, SQL for database statements) when untagged.
2. **Text Selection & Tooltip Etiquette:**
   - NEVER attach global or root-level selection listeners that trigger full modals or API calls on `@mouseup`.
   - Selection actions MUST appear as a **discreet floating mini-menu** near the highlighted text inside the reading pane only.
   - Interactive elements (Micro Quizzes, buttons, textareas, inputs) MUST NEVER trigger selection tooltips or AI lookups.
   - Users selecting text to copy (`Ctrl+C`) or translate MUST NOT have their flow interrupted.
3. **Typography & Dual Theme Standards:**
   - **Responsive Typography Scale:** Body text, article paragraphs, summaries, card descriptions, scenario options, and explanations MUST be at least `text-sm` (14px) on mobile viewports and scale to at least `text-base` (16px) or `text-lg` (18px) on large viewports (`≥ md`).
   - Micro text (`text-xs` / 12px) is forbidden for readable copy/descriptions and is strictly reserved for compact metadata (tags, badges, timestamps, tooltips). Sub-12px micro text (e.g. `text-[10px]`) is prohibited.
   - All components MUST support dual theme classes (`bg-white dark:bg-slate-900`, `text-slate-900 dark:text-white`, `border-slate-200 dark:border-slate-800`).
4. **Bilingual UI Verification & Length Resilience (English & Vietnamese):**
   - Both English and Vietnamese MUST be verified in responsive layouts. While Vietnamese phrases are often longer for general prose (*"Bảo Mật & Mật Khẩu"* vs *"Security"*), English phrases can be significantly longer for action calls (*"Continue Reading"* vs *"Đọc tiếp"*, *"Resumes at Slice 8"* vs *"Đọc tiếp lát 8"*).
   - All interactive action buttons, card footers, and badges MUST enforce `whitespace-nowrap shrink-0` with minimum `gap-2` or `gap-3` to prevent multi-line text wrapping or layout collisions across all screen sizes and language switches.
5. **Header Action Buttons & Touch Targets:**
   - Action buttons in header banners (e.g., *Ngẫu Nhiên* and *Tạo Với AI* on Insights) MUST maintain visual balance and avoid awkward wrapping.
   - On large screens (`≥ sm`), action buttons MUST sit side-by-side horizontally (`flex-nowrap`, `shrink-0`) aligned with the header.
   - On mobile screens, button pairs MUST distribute space evenly (`flex-1`) with minimum 44px touch targets (`h-11 sm:h-12`) and `text-sm md:text-base font-bold`.
6. **Production-Grade UI Purity & Zero Local-Dev Workarounds:**
   - All frontend components, layouts, banners, toasts, and localized copy MUST be designed strictly for production environments (e.g. registered domains, HTTPS, fully functional OAuth).
   - NEVER inject UI banners, callouts, or workarounds explaining or mitigating local development constraints (e.g. LAN IP OAuth origin mismatches, localhost port conflicts).
   - Infrastructure, environment troubleshooting, and local machine setup guides belong strictly in developer documentation (`README.md`, `docs/dev-setup.md`), NEVER in production user-facing UI.
7. **Prohibition of Browser Native Dialogs (`alert`, `confirm`, `prompt`):**
   - Native popups block execution threads, are impossible to theme, and degrade the user experience.
   - All interactive confirmations (e.g. deleting highlights, removing library documents) MUST use custom Vue modals rendered via `<Teleport to="body">` with standard dark/light theme tokens and bilingual localization.

---

## 4. Spaced Repetition (SM-2) Invariants
1. **Mathematical Boundaries:**
   - $EF' = EF + (0.1 - (5 - q) \times (0.08 + (5 - q) \times 0.02))$
   - Ease Factor ($EF$) is bounded to $[1.30, 2.50]$.
   - Progression intervals: $I_1 = 1 \text{ day}$, $I_2 = 6 \text{ days}$, $I_n = I_{n-1} \times EF$.
   - A grade of $q < 3$ resets $I$ to 1 and `RepetitionCount` to 0.

---

## 5. Infinite Tech Insights Feed & AI Synthesis Invariants
1. **AI Output Token Budget & Reasoning Resilience:**
   - LLM generation endpoints (e.g. Gemini 3.6 Flash) MUST specify a sufficient `maxOutputTokens` budget (at least **8,192 tokens**) to account for internal reasoning thought tokens and prevent truncated JSON structures.
2. **Language Strictness in AI Synthesis:**
   - If the user explicitly mentions a target programming language (e.g., Rust, Go, Python, TypeScript, C#, SQL, Zig), the AI generator MUST generate valid, idiomatic code snippets (`problemSnippet`, `solutionSnippet`) written strictly in that requested language.
3. **Syntax Highlighter Tag Priority:**
   - Shiki code block language detection MUST check explicit tags (e.g., `#rust`, `#go`, `#python`, `#sql`, `#vue`) before falling back to generic token regexes to avoid false positives (e.g., Rust `let mut` vs JavaScript `let`).
4. **Header Banner & Reader Theme Parity:**
   - All banner headers, progress trackers, and reader cards MUST dynamically adapt between Light Mode (`bg-gradient-to-br from-indigo-50/80 via-white to-sky-50/50 text-slate-900`) and Dark Mode (`dark:from-slate-900 dark:via-slate-900 dark:to-indigo-950 dark:text-white`).

---

## 6. Infrastructure, Reverse Proxy & Deployment Invariants
1. **Docker Compose Upstream DNS Caching (502 Bad Gateway Prevention):**
   - Nginx caches upstream container IPs at startup. When backend/frontend containers are rebuilt and assigned new internal IPs, Nginx MUST be restarted via `docker compose restart nginx` as the final step in the CI/CD deploy pipeline.
2. **Reverse Proxy Relative Path Routing:**
   - In production behind Nginx reverse proxy, frontend API requests MUST use relative paths (`""`) on port 80/443 rather than attempting to connect to closed backend ports (e.g. `:5000`), which are blocked by host firewalls.
3. **SSH Key Standards (ED25519 & Cloud Metadata Synchronization):**
   - All SSH keys MUST use modern `ED25519` format (`ssh-ed25519`) for compatibility with modern Linux distros (Debian 13+).
   - On cloud virtual machines (GCP Compute Engine), public keys MUST be registered in VM Metadata (`<protocol> <key-data> <username>`) to prevent the Cloud Guest Agent from removing them from `~/.ssh/authorized_keys` upon VM reboot.
4. **SSL / HTTPS Bootstrapping Discipline:**
   - Nginx fails to start if `ssl_certificate` directives point to non-existent certificate paths.
   - When configuring SSL for a new domain, obtain Let's Encrypt certificates first (`certbot certonly --standalone` or via webroot `/.well-known/acme-challenge/`), verify certificate existence, and then mount `/etc/letsencrypt` and enable `listen 443 ssl`.

---

## 7. AI Terminology & Floating Explainer Invariants
1. **Selection Length Capacity:**
   - Scoped floating selection listeners in reader panes MUST support text selections between **2 and 500 characters** (approx. 70–80 words), accommodating compound technical concepts and multi-clause architectural definitions.
2. **Category Resilience & Defaults:**
   - The `ExplainTerm` use-case handler and validator MUST treat `Category` as optional. If omitted or whitespace, default to `"Software Architecture"` rather than failing validation.
3. **Markdown Rendering Purity:**
   - AI-generated terminology explanations containing Markdown (`**bold**`, `` `code` ``, lists) MUST be rendered using `useMarkdownRenderer()` (`v-html="renderedExplanation"`) styled with Tailwind `prose` classes, NEVER raw string interpolation.

---

## 8. Anti-Patterns to NEVER Repeat

| Anti-Pattern | Why it is Forbidden | Correct Approach |
| :--- | :--- | :--- |
| **Dev 1-Click Login Bypass** | Bypasses real security flow and leads to broken production auth | Standard Email/Password registration + Google OAuth 2.0 with real JWT tokens |
| **Hardcoded User Fallback Guids** | Masks missing authentication, leaks fake data to unauthenticated users | Return `401 Unauthorized` and enforce `.RequireAuthorization()` |
| **Indiscriminate `@mouseup` Listeners** | Triggers unwanted popups during quiz clicks, copy, or translate | Scoped selection listener with floating mini-toolbar on reading markdown only |
| **Unsanitized PDF Binary Ingestion** | Null bytes `0x00` in raw PDF streams crash PostgreSQL database with error 22021 | Sanitize control characters before saving entities in EF Core |
| **Inline Composable Invocations** | Calling `useI18n()` inside async event handlers throws Vue lifecycle errors | Destructure composables at top level of `<script setup>` |
| **Hardcoded Dark Theme Classes** | Breaks light mode and makes UI illegible in bright environments | Dual Tailwind classes (`dark:bg-slate-950 bg-slate-50`) synced with `@nuxtjs/color-mode` |
| **Hardcoded C# Language in Code Blocks** | Causes SQL, Rust, Python, and TypeScript to be labeled and styled as C# | Dynamic language inference + explicit tag priority with Shiki highlighter |
| **Low Token Budget for AI Reasoning Models** | Causes `MAX_TOKENS` truncation and broken JSON fallbacks | Set `maxOutputTokens >= 8192` for structured JSON output |
| **Omission of Nginx Restart on Deploy** | Causes Nginx to hold stale container IPs and return `502 Bad Gateway` after container recreate | Add `docker compose restart nginx` after `docker compose up -d --build` |
| **RSA Key Exclusivity on Modern Linux** | Debian 13/OpenSSH 9.8+ deprecates legacy RSA and causes publickey auth failures | Standardize on modern `ED25519` keys across local config and CI/CD secrets |
| **Raw Text Interpolation for AI Tooltips** | Shows raw `**bold**` asterisks and backticks in UI popups | Parse AI markdown through `useMarkdownRenderer()` in a `prose` container |
