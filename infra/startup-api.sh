#!/bin/bash
export DOCKER_CONFIG=/tmp/.docker
mkdir -p /tmp/.docker

TOKEN=$(curl -s "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/default/token" \
  -H "Metadata-Flavor: Google" | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])")

ENV=$(curl -s "http://metadata.google.internal/computeMetadata/v1/instance/attributes/env" \
  -H "Metadata-Flavor: Google" 2>/dev/null || echo "prod")

PROJECT_ID=$(curl -s "http://metadata.google.internal/computeMetadata/v1/project/project-id" \
  -H "Metadata-Flavor: Google")

# prod gebruikt :latest, andere omgevingen gebruiken :{ENV}-latest
IMAGE_TAG="$([ "${ENV}" = "prod" ] && echo "latest" || echo "${ENV}-latest")"

get_secret() {
  curl -s "https://secretmanager.googleapis.com/v1/projects/${PROJECT_ID}/secrets/${ENV}-$1/versions/latest:access" \
    -H "Authorization: Bearer $TOKEN" | python3 -c "import sys,json,base64; print(base64.b64decode(json.load(sys.stdin)['payload']['data']).decode())" | tr -d '\r'
}

docker login -u oauth2accesstoken -p "$TOKEN" https://gcr.io
docker pull "gcr.io/${PROJECT_ID}/mvp-app:${IMAGE_TAG}"
docker stop mvp-api 2>/dev/null; docker rm mvp-api 2>/dev/null

docker run -d --name mvp-api --restart always -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e AllowedHosts='*' \
  -e DB_HOST="$(get_secret db-host)" \
  -e DB_PORT=5432 \
  -e DB_NAME=mvp-db \
  -e DB_USER=mvp-user \
  -e DB_PASSWORD="$(get_secret db-password)" \
  -e ApiKey="$(get_secret api-key)" \
  -e RESEND_API_KEY="$(get_secret resend-api-key)" \
  -e EMAIL_FROM="noreply@synapse-ip.xyz" \
  -e GCS_BUCKET="$(get_secret gcs-bucket)" \
  "gcr.io/${PROJECT_ID}/mvp-app:${IMAGE_TAG}"
