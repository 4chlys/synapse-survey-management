#!/bin/bash
#=============================================================================================================================
# Autheurs: Quinten Morreel, Noa Robles Levy                                                                                  |
# IP Project Sprint 1 Cloud Run Destroy Script                                                                                |
# Description: Dit script verwijdert alle Cloud Run resources die aangemaakt zijn door deploy-cloudrun.sh.                  |
#=============================================================================================================================


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
echo "  SYNAPSE - CLOUD RUN DESTROY SCRIPT"
echo "======================================"
echo ""
echo "WAARSCHUWING: Dit verwijdert alle Cloud Run services en de database!"
read -p "Ben je zeker? (y/n): " CONFIRM
if [ "$CONFIRM" != "y" ]; then
  echo "Geannuleerd."
  exit 1
fi

echo ""
echo "=== Cloud Run services verwijderen ==="
gcloud run services delete mvp-ui \
  --platform=managed \
  --region=$REGION \
  --quiet 2>/dev/null || echo "mvp-ui bestond niet"

gcloud run services delete mvp-api \
  --platform=managed \
  --region=$REGION \
  --quiet 2>/dev/null || echo "mvp-api bestond niet"

echo ""
echo "=== Cloud SQL verwijderen ==="
gcloud sql instances delete $DB_INSTANCE \
  --quiet 2>/dev/null || echo "Cloud SQL instantie bestond niet"

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
