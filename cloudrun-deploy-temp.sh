#!/bin/bash
DB_IP=35.205.22.153

gcloud run deploy mvp-api \
  --image=gcr.io/synapse-ip-j2/mvp-app:latest \
  --platform=managed --region=europe-west1 \
  --allow-unauthenticated --port=8080 \
  --set-env-vars="ASPNETCORE_ENVIRONMENT=Development,ASPNETCORE_URLS=http://+:8080,DB_HOST=$DB_IP,DB_PORT=5432,DB_USER=mvp-user,DB_PASSWORD=REDACTED,DB_NAME=mvp-db"

API_URL=$(gcloud run services describe mvp-api --platform=managed --region=europe-west1 --format="value(status.url)")
echo "API URL: $API_URL"

gcloud run deploy mvp-ui \
  --image=gcr.io/synapse-ip-j2/mvp-ui:latest \
  --platform=managed --region=europe-west1 \
  --allow-unauthenticated --port=8080 \
  --set-env-vars="ASPNETCORE_ENVIRONMENT=Production,ASPNETCORE_URLS=http://+:8080,ApiBaseUrl=$API_URL"

UI_URL=$(gcloud run services describe mvp-ui --platform=managed --region=europe-west1 --format="value(status.url)")
echo "UI URL: $UI_URL"

gcloud run services update mvp-api \
  --platform=managed --region=europe-west1 \
  --update-env-vars="UI_URL=$UI_URL"

echo ""
echo "UI: $UI_URL"
echo "API: $API_URL"
