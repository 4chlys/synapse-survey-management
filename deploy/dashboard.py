#!/usr/bin/env python3
"""
Synapse – Web Dashboard
Start: python deploy/dashboard.py
Open:  http://localhost:8080
"""

import http.server
import json
import os
import subprocess
import threading
import webbrowser
from pathlib import Path

# ── Config ─────────────────────────────────────────────────────
SCRIPT_DIR = Path(__file__).parent
CONFIG_FILE = SCRIPT_DIR / "config.env"
PORT = 8080

config = {}
if CONFIG_FILE.exists():
    for line in CONFIG_FILE.read_text().splitlines():
        line = line.strip()
        if "=" in line and not line.startswith("#"):
            k, _, v = line.partition("=")
            config[k.strip()] = v.strip().strip('"')

PROJECT_ID = config.get("PROJECT_ID", "")
REGION     = config.get("REGION", "europe-west1")
DOMAIN     = config.get("DOMAIN", "")

MIGS = [
    {"id": "synapse-api-mig",   "label": "API",   "url": f"https://api.{DOMAIN}"},
    {"id": "synapse-ui-mig",    "label": "UI",    "url": f"https://{DOMAIN}"},
    {"id": "synapse-admin-mig", "label": "Admin", "url": f"https://admin.{DOMAIN}"},
]

# ── GCloud helpers ─────────────────────────────────────────────
def gcloud(*args, timeout=15):
    try:
        result = subprocess.run(
            ["gcloud", *args],
            capture_output=True, text=True, timeout=timeout
        )
        return result.stdout.strip(), result.returncode
    except subprocess.TimeoutExpired:
        return "timeout", 1
    except FileNotFoundError:
        return "gcloud not found", 1

def mig_size(mig_id):
    out, code = gcloud(
        "compute", "instance-groups", "managed", "describe", mig_id,
        f"--region={REGION}", f"--project={PROJECT_ID}",
        "--format=value(targetSize)"
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

def lb_ip():
    out, code = gcloud(
        "compute", "addresses", "describe", "synapse-lb-ip",
        "--global", f"--project={PROJECT_ID}", "--format=value(address)"
    )
    return out if code == 0 else "onbekend"

def ssl_status():
    out, code = gcloud(
        "compute", "ssl-certificates", "list",
        f"--project={PROJECT_ID}",
        "--format=value(name,managed.status)"
    )
    if code != 0:
        return []
    results = []
    for line in out.splitlines():
        parts = line.split()
        if len(parts) >= 2:
            results.append({"name": parts[0], "status": parts[1]})
    return results

def recent_logs(limit=30):
    out, _ = gcloud(
        "logging", "read",
        f"resource.type=gce_instance",
        f"--limit={limit}", f"--project={PROJECT_ID}",
        "--format=value(timestamp,textPayload)",
        timeout=20
    )
    lines = [l for l in out.splitlines() if l.strip()]
    return lines[-limit:] if lines else ["Geen logs gevonden."]

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
    --bg: #0f172a; --surface: #1e293b; --border: #334155;
    --text: #f1f5f9; --muted: #94a3b8;
    --green: #22c55e; --red: #ef4444; --yellow: #f59e0b; --blue: #3b82f6;
  }
  body { background: var(--bg); color: var(--text); font-family: system-ui, sans-serif; min-height: 100vh; }
  header { background: var(--surface); border-bottom: 1px solid var(--border); padding: 1rem 2rem; display: flex; align-items: center; gap: 1rem; }
  header h1 { font-size: 1.25rem; font-weight: 700; }
  .project { color: var(--muted); font-size: .85rem; }
  .pulse { width: 8px; height: 8px; border-radius: 50%; background: var(--green); animation: pulse 2s infinite; }
  @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.4} }
  main { padding: 2rem; max-width: 1100px; margin: 0 auto; }
  .actions { display: flex; gap: .75rem; margin-bottom: 2rem; flex-wrap: wrap; align-items: center; }
  .btn { padding: .5rem 1.1rem; border-radius: 8px; font-size: .85rem; font-weight: 600; border: none; cursor: pointer; transition: opacity .15s; }
  .btn:hover { opacity: .85; }
  .btn:disabled { opacity: .4; cursor: not-allowed; }
  .btn-green { background: var(--green); color: #fff; }
  .btn-red   { background: var(--red);   color: #fff; }
  .btn-ghost { background: var(--surface); border: 1px solid var(--border); color: var(--text); }
  .note { color: var(--muted); font-size: .75rem; margin-left: auto; }
  .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 1.5rem; margin-bottom: 2rem; }
  .card { background: var(--surface); border: 1px solid var(--border); border-radius: 12px; padding: 1.5rem; }
  .card-title { font-size: .7rem; text-transform: uppercase; letter-spacing: .1em; color: var(--muted); margin-bottom: 1rem; }
  .row { display: flex; align-items: center; justify-content: space-between; padding: .7rem 0; border-bottom: 1px solid var(--border); }
  .row:last-child { border-bottom: none; }
  .row-left { display: flex; align-items: center; gap: .75rem; }
  .dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; }
  .dot-on  { background: var(--green); box-shadow: 0 0 6px var(--green); }
  .dot-off { background: var(--red); }
  .dot-unk { background: var(--yellow); animation: pulse 1s infinite; }
  .name { font-weight: 600; font-size: .9rem; }
  .sub  { font-size: .75rem; color: var(--muted); }
  .row-right { display: flex; gap: .4rem; align-items: center; }
  .btn-sm { padding: .3rem .75rem; border-radius: 6px; font-size: .75rem; font-weight: 600; border: none; cursor: pointer; transition: opacity .15s; }
  .btn-sm:hover { opacity: .85; }
  .btn-sm-green { background: var(--green); color: #fff; }
  .btn-sm-red   { background: var(--red);   color: #fff; }
  .btn-sm-ghost { background: transparent; border: 1px solid var(--border); color: var(--muted); text-decoration: none; }
  .badge { display: inline-block; padding: .2rem .55rem; border-radius: 999px; font-size: .7rem; font-weight: 700; letter-spacing: .03em; }
  .badge-active      { background: #166534; color: #86efac; }
  .badge-provisioning{ background: #854d0e; color: #fde68a; }
  .badge-failed      { background: #7f1d1d; color: #fca5a5; }
  .logs-card { background: var(--surface); border: 1px solid var(--border); border-radius: 12px; padding: 1.5rem; }
  .logs-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
  pre { background: #070d1a; border-radius: 8px; padding: 1rem; font-size: .72rem; line-height: 1.7; overflow-x: auto; max-height: 320px; overflow-y: auto; color: #7dd3fc; white-space: pre-wrap; word-break: break-all; }
  a { color: var(--blue); text-decoration: none; }
  a:hover { text-decoration: underline; }
</style>
</head>
<body>
<header>
  <div class="pulse"></div>
  <h1>Synapse Dashboard</h1>
  <span class="project" id="project-label"></span>
</header>
<main>
  <div class="actions">
    <button class="btn btn-green" id="btn-all-start">&#9654; Alles starten</button>
    <button class="btn btn-red"   id="btn-all-stop">&#9632; Alles stoppen</button>
    <button class="btn btn-ghost" id="btn-refresh">&#8635; Verversen</button>
    <span class="note" id="last-refresh"></span>
  </div>

  <div class="grid">
    <div class="card">
      <div class="card-title">Services (MIGs)</div>
      <div id="mig-list"></div>
    </div>
    <div class="card">
      <div class="card-title">Netwerk</div>
      <div id="network-info"></div>
    </div>
    <div class="card">
      <div class="card-title">SSL Certificaten</div>
      <div id="ssl-info"></div>
    </div>
  </div>

  <div class="logs-card">
    <div class="logs-header">
      <span class="card-title" style="margin:0">Cloud Logging</span>
      <button class="btn btn-ghost" id="btn-logs">&#8635; Logs laden</button>
    </div>
    <pre id="log-output">Klik op "Logs laden" om te starten.</pre>
  </div>
</main>

<script>
const esc = s => String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');

// Injected server-side — these come from trusted config, not user input
const MIGS = __MIGS__;
const PROJECT_ID = '__PROJECT_ID__';

document.getElementById('project-label').textContent = PROJECT_ID;

async function loadStatus() {
  const data = await fetch('/api/status').then(r => r.json());
  renderMigs(data);
  renderNetwork(data);
  renderSsl(data.ssl);
  document.getElementById('last-refresh').textContent = 'Bijgewerkt: ' + new Date().toLocaleTimeString('nl-BE');
}

function renderMigs(data) {
  const container = document.getElementById('mig-list');
  container.textContent = '';
  for (const m of MIGS) {
    const size = data.migs[m.id] ?? -1;
    const on   = size > 0;
    const row  = document.createElement('div');
    row.className = 'row';

    const left = document.createElement('div');
    left.className = 'row-left';

    const dot = document.createElement('div');
    dot.className = 'dot ' + (size < 0 ? 'dot-unk' : on ? 'dot-on' : 'dot-off');

    const info = document.createElement('div');
    const nameEl = document.createElement('div');
    nameEl.className = 'name';
    nameEl.textContent = m.label;
    const subEl = document.createElement('div');
    subEl.className = 'sub';
    subEl.textContent = size < 0 ? 'onbekend' : size + ' instantie(s)';
    info.append(nameEl, subEl);
    left.append(dot, info);

    const right = document.createElement('div');
    right.className = 'row-right';

    if (m.url) {
      const link = document.createElement('a');
      link.className = 'btn-sm btn-sm-ghost';
      link.href = m.url;
      link.target = '_blank';
      link.textContent = '\u2197 Open';
      right.appendChild(link);
    }

    const toggleBtn = document.createElement('button');
    toggleBtn.className = 'btn-sm ' + (on ? 'btn-sm-red' : 'btn-sm-green');
    toggleBtn.textContent = on ? 'Stoppen' : 'Starten';
    toggleBtn.addEventListener('click', () => toggleMig(m.id, on));
    right.appendChild(toggleBtn);

    row.append(left, right);
    container.appendChild(row);
  }
}

function renderNetwork(data) {
  const container = document.getElementById('network-info');
  container.textContent = '';
  const rows = [
    ['Load Balancer IP', data.lb_ip],
    ['UI',    data.ui_url],
    ['API',   data.api_url],
    ['Admin', data.admin_url],
  ];
  for (const [label, value] of rows) {
    const row = document.createElement('div');
    row.className = 'row';
    const l = document.createElement('span');
    l.textContent = label;
    const v = document.createElement('span');
    if (String(value).startsWith('https')) {
      const a = document.createElement('a');
      a.href = value;
      a.target = '_blank';
      a.textContent = value;
      v.appendChild(a);
    } else {
      v.textContent = value;
    }
    v.style.fontWeight = '500';
    row.append(l, v);
    container.appendChild(row);
  }
}

function renderSsl(certs) {
  const container = document.getElementById('ssl-info');
  container.textContent = '';
  if (!certs.length) {
    const p = document.createElement('p');
    p.style.color = 'var(--muted)';
    p.textContent = 'Geen certificaten gevonden.';
    container.appendChild(p);
    return;
  }
  for (const cert of certs) {
    const row = document.createElement('div');
    row.className = 'row';
    const name = document.createElement('span');
    name.textContent = cert.name;
    const badge = document.createElement('span');
    const cls = cert.status === 'ACTIVE' ? 'badge-active'
               : cert.status === 'PROVISIONING' ? 'badge-provisioning'
               : 'badge-failed';
    badge.className = 'badge ' + cls;
    badge.textContent = cert.status;
    row.append(name, badge);
    container.appendChild(row);
  }
}

async function toggleMig(migId, isOn) {
  await fetch('/api/mig/' + migId, {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({size: isOn ? 0 : 1})
  });
  await loadStatus();
}

document.getElementById('btn-all-start').addEventListener('click', async () => {
  await fetch('/api/all', {method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({size:1})});
  await loadStatus();
});

document.getElementById('btn-all-stop').addEventListener('click', async () => {
  if (!confirm('Alle services stoppen?')) return;
  await fetch('/api/all', {method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({size:0})});
  await loadStatus();
});

document.getElementById('btn-refresh').addEventListener('click', loadStatus);

document.getElementById('btn-logs').addEventListener('click', async () => {
  const pre = document.getElementById('log-output');
  pre.textContent = 'Logs ophalen...';
  const data = await fetch('/api/logs').then(r => r.json());
  pre.textContent = data.lines.join('\n') || 'Geen logs.';
});

loadStatus();
setInterval(loadStatus, 15000);
</script>
</body>
</html>
"""

# ── Request handler ────────────────────────────────────────────
class Handler(http.server.BaseHTTPRequestHandler):
    def log_message(self, fmt, *args):
        pass  # suppress default access log

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
            page = HTML \
                .replace("__MIGS__", json.dumps(MIGS)) \
                .replace("__PROJECT_ID__", PROJECT_ID)
            self.send_html(page)

        elif self.path == "/api/status":
            sizes = {m["id"]: mig_size(m["id"]) for m in MIGS}
            ip = lb_ip()
            domain = DOMAIN or "synapse-ip.xyz"
            self.send_json({
                "migs": sizes,
                "lb_ip": ip,
                "ui_url": f"https://{domain}",
                "api_url": f"https://api.{domain}",
                "admin_url": f"https://admin.{domain}",
                "ssl": ssl_status(),
            })

        elif self.path == "/api/logs":
            self.send_json({"lines": recent_logs()})

        else:
            self.send_response(404)
            self.end_headers()

    def do_POST(self):
        length = int(self.headers.get("Content-Length", 0))
        body = json.loads(self.rfile.read(length)) if length else {}

        if self.path.startswith("/api/mig/"):
            mig_id = self.path[len("/api/mig/"):]
            # only allow known MIG IDs
            valid_ids = {m["id"] for m in MIGS}
            if mig_id not in valid_ids:
                self.send_response(400)
                self.end_headers()
                return
            size = max(0, min(10, int(body.get("size", 1))))
            ok = set_mig_size(mig_id, size)
            self.send_json({"ok": ok})

        elif self.path == "/api/all":
            size = max(0, min(10, int(body.get("size", 1))))
            results = {m["id"]: set_mig_size(m["id"], size) for m in MIGS}
            self.send_json({"ok": True, "results": results})

        else:
            self.send_response(404)
            self.end_headers()

# ── Main ───────────────────────────────────────────────────────
if __name__ == "__main__":
    if not PROJECT_ID:
        print("Geen config.env gevonden. Maak deploy/config.env aan of voer bootstrap.sh uit.")
        raise SystemExit(1)

    server = http.server.HTTPServer(("localhost", PORT), Handler)
    url = f"http://localhost:{PORT}"
    print(f"Synapse Dashboard → {url}")
    print(f"Project: {PROJECT_ID} | Domein: {DOMAIN}")
    print("Ctrl+C om te stoppen.\n")

    threading.Timer(1.0, lambda: webbrowser.open(url)).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nDashboard gestopt.")
