#!/bin/bash
# GCP APIs inschakelen + service account + IAM rollen

enable_apis() {
  echo ">>> APIs inschakelen..."
  gcloud services enable \
    compute.googleapis.com \
    sqladmin.googleapis.com \
    secretmanager.googleapis.com \
    servicenetworking.googleapis.com \
    artifactregistry.googleapis.com \
    storage.googleapis.com \
    --project="${PROJECT_ID}"
}

create_service_account() {
  echo ">>> Service account aanmaken: ${CICD_SA}..."
  gcloud iam service-accounts create synapse-cicd \
    --display-name="Synapse CI/CD Service Account" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"
}

assign_iam_roles() {
  echo ">>> IAM rollen toekennen aan ${CICD_SA}..."
  local roles=(
    "roles/compute.instanceAdmin.v1"
    "roles/storage.admin"
    "roles/secretmanager.secretAccessor"
    "roles/artifactregistry.writer"
    "roles/iam.serviceAccountUser"
  )
  for role in "${roles[@]}"; do
    gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
      --member="serviceAccount:${CICD_SA}" \
      --role="${role}" \
      --quiet
  done
}

destroy_service_account() {
  echo ">>> Service account verwijderen: ${CICD_SA}..."
  gcloud iam service-accounts delete "${CICD_SA}" \
    --project="${PROJECT_ID}" --quiet 2>/dev/null || true
}
