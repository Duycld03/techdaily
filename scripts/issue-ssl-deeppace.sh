#!/usr/bin/env bash
# ==============================================================================
# DeepPace — Automated Let's Encrypt SSL Issuance Script
# ==============================================================================
# Issues a multi-domain SAN SSL certificate covering:
#   - deeppace.duckdns.org (Primary CN)
#   - techdaily.duckdns.org (Secondary SAN)
# via Certbot webroot mode without downtime.
# ==============================================================================

set -euo pipefail

GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log_info() { echo -e "${BLUE}ℹ️  [INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}✅ [SUCCESS]${NC} $1"; }
log_warn() { echo -e "${YELLOW}⚠️  [WARN]${NC} $1"; }
log_error() { echo -e "${RED}❌ [ERROR]${NC} $1"; }

PRIMARY_DOMAIN="deeppace.duckdns.org"
SECONDARY_DOMAIN="techdaily.duckdns.org"
WEBROOT_PATH="/var/www/certbot"
EMAIL="${CERTBOT_EMAIL:-admin@deeppace.duckdns.org}"
DRY_RUN="${DRY_RUN:-false}"

CLEANUP_SELF=false
if [[ "${1:-}" == "--cleanup-self" ]] || [[ "${AUTO_CLEANUP_SCRIPT:-false}" == "true" ]]; then
  CLEANUP_SELF=true
fi

cleanup() {
  local exit_code=$?
  if [ "${exit_code}" -eq 0 ] && [ "${CLEANUP_SELF}" = "true" ]; then
    log_info "Self-cleanup enabled: removing one-time issuance script '${BASH_SOURCE[0]}'..."
    rm -f -- "${BASH_SOURCE[0]}"
    log_success "One-time SSL script removed."
  fi
}
trap cleanup EXIT SIGINT SIGTERM

echo "=========================================================="
echo "🔒 DeepPace Automated Let's Encrypt SSL Certificate Setup"
echo "=========================================================="

# 1. DNS Pre-Flight Resolution Checks
log_info "1. Executing DNS pre-flight checks..."

check_dns() {
  local domain="$1"
  log_info "Resolving ${domain}..."
  if getent ahosts "${domain}" >/dev/null 2>&1; then
    local ip
    ip=$(getent ahosts "${domain}" | awk '{print $1}' | head -n 1)
    log_success "Domain ${domain} resolves to ${ip}"
  else
    log_warn "Domain ${domain} could not be resolved via getent. Checking with ping/curl..."
    if ping -c 1 -W 2 "${domain}" >/dev/null 2>&1; then
      log_success "Domain ${domain} is reachable via ICMP."
    else
      log_warn "Pre-flight warning: ${domain} did not resolve locally. Continuing certbot issuance..."
    fi
  fi
}

check_dns "${PRIMARY_DOMAIN}"
check_dns "${SECONDARY_DOMAIN}"

# 2. Check Certbot Installation
log_info "2. Verifying Certbot installation..."
if ! command -v certbot >/dev/null 2>&1; then
  log_error "Certbot is not installed on this host. Install via: apt update && apt install -y certbot"
  exit 1
fi

# 3. Ensure ACME webroot directory exists
log_info "3. Ensuring ACME challenge directory exists at ${WEBROOT_PATH}..."
if [ ! -d "${WEBROOT_PATH}" ]; then
  mkdir -p "${WEBROOT_PATH}"
  chmod 755 "${WEBROOT_PATH}"
fi

# 4. Execute Certbot Multi-Domain SAN Issuance
log_info "4. Issuing multi-domain certificate for ${PRIMARY_DOMAIN} and ${SECONDARY_DOMAIN}..."

CERTBOT_ARGS=(
  certonly
  --webroot
  -w "${WEBROOT_PATH}"
  -d "${PRIMARY_DOMAIN}"
  -d "${SECONDARY_DOMAIN}"
  --cert-name "${PRIMARY_DOMAIN}"
  --non-interactive
  --agree-tos
  -m "${EMAIL}"
)

if [ "${DRY_RUN}" = "true" ]; then
  log_info "Running in DRY-RUN simulation mode..."
  CERTBOT_ARGS+=(--dry-run)
fi

certbot "${CERTBOT_ARGS[@]}"

# 5. Verify Certificate Existence
CERT_PATH="/etc/letsencrypt/live/${PRIMARY_DOMAIN}/fullchain.pem"
KEY_PATH="/etc/letsencrypt/live/${PRIMARY_DOMAIN}/privkey.pem"

if [ "${DRY_RUN}" != "true" ]; then
  if [ -f "${CERT_PATH}" ] && [ -f "${KEY_PATH}" ]; then
    log_success "Certificate files verified:"
    log_info "  - Fullchain: ${CERT_PATH}"
    log_info "  - Private Key: ${KEY_PATH}"
  else
    log_error "Certificate files not found at expected paths!"
    exit 1
  fi
fi

# 6. Reload / Restart Nginx Container
log_info "5. Signaling Nginx container to reload SSL certificate..."
if docker ps --format '{{.Names}}' | grep -q "deeppace_nginx_prod"; then
  log_info "Reloading Nginx in 'deeppace_nginx_prod'..."
  docker exec deeppace_nginx_prod nginx -s reload || docker compose -f docker-compose.prod.yml restart nginx
  log_success "Nginx reloaded successfully with new certificate."
else
  log_warn "Container 'deeppace_nginx_prod' is not currently running. Restart via docker compose when ready:"
  echo "  docker compose -f docker-compose.prod.yml restart nginx"
fi

echo ""
log_success "Multi-domain SSL setup complete for https://${PRIMARY_DOMAIN} and https://${SECONDARY_DOMAIN}!"

if [ "${CLEANUP_SELF}" != "true" ]; then
  echo ""
  log_info "💡 Note: Once SSL is active, you can clean up this one-time script with:"
  echo "   rm -f scripts/issue-ssl-deeppace.sh"
fi
