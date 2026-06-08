// Read-only health probe. Safe to run against prod (no writes to Postgres).
// GET /health does a real DB CanConnect, so this exercises the DB roundtrip too.
//
//   k6 run -e ENV=prod -e PROFILE=load k6/health.js
//   k6 cloud run -e ENV=prod -e PROFILE=load k6/health.js

import http from 'k6/http';
import { check } from 'k6';
import { API, stages, thresholds, params } from './lib/config.js';

export const options = { stages, thresholds, cloud: { name: 'Synapse health' } };

export default function () {
  const res = http.get(`${API}/health`, params);
  check(res, {
    'status 200': (r) => r.status === 200,
    'reports healthy': (r) => r.json('status') === 'healthy',
  });
}
