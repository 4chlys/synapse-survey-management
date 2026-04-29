#!/bin/bash
set -euo pipefail

# ══════════════════════════════════════════════════════════════
#  Synapse – Service Management Menu
#  Gebruik: ./deploy/menu.sh
# ══════════════════════════════════════════════════════════════

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_FILE="${SCRIPT_DIR}/config.env"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'
CYAN='\033[0;36m'; BOLD='\033[1m'; RESET='\033[0m'

info()    { echo -e "${CYAN}>>> $*${RESET}"; }
success() { echo -e "${GREEN}✅ $*${RESET}"; }
warn()    { echo -e "${YELLOW}⚠️  $*${RESET}"; }
error()   { echo -e "${RED}❌ $*${RESET}"; }

# ── Config laden ───────────────────────────────────────────────
if [[ -f "${CONFIG_FILE}" ]]; then
  source "${CONFIG_FILE}"
else
  warn "Geen config.env gevonden. Voer eerst bootstrap.sh uit."
  echo "PROJECT_ID:"
  read -r PROJECT_ID
  echo "REGION [europe-west1]:"
  read -r REGION_INPUT
  REGION="${REGION_INPUT:-europe-west1}"
fi

gcloud config set project "${PROJECT_ID}" --quiet 2>/dev/null

# ── Helpers ────────────────────────────────────────────────────
mig_status() {
  local mig="$1"
  local size
  size=$(gcloud compute instance-groups managed describe "${mig}" \
    --zone="${REGION}" --project="${PROJECT_ID}" \
    --format="value(targetSize)" 2>/dev/null || echo "?")
  echo "${size}"
}

set_mig_size() {
  local mig="$1" size="$2"
  gcloud compute instance-groups managed resize "${mig}" \
    --size="${size}" --zone="${REGION}" --project="${PROJECT_ID}" --quiet
}

show_status() {
  echo ""
  echo -e "${BOLD}── MIG Status ─────────────────────────────────${RESET}"
  for mig in synapse-api-mig synapse-ui-mig synapse-admin-mig; do
    local n
    n=$(mig_status "${mig}")
    if [[ "${n}" == "0" ]]; then
      echo -e "  ${RED}●${RESET} ${mig}: gestopt (0 instanties)"
    else
      echo -e "  ${GREEN}●${RESET} ${mig}: actief (${n} instanties)"
    fi
  done
  echo ""
}

show_logs() {
  local service="$1"
  info "Laatste 50 logregels van ${service}..."
  gcloud logging read \
    "resource.type=gce_instance AND logName=projects/${PROJECT_ID}/logs/cos_containers AND jsonPayload.container.name=\"/${service}\"" \
    --limit=50 --project="${PROJECT_ID}" \
    --format="value(timestamp,jsonPayload.message)" 2>/dev/null | head -50 || \
  gcloud logging read \
    "resource.type=gce_instance" \
    --limit=50 --project="${PROJECT_ID}" \
    --format="table(timestamp,resource.labels.instance_id,textPayload)" 2>/dev/null | head -50
}

# ── Hoofdmenu ─────────────────────────────────────────────────
while true; do
  echo ""
  echo -e "${BOLD}╔══════════════════════════════════════════════╗${RESET}"
  echo -e "${BOLD}║        Synapse – Beheer Menu                 ║${RESET}"
  echo -e "${BOLD}║  Project: ${PROJECT_ID}${RESET}"
  echo -e "${BOLD}╚══════════════════════════════════════════════╝${RESET}"
  echo ""
  echo "  1) Status tonen (alle MIGs)"
  echo "  2) API starten / stoppen"
  echo "  3) UI starten / stoppen"
  echo "  4) Admin starten / stoppen"
  echo "  5) Alles starten"
  echo "  6) Alles stoppen"
  echo "  7) Logs bekijken (API)"
  echo "  8) Logs bekijken (UI)"
  echo "  9) Load Balancer IP tonen"
  echo " 10) SSL certificaat status"
  echo "  q) Afsluiten"
  echo ""
  echo -n "Keuze: "
  read -r choice

  case "${choice}" in
    1)
      show_status
      ;;
    2)
      n=$(mig_status "synapse-api-mig")
      if [[ "${n}" == "0" ]]; then
        info "API starten (1 instantie)..."
        set_mig_size "synapse-api-mig" 1
        success "API gestart."
      else
        info "API stoppen..."
        set_mig_size "synapse-api-mig" 0
        success "API gestopt."
      fi
      ;;
    3)
      n=$(mig_status "synapse-ui-mig")
      if [[ "${n}" == "0" ]]; then
        info "UI starten (1 instantie)..."
        set_mig_size "synapse-ui-mig" 1
        success "UI gestart."
      else
        info "UI stoppen..."
        set_mig_size "synapse-ui-mig" 0
        success "UI gestopt."
      fi
      ;;
    4)
      n=$(mig_status "synapse-admin-mig")
      if [[ "${n}" == "0" ]]; then
        info "Admin starten (1 instantie)..."
        set_mig_size "synapse-admin-mig" 1
        success "Admin gestart."
      else
        info "Admin stoppen..."
        set_mig_size "synapse-admin-mig" 0
        success "Admin gestopt."
      fi
      ;;
    5)
      info "Alle services starten..."
      set_mig_size "synapse-api-mig"   1
      set_mig_size "synapse-ui-mig"    1
      set_mig_size "synapse-admin-mig" 1
      success "Alle services gestart."
      ;;
    6)
      warn "Dit stopt alle VMs. Weet je het zeker? (ja/nee)"
      read -r confirm
      if [[ "${confirm}" == "ja" ]]; then
        set_mig_size "synapse-api-mig"   0
        set_mig_size "synapse-ui-mig"    0
        set_mig_size "synapse-admin-mig" 0
        success "Alle services gestopt."
      else
        echo "Geannuleerd."
      fi
      ;;
    7)
      show_logs "mvp-api"
      ;;
    8)
      show_logs "mvp-ui"
      ;;
    9)
      LB_IP=$(gcloud compute addresses describe synapse-lb-ip \
        --global --format="value(address)" --project="${PROJECT_ID}" 2>/dev/null || echo "niet gevonden")
      echo ""
      echo -e "  Load Balancer IP: ${BOLD}${LB_IP}${RESET}"
      echo -e "  UI:    https://${DOMAIN:-synapse-ip.xyz}"
      echo -e "  API:   https://api.${DOMAIN:-synapse-ip.xyz}"
      echo -e "  Admin: https://admin.${DOMAIN:-synapse-ip.xyz}"
      echo ""
      ;;
    10)
      info "SSL certificaat status..."
      gcloud compute ssl-certificates list --project="${PROJECT_ID}" \
        --format="table(name,managed.status,managed.domainStatus)" 2>/dev/null
      ;;
    q|Q|quit|exit)
      echo "Tot ziens."
      exit 0
      ;;
    *)
      warn "Ongeldige keuze: ${choice}"
      ;;
  esac
done
