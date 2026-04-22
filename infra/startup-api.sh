#!/bin/bash
docker-credential-gcr configure-docker --registries=gcr.io
docker pull gcr.io/synapse-ip-j2/mvp-app:latest
docker stop mvp-api 2>/dev/null; docker rm mvp-api 2>/dev/null
docker run -d --name mvp-api --restart always -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e DB_HOST=$(gcloud secrets versions access latest --secret=db-host) \
  -e DB_PORT=5432 \
  -e DB_NAME=mvp-db \
  -e DB_USER=mvp-user \
  -e DB_PASSWORD=$(gcloud secrets versions access latest --secret=db-password) \
  -e ApiKey=$(gcloud secrets versions access latest --secret=api-key) \
  gcr.io/synapse-ip-j2/mvp-app:latest
