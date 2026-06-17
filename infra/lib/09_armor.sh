#!/bin/bash

ARMOR_POLICY="synapse-${ENV}-armor"

create_cloud_armor() {
  echo ">>> Cloud Armor policy aanmaken: ${ARMOR_POLICY} (omgeving: ${ENV})..."
  gcloud compute security-policies create "${ARMOR_POLICY}" \
    --description="Synapse WAF - DDoS, OWASP, rate limiting" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  # WAF and rate-limiting run before geo rules so EU traffic is not exempt from them.
  # All WAF rules are skipped for admin controller paths (/Project/, /Question/, /Category/,
  # /Platform/, /Settings/, /Auth/, /AdminMembership/, /AdminEmailQueue/, /api/admin/) because
  # admin form data (HTML content, AI prompts, config values) triggers XSS/SQLi/RCE/LFI false
  # positives. Admins are authenticated; server-side API calls from UI/Admin VMs travel via the
  # internal NLB and never reach Cloud Armor, so no VM IP whitelist is needed.
  # Image upload and AI chat paths are also excluded for the same reason (binary data, HMAC tokens,
  # AI-generated free text trigger multiple rules unpredictably).
  # SQLi exclusions on remaining participant paths: character anomaly rules (942xxx) fire on
  # ASP.NET antiforgery tokens. RCE exclusions (932100/190/200): URL-encoded returnUrl values.
  # XSS exclusions (941320/330/340): filter-evasion rules fire on JPEG data and base64 HMAC tokens.
  # LFI exclusion (930130): restricted file access triggers on ASP.NET API routes with dots.
  echo "  >>> WAF-regel toevoegen (SQLi + XSS + LFI + RCE)..."
  gcloud compute security-policies rules create 1000 \
    --security-policy="${ARMOR_POLICY}" \
    --expression="!request.path.matches('^/[0-9]+/api/IdeaSupportApi/chat/?$|^/[0-9]+/api/IdeaImageApi/upload/?$|^/[0-9]+/api/IdeaImageApi/describe/?$|^/api/upload/idea-image|^/api/upload/describe-image|^/api/admin/|^/Project/|^/Question/|^/Category/|^/Platform/|^/Settings/|^/Auth/|^/AdminMembership/|^/AdminEmailQueue/|^/api/projects/[0-9]+/social/upload-image') && (evaluatePreconfiguredExpr('sqli-v33-stable', ['owasp-crs-v030301-id942110-sqli', 'owasp-crs-v030301-id942130-sqli', 'owasp-crs-v030301-id942200-sqli', 'owasp-crs-v030301-id942260-sqli', 'owasp-crs-v030301-id942300-sqli', 'owasp-crs-v030301-id942340-sqli', 'owasp-crs-v030301-id942370-sqli', 'owasp-crs-v030301-id942420-sqli', 'owasp-crs-v030301-id942421-sqli', 'owasp-crs-v030301-id942430-sqli', 'owasp-crs-v030301-id942431-sqli', 'owasp-crs-v030301-id942432-sqli', 'owasp-crs-v030301-id942440-sqli', 'owasp-crs-v030301-id942460-sqli', 'owasp-crs-v030301-id942490-sqli']) || evaluatePreconfiguredExpr('rce-v33-stable', ['owasp-crs-v030301-id932100-rce', 'owasp-crs-v030301-id932190-rce', 'owasp-crs-v030301-id932200-rce']) || evaluatePreconfiguredExpr('xss-v33-stable', ['owasp-crs-v030301-id941320-xss', 'owasp-crs-v030301-id941330-xss', 'owasp-crs-v030301-id941340-xss']) || evaluatePreconfiguredExpr('lfi-v33-stable', ['owasp-crs-v030301-id930130-lfi']))" \
    --action=deny-403 \
    --description="OWASP WAF: all rules skip admin paths + upload/AI paths; apply to participant paths only" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  # Rate-limiting is combined with geo-allow: each rule covers max 5 EU countries (Cloud Armor
  # limit per expression). conform-action=allow stops evaluation for under-threshold EU traffic,
  # so non-EU traffic falls through all 6 rate-limit rules to the geo-deny catch-all below.
  echo "  >>> Rate limiting + geo-allow regels toevoegen (EU, max 100 req/min per IP)..."
  gcloud compute security-policies rules create 1100 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'BE' || origin.region_code == 'NL' || origin.region_code == 'LU' || origin.region_code == 'DE' || origin.region_code == 'FR'" --action=rate-based-ban --rate-limit-threshold-count=100 --rate-limit-threshold-interval-sec=60 --ban-duration-sec=300 --conform-action=allow --exceed-action=deny-429 --enforce-on-key=IP --description="Rate limiting EU (BE NL LU DE FR): max 100 req/min per IP, ban 5 min" --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"
  gcloud compute security-policies rules create 1110 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'IT' || origin.region_code == 'ES' || origin.region_code == 'PT' || origin.region_code == 'AT' || origin.region_code == 'SE'" --action=rate-based-ban --rate-limit-threshold-count=100 --rate-limit-threshold-interval-sec=60 --ban-duration-sec=300 --conform-action=allow --exceed-action=deny-429 --enforce-on-key=IP --description="Rate limiting EU (IT ES PT AT SE): max 100 req/min per IP, ban 5 min" --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"
  gcloud compute security-policies rules create 1120 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'FI' || origin.region_code == 'DK' || origin.region_code == 'IE' || origin.region_code == 'PL' || origin.region_code == 'CZ'" --action=rate-based-ban --rate-limit-threshold-count=100 --rate-limit-threshold-interval-sec=60 --ban-duration-sec=300 --conform-action=allow --exceed-action=deny-429 --enforce-on-key=IP --description="Rate limiting EU (FI DK IE PL CZ): max 100 req/min per IP, ban 5 min" --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"
  gcloud compute security-policies rules create 1130 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'SK' || origin.region_code == 'HU' || origin.region_code == 'RO' || origin.region_code == 'BG' || origin.region_code == 'HR'" --action=rate-based-ban --rate-limit-threshold-count=100 --rate-limit-threshold-interval-sec=60 --ban-duration-sec=300 --conform-action=allow --exceed-action=deny-429 --enforce-on-key=IP --description="Rate limiting EU (SK HU RO BG HR): max 100 req/min per IP, ban 5 min" --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"
  gcloud compute security-policies rules create 1140 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'SI' || origin.region_code == 'EE' || origin.region_code == 'LV' || origin.region_code == 'LT' || origin.region_code == 'GR'" --action=rate-based-ban --rate-limit-threshold-count=100 --rate-limit-threshold-interval-sec=60 --ban-duration-sec=300 --conform-action=allow --exceed-action=deny-429 --enforce-on-key=IP --description="Rate limiting EU (SI EE LV LT GR): max 100 req/min per IP, ban 5 min" --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"
  gcloud compute security-policies rules create 1150 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'CY' || origin.region_code == 'MT'" --action=rate-based-ban --rate-limit-threshold-count=100 --rate-limit-threshold-interval-sec=60 --ban-duration-sec=300 --conform-action=allow --exceed-action=deny-429 --enforce-on-key=IP --description="Rate limiting EU (CY MT): max 100 req/min per IP, ban 5 min" --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  echo "  >>> Geo-blocking: catch-all deny voor niet-EU verkeer..."
  gcloud compute security-policies rules create 1200 --security-policy="${ARMOR_POLICY}" --expression="true" --action=deny-403 --description="Geo-blocking: alleen EU toegelaten" --project="${PROJECT_ID}" 2>/dev/null || echo "  (al aanwezig, overgeslagen)"

  echo "  >>> Policy koppelen aan backend services..."
  for backend in "synapse-${ENV}-api-backend" "synapse-${ENV}-ui-backend" "synapse-${ENV}-admin-backend"; do
    gcloud compute backend-services update "${backend}" \
      --security-policy="${ARMOR_POLICY}" \
      --global \
      --project="${PROJECT_ID}" 2>/dev/null || echo "  (${backend} niet gevonden, overgeslagen)"
  done

  echo "  Cloud Armor actief. Kost: ~\$10/maand (policy + regels) + requests."
}

fix_armor_priorities() {
  # Migrates existing deployments to the correct layout:
  # WAF (1000) → rate-limit+geo-allow (1100-1150) → geo-deny (1200)
  echo ">>> Cloud Armor prioriteiten corrigeren voor omgeving ${ENV}..."

  local old_priorities=(100 110 120 130 140 150 200 1500 1510 1520 1530 1540 1550 1600 2000 3000 3010 3020 3030 3040 3050 3100)
  for p in "${old_priorities[@]}"; do
    gcloud compute security-policies rules delete "${p}" \
      --security-policy="${ARMOR_POLICY}" \
      --project="${PROJECT_ID}" --quiet 2>/dev/null || echo "  (prio ${p} niet aanwezig, overgeslagen)"
  done

  gcloud compute security-policies rules create 1100 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'BE' || origin.region_code == 'NL' || origin.region_code == 'LU' || origin.region_code == 'DE' || origin.region_code == 'FR'" --action=rate-based-ban --rate-limit-threshold-count=100 --rate-limit-threshold-interval-sec=60 --ban-duration-sec=300 --conform-action=allow --exceed-action=deny-429 --enforce-on-key=IP --description="Rate limiting EU (BE NL LU DE FR): max 100 req/min per IP, ban 5 min" --project="${PROJECT_ID}" 2>/dev/null || echo "  (prio 1100 al aanwezig)"
  gcloud compute security-policies rules create 1110 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'IT' || origin.region_code == 'ES' || origin.region_code == 'PT' || origin.region_code == 'AT' || origin.region_code == 'SE'" --action=rate-based-ban --rate-limit-threshold-count=100 --rate-limit-threshold-interval-sec=60 --ban-duration-sec=300 --conform-action=allow --exceed-action=deny-429 --enforce-on-key=IP --description="Rate limiting EU (IT ES PT AT SE): max 100 req/min per IP, ban 5 min" --project="${PROJECT_ID}" 2>/dev/null || echo "  (prio 1110 al aanwezig)"
  gcloud compute security-policies rules create 1120 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'FI' || origin.region_code == 'DK' || origin.region_code == 'IE' || origin.region_code == 'PL' || origin.region_code == 'CZ'" --action=rate-based-ban --rate-limit-threshold-count=100 --rate-limit-threshold-interval-sec=60 --ban-duration-sec=300 --conform-action=allow --exceed-action=deny-429 --enforce-on-key=IP --description="Rate limiting EU (FI DK IE PL CZ): max 100 req/min per IP, ban 5 min" --project="${PROJECT_ID}" 2>/dev/null || echo "  (prio 1120 al aanwezig)"
  gcloud compute security-policies rules create 1130 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'SK' || origin.region_code == 'HU' || origin.region_code == 'RO' || origin.region_code == 'BG' || origin.region_code == 'HR'" --action=rate-based-ban --rate-limit-threshold-count=100 --rate-limit-threshold-interval-sec=60 --ban-duration-sec=300 --conform-action=allow --exceed-action=deny-429 --enforce-on-key=IP --description="Rate limiting EU (SK HU RO BG HR): max 100 req/min per IP, ban 5 min" --project="${PROJECT_ID}" 2>/dev/null || echo "  (prio 1130 al aanwezig)"
  gcloud compute security-policies rules create 1140 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'SI' || origin.region_code == 'EE' || origin.region_code == 'LV' || origin.region_code == 'LT' || origin.region_code == 'GR'" --action=rate-based-ban --rate-limit-threshold-count=100 --rate-limit-threshold-interval-sec=60 --ban-duration-sec=300 --conform-action=allow --exceed-action=deny-429 --enforce-on-key=IP --description="Rate limiting EU (SI EE LV LT GR): max 100 req/min per IP, ban 5 min" --project="${PROJECT_ID}" 2>/dev/null || echo "  (prio 1140 al aanwezig)"
  gcloud compute security-policies rules create 1150 --security-policy="${ARMOR_POLICY}" --expression="origin.region_code == 'CY' || origin.region_code == 'MT'" --action=rate-based-ban --rate-limit-threshold-count=100 --rate-limit-threshold-interval-sec=60 --ban-duration-sec=300 --conform-action=allow --exceed-action=deny-429 --enforce-on-key=IP --description="Rate limiting EU (CY MT): max 100 req/min per IP, ban 5 min" --project="${PROJECT_ID}" 2>/dev/null || echo "  (prio 1150 al aanwezig)"
  gcloud compute security-policies rules create 1200 --security-policy="${ARMOR_POLICY}" --expression="true" --action=deny-403 --description="Geo-blocking: alleen EU toegelaten" --project="${PROJECT_ID}" 2>/dev/null || echo "  (prio 1200 al aanwezig)"

  echo "  Prioriteiten gecorrigeerd. Nieuwe volgorde: WAF (1000) → rate-limit+geo-allow (1100-1150) → geo-deny (1200)."
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
