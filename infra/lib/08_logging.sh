#!/bin/bash
# Cloud Logging & Monitoring — IAM rollen + uptime checks

setup_logging_iam() {
  echo ">>> Logging/Monitoring IAM rollen toekennen aan compute SA..."
  local compute_sa="${PROJECT_NUMBER}-compute@developer.gserviceaccount.com"

  gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
    --member="serviceAccount:${compute_sa}" \
    --role="roles/logging.logWriter" \
    --quiet

  gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
    --member="serviceAccount:${compute_sa}" \
    --role="roles/monitoring.metricWriter" \
    --quiet

  echo "  Docker stdout/stderr op COS VMs wordt automatisch doorgestuurd naar Cloud Logging."
}

create_uptime_checks() {
  echo ">>> Uptime checks aanmaken..."

  gcloud monitoring uptime create synapse-api-uptime \
    --display-name="Synapse API Health" \
    --resource-type=uptime-url \
    --resource-labels="host=api.synapse-ip.xyz,project_id=${PROJECT_ID}" \
    --path="/health" \
    --period=60 \
    --timeout=10 \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-api-uptime al aanwezig)"

  gcloud monitoring uptime create synapse-ui-uptime \
    --display-name="Synapse UI Health" \
    --resource-type=uptime-url \
    --resource-labels="host=synapse-ip.xyz,project_id=${PROJECT_ID}" \
    --path="/health" \
    --period=60 \
    --timeout=10 \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-ui-uptime al aanwezig)"
}

destroy_uptime_checks() {
  echo ">>> Uptime checks verwijderen..."
  for check in synapse-api-uptime synapse-ui-uptime; do
    gcloud monitoring uptime delete "${check}" \
      --project="${PROJECT_ID}" --quiet 2>/dev/null || true
  done
}
