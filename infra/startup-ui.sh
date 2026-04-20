#!/bin/bash
docker-credential-gcr configure-docker --registries=gcr.io
docker pull gcr.io/synapse-ip-j2/mvp-ui:latest
docker stop mvp-ui 2>/dev/null; docker rm mvp-ui 2>/dev/null
docker run -d --name mvp-ui --restart always -p 80:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ApiBaseUrl=$(gcloud secrets versions access latest --secret=api-base-url) \
  -e ApiKey=$(gcloud secrets versions access latest --secret=api-key) \
  gcr.io/synapse-ip-j2/mvp-ui:latest
