// Realistic participant journey — WRITES to Postgres (participant, responses, submit).
// Prod is pre-launch (no real participants), so writing to it is fine — but the guard
// stays as a footgun-stop once prod goes live. Pass ALLOW_PROD_WRITES=true to override.
//
//   k6 run -e ENV=test -e PROFILE=stress -e PROJECT_ID=1 k6/journey.js
//   k6 cloud run -e ENV=prod -e PROFILE=stress -e PROJECT_ID=1 -e ALLOW_PROD_WRITES=true k6/journey.js

import http from 'k6/http';
import { check, group, sleep, fail } from 'k6';
import { API, PROJECT_ID, isProd, stages, thresholds, params } from './lib/config.js';

if (isProd && (__ENV.ALLOW_PROD_WRITES || '').toLowerCase() !== 'true') {
  fail('journey.js writes data to prod. Re-run with -e ALLOW_PROD_WRITES=true once you have confirmed prod has no real participants.');
}

export const options = { stages, thresholds, cloud: { name: 'Synapse participant journey' } };

export default function () {
  let token;

  group('start session', () => {
    const res = http.post(`${API}/api/projects/${PROJECT_ID}/participants?languageCode=nl`, null, params);
    check(res, { 'session created (201)': (r) => r.status === 201 });
    token = res.json('participantToken');
  });

  if (!token) return; // rate-limited / blocked — nothing more to do this iteration
  const authed = { headers: { ...params.headers, 'X-Participant-Token': token } };

  let firstQuestionId;
  group('load questionnaire', () => {
    http.get(`${API}/api/projects/${PROJECT_ID}/questionnaire`, authed);
    const q = http.get(`${API}/api/projects/${PROJECT_ID}/questionnaire/questions`, authed);
    check(q, { 'questions 200': (r) => r.status === 200 });
    const questions = q.json();
    if (Array.isArray(questions) && questions.length) firstQuestionId = questions[0].questionId;
  });

  sleep(1); // think time

  if (firstQuestionId) {
    group('answer + submit', () => {
      const body = JSON.stringify({ questionId: firstQuestionId, value: 'k6 load test' });
      const r = http.post(`${API}/api/projects/${PROJECT_ID}/responses`, body, authed);
      check(r, { 'response saved (201)': (res) => res.status === 201 });
      const s = http.post(`${API}/api/projects/${PROJECT_ID}/submit`, null, authed);
      check(s, { 'submit ok': (res) => res.status === 200 });
    });
  }

  sleep(1);
}
