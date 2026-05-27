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

_wait_api_schema_ready() {
  echo "  ⏳ Wachten op API database-initialisatie (max 8 minuten)..."

  gcloud compute instance-groups managed wait-until "synapse-${ENV}-api-mig" \
    --stable --zone="${ZONE}" --project="${PROJECT_ID}" 2>/dev/null || true

  local api_vm
  api_vm=$(gcloud compute instance-groups managed list-instances "synapse-${ENV}-api-mig" \
    --zone="${ZONE}" --project="${PROJECT_ID}" \
    --format="value(instance)" 2>/dev/null | head -1 | sed 's|.*/||')

  if [[ -z "$api_vm" ]]; then
    echo "  ⚠️  Geen API VM gevonden, 4 minuten wachten als fallback..."
    sleep 240
    return
  fi

  local elapsed=0
  while [[ $elapsed -lt 480 ]]; do
    local status
    status=$(gcloud compute ssh "${api_vm}" \
      --zone="${ZONE}" --project="${PROJECT_ID}" \
      --ssh-flag="-o StrictHostKeyChecking=no -o LogLevel=ERROR" \
      --command="docker inspect mvp-api --format='{{.State.Status}}' 2>/dev/null" \
      --quiet 2>/dev/null || echo "")

    if [[ "${status}" == "running" ]]; then
      echo "  ✅ API container actief — 60s buffer voor EnsureCreated + seeding..."
      sleep 60
      echo "  ✅ API schema klaar"
      return
    fi

    echo "  ... (${elapsed}s) API nog niet actief (${status:-onbekend})..."
    sleep 20
    elapsed=$((elapsed + 20))
  done

  echo "  ⚠️  Timeout na 8 minuten — Admin toch starten"
}

create_migs() {
  echo ">>> Managed Instance Groups aanmaken (omgeving: ${ENV})..."

  # API eerst: EnsureCreated() moet het schema aanmaken voordat
  # Admin's MigrateAsync() dezelfde database ziet en overslaat.
  _create_mig "synapse-${ENV}-api-mig" "synapse-${ENV}-api-template" 1 5
  _wait_api_schema_ready

  _create_mig "synapse-${ENV}-ui-mig"    "synapse-${ENV}-ui-template"    1 5
  _create_mig "synapse-${ENV}-admin-mig" "synapse-${ENV}-admin-template" 1 1
}

configure_autohealing() {
  echo ">>> Autohealing configureren op MIGs (omgeving: ${ENV})..."
  for type in api ui admin; do
    local mig="synapse-${ENV}-${type}-mig"
    local hc="synapse-${ENV}-${type}-health"
    echo "  - ${mig} → ${hc} (initial-delay=300s)"
    gcloud compute instance-groups managed update "${mig}" \
      --health-check="${hc}" \
      --initial-delay=300 \
      --zone="${ZONE}" \
      --project="${PROJECT_ID}" 2>/dev/null || echo "    (autohealing al geconfigureerd)"
  done
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
