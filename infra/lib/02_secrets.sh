#!/bin/bash
# Secret Manager — secrets aanmaken en waardes instellen

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
  echo ">>> Secrets aanmaken in Secret Manager..."
  echo "  (BELANGRIJK: vul echte waarden in via update_secret na aanmaken infra)"

  # Placeholders — overschrijf met update_secret na uitrollen
  _create_secret "db-password"  "${DB_PASSWORD:-CHANGEME}"
  _create_secret "api-key"      "${API_KEY:-CHANGEME}"
  _create_secret "db-host"      "${DB_HOST:-PENDING}"
  _create_secret "api-base-url" "${API_BASE_URL:-PENDING}"
  _create_secret "gcs-bucket"   "${GCS_BUCKET:-PENDING}"
}

update_secret() {
  local name="$1"
  local value="$2"
  echo ">>> Secret updaten: ${name}..."
  printf '%s' "${value}" | gcloud secrets versions add "${name}" \
    --data-file=- \
    --project="${PROJECT_ID}"
}

destroy_secrets() {
  echo ">>> Secrets verwijderen..."
  for secret in db-password api-key db-host api-base-url gcs-bucket; do
    gcloud secrets delete "${secret}" \
      --project="${PROJECT_ID}" --quiet 2>/dev/null || true
  done
}
