#!/bin/bash
set -euo pipefail

# ── Configuratie ────────────────────────────────────────────────
PROJECT_ID="${PROJECT_ID:-synapse-ip-j2}"
REGION="${REGION:-europe-west1}"
ZONE="${ZONE:-europe-west1-b}"
GCS_BUCKET="${GCS_BUCKET:-synapse-assets-prod}"
PROJECT_NUMBER="${PROJECT_NUMBER:-667656673282}"
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

# ── Setup ───────────────────────────────────────────────────────
echo "======================================================"
echo " Synapse GCP Setup — project: ${PROJECT_ID}"
echo "======================================================"

enable_apis
create_service_account
assign_iam_roles
create_prod_service_account
assign_prod_iam_roles

create_secrets
create_cloud_sql

create_vpc_peering
create_firewall_rules

create_instance_templates
create_migs

create_storage_bucket

create_health_checks
create_backend_services
create_url_map
create_ssl_and_proxy
reserve_static_ip
create_forwarding_rules

setup_logging_iam
create_uptime_checks

echo ""
echo "======================================================"
echo " Setup klaar!"
echo " LB IP: $(gcloud compute addresses describe synapse-lb-ip --global --format='value(address)' --project="${PROJECT_ID}")"
echo " Volgende stap: DNS A-records instellen op 35.190.41.73"
echo "======================================================"
