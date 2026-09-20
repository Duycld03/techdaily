# Tasks

## 1. Infrastructure — Nginx Security Headers

- [x] 1.1 Remove `add_header X-Frame-Options "SAMEORIGIN" always;` from the HTTPS server block in `nginx/nginx.conf` (line 57)
- [x] 1.2 Remove `add_header X-XSS-Protection "1; mode=block" always;` from the HTTPS server block in `nginx/nginx.conf` (line 59)
- [x] 1.3 Add `Content-Security-Policy` header with directives: `frame-ancestors 'self'; frame-src 'self' https://accounts.google.com; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://accounts.google.com;` to the HTTPS server block security headers section

## 2. Verification

- [x] 2.1 Validate Nginx config syntax with `docker compose exec nginx nginx -t` (or local `nginx -t` if available)
- [ ] 2.2 Restart Nginx (`docker compose restart nginx`) and verify the Google Sign-In button renders correctly on `techdaily.duckdns.org/login` with visible Google icon and "Tiếp tục sử dụng dịch vụ bằng Google" text
- [ ] 2.3 Verify clickjacking protection is active by checking response headers include `Content-Security-Policy` with `frame-ancestors 'self'` using browser DevTools Network tab
- [ ] 2.4 Verify Google OAuth login flow completes end-to-end (click button → Google consent → redirect back → authenticated)
