#!/bin/bash

create_cloud_sql() {
  echo ">>> Cloud SQL instantie aanmaken: mvp-postgres-${ENV}..."
  gcloud sql instances create "mvp-postgres-${ENV}" \
    --database-version=POSTGRES_15 \
    --tier=db-f1-micro \
    --region="${REGION}" \
    --no-assign-ip \
    --network="projects/${PROJECT_ID}/global/networks/default" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  echo "  - database aanmaken: mvp-db"
  gcloud sql databases create mvp-db \
    --instance="mvp-postgres-${ENV}" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  echo "  - gebruiker aanmaken: mvp-user"
  gcloud sql users create mvp-user \
    --instance="mvp-postgres-${ENV}" \
    --password="${DB_PASSWORD:-CHANGEME}" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  local db_host
  db_host=$(gcloud sql instances describe "mvp-postgres-${ENV}" \
    --project="${PROJECT_ID}" \
    --format="value(ipAddresses[0].ipAddress)")
  echo "  Cloud SQL privaat IP: ${db_host}"
  echo "  TIP: update_secret db-host ${db_host}"
}

enable_pgvector() {
  echo ">>> pgvector extensie activeren in mvp-db (instantie: mvp-postgres-${ENV})..."
  PGPASSWORD="${DB_PASSWORD:-CHANGEME}" gcloud sql connect "mvp-postgres-${ENV}" \
    --user=mvp-user \
    --database=mvp-db \
    --project="${PROJECT_ID}" \
    --quiet <<< "CREATE EXTENSION IF NOT EXISTS vector;" \
    2>/dev/null \
    && echo "  pgvector actief." \
    || echo "  ⚠️  pgvector activeren mislukt — controleer of de instantie bereikbaar is."
}

destroy_cloud_sql() {
  echo ">>> Cloud SQL instantie verwijderen: mvp-postgres-${ENV}..."
  gcloud sql instances delete "mvp-postgres-${ENV}" \
    --project="${PROJECT_ID}" --quiet 2>/dev/null || true
}
