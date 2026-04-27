#!/bin/bash
# Global HTTPS Load Balancer: health checks, backend services, URL map, SSL, forwarding rules

create_health_checks() {
  echo ">>> Health checks aanmaken..."

  gcloud compute health-checks create http synapse-api-health \
    --port=8080 --request-path=/health \
    --check-interval=10 --timeout=5 \
    --healthy-threshold=2 --unhealthy-threshold=3 \
    --global --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-api-health al aanwezig)"

  gcloud compute health-checks create http synapse-ui-health \
    --port=80 --request-path=/health \
    --check-interval=10 --timeout=5 \
    --healthy-threshold=2 --unhealthy-threshold=3 \
    --global --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-ui-health al aanwezig)"

  gcloud compute health-checks create http synapse-admin-health \
    --port=80 --request-path=/health \
    --check-interval=10 --timeout=5 \
    --healthy-threshold=2 --unhealthy-threshold=3 \
    --global --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-admin-health al aanwezig)"
}

create_backend_services() {
  echo ">>> Backend services aanmaken..."
  for svc in api ui admin; do
    gcloud compute backend-services create "synapse-${svc}-backend" \
      --protocol=HTTP --port-name=http \
      --health-checks="synapse-${svc}-health" \
      --global --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-${svc}-backend al aanwezig)"

    gcloud compute backend-services add-backend "synapse-${svc}-backend" \
      --instance-group="synapse-${svc}-mig" \
      --instance-group-zone="${ZONE}" \
      --balancing-mode=UTILIZATION \
      --max-utilization=0.8 \
      --global --project="${PROJECT_ID}" 2>/dev/null || true
  done
}

create_url_map() {
  echo ">>> URL map aanmaken..."
  gcloud compute url-maps create synapse-url-map \
    --default-service=synapse-ui-backend \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-url-map al aanwezig)"

  gcloud compute url-maps add-path-matcher synapse-url-map \
    --path-matcher-name=api-matcher \
    --default-service=synapse-api-backend \
    --new-hosts=api.synapse-ip.xyz \
    --project="${PROJECT_ID}" 2>/dev/null || true

  gcloud compute url-maps add-path-matcher synapse-url-map \
    --path-matcher-name=admin-matcher \
    --default-service=synapse-admin-backend \
    --new-hosts=admin.synapse-ip.xyz \
    --project="${PROJECT_ID}" 2>/dev/null || true
}

create_ssl_and_proxy() {
  echo ">>> SSL certificaat + HTTPS proxy aanmaken..."
  gcloud compute ssl-certificates create synapse-ssl-cert \
    --domains="synapse-ip.xyz,api.synapse-ip.xyz,admin.synapse-ip.xyz,*.synapse-ip.xyz" \
    --global --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-ssl-cert al aanwezig)"

  gcloud compute target-https-proxies create synapse-https-proxy \
    --url-map=synapse-url-map \
    --ssl-certificates=synapse-ssl-cert \
    --global --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-https-proxy al aanwezig)"
}

reserve_static_ip() {
  echo ">>> Statisch IP reserveren..."
  gcloud compute addresses create synapse-lb-ip \
    --global --ip-version=IPV4 \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-lb-ip al aanwezig)"

  local ip
  ip=$(gcloud compute addresses describe synapse-lb-ip \
    --global --format="value(address)" --project="${PROJECT_ID}")
  echo "  LB IP: ${ip}"
}

create_forwarding_rules() {
  echo ">>> Forwarding rules aanmaken (HTTPS + HTTP redirect)..."

  gcloud compute forwarding-rules create synapse-https-rule \
    --address=synapse-lb-ip \
    --global \
    --target-https-proxy=synapse-https-proxy \
    --ports=443 \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-https-rule al aanwezig)"

  # HTTP → HTTPS redirect URL map
  local redirect_yaml
  redirect_yaml=$(mktemp)
  cat > "${redirect_yaml}" <<'YAML'
name: synapse-http-redirect
defaultUrlRedirect:
  httpsRedirect: true
  redirectResponseCode: MOVED_PERMANENTLY_DEFAULT
YAML
  gcloud compute url-maps import synapse-http-redirect \
    --global --source="${redirect_yaml}" --quiet \
    --project="${PROJECT_ID}" 2>/dev/null || true
  rm -f "${redirect_yaml}"

  gcloud compute target-http-proxies create synapse-http-proxy \
    --url-map=synapse-http-redirect \
    --global --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-http-proxy al aanwezig)"

  gcloud compute forwarding-rules create synapse-http-rule \
    --address=synapse-lb-ip \
    --global \
    --target-http-proxy=synapse-http-proxy \
    --ports=80 \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (synapse-http-rule al aanwezig)"
}

destroy_loadbalancer() {
  echo ">>> Load Balancer afbreken..."

  gcloud compute forwarding-rules delete synapse-https-rule synapse-http-rule \
    --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  gcloud compute target-https-proxies delete synapse-https-proxy \
    --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  gcloud compute target-http-proxies delete synapse-http-proxy \
    --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  gcloud compute ssl-certificates delete synapse-ssl-cert \
    --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  gcloud compute url-maps delete synapse-url-map synapse-http-redirect \
    --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true

  for svc in api ui admin; do
    gcloud compute backend-services delete "synapse-${svc}-backend" \
      --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true
    gcloud compute health-checks delete "synapse-${svc}-health" \
      --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true
  done

  gcloud compute addresses delete synapse-lb-ip \
    --global --quiet --project="${PROJECT_ID}" 2>/dev/null || true
}
