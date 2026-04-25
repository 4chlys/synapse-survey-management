#!/bin/bash
#=============================================================================================================================
# Autheurs: Quinten Morreel, Noa Robles Levy                                                                                  |
# IP Project Sprint 1 Cloud Run Deploy Script                                                                                 |
# Description: Dit script deployt de applicatie naar Google Cloud Run (serverless containers).                               |
#              Cloud Run beheert de containers automatisch en schaalt naar 0 als er geen verkeer is.                         |
#              De API en UI krijgen elk een eigen HTTPS URL van Google.                                                       |
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
echo "  SYNAPSE - CLOUD RUN DEPLOY SCRIPT"
echo "======================================"

echo ""
echo "=== Stap 1: Project instellen ==="
gcloud config set project $PROJECT_ID
gcloud services enable run.googleapis.com
gcloud services enable sqladmin.googleapis.com
gcloud services enable containerregistry.googleapis.com
gcloud services enable artifactregistry.googleapis.com
gcloud services enable aiplatform.googleapis.com

echo ""
echo "=== Stap 2: Cloud SQL aanmaken ==="
gcloud sql instances create $DB_INSTANCE \
  --database-version=POSTGRES_15 \
  --tier=db-f1-micro \
  --region=$REGION \
  --root-password=$DB_PASSWORD 2>/dev/null || echo "Cloud SQL instantie bestaat al"

gcloud sql databases create $DB_NAME \
  --instance=$DB_INSTANCE 2>/dev/null || echo "Database bestaat al"

gcloud sql users create $DB_USER \
  --instance=$DB_INSTANCE \
  --password=$DB_PASSWORD 2>/dev/null || echo "Gebruiker bestaat al"

# Cloud SQL IP ophalen
DB_IP=$(gcloud sql instances describe $DB_INSTANCE \
  --format="value(ipAddresses[0].ipAddress)")
echo "Cloud SQL IP: $DB_IP"

# Cloud Run heeft dynamische IPs, dus alle IPs toestaan voor Cloud SQL
echo "Cloud SQL openstellen voor Cloud Run..."
gcloud sql instances patch $DB_INSTANCE \
  --authorized-networks=0.0.0.0/0 \
  --quiet

echo ""
echo "=== Stap 3: Docker images bouwen en pushen ==="
gcloud auth configure-docker --quiet

# API image
docker build --provenance=false -t $IMAGE_API .
docker push $IMAGE_API

# UI-User image
docker build --provenance=false -t $IMAGE_UI -f Dockerfile.ui-user .
docker push $IMAGE_UI

echo ""
echo "=== Stap 4: API deployen naar Cloud Run ==="
gcloud run deploy mvp-api \
  --image=$IMAGE_API \
  --platform=managed \
  --region=$REGION \
  --allow-unauthenticated \
  --port=8080 \
  --set-env-vars="ASPNETCORE_ENVIRONMENT=Production,ASPNETCORE_URLS=http://+:8080,DB_HOST=$DB_IP,DB_PORT=5432,DB_USER=$DB_USER,DB_PASSWORD=$DB_PASSWORD,DB_NAME=$DB_NAME,ApiKey=$ApiKey"

# API URL ophalen
API_URL=$(gcloud run services describe mvp-api \
  --platform=managed \
  --region=$REGION \
  --format="value(status.url)")
echo "API URL: $API_URL"

echo ""
echo "=== Stap 5: UI deployen naar Cloud Run ==="
gcloud run deploy mvp-ui \
  --image=$IMAGE_UI \
  --platform=managed \
  --region=$REGION \
  --allow-unauthenticated \
  --port=8080 \
  --set-env-vars="ASPNETCORE_ENVIRONMENT=Production,ASPNETCORE_URLS=http://+:8080,ApiKey=$ApiKey,ApiBaseUrl=$API_URL"

# UI URL ophalen
UI_URL=$(gcloud run services describe mvp-ui \
  --platform=managed \
  --region=$REGION \
  --format="value(status.url)")
echo "UI URL: $UI_URL"

echo ""
echo "=== Stap 6: API CORS updaten met UI URL ==="
gcloud run services update mvp-api \
  --platform=managed \
  --region=$REGION \
  --update-env-vars="UI_URL=$UI_URL"

echo ""
echo "======================================"
echo "  CLOUD RUN DEPLOY VOLTOOID!"
echo "======================================"
echo ""
echo "UI bereikbaar op:  $UI_URL"
echo "API bereikbaar op: $API_URL"
echo ""
