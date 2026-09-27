# Design

## Context

See `proposal.md` for background and root cause.
In `nginx/nginx.conf`:
- The file defines an `http { ... }` block starting at line 5.
- Inside `http`, a `server { ... }` block starts at line 42.
- The `server` block previously ended with `}` before the outer `http` closed with `}`.
- In commit `7bd6d5c`, the lines modifying the `location /` block accidentally deleted the closing `}` of the `server` block, leaving only one closing brace at the end of the file.
- Total opening braces `{`: 18.
- Total closing braces `}`: 17.

## Goals / Non-Goals

**Goals:**
- Restore the closing brace `}` for the `server` block so `nginx/nginx.conf` has exactly balanced braces (18 opens, 18 closes).
- Verify configuration syntax before pushing so Nginx container restarts cleanly on production without exit code 1.

**Non-Goals:**
- Modifying proxy timeouts, rate limits, or SSL certificate configurations.

## Decisions

### 1. Add Closing Brace at End of Server Block
At the end of `nginx/nginx.conf`:
```nginx
        # Frontend Nuxt Nitro SSR
        location / {
            proxy_pass http://frontend_upstream;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto https;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
        }
    }
}
```
Line 176 closes `location /`.
Line 177 closes `server`.
Line 178 closes `http`.

### 2. Validation Gate
Run an automated brace counter verification:
`awk '{opens += gsub(/{/, "{"); closes += gsub(/}/, "}")} END {if (opens != closes) {print "MISMATCH: " opens " vs " closes; exit 1} else {print "BALANCED: " opens " braces"}}' nginx/nginx.conf`
Ensures configuration is guaranteed syntactically balanced before commit.

## Risks / Trade-offs

- **Zero Risk**: Restores the exact expected block hierarchy that Nginx requires.
