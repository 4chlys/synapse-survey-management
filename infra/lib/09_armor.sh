#!/bin/bash
# Cloud Armor beveiligingsbeleid + WAF regels

ARMOR_POLICY="synapse-armor"

create_cloud_armor() {
  echo ">>> Cloud Armor policy aanmaken: ${ARMOR_POLICY}..."
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

  echo "  >>> Policy koppelen aan backend services..."
  for backend in synapse-api-backend synapse-ui-backend synapse-admin-backend; do
    gcloud compute backend-services update "${backend}" \
      --security-policy="${ARMOR_POLICY}" \
      --global \
      --project="${PROJECT_ID}" 2>/dev/null || echo "  (${backend} niet gevonden, overgeslagen)"
  done

  echo "  Cloud Armor actief. Kost: ~\$5/maand (policy) + requests."
}

destroy_cloud_armor() {
  echo ">>> Cloud Armor policy ontkoppelen en verwijderen..."
  for backend in synapse-api-backend synapse-ui-backend synapse-admin-backend; do
    gcloud compute backend-services update "${backend}" \
      --security-policy="" \
      --global \
      --project="${PROJECT_ID}" 2>/dev/null || true
  done
  gcloud compute security-policies delete "${ARMOR_POLICY}" \
    --project="${PROJECT_ID}" --quiet 2>/dev/null || true
}
