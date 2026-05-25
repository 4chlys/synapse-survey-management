#!/bin/bash
# MIG instance templates + Managed Instance Groups + autoscalers

_INFRA_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

_create_template() {
  local name="$1"
  local machine_type="$2"
  local startup_script="$3"
  echo "  - template: ${name} (${machine_type})"
  # ENV en PROJECT_ID als instance-metadata meegeven zodat startup scripts ze
  # kunnen opvragen via de Metadata Server, onafhankelijk van de aanroepende shell.
  gcloud compute instance-templates create "${name}" \
    --machine-type="${machine_type}" \
    --image-family=cos-stable \
    --image-project=cos-cloud \
    --tags=allow-health-check \
    --scopes=cloud-platform \
    --metadata="env=${ENV},project-id=${PROJECT_ID},domain=${DOMAIN}" \
    --metadata-from-file="startup-script=${_INFRA_DIR}/${startup_script}" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "    (al aanwezig, overgeslagen)"
}

_create_mig() {
  local name="$1"
  local template="$2"
  local min_replicas="$3"
  local max_replicas="$4"
  echo "  - MIG: ${name} (min ${min_replicas} / max ${max_replicas})"

  gcloud compute instance-groups managed create "${name}" \
    --template="${template}" \
    --size="${min_replicas}" \
    --zone="${ZONE}" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "    (al aanwezig, overgeslagen)"

  gcloud compute instance-groups managed set-autoscaling "${name}" \
    --min-num-replicas="${min_replicas}" \
    --max-num-replicas="${max_replicas}" \
    --target-cpu-utilization=0.70 \
    --zone="${ZONE}" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "    (autoscaling al ingesteld)"

  local port
  port="$([ "${name}" = "synapse-${ENV}-api-mig" ] && echo 8080 || echo 80)"
  gcloud compute instance-groups managed set-named-ports "${name}" \
    --named-ports="http:${port}" \
    --zone="${ZONE}" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "    (named ports al ingesteld)"
}

create_instance_templates() {
  echo ">>> Instance templates aanmaken (omgeving: ${ENV})..."
  _create_template "synapse-${ENV}-api-template"   "e2-small"  "startup-api.sh"
  _create_template "synapse-${ENV}-ui-template"    "e2-small"  "startup-ui.sh"
  _create_template "synapse-${ENV}-admin-template" "e2-micro"  "startup-admin.sh"
}

create_migs() {
  echo ">>> Managed Instance Groups aanmaken (omgeving: ${ENV})..."
  _create_mig "synapse-${ENV}-api-mig"   "synapse-${ENV}-api-template"   1 5
  _create_mig "synapse-${ENV}-ui-mig"    "synapse-${ENV}-ui-template"    1 5
  _create_mig "synapse-${ENV}-admin-mig" "synapse-${ENV}-admin-template" 1 1
}

destroy_migs() {
  echo ">>> MIGs verwijderen (omgeving: ${ENV})..."
  for mig in synapse-${ENV}-api-mig synapse-${ENV}-ui-mig synapse-${ENV}-admin-mig; do
    gcloud compute instance-groups managed delete "${mig}" \
      --zone="${ZONE}" --quiet \
      --project="${PROJECT_ID}" 2>/dev/null || true
  done
}

destroy_instance_templates() {
  echo ">>> Instance templates verwijderen (omgeving: ${ENV})..."
  for tpl in synapse-${ENV}-api-template synapse-${ENV}-ui-template synapse-${ENV}-admin-template; do
    gcloud compute instance-templates delete "${tpl}" \
      --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  done
}
