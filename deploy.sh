#!/bin/bash
#=============================================================================================================================
# Authors: Quinten Morreel, Noa Robles Levy                                                                                  |
# IP Project Sprint 1 Startup Script                                                                                         |
# Description: This script starts the various components needed to use our IP project via                                    |
#   the cloud. The components that are created/configured are: VM, Cloud SQL, Firewall and DNS.                              |
#  The VM then receives the startup script that is created here.                                                             |
#=============================================================================================================================

# if [ "$(id -u)" != "0" ];
# then 
# echo "Use as sudo"
# exit 1
# fi

# ===== LOADING SECRETS FROM .env =====
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
echo "  SYNAPSE - DEPLOY SCRIPT"
echo "======================================"

echo ""
echo "=== Stap 1: Project instellen ==="
gcloud config set project $PROJECT_ID
gcloud services enable compute.googleapis.com
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
  --root-password=$DB_PASSWORD

gcloud sql databases create $DB_NAME \
  --instance=$DB_INSTANCE

gcloud sql users create $DB_USER \
  --instance=$DB_INSTANCE \
  --password=$DB_PASSWORD

# Getting Cloud SQL IP 
DB_IP=$(gcloud sql instances describe $DB_INSTANCE \
  --format="value(ipAddresses[0].ipAddress)")
echo "Cloud SQL IP: $DB_IP"

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
echo "=== Stap 4: Zonal DNS instellen ==="
gcloud compute project-info add-metadata \
  --metadata=VmDnsSetting=ZonalOnly

echo ""
echo "=== Stap 5: Static IP reserveren ==="
gcloud compute addresses create mvp-static-ip --region=$REGION 2>/dev/null || echo "Static IP bestaat al"
STATIC_IP=$(gcloud compute addresses describe mvp-static-ip \
  --region=$REGION --format="value(address)")
echo "Static IP: $STATIC_IP"

echo ""
echo "=== Stap 6: VM aanmaken ==="

# Create startup script with correct variables
cat > /tmp/startup.sh << EOF
#!/bin/bash
apt-get update -y
apt-get install -y docker.io
systemctl start docker
systemctl enable docker
gcloud auth configure-docker --quiet

docker network create mvp-net

# API container
docker pull $IMAGE_API
docker run -d \\
  --name mvp-api \\
  --restart always \\
  --network mvp-net \\
  -p 8080:8080 \\
  -e ASPNETCORE_ENVIRONMENT=Production \\
  -e DB_HOST=$DB_IP \\
  -e DB_PORT=5432 \\
  -e DB_USER=$DB_USER \\
  -e DB_PASSWORD=$DB_PASSWORD \\
  -e DB_NAME=$DB_NAME \\
  -e ApiKey=$ApiKey \\
  $IMAGE_API

# UI-User container
docker pull $IMAGE_UI
docker run -d \\
  --name mvp-ui \\
  --restart always \\
  --network mvp-net \\
  -p 80:8080 \\
  -e ASPNETCORE_ENVIRONMENT=Production \\
  -e ApiBaseUrl=http://mvp-api:8080 \\
  -e ApiKey=$ApiKey \\
  $IMAGE_UI
EOF

gcloud compute instances create $VM_NAME \
  --zone=$ZONE \
  --machine-type=e2-micro \
  --image-family=ubuntu-2204-lts \
  --image-project=ubuntu-os-cloud \
  --boot-disk-size=20GB \
  --tags=http-server,https-server \
  --scopes=cloud-platform \
  --address=$STATIC_IP \
  --metadata-from-file startup-script=/tmp/startup.sh

echo ""
echo "=== Stap 7: Firewall instellen ==="
gcloud compute firewall-rules create allow-http \
  --allow=tcp:80 \
  --target-tags=http-server 2>/dev/null || echo "Firewall regel allow-http bestaat al"

gcloud compute firewall-rules create allow-8080 \
  --allow=tcp:8080 \
  --target-tags=http-server 2>/dev/null || echo "Firewall regel allow-8080 bestaat al"

echo ""
echo "=== Stap 8: VM IP toevoegen aan Cloud SQL ==="
VM_IP=$STATIC_IP

# Getting existing authorized networks and VM IP toevoegen (so Cloud Run keeps access)
EXISTING=$(gcloud sql instances describe $DB_INSTANCE \
  --format="value(settings.ipConfiguration.authorizedNetworks[].value)" | tr ';' ',')
if [ -z "$EXISTING" ]; then
  NETWORKS="$VM_IP"
else
  NETWORKS="$EXISTING,$VM_IP"
fi

gcloud sql instances patch $DB_INSTANCE \
  --authorized-networks=$NETWORKS

echo ""
echo "======================================"
echo "  DEPLOY VOLTOOID!"
echo "======================================"
echo ""
echo "Wacht 2-3 minuten voor de app opstart..."
echo "UI bereikbaar op:  http://$STATIC_IP"
echo "API bereikbaar op: http://$STATIC_IP:8080"
echo ""
