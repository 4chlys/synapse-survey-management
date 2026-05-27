#!/bin/bash

create_storage_bucket() {
  echo ">>> Cloud Storage bucket aanmaken: gs://${GCS_BUCKET}..."

  gsutil mb -l "${REGION}" "gs://${GCS_BUCKET}" 2>/dev/null || echo "  (bucket al aanwezig, overgeslagen)"

  gsutil uniformbucketlevelaccess set on "gs://${GCS_BUCKET}"

  gsutil iam ch allUsers:objectViewer "gs://${GCS_BUCKET}"

  local compute_sa="${PROJECT_NUMBER}-compute@developer.gserviceaccount.com"
  gsutil iam ch "serviceAccount:${compute_sa}:objectCreator" "gs://${GCS_BUCKET}"

  echo "  Bucket klaar: https://storage.googleapis.com/${GCS_BUCKET}/"
}

destroy_storage_bucket() {
  echo ">>> Cloud Storage bucket verwijderen: gs://${GCS_BUCKET}..."
  gsutil -m rm -r "gs://${GCS_BUCKET}/**" 2>/dev/null || true
  gsutil rb "gs://${GCS_BUCKET}" 2>/dev/null || true
}
