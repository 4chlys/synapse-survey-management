#!/bin/bash
set -euo pipefail

# ══════════════════════════════════════════════════════════════
#  Synapse – Database Backup Script
#  Gebruik: ./infra/backup.sh
# ══════════════════════════════════════════════════════════════

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_FILE="${SCRIPT_DIR}/../deploy/config.env"

[[ -f "${CONFIG_FILE}" ]] || { echo "Geen config.env gevonden."; exit 1; }
source "${CONFIG_FILE}"

export PROJECT_ID REGION

source "${SCRIPT_DIR}/lib/10_backup.sh"

gcloud config set project "${PROJECT_ID}" --quiet

backup_database
