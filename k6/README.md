# Load & stress testing with k6 + Grafana Cloud

[k6](https://k6.io) (by Grafana Labs) generates the traffic; **Grafana Cloud k6**
stores and visualises the results. Grafana itself does not generate load.

## What's here

| Script       | Hits                                   | Writes data? | Safe on prod? |
|--------------|----------------------------------------|--------------|---------------|
| `health.js`  | `GET /health` (real DB CanConnect)     | no           | yes           |
| `journey.js` | full participant flow (session → answer → submit) | **yes** | yes — prod is pre-launch; needs `ALLOW_PROD_WRITES=true` |
| `lib/config.js` | env hosts, intensity profiles, SLO thresholds | — | — |

## Environments

| ENV   | API base                          |
|-------|-----------------------------------|
| test  | `https://api.test.synapse-ip.xyz` |
| prod  | `https://api.synapse-ip.xyz`      |

## Intensity profiles (`PROFILE=`)

- `smoke` — 5 VUs, 1 min. Verify wiring first, always.
- `load`  — 50 VUs steady. Expected peak.
- `stress`— ramp to 500 VUs. Find the breaking point. **Test only.**
- `soak`  — 40 VUs for 1 h. Leaks / connection exhaustion.

## Run

```bash
# install (Windows / PowerShell)
winget install k6 --source winget

# 1) local terminal output, results stay on your machine
k6 run -e ENV=prod -e PROFILE=smoke -e PROJECT_ID=1 k6/health.js

# 2) Grafana Cloud — streams to a hosted dashboard
k6 cloud login --token <GRAFANA_CLOUD_K6_TOKEN>   # from Grafana Cloud > k6 > API token

# prod, ramp through the real defenses, read-only
k6 cloud run -e ENV=prod -e PROFILE=load  k6/health.js

# prod, full write journey to breaking point (pre-launch only)
k6 cloud run -e ENV=prod -e PROFILE=stress -e PROJECT_ID=1 -e ALLOW_PROD_WRITES=true k6/journey.js
```

`PROJECT_ID` must be an active project with a published questionnaire (defaults to 1).

## Reading the results — you're testing the whole stack, not just the app

Traffic goes through the load balancer → **Cloud Armor** → **rate limiter** → app,
exactly as a real client would. That's deliberate. It also means:

1. **Rate limiter** (`app.UseRateLimiter()` in `API/Program.cs`) will return `429`
   once a single source exceeds its quota. From Grafana Cloud, load egresses from a
   small set of IPs, so you may hit this early — that `429` rate **is a result**:
   it's the per-source throughput your edge allows.
2. **Cloud Armor** (`synapse-*-armor`) may return `403`/block the k6 IPs under
   sustained volume — also a result (your WAF sheds high-volume single-source load).
3. To find the **app/DB** breaking point rather than the **edge** breaking point,
   you'd need many source IPs (k6 multiple load zones) so no single IP trips the
   limiter — otherwise the number you get is the edge cap, which for a real DDoS-ish
   spike is arguably the more useful one anyway.
4. **Watch Grafana while it runs** (MIG CPU, Cloud SQL connections/CPU). Pre-launch
   there's no one to disrupt, so let it break — note *what* breaks first (edge,
   app CPU, or Postgres connections).

## Cleanup after a write run

`journey.js` leaves participants/responses/submissions in the prod Postgres. Before
launch, wipe them — restore a snapshot, or truncate the affected tables. Don't ship
with k6 test rows in the live DB.
