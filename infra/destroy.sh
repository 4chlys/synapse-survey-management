#!/bin/bash
set -euo pipefail

# ══════════════════════════════════════════════════════════════
#  Synapse – Destroy Script
#  Verwijdert alle GCP resources van één omgeving.
#
#  Gebruik:
#    ./infra/destroy.sh <omgeving>
#
#  Voorbeeld:
#    ./infra/destroy.sh test    # verwijdert test-omgeving
#    ./infra/destroy.sh prod    # verwijdert productie-omgeving
# ══════════════════════════════════════════════════════════════

ENV="${1:-}"
if [[ -z "${ENV}" ]]; then
  echo "❌ Geef een omgeving op als argument: ./destroy.sh <test|prod>"
  exit 1
fi
export ENV

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_FILE="${SCRIPT_DIR}/../deploy/config.${ENV}.env"

if [[ -f "${CONFIG_FILE}" ]]; then
  # shellcheck source=/dev/null
  source "${CONFIG_FILE}"
else
  echo "⚠️  Geen config.${ENV}.env gevonden. Geef PROJECT_ID handmatig:"
  read -r PROJECT_ID
  REGION="${REGION:-europe-west1}"
  ZONE="${ZONE:-europe-west1-b}"
fi

export PROJECT_ID REGION ZONE
GCS_BUCKET="${GCS_BUCKET:-${PROJECT_ID}-assets-${ENV}}"
export GCS_BUCKET

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
# update_secret is used by create_internal_api_lb but not needed during destroy; keep sourced for completeness.

echo "======================================================"
echo " WAARSCHUWING: Dit verwijdert ALLE Synapse '${ENV}' resources!"
echo " Project : ${PROJECT_ID}"
echo " Omgeving: ${ENV}"
echo "======================================================"
read -r -p "Typ 'ja' om te bevestigen: " confirm
[ "${confirm}" = "ja" ] || { echo "Afgebroken."; exit 1; }

echo ""

# LB must be torn down before backends to avoid dangling forwarding rules.
destroy_cloud_armor
destroy_alert_policy
destroy_uptime_checks
destroy_internal_api_lb
destroy_loadbalancer

( destroy_migs && destroy_instance_templates ) &
PID_MIGS=$!

( destroy_cloud_sql ) &
PID_SQL=$!

( destroy_storage_bucket && destroy_backup_bucket ) &
PID_BUCKET=$!

( destroy_secrets ) &
PID_SECRETS=$!

wait $PID_MIGS $PID_SQL $PID_BUCKET $PID_SECRETS

# VPC and firewall rules are project-wide; removing them would break other environments.
# destroy_firewall_rules && destroy_vpc_peering

echo ""
echo "======================================================"
echo " Teardown klaar voor omgeving '${ENV}'."
echo "======================================================"
