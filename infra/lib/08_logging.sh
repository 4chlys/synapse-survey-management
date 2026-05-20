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
  echo ">>> Uptime checks aanmaken (omgeving: ${ENV})..."

  gcloud monitoring uptime create "synapse-${ENV}-api-uptime" \
    --display-name="Synapse ${ENV} API Health" \
    --resource-type=uptime-url \
    --resource-labels="host=api.${DOMAIN},project_id=${PROJECT_ID}" \
    --path="/health" \
    --period=5 \
    --timeout=10 \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-api-uptime al aanwezig)"

  gcloud monitoring uptime create "synapse-${ENV}-ui-uptime" \
    --display-name="Synapse ${ENV} UI Health" \
    --resource-type=uptime-url \
    --resource-labels="host=${DOMAIN},project_id=${PROJECT_ID}" \
    --path="/health" \
    --period=5 \
    --timeout=10 \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-ui-uptime al aanwezig)"
}

destroy_uptime_checks() {
  echo ">>> Uptime checks verwijderen (omgeving: ${ENV})..."
  for check in "synapse-${ENV}-api-uptime" "synapse-${ENV}-ui-uptime"; do
    gcloud monitoring uptime delete "${check}" \
      --project="${PROJECT_ID}" --quiet 2>/dev/null || true
  done
}
