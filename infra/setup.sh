#!/bin/bash
set -euo pipefail

# ── Configuratie ────────────────────────────────────────────────
PROJECT_ID="${PROJECT_ID:-synapse-ip-j2}"
REGION="${REGION:-europe-west1}"
ZONE="${ZONE:-europe-west1-b}"
GCS_BUCKET="${GCS_BUCKET:-synapse-ip-j2-assets-prod}"
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
source "${SCRIPT_DIR}/lib/09_armor.sh"

# ── Setup ───────────────────────────────────────────────────────
echo "======================================================"
echo " Synapse GCP Setup — project: ${PROJECT_ID} (Parallel)"
echo "======================================================"

# 1. APIs moeten eerst actief zijn (sequentieel)
enable_apis

# 2. Start parallelle processen
( create_service_account && assign_iam_roles && create_prod_service_account && assign_prod_iam_roles ) &
PID_IAM=$!

( create_secrets ) &
PID_SECRETS=$!

( create_storage_bucket ) &
PID_BUCKET=$!

( create_health_checks ) &
PID_HC=$!

( create_instance_templates && create_migs ) &
PID_MIGS=$!

# Cloud SQL duurt het langst. Start dit proces en laat de rest niet onnodig wachten.
( create_vpc_peering && create_firewall_rules && create_cloud_sql && enable_pgvector ) &
PID_SQL=$!

# 3. Wacht op MIGs en Health Checks (nodig voor Load Balancer)
wait $PID_MIGS $PID_HC

# 4. Configureer Load Balancer en Backend componenten
create_backend_services
create_cloud_armor
create_url_map
create_ssl_and_proxy
reserve_static_ip
create_forwarding_rules

# 5. Overige configuraties
setup_logging_iam
create_uptime_checks
create_alert_policy

# 6. Wacht tot ook Cloud SQL en de rest helemaal klaar is
wait $PID_SQL $PID_IAM $PID_SECRETS $PID_BUCKET

echo ""
echo "======================================================"
echo " Setup klaar!"
echo " LB IP: $(gcloud compute addresses describe synapse-lb-ip --global --format='value(address)' --project="${PROJECT_ID}")"
echo " Volgende stap: DNS A-records instellen:"
echo "   synapse-ip.xyz      → $(gcloud compute addresses describe synapse-lb-ip --global --format='value(address)' --project="${PROJECT_ID}")"
echo "   *.synapse-ip.xyz    → zelfde IP (wildcard voor project-subdomains)"
echo "======================================================"
