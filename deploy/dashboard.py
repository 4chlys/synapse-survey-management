#!/usr/bin/env python3
"""
Synapse – Web Dashboard
Start: python deploy/dashboard.py
Open:  http://localhost:8080
"""

import concurrent.futures
import http.server
import json
import subprocess
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
DOMAIN     = config.get("DOMAIN", "synapse-ip.xyz")

MIGS = [
    {"id": "synapse-api-mig",   "label": "API",   "url": f"https://api.{DOMAIN}",   "health": "synapse-api-backend"},
    {"id": "synapse-ui-mig",    "label": "UI",    "url": f"https://{DOMAIN}",        "health": "synapse-ui-backend"},
    {"id": "synapse-admin-mig", "label": "Admin", "url": f"https://admin.{DOMAIN}", "health": "synapse-admin-backend"},
]

# ── GCloud helpers ─────────────────────────────────────────────
def gcloud(*args, timeout=20):
    try:
        r = subprocess.run(["gcloud", *args], capture_output=True, text=True, timeout=timeout)
        return r.stdout.strip(), r.returncode
    except subprocess.TimeoutExpired:
        return "", 1
    except FileNotFoundError:
        return "", 1

def mig_size(mig_id):
    out, code = gcloud(
        "compute", "instance-groups", "managed", "describe", mig_id,
        f"--region={REGION}", f"--project={PROJECT_ID}", "--format=value(targetSize)"
    )
    try:
        return int(out) if code == 0 else -1
    except ValueError:
        return -1

def set_mig_size(mig_id, size):
    _, code = gcloud(
        "compute", "instance-groups", "managed", "resize", mig_id,
        f"--size={size}", f"--region={REGION}", f"--project={PROJECT_ID}", "--quiet",
        timeout=30
    )
    return code == 0

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

def get_backend_health(backend):
    out, code = gcloud(
        "compute", "backend-services", "get-health", backend,
        "--global", f"--project={PROJECT_ID}",
        "--format=value(status.healthStatus.healthState)",
        timeout=15
    )
    if code != 0 or not out:
        return "UNKNOWN"
    states = [s.strip() for s in out.splitlines() if s.strip()]
    if not states:
        return "NO_INSTANCES"
    return "HEALTHY" if all(s == "HEALTHY" for s in states) else "UNHEALTHY"

def get_cloud_sql_status():
    out, code = gcloud(
        "sql", "instances", "describe", "mvp-postgres",
        f"--project={PROJECT_ID}", "--format=value(state)"
    )
    return out if (code == 0 and out) else "UNKNOWN"

def get_recent_logs(limit=40):
    out, _ = gcloud(
        "logging", "read",
        "resource.type=gce_instance",
        f"--limit={limit}", f"--project={PROJECT_ID}",
        "--format=value(timestamp,textPayload)",
        timeout=25
    )
    lines = [l for l in out.splitlines() if l.strip()]
    return lines if lines else ["Geen logs gevonden."]

# ── Status (parallel) ──────────────────────────────────────────
def fetch_all_status():
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as ex:
        mig_futures    = {m["id"]: ex.submit(mig_size, m["id"]) for m in MIGS}
        health_futures = {m["id"]: ex.submit(get_backend_health, m["health"]) for m in MIGS}
        lb_future      = ex.submit(get_lb_ip)
        ssl_future     = ex.submit(get_ssl_certs)
        sql_future     = ex.submit(get_cloud_sql_status)

    domain = DOMAIN or "synapse-ip.xyz"
    lb_ip  = lb_future.result()

    return {
        "migs": {mid: f.result() for mid, f in mig_futures.items()},
        "health": {mid: f.result() for mid, f in health_futures.items()},
        "lb_ip": lb_ip or "niet gevonden",
        "lb_ip_found": lb_ip is not None,
        "ui_url":    f"https://{domain}",
        "api_url":   f"https://api.{domain}",
        "admin_url": f"https://admin.{domain}",
        "console_url": f"https://console.cloud.google.com/compute/instanceGroups?project={PROJECT_ID}",
        "logs_url": f"https://console.cloud.google.com/logs/query?project={PROJECT_ID}",
        "sql_url": f"https://console.cloud.google.com/sql/instances?project={PROJECT_ID}",
        "ssl": ssl_future.result(),
        "sql_status": sql_future.result(),
        "project_id": PROJECT_ID,
        "region": REGION,
        "domain": domain,
    }

# ── HTML ───────────────────────────────────────────────────────
HTML = r"""<!DOCTYPE html>
<html lang="nl">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Synapse Dashboard</title>
<style>
* { box-sizing: border-box; margin: 0; padding: 0; }
:root {
  --bg:#0f172a; --surface:#1e293b; --surface2:#263347; --border:#334155;
  --text:#f1f5f9; --muted:#94a3b8;
  --green:#22c55e; --red:#ef4444; --yellow:#f59e0b; --blue:#3b82f6; --purple:#a78bfa;
}
body { background:var(--bg); color:var(--text); font-family:system-ui,sans-serif; min-height:100vh; }
header { background:var(--surface); border-bottom:1px solid var(--border); padding:.9rem 2rem; display:flex; align-items:center; gap:.9rem; }
header h1 { font-size:1.15rem; font-weight:700; }
.project-chip { background:var(--surface2); border:1px solid var(--border); border-radius:6px; padding:.2rem .7rem; font-size:.75rem; color:var(--muted); }
.pulse { width:8px; height:8px; border-radius:50%; background:var(--green); animation:blink 2s infinite; flex-shrink:0; }
.header-right { margin-left:auto; display:flex; gap:.5rem; align-items:center; }
@keyframes blink { 0%,100%{opacity:1} 50%{opacity:.3} }
main { padding:1.75rem 2rem; max-width:1200px; margin:0 auto; }

/* action bar */
.actions { display:flex; gap:.6rem; margin-bottom:1.75rem; flex-wrap:wrap; align-items:center; }
.btn { display:inline-flex; align-items:center; gap:.35rem; padding:.45rem 1rem; border-radius:7px; font-size:.8rem; font-weight:600; border:none; cursor:pointer; transition:opacity .15s,transform .1s; text-decoration:none; }
.btn:hover { opacity:.85; }
.btn:active { transform:scale(.97); }
.btn:disabled { opacity:.4; cursor:not-allowed; }
.btn-green  { background:var(--green);  color:#fff; }
.btn-red    { background:var(--red);    color:#fff; }
.btn-ghost  { background:var(--surface); border:1px solid var(--border); color:var(--text); }
.btn-blue   { background:var(--blue);   color:#fff; }
.refresh-note { color:var(--muted); font-size:.72rem; margin-left:auto; }

/* grid */
.grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(310px,1fr)); gap:1.25rem; margin-bottom:1.25rem; }
.card { background:var(--surface); border:1px solid var(--border); border-radius:12px; padding:1.25rem; }
.card-title { font-size:.68rem; text-transform:uppercase; letter-spacing:.1em; color:var(--muted); margin-bottom:1rem; display:flex; justify-content:space-between; align-items:center; }
.row { display:flex; align-items:center; justify-content:space-between; padding:.65rem 0; border-bottom:1px solid var(--border); }
.row:last-child { border-bottom:none; }
.row-left { display:flex; align-items:center; gap:.7rem; }
.dot { width:9px; height:9px; border-radius:50%; flex-shrink:0; }
.dot-on  { background:var(--green); box-shadow:0 0 5px var(--green); }
.dot-off { background:var(--red); }
.dot-unk { background:var(--yellow); animation:blink 1s infinite; }
.label { font-weight:600; font-size:.875rem; }
.sub   { font-size:.72rem; color:var(--muted); margin-top:1px; }
.row-right { display:flex; gap:.35rem; align-items:center; flex-wrap:wrap; justify-content:flex-end; }
.btn-xs { padding:.28rem .65rem; border-radius:5px; font-size:.72rem; font-weight:600; border:none; cursor:pointer; transition:opacity .15s; text-decoration:none; display:inline-flex; align-items:center; gap:.25rem; }
.btn-xs:hover { opacity:.8; }
.btn-xs-green { background:var(--green); color:#fff; }
.btn-xs-red   { background:var(--red);   color:#fff; }
.btn-xs-ghost { background:transparent; border:1px solid var(--border); color:var(--muted); }
.btn-xs-blue  { background:var(--blue);  color:#fff; }

/* badges */
.badge { display:inline-flex; align-items:center; gap:.3rem; padding:.18rem .55rem; border-radius:999px; font-size:.68rem; font-weight:700; }
.badge-green { background:#166534; color:#86efac; }
.badge-red   { background:#7f1d1d; color:#fca5a5; }
.badge-yellow{ background:#854d0e; color:#fde68a; }
.badge-muted { background:var(--surface2); color:var(--muted); }

/* info rows */
.info-key { color:var(--muted); font-size:.8rem; }
.info-val { font-size:.8rem; font-weight:500; display:flex; align-items:center; gap:.4rem; }
a { color:var(--blue); text-decoration:none; }
a:hover { text-decoration:underline; }

/* copy btn */
.copy-btn { background:none; border:none; cursor:pointer; color:var(--muted); font-size:.75rem; padding:.1rem .3rem; border-radius:3px; }
.copy-btn:hover { color:var(--text); background:var(--border); }

/* logs */
.logs-card { background:var(--surface); border:1px solid var(--border); border-radius:12px; padding:1.25rem; }
.logs-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:.9rem; }
pre { background:#070d1a; border-radius:8px; padding:1rem; font-size:.7rem; line-height:1.75; overflow-x:auto; max-height:280px; overflow-y:auto; color:#7dd3fc; white-space:pre-wrap; word-break:break-all; }

/* spinner */
.spin { display:inline-block; animation:spin .8s linear infinite; }
@keyframes spin { to { transform:rotate(360deg); } }
</style>
</head>
<body>
<header>
  <div class="pulse"></div>
  <h1>Synapse Dashboard</h1>
  <span class="project-chip" id="project-chip">laden...</span>
  <div class="header-right">
    <a id="console-link" class="btn btn-ghost" href="#" target="_blank">&#9881; GCP Console</a>
  </div>
</header>
<main>
  <div class="actions">
    <button class="btn btn-green" id="btn-all-start">&#9654; Alles starten</button>
    <button class="btn btn-red"   id="btn-all-stop" >&#9632; Alles stoppen</button>
    <button class="btn btn-ghost" id="btn-refresh"  >&#8635; Verversen</button>
    <span class="refresh-note" id="last-refresh"></span>
  </div>

  <div class="grid">
    <!-- Services -->
    <div class="card">
      <div class="card-title">
        <span>Services (MIGs)</span>
        <a id="migs-console" href="#" target="_blank" class="btn-xs btn-xs-ghost">&#8599; Console</a>
      </div>
      <div id="mig-list"><div style="color:var(--muted);font-size:.8rem">Laden <span class="spin">&#9696;</span></div></div>
    </div>

    <!-- Netwerk -->
    <div class="card">
      <div class="card-title">Netwerk &amp; Domeinen</div>
      <div id="network-info"><div style="color:var(--muted);font-size:.8rem">Laden <span class="spin">&#9696;</span></div></div>
    </div>

    <!-- Infra status -->
    <div class="card">
      <div class="card-title">Infrastructuur</div>
      <div id="infra-info"><div style="color:var(--muted);font-size:.8rem">Laden <span class="spin">&#9696;</span></div></div>
    </div>
  </div>

  <!-- Logs -->
  <div class="logs-card">
    <div class="logs-header">
      <span class="card-title" style="margin:0">Cloud Logging</span>
      <div style="display:flex;gap:.5rem">
        <a id="logs-console-link" href="#" target="_blank" class="btn btn-ghost" style="font-size:.75rem">&#8599; Open in Console</a>
        <button class="btn btn-ghost" id="btn-logs" style="font-size:.75rem">&#8635; Logs laden</button>
      </div>
    </div>
    <pre id="log-output">Klik op "Logs laden" om logregels op te halen.</pre>
  </div>
</main>

<script>
// Injected by server — trusted config values, not user input
const MIGS = __MIGS_JSON__;

// ── DOM helpers ────────────────────────────────────────────────
const $ = id => document.getElementById(id);

function el(tag, cls, text) {
  const e = document.createElement(tag);
  if (cls)  e.className = cls;
  if (text !== undefined) e.textContent = text;
  return e;
}

function badge(text, type) {
  const b = el('span', `badge badge-${type}`);
  b.textContent = text;
  return b;
}

function copyBtn(text) {
  const b = el('button', 'copy-btn', '⎘');
  b.title = 'Kopiëren';
  b.addEventListener('click', () => {
    navigator.clipboard.writeText(text);
    b.textContent = '✓';
    setTimeout(() => { b.textContent = '⎘'; }, 1200);
  });
  return b;
}

// ── Render services ────────────────────────────────────────────
function renderMigs(data) {
  const container = $('mig-list');
  container.textContent = '';

  for (const m of MIGS) {
    const size   = data.migs[m.id] ?? -1;
    const health = data.health[m.id] ?? 'UNKNOWN';
    const on     = size > 0;

    const row   = el('div', 'row');
    const left  = el('div', 'row-left');
    const dot   = el('div', 'dot ' + (size < 0 ? 'dot-unk' : on ? 'dot-on' : 'dot-off'));
    const info  = el('div');
    const name  = el('div', 'label', m.label);
    const sub   = el('div', 'sub');
    sub.textContent = size < 0 ? 'status onbekend' : `${size} instantie${size !== 1 ? 's' : ''}`;
    info.append(name, sub);
    left.append(dot, info);

    const right = el('div', 'row-right');

    // health badge
    if (on) {
      const htype = health === 'HEALTHY' ? 'green' : health === 'UNKNOWN' || health === 'NO_INSTANCES' ? 'muted' : 'red';
      right.appendChild(badge(health === 'NO_INSTANCES' ? 'geen VM' : health, htype));
    }

    // open link
    if (m.url) {
      const a = el('a', 'btn-xs btn-xs-ghost', '↗');
      a.href = m.url; a.target = '_blank'; a.title = m.url;
      a.appendChild(document.createTextNode(' Open'));
      right.appendChild(a);
    }

    // toggle btn
    const toggle = el('button', `btn-xs ${on ? 'btn-xs-red' : 'btn-xs-green'}`, on ? '■ Stoppen' : '▶ Starten');
    toggle.addEventListener('click', () => toggleMig(m.id, on, toggle));
    right.appendChild(toggle);

    row.append(left, right);
    container.appendChild(row);
  }
}

// ── Render network ─────────────────────────────────────────────
function renderNetwork(data) {
  const container = $('network-info');
  container.textContent = '';

  // LB IP row
  const ipRow = el('div', 'row');
  const ipKey = el('span', 'info-key', 'Load Balancer IP');
  const ipVal = el('div', 'info-val');
  if (data.lb_ip_found) {
    ipVal.textContent = data.lb_ip;
    ipVal.appendChild(copyBtn(data.lb_ip));
  } else {
    const warn = badge('niet gevonden', 'yellow');
    ipVal.appendChild(warn);
    const hint = el('span');
    hint.style.cssText = 'color:var(--muted);font-size:.68rem;display:block;margin-top:2px';
    hint.textContent = 'Controleer: gcloud compute addresses list';
    ipVal.appendChild(hint);
  }
  ipRow.append(ipKey, ipVal);
  container.appendChild(ipRow);

  // domain rows
  const links = [
    ['UI',    data.ui_url],
    ['API',   data.api_url],
    ['Admin', data.admin_url],
  ];
  for (const [label, url] of links) {
    const row = el('div', 'row');
    const k   = el('span', 'info-key', label);
    const v   = el('div', 'info-val');
    const a   = el('a', null, url);
    a.href = url; a.target = '_blank';
    v.append(a, copyBtn(url));
    row.append(k, v);
    container.appendChild(row);
  }

  // region row
  const regRow = el('div', 'row');
  regRow.append(el('span', 'info-key', 'Regio'), el('span', 'info-val', data.region));
  container.appendChild(regRow);
}

// ── Render infra ───────────────────────────────────────────────
function renderInfra(data) {
  const container = $('infra-info');
  container.textContent = '';

  // SSL certs
  const sslTitle = el('div', 'card-title', 'SSL Certificaten');
  sslTitle.style.marginBottom = '.5rem';
  container.appendChild(sslTitle);

  if (!data.ssl.length) {
    container.appendChild(el('p', null, 'Geen certificaten.')).style.color = 'var(--muted)';
  } else {
    for (const cert of data.ssl) {
      const row  = el('div', 'row');
      const name = el('span', 'info-key');
      name.textContent = cert.name.replace('synapse-ssl-cert', 'SSL cert');
      const btype = cert.status === 'ACTIVE' ? 'green' : cert.status === 'PROVISIONING' ? 'yellow' : 'red';
      const dot = cert.status === 'ACTIVE' ? '●' : cert.status === 'PROVISIONING' ? '◐' : '✕';
      row.append(name, badge(`${dot} ${cert.status}`, btype));
      container.appendChild(row);
    }
  }

  // spacer
  container.appendChild(el('div')).style.marginTop = '.75rem';

  // Cloud SQL
  const sqlTitle = el('div', 'card-title', 'Cloud SQL');
  sqlTitle.style.marginBottom = '.5rem';
  container.appendChild(sqlTitle);

  const sqlRow  = el('div', 'row');
  const sqlKey  = el('span', 'info-key', 'mvp-postgres');
  const sqlSt   = data.sql_status;
  const sqlType = sqlSt === 'RUNNABLE' ? 'green' : sqlSt === 'UNKNOWN' ? 'muted' : 'red';
  const sqlDot  = sqlSt === 'RUNNABLE' ? '●' : '○';
  const sqlBadge = badge(`${sqlDot} ${sqlSt}`, sqlType);
  const sqlLink = el('a', 'btn-xs btn-xs-ghost', '↗');
  sqlLink.href = data.sql_url; sqlLink.target = '_blank';
  const sqlRight = el('div', 'row-right');
  sqlRight.append(sqlBadge, sqlLink);
  sqlRow.append(sqlKey, sqlRight);
  container.appendChild(sqlRow);
}

// ── Full status load ───────────────────────────────────────────
async function loadStatus() {
  const data = await fetch('/api/status').then(r => r.json());

  $('project-chip').textContent = data.project_id;
  $('console-link').href = data.console_url;
  $('migs-console').href = data.console_url;
  $('logs-console-link').href = data.logs_url;

  renderMigs(data);
  renderNetwork(data);
  renderInfra(data);

  $('last-refresh').textContent = 'Bijgewerkt: ' + new Date().toLocaleTimeString('nl-BE');
}

// ── Actions ────────────────────────────────────────────────────
async function toggleMig(migId, isOn, btn) {
  btn.disabled = true;
  btn.textContent = '…';
  await fetch('/api/mig/' + migId, {
    method: 'POST',
    headers: {'Content-Type':'application/json'},
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

$('btn-refresh').addEventListener('click', loadStatus);

$('btn-logs').addEventListener('click', async () => {
  $('log-output').textContent = 'Logs ophalen…';
  const data = await fetch('/api/logs').then(r => r.json());
  $('log-output').textContent = data.lines.join('\n') || 'Geen logs.';
});

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
            page = HTML.replace("__MIGS_JSON__", json.dumps(MIGS))
            self.send_html(page)
        elif self.path == "/api/status":
            self.send_json(fetch_all_status())
        elif self.path == "/api/logs":
            self.send_json({"lines": get_recent_logs()})
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
    print(f"Synapse Dashboard → {url}")
    print(f"Project: {PROJECT_ID} | Domein: {DOMAIN} | Regio: {REGION}")
    print("Ctrl+C om te stoppen.\n")

    threading.Timer(1.0, lambda: webbrowser.open(url)).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nDashboard gestopt.")
