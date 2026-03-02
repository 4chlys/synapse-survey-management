# SPRINT 1 DOCUMENTATIE

## Stap 1: GC Project opzetten

```
# Login to google cloud via SDK
    gcloud auth login

# Project als actief project zetten
    gcloud config set project synapse-ip-j2

# Benodigde APIs installeren
    gcloud services enable compute.googleapis.com
    gcloud services enable sqladmin.googleapis.com
    gcloud services enable containerregistry.googleapis.com
```

## Stap 2: GC Cloud SQL Database aanmaken

```
# Cloud SQL instantie aanmaken (PostgreSQL)
    gcloud sql instances create mvp-postgres \ # Naam van de instantie
    --database-version=POSTGRES_15 \ # Gekozen database en versie
    --tier=db-f1-micro \ # Gekozen databank tier in google cloud
    --region=europe-west1 \ # Gekozen regio (beste voor belgie)
    --root-password=Admin123! # Gekozen wachtwoord meegeven
```
![alt text](image.png)

![alt text](image-1.png)

```
# Database aanmaken
    gcloud sql databases create mvp-db \ # Naam van de Databank
    --instance=mvp-postgres # Gebruikte instantie
```
![alt text](image-2.png)

## Stap 3: GC VM aanmaken

```
# VM aanmaken
    gcloud compute project-info add-metadata \
    --metadata=VmDnsSetting=ZonalOnly # Zorgen dat de regio zonal is voor bescherming tegen outages

      gcloud compute instances create mvp-vm \ # Start van het commando met de gekozen naam
    --zone=europe-west1-b \ # Gekozen zone
    --machine-type=e2-micro \ # Goedkoopste type vm
    --image-family=ubuntu-2204-lts \ # Gekozen linux distro
    --image-project=ubuntu-os-cloud \ # Waar GC de distro kan vinden
    --boot-disk-size=20GB \ # Grootte van de harde schijf
    --tags=http-server,https-server # Tags voor de firewall regels

```
![alt text](image-3.png)

## Stap 4: Firewall regels instellen

```
 gcloud compute firewall-rules create allow-http \ # Gegeven naam
    --allow=tcp:80 \ # De regel (verkeer op poort 80 toestaan)
    --target-tags=http-server # De related tags (zie vm tags)
```
![alt text](image-4.png)
```

  gcloud compute firewall-rules create allow-8080 \ # idem maar voor HTTPS
    --allow=tcp:8080 \
    --target-tags=http-server

```
![alt text](image-5.png)


## Stap 5: Docker image bouwen en pushen

```
    gcloud auth configure-docker

    docker build -t gcr.io/synapse-ip-j2/mvp-app:latest .

    docker push gcr.io/synapse-ip-j2/mvp-app:latest
```
