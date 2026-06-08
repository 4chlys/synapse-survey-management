// Shared config for all k6 scripts.
// Pick the environment with ENV=test|prod (default: test).
// Pick the intensity with PROFILE=smoke|load|stress|soak (default: smoke).

const ENV = (__ENV.ENV || 'test').toLowerCase();

const API_HOSTS = {
  test: 'https://api.test.synapse-ip.xyz',
  prod: 'https://api.synapse-ip.xyz',
};

const UI_HOSTS = {
  test: 'https://test.synapse-ip.xyz',
  prod: 'https://synapse-ip.xyz',
};

export const isProd = ENV === 'prod';
export const API = __ENV.API_BASE_URL || API_HOSTS[ENV];
export const UI = __ENV.UI_BASE_URL || UI_HOSTS[ENV];

// Project under test (an active project id with a published questionnaire).
export const PROJECT_ID = parseInt(__ENV.PROJECT_ID || '1', 10);

// Intensity profiles. Tune target VUs to what the env should survive.
const PROFILES = {
  // Sanity check: a trickle of traffic, verify wiring before a real run.
  smoke: { stages: [{ duration: '1m', target: 5 }] },
  // Expected peak load held steady.
  load: {
    stages: [
      { duration: '2m', target: 50 },
      { duration: '5m', target: 50 },
      { duration: '2m', target: 0 },
    ],
  },
  // Push past expected peak to find the breaking point. TEST ONLY.
  stress: {
    stages: [
      { duration: '2m', target: 100 },
      { duration: '3m', target: 100 },
      { duration: '3m', target: 300 },
      { duration: '3m', target: 500 },
      { duration: '2m', target: 0 },
    ],
  },
  // Moderate load over a long window to surface leaks / GC / connection exhaustion.
  soak: {
    stages: [
      { duration: '2m', target: 40 },
      { duration: '1h', target: 40 },
      { duration: '2m', target: 0 },
    ],
  },
};

export const PROFILE = (__ENV.PROFILE || 'smoke').toLowerCase();
export const stages = (PROFILES[PROFILE] || PROFILES.smoke).stages;

// SLO gates — a run "fails" (non-zero exit) if these are breached.
export const thresholds = {
  http_req_failed: ['rate<0.01'],          // <1% errors
  http_req_duration: ['p(95)<800', 'p(99)<2000'],
};

export const params = {
  headers: { 'Content-Type': 'application/json' },
  // dev/self-signed certs only matter for localhost; prod/test use real certs.
};
