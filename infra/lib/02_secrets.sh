#!/bin/bash
# Secret Manager — secrets aanmaken en waardes instellen
# Alle secret-namen krijgen een "${ENV}-" prefix zodat omgevingen gescheiden blijven.

_create_secret() {
  local name="$1"
  local value="$2"
  echo "  - secret: ${name}"
  gcloud secrets create "${name}" \
    --replication-policy="automatic" \
    --project="${PROJECT_ID}" 2>/dev/null || true
  printf '%s' "${value}" | gcloud secrets versions add "${name}" \
    --data-file=- \
    --project="${PROJECT_ID}"
}

create_secrets() {
  echo ">>> Secrets aanmaken in Secret Manager (omgeving: ${ENV})..."
  echo "  (BELANGRIJK: vul echte waarden in via update_secret na aanmaken infra)"

  _create_secret "${ENV}-db-password"  "${DB_PASSWORD:-CHANGEME}"
  _create_secret "${ENV}-api-key"      "${API_KEY:-CHANGEME}"
  _create_secret "${ENV}-db-host"      "${DB_HOST:-PENDING}"
  _create_secret "${ENV}-api-base-url" "${API_BASE_URL:-PENDING}"
  _create_secret "${ENV}-gcs-bucket"   "${GCS_BUCKET:-PENDING}"
}

update_secret() {
  local name="$1"
  local value="$2"
  echo ">>> Secret updaten: ${ENV}-${name}..."
  printf '%s' "${value}" | gcloud secrets versions add "${ENV}-${name}" \
    --data-file=- \
    --project="${PROJECT_ID}"
}

destroy_secrets() {
  echo ">>> Secrets verwijderen (omgeving: ${ENV})..."
  for secret in db-password api-key db-host api-base-url gcs-bucket; do
    gcloud secrets delete "${ENV}-${secret}" \
      --project="${PROJECT_ID}" --quiet 2>/dev/null || true
  done
}
