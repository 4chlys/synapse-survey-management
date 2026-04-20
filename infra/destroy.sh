#!/bin/bash
set -euo pipefail

# ── Configuratie ────────────────────────────────────────────────
PROJECT_ID="${PROJECT_ID:-synapse-ip-j2}"
REGION="${REGION:-europe-west1}"
ZONE="${ZONE:-europe-west1-b}"
GCS_BUCKET="${GCS_BUCKET:-synapse-assets-prod}"
CICD_SA="synapse-cicd@${PROJECT_ID}.iam.gserviceaccount.com"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ── Lib laden ───────────────────────────────────────────────────
source "${SCRIPT_DIR}/lib/01_apis_iam.sh"
source "${SCRIPT_DIR}/lib/02_secrets.sh"
source "${SCRIPT_DIR}/lib/03_vpc.sh"
source "${SCRIPT_DIR}/lib/04_mig.sh"
source "${SCRIPT_DIR}/lib/05_storage.sh"
source "${SCRIPT_DIR}/lib/06_loadbalancer.sh"

# ── Bevestiging ─────────────────────────────────────────────────
echo "======================================================"
echo " WAARSCHUWING: Dit verwijdert ALLE Synapse infra!"
echo " Project: ${PROJECT_ID}"
echo "======================================================"
read -r -p "Typ 'ja' om te bevestigen: " confirm
[ "${confirm}" = "ja" ] || { echo "Afgebroken."; exit 1; }

# ── Teardown (omgekeerde volgorde van setup) ────────────────────
echo ""
destroy_loadbalancer
destroy_migs
destroy_instance_templates
destroy_storage_bucket
destroy_firewall_rules
destroy_vpc_peering
destroy_secrets
# destroy_service_account  # uitgecommentarieerd: SA heeft JSON key in GitLab CI/CD

echo ""
echo "======================================================"
echo " Teardown klaar."
echo "======================================================"
