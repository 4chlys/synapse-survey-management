// Blackbox browse load against the UI-User frontend — the real external surface.
// UI-User injects X-Api-Key server-side and calls the API -> Postgres, so hitting
// these public pages transitively stresses the whole stack exactly as a real
// visitor (or attacker) would, with no secret needed. k6 keeps a per-VU cookie
// jar and follows redirects, so the session-setup 302s resolve like a browser.
//
//   k6 run             -e ENV=prod -e PROFILE=load   -e PROJECT_ID=1 k6/browse.js
//   k6 run --out cloud -e ENV=prod -e PROFILE=stress -e PROJECT_ID=1 k6/browse.js   # -> Grafana Cloud

import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { UI, PROJECT_ID, stages, thresholds } from './lib/config.js';

export const options = { stages, thresholds, cloud: { name: 'Synapse blackbox browse' } };

export default function () {
  group('landing', () => {
    const r = http.get(`${UI}/${PROJECT_ID}`);
    check(r, { 'landing 200': (res) => res.status === 200 });
  });
  sleep(1); // think time

  group('survey', () => {
    const r = http.get(`${UI}/${PROJECT_ID}/Survey/Index`);
    check(r, { 'survey 200': (res) => res.status === 200 });
  });
  sleep(1);

  group('ideation', () => {
    const r = http.get(`${UI}/${PROJECT_ID}/Ideation/IdeaHub?categoryId=1`);
    check(r, { 'ideahub reachable': (res) => res.status === 200 });
  });
  sleep(1);
}
