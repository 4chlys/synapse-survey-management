#!/bin/bash
# MIG instance templates + Managed Instance Groups + autoscalers

_create_template() {
  local name="$1"
  local machine_type="$2"
  local startup_script="$3"
  echo "  - template: ${name} (${machine_type})"
  gcloud compute instance-templates create "${name}" \
    --machine-type="${machine_type}" \
    --image-family=cos-stable \
    --image-project=cos-cloud \
    --tags=allow-health-check \
    --scopes=cloud-platform \
    --metadata-from-file "startup-script=${SCRIPT_DIR}/${startup_script}" \
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
    --project="${PROJECT_ID}"

  gcloud compute instance-groups managed set-named-ports "${name}" \
    --named-ports="http:$([ "${name}" = "synapse-api-mig" ] && echo 8080 || echo 80)" \
    --zone="${ZONE}" \
    --project="${PROJECT_ID}"
}

create_instance_templates() {
  echo ">>> Instance templates aanmaken..."
  _create_template "synapse-api-template"   "e2-small"  "startup-api.sh"
  _create_template "synapse-ui-template"    "e2-small"  "startup-ui.sh"
  _create_template "synapse-admin-template" "e2-micro"  "startup-admin.sh"
}

create_migs() {
  echo ">>> Managed Instance Groups aanmaken..."
  _create_mig "synapse-api-mig"   "synapse-api-template"   1 5
  _create_mig "synapse-ui-mig"    "synapse-ui-template"    1 5
  _create_mig "synapse-admin-mig" "synapse-admin-template" 0 1
}

destroy_migs() {
  echo ">>> MIGs verwijderen..."
  for mig in synapse-api-mig synapse-ui-mig synapse-admin-mig; do
    gcloud compute instance-groups managed delete "${mig}" \
      --zone="${ZONE}" --quiet \
      --project="${PROJECT_ID}" 2>/dev/null || true
  done
}

destroy_instance_templates() {
  echo ">>> Instance templates verwijderen..."
  for tpl in synapse-api-template synapse-ui-template synapse-admin-template; do
    gcloud compute instance-templates delete "${tpl}" \
      --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  done
}
