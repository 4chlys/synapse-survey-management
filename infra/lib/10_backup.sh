#!/bin/bash
# Database backup en restore via Cloud SQL export/import naar GCS

BACKUP_BUCKET="${PROJECT_ID}-db-backups"
SQL_INSTANCE="mvp-postgres-${ENV}"
DB_NAME="mvp-db"

create_backup_bucket() {
  echo ">>> Backup bucket aanmaken: gs://${BACKUP_BUCKET}..."
  gsutil mb -l "${REGION}" "gs://${BACKUP_BUCKET}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"
  gsutil uniformbucketlevelaccess set on "gs://${BACKUP_BUCKET}"
  echo "  Backup bucket klaar (privaat, geen publieke toegang)."
}

backup_database() {
  local timestamp
  timestamp=$(date +%Y%m%d_%H%M%S)
  local backup_file="gs://${BACKUP_BUCKET}/backup-${timestamp}.sql.gz"

  echo ">>> Database backup aanmaken..."
  echo "  Instantie : ${SQL_INSTANCE}"
  echo "  Database  : ${DB_NAME}"
  echo "  Bestand   : ${backup_file}"

  gcloud sql export sql "${SQL_INSTANCE}" "${backup_file}" \
    --database="${DB_NAME}" \
    --project="${PROJECT_ID}"

  echo "  Backup voltooid: ${backup_file}"
}

restore_database() {
  echo ">>> Beschikbare backups:"
  gsutil ls "gs://${BACKUP_BUCKET}/" 2>/dev/null | sort -r | nl -w2 -s') '

  echo ""
  echo -n "Geef de volledige gs:// URL van de backup die je wil herstellen: "
  read -r backup_file

  [[ "${backup_file}" == gs://* ]] || { echo "Ongeldige URL."; return 1; }

  echo ""
  echo "  WAARSCHUWING: Dit overschrijft de huidige database (${DB_NAME})!"
  echo -n "  Typ 'ja' om te bevestigen: "
  read -r confirm
  [[ "${confirm}" == "ja" ]] || { echo "Afgebroken."; return 1; }

  echo ">>> Database herstellen vanuit ${backup_file}..."
  gcloud sql import sql "${SQL_INSTANCE}" "${backup_file}" \
    --database="${DB_NAME}" \
    --project="${PROJECT_ID}" \
    --quiet

  echo "  Restore voltooid."
}

destroy_backup_bucket() {
  echo ">>> Backup bucket verwijderen: gs://${BACKUP_BUCKET}..."
  gsutil -m rm -r "gs://${BACKUP_BUCKET}/**" 2>/dev/null || true
  gsutil rb "gs://${BACKUP_BUCKET}" 2>/dev/null || true
}
