# Synapse — Technical Deployment Documentation

This document describes the complete GCP infrastructure for the Synapse platform, how every component works, how they interact, and every script involved.

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [GCP Components](#2-gcp-components)
3. [Repository Structure — Infra Scripts](#3-repository-structure--infra-scripts)
4. [Bootstrap Script](#4-bootstrap-script)
5. [Startup Scripts (VM)](#5-startup-scripts-vm)
6. [Load Balancer & SSL](#6-load-balancer--ssl)
7. [Managed Instance Groups (MIGs)](#7-managed-instance-groups-migs)
8. [Cloud SQL (PostgreSQL)](#8-cloud-sql-postgresql)
9. [Secret Manager](#9-secret-manager)
10. [Cloud Armor (WAF)](#10-cloud-armor-waf)
11. [Cloud Storage](#11-cloud-storage)
12. [IAM & Service Accounts](#12-iam--service-accounts)
13. [CI/CD Pipeline](#13-cicd-pipeline)
14. [DNS Configuration](#14-dns-configuration)
15. [Subdomain Routing](#15-subdomain-routing)
16. [Zero Downtime Deployments](#16-zero-downtime-deployments)
17. [Monitoring & Health Checks](#17-monitoring--health-checks)
18. [Destroy & Teardown](#18-destroy--teardown)
19. [Environment Separation](#19-environment-separation)

---

## 1. Architecture Overview

```
Internet
   │
   ▼
[Namecheap DNS]
   ├── synapse-ip.xyz           → 8.233.32.26  (prod LB IP)
   ├── *.synapse-ip.xyz         → 8.233.32.26
   ├── api.synapse-ip.xyz       → 8.233.32.26
   ├── admin.synapse-ip.xyz     → 8.233.32.26
   ├── test.synapse-ip.xyz      → 8.232.142.233 (test LB IP)
   ├── *.test.synapse-ip.xyz    → 8.232.142.233
   ├── api.test.synapse-ip.xyz  → 8.232.142.233
   └── admin.test.synapse-ip.xyz→ 8.232.142.233
   │
   ▼
[Google Global Load Balancer]
   ├── HTTPS :443 → target-https-proxy → certificate-map (wildcard)
   │              → url-map
   │                 ├── api.{DOMAIN}   → api-backend
   │                 ├── admin.{DOMAIN} → admin-backend
   │                 └── *              → ui-backend (default)
   └── HTTP  :80  → target-http-proxy  → http-redirect url-map
                                        → 301 redirect to HTTPS
   │
   ▼
[Cloud Armor WAF]
   ├── Rule 50:   Allow UI + Admin VM IPs (server-side API calls)
   ├── Rule 100-150: Allow EU countries
   ├── Rule 200:  Deny all other geo
   ├── Rule 1000: Block OWASP (SQLi, XSS, LFI, RCE)
   └── Rule 2000: Rate limit 100 req/min per IP, ban 5 min
   │
   ▼
[Backend Services]
   ├── synapse-{ENV}-api-backend   → synapse-{ENV}-api-mig   (port 8080)
   ├── synapse-{ENV}-ui-backend    → synapse-{ENV}-ui-mig    (port 80)
   └── synapse-{ENV}-admin-backend → synapse-{ENV}-admin-mig (port 80)
   │
   ▼
[Managed Instance Groups — GCE VMs in europe-west1-b]
   ├── API VM:   runs Docker container mvp-app   (ASP.NET API on :8080)
   ├── UI VM:    runs Docker container mvp-ui    (ASP.NET MVC on :80)
   └── Admin VM: runs Docker container mvp-admin (ASP.NET MVC on :80)
   │
   ▼
[Cloud SQL — PostgreSQL 15]
   Private IP via VPC peering (10.x.x.x)
   VMs connect via private IP — no Cloud SQL proxy needed

[GCP Secret Manager]
   All secrets prefixed with {ENV}-
   Read by VMs at startup via Metadata Server token

[Google Cloud Storage]
   - {PROJECT_ID}-assets-{ENV}: uploaded files (images, videos)
   - {PROJECT_ID}-backup-{ENV}: Cloud SQL backups
```

---

## 2. GCP Components

### 2.1 Global Load Balancer

A single Global HTTPS Load Balancer handles all traffic for both environments.

| Resource | Name pattern | Purpose |
|---|---|---|
| Static IP | `synapse-{ENV}-lb-ip` | Fixed public IP for DNS A-records |
| URL Map | `synapse-{ENV}-url-map` | Routes by hostname to backend |
| HTTP Redirect Map | `synapse-{ENV}-http-redirect` | 301 redirect HTTP → HTTPS |
| Target HTTPS Proxy | `synapse-{ENV}-https-proxy` | TLS termination, uses cert map |
| Target HTTP Proxy | `synapse-{ENV}-http-proxy` | Handles port 80, redirects |
| Forwarding Rule HTTPS | `synapse-{ENV}-https-rule` | :443 → https-proxy |
| Forwarding Rule HTTP | `synapse-{ENV}-http-rule` | :80 → http-proxy |
| Backend Service API | `synapse-{ENV}-api-backend` | LB → API MIG, port 8080 |
| Backend Service UI | `synapse-{ENV}-ui-backend` | LB → UI MIG, port 80 |
| Backend Service Admin | `synapse-{ENV}-admin-backend` | LB → Admin MIG, port 80 |

**Connection draining:** All backend services have `--connection-draining-timeout=30`. When a VM is removed from rotation, in-flight requests have 30 seconds to complete before the connection is terminated.

### 2.2 SSL — Google Certificate Manager

Classic `gcloud compute ssl-certificates` does **not** support wildcard domains. Certificate Manager with DNS-01 authorization is used instead.

| Resource | Name pattern | Purpose |
|---|---|---|
| DNS Authorization | `synapse-{ENV}-dns-auth` | Proves domain ownership via CNAME |
| Certificate | `synapse-{ENV}-cert` | Covers `{DOMAIN}` and `*.{DOMAIN}` |
| Certificate Map | `synapse-{ENV}-cert-map` | Attached to HTTPS proxy |
| Map Entry apex | `synapse-{ENV}-cert-entry-apex` | `{DOMAIN}` → cert |
| Map Entry wildcard | `synapse-{ENV}-cert-entry-wildcard` | `*.{DOMAIN}` → cert |

**How DNS-01 works:**
1. Google generates a unique CNAME record value for the dns-authorization.
2. You add `_acme-challenge.{DOMAIN}` → `<value>.authorize.certificatemanager.goog.` in your DNS registrar.
3. Google's ACME client verifies the CNAME is reachable and issues the certificate.
4. The certificate covers both the apex domain and all subdomains (`*.synapse-ip.xyz`).

**Important:** The CNAME value is unique per dns-authorization. If you destroy and recreate the dns-authorization, you must update the CNAME in your registrar.

### 2.3 Managed Instance Groups (MIGs)

Each service (api, ui, admin) runs on a separate MIG. Each MIG contains one VM (min 1, max 5) running Container-Optimized OS (COS).

| MIG | Machine type | Image | Port |
|---|---|---|---|
| `synapse-{ENV}-api-mig` | e2-small | mvp-app | 8080 |
| `synapse-{ENV}-ui-mig` | e2-small | mvp-ui | 80 |
| `synapse-{ENV}-admin-mig` | e2-micro | mvp-admin | 80 |

Each MIG has an autoscaler configured at 70% CPU utilization.

**Instance templates** define the VM configuration. They embed the startup script and instance metadata (`env`, `project-id`, `domain`). When a VM boots, it runs the startup script which reads this metadata to know which environment it belongs to.

### 2.4 Cloud SQL

- Engine: PostgreSQL 15
- Instance name: `mvp-postgres-{ENV}`
- Database: `mvp-db`
- User: `mvp-user`
- Connectivity: private IP via VPC peering (`servicenetworking.googleapis.com`)
- VMs connect directly via private IP — no Cloud SQL Auth proxy needed
- The `pgvector` extension is installed by the API on first startup via `ExecuteSqlRaw`

**VPC peering:** A VPC peering connection between the default VPC and Google's `servicenetworking` network allows Cloud SQL to be assigned a private IP accessible from all VMs in the project.

### 2.5 Secret Manager

All secrets are prefixed with `{ENV}-` to keep environments separated within the same GCP project.

| Secret name | Contents |
|---|---|
| `{ENV}-db-password` | PostgreSQL password |
| `{ENV}-db-host` | Cloud SQL private IP |
| `{ENV}-api-key` | Internal API key (UI → API auth) |
| `{ENV}-api-base-url` | `https://api.{DOMAIN}` |
| `{ENV}-resend-api-key` | Resend email service API key |
| `{ENV}-gcs-bucket` | GCS bucket name for uploads |
| `{ENV}-domain` | Base domain (`synapse-ip.xyz` or `test.synapse-ip.xyz`) |

Secrets are read by VMs at startup using the instance's default service account token obtained from the GCE metadata server:

```bash
TOKEN=$(curl -s "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/default/token" \
  -H "Metadata-Flavor: Google" | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])")

get_secret() {
  curl -s "https://secretmanager.googleapis.com/v1/projects/${PROJECT_ID}/secrets/${ENV}-$1/versions/latest:access" \
    -H "Authorization: Bearer $TOKEN" | python3 -c "import sys,json,base64; print(base64.b64decode(json.load(sys.stdin)['payload']['data']).decode())" | tr -d '\r'
}
```

### 2.6 Cloud Armor

Cloud Armor is a WAF (Web Application Firewall) + DDoS protection layer attached to all three backend services.

**Security rules (in priority order):**

| Priority | Rule | Action |
|---|---|---|
| 50 | UI + Admin VM external IPs | Allow — permits server-side calls from UI/Admin to API through the LB |
| 100–150 | EU country codes | Allow — all 27 EU member states |
| 200 | Catch-all | Deny 403 — blocks non-EU traffic |
| 1000 | OWASP Top 10 (SQLi, XSS, LFI, RCE) | Deny 403 |
| 2000 | Rate limiting: 100 req/min per IP | Rate-based ban 5 min, 429 response |

**Rule 50 — why it exists:** The UI and Admin are server-side MVC apps. When they call the API (e.g., to load project data), the request comes from the VM's external IP, passes through the load balancer, and hits Cloud Armor on the API backend. Without rule 50, the geo-blocking rule 200 would block these calls. Rule 50 explicitly allows the VM IPs.

**Rule 50 — updated on every deploy:** Because VM IPs change when instances are replaced (rolling replace creates a new VM), the CI/CD pipeline fetches the new VM IPs after each deploy and updates rule 50.

### 2.7 Cloud Storage

| Bucket | Purpose |
|---|---|
| `{PROJECT_ID}-assets-{ENV}` | User-uploaded files: project images, idea images, audio, video |
| `{PROJECT_ID}-backup-{ENV}` | Automated Cloud SQL database backups |

---

## 3. Repository Structure — Infra Scripts

```
deploy/
  bootstrap.sh          # Main setup script — provisions full environment from scratch
  config.{ENV}.env      # Per-environment config (not committed, gitignored)
  config.example.env    # Template for config files

infra/
  startup-api.sh        # VM startup script for API instances
  startup-ui.sh         # VM startup script for UI instances
  startup-admin.sh      # VM startup script for Admin instances
  destroy.sh            # Tears down all resources for an environment
  setup.sh              # (partial setup helper)
  backup.sh             # Manual backup trigger
  restore.sh            # Restore from backup

  lib/
    01_apis_iam.sh      # Enable GCP APIs + service accounts + IAM roles
    02_secrets.sh       # Create/update/destroy Secret Manager secrets
    03_vpc.sh           # VPC peering + firewall rules
    04_mig.sh           # Instance templates + MIGs + autoscalers
    05_storage.sh       # GCS bucket creation
    06_loadbalancer.sh  # Health checks, backends, URL map, SSL, forwarding rules
    07_cloudsql.sh      # Cloud SQL instance + database + user
    08_logging.sh       # Cloud Logging IAM + uptime checks
    09_armor.sh         # Cloud Armor WAF policies + rules
    10_backup.sh        # Backup bucket + backup schedule
```

---

## 4. Bootstrap Script

`deploy/bootstrap.sh <env>` provisions a complete environment from scratch. It is idempotent — resources that already exist are skipped.

### Execution order

```
1. enable_apis                              # Must be first — all other steps need APIs active
2. Parallel:
   - create_service_account + assign_iam_roles
   - create_secrets
   - create_storage_bucket + create_backup_bucket
   - create_health_checks
   - create_instance_templates
   - create_vpc_peering + create_firewall_rules + create_cloud_sql
3. Wait for all parallel jobs
4. update_secret db-host                    # Set real Cloud SQL private IP
5. create_migs
6. create_backend_services
7. create_url_map
8. create_ssl_and_proxy                     # Creates Certificate Manager cert + prints CNAME
9. reserve_static_ip
10. create_forwarding_rules
11. create_cloud_armor
12. add_mig_ips_to_armor
13. setup_logging_iam
14. create_uptime_checks
```

### Config file

`deploy/config.{ENV}.env` must exist before bootstrap runs. It contains:

```bash
PROJECT_ID="synapse-ip-j2"
PROJECT_NUMBER=""
REGION="europe-west1"
ZONE="europe-west1-b"
DOMAIN="synapse-ip.xyz"           # prod: apex domain, test: test.synapse-ip.xyz
DB_PASSWORD=REDACTED password>"
API_KEY=REDACTED key>"
RESEND_API_KEY=REDACTED"
GCS_BUCKET="synapse-ip-j2-assets-prod"
VERTEX_AI_LOCATION="europe-west1"
```

If the file does not exist, bootstrap enters interactive mode and asks for all values.

### Why bootstrap is not run by CI

Bootstrap creates infrastructure (MIGs, LB, SQL, etc.). This only needs to happen once. CI only does rolling replaces on existing MIGs. If MIGs do not exist, the CI deploy job will fail — bootstrap must be run manually first.

---

## 5. Startup Scripts (VM)

When a GCE VM starts, it runs the startup script embedded in its instance template. All three scripts follow the same pattern:

1. **Get an access token** from the GCE metadata server (the VM's service account).
2. **Read `ENV` and `PROJECT_ID`** from instance metadata (set during `create_instance_templates`).
3. **Determine image tag**: `latest` for prod, `{ENV}-latest` for other environments.
4. **Define `get_secret()`** — fetches a secret from Secret Manager using the access token.
5. **Docker login** to GCR using the access token.
6. **Pull the Docker image**.
7. **Stop and remove** the old container (if any).
8. **Start the new container** with all environment variables from Secret Manager.

### startup-api.sh

Runs `mvp-app` (the .NET API) on port 8080.

Key environment variables passed:
- `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` — Cloud SQL connection
- `ApiKey` — validates requests from UI/Admin
- `RESEND_API_KEY` — email sending
- `GCS_BUCKET` — file uploads

### startup-ui.sh

Runs `mvp-ui` (the user-facing MVC app) on port 80→8080.

Key environment variables passed:
- `ApiBaseUrl` — URL of the API (`https://api.{DOMAIN}`)
- `ApiKey` — authenticates requests to the API
- `Subdomains__BaseDomain` — base domain for subdomain routing middleware (read from `{ENV}-domain` secret)
- `GCS_BUCKET` — for serving uploaded files

### startup-admin.sh

Runs `mvp-admin` (the admin panel) on port 80→8080.

Key environment variables passed:
- `ApiBaseUrl`, `ApiKey` — API connection
- `DB_*` — direct database access for admin-specific tables (widget preferences, admin auth)
- `RESEND_API_KEY` — sending invite emails
- `ParticipantDomain` — base domain for building survey URLs and QR codes (read from `{ENV}-domain` secret)

### Why `ParticipantDomain` matters

The Admin panel builds survey URLs like `https://{shortForm}.{ParticipantDomain}`. Without this env var, it defaults to the hardcoded value in `appsettings.json` (`synapse-ip.xyz`), which means the test environment would generate links pointing to production.

---

## 6. Load Balancer & SSL

### URL routing

```
Host: api.{DOMAIN}   → path-matcher "api-matcher"   → synapse-{ENV}-api-backend
Host: admin.{DOMAIN} → path-matcher "admin-matcher" → synapse-{ENV}-admin-backend
Host: *              → default service              → synapse-{ENV}-ui-backend
```

All subdomain traffic (e.g., `mentaal-welzijn.synapse-ip.xyz`) hits the UI backend because there is no explicit host rule for it — it falls through to the default.

### HTTP → HTTPS redirect

A separate URL map (`synapse-{ENV}-http-redirect`) with `defaultUrlRedirect.httpsRedirect: true` handles all port 80 traffic and issues a `301 MOVED_PERMANENTLY` to HTTPS.

### Certificate Manager — detailed flow

```
bootstrap runs create_ssl_and_proxy:
  1. Create dns-authorization for {DOMAIN}
     → Google returns a CNAME record: _acme-challenge.{DOMAIN} → <uuid>.authorize.certificatemanager.goog.
  2. Bootstrap prints this CNAME → you add it in your DNS registrar
  3. Create certificate for {DOMAIN} and *.{DOMAIN} using the dns-authorization
     → Status: AUTHORIZING (waiting for CNAME to propagate)
  4. Create cert-map + entries (apex + wildcard)
  5. Create HTTPS proxy with --certificate-map (NOT --ssl-certificates)
     → The proxy now serves the wildcard cert once it becomes ACTIVE

DNS propagates → Google verifies CNAME → Certificate status: ACTIVE
```

**Key difference from classic SSL certificates:**
- `gcloud compute ssl-certificates` supports only exact domains, no wildcards.
- `gcloud certificate-manager certificates` supports wildcards via DNS-01 authorization.
- The HTTPS proxy must use `--certificate-map`, not `--ssl-certificates` — they are mutually exclusive.

---

## 7. Managed Instance Groups (MIGs)

### Instance templates

Instance templates are created by `create_instance_templates` in `04_mig.sh`. Each template:
- Uses `cos-stable` (Container-Optimized OS) — no Docker installation needed
- Has `--scopes=cloud-platform` — grants the VM access to all GCP APIs using its service account
- Has `--tags=allow-health-check` — matches the firewall rule that allows health check traffic from GCP's health checker IP ranges
- Embeds the startup script via `--metadata-from-file`
- Sets instance metadata: `env`, `project-id`, `domain` — read by the startup script at runtime

### Updating templates

`_create_template` skips creation if the template already exists (`2>/dev/null || echo "al aanwezig"`). To update a template with a changed startup script:

```bash
# Delete old template
gcloud compute instance-templates delete synapse-{ENV}-{svc}-template --quiet --project={PROJECT_ID}

# Recreate with new startup script (from repo root)
gcloud compute instance-templates create synapse-{ENV}-{svc}-template \
  --machine-type=e2-small \
  --image-family=cos-stable --image-project=cos-cloud \
  --tags=allow-health-check --scopes=cloud-platform \
  --metadata="env={ENV},project-id={PROJECT_ID},domain={DOMAIN}" \
  --metadata-from-file="startup-script=infra/startup-{svc}.sh" \
  --project={PROJECT_ID}

# Point MIG to new template
gcloud compute instance-groups managed set-instance-template synapse-{ENV}-{svc}-mig \
  --template=synapse-{ENV}-{svc}-template \
  --zone={ZONE} --project={PROJECT_ID}

# Rolling replace
gcloud compute instance-groups managed rolling-action replace synapse-{ENV}-{svc}-mig \
  --zone={ZONE} --max-unavailable=0 --max-surge=1 --project={PROJECT_ID}
```

---

## 8. Cloud SQL (PostgreSQL)

- **Instance:** `mvp-postgres-{ENV}`, PostgreSQL 15, `db-f1-micro`
- **Private IP:** allocated from the VPC peering range
- **Database:** `mvp-db`, owner `mvp-user`
- **pgvector:** installed by the API at startup via `ExecuteSqlRaw("CREATE EXTENSION IF NOT EXISTS vector")`

**VPC peering setup** (`03_vpc.sh`):
```bash
gcloud compute addresses create google-managed-services-default \
  --global --purpose=VPC_PEERING --prefix-length=16 \
  --network=default --project={PROJECT_ID}

gcloud services vpc-peerings connect \
  --service=servicenetworking.googleapis.com \
  --ranges=google-managed-services-default \
  --network=default --project={PROJECT_ID}
```

After `create_cloud_sql`, bootstrap fetches the private IP and stores it in the `{ENV}-db-host` secret:
```bash
DB_HOST=$(gcloud sql instances describe "mvp-postgres-${ENV}" \
  --format="value(ipAddresses[0].ipAddress)")
update_secret "db-host" "${DB_HOST}"
```

---

## 9. Secret Manager

### Creating secrets

`_create_secret` in `02_secrets.sh` always creates a new version (it does not check if the value changed). This means running bootstrap twice for the same environment will overwrite secrets with the same values.

**If a secret already has a correct value in Secret Manager**, bootstrap will overwrite it. This is intentional — bootstrap is the source of truth for initial values. Update secrets manually after bootstrap if needed:

```bash
gcloud secrets versions add {ENV}-resend-api-key \
  --data-file=- --project={PROJECT_ID} <<< "re_actual_value"
```

### Reading secrets at runtime

VMs read secrets using a `get_secret` bash function that calls the Secret Manager REST API with the VM's access token. The token is obtained from the GCE metadata server and is automatically refreshed by GCP.

---

## 10. Cloud Armor (WAF)

### Rule priority system

Lower number = higher priority. Rules are evaluated top to bottom, first match wins.

```
50   → Allow UI + Admin VM IPs         (server-side API calls bypass geo-block)
100  → Allow BE, NL, LU, DE, FR
110  → Allow IT, ES, PT, AT, SE
120  → Allow FI, DK, IE, PL, CZ
130  → Allow SK, HU, RO, BG, HR
140  → Allow SI, EE, LV, LT, GR
150  → Allow CY, MT
200  → Deny all (catch-all geo-block)
1000 → Block OWASP (SQLi, XSS, LFI, RCE)  [evaluated after allow rules]
2000 → Rate limit 100 req/min per IP
```

### Why rule 50 must be updated on every deploy

UI and Admin VMs make server-side HTTP calls to the API (`ApiBaseUrl = https://api.{DOMAIN}`). These calls pass through the load balancer and hit Cloud Armor. The geo-block (rule 200) would reject them unless their external IP is explicitly allowed.

Since rolling replaces assign new external IPs to new VMs, the CI/CD `deploy-test` job fetches the new IPs after each deploy and updates rule 50:

```bash
UI_IP=$(gcloud compute instances describe "${UI_INSTANCE}" ...)
ADMIN_IP=$(gcloud compute instances describe "${ADMIN_INSTANCE}" ...)
gcloud compute security-policies rules update 50 \
  --security-policy="synapse-test-armor" \
  --expression="origin.ip == '${UI_IP}' || origin.ip == '${ADMIN_IP}'" \
  --action=allow
```

---

## 11. Cloud Storage

Two buckets per environment:

**Assets bucket** (`{PROJECT_ID}-assets-{ENV}`):
- Stores user uploads: project cover images, idea images, audio files, video files
- Accessed by the API container via the `GCS_BUCKET` environment variable
- Service account has `roles/storage.admin`

**Backup bucket** (`{PROJECT_ID}-backup-{ENV}`):
- Stores exported Cloud SQL backups (pg_dump format)
- Created by `create_backup_bucket` in `05_storage.sh`

---

## 12. IAM & Service Accounts

### CI/CD Service Account

`synapse-cicd@{PROJECT_ID}.iam.gserviceaccount.com`

Roles:
- `roles/compute.instanceAdmin.v1` — manage VMs/MIGs
- `roles/storage.admin` — push Docker images, manage GCS
- `roles/secretmanager.secretAccessor` — read secrets during deploy
- `roles/artifactregistry.writer` — push images
- `roles/iam.serviceAccountUser` — impersonate service accounts

### Workload Identity Federation (WIF)

The CI/CD pipeline authenticates with GCP using **Workload Identity Federation** — no long-lived service account keys are stored in GitLab. Instead:

1. GitLab generates an OIDC token (`GCP_ID_TOKEN`) for the pipeline job.
2. The pipeline exchanges this token for a short-lived GCP access token via a WIF pool.
3. Authentication uses `gcloud auth login --cred-file=gcp-wif-{ENV}.json`.

The WIF pool/provider is pre-configured in GCP and referenced by the `aud` field in the GitLab CI id_tokens configuration.

### VM Service Account

VMs use the default Compute Engine service account with `--scopes=cloud-platform`. This allows them to access Secret Manager, GCR, and other GCP services.

---

## 13. CI/CD Pipeline

### Stages

```
build → scan → push → sign → deploy
```

### Jobs

| Job | Trigger | Purpose |
|---|---|---|
| `build-api` | development, main | Build API Docker image |
| `build-ui` | development, main | Build UI Docker image |
| `build-admin` | development, main | Build Admin Docker image |
| `build-nginx` | development | Build Nginx image (test only) |
| `scan-api/ui/admin/nginx` | development, main | Trivy vulnerability scan + SBOM |
| `push-test` | development | Push images tagged `{SHA}` + `test-latest` |
| `push-prod` | main | Push images tagged `{SHA}` + `latest` |
| `sign-prod` | main | Cosign image signing for supply chain security |
| `deploy-test` | development (auto) | Rolling replace of test MIGs + Cloud Armor IP update |
| `deploy-production` | main (manual) | Rolling replace of prod MIGs |

### Image tagging strategy

| Environment | Tag | Updated when |
|---|---|---|
| Test | `test-latest` | Every merge to `development` |
| Prod | `latest` | Every merge to `main` |

VMs always pull the environment's `latest` tag at startup. This means:
- A new test VM always gets the latest development build.
- A new prod VM always gets the latest main build.

### Zero downtime rolling replace

```yaml
gcloud compute instance-groups managed rolling-action replace synapse-{ENV}-{svc}-mig \
  --zone="${ZONE}" --max-unavailable=0 --max-surge=1
```

- `--max-surge=1`: creates a new VM before removing the old one
- `--max-unavailable=0`: never has fewer healthy instances than the target size

The load balancer only routes to the new instance once its health check passes. Connection draining (30s) ensures in-flight requests complete before the old instance is terminated.

---

## 14. DNS Configuration

All DNS records are managed in **Namecheap** for `synapse-ip.xyz`.

### Required A-records

| Host | Value | Purpose |
|---|---|---|
| `@` | prod LB IP | `synapse-ip.xyz` |
| `*` | prod LB IP | All project subdomains (`mentaal-welzijn.synapse-ip.xyz`) |
| `api` | prod LB IP | `api.synapse-ip.xyz` |
| `admin` | prod LB IP | `admin.synapse-ip.xyz` |
| `test` | test LB IP | `test.synapse-ip.xyz` |
| `*.test` | test LB IP | All test project subdomains |
| `api.test` | test LB IP | `api.test.synapse-ip.xyz` |
| `admin.test` | test LB IP | `admin.test.synapse-ip.xyz` |

### Required CNAME-records (for SSL)

| Host | Value | Purpose |
|---|---|---|
| `_acme-challenge` | `{uuid}.14.authorize.certificatemanager.goog.` | Prod wildcard SSL DNS-01 |
| `_acme-challenge.test` | `{uuid}.19.authorize.certificatemanager.goog.` | Test wildcard SSL DNS-01 |

The exact CNAME values are generated by Google and printed by bootstrap. They must match the current dns-authorization resources. If you destroy and recreate a dns-authorization, the CNAME value changes.

---

## 15. Subdomain Routing

Project short forms (e.g., `mentaal-welzijn`) are exposed as subdomains of the base domain. This allows survey participants to access `https://mentaal-welzijn.synapse-ip.xyz` instead of `https://synapse-ip.xyz/1`.

### How it works

`ProjectShortFormResolverMiddleware` in `UI-User` intercepts every request:

1. Extracts the subdomain from the `Host` header by comparing against `Subdomains:BaseDomain` config.
2. Checks if the subdomain is reserved (`admin`, `api`, `www`, `app`, etc.).
3. If not reserved, looks up the project by short form via the API (with 60s in-memory cache).
4. If the project is `Active`, rewrites the request path to `/{projectId}/...`.
5. If not found or not active, redirects to an appropriate error page.

**Config value:** `Subdomains__BaseDomain` is set via environment variable from `{ENV}-domain` secret, so test uses `test.synapse-ip.xyz` and prod uses `synapse-ip.xyz`.

### Survey URL generation

The Admin panel builds survey URLs in `ProjectController.cs`:
```csharp
SurveyUrl = $"{scheme}://{project.ShortForm}.{participantDomain}"
```

`participantDomain` is read from `AdminSettings.ParticipantDomain`, which is overridden at runtime by the `ParticipantDomain` environment variable (set from `{ENV}-domain` secret in `startup-admin.sh`).

---

## 16. Zero Downtime Deployments

### Problem

With `--max-unavailable=1 --max-surge=0` (old config), the MIG removes the old VM first, then creates a new one. During this window there are zero healthy instances — the load balancer returns 502 errors.

### Solution

With `--max-unavailable=0 --max-surge=1` (current config):

```
Time 0:   Old VM running, serving traffic
Time 30s: New VM starts (surge), pulls Docker image, starts container
Time 60s: Health check passes on new VM (2 × 10s intervals)
Time 60s: Load balancer adds new VM to rotation
Time 60s: Old VM enters connection draining (30s window)
Time 90s: Old VM removed, all traffic on new VM
```

No traffic interruption at any point.

### Health check configuration

```
interval:            10s
timeout:             5s
healthy threshold:   2 consecutive successes (20s to become healthy)
unhealthy threshold: 3 consecutive failures
```

Health check endpoints:
- API: `GET /health` on port 8080
- UI: `GET /health` on port 80
- Admin: `GET /health` on port 80

---

## 17. Monitoring & Health Checks

**GCP health checks** (`synapse-{ENV}-{svc}-health`) are used by the load balancer to determine which instances receive traffic. An instance only enters rotation when the health check passes.

**Uptime checks** are created by `create_uptime_checks` in `08_logging.sh` and can be configured to alert on downtime.

**Cloud Logging** — the service account has `roles/logging.logWriter` assigned.

---

## 18. Destroy & Teardown

`infra/destroy.sh <env>` removes all environment-specific resources. It does **not** remove VPC peering or firewall rules (project-wide, shared between environments).

Destruction order:
1. Cloud Armor (unlink from backends first, then delete policy)
2. Uptime checks
3. Load balancer resources (forwarding rules → proxies → SSL → URL maps → backends → health checks → IP)
4. Certificate Manager resources (map entries → map → cert → dns-auth)
5. Parallel: MIGs + templates, Cloud SQL, GCS buckets, Secrets

---

## 19. Environment Separation

Both `test` and `prod` environments live in the **same GCP project** (`synapse-ip-j2`). They are separated by resource name prefixes (`synapse-test-*` vs `synapse-prod-*`) and secret name prefixes (`test-*` vs `prod-*`).

Shared (project-wide):
- Enabled APIs
- VPC network and peering
- Firewall rules
- GCR image registry
- WIF pool and provider

Per-environment (fully isolated):
- Load balancer (separate IPs, proxies, URL maps)
- MIGs and instance templates
- Cloud SQL instance
- Secret Manager secrets
- Cloud Armor policy
- GCS buckets
- Certificate Manager resources

**CI/CD environment separation:**
- `development` branch → auto-deploys to test (uses `gcp-wif-test.json`)
- `main` branch → manual deploy to prod (uses `gcp-wif-prod.json`)
