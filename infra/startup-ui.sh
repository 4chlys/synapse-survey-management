#!/bin/bash
export DOCKER_CONFIG=/tmp/.docker
mkdir -p /tmp/.docker

TOKEN=$(curl -s "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/default/token" \
  -H "Metadata-Flavor: Google" | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])")

docker login -u oauth2accesstoken -p "$TOKEN" https://gcr.io
docker pull gcr.io/synapse-ip-j2/mvp-ui:latest
docker stop mvp-ui 2>/dev/null; docker rm mvp-ui 2>/dev/null

get_secret() {
  curl -s "https://secretmanager.googleapis.com/v1/projects/synapse-ip-j2/secrets/$1/versions/latest:access" \
    -H "Authorization: Bearer $TOKEN" | python3 -c "import sys,json,base64; print(base64.b64decode(json.load(sys.stdin)['payload']['data']).decode())" | tr -d '\r'
}

docker run -d --name mvp-ui --restart always -p 80:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e AllowedHosts='*' \
  -e ApiBaseUrl=$(get_secret api-base-url) \
  -e ApiKey=$(get_secret api-key) \
  -e GCS_BUCKET=$(get_secret gcs-bucket) \
  gcr.io/synapse-ip-j2/mvp-ui:latest
