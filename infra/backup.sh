#!/bin/bash
set -euo pipefail

# ══════════════════════════════════════════════════════════════
#  Synapse – Database Backup Script
#  Gebruik: ./infra/backup.sh <test|prod>
# ══════════════════════════════════════════════════════════════

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

ENV="${1:-}"
if [[ -z "${ENV}" ]]; then
  echo "❌ Geef een omgeving op als argument: ./backup.sh <test|prod>"
  exit 1
fi
export ENV

CONFIG_FILE="${SCRIPT_DIR}/../deploy/config.${ENV}.env"

[[ -f "${CONFIG_FILE}" ]] || { echo "Geen config.${ENV}.env gevonden."; exit 1; }
source "${CONFIG_FILE}"

export PROJECT_ID REGION

source "${SCRIPT_DIR}/lib/10_backup.sh"

gcloud config set project "${PROJECT_ID}" --quiet

backup_database
