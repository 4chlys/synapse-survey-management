#!/bin/bash
set -euo pipefail

# ══════════════════════════════════════════════════════════════
#  Synapse – Bootstrap Script
#  Zet een volledige GCP-omgeving op van nul.
#
#  Gebruik:
#    ./deploy/bootstrap.sh <omgeving>
#
#  Omgevingen:
#    test   – testomgeving (aparte Cloud SQL, MIGs, LB, secrets)
#    prod   – productieomgeving
#
#  Configuratie per omgeving: deploy/config.<omgeving>.env
#  Maak aan op basis van deploy/config.example.env
# ══════════════════════════════════════════════════════════════

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_DIR="${SCRIPT_DIR}/../infra"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'
CYAN='\033[0;36m'; BOLD='\033[1m'; RESET='\033[0m'

info()    { echo -e "${CYAN}>>> $*${RESET}"; }
success() { echo -e "${GREEN}✅ $*${RESET}"; }
warn()    { echo -e "${YELLOW}⚠️  $*${RESET}"; }
error()   { echo -e "${RED}❌ $*${RESET}"; exit 1; }
ask()     { echo -e "${BOLD}$*${RESET}"; }

# ── Stap 1: Omgeving als parameter ───────────────────────────
ENV="${1:-}"
if [[ -z "${ENV}" ]]; then
  error "Geef een omgeving op als argument: ./bootstrap.sh <test|prod>"
fi
[[ "${ENV}" =~ ^[a-z]+$ ]] || error "Omgeving mag alleen kleine letters bevatten (bijv. test, prod)."
export ENV

CONFIG_FILE="${SCRIPT_DIR}/config.${ENV}.env"

# ── Banner ─────────────────────────────────────────────────────
echo ""
echo -e "${BOLD}╔══════════════════════════════════════════════╗${RESET}"
echo -e "${BOLD}║        Synapse Infrastructure Setup          ║${RESET}"
echo -e "${BOLD}║   Volledig geautomatiseerd op Google Cloud   ║${RESET}"
echo -e "${BOLD}║   Omgeving: ${ENV}$(printf '%*s' $((30-${#ENV})) '')║${RESET}"
echo -e "${BOLD}╚══════════════════════════════════════════════╝${RESET}"
echo ""

# ── Stap 2: Prerequisites ─────────────────────────────────────
info "Vereisten controleren..."
command -v gcloud >/dev/null 2>&1 || error "gcloud is niet geïnstalleerd."
command -v gsutil >/dev/null 2>&1 || error "gsutil is niet geïnstalleerd (komt mee met gcloud)."
command -v docker  >/dev/null 2>&1 || warn  "Docker niet gevonden — niet vereist voor infra setup."
success "gcloud en gsutil gevonden."

# ── Stap 3: Config laden of interactief invullen ──────────────
if [[ -f "${CONFIG_FILE}" ]]; then
  info "Configuratie laden uit config.${ENV}.env..."
  # shellcheck source=/dev/null
  source "${CONFIG_FILE}"
  success "config.${ENV}.env geladen."
else
  warn "Geen config.${ENV}.env gevonden — interactieve setup starten."
  echo ""

  ask "GCP Project ID (bijv. mijn-bedrijf-synapse):"
  read -r PROJECT_ID

  ask "GCP Regio [europe-west1]:"
  read -r REGION_INPUT
  REGION="${REGION_INPUT:-europe-west1}"

  ask "GCP Zone [europe-west1-b]:"
  read -r ZONE_INPUT
  ZONE="${ZONE_INPUT:-europe-west1-b}"

  ask "Domein voor deze omgeving (bijv. synapse-ip.xyz voor prod, test.synapse-ip.xyz voor test):"
  read -r DOMAIN

  ask "Database wachtwoord (minimaal 12 tekens):"
  read -rs DB_PASSWORD
  echo ""
  [[ ${#DB_PASSWORD} -lt 12 ]] && error "Wachtwoord moet minimaal 12 tekens zijn."

  ask "API sleutel (druk Enter om automatisch te genereren):"
  read -r API_KEY_INPUT
  if [[ -z "${API_KEY_INPUT}" ]]; then
    API_KEY=$(openssl rand -base64 32)
    success "API sleutel automatisch gegenereerd."
  else
    API_KEY="${API_KEY_INPUT}"
  fi

  GCS_BUCKET="${PROJECT_ID}-assets-${ENV}"
  VERTEX_AI_LOCATION="${REGION}"

  cat > "${CONFIG_FILE}" <<EOF
PROJECT_ID="${PROJECT_ID}"
PROJECT_NUMBER=""
REGION="${REGION}"
ZONE="${ZONE}"
DOMAIN="${DOMAIN}"
DB_PASSWORD="${DB_PASSWORD}"
API_KEY="${API_KEY}"
GCS_BUCKET="${GCS_BUCKET}"
VERTEX_AI_LOCATION="${VERTEX_AI_LOCATION}"
EOF
  success "Configuratie opgeslagen in deploy/config.${ENV}.env"
fi

# GCS_BUCKET standaardwaarde als niet in config
GCS_BUCKET="${GCS_BUCKET:-${PROJECT_ID}-assets-${ENV}}"

# ── Stap 4: Samenvatten en bevestigen ─────────────────────────
echo ""
echo -e "${BOLD}── Configuratieoverzicht (omgeving: ${ENV}) ────${RESET}"
echo "  Project ID      : ${PROJECT_ID}"
echo "  Regio           : ${REGION} / ${ZONE}"
echo "  Domein          : ${DOMAIN}"
echo "  API domein      : api.${DOMAIN}"
echo "  Admin domein    : admin.${DOMAIN}"
echo "  GCS Bucket      : ${GCS_BUCKET}"
echo "  Cloud SQL       : mvp-postgres-${ENV}"
echo "  MIGs            : synapse-${ENV}-{api,ui,admin}-mig"
echo "  Load Balancer   : synapse-${ENV}-lb-ip"
echo "  Secrets prefix  : ${ENV}-"
echo ""
warn "Controleer bovenstaande gegevens zorgvuldig."
ask "Alles correct? Typ 'ja' om te starten:"
read -r confirm
[[ "${confirm}" == "ja" ]] || { echo "Afgebroken."; exit 1; }

# ── Stap 5: GCP authenticatie ─────────────────────────────────
info "GCP authenticatie controleren..."
gcloud auth print-access-token >/dev/null 2>&1 || {
  warn "Nog niet ingelogd bij GCP. Login wordt gestart..."
  gcloud auth login
}
gcloud config set project "${PROJECT_ID}"

PROJECT_NUMBER=$(gcloud projects describe "${PROJECT_ID}" --format="value(projectNumber)")
export PROJECT_NUMBER
success "Ingelogd bij project ${PROJECT_ID} (${PROJECT_NUMBER})"

# ── Stap 6: Lib functies laden ─────────────────────────────────
info "Infra functies laden..."
export PROJECT_ID REGION ZONE GCS_BUCKET PROJECT_NUMBER ENV
export DB_PASSWORD API_KEY DOMAIN
export CICD_SA="synapse-cicd@${PROJECT_ID}.iam.gserviceaccount.com"
export API_BASE_URL="https://api.${DOMAIN}"

source "${INFRA_DIR}/lib/01_apis_iam.sh"
source "${INFRA_DIR}/lib/02_secrets.sh"
source "${INFRA_DIR}/lib/03_vpc.sh"
source "${INFRA_DIR}/lib/04_mig.sh"
source "${INFRA_DIR}/lib/05_storage.sh"
source "${INFRA_DIR}/lib/06_loadbalancer.sh"
source "${INFRA_DIR}/lib/07_cloudsql.sh"
source "${INFRA_DIR}/lib/08_logging.sh"
source "${INFRA_DIR}/lib/09_armor.sh"
source "${INFRA_DIR}/lib/10_backup.sh"

# ── Stap 7: Setup uitvoeren ────────────────────────────────────
echo ""
echo -e "${BOLD}── Setup starten voor omgeving '${ENV}' (Parallel) ──${RESET}"

# 1. APIs moeten eerst actief zijn (eenmalig, project-breed)
enable_apis

# 2. Parallelle processen
( create_service_account && assign_iam_roles ) &
PID_IAM=$!

( create_secrets ) &
PID_SECRETS=$!

( create_storage_bucket && create_backup_bucket ) &
PID_BUCKET=$!

( create_health_checks ) &
PID_HC=$!

( create_instance_templates ) &
PID_TEMPLATES=$!

# VPC peering is project-breed — sla over als al aanwezig
( create_vpc_peering && create_firewall_rules && create_cloud_sql ) &
PID_SQL=$!

# 3. Wacht op alles voor MIGs
wait $PID_SQL $PID_IAM $PID_SECRETS $PID_BUCKET $PID_TEMPLATES $PID_HC

# db-host secret updaten met het echte Cloud SQL privaat IP
DB_HOST=$(gcloud sql instances describe "mvp-postgres-${ENV}" \
  --project="${PROJECT_ID}" \
  --format="value(ipAddresses[0].ipAddress)" 2>/dev/null || echo "PENDING")
if [[ "${DB_HOST}" != "PENDING" ]]; then
  update_secret "db-host" "${DB_HOST}"
fi

enable_pgvector

# 4. MIGs aanmaken
create_migs

# 5. Load Balancer
create_backend_services
create_url_map
create_ssl_and_proxy
reserve_static_ip
create_forwarding_rules
create_cloud_armor

# 6. Monitoring
setup_logging_iam
create_uptime_checks

LB_IP=$(gcloud compute addresses describe "synapse-${ENV}-lb-ip" \
  --global --format="value(address)" --project="${PROJECT_ID}")

# ── Stap 8: Samenvatting ───────────────────────────────────────
echo ""
echo -e "${GREEN}${BOLD}╔══════════════════════════════════════════════╗${RESET}"
echo -e "${GREEN}${BOLD}║  Setup klaar! Omgeving: ${ENV}$(printf '%*s' $((20-${#ENV})) '')🎉  ║${RESET}"
echo -e "${GREEN}${BOLD}╚══════════════════════════════════════════════╝${RESET}"
echo ""
echo -e "  Load Balancer IP : ${BOLD}${LB_IP}${RESET}"
echo -e "  Cloud SQL        : ${BOLD}mvp-postgres-${ENV}${RESET}"
echo ""
echo -e "${YELLOW}${BOLD}── Volgende stappen ────────────────────────────${RESET}"
echo "  1. DNS A-records instellen bij je domeinregistrar:"
echo "     ${DOMAIN}       → ${LB_IP}"
echo "     api.${DOMAIN}   → ${LB_IP}"
echo "     admin.${DOMAIN} → ${LB_IP}"
echo ""
echo "  2. SSL certificaat activering afwachten (tot 24u na DNS)"
echo ""
echo "  3. GitLab CI/CD variabelen instellen:"
echo "     PROJECT_ID, REGION, ZONE, GCR_SERVICE_ACCOUNT_KEY"
echo ""
echo -e "  Je app wordt bereikbaar op: ${BOLD}https://${DOMAIN}${RESET}"
echo ""
