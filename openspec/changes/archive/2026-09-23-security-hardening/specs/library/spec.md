# Spec Delta

## ADDED Requirements

### Requirement: Authenticated AI curation and generation endpoints
The AI-powered endpoints that invoke external generation services SHALL require JWT authentication and per-user rate limiting. Specifically, the slice curation endpoint (`/api/v1/library/books/{id}/slices/{order}/curate`), the term explanation endpoint, and the chunk challenge endpoint SHALL reject unauthenticated requests with `HTTP 401 Unauthorized`. These endpoints SHALL also enforce per-user rate limits to protect external AI provider quota from abuse by individual authenticated users.

#### Scenario: Unauthenticated request to curate a slice
- **WHEN** an unauthenticated client calls `GET /api/v1/library/books/{id}/slices/{order}/curate`
- **THEN** the system returns `HTTP 401 Unauthorized`

#### Scenario: Authenticated request to curate a slice
- **WHEN** an authenticated user calls `GET /api/v1/library/books/{id}/slices/{order}/curate`
- **THEN** the system processes the curation request normally

#### Scenario: Rate-limited user exceeds AI endpoint quota
- **WHEN** an authenticated user exceeds the per-user rate limit on AI generation endpoints
- **THEN** the system returns `HTTP 429 Too Many Requests`

### Requirement: Book deletion ownership verification
The delete book endpoint SHALL verify that the requesting user is the owner of the book (the user who created or imported it, identified by the `CreatedByUserId` field on `DocumentBook`). If the requesting user is not the owner, the system SHALL return `HTTP 403 Forbidden` with error code `LIBRARY_FORBIDDEN`. Books with no recorded owner (`CreatedByUserId` is null, from pre-migration data) SHALL also be rejected with `HTTP 403 Forbidden` — deletion of unowned legacy books requires an explicit admin mechanism not in scope for this change.

#### Scenario: Owner deletes their own book
- **WHEN** the user who created a book sends `DELETE /api/v1/library/books/{id}`
- **THEN** the system soft-deletes the book and returns `HTTP 200`

#### Scenario: Non-owner attempts to delete a book
- **WHEN** a user who did not create the book sends `DELETE /api/v1/library/books/{id}`
- **THEN** the system returns `HTTP 403 Forbidden` with `{ "code": "LIBRARY_FORBIDDEN" }`

#### Scenario: Deleting a legacy book with no owner
- **WHEN** an authenticated user sends `DELETE /api/v1/library/books/{id}` for a book where `CreatedByUserId` is null
- **THEN** the system returns `HTTP 403 Forbidden` with `{ "code": "LIBRARY_FORBIDDEN" }`

### Requirement: PDF upload content validation
The PDF upload endpoint SHALL validate uploaded files by checking that the file stream begins with the PDF magic bytes (`%PDF-`) in addition to the existing file extension check. Files that do not pass both checks SHALL be rejected with `HTTP 400 Bad Request` and error code `INVALID_PDF_FORMAT`. The existing 350 MB file size limit, request timeouts (nginx 300s proxy_read_timeout), and `CancellationToken` propagation SHALL be preserved. On validation failure after the file has been written to temporary storage, the temporary file SHALL be cleaned up.

#### Scenario: Valid PDF file uploaded
- **WHEN** an authenticated user uploads a file with `.pdf` extension whose content starts with `%PDF-`
- **THEN** the system accepts the upload and begins processing

#### Scenario: Non-PDF file with .pdf extension uploaded
- **WHEN** a user uploads a file named `malware.pdf` whose content does not start with `%PDF-`
- **THEN** the system returns `HTTP 400` with `{ "code": "INVALID_PDF_FORMAT" }` and any temporary file is cleaned up

### Requirement: SSRF validation with pinned DNS resolution and redirect protection
The URL security validator SHALL resolve the target hostname's DNS once and pin the resolved IP address through to the HTTP client connection via `ConnectCallback`. The `ConnectCallback` SHALL preserve the original hostname for TLS SNI and certificate validation while directing the TCP connection to the validated IP address.

HTTP clients performing SSRF-validated requests SHALL disable automatic redirect following (`AllowAutoRedirect = false`). If redirect following is required, each redirect destination SHALL be validated through the same SSRF validation pipeline (URL parsing, scheme validation, DNS resolution, IP validation, and connection pinning) before the redirect is followed.

The validation SHALL reject the following IP ranges: private IPv4 (10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16), loopback (127.0.0.0/8), link-local (169.254.0.0/16), CGNAT/shared (100.64.0.0/10), multicast IPv4 (224.0.0.0/4), broadcast (255.255.255.255), unspecified IPv4 (0.0.0.0/8), IPv6 loopback (::1), IPv6 link-local (fe80::/10), IPv6 site-local (fec0::/10), IPv6 unique-local (fc00::/7), IPv6 multicast (ff00::/8), IPv6 unspecified (::), IPv4-mapped IPv6 addresses (::ffff:0:0/96) where the mapped IPv4 falls into any rejected range, and internal hostnames. DNS resolution results SHALL be checked against all of these ranges.

#### Scenario: Normal URL passes SSRF validation
- **WHEN** a URL resolves to a public IP address
- **THEN** the HTTP client connects to the same IP that was validated, not a separately resolved address

#### Scenario: DNS rebinding attack attempt
- **WHEN** a URL's first DNS resolution returns a public IP but subsequent resolutions would return a private IP
- **THEN** the HTTP client still connects to the original validated public IP, preventing access to internal services

#### Scenario: Redirect to private IP
- **WHEN** a validated URL returns a 3xx redirect to a URL that resolves to a private or loopback IP
- **THEN** the redirect is not followed and the request is rejected

#### Scenario: HTTPS request with IP pinning
- **WHEN** an HTTPS URL is validated and the connection is pinned to a specific IP
- **THEN** the TLS handshake uses the original hostname for SNI and certificate validation, not the pinned IP

### Requirement: Secure markdown rendering on all pages
All frontend pages rendering user-generated, AI-generated, or crawler-sourced markdown content via `v-html` SHALL configure their MarkdownIt instances with `html: false` to prevent injection of raw HTML. No MarkdownIt instance in the application SHALL enable the `html` option.

#### Scenario: AI-generated content with HTML tags
- **WHEN** AI-generated content contains `<script>alert("xss")</script>` or `<img onerror="steal()">` HTML tags
- **THEN** the markdown renderer escapes these tags as text rather than interpreting them as HTML

#### Scenario: Markdown rendering on insights, notes, and review pages
- **WHEN** markdown content is rendered on the insights, notes, or review pages
- **THEN** the MarkdownIt instance uses `html: false`, matching the reader page's configuration

### Requirement: Rate limiter real client IP partitioning
The rate limiting middleware SHALL partition requests by the real client IP address when the application runs behind a reverse proxy. The application SHALL configure forwarded headers middleware to trust `X-Forwarded-For` from the known proxy network (Docker bridge subnet, `ForwardLimit = 1`) and reject forwarded headers from untrusted sources. The middleware SHALL correctly detect HTTPS via `X-Forwarded-Proto` from the trusted proxy.

#### Scenario: Multiple clients behind nginx proxy
- **WHEN** two different clients make requests through the nginx reverse proxy
- **THEN** each client's requests are rate-limited independently based on their real IP from `X-Forwarded-For`

#### Scenario: Request without X-Forwarded-For
- **WHEN** a request arrives without an `X-Forwarded-For` header
- **THEN** the rate limiter falls back to the connection's remote IP address

#### Scenario: Client-supplied X-Forwarded-For spoofing attempt
- **WHEN** a client directly sends a request with a spoofed `X-Forwarded-For` header not from the trusted proxy network
- **THEN** the middleware ignores the untrusted forwarded header and uses the connection's remote IP address

### Requirement: DocumentBook ownership tracking
The `DocumentBook` entity SHALL include a `CreatedByUserId` field (nullable Guid FK → Users) recording the user who created or imported the book. All book creation endpoints (PDF upload, URL crawl, markdown import, remote PDF import) SHALL set `CreatedByUserId` to the authenticated user's ID. Existing books created before this migration will have `CreatedByUserId = null`.

#### Scenario: User imports a book via PDF upload
- **WHEN** an authenticated user uploads a PDF via `POST /api/v1/library/upload-pdf`
- **THEN** the created `DocumentBook` has `CreatedByUserId` set to the authenticated user's ID

#### Scenario: Querying books created before the migration
- **WHEN** the system queries a book created before the `CreatedByUserId` field was added
- **THEN** `CreatedByUserId` is null and the book is treated as having no recorded owner
