#!/bin/bash

create_vpc_peering() {
  echo ">>> VPC peering instellen voor Cloud SQL..."

  gcloud compute addresses create google-managed-services-default \
    --global \
    --prefix-length=16 \
    --network=default \
    --purpose=VPC_PEERING \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (IP-range al aanwezig, overgeslagen)"

  gcloud services vpc-peerings connect \
    --service=servicenetworking.googleapis.com \
    --ranges=google-managed-services-default \
    --network=default \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (peering al actief, overgeslagen)"
}

create_firewall_rules() {
  echo ">>> Firewall rules aanmaken..."

  gcloud compute firewall-rules create allow-lb-to-mig \
    --network=default \
    --allow=tcp:8080,tcp:80 \
    --source-ranges=130.211.0.0/22,35.191.0.0/16 \
    --target-tags=allow-health-check \
    --description="Load Balancer health checks en verkeer naar MIG VMs" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (allow-lb-to-mig al aanwezig)"

  gcloud compute firewall-rules create allow-iap-ssh \
    --network=default \
    --allow=tcp:22 \
    --source-ranges=35.235.240.0/20 \
    --description="SSH via Cloud IAP" \
    --project="${PROJECT_ID}" 2>/dev/null || echo "  (allow-iap-ssh al aanwezig)"
}

destroy_vpc_peering() {
  echo ">>> VPC peering verwijderen..."
  gcloud services vpc-peerings delete \
    --service=servicenetworking.googleapis.com \
    --network=default \
    --project="${PROJECT_ID}" --quiet 2>/dev/null || true

  gcloud compute addresses delete google-managed-services-default \
    --global --quiet \
    --project="${PROJECT_ID}" 2>/dev/null || true
}

destroy_firewall_rules() {
  echo ">>> Firewall rules verwijderen..."
  gcloud compute firewall-rules delete allow-lb-to-mig allow-iap-ssh \
    --quiet --project="${PROJECT_ID}" 2>/dev/null || true
}
