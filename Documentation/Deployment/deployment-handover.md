# Synapse — Deployment Handover Guide

This guide walks a new person through setting up the complete Synapse infrastructure from zero, from cloning the repository to a fully working application on Google Cloud.

**Read this before you start:** This guide assumes no prior GCP resources exist. If an environment already exists, see the [Partial Setup](#partial-setup) section.

---

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Clone the Repository](#2-clone-the-repository)
3. [Understand the Architecture](#3-understand-the-architecture)
4. [GCP Project Setup](#4-gcp-project-setup)
5. [Domain Setup (Namecheap)](#5-domain-setup-namecheap)
6. [Create the Config File](#6-create-the-config-file)
7. [Bootstrap the Environment](#7-bootstrap-the-environment)
8. [Add DNS Records](#8-add-dns-records)
9. [Wait for SSL Certificate](#9-wait-for-ssl-certificate)
10. [GitLab CI/CD Variables](#10-gitlab-cicd-variables)
11. [First Production Deploy](#11-first-production-deploy)
12. [Verify Everything Works](#12-verify-everything-works)
13. [Day-to-Day Operations](#13-day-to-day-operations)
14. [Partial Setup](#14-partial-setup)
15. [Troubleshooting](#15-troubleshooting)

---

## 1. Prerequisites

### Tools

Install the following tools before you begin:

**gcloud CLI:**
```
https://cloud.google.com/sdk/docs/install
```
After installing:
```bash
gcloud auth login
gcloud auth application-default login
```

**Docker:**
```
https://docs.docker.com/get-docker/
```

**Git:**
```
https://git-scm.com/downloads
```

**Bash** (Windows users):
- Use Git Bash or WSL2 (recommended). Do not use PowerShell for bootstrap — `read -rs` (silent password input) does not work correctly in PowerShell.

### Accounts and access

You need:
- **GCP project** with billing enabled and Owner or Editor role
- **GitLab** access to the repository with Maintainer role (to set CI/CD variables)
- **Namecheap** access to manage DNS for `synapse-ip.xyz`
- **Resend API key** (`re_...`) — create at https://resend.com

### GCP APIs that will be enabled

Bootstrap enables these automatically:
- `compute.googleapis.com`
- `sqladmin.googleapis.com`
- `secretmanager.googleapis.com`
- `servicenetworking.googleapis.com`
- `artifactregistry.googleapis.com`
- `storage.googleapis.com`
- `aiplatform.googleapis.com`

---

## 2. Clone the Repository

```bash
git clone https://gitlab.com/kdg-ti/integratieproject-1/2526/3_synapse/development.git
cd development
git checkout main   # prod deploys from main
```

---

## 3. Understand the Architecture

Before touching anything, understand what you are setting up:

- **Three services:** API (REST), UI-User (survey frontend), UI-Admin (admin panel)
- **Each service** runs as a Docker container on a separate GCE VM inside a Managed Instance Group
- **A Global Load Balancer** distributes traffic and terminates SSL
- **Wildcard SSL** (`*.synapse-ip.xyz`) allows project short forms as subdomains: `mentaal-welzijn.synapse-ip.xyz`
- **Cloud Armor** is a WAF in front of all services (EU geo-block + OWASP rules + rate limiting)
- **Cloud SQL** is the PostgreSQL database, accessed via private IP
- **Secret Manager** stores all sensitive config; VMs read secrets at startup

For a detailed explanation of every component, see `Documentation/deployment-technical.md`.

---

## 4. GCP Project Setup

### 4.1 Set your project

```bash
gcloud config set project YOUR_PROJECT_ID
```

### 4.2 Verify billing is enabled

Go to https://console.cloud.google.com/billing and confirm the project has an active billing account.

### 4.3 Set up Workload Identity Federation (WIF) for CI/CD

WIF allows GitLab CI pipelines to authenticate with GCP without storing long-lived service account keys. This needs to be set up once per project.

**Create the WIF pool:**
```bash
gcloud iam workload-identity-pools create gitlab-pool \
  --location=global \
  --display-name="GitLab CI Pool" \
  --project=YOUR_PROJECT_ID
```

**Create the WIF provider:**
```bash
gcloud iam workload-identity-pools providers create-oidc gitlab-provider \
  --location=global \
  --workload-identity-pool=gitlab-pool \
  --display-name="GitLab OIDC" \
  --attribute-mapping="google.subject=assertion.sub,attribute.project_path=assertion.project_path" \
  --issuer-uri="https://gitlab.com" \
  --project=YOUR_PROJECT_ID
```

**Get the pool resource name** (you'll need this for the credential file):
```bash
gcloud iam workload-identity-pools describe gitlab-pool \
  --location=global \
  --project=YOUR_PROJECT_ID \
  --format="value(name)"
```

**Create the WIF credential files** (one per environment). Save as `gcp-wif-test.json` and `gcp-wif-prod.json` in the repo root:

```json
{
  "type": "external_account",
  "audience": "//iam.googleapis.com/projects/PROJECT_NUMBER/locations/global/workloadIdentityPools/gitlab-pool/providers/gitlab-provider",
  "subject_token_type": "urn:ietf:params:oauth:token-type:id_token",
  "token_url": "https://sts.googleapis.com/v1/token",
  "credential_source": {
    "file": "/tmp/gcp_token",
    "format": {
      "type": "text"
    }
  },
  "service_account_impersonation_url": "https://iamcredentials.googleapis.com/v1/projects/-/serviceAccounts/synapse-cicd@YOUR_PROJECT_ID.iam.gserviceaccount.com:generateAccessToken"
}
```

Replace `PROJECT_NUMBER` with your project number (`gcloud projects describe YOUR_PROJECT_ID --format="value(projectNumber)"`) and `YOUR_PROJECT_ID` with your project ID. Use `synapse-cicd-prod` instead of `synapse-cicd` for `gcp-wif-prod.json`.

**Bind the GitLab project to the service account:**

After bootstrap creates the service accounts, run:
```bash
# For test (synapse-cicd)
gcloud iam service-accounts add-iam-policy-binding \
  synapse-cicd@YOUR_PROJECT_ID.iam.gserviceaccount.com \
  --role=roles/iam.workloadIdentityUser \
  --member="principalSet://iam.googleapis.com/projects/PROJECT_NUMBER/locations/global/workloadIdentityPools/gitlab-pool/attribute.project_path/kdg-ti/integratieproject-1/2526/3_synapse/development" \
  --project=YOUR_PROJECT_ID

# For prod (synapse-cicd-prod)
gcloud iam service-accounts add-iam-policy-binding \
  synapse-cicd-prod@YOUR_PROJECT_ID.iam.gserviceaccount.com \
  --role=roles/iam.workloadIdentityUser \
  --member="principalSet://iam.googleapis.com/projects/PROJECT_NUMBER/locations/global/workloadIdentityPools/gitlab-pool/attribute.project_path/kdg-ti/integratieproject-1/2526/3_synapse/development" \
  --project=YOUR_PROJECT_ID
```

---

## 5. Domain Setup (Namecheap)

You need a domain with access to DNS management. The current domain is `synapse-ip.xyz` managed in Namecheap.

**You will add DNS records in two rounds:**
- **Round 1** (before bootstrap): nothing yet — you need the LB IPs first
- **Round 2** (after bootstrap): add A-records with the LB IPs and CNAME for SSL

For now, just make sure you have access to the Namecheap DNS management panel.

---

## 6. Create the Config File

Bootstrap reads its configuration from `deploy/config.{ENV}.env`. Create this file for each environment you want to provision.

**Do not commit this file — it contains secrets.**

### For production (`deploy/config.prod.env`):

```bash
PROJECT_ID="YOUR_GCP_PROJECT_ID"
PROJECT_NUMBER=""
REGION="europe-west1"
ZONE="europe-west1-b"
DOMAIN="synapse-ip.xyz"
DB_PASSWORD=REDACTED password, min 12 chars>"
API_KEY=REDACTED string, e.g. openssl rand -base64 32>"
RESEND_API_KEY=REDACTED"
GCS_BUCKET="YOUR_GCP_PROJECT_ID-assets-prod"
VERTEX_AI_LOCATION="europe-west1"
```

### For test (`deploy/config.test.env`):

```bash
PROJECT_ID="YOUR_GCP_PROJECT_ID"
PROJECT_NUMBER=""
REGION="europe-west1"
ZONE="europe-west1-b"
DOMAIN="test.synapse-ip.xyz"
DB_PASSWORD=REDACTED password, min 12 chars>"
API_KEY=REDACTED string>"
RESEND_API_KEY=REDACTED"
GCS_BUCKET="YOUR_GCP_PROJECT_ID-assets-test"
VERTEX_AI_LOCATION="europe-west1"
```

**Important:** Use a different strong password for each environment. Never reuse passwords across environments.

---

## 7. Bootstrap the Environment

Bootstrap must be run from Git Bash or WSL2 on Windows (not PowerShell). From the repo root:

```bash
# For test environment
bash deploy/bootstrap.sh test

# For production environment
bash deploy/bootstrap.sh prod
```

Bootstrap will:
1. Show you a summary of the config and ask you to confirm with `ja`
2. Check gcloud authentication (will open browser if not logged in)
3. Provision all GCP resources (takes 5–15 minutes)
4. **Print a CNAME record** you must add to your DNS registrar — write this down

### What bootstrap provisions

- GCP APIs enabled
- Service accounts + IAM roles
- Secret Manager secrets (populated from config)
- VPC peering for Cloud SQL private IP
- Firewall rules
- Cloud SQL PostgreSQL instance
- GCS buckets (assets + backup)
- Instance templates for API, UI, Admin VMs
- Managed Instance Groups (1 VM each)
- Health checks
- Global Load Balancer (URL map, backends, forwarding rules)
- Certificate Manager (DNS authorization + wildcard cert + cert map)
- Cloud Armor WAF
- Uptime checks

### After bootstrap completes

Bootstrap prints a summary including:
- **Load Balancer IP** — you need this for DNS A-records
- **CNAME record** — you need this for wildcard SSL

**Write down both values before continuing.**

---

## 8. Add DNS Records

Go to your DNS registrar (Namecheap) and add these records.

### For production

| Type | Host | Value |
|---|---|---|
| A | `@` | `<prod LB IP>` |
| A | `*` | `<prod LB IP>` |
| A | `api` | `<prod LB IP>` |
| A | `admin` | `<prod LB IP>` |
| CNAME | `_acme-challenge` | `<value from bootstrap output>` |

### For test

| Type | Host | Value |
|---|---|---|
| A | `test` | `<test LB IP>` |
| A | `*.test` | `<test LB IP>` |
| A | `api.test` | `<test LB IP>` |
| A | `admin.test` | `<test LB IP>` |
| CNAME | `_acme-challenge.test` | `<value from bootstrap output>` |

### Notes

- In Namecheap, do **not** include the base domain in the Host field — Namecheap appends it automatically. For example, `api.test` means `api.test.synapse-ip.xyz`.
- The CNAME value from Google ends with a dot (`.`). Some registrars require you to remove the trailing dot. If the cert gets stuck on `CNAME_MISMATCH`, try removing the trailing dot.
- DNS propagation takes anywhere from a few minutes to a few hours.

### Verify DNS is propagating

```bash
nslookup synapse-ip.xyz 8.8.8.8
nslookup test.synapse-ip.xyz 8.8.8.8
nslookup mentaal-welzijn.synapse-ip.xyz 8.8.8.8
```

All should return the correct LB IP.

---

## 9. Wait for SSL Certificate

After the CNAME record propagates, Google will automatically verify domain ownership and provision the wildcard certificate. This can take 10 minutes to a few hours.

Check the certificate status:

```bash
# Production
gcloud certificate-manager certificates describe synapse-prod-cert \
  --project=YOUR_PROJECT_ID

# Test
gcloud certificate-manager certificates describe synapse-test-cert \
  --project=YOUR_PROJECT_ID
```

Look for `state: ACTIVE`. While the certificate is `AUTHORIZING`, HTTPS will not work. HTTP will work (and redirect to HTTPS, which will fail until cert is active).

**Do not proceed to the next steps until the certificate is ACTIVE.**

---

## 10. GitLab CI/CD Variables

Go to **GitLab → Repository → Settings → CI/CD → Variables** and add the following variables. Mark sensitive values as "Masked".

| Variable | Value | Environment |
|---|---|---|
| `PROJECT_ID` | Your GCP project ID | All |
| `ZONE` | `europe-west1-b` | All |
| `GCR_SERVICE_ACCOUNT_KEY` | Base64-encoded JSON key for a GCR push service account | All |
| `COSIGN_KEY_B64` | Base64-encoded Cosign private key | All |
| `COSIGN_PASSWORD` | Password for Cosign key | All |

The `gcp-wif-test.json` and `gcp-wif-prod.json` files must also be committed to the repository (they do not contain secrets — they reference the WIF pool by resource name).

### Generate GCR service account key

The `GCR_SERVICE_ACCOUNT_KEY` is used only for pushing Docker images to GCR. Create a dedicated service account:

```bash
gcloud iam service-accounts create gcr-pusher \
  --display-name="GCR Image Pusher" \
  --project=YOUR_PROJECT_ID

gcloud projects add-iam-policy-binding YOUR_PROJECT_ID \
  --member="serviceAccount:gcr-pusher@YOUR_PROJECT_ID.iam.gserviceaccount.com" \
  --role="roles/storage.admin"

gcloud iam service-accounts keys create /tmp/gcr-key.json \
  --iam-account="gcr-pusher@YOUR_PROJECT_ID.iam.gserviceaccount.com"

# Base64 encode for GitLab variable
base64 -w 0 /tmp/gcr-key.json
```

### Generate Cosign keys

Cosign is used for signing Docker images (supply chain security):

```bash
# Install cosign: https://docs.sigstore.dev/cosign/system_config/installation/
cosign generate-key-pair

# This creates cosign.key and cosign.pub
base64 -w 0 cosign.key    # → COSIGN_KEY_B64
# Store the password you used → COSIGN_PASSWORD
```

---

## 11. First Production Deploy

Production deployment is **manual** and only triggered from the `main` branch.

### Step 1: Merge development to main

In GitLab, create a merge request from `development` to `main` and merge it.

### Step 2: Wait for the pipeline

The `main` pipeline runs:
1. `build-api`, `build-ui`, `build-admin`
2. `scan-api`, `scan-ui`, `scan-admin` (Trivy vulnerability scans)
3. `push-prod` — pushes images tagged `latest`
4. `sign-prod` — signs images with Cosign

### Step 3: Trigger the production deploy

In GitLab → CI/CD → Pipelines, find the `main` pipeline and click the **play button** on the `deploy-production` job.

This does a rolling replace of all three production MIGs with `--max-unavailable=0 --max-surge=1` (zero downtime).

---

## 12. Verify Everything Works

After bootstrap, DNS propagation, cert activation, and the first pipeline deploy:

### Check the main domains

```
https://synapse-ip.xyz          → UI landing page (user-facing)
https://api.synapse-ip.xyz      → API (should return 404 or JSON error, not connection refused)
https://admin.synapse-ip.xyz    → Admin panel login
```

### Check wildcard subdomain routing

1. Log in to the Admin panel
2. Create or find a project with a short form (e.g., `mentaal-welzijn`)
3. Navigate to `https://mentaal-welzijn.synapse-ip.xyz`
4. You should see the project survey, not a "not found" page

### Check SSL

```bash
curl -v https://synapse-ip.xyz 2>&1 | grep "SSL connection"
curl -v https://mentaal-welzijn.synapse-ip.xyz 2>&1 | grep "SSL connection"
```

Both should show `SSL connection using TLSv1.3`.

### Check backend health

```bash
gcloud compute backend-services get-health synapse-prod-ui-backend \
  --global --project=YOUR_PROJECT_ID
```

Should show `healthState: HEALTHY` for all instances.

### Check that HTTP redirects to HTTPS

```bash
curl -I http://synapse-ip.xyz
# Should return: HTTP/1.1 301 Moved Permanently
# Location: https://synapse-ip.xyz/
```

### Check AI configuration

After the first deploy, configure the AI model in the Admin panel:
- Go to **Admin → System → AI Settings**
- Set the model to `gemini-2.5-flash-lite` (not `gemini-2.0-flash` — that model is unavailable in `europe-west1`)

---

## 13. Day-to-Day Operations

### Deploying new code to test

Every merge to `development` triggers the CI pipeline automatically. The pipeline builds, scans, pushes (`test-latest`), and rolls out to the test environment. No manual action needed.

Pipeline takes approximately 10–15 minutes. Monitor in GitLab → CI/CD → Pipelines.

### Deploying new code to production

1. Merge `development` to `main` (via MR in GitLab)
2. Wait for the `main` pipeline to complete (build + scan + push steps run automatically)
3. Manually trigger `deploy-production` in the pipeline UI

### Updating a Secret Manager secret

```bash
gcloud secrets versions add {ENV}-{secret-name} \
  --data-file=- --project=YOUR_PROJECT_ID <<< "new_value"
```

After updating a secret, the change only takes effect when the VM restarts (rolling replace). To apply immediately:

```bash
gcloud compute instance-groups managed rolling-action replace synapse-{ENV}-{svc}-mig \
  --zone=europe-west1-b --max-unavailable=0 --max-surge=1 --project=YOUR_PROJECT_ID
```

### Updating instance templates (after changing a startup script)

If you change `infra/startup-api.sh`, `startup-ui.sh`, or `startup-admin.sh`, the existing instance templates are not automatically updated. You must recreate them manually:

```bash
# 1. Delete old template
gcloud compute instance-templates delete synapse-{ENV}-{svc}-template \
  --quiet --project=YOUR_PROJECT_ID

# 2. Recreate (from repo root, after pulling latest changes)
gcloud compute instance-templates create synapse-{ENV}-{svc}-template \
  --machine-type=e2-small \
  --image-family=cos-stable --image-project=cos-cloud \
  --tags=allow-health-check --scopes=cloud-platform \
  --metadata="env={ENV},project-id=YOUR_PROJECT_ID,domain=YOUR_DOMAIN" \
  --metadata-from-file="startup-script=infra/startup-{svc}.sh" \
  --project=YOUR_PROJECT_ID

# 3. Point MIG to new template
gcloud compute instance-groups managed set-instance-template synapse-{ENV}-{svc}-mig \
  --template=synapse-{ENV}-{svc}-template \
  --zone=europe-west1-b --project=YOUR_PROJECT_ID

# 4. Rolling replace
gcloud compute instance-groups managed rolling-action replace synapse-{ENV}-{svc}-mig \
  --zone=europe-west1-b --max-unavailable=0 --max-surge=1 --project=YOUR_PROJECT_ID
```

### Checking VM logs

```bash
# Get instance name
gcloud compute instance-groups managed list-instances synapse-{ENV}-{svc}-mig \
  --zone=europe-west1-b --project=YOUR_PROJECT_ID

# SSH into instance
gcloud compute ssh INSTANCE_NAME --zone=europe-west1-b --project=YOUR_PROJECT_ID

# On the VM:
docker logs mvp-api    # or mvp-ui or mvp-admin
docker ps              # check running containers
```

### Destroying an environment

```bash
bash infra/destroy.sh test   # or prod
```

This removes all environment-specific resources. VPC peering and firewall rules are NOT removed (they are shared). DNS records must be removed manually from Namecheap.

### Re-bootstrapping after destroy

After destroying, simply re-run bootstrap. Bootstrap is idempotent — it skips resources that already exist. For a fresh environment after full destroy, all resources will be created from scratch.

**Remember:** After re-bootstrapping, the CNAME value for the wildcard SSL will be a new value. Update it in Namecheap.

---

## 14. Partial Setup

### If MIGs exist but templates are outdated

See "Updating instance templates" in the Day-to-Day Operations section.

### If SSL certificate is stuck on FAILED_NOT_VISIBLE or CNAME_MISMATCH

Check:
1. Does the CNAME record exist in Namecheap?
2. Is the Host field correct? (e.g., `_acme-challenge` — not `_acme-challenge.synapse-ip.xyz`, Namecheap appends the domain)
3. Is the CNAME value correct? Get the expected value:
   ```bash
   gcloud certificate-manager dns-authorizations describe synapse-{ENV}-dns-auth \
     --project=YOUR_PROJECT_ID
   ```
4. Has DNS propagated? Check with:
   ```bash
   nslookup -type=CNAME _acme-challenge.synapse-ip.xyz 8.8.8.8
   ```

### If backend is UNHEALTHY

```bash
gcloud compute backend-services get-health synapse-{ENV}-{svc}-backend \
  --global --project=YOUR_PROJECT_ID
```

SSH into the VM and check Docker:
```bash
docker ps
docker logs mvp-{svc}
```

Common causes: startup script failed (wrong secret, Docker pull failed, app crash on startup).

### If Cloud Armor is blocking requests (403)

The most common cause is Rule 50 (UI/Admin VM IP allowlist) being outdated after a rolling replace.

Check current VM IPs:
```bash
gcloud compute instance-groups managed list-instances synapse-{ENV}-ui-mig \
  --zone=europe-west1-b --project=YOUR_PROJECT_ID
# Get the instance name, then:
gcloud compute instances describe INSTANCE_NAME --zone=europe-west1-b \
  --format="value(networkInterfaces[0].accessConfigs[0].natIP)"
```

Update Rule 50:
```bash
gcloud compute security-policies rules update 50 \
  --security-policy=synapse-{ENV}-armor \
  --expression="origin.ip == 'UI_IP' || origin.ip == 'ADMIN_IP'" \
  --action=allow --project=YOUR_PROJECT_ID
```

---

## 15. Troubleshooting

### "unexpectedly closed the connection" in browser

Usually a browser cache issue after SSL cert changes.
- Try incognito mode
- Clear browser cache: Ctrl+Shift+Delete

If the issue persists in incognito, check:
1. `gcloud certificate-manager certificates describe synapse-{ENV}-cert` → must be ACTIVE
2. `gcloud compute target-https-proxies describe synapse-{ENV}-https-proxy` → must have `certificateMap` (not `sslCertificates`)
3. `gcloud compute backend-services get-health synapse-{ENV}-ui-backend` → must be HEALTHY

### "synapse-{ENV}-api-mig not found" in CI

Bootstrap has not been run yet, or the environment was destroyed. The CI pipeline can only roll-replace existing MIGs — it cannot create them. Run `bash deploy/bootstrap.sh {ENV}` first.

### `read -rs` prompt exits immediately in PowerShell

PowerShell does not support the `-s` (silent) flag for `read`. Use Git Bash or WSL2 to run bootstrap.

### `gcloud compute ssl-certificates create` wildcard error

Classic SSL certificates do not support wildcard domains. This project uses Google Certificate Manager instead. See `infra/lib/06_loadbalancer.sh` → `create_ssl_and_proxy`.

### AI not working (model not found)

`gemini-2.0-flash` is not available in `europe-west1`. In the Admin panel, go to System → AI Settings and change the model to `gemini-2.5-flash-lite`.

### Survey links point to wrong environment

The `ParticipantDomain` environment variable must be set on the admin container. It is read from the `{ENV}-domain` Secret Manager secret in `infra/startup-admin.sh`. If it is missing, the admin container falls back to the hardcoded `synapse-ip.xyz` in `appsettings.json`.

Check:
```bash
gcloud compute ssh ADMIN_INSTANCE --zone=europe-west1-b --project=YOUR_PROJECT_ID \
  --command='docker inspect mvp-admin | python3 -c "import sys,json; env=[e for e in json.load(sys.stdin)[0][\"Config\"][\"Env\"] if \"Participant\" in e]; print(env)"'
```

If empty, the instance template needs to be updated and the VM replaced.
