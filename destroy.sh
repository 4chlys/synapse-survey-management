#!/bin/bash

# ===== SECRETS LADEN UIT .env =====
if [ ! -f .env ]; then
  echo "FOUT: .env bestand niet gevonden!"
  echo "Kopieer .env.example naar .env en vul de waarden in:"
  echo "  cp .env.example .env"
  exit 1
fi
source .env

IMAGE_API="gcr.io/$PROJECT_ID/mvp-app:latest"
IMAGE_UI="gcr.io/$PROJECT_ID/mvp-ui:latest"

echo "======================================"
echo "  SYNAPSE - DESTROY SCRIPT"
echo "======================================"
echo ""
echo "WAARSCHUWING: Dit verwijdert alles!"
read -p "Ben je zeker? (y/n): " CONFIRM
if [ "$CONFIRM" != "y" ]; then
  echo "Geannuleerd."
  exit 1
fi

echo ""
echo "=== VM verwijderen ==="
gcloud compute instances delete $VM_NAME \
  --zone=$ZONE \
  --quiet

echo ""
echo "=== Cloud SQL verwijderen ==="
gcloud sql instances delete $DB_INSTANCE \
  --quiet

echo ""
echo "=== Firewall regels verwijderen ==="
gcloud compute firewall-rules delete allow-http --quiet 2>/dev/null || echo "allow-http bestond niet"
gcloud compute firewall-rules delete allow-8080 --quiet 2>/dev/null || echo "allow-8080 bestond niet"

echo ""
echo "=== Docker images verwijderen ==="
gcloud container images delete $IMAGE_API \
  --quiet \
  --force-delete-tags 2>/dev/null || echo "API image bestond niet"
gcloud container images delete $IMAGE_UI \
  --quiet \
  --force-delete-tags 2>/dev/null || echo "UI image bestond niet"

echo ""
echo "======================================"
echo "  ALLES VERWIJDERD!"
echo "======================================"
echo ""
