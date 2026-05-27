#!/bin/bash
# Uptime checks and alert policies use the Monitoring REST API because gcloud
# has no --use-ssl flag for uptime checks; REST is the only way to set useSsl: true.

setup_logging_iam() {
  echo ">>> Logging/Monitoring IAM rollen toekennen aan compute SA..."
  local compute_sa="${PROJECT_NUMBER}-compute@developer.gserviceaccount.com"

  gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
    --member="serviceAccount:${compute_sa}" \
    --role="roles/logging.logWriter" \
    --quiet

  gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
    --member="serviceAccount:${compute_sa}" \
    --role="roles/monitoring.metricWriter" \
    --quiet

  echo "  Docker stdout/stderr op COS VMs wordt automatisch doorgestuurd naar Cloud Logging."
}

_monitoring_token() {
  gcloud auth print-access-token
}

_find_uptime_check() {
  local display="$1" token="$2"
  curl -sf \
    "https://monitoring.googleapis.com/v3/projects/${PROJECT_ID}/uptimeCheckConfigs" \
    -H "Authorization: Bearer ${token}" \
    | python3 -c "
import sys, json
for c in json.load(sys.stdin).get('uptimeCheckConfigs', []):
    if c.get('displayName') == sys.argv[1]:
        print(c['name']); break
" "${display}" 2>/dev/null
}

_find_alert_policy() {
  local display="$1" token="$2"
  curl -sf \
    "https://monitoring.googleapis.com/v3/projects/${PROJECT_ID}/alertPolicies" \
    -H "Authorization: Bearer ${token}" \
    | python3 -c "
import sys, json
for p in json.load(sys.stdin).get('alertPolicies', []):
    if p.get('displayName') == sys.argv[1]:
        print(p['name']); break
" "${display}" 2>/dev/null
}

create_uptime_checks() {
  echo ">>> Uptime checks aanmaken via REST API (HTTPS, omgeving: ${ENV})..."
  local token
  token=$(_monitoring_token)

  local -A checks=(
    ["API"]="api.${DOMAIN}"
    ["UI"]="${DOMAIN}"
  )

  for svc in "${!checks[@]}"; do
    local host="${checks[$svc]}"
    local display="Synapse ${ENV} ${svc} Health"

    if [[ -n "$(_find_uptime_check "${display}" "${token}")" ]]; then
      echo "  (${display} al aanwezig)"
      continue
    fi

    local body
    body=$(python3 -c "
import json, sys
print(json.dumps({
    'displayName': sys.argv[1],
    'monitoredResource': {
        'type': 'uptime_url',
        'labels': {'host': sys.argv[2], 'project_id': sys.argv[3]}
    },
    'httpCheck': {
        'path': '/health',
        'port': 443,
        'useSsl': True,
        'validateSsl': True,
        'requestMethod': 'GET'
    },
    'period': '300s',
    'timeout': '10s'
}))" "${display}" "${host}" "${PROJECT_ID}")

    if curl -sf -X POST \
        "https://monitoring.googleapis.com/v3/projects/${PROJECT_ID}/uptimeCheckConfigs" \
        -H "Authorization: Bearer ${token}" \
        -H "Content-Type: application/json" \
        -d "${body}" > /dev/null; then
      echo "  ✅ ${display} aangemaakt"
    else
      echo "  ⚠️  ${display} kon niet aangemaakt worden"
    fi
  done
}

destroy_uptime_checks() {
  echo ">>> Uptime checks verwijderen (omgeving: ${ENV})..."
  local token
  token=$(_monitoring_token)

  local names
  names=$(curl -sf \
    "https://monitoring.googleapis.com/v3/projects/${PROJECT_ID}/uptimeCheckConfigs" \
    -H "Authorization: Bearer ${token}" \
    | python3 -c "
import sys, json
prefix = 'Synapse ' + sys.argv[1] + ' '
for c in json.load(sys.stdin).get('uptimeCheckConfigs', []):
    d = c.get('displayName', '')
    if d.startswith(prefix) and d.endswith(' Health'):
        print(c['name'])
" "${ENV}" 2>/dev/null)

  if [[ -z "${names}" ]]; then
    echo "  (geen uptime checks gevonden voor omgeving ${ENV})"
    return
  fi

  while IFS= read -r name; do
    [[ -z "${name}" ]] && continue
    if curl -sf -X DELETE \
        "https://monitoring.googleapis.com/v3/${name}" \
        -H "Authorization: Bearer ${token}" > /dev/null; then
      echo "  Verwijderd: ${name##*/}"
    else
      echo "  ⚠️  Kon niet verwijderen: ${name##*/}"
    fi
  done <<< "${names}"
}

# Filters on checker_location="eur-belgium" because Cloud Armor blocks non-EU checkers
# with 403 — that is expected behaviour and must not trigger an alert.
# Notification channels cannot be created via gcloud and must be added manually in Console:
#   https://console.cloud.google.com/monitoring/alerting
create_alert_policy() {
  echo ">>> Alert policy aanmaken (omgeving: ${ENV}, filter: eur-belgium)..."
  local token
  token=$(_monitoring_token)

  local display="Synapse ${ENV} - Uptime Failure (eur-belgium)"

  if [[ -n "$(_find_alert_policy "${display}" "${token}")" ]]; then
    echo "  (alert policy al aanwezig)"
    return
  fi

  local body
  body=$(python3 -c "
import json, sys
display, project = sys.argv[1], sys.argv[2]
print(json.dumps({
    'displayName': display,
    'conditions': [{
        'displayName': 'Uptime check failed in eur-belgium',
        'conditionThreshold': {
            'filter': (
                'metric.type=\"monitoring.googleapis.com/uptime_check/check_passed\"'
                ' AND resource.type=\"uptime_url\"'
                ' AND metric.labels.checker_location=\"eur-belgium\"'
                ' AND resource.label.project_id=\"' + project + '\"'
            ),
            'comparison': 'COMPARISON_LT',
            'thresholdValue': 1,
            'duration': '60s',
            'trigger': {'count': 1},
            'aggregations': [{
                'alignmentPeriod': '300s',
                'perSeriesAligner': 'ALIGN_FRACTION_TRUE'
            }]
        }
    }],
    'combiner': 'OR',
    'enabled': True,
    'alertStrategy': {'autoClose': '604800s'},
    'notificationChannels': []
}))" "${display}" "${PROJECT_ID}")

  if curl -sf -X POST \
      "https://monitoring.googleapis.com/v3/projects/${PROJECT_ID}/alertPolicies" \
      -H "Authorization: Bearer ${token}" \
      -H "Content-Type: application/json" \
      -d "${body}" > /dev/null; then
    echo "  ✅ Alert policy aangemaakt"
    echo "  ⚠️  Voeg manueel notification channels toe via Cloud Console:"
    echo "     https://console.cloud.google.com/monitoring/alerting?project=${PROJECT_ID}"
  else
    echo "  ⚠️  Alert policy kon niet aangemaakt worden"
  fi
}

destroy_alert_policy() {
  echo ">>> Alert policy verwijderen (omgeving: ${ENV})..."
  local token
  token=$(_monitoring_token)

  local display="Synapse ${ENV} - Uptime Failure (eur-belgium)"
  local name
  name=$(_find_alert_policy "${display}" "${token}")

  if [[ -z "${name}" ]]; then
    echo "  (geen alert policy gevonden voor omgeving ${ENV})"
    return
  fi

  if curl -sf -X DELETE \
      "https://monitoring.googleapis.com/v3/${name}" \
      -H "Authorization: Bearer ${token}" > /dev/null; then
    echo "  Verwijderd: ${name##*/}"
  else
    echo "  ⚠️  Kon niet verwijderen: ${name##*/}"
  fi
}
