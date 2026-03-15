#!/bin/bash
#=============================================================================================================================
# Autheurs: Quinten Morreel, Noa Robles Levy                                                                                  |
# IP Project Sprint 1 Startup Script                                                                                          |
# Description: Dit script start de verschillende componenten op om ons IP project te kunnen gebruiken via de cloud,           |
#              de verschillende componenten die worden aangemaakt/ingesteld zijn: VM, Cloud SQL, Firewall and DNS.            |
#              De VM krijgt dan oon het startup script nodig dat hier in aangemaakt wordt.                                    |
#=============================================================================================================================

# if [ "$(id -u)" != "0" ];
# then 
# echo "Use as sudo"
# exit 1
# fi

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
# echo "=== Stap 2: Cloud SQL aanmaken ==="
# gcloud sql instances create $DB_INSTANCE \
#   --database-version=POSTGRES_15 \
#   --tier=db-f1-micro \
#   --region=$REGION \
#   --root-password=$DB_PASSWORD

# gcloud sql databases create $DB_NAME \
#   --instance=$DB_INSTANCE

# gcloud sql users create $DB_USER \
#   --instance=$DB_INSTANCE \
#   --password=$DB_PASSWORD

# Cloud SQL IP ophalen
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
echo "=== Stap 5: VM aanmaken ==="

# Startup script aanmaken met de juiste variabelen
cat > /tmp/startup.sh << EOF
#!/bin/bash
apt-get update -y
apt-get install -y docker.io
systemctl start docker
systemctl enable docker
gcloud auth configure-docker --quiet

# API container
docker pull $IMAGE_API
docker run -d \\
  --name mvp-api \\
  --restart always \\
  -p 8080:8080 \\
  -e ASPNETCORE_ENVIRONMENT=Production \\
  -e DB_HOST=$DB_IP \\
  -e DB_PORT=5432 \\
  -e DB_USER=$DB_USER \\
  -e DB_PASSWORD=$DB_PASSWORD \\
  -e DB_NAME=$DB_NAME \\
  -e ApiKey=REDACTED" \\
  $IMAGE_API

# UI-User container
docker pull $IMAGE_UI
docker run -d \\
  --name mvp-ui \\
  --restart always \\
  -p 80:8080 \\
  -e ASPNETCORE_ENVIRONMENT=Production \\
  -e ApiBaseUrl=http://localhost:8080 \\
    -e ApiKey=REDACTED" \\
  $IMAGE_UI
EOF

gcloud compute instances create $VM_NAME \
  --zone=$ZONE \
  --machine-type=e2-micro \
  --image-family=ubuntu-2204-lts \
  --image-project=ubuntu-os-cloud \
  --boot-disk-size=20GB \
  --tags=http-server,https-server \
  --metadata-from-file startup-script=/tmp/startup.sh

echo ""
echo "=== Stap 6: Firewall instellen ==="
gcloud compute firewall-rules create allow-http \
  --allow=tcp:80 \
  --target-tags=http-server 2>/dev/null || echo "Firewall regel allow-http bestaat al"

gcloud compute firewall-rules create allow-8080 \
  --allow=tcp:8080 \
  --target-tags=http-server 2>/dev/null || echo "Firewall regel allow-8080 bestaat al"

echo ""
echo "=== Stap 7: VM IP toevoegen aan Cloud SQL ==="
VM_IP=$(gcloud compute instances describe $VM_NAME \
  --zone=$ZONE \
  --format="value(networkInterfaces[0].accessConfigs[0].natIP)")

# Bestaande authorized networks ophalen en VM IP toevoegen (zodat Cloud Run toegang behoudt)
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
echo "UI bereikbaar op:  http://$VM_IP"
echo "API bereikbaar op: http://$VM_IP:8080"
echo ""
