#!/bin/bash
# Cloud SQL PostgreSQL instantie, database en gebruiker

create_cloud_sql() {
  echo ">>> Cloud SQL instantie aanmaken: mvp-postgres..."
  gcloud sql instances create mvp-postgres \
    --database-version=POSTGRES_15 \
    --tier=db-f1-micro \
    --region="${REGION}" \
    --no-assign-ip \
    --network=default \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  echo "  - database aanmaken: mvp-db"
  gcloud sql databases create mvp-db \
    --instance=mvp-postgres \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  echo "  - gebruiker aanmaken: mvp-user"
  gcloud sql users create mvp-user \
    --instance=mvp-postgres \
    --password="${DB_PASSWORD:-CHANGEME}" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  local db_host
  db_host=$(gcloud sql instances describe mvp-postgres \
    --project="${PROJECT_ID}" \
    --format="value(ipAddresses[0].ipAddress)")
  echo "  Cloud SQL privaat IP: ${db_host}"
  echo "  TIP: update_secret db-host ${db_host}"
}

destroy_cloud_sql() {
  echo ">>> Cloud SQL instantie verwijderen: mvp-postgres..."
  gcloud sql instances delete mvp-postgres \
    --project="${PROJECT_ID}" --quiet 2>/dev/null || true
}
