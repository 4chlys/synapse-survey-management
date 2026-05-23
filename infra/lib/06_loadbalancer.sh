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
  echo ">>> SSL certificaat + HTTPS proxy aanmaken (omgeving: ${ENV})..."
  # *.${DOMAIN} dekt project-subdomains (vb. mentaal-welzijn.test.synapse-ip.xyz).
  # Wildcard vereist DNS-01 verificatie: voeg na aanmaken het CNAME-record toe dat
  # gcloud aangeeft via: gcloud compute ssl-certificates describe synapse-${ENV}-ssl-cert --global
  gcloud compute ssl-certificates create "synapse-${ENV}-ssl-cert" \
    --domains="${DOMAIN},*.${DOMAIN},api.${DOMAIN},admin.${DOMAIN}" \
    --global --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${ENV}-ssl-cert al aanwezig)"

  gcloud compute target-https-proxies create "synapse-${ENV}-https-proxy" \
    --url-map="synapse-${ENV}-url-map" \
    --ssl-certificates="synapse-${ENV}-ssl-cert" \
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
  gcloud compute ssl-certificates delete "synapse-${ENV}-ssl-cert" \
    --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  gcloud compute url-maps delete "synapse-${ENV}-url-map" "synapse-${ENV}-http-redirect" \
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
