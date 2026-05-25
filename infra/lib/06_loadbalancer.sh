#!/bin/bash
# Global HTTPS Load Balancer: health checks, backend services, URL map, SSL, forwarding rules

create_health_checks() {
  echo ">>> Health checks aanmaken (omgeving: ${ENV})..."

  gcloud compute health-checks create http "synapse-${ENV}-api-health" \
    --port=8080 --request-path=/health \
    --check-interval=10 --timeout=5 \
    --healthy-threshold=2 --unhealthy-threshold=3 \
    --global --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-api-health al aanwezig)"

  gcloud compute health-checks create http "synapse-${ENV}-ui-health" \
    --port=80 --request-path=/health \
    --check-interval=10 --timeout=5 \
    --healthy-threshold=2 --unhealthy-threshold=3 \
    --global --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-ui-health al aanwezig)"

  gcloud compute health-checks create http "synapse-${ENV}-admin-health" \
    --port=80 --request-path=/health \
    --check-interval=10 --timeout=5 \
    --healthy-threshold=2 --unhealthy-threshold=3 \
    --global --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-admin-health al aanwezig)"
}

create_backend_services() {
  echo ">>> Backend services aanmaken (omgeving: ${ENV})..."
  for svc in api ui admin; do
    gcloud compute backend-services create "synapse-${ENV}-${svc}-backend" \
      --protocol=HTTP --port-name=http \
      --health-checks="synapse-${ENV}-${svc}-health" \
      --connection-draining-timeout=30 \
      --global --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-${svc}-backend al aanwezig)"

    gcloud compute backend-services add-backend "synapse-${ENV}-${svc}-backend" \
      --instance-group="synapse-${ENV}-${svc}-mig" \
      --instance-group-zone="${ZONE}" \
      --balancing-mode=UTILIZATION \
      --max-utilization=0.8 \
      --global --project="${PROJECT_ID}" 2>/dev/null || true
  done
}

create_url_map() {
  echo ">>> URL map aanmaken (omgeving: ${ENV})..."
  gcloud compute url-maps create "synapse-${ENV}-url-map" \
    --default-service="synapse-${ENV}-ui-backend" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-url-map al aanwezig)"

  gcloud compute url-maps add-path-matcher "synapse-${ENV}-url-map" \
    --path-matcher-name=api-matcher \
    --default-service="synapse-${ENV}-api-backend" \
    --new-hosts="api.${DOMAIN}" \
    --project="${PROJECT_ID}" 2>/dev/null || true

  gcloud compute url-maps add-path-matcher "synapse-${ENV}-url-map" \
    --path-matcher-name=admin-matcher \
    --default-service="synapse-${ENV}-admin-backend" \
    --new-hosts="admin.${DOMAIN}" \
    --project="${PROJECT_ID}" 2>/dev/null || true
}

create_ssl_and_proxy() {
  echo ">>> SSL certificaat + HTTPS proxy aanmaken via Certificate Manager (omgeving: ${ENV})..."

  # DNS authorization — vereist een CNAME-record bij de domeinregistrar.
  # Het certificaat dekt zowel het apex-domein als alle subdomains (*.${DOMAIN}).
  gcloud certificate-manager dns-authorizations create "synapse-${ENV}-dns-auth" \
    --domain="${DOMAIN}" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-dns-auth al aanwezig)"

  local cname_name cname_target
  cname_name=$(gcloud certificate-manager dns-authorizations describe "synapse-${ENV}-dns-auth" \
    --project="${PROJECT_ID}" --format="value(dnsResourceRecord.name)")
  cname_target=$(gcloud certificate-manager dns-authorizations describe "synapse-${ENV}-dns-auth" \
    --project="${PROJECT_ID}" --format="value(dnsResourceRecord.data)")

  echo ""
  echo "  ⚠️  Voeg dit CNAME-record toe bij je domeinregistrar (voor wildcard SSL):"
  echo "     Naam  : ${cname_name}"
  echo "     Waarde: ${cname_target}"
  echo "  Het certificaat provisioneert automatisch zodra het CNAME-record zichtbaar is."
  echo ""

  # Certificate Manager certificaat met wildcard
  gcloud certificate-manager certificates create "synapse-${ENV}-cert" \
    --domains="${DOMAIN},*.${DOMAIN}" \
    --dns-authorizations="synapse-${ENV}-dns-auth" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-cert al aanwezig)"

  # Certificate map + entries (apex en wildcard)
  gcloud certificate-manager maps create "synapse-${ENV}-cert-map" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-cert-map al aanwezig)"

  gcloud certificate-manager maps entries create "synapse-${ENV}-cert-entry-apex" \
    --map="synapse-${ENV}-cert-map" \
    --certificates="synapse-${ENV}-cert" \
    --hostname="${DOMAIN}" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-cert-entry-apex al aanwezig)"

  gcloud certificate-manager maps entries create "synapse-${ENV}-cert-entry-wildcard" \
    --map="synapse-${ENV}-cert-map" \
    --certificates="synapse-${ENV}-cert" \
    --hostname="*.${DOMAIN}" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-cert-entry-wildcard al aanwezig)"

  # HTTPS proxy met certificate map (ondersteunt wildcards, ipv ssl-certificates)
  gcloud compute target-https-proxies create "synapse-${ENV}-https-proxy" \
    --url-map="synapse-${ENV}-url-map" \
    --certificate-map="synapse-${ENV}-cert-map" \
    --global --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-https-proxy al aanwezig)"
}

reserve_static_ip() {
  echo ">>> Statisch IP reserveren (omgeving: ${ENV})..."
  gcloud compute addresses create "synapse-${ENV}-lb-ip" \
    --global --ip-version=IPV4 \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-lb-ip al aanwezig)"

  local ip
  ip=$(gcloud compute addresses describe "synapse-${ENV}-lb-ip" \
    --global --format="value(address)" --project="${PROJECT_ID}")
  echo "  LB IP: ${ip}"
}

create_forwarding_rules() {
  echo ">>> Forwarding rules aanmaken (omgeving: ${ENV}, HTTPS + HTTP redirect)..."

  gcloud compute forwarding-rules create "synapse-${ENV}-https-rule" \
    --address="synapse-${ENV}-lb-ip" \
    --global \
    --target-https-proxy="synapse-${ENV}-https-proxy" \
    --ports=443 \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-https-rule al aanwezig)"

  local redirect_yaml
  redirect_yaml=$(mktemp)
  cat > "${redirect_yaml}" <<EOF
name: synapse-${ENV}-http-redirect
defaultUrlRedirect:
  httpsRedirect: true
  redirectResponseCode: MOVED_PERMANENTLY_DEFAULT
EOF
  gcloud compute url-maps import "synapse-${ENV}-http-redirect" \
    --global --source="${redirect_yaml}" --quiet \
    --project="${PROJECT_ID}" 2>/dev/null || true
  rm -f "${redirect_yaml}"

  gcloud compute target-http-proxies create "synapse-${ENV}-http-proxy" \
    --url-map="synapse-${ENV}-http-redirect" \
    --global --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-http-proxy al aanwezig)"

  gcloud compute forwarding-rules create "synapse-${ENV}-http-rule" \
    --address="synapse-${ENV}-lb-ip" \
    --global \
    --target-http-proxy="synapse-${ENV}-http-proxy" \
    --ports=80 \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-http-rule al aanwezig)"
}

destroy_loadbalancer() {
  echo ">>> Load Balancer afbreken (omgeving: ${ENV})..."

  gcloud compute forwarding-rules delete "synapse-${ENV}-https-rule" "synapse-${ENV}-http-rule" \
    --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  gcloud compute target-https-proxies delete "synapse-${ENV}-https-proxy" \
    --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  gcloud compute target-http-proxies delete "synapse-${ENV}-http-proxy" \
    --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  gcloud compute url-maps delete "synapse-${ENV}-url-map" "synapse-${ENV}-http-redirect" \
    --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true

  # Certificate Manager resources
  gcloud certificate-manager maps entries delete "synapse-${ENV}-cert-entry-apex" \
    --map="synapse-${ENV}-cert-map" --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  gcloud certificate-manager maps entries delete "synapse-${ENV}-cert-entry-wildcard" \
    --map="synapse-${ENV}-cert-map" --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  gcloud certificate-manager maps delete "synapse-${ENV}-cert-map" \
    --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  gcloud certificate-manager certificates delete "synapse-${ENV}-cert" \
    --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  gcloud certificate-manager dns-authorizations delete "synapse-${ENV}-dns-auth" \
    --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  # Verouderd: verwijder ook eventuele legacy ssl-certificates
  gcloud compute ssl-certificates delete "synapse-${ENV}-ssl-cert" \
    --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true

  for svc in api ui admin; do
    gcloud compute backend-services delete "synapse-${ENV}-${svc}-backend" \
      --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true
    gcloud compute health-checks delete "synapse-${ENV}-${svc}-health" \
      --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  done

  gcloud compute addresses delete "synapse-${ENV}-lb-ip" \
    --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true
}
