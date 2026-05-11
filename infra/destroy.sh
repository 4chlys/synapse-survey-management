#!/bin/bash
set -euo pipefail

# ── Configuratie ────────────────────────────────────────────────
PROJECT_ID="${PROJECT_ID:-synapse-ip-j2}"
REGION="${REGION:-europe-west1}"
ZONE="${ZONE:-europe-west1-b}"
GCS_BUCKET="${GCS_BUCKET:-synapse-ip-j2-assets-prod}"
CICD_SA="synapse-cicd@${PROJECT_ID}.iam.gserviceaccount.com"
CICD_SA_PROD="synapse-cicd-prod@${PROJECT_ID}.iam.gserviceaccount.com"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ── Lib laden ───────────────────────────────────────────────────
source "${SCRIPT_DIR}/lib/01_apis_iam.sh"
source "${SCRIPT_DIR}/lib/02_secrets.sh"
source "${SCRIPT_DIR}/lib/03_vpc.sh"
source "${SCRIPT_DIR}/lib/04_mig.sh"
source "${SCRIPT_DIR}/lib/05_storage.sh"
source "${SCRIPT_DIR}/lib/06_loadbalancer.sh"
source "${SCRIPT_DIR}/lib/07_cloudsql.sh"
source "${SCRIPT_DIR}/lib/08_logging.sh"
source "${SCRIPT_DIR}/lib/09_armor.sh"
source "${SCRIPT_DIR}/lib/10_backup.sh"

# ── Bevestiging ─────────────────────────────────────────────────
echo "======================================================"
echo " WAARSCHUWING: Dit verwijdert ALLE Synapse infra! (parallel)"
echo " Project: ${PROJECT_ID}"
echo "======================================================"
read -r -p "Typ 'ja' om te bevestigen: " confirm
[ "${confirm}" = "ja" ] || { echo "Afgebroken."; exit 1; }

# ── Teardown (parallel waar mogelijk) ──────────────────────────
echo ""

# 1. Cloud Armor en Load Balancer eerst (backends hangen eraan)
destroy_cloud_armor
destroy_uptime_checks
destroy_loadbalancer

# 2. MIGs, SQL, Storage, VPC en Secrets zijn nu onafhankelijk — parallel verwijderen
( destroy_migs && destroy_instance_templates ) &
PID_MIGS=$!

( destroy_cloud_sql ) &
PID_SQL=$!

( destroy_storage_bucket && destroy_backup_bucket ) &
PID_BUCKET=$!

( destroy_firewall_rules && destroy_vpc_peering ) &
PID_VPC=$!

( destroy_secrets ) &
PID_SECRETS=$!

# 3. Wacht tot alles verwijderd is
wait $PID_MIGS $PID_SQL $PID_BUCKET $PID_VPC $PID_SECRETS

# destroy_service_account       # uitgecommentarieerd: SA heeft JSON key in GitLab CI/CD
# destroy_prod_service_account  # uitgecommentarieerd: SA heeft JSON key in GitLab CI/CD

echo ""
echo "======================================================"
echo " Teardown klaar."
echo "======================================================"
