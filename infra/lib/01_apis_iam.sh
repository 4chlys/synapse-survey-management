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

create_prod_service_account() {
  echo ">>> Production service account aanmaken: ${CICD_SA_PROD}..."
  gcloud iam service-accounts create synapse-cicd-prod \
    --display-name="Synapse CI/CD Production" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"
}

assign_prod_iam_roles() {
  echo ">>> IAM rollen toekennen aan ${CICD_SA_PROD}..."
  local roles=(
    "roles/compute.instanceAdmin.v1"
    "roles/storage.admin"
    "roles/secretmanager.secretAccessor"
    "roles/artifactregistry.writer"
  )
  for role in "${roles[@]}"; do
    gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
      --member="serviceAccount:${CICD_SA_PROD}" \
      --role="${role}" \
      --quiet
  done
}

destroy_prod_service_account() {
  echo ">>> Production service account verwijderen: ${CICD_SA_PROD}..."
  gcloud iam service-accounts delete "${CICD_SA_PROD}" \
    --project="${PROJECT_ID}" --quiet 2>/dev/null || true
}
