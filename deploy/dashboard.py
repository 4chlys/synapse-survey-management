#!/usr/bin/env python3
"""
Synapse – Web Dashboard  (GCP Console stijl)
Start: python deploy/dashboard.py
Open:  http://localhost:8080
"""

import concurrent.futures
import http.server
import json
import subprocess
import sys
import threading
import webbrowser
from pathlib import Path

SCRIPT_DIR = Path(__file__).parent
CONFIG_FILE = SCRIPT_DIR / "config.env"
PORT = 8080

# ── Config ─────────────────────────────────────────────────────
config = {}
if CONFIG_FILE.exists():
    for line in CONFIG_FILE.read_text().splitlines():
        line = line.strip()
        if "=" in line and not line.startswith("#"):
            k, _, v = line.partition("=")
            config[k.strip()] = v.strip().strip('"')

PROJECT_ID = config.get("PROJECT_ID", "")
REGION     = config.get("REGION", "europe-west1")
ZONE       = config.get("ZONE", REGION + "-b") if "REGION" in config else config.get("ZONE", "europe-west1-b")
DOMAIN     = config.get("DOMAIN", "synapse-ip.xyz")

MIGS = [
    {"id": "synapse-api-mig",   "label": "API",   "url": f"https://api.{DOMAIN}",   "port": "8080"},
    {"id": "synapse-ui-mig",    "label": "UI",    "url": f"https://{DOMAIN}",        "port": "80"},
    {"id": "synapse-admin-mig", "label": "Admin", "url": f"https://admin.{DOMAIN}", "port": "8081"},
]

# ── GCloud helpers ─────────────────────────────────────────────
# On Windows, gcloud is installed as gcloud.cmd — subprocess needs the full name
_GCLOUD = "gcloud.cmd" if sys.platform == "win32" else "gcloud"

def gcloud(*args, timeout=20):
    try:
        r = subprocess.run(
            [_GCLOUD, *args],
            capture_output=True, text=True, timeout=timeout,
            shell=False
        )
        return r.stdout.strip(), r.returncode
    except subprocess.TimeoutExpired:
        return "", 1
    except FileNotFoundError:
        # fallback: try with shell=True (e.g. custom PATH setups)
        try:
            r = subprocess.run(
                "gcloud " + " ".join(str(a) for a in args),
                capture_output=True, text=True, timeout=timeout,
                shell=True
            )
            return r.stdout.strip(), r.returncode
        except Exception:
            return "", 1

def mig_size(mig_id):
    out, code = gcloud(
        "compute", "instance-groups", "managed", "describe", mig_id,
        f"--zone={ZONE}", f"--project={PROJECT_ID}", "--format=value(targetSize)"
    )
    try:
        return int(out) if code == 0 else -1
    except ValueError:
        return -1

def set_mig_size(mig_id, size):
    _, code = gcloud(
        "compute", "instance-groups", "managed", "resize", mig_id,
        f"--size={size}", f"--zone={ZONE}", f"--project={PROJECT_ID}", "--quiet",
        timeout=30
    )
    return code == 0

def get_mig_instances(mig_id):
    out, code = gcloud(
        "compute", "instance-groups", "managed", "list-instances", mig_id,
        f"--zone={ZONE}", f"--project={PROJECT_ID}",
        "--format=value(instance,instanceStatus,currentAction)"
    )
    if code != 0 or not out:
        return []
    instances = []
    for line in out.splitlines():
        parts = line.split()
        if parts:
            instances.append({
                "name": parts[0].split("/")[-1],
                "status": parts[1] if len(parts) > 1 else "?",
                "action": parts[2] if len(parts) > 2 else "NONE",
            })
    return instances

def get_lb_ip():
    out, code = gcloud(
        "compute", "addresses", "describe", "synapse-lb-ip",
        "--global", f"--project={PROJECT_ID}", "--format=value(address)"
    )
    return out if (code == 0 and out) else None

def get_ssl_certs():
    out, code = gcloud(
        "compute", "ssl-certificates", "list",
        f"--project={PROJECT_ID}", "--format=value(name,managed.status)"
    )
    if code != 0 or not out:
        return []
    results = []
    for line in out.splitlines():
        parts = line.split()
        if len(parts) >= 2:
            results.append({"name": parts[0], "status": parts[1]})
    return results

def get_cloud_sql_status():
    out, code = gcloud(
        "sql", "instances", "describe", "mvp-postgres",
        f"--project={PROJECT_ID}", "--format=value(state,ipAddresses[0].ipAddress)"
    )
    if code != 0 or not out:
        return {"state": "UNKNOWN", "ip": ""}
    parts = out.split()
    return {"state": parts[0], "ip": parts[1] if len(parts) > 1 else ""}

def get_recent_logs(limit=50):
    out, code = gcloud(
        "logging", "read",
        'resource.type="gce_instance"',
        f"--limit={limit}", f"--project={PROJECT_ID}",
        "--format=json", "--freshness=6h",
        timeout=30
    )
    if code != 0 or not out:
        return ["Geen logs gevonden."]
    try:
        entries = json.loads(out)
    except json.JSONDecodeError:
        return [out[:3000]]
    lines = []
    for e in entries:
        ts  = e.get("timestamp", "")[:19].replace("T", " ")
        msg = (e.get("jsonPayload") or {}).get("message") \
           or e.get("textPayload") \
           or str((e.get("protoPayload") or {}).get("methodName", ""))
        if msg and msg.strip():
            lines.append(f"{ts}  {msg.strip()}")
    return lines if lines else ["Geen logberichten gevonden."]

# ── VM monitoring ─────────────────────────────────────────────
def get_vm_details():
    out, code = gcloud(
        "compute", "instances", "list",
        f"--project={PROJECT_ID}",
        "--format=json(name,status,machineType,zone,networkInterfaces[0].networkIP,creationTimestamp)",
        timeout=20
    )
    if code != 0 or not out:
        return []
    try:
        vms = json.loads(out)
    except json.JSONDecodeError:
        return []
    results = []
    for vm in vms:
        machine = vm.get("machineType", "").split("/")[-1]
        zone    = vm.get("zone", "").split("/")[-1]
        ip      = (vm.get("networkInterfaces") or [{}])[0].get("networkIP", "")
        results.append({
            "name":    vm.get("name", ""),
            "status":  vm.get("status", ""),
            "machine": machine,
            "zone":    zone,
            "ip":      ip,
        })
    return results

def get_cpu_metrics():
    """Returns latest CPU utilization per instance (0-100%)."""
    import datetime
    now   = datetime.datetime.utcnow()
    start = (now - datetime.timedelta(minutes=5)).strftime("%Y-%m-%dT%H:%M:%SZ")
    end   = now.strftime("%Y-%m-%dT%H:%M:%SZ")
    out, code = gcloud(
        "monitoring", "time-series", "list",
        f"--project={PROJECT_ID}",
        '--filter=metric.type="compute.googleapis.com/instance/cpu/utilization"',
        f"--start-time={start}",
        f"--end-time={end}",
        "--format=json",
        timeout=20
    )
    if code != 0 or not out:
        return {}
    try:
        series = json.loads(out)
    except json.JSONDecodeError:
        return {}
    cpu = {}
    for s in series:
        name = s.get("resource", {}).get("labels", {}).get("instance_id", "")
        points = s.get("points", [])
        if points:
            val = points[0].get("value", {}).get("doubleValue", 0)
            cpu[name] = round(val * 100, 1)
    return cpu

# ── Cost estimate ─────────────────────────────────────────────
# europe-west1 on-demand prices (USD/month, approximate)
_COSTS = {
    "e2-small":    13.14,
    "e2-micro":     6.11,
    "db-f1-micro": 10.08,   # Cloud SQL
    "lb":          18.26,   # Global LB (forwarding rules + min data)
    "storage":      0.23,   # ~10 GB Cloud Storage
    "armor":        5.00,   # Cloud Armor base
}

def estimate_cost(mig_sizes):
    """Returns estimated monthly cost based on running VMs."""
    # API + UI = e2-small, Admin = e2-micro
    api_count   = mig_sizes.get("synapse-api-mig", 0)
    ui_count    = mig_sizes.get("synapse-ui-mig", 0)
    admin_count = mig_sizes.get("synapse-admin-mig", 0)

    vm_cost = (
        max(api_count, 0)   * _COSTS["e2-small"] +
        max(ui_count, 0)    * _COSTS["e2-small"] +
        max(admin_count, 0) * _COSTS["e2-micro"]
    )

    total = vm_cost + _COSTS["db-f1-micro"] + _COSTS["lb"] + _COSTS["storage"] + _COSTS["armor"]
    per_day = total / 30

    return {
        "vms":     round(vm_cost, 2),
        "sql":     _COSTS["db-f1-micro"],
        "lb":      _COSTS["lb"],
        "storage": _COSTS["storage"],
        "armor":   _COSTS["armor"],
        "total":   round(total, 2),
        "per_day": round(per_day, 2),
        "budgets": [10, 25, 40],
        "billing_url": f"https://console.cloud.google.com/billing?project={PROJECT_ID}",
    }

# ── Parallel status fetch ──────────────────────────────────────
def fetch_all_status():
    with concurrent.futures.ThreadPoolExecutor(max_workers=11) as ex:
        mig_futures      = {m["id"]: ex.submit(mig_size, m["id"]) for m in MIGS}
        instance_futures = {m["id"]: ex.submit(get_mig_instances, m["id"]) for m in MIGS}
        lb_future        = ex.submit(get_lb_ip)
        ssl_future       = ex.submit(get_ssl_certs)
        sql_future       = ex.submit(get_cloud_sql_status)
        vm_future        = ex.submit(get_vm_details)
        cpu_future       = ex.submit(get_cpu_metrics)

    lb_ip = lb_future.result()
    sql   = sql_future.result()
    domain = DOMAIN or "synapse-ip.xyz"
    mig_sizes = {mid: f.result() for mid, f in mig_futures.items()}

    return {
        "migs":      mig_sizes,
        "instances": {mid: f.result() for mid, f in instance_futures.items()},
        "lb_ip": lb_ip or "",
        "lb_ip_found": lb_ip is not None,
        "ui_url":    f"https://{domain}",
        "api_url":   f"https://api.{domain}",
        "admin_url": f"https://admin.{domain}",
        "console_url": f"https://console.cloud.google.com/compute/instanceGroups?project={PROJECT_ID}",
        "logs_url":    f"https://console.cloud.google.com/logs/query?project={PROJECT_ID}",
        "sql_console": f"https://console.cloud.google.com/sql/instances/mvp-postgres/overview?project={PROJECT_ID}",
        "ssl": ssl_future.result(),
        "sql": sql,
        "project_id": PROJECT_ID,
        "region": REGION,
        "zone": ZONE,
        "domain": domain,
        "cost": estimate_cost(mig_sizes),
        "vms":  vm_future.result(),
        "cpu":  cpu_future.result(),
    }

# ── HTML ───────────────────────────────────────────────────────
HTML = r"""<!DOCTYPE html>
<html lang="nl">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Synapse – Cloud Console</title>
<link href="https://fonts.googleapis.com/css2?family=Google+Sans:wght@400;500;700&family=Roboto:wght@400;500&display=swap" rel="stylesheet">
<style>
* { box-sizing: border-box; margin: 0; padding: 0; }
:root {
  --blue:    #1a73e8;
  --blue-dk: #1557b0;
  --green:   #1e8e3e;
  --red:     #d93025;
  --yellow:  #f9ab00;
  --gray1:   #202124;
  --gray2:   #3c4043;
  --gray3:   #5f6368;
  --gray4:   #80868b;
  --gray5:   #dadce0;
  --gray6:   #f1f3f4;
  --gray7:   #f8f9fa;
  --white:   #ffffff;
}
body { font-family: 'Roboto', sans-serif; background: var(--gray7); color: var(--gray1); font-size: 14px; }

/* ── Top bar ── */
.topbar {
  height: 64px; background: var(--white); border-bottom: 1px solid var(--gray5);
  display: flex; align-items: center; padding: 0 16px; gap: 8px;
  position: sticky; top: 0; z-index: 100; box-shadow: 0 1px 2px rgba(0,0,0,.1);
}
.topbar-logo { display: flex; align-items: center; gap: 8px; text-decoration: none; }
.gcp-logo { width: 30px; height: 30px; }
.topbar-title { font-family: 'Google Sans', sans-serif; font-size: 18px; color: var(--gray2); font-weight: 400; }
.topbar-sep { width: 1px; height: 24px; background: var(--gray5); margin: 0 8px; }
.project-select {
  display: flex; align-items: center; gap: 6px;
  border: 1px solid var(--gray5); border-radius: 4px;
  padding: 6px 12px; cursor: pointer; background: var(--white);
  font-size: 14px; color: var(--gray2); transition: border-color .15s;
}
.project-select:hover { border-color: var(--blue); }
.project-select .proj-icon { font-size: 16px; }
.project-label { font-size: 12px; color: var(--gray3); line-height: 1.2; }
.project-id    { font-size: 13px; font-weight: 500; color: var(--gray1); }
.topbar-actions { margin-left: auto; display: flex; gap: 8px; align-items: center; }
.icon-btn { background: none; border: none; cursor: pointer; padding: 8px; border-radius: 50%; color: var(--gray3); font-size: 18px; transition: background .15s; display: flex; align-items: center; }
.icon-btn:hover { background: var(--gray6); color: var(--gray1); }

/* ── Layout ── */
.layout { display: flex; min-height: calc(100vh - 64px); }

/* ── Sidebar ── */
.sidebar {
  width: 256px; background: var(--white); border-right: 1px solid var(--gray5);
  padding: 8px 0; flex-shrink: 0;
}
.sidebar-section { padding: 8px 0; }
.sidebar-header { padding: 8px 16px 4px; font-size: 11px; font-weight: 500; color: var(--gray3); text-transform: uppercase; letter-spacing: .08em; }
.nav-item {
  display: flex; align-items: center; gap: 16px;
  padding: 0 16px; height: 40px; cursor: pointer;
  color: var(--gray2); border-left: 3px solid transparent;
  transition: background .15s; text-decoration: none; font-size: 13px;
}
.nav-item:hover  { background: var(--gray6); }
.nav-item.active { background: #e8f0fe; color: var(--blue); border-left-color: var(--blue); font-weight: 500; }
.nav-icon { font-size: 16px; width: 20px; text-align: center; }
.sidebar-divider { height: 1px; background: var(--gray5); margin: 4px 0; }

/* ── Main ── */
.main { flex: 1; padding: 24px; overflow-x: hidden; }
.page-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px; }
.page-title  { font-family: 'Google Sans', sans-serif; font-size: 22px; font-weight: 400; color: var(--gray1); }
.breadcrumb  { font-size: 12px; color: var(--gray3); margin-bottom: 4px; }
.header-actions { display: flex; gap: 8px; }

/* ── Buttons ── */
.btn-primary {
  background: var(--blue); color: var(--white); border: none;
  border-radius: 4px; padding: 0 16px; height: 36px; font-size: 14px; font-weight: 500;
  cursor: pointer; display: inline-flex; align-items: center; gap: 6px;
  transition: background .15s, box-shadow .15s; font-family: 'Google Sans', sans-serif;
}
.btn-primary:hover { background: var(--blue-dk); box-shadow: 0 1px 3px rgba(0,0,0,.3); }
.btn-primary:disabled { background: var(--gray5); color: var(--gray4); cursor: not-allowed; }
.btn-secondary {
  background: var(--white); color: var(--blue); border: 1px solid var(--gray5);
  border-radius: 4px; padding: 0 16px; height: 36px; font-size: 14px; font-weight: 500;
  cursor: pointer; display: inline-flex; align-items: center; gap: 6px;
  transition: background .15s, border-color .15s; font-family: 'Google Sans', sans-serif;
}
.btn-secondary:hover { background: #e8f0fe; border-color: var(--blue); }
.btn-danger {
  background: var(--white); color: var(--red); border: 1px solid var(--gray5);
  border-radius: 4px; padding: 0 16px; height: 36px; font-size: 14px; font-weight: 500;
  cursor: pointer; display: inline-flex; align-items: center; gap: 6px;
  transition: background .15s; font-family: 'Google Sans', sans-serif;
}
.btn-danger:hover { background: #fce8e6; border-color: var(--red); }
.btn-sm { height: 28px; padding: 0 12px; font-size: 12px; }

/* ── Summary cards ── */
.summary-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 16px; margin-bottom: 24px; }
.summary-card {
  background: var(--white); border: 1px solid var(--gray5); border-radius: 8px;
  padding: 20px; cursor: default;
  transition: box-shadow .2s;
}
.summary-card:hover { box-shadow: 0 1px 6px rgba(32,33,36,.28); }
.summary-card-top { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 12px; }
.summary-card-label { font-size: 12px; color: var(--gray3); font-weight: 500; }
.summary-card-icon { font-size: 20px; }
.summary-card-value { font-family: 'Google Sans', sans-serif; font-size: 28px; font-weight: 400; color: var(--gray1); margin-bottom: 4px; }
.summary-card-sub { font-size: 12px; color: var(--gray3); }
.summary-card-status { display: flex; align-items: center; gap: 6px; margin-top: 8px; }

/* ── Section card ── */
.section-card {
  background: var(--white); border: 1px solid var(--gray5); border-radius: 8px;
  margin-bottom: 24px; overflow: hidden;
}
.section-card-header {
  padding: 16px 24px; border-bottom: 1px solid var(--gray5);
  display: flex; align-items: center; justify-content: space-between;
}
.section-card-title { font-family: 'Google Sans', sans-serif; font-size: 16px; font-weight: 400; }
.section-card-actions { display: flex; gap: 8px; }

/* ── Table ── */
.table-wrap { overflow-x: auto; }
table { width: 100%; border-collapse: collapse; }
thead th {
  text-align: left; padding: 12px 16px;
  font-size: 12px; font-weight: 500; color: var(--gray3);
  border-bottom: 1px solid var(--gray5);
  white-space: nowrap; user-select: none;
}
tbody tr { border-bottom: 1px solid var(--gray6); transition: background .1s; }
tbody tr:last-child { border-bottom: none; }
tbody tr:hover { background: var(--gray7); }
tbody td { padding: 12px 16px; font-size: 13px; vertical-align: middle; }
.td-name { font-weight: 500; color: var(--blue); }
.td-mono { font-family: monospace; font-size: 12px; color: var(--gray2); }

/* ── Chips / badges ── */
.chip {
  display: inline-flex; align-items: center; gap: 4px;
  padding: 2px 8px; border-radius: 12px; font-size: 11px; font-weight: 500;
}
.chip-green  { background: #e6f4ea; color: #137333; }
.chip-red    { background: #fce8e6; color: #c5221f; }
.chip-yellow { background: #fef7e0; color: #b05e00; }
.chip-blue   { background: #e8f0fe; color: #1967d2; }
.chip-gray   { background: var(--gray6); color: var(--gray3); }
.chip-dot { width: 6px; height: 6px; border-radius: 50%; flex-shrink: 0; }
.chip-dot-green  { background: #34a853; }
.chip-dot-red    { background: #ea4335; }
.chip-dot-yellow { background: #fbbc04; animation: pulse .8s infinite; }
.chip-dot-gray   { background: var(--gray4); }
@keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.3} }

/* ── Info grid ── */
.info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0; }
.info-grid-item { padding: 12px 24px; border-bottom: 1px solid var(--gray6); border-right: 1px solid var(--gray6); }
.info-grid-item:nth-child(2n) { border-right: none; }
.info-grid-item:nth-last-child(-n+2) { border-bottom: none; }
.info-key { font-size: 11px; color: var(--gray3); text-transform: uppercase; letter-spacing: .06em; margin-bottom: 4px; }
.info-val { font-size: 13px; color: var(--gray1); font-weight: 500; display: flex; align-items: center; gap: 6px; }
.info-val a { color: var(--blue); text-decoration: none; }
.info-val a:hover { text-decoration: underline; }

/* ── Logs ── */
.logs-toolbar { padding: 12px 24px; border-bottom: 1px solid var(--gray5); display: flex; align-items: center; gap: 8px; }
.logs-filter { border: 1px solid var(--gray5); border-radius: 4px; padding: 6px 12px; font-size: 13px; flex: 1; color: var(--gray2); outline: none; }
.logs-filter:focus { border-color: var(--blue); }
.log-pre { font-family: 'Roboto Mono', monospace; font-size: 12px; line-height: 1.8; padding: 16px 24px; max-height: 340px; overflow-y: auto; background: #f8f9fa; color: #202124; white-space: pre-wrap; word-break: break-all; }
.log-line { padding: 1px 0; }
.log-line:hover { background: #e8f0fe; }
.log-ts { color: var(--gray3); margin-right: 8px; }

/* ── Copy btn ── */
.copy-btn { background: none; border: none; cursor: pointer; color: var(--gray4); font-size: 13px; padding: 2px 4px; border-radius: 3px; transition: color .1s; }
.copy-btn:hover { color: var(--gray1); background: var(--gray6); }

/* ── Two-col layout ── */
.two-col { display: grid; grid-template-columns: 1fr 340px; gap: 24px; }
@media (max-width: 1100px) { .two-col { grid-template-columns: 1fr; } }

/* ── Refresh bar ── */
.refresh-bar { display: flex; align-items: center; gap: 8px; margin-bottom: 16px; }
.refresh-note { font-size: 12px; color: var(--gray3); margin-left: auto; }
</style>
</head>
<body>

<!-- ── Top bar ──────────────────────────────────────────────── -->
<div class="topbar">
  <div class="topbar-logo">
    <svg class="gcp-logo" viewBox="0 0 24 24" fill="none">
      <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2z" fill="#4285f4"/>
      <path d="M7 12l3 3 7-7" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
    </svg>
    <span class="topbar-title">Synapse Console</span>
  </div>
  <div class="topbar-sep"></div>
  <div class="project-select">
    <span class="proj-icon">▤</span>
    <div>
      <div class="project-label">Project</div>
      <div class="project-id" id="header-project">laden...</div>
    </div>
  </div>
  <div class="topbar-actions">
    <button class="icon-btn" id="btn-refresh-top" title="Verversen">↺</button>
    <a class="icon-btn" id="console-link" href="#" target="_blank" title="Open GCP Console">⧉</a>
  </div>
</div>

<!-- ── Layout ───────────────────────────────────────────────── -->
<div class="layout">

  <!-- Sidebar -->
  <nav class="sidebar">
    <div class="sidebar-section">
      <div class="sidebar-header">Compute</div>
      <a class="nav-item active" href="#" onclick="return false">
        <span class="nav-icon">⊞</span> Instance Groups
      </a>
      <a class="nav-item" id="nav-vm-link" href="#" target="_blank">
        <span class="nav-icon">▣</span> VM Instances
      </a>
    </div>
    <div class="sidebar-divider"></div>
    <div class="sidebar-section">
      <div class="sidebar-header">Netwerk</div>
      <a class="nav-item" id="nav-lb-link" href="#" target="_blank">
        <span class="nav-icon">⇄</span> Load Balancing
      </a>
      <a class="nav-item" id="nav-armor-link" href="#" target="_blank">
        <span class="nav-icon">⛨</span> Cloud Armor
      </a>
    </div>
    <div class="sidebar-divider"></div>
    <div class="sidebar-section">
      <div class="sidebar-header">Data</div>
      <a class="nav-item" id="nav-sql-link" href="#" target="_blank">
        <span class="nav-icon">⛁</span> Cloud SQL
      </a>
      <a class="nav-item" id="nav-storage-link" href="#" target="_blank">
        <span class="nav-icon">◫</span> Cloud Storage
      </a>
      <a class="nav-item" id="nav-secrets-link" href="#" target="_blank">
        <span class="nav-icon">🔑</span> Secret Manager
      </a>
    </div>
    <div class="sidebar-divider"></div>
    <div class="sidebar-section">
      <div class="sidebar-header">Observability</div>
      <a class="nav-item" id="nav-logs-link" href="#" target="_blank">
        <span class="nav-icon">≡</span> Logging
      </a>
      <a class="nav-item" id="nav-uptime-link" href="#" target="_blank">
        <span class="nav-icon">◉</span> Uptime Checks
      </a>
    </div>
    <div class="sidebar-divider"></div>
    <div class="sidebar-section">
      <div class="sidebar-header">Kosten</div>
      <a class="nav-item" id="nav-billing-link" href="#" target="_blank">
        <span class="nav-icon">$</span> Billing
      </a>
    </div>
  </nav>

  <!-- Main content -->
  <main class="main">
    <div class="refresh-bar">
      <div>
        <div class="breadcrumb">Compute Engine &rsaquo; Instance Groups</div>
        <div class="page-title">Instance Groups</div>
      </div>
      <div style="margin-left:auto;display:flex;gap:8px;align-items:center">
        <button class="btn-primary" id="btn-all-start">▶ Alles starten</button>
        <button class="btn-danger"  id="btn-all-stop" >■ Alles stoppen</button>
        <span class="refresh-note" id="last-refresh"></span>
      </div>
    </div>

    <!-- Summary cards -->
    <div class="summary-grid" id="summary-cards"></div>

    <!-- Two-col layout -->
    <div class="two-col">

      <!-- Left column: MIG + VM + Logs -->
      <div>
        <!-- Instance Groups table -->
        <div class="section-card" style="margin-bottom:16px">
          <div class="section-card-header">
            <span class="section-card-title">Managed Instance Groups</span>
            <div class="section-card-actions">
              <button class="btn-secondary btn-sm" id="btn-refresh-table">↺ Verversen</button>
            </div>
          </div>
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Naam</th>
                  <th>Status</th>
                  <th>Instanties</th>
                  <th>Zone</th>
                  <th>Poort</th>
                  <th>Acties</th>
                </tr>
              </thead>
              <tbody id="mig-tbody"></tbody>
            </table>
          </div>
        </div>

        <!-- VM Instances table -->
        <div class="section-card" style="margin-bottom:16px">
          <div class="section-card-header">
            <span class="section-card-title">VM Instances</span>
            <div class="section-card-actions">
              <a class="btn-secondary btn-sm" id="vm-console-btn" href="#" target="_blank">⧉ Console</a>
            </div>
          </div>
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Naam</th>
                  <th>Status</th>
                  <th>Machine type</th>
                  <th>Zone</th>
                  <th>Intern IP</th>
                  <th style="min-width:160px">CPU gebruik (5 min)</th>
                  <th>Acties</th>
                </tr>
              </thead>
              <tbody id="vm-tbody"></tbody>
            </table>
          </div>
        </div>

        <!-- Logs -->
        <div class="section-card">
          <div class="section-card-header">
            <span class="section-card-title">Logging</span>
            <div class="section-card-actions">
              <a class="btn-secondary btn-sm" id="open-logs-btn" href="#" target="_blank">⧉ Open in Console</a>
              <button class="btn-secondary btn-sm" id="btn-load-logs">↺ Laden</button>
            </div>
          </div>
          <div class="logs-toolbar">
            <input class="logs-filter" id="log-filter" type="text" placeholder="Filter logs...">
          </div>
          <div class="log-pre" id="log-output">Klik op "Laden" om logregels op te halen.</div>
        </div>
      </div>

      <!-- Right column: Network + Infra + Cost -->
      <div>
        <div class="section-card" style="margin-bottom:16px">
          <div class="section-card-header">
            <span class="section-card-title">Netwerk</span>
          </div>
          <div id="network-info"></div>
        </div>

        <div class="section-card" style="margin-bottom:16px">
          <div class="section-card-header">
            <span class="section-card-title">Infrastructuur</span>
          </div>
          <div id="infra-info"></div>
        </div>

        <div class="section-card">
          <div class="section-card-header">
            <span class="section-card-title">Geschatte Kosten</span>
            <a class="btn-secondary btn-sm" id="billing-console-btn" href="#" target="_blank">⧉ Billing</a>
          </div>
          <div id="cost-info"></div>
        </div>
      </div>
    </div>

  </main>
</div>

<script>
const MIGS = __MIGS_JSON__;
const $ = id => document.getElementById(id);

// ── Helpers ────────────────────────────────────────────────────
function chip(text, type, dot) {
  const c = document.createElement('span');
  c.className = `chip chip-${type}`;
  if (dot) {
    const d = document.createElement('span');
    d.className = `chip-dot chip-dot-${type}`;
    c.appendChild(d);
  }
  c.appendChild(document.createTextNode(text));
  return c;
}

function copyBtn(text) {
  const b = document.createElement('button');
  b.className = 'copy-btn';
  b.textContent = '⎘';
  b.title = 'Kopiëren';
  b.addEventListener('click', () => {
    navigator.clipboard.writeText(text);
    b.textContent = '✓';
    setTimeout(() => b.textContent = '⎘', 1200);
  });
  return b;
}

function linkEl(href, text) {
  const a = document.createElement('a');
  a.href = href; a.target = '_blank'; a.textContent = text;
  return a;
}

function infoRow(key, valueEl) {
  const wrap = document.createElement('div');
  wrap.className = 'info-grid-item';
  const k = document.createElement('div'); k.className = 'info-key'; k.textContent = key;
  const v = document.createElement('div'); v.className = 'info-val';
  if (typeof valueEl === 'string') v.textContent = valueEl;
  else v.appendChild(valueEl);
  wrap.append(k, v);
  return wrap;
}

// ── Render summary cards ───────────────────────────────────────
function renderSummary(data) {
  const container = $('summary-cards');
  container.textContent = '';
  for (const m of MIGS) {
    const size = data.migs[m.id] ?? -1;
    const insts = data.instances[m.id] ?? [];
    const running = insts.filter(i => i.status === 'RUNNING').length;
    const on = size > 0;

    const card = document.createElement('div');
    card.className = 'summary-card';

    const top = document.createElement('div');
    top.className = 'summary-card-top';
    const lbl = document.createElement('div');
    lbl.className = 'summary-card-label';
    lbl.textContent = m.label.toUpperCase();
    const icon = document.createElement('div');
    icon.className = 'summary-card-icon';
    icon.textContent = on ? '⊞' : '⊟';
    top.append(lbl, icon);

    const val = document.createElement('div');
    val.className = 'summary-card-value';
    val.textContent = size < 0 ? '–' : String(size);

    const sub = document.createElement('div');
    sub.className = 'summary-card-sub';
    sub.textContent = size < 0 ? 'status onbekend' : on ? `${running} running` : 'gestopt';

    const status = document.createElement('div');
    status.className = 'summary-card-status';
    if (size < 0) status.appendChild(chip('UNKNOWN', 'gray', true));
    else if (!on) status.appendChild(chip('GESTOPT', 'red', true));
    else if (running === size) status.appendChild(chip('RUNNING', 'green', true));
    else status.appendChild(chip('STARTING', 'yellow', true));

    card.append(top, val, sub, status);
    container.appendChild(card);
  }

  // Cloud SQL card
  const sql = data.sql;
  const sqlCard = document.createElement('div');
  sqlCard.className = 'summary-card';
  const sqlTop = document.createElement('div');
  sqlTop.className = 'summary-card-top';
  const sqlLbl = document.createElement('div');
  sqlLbl.className = 'summary-card-label'; sqlLbl.textContent = 'CLOUD SQL';
  sqlTop.append(sqlLbl, Object.assign(document.createElement('div'), {className:'summary-card-icon',textContent:'⛁'}));
  const sqlVal = document.createElement('div');
  sqlVal.className = 'summary-card-value'; sqlVal.style.fontSize = '16px'; sqlVal.textContent = 'mvp-postgres';
  const sqlSub = document.createElement('div');
  sqlSub.className = 'summary-card-sub'; sqlSub.textContent = sql.ip || 'privaat IP';
  const sqlStatus = document.createElement('div');
  sqlStatus.className = 'summary-card-status';
  const sqlType = sql.state === 'RUNNABLE' ? 'green' : sql.state === 'UNKNOWN' ? 'gray' : 'red';
  sqlStatus.appendChild(chip(sql.state, sqlType, true));
  sqlCard.append(sqlTop, sqlVal, sqlSub, sqlStatus);
  container.appendChild(sqlCard);
}

// ── Render MIG table ───────────────────────────────────────────
function renderTable(data) {
  const tbody = $('mig-tbody');
  tbody.textContent = '';
  for (const m of MIGS) {
    const size  = data.migs[m.id] ?? -1;
    const insts = data.instances[m.id] ?? [];
    const running = insts.filter(i => i.status === 'RUNNING').length;
    const on = size > 0;
    const action = insts.find(i => i.action && i.action !== 'NONE');

    const tr = document.createElement('tr');

    // Name
    const tdName = document.createElement('td');
    const nameLink = document.createElement('a');
    nameLink.className = 'td-name';
    nameLink.href = '#'; nameLink.textContent = m.id;
    tdName.appendChild(nameLink);
    tr.appendChild(tdName);

    // Status chip
    const tdStatus = document.createElement('td');
    if (size < 0) tdStatus.appendChild(chip('UNKNOWN', 'gray', true));
    else if (!on) tdStatus.appendChild(chip('STOPPED', 'gray', true));
    else if (action) tdStatus.appendChild(chip(action.action, 'yellow', true));
    else if (running === size) tdStatus.appendChild(chip('RUNNING', 'green', true));
    else tdStatus.appendChild(chip('STARTING', 'yellow', true));
    tr.appendChild(tdStatus);

    // Instances
    const tdInst = document.createElement('td');
    tdInst.textContent = size < 0 ? '–' : `${running} / ${size}`;
    tr.appendChild(tdInst);

    // Region
    const tdZone = document.createElement('td');
    tdZone.className = 'td-mono';
    tdZone.textContent = data.zone;
    tr.appendChild(tdZone);

    // Port
    const tdPort = document.createElement('td');
    tdPort.className = 'td-mono';
    tdPort.textContent = m.port;
    tr.appendChild(tdPort);

    // Actions
    const tdAct = document.createElement('td');
    tdAct.style.cssText = 'display:flex;gap:6px;align-items:center;flex-wrap:wrap';

    if (m.url) {
      const openBtn = document.createElement('a');
      openBtn.className = 'btn-secondary btn-sm';
      openBtn.href = m.url; openBtn.target = '_blank';
      openBtn.textContent = '↗ Open';
      tdAct.appendChild(openBtn);
    }

    const toggleBtn = document.createElement('button');
    if (on) {
      toggleBtn.className = 'btn-danger btn-sm';
      toggleBtn.textContent = '■ Stop';
    } else {
      toggleBtn.className = 'btn-primary btn-sm';
      toggleBtn.textContent = '▶ Start';
    }
    toggleBtn.addEventListener('click', () => toggleMig(m.id, on, toggleBtn));
    tdAct.appendChild(toggleBtn);

    tr.appendChild(tdAct);
    tbody.appendChild(tr);
  }
}

// ── Render network ─────────────────────────────────────────────
function renderNetwork(data) {
  const c = $('network-info');
  c.textContent = '';
  const grid = document.createElement('div');
  grid.className = 'info-grid';

  // LB IP
  const ipVal = document.createElement('div');
  if (data.lb_ip_found) {
    const mono = document.createElement('span');
    mono.className = 'td-mono'; mono.textContent = data.lb_ip;
    ipVal.append(mono, copyBtn(data.lb_ip));
  } else {
    ipVal.appendChild(chip('niet gevonden', 'yellow'));
  }
  grid.appendChild(infoRow('Load Balancer IP', ipVal));

  const urls = [['UI', data.ui_url], ['API', data.api_url], ['Admin', data.admin_url]];
  for (const [label, url] of urls) {
    const wrap = document.createElement('div');
    wrap.append(linkEl(url, url.replace('https://', '')), copyBtn(url));
    grid.appendChild(infoRow(label, wrap));
  }

  const regionEl = document.createElement('span');
  regionEl.className = 'td-mono'; regionEl.textContent = data.region;
  grid.appendChild(infoRow('Regio', regionEl));

  c.appendChild(grid);
}

// ── Render infra ───────────────────────────────────────────────
function renderInfra(data) {
  const c = $('infra-info');
  c.textContent = '';
  const grid = document.createElement('div');
  grid.className = 'info-grid';

  // SSL certs
  for (const cert of data.ssl) {
    const t = cert.status === 'ACTIVE' ? 'green' : cert.status === 'PROVISIONING' ? 'yellow' : 'red';
    const name = cert.name.replace('synapse-ssl-cert', 'SSL cert');
    grid.appendChild(infoRow(name, chip(cert.status, t, true)));
  }
  if (!data.ssl.length) {
    grid.appendChild(infoRow('SSL', chip('geen', 'gray')));
  }

  // SQL
  const sqlType = data.sql.state === 'RUNNABLE' ? 'green' : data.sql.state === 'UNKNOWN' ? 'gray' : 'red';
  grid.appendChild(infoRow('Cloud SQL', chip(data.sql.state, sqlType, true)));

  if (data.sql.ip) {
    const ipEl = document.createElement('span');
    ipEl.className = 'td-mono'; ipEl.textContent = data.sql.ip;
    grid.appendChild(infoRow('SQL privaat IP', ipEl));
  }

  c.appendChild(grid);
}

// ── Render VMs ─────────────────────────────────────────────────
function renderVms(data) {
  const tbody = $('vm-tbody');
  tbody.textContent = '';

  if (!data.vms || !data.vms.length) {
    const tr = document.createElement('tr');
    const td = document.createElement('td');
    td.colSpan = 7;
    td.style.cssText = 'padding:24px;color:var(--gray3);text-align:center;font-size:13px';
    td.textContent = 'Geen VM instances gevonden.';
    tr.appendChild(td); tbody.appendChild(tr); return;
  }

  const cpuValues = Object.values(data.cpu || {});

  data.vms.forEach((vm, i) => {
    const cpuPct  = cpuValues[i] ?? null;
    const pct     = cpuPct !== null ? Math.min(cpuPct, 100) : 0;
    const color   = pct < 50 ? '#34a853' : pct < 80 ? '#fbbc04' : '#ea4335';
    const tr      = document.createElement('tr');

    // Name
    const tdName = document.createElement('td');
    tdName.className = 'td-name'; tdName.textContent = vm.name;
    tr.appendChild(tdName);

    // Status
    const tdStatus = document.createElement('td');
    tdStatus.appendChild(chip(vm.status, vm.status === 'RUNNING' ? 'green' : 'red', true));
    tr.appendChild(tdStatus);

    // Machine
    const tdMachine = document.createElement('td');
    tdMachine.className = 'td-mono'; tdMachine.textContent = vm.machine;
    tr.appendChild(tdMachine);

    // Zone
    const tdZone = document.createElement('td');
    tdZone.className = 'td-mono'; tdZone.textContent = vm.zone;
    tr.appendChild(tdZone);

    // IP
    const tdIp = document.createElement('td');
    tdIp.className = 'td-mono'; tdIp.textContent = vm.ip || '–';
    tr.appendChild(tdIp);

    // CPU bar
    const tdCpu = document.createElement('td');
    const wrap = document.createElement('div');
    wrap.style.cssText = 'display:flex;align-items:center;gap:8px';
    const barBg = document.createElement('div');
    barBg.style.cssText = 'flex:1;background:var(--gray6);border-radius:3px;height:7px;overflow:hidden';
    const fill = document.createElement('div');
    fill.style.cssText = `height:100%;width:${pct.toFixed(1)}%;background:${color};border-radius:3px;transition:width .5s`;
    barBg.appendChild(fill);
    const pctLbl = document.createElement('span');
    pctLbl.style.cssText = 'font-size:12px;color:var(--gray2);min-width:32px;text-align:right';
    pctLbl.textContent = cpuPct !== null ? `${cpuPct}%` : 'n/a';
    wrap.append(barBg, pctLbl);
    tdCpu.appendChild(wrap);
    tr.appendChild(tdCpu);

    // Actions
    const tdAct = document.createElement('td');
    const link = document.createElement('a');
    link.className = 'btn-secondary btn-sm';
    link.href = `https://console.cloud.google.com/compute/instancesDetail/zones/${vm.zone}/instances/${vm.name}?project=${data.project_id}`;
    link.target = '_blank'; link.textContent = '↗ Details';
    tdAct.appendChild(link);
    tr.appendChild(tdAct);

    tbody.appendChild(tr);
  });
}

// ── Render cost ────────────────────────────────────────────────
function renderCost(data) {
  const c = $('cost-info');
  c.textContent = '';
  const cost = data.cost;

  // Big total
  const hero = document.createElement('div');
  hero.style.cssText = 'padding:16px 24px 8px;display:flex;align-items:flex-end;gap:8px;border-bottom:1px solid var(--gray6)';
  const totalEl = document.createElement('div');
  totalEl.style.cssText = 'font-family:"Google Sans",sans-serif;font-size:32px;color:var(--gray1)';
  totalEl.textContent = `$${cost.total.toFixed(2)}`;
  const perDay = document.createElement('div');
  perDay.style.cssText = 'font-size:12px;color:var(--gray3);padding-bottom:6px';
  perDay.textContent = `/ maand  ·  ~$${cost.per_day.toFixed(2)} / dag`;
  hero.append(totalEl, perDay);
  c.appendChild(hero);

  // Breakdown
  const grid = document.createElement('div');
  grid.className = 'info-grid';

  const rows = [
    ['VMs (compute)', `$${cost.vms.toFixed(2)}`],
    ['Cloud SQL',     `$${cost.sql.toFixed(2)}`],
    ['Load Balancer', `$${cost.lb.toFixed(2)}`],
    ['Cloud Armor',   `$${cost.armor.toFixed(2)}`],
    ['Cloud Storage', `$${cost.storage.toFixed(2)}`],
  ];
  for (const [k, v] of rows) {
    const wrap = document.createElement('div');
    wrap.className = 'info-grid-item';
    const key = document.createElement('div'); key.className = 'info-key'; key.textContent = k;
    const val = document.createElement('div'); val.className = 'info-val'; val.textContent = v;
    wrap.append(key, val);
    grid.appendChild(wrap);
  }
  c.appendChild(grid);

  // Budget bar
  const barWrap = document.createElement('div');
  barWrap.style.cssText = 'padding:14px 24px;border-top:1px solid var(--gray6)';
  const barLabel = document.createElement('div');
  barLabel.style.cssText = 'font-size:11px;color:var(--gray3);text-transform:uppercase;letter-spacing:.06em;margin-bottom:8px';
  barLabel.textContent = 'Budget gebruik (max $40/maand)';
  const barBg = document.createElement('div');
  barBg.style.cssText = 'background:var(--gray6);border-radius:4px;height:8px;overflow:hidden';
  const pct = Math.min((cost.total / 40) * 100, 100);
  const barFill = document.createElement('div');
  const fillColor = pct < 50 ? '#34a853' : pct < 80 ? '#fbbc04' : '#ea4335';
  barFill.style.cssText = `height:100%;width:${pct.toFixed(1)}%;background:${fillColor};border-radius:4px;transition:width .5s`;
  barBg.appendChild(barFill);

  // Budget ticks
  const ticks = document.createElement('div');
  ticks.style.cssText = 'display:flex;justify-content:space-between;margin-top:4px';
  for (const b of cost.budgets) {
    const t = document.createElement('span');
    t.style.cssText = 'font-size:10px;color:var(--gray4)';
    t.textContent = `$${b}`;
    ticks.appendChild(t);
  }

  const pctLabel = document.createElement('div');
  pctLabel.style.cssText = 'font-size:11px;color:var(--gray3);margin-top:4px';
  pctLabel.textContent = `${pct.toFixed(0)}% van $40 budget`;

  barWrap.append(barLabel, barBg, ticks, pctLabel);
  c.appendChild(barWrap);

  // Note
  const note = document.createElement('div');
  note.style.cssText = 'padding:8px 24px 14px;font-size:11px;color:var(--gray4)';
  note.textContent = '* Schatting op basis van draaiende resources. Werkelijke kost zie je in de GCP Billing Console.';
  c.appendChild(note);
}

// ── Load status ────────────────────────────────────────────────
async function loadStatus() {
  const data = await fetch('/api/status').then(r => r.json());

  $('header-project').textContent = data.project_id;
  $('console-link').href   = data.console_url;
  $('open-logs-btn').href  = data.logs_url;
  $('nav-vm-link').href    = `https://console.cloud.google.com/compute/instances?project=${data.project_id}`;
  $('nav-lb-link').href    = `https://console.cloud.google.com/net-services/loadbalancing/list/loadBalancers?project=${data.project_id}`;
  $('nav-armor-link').href = `https://console.cloud.google.com/net-security/securitypolicies/list?project=${data.project_id}`;
  $('nav-sql-link').href   = data.sql_console;
  $('nav-storage-link').href  = `https://console.cloud.google.com/storage/browser?project=${data.project_id}`;
  $('nav-secrets-link').href  = `https://console.cloud.google.com/security/secret-manager?project=${data.project_id}`;
  $('nav-logs-link').href    = data.logs_url;
  $('nav-uptime-link').href  = `https://console.cloud.google.com/monitoring/uptime?project=${data.project_id}`;
  $('nav-billing-link').href    = data.cost.billing_url;
  $('billing-console-btn').href = data.cost.billing_url;
  $('vm-console-btn').href    = `https://console.cloud.google.com/compute/instances?project=${data.project_id}`;
  $('btn-refresh-vms').addEventListener('click', loadStatus);

  renderSummary(data);
  renderTable(data);
  renderNetwork(data);
  renderInfra(data);
  renderVms(data);
  renderCost(data);

  $('last-refresh').textContent = 'Bijgewerkt: ' + new Date().toLocaleTimeString('nl-BE');
}

// ── Logs ───────────────────────────────────────────────────────
let allLogLines = [];

async function loadLogs() {
  const out = $('log-output');
  out.textContent = 'Logs ophalen…';
  const data = await fetch('/api/logs').then(r => r.json());
  allLogLines = data.lines;
  renderLogs($('log-filter').value);
}

function renderLogs(filter) {
  const out = $('log-output');
  out.textContent = '';
  const lines = filter
    ? allLogLines.filter(l => l.toLowerCase().includes(filter.toLowerCase()))
    : allLogLines;
  for (const line of lines) {
    const div = document.createElement('div');
    div.className = 'log-line';
    const ts  = line.slice(0, 19);
    const msg = line.slice(20);
    const tsEl = document.createElement('span');
    tsEl.className = 'log-ts'; tsEl.textContent = ts;
    div.append(tsEl, document.createTextNode(msg));
    out.appendChild(div);
  }
  if (!lines.length) out.textContent = 'Geen resultaten.';
}

$('log-filter').addEventListener('input', e => renderLogs(e.target.value));

// ── Actions ────────────────────────────────────────────────────
async function toggleMig(migId, isOn, btn) {
  btn.disabled = true; btn.textContent = '…';
  await fetch('/api/mig/' + migId, {
    method: 'POST', headers: {'Content-Type':'application/json'},
    body: JSON.stringify({size: isOn ? 0 : 1})
  });
  await loadStatus();
}

$('btn-all-start').addEventListener('click', async () => {
  $('btn-all-start').disabled = true;
  await fetch('/api/all', {method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify({size:1})});
  await loadStatus();
  $('btn-all-start').disabled = false;
});

$('btn-all-stop').addEventListener('click', async () => {
  if (!confirm('Alle services stoppen?')) return;
  $('btn-all-stop').disabled = true;
  await fetch('/api/all', {method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify({size:0})});
  await loadStatus();
  $('btn-all-stop').disabled = false;
});

$('btn-refresh-top').addEventListener('click', loadStatus);
$('btn-refresh-table').addEventListener('click', loadStatus);
$('btn-load-logs').addEventListener('click', loadLogs);

loadStatus();
setInterval(loadStatus, 20000);
</script>
</body>
</html>
"""

# ── Request handler ────────────────────────────────────────────
class Handler(http.server.BaseHTTPRequestHandler):
    def log_message(self, fmt, *args): pass

    def send_json(self, data, code=200):
        body = json.dumps(data).encode()
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def send_html(self, html):
        body = html.encode()
        self.send_response(200)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        if self.path == "/":
            self.send_html(HTML.replace("__MIGS_JSON__", json.dumps(MIGS)))
        elif self.path == "/api/status":
            self.send_json(fetch_all_status())
        elif self.path == "/api/logs":
            self.send_json({"lines": get_recent_logs()})
        elif self.path == "/api/debug":
            out1, c1 = gcloud("config", "get-value", "project")
            out2, c2 = gcloud("compute", "instance-groups", "managed", "list",
                              f"--project={PROJECT_ID}", "--format=value(name,targetSize)")
            out3, c3 = gcloud("compute", "addresses", "describe", "synapse-lb-ip",
                              "--global", f"--project={PROJECT_ID}", "--format=value(address)")
            out4, c4 = gcloud("compute", "instance-groups", "managed", "describe", "synapse-ui-mig",
                              f"--zone={ZONE}", f"--project={PROJECT_ID}", "--format=value(targetSize)")
            self.send_json({
                "gcloud_cmd": _GCLOUD,
                "platform": sys.platform,
                "project_id": PROJECT_ID,
                "zone": ZONE,
                "region": REGION,
                "active_project":  {"out": out1, "code": c1},
                "mig_list":        {"out": out2, "code": c2},
                "lb_ip":           {"out": out3, "code": c3},
                "mig_describe_ui": {"out": out4, "code": c4},
            })
        else:
            self.send_response(404); self.end_headers()

    def do_POST(self):
        length = int(self.headers.get("Content-Length", 0))
        body = json.loads(self.rfile.read(length)) if length else {}
        valid_ids = {m["id"] for m in MIGS}

        if self.path.startswith("/api/mig/"):
            mig_id = self.path[len("/api/mig/"):]
            if mig_id not in valid_ids:
                self.send_response(400); self.end_headers(); return
            size = max(0, min(10, int(body.get("size", 1))))
            self.send_json({"ok": set_mig_size(mig_id, size)})

        elif self.path == "/api/all":
            size = max(0, min(10, int(body.get("size", 1))))
            results = {m["id"]: set_mig_size(m["id"], size) for m in MIGS}
            self.send_json({"ok": True, "results": results})

        else:
            self.send_response(404); self.end_headers()

# ── Main ───────────────────────────────────────────────────────
if __name__ == "__main__":
    if not PROJECT_ID:
        print("Geen config.env gevonden. Voer eerst deploy/bootstrap.sh uit.")
        raise SystemExit(1)

    server = http.server.HTTPServer(("localhost", PORT), Handler)
    url = f"http://localhost:{PORT}"
    print(f"Synapse Console → {url}")
    print(f"Project: {PROJECT_ID} | Domein: {DOMAIN} | Regio: {REGION}")
    print("Ctrl+C om te stoppen.\n")

    threading.Timer(1.0, lambda: webbrowser.open(url)).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nDashboard gestopt.")
