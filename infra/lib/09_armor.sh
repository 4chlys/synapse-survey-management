#!/bin/bash

ARMOR_POLICY="synapse-${ENV}-armor"

create_cloud_armor() {
  echo ">>> Cloud Armor policy aanmaken: ${ARMOR_POLICY} (omgeving: ${ENV})..."
  gcloud compute security-policies create "${ARMOR_POLICY}" \
    --description="Synapse WAF - DDoS, OWASP, rate limiting" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  # WAF and rate-limiting run before geo rules so EU traffic is not exempt from them.
  # SQLi exclusions: character anomaly rules (942xxx) trigger on ASP.NET antiforgery tokens,
  # session cookies, passwords with special chars, and admin form data. 942130 (tautology),
  # 942300/942370 (comments/anomaly), 942460/942490 (meta-char/classic) added after preview-mode audit.
  # XSS exclusion (941320): filter-evasion rule fires on HTML entities in admin content fields.
  # LFI exclusion (930130): restricted file access triggers on ASP.NET API routes with dots.
  # RCE exclusions (932190/932200): Unix shell expression rules trigger on URL-encoded
  # returnUrl parameters containing slashes and equals signs (%2F, %3D).
  echo "  >>> WAF-regel toevoegen (SQLi + XSS + LFI + RCE)..."
  gcloud compute security-policies rules create 1000 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="evaluatePreconfiguredExpr('sqli-v33-stable', ['owasp-crs-v030301-id942130-sqli', 'owasp-crs-v030301-id942200-sqli', 'owasp-crs-v030301-id942260-sqli', 'owasp-crs-v030301-id942300-sqli', 'owasp-crs-v030301-id942340-sqli', 'owasp-crs-v030301-id942370-sqli', 'owasp-crs-v030301-id942420-sqli', 'owasp-crs-v030301-id942421-sqli', 'owasp-crs-v030301-id942430-sqli', 'owasp-crs-v030301-id942431-sqli', 'owasp-crs-v030301-id942432-sqli', 'owasp-crs-v030301-id942440-sqli', 'owasp-crs-v030301-id942460-sqli', 'owasp-crs-v030301-id942490-sqli']) || evaluatePreconfiguredExpr('xss-v33-stable', ['owasp-crs-v030301-id941320-xss']) || evaluatePreconfiguredExpr('lfi-v33-stable', ['owasp-crs-v030301-id930130-lfi']) || evaluatePreconfiguredExpr('rce-v33-stable', ['owasp-crs-v030301-id932190-rce', 'owasp-crs-v030301-id932200-rce'])" \
    --action=deny-403 \
    --description="OWASP WAF: SQLi + XSS + LFI + RCE (ASP.NET false positives excluded)" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  # Geo-allow rules at 1500-1550 (max 5 expressions per rule due to Cloud Armor CEVAL limit).
  # Geo-deny at 1600 blocks all non-EU traffic before rate-limiting at 2000.
  echo "  >>> Geo-blocking: EU allow-regels toevoegen (prio 1500-1550)..."
  gcloud compute security-policies rules create 1500 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="origin.region_code == 'BE' || origin.region_code == 'NL' || origin.region_code == 'LU' || origin.region_code == 'DE' || origin.region_code == 'FR'" \
    --action=allow \
    --description="Geo allow: BE NL LU DE FR" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  gcloud compute security-policies rules create 1510 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="origin.region_code == 'IT' || origin.region_code == 'ES' || origin.region_code == 'PT' || origin.region_code == 'AT' || origin.region_code == 'SE'" \
    --action=allow \
    --description="Geo allow: IT ES PT AT SE" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  gcloud compute security-policies rules create 1520 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="origin.region_code == 'FI' || origin.region_code == 'DK' || origin.region_code == 'IE' || origin.region_code == 'PL' || origin.region_code == 'CZ'" \
    --action=allow \
    --description="Geo allow: FI DK IE PL CZ" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  gcloud compute security-policies rules create 1530 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="origin.region_code == 'SK' || origin.region_code == 'HU' || origin.region_code == 'RO' || origin.region_code == 'BG' || origin.region_code == 'HR'" \
    --action=allow \
    --description="Geo allow: SK HU RO BG HR" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  gcloud compute security-policies rules create 1540 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="origin.region_code == 'SI' || origin.region_code == 'EE' || origin.region_code == 'LV' || origin.region_code == 'LT' || origin.region_code == 'GR'" \
    --action=allow \
    --description="Geo allow: SI EE LV LT GR" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  gcloud compute security-policies rules create 1550 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="origin.region_code == 'CY' || origin.region_code == 'MT'" \
    --action=allow \
    --description="Geo allow: CY MT" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  gcloud compute security-policies rules create 1600 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="true" \
    --action=deny-403 \
    --description="Geo-blocking: alleen EU toegelaten" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  echo "  >>> Rate limiting regel toevoegen (max 100 req/min per IP)..."
  gcloud compute security-policies rules create 2000 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="true" \
    --action=rate-based-ban \
    --rate-limit-threshold-count=100 \
    --rate-limit-threshold-interval-sec=60 \
    --ban-duration-sec=300 \
    --conform-action=allow \
    --exceed-action=deny-429 \
    --enforce-on-key=IP \
    --description="Rate limiting: max 100 req/min per IP, ban 5 min" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  echo "  >>> Policy koppelen aan backend services..."
  for backend in "synapse-${ENV}-api-backend" "synapse-${ENV}-ui-backend" "synapse-${ENV}-admin-backend"; do
    gcloud compute backend-services update "${backend}" \
      --security-policy="${ARMOR_POLICY}" \
      --global \
      --project="${PROJECT_ID}" 2>/dev/null || echo "  (${backend} niet gevonden, overgeslagen)"
  done

  echo "  Cloud Armor actief. Kost: ~\$10/maand (policy + regels) + requests."
}

add_mig_ips_to_armor() {
  echo ">>> UI en Admin VM IPs toevoegen aan Cloud Armor (omgeving: ${ENV})..."

  gcloud compute instance-groups managed wait-until "synapse-${ENV}-ui-mig" \
    --stable --zone="${ZONE}" --project="${PROJECT_ID}" 2>/dev/null || true
  gcloud compute instance-groups managed wait-until "synapse-${ENV}-admin-mig" \
    --stable --zone="${ZONE}" --project="${PROJECT_ID}" 2>/dev/null || true

  UI_INSTANCE=$(gcloud compute instance-groups managed list-instances "synapse-${ENV}-ui-mig" \
    --zone="${ZONE}" --project="${PROJECT_ID}" --format="value(instance)" | head -1 | sed 's|.*/||')
  ADMIN_INSTANCE=$(gcloud compute instance-groups managed list-instances "synapse-${ENV}-admin-mig" \
    --zone="${ZONE}" --project="${PROJECT_ID}" --format="value(instance)" | head -1 | sed 's|.*/||')

  UI_IP=$(gcloud compute instances describe "${UI_INSTANCE}" \
    --zone="${ZONE}" --project="${PROJECT_ID}" \
    --format="value(networkInterfaces[0].accessConfigs[0].natIP)" 2>/dev/null || true)
  ADMIN_IP=$(gcloud compute instances describe "${ADMIN_INSTANCE}" \
    --zone="${ZONE}" --project="${PROJECT_ID}" \
    --format="value(networkInterfaces[0].accessConfigs[0].natIP)" 2>/dev/null || true)

  if [[ -z "${UI_IP}" || -z "${ADMIN_IP}" ]]; then
    echo "  ⚠️  Kon externe IPs niet ophalen — voeg handmatig toe."
    return
  fi

  echo "  UI IP: ${UI_IP} | Admin IP: ${ADMIN_IP}"
  gcloud compute security-policies rules create 50 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="origin.ip == '${UI_IP}' || origin.ip == '${ADMIN_IP}'" \
    --action=allow \
    --description="Allow UI en Admin MIG server-side API calls" \
    --project="${PROJECT_ID}" 2>/dev/null || \
  gcloud compute security-policies rules update 50 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="origin.ip == '${UI_IP}' || origin.ip == '${ADMIN_IP}'" \
    --action=allow \
    --description="Allow UI en Admin MIG server-side API calls" \
    --project="${PROJECT_ID}"
}

fix_armor_priorities() {
  # Verwijdert alle oude geo-regels en maakt de correcte layout aan:
  # WAF (1000) → geo-allow (1500-1550) → geo-deny (1600) → rate-limit (2000)
  echo ">>> Cloud Armor prioriteiten corrigeren voor omgeving ${ENV}..."

  local old_geo_priorities=(100 110 120 130 140 150 200 3000 3010 3020 3030 3040 3050 3100)
  for p in "${old_geo_priorities[@]}"; do
    gcloud compute security-policies rules delete "${p}" \
      --security-policy="${ARMOR_POLICY}" \
      --project="${PROJECT_ID}" --quiet 2>/dev/null || echo "  (prio ${p} niet aanwezig, overgeslagen)"
  done

  echo "  >>> Geo-allow regels aanmaken op prioriteit 1500-1550..."
  gcloud compute security-policies rules create 1500 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="origin.region_code == 'BE' || origin.region_code == 'NL' || origin.region_code == 'LU' || origin.region_code == 'DE' || origin.region_code == 'FR'" \
    --action=allow --description="Geo allow: BE NL LU DE FR" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (prio 1500 al aanwezig)"

  gcloud compute security-policies rules create 1510 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="origin.region_code == 'IT' || origin.region_code == 'ES' || origin.region_code == 'PT' || origin.region_code == 'AT' || origin.region_code == 'SE'" \
    --action=allow --description="Geo allow: IT ES PT AT SE" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (prio 1510 al aanwezig)"

  gcloud compute security-policies rules create 1520 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="origin.region_code == 'FI' || origin.region_code == 'DK' || origin.region_code == 'IE' || origin.region_code == 'PL' || origin.region_code == 'CZ'" \
    --action=allow --description="Geo allow: FI DK IE PL CZ" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (prio 1520 al aanwezig)"

  gcloud compute security-policies rules create 1530 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="origin.region_code == 'SK' || origin.region_code == 'HU' || origin.region_code == 'RO' || origin.region_code == 'BG' || origin.region_code == 'HR'" \
    --action=allow --description="Geo allow: SK HU RO BG HR" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (prio 1530 al aanwezig)"

  gcloud compute security-policies rules create 1540 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="origin.region_code == 'SI' || origin.region_code == 'EE' || origin.region_code == 'LV' || origin.region_code == 'LT' || origin.region_code == 'GR'" \
    --action=allow --description="Geo allow: SI EE LV LT GR" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (prio 1540 al aanwezig)"

  gcloud compute security-policies rules create 1550 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="origin.region_code == 'CY' || origin.region_code == 'MT'" \
    --action=allow --description="Geo allow: CY MT" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (prio 1550 al aanwezig)"

  gcloud compute security-policies rules create 1600 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="true" \
    --action=deny-403 \
    --description="Geo-blocking: alleen EU toegelaten" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (prio 1600 al aanwezig)"

  echo "  >>> Rate-limit expressie corrigeren naar true..."
  gcloud compute security-policies rules update 2000 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="true" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (prio 2000 niet aanwezig, overgeslagen)"

  echo "  Prioriteiten gecorrigeerd. Nieuwe volgorde: WAF (1000) → geo-allow (1500-1550) → geo-deny (1600) → rate-limit (2000)."
}

destroy_cloud_armor() {
  echo ">>> Cloud Armor policy ontkoppelen en verwijderen (omgeving: ${ENV})..."
  if ! gcloud compute security-policies describe "${ARMOR_POLICY}" \
      --project="${PROJECT_ID}" --quiet &>/dev/null; then
    echo "  (${ARMOR_POLICY} niet gevonden, overgeslagen)"
    return
  fi
  for backend in "synapse-${ENV}-api-backend" "synapse-${ENV}-ui-backend" "synapse-${ENV}-admin-backend"; do
    gcloud compute backend-services update "${backend}" \
      --security-policy="" \
      --global \
      --project="${PROJECT_ID}" 2>/dev/null || true
  done
  gcloud compute security-policies delete "${ARMOR_POLICY}" \
    --project="${PROJECT_ID}" --quiet 2>/dev/null || true
}
