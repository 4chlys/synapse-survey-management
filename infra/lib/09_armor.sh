#!/bin/bash
# Cloud Armor beveiligingsbeleid + WAF regels

ARMOR_POLICY="synapse-${ENV}-armor"

create_cloud_armor() {
  echo ">>> Cloud Armor policy aanmaken: ${ARMOR_POLICY} (omgeving: ${ENV})..."
  gcloud compute security-policies create "${ARMOR_POLICY}" \
    --description="Synapse WAF - DDoS, OWASP, rate limiting" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  echo "  >>> Gecombineerde WAF-regel toevoegen (SQLi + XSS + LFI + RCE)..."
  gcloud compute security-policies rules create 1000 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="evaluatePreconfiguredExpr('sqli-v33-stable') || evaluatePreconfiguredExpr('xss-v33-stable') || evaluatePreconfiguredExpr('lfi-v33-stable') || evaluatePreconfiguredExpr('rce-v33-stable')" \
    --action=deny-403 \
    --description="OWASP Top 10 WAF combinatie (1 regel = lagere kost)" \
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

  echo "  >>> Geo-blocking: EU allow-regels toevoegen..."
  gcloud compute security-policies rules create 100 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'BE' || origin.region_code == 'NL' || origin.region_code == 'LU' || origin.region_code == 'DE' || origin.region_code == 'FR'" --action=allow --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"
  gcloud compute security-policies rules create 110 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'IT' || origin.region_code == 'ES' || origin.region_code == 'PT' || origin.region_code == 'AT' || origin.region_code == 'SE'" --action=allow --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"
  gcloud compute security-policies rules create 120 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'FI' || origin.region_code == 'DK' || origin.region_code == 'IE' || origin.region_code == 'PL' || origin.region_code == 'CZ'" --action=allow --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"
  gcloud compute security-policies rules create 130 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'SK' || origin.region_code == 'HU' || origin.region_code == 'RO' || origin.region_code == 'BG' || origin.region_code == 'HR'" --action=allow --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"
  gcloud compute security-policies rules create 140 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'SI' || origin.region_code == 'EE' || origin.region_code == 'LV' || origin.region_code == 'LT' || origin.region_code == 'GR'" --action=allow --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"
  gcloud compute security-policies rules create 150 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'CY' || origin.region_code == 'MT'" --action=allow --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"
  gcloud compute security-policies rules create 200 --security-policy="${ARMOR_POLICY}" --expression="true" --action=deny-403 --description="Geo-blocking: alleen EU toegelaten" --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  echo "  >>> Policy koppelen aan backend services..."
  for backend in "synapse-${ENV}-api-backend" "synapse-${ENV}-ui-backend" "synapse-${ENV}-admin-backend"; do
    gcloud compute backend-services update "${backend}" \
      --security-policy="${ARMOR_POLICY}" \
      --global \
      --project="${PROJECT_ID}" 2>/dev/null || echo "  (${backend} niet gevonden, overgeslagen)"
  done

  echo "  Cloud Armor actief. Kost: ~\$10/maand (policy + regels) + requests."
}

destroy_cloud_armor() {
  echo ">>> Cloud Armor policy ontkoppelen en verwijderen (omgeving: ${ENV})..."
  for backend in "synapse-${ENV}-api-backend" "synapse-${ENV}-ui-backend" "synapse-${ENV}-admin-backend"; do
    gcloud compute backend-services update "${backend}" \
      --security-policy="" \
      --global \
      --project="${PROJECT_ID}" 2>/dev/null || true
  done
  gcloud compute security-policies delete "${ARMOR_POLICY}" \
    --project="${PROJECT_ID}" --quiet 2>/dev/null || true
}
