# Synapse – Deployment Guide

Volledige GCP infrastructuur opzetten van nul met één script.

## Vereisten

| Tool | Installatie |
|------|-------------|
| [gcloud CLI](https://cloud.google.com/sdk/docs/install) | Verplicht |
| GCP account met actieve billing | Verplicht |
| Geregistreerd domein | Verplicht |
| Docker (optioneel) | Enkel voor lokaal testen |

## Stap 1 – GCP Project aanmaken

Maak een nieuw project aan via de [GCP Console](https://console.cloud.google.com) en activeer billing.

## Stap 2 – Configuratie invullen

```bash
cp deploy/config.example.env deploy/config.env
# Vul alle waarden in config.env in
```

Of sla deze stap over — het bootstrap script vraagt alles interactief.

## Stap 3 – Bootstrap uitvoeren

```bash
chmod +x deploy/bootstrap.sh
./deploy/bootstrap.sh
```

Het script doet automatisch:
- APIs inschakelen
- Service accounts + IAM aanmaken
- Secrets aanmaken in Secret Manager
- Cloud SQL (PostgreSQL) aanmaken
- VPC peering instellen
- MIGs aanmaken (API, UI, Admin)
- Cloud Storage bucket aanmaken
- Global HTTPS Load Balancer + SSL
- Cloud Logging + uptime checks

## Stap 3.5 – pgvector extensie installeren

Na bootstrap, vóór de eerste deployment:

```bash
gcloud sql connect mvp-postgres --user=mvp-user --database=mvp-db --project=<PROJECT_ID>
```

Voer in de psql prompt uit:

```sql
CREATE EXTENSION IF NOT EXISTS vector;
```

Vereist omdat `Idea.Embedding` een `vector(3072)` kolom gebruikt. `EnsureCreated` mislukt zonder deze extensie.

**Verificatie:** `\dx` in psql moet `vector` tonen.

## Stap 4 – DNS records instellen

Na het script zie je het Load Balancer IP. Stel bij je domeinregistrar in:

```
@           A   <LB_IP>
api         A   <LB_IP>
admin       A   <LB_IP>
```

## Stap 5 – GitLab CI/CD variabelen

Ga naar je GitLab project → Settings → CI/CD → Variables en voeg toe:

| Variabele | Waarde |
|-----------|--------|
| `PROJECT_ID` | Jouw GCP project ID |
| `REGION` | `europe-west1` |
| `ZONE` | `europe-west1-b` |
| `GCR_SERVICE_ACCOUNT_KEY` | Base64 van synapse-cicd JSON key |
| `GCP_SERVICE_ACCOUNT_KEY` | Base64 van synapse-cicd JSON key |

## Infrastructuur afbreken

```bash
chmod +x infra/destroy.sh
./infra/destroy.sh
```

> ⚠️ Dit verwijdert alle GCP resources inclusief de database. Maak eerst een backup.
