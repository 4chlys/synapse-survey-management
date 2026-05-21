# Authentication & Security

This document describes the authentication and security model of the SynapseEnqueteManagement
platform.

## 1. Overview

The platform serves two completely separate user groups, and each has its own authentication
model — by design, not by accident.

**Participants** are young people (the platform targets roughly the 12–30 age range) who fill in
questionnaires, submit ideas, and post comments. They are **anonymous by design**. There is no
account, no password, and no login screen. This is a deliberate privacy choice: the platform
collects input from minors, and asking them to identify themselves would both discourage
participation and create a needless personal-data liability under the GDPR. A participant is
therefore represented by a *pseudonymous* identifier — a random token — and nothing more.

**Administrators** are the people who run platforms and projects. They need the opposite:
a verifiable identity, a password, and role-based permissions, because they create projects,
read aggregated results, and change platform settings. Administrators therefore use a
conventional account system (ASP.NET Identity) with roles.

These two models never mix. The participant-facing application (`UI-User`) and the admin panel
(`UI-Admin`) are separate ASP.NET MVC applications, and the shared backend (`API`) is a third
application that both of them call. The sections below describe each layer in turn.

## 2. Participant identification — session tokens

### No login, no password

Participants are never asked to register. There is no participant table of usernames or
password hashes, and ASP.NET Identity is not used for them at all. Instead, a participant is
identified by a single random value.

### The session token

The `Participant` domain entity (`Domain/Project/Entities/Participant.cs`) carries a
`SessionToken` of type `Guid`, generated automatically when the entity is created:

```csharp
public Guid SessionToken { get; set; } = Guid.NewGuid();
```

A GUID is a 128-bit value (122 bits of randomness). It is effectively unguessable, which is the
only property the token needs — see *"Why a GUID is enough"* below.

### What the `Participant` entity stores

The entity holds only what the platform genuinely needs:

| Field          | Purpose                                                           |
|----------------|-------------------------------------------------------------------|
| `ParticipantId`| Internal database key.                                            |
| `SessionToken` | The pseudonymous identifier (GUID).                               |
| `CreatedAt`    | When the session started.                                         |
| `HasSubmitted` | Whether the participant finished the questionnaire.               |
| `CompletedAt`  | When they finished (nullable).                                    |
| `Email`        | **Optional** — only filled if the participant *chooses* to leave contact info; validated with `[EmailAddress]` and `[MaxLength(200)]`. |
| `Language`     | The participant's chosen language.                                |
| `Project` / `Questionnaire` | Which survey this session belongs to.                |

There is no name, no date of birth, no address — nothing that identifies a real person. The
`Participant` row is a *pseudonym*: it groups one person's answers together for the duration of
a survey, but on its own it points to nobody. The single exception is `Email`, and that field
is only populated when a participant deliberately enters contact details (for example, to be
contacted about the results) — it is never collected silently.

### How the token travels

The token is created and then carried back and forth between three places:

1. **Creation.** When a participant starts a survey, `UI-User`'s `SurveyApiController.StartSession`
   calls the API, which creates a `Participant` and returns its `SessionToken`.
2. **Browser storage.** `CookieSessionService.SetToken` stores the token in an HttpOnly cookie
   named `ParticipantToken_{projectId}`. The cookie name is **per project**, so a participant
   who takes part in two different projects holds two independent sessions and the two cannot
   be correlated through the cookie.
3. **API calls.** When `UI-User` calls the backend on the participant's behalf, the
   `AuthenticatedApiService` base class reads the token from the cookie and attaches it as the
   `X-Participant-Token` request header.

### How the backend resolves the token

Every participant-aware API controller derives from `ParticipantControllerBase`. Its job is to
turn the incoming header back into a `Participant`:

```csharp
protected bool TryGetParticipantToken(out Guid token)
{
    token = Guid.Empty;
    if (!Request.Headers.TryGetValue("X-Participant-Token", out var headerValue))
        return false;
    return Guid.TryParse(headerValue.ToString(), out token);
}
```

`GetParticipantAsync` then looks the `Participant` up through `IParticipantManager`
(`ParticipantManager.GetParticipantBySessionTokenForUpdateAsync`), which queries the database by
session token. A request with a missing or unparsable token receives **401 Unauthorized**; a
well-formed token that matches no participant receives **404 Not Found**.

There is one extra check worth noting. The overload `GetParticipantAsync(int projectId)`
verifies that the resolved participant actually belongs to the project named in the route, and
returns **403 Forbidden** otherwise. This prevents a token issued for one project from being
replayed against another.

### Cookie settings

`CookieSessionService` sets the participant cookie with:

- **`HttpOnly = true`** — the cookie is invisible to JavaScript, so a cross-site scripting bug
  cannot read the token out of `document.cookie`.
- **`Secure = isHttps`** — in production the application runs behind HTTPS (the
  `X-Forwarded-Proto` header is honoured), so the cookie is marked Secure and is only sent over
  encrypted connections. In local development over plain HTTP the flag is omitted so the cookie
  still works.
- **`SameSite = Lax`** — the cookie is not sent on cross-site sub-requests, which blunts
  cross-site request forgery.
- **`MaxAge = 365 days`** — a participant can leave and come back to an unfinished survey.

### Why a GUID is enough

A bare random token, with no signature and no password, may look weak. It is not, because of
*what a participant could gain by stealing one*. Every submission — every idea, comment, and
answer — is anonymous. There is no profile to take over, no balance to drain, no private
message to read. Impersonating another participant would let an attacker, at most, see that
participant's own ideas marked as "mine" — information of no value. The token is a *capability*
to continue one anonymous survey session, nothing more. Combined with the unguessable size of
a GUID and the HttpOnly + Secure cookie, that is a proportionate design.

## 3. Admin authentication

The admin panel (`UI-Admin`) uses a conventional, identity-based model.

### ASP.NET Identity

`UI-Admin/Program.cs` registers ASP.NET Identity with `ApplicationUser` and `IdentityRole`:

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit          = true;
    options.Password.RequiredLength        = 8;
    options.Password.RequireUppercase      = true;
    options.Password.RequireNonAlphanumeric = false;
})
```

Passwords are stored as salted hashes by ASP.NET Identity — the framework never stores a
plaintext password. Sign-in goes through `SignInManager.PasswordSignInAsync` with
`lockoutOnFailure: true`, so repeated failed logins lock the account; the login screen reports
this with a localised "account locked" message. The authentication cookie is configured to
expire after **8 hours**, and the framework's default protections apply (the cookie is HttpOnly
and is encrypted/signed by the ASP.NET Data Protection system).

### A separate, migrated database

Admin identity data lives in its own `AdminDbContext`. This is the **only** database in the
project that is managed with EF Core migrations: `UI-Admin` runs `Database.MigrateAsync()` on
startup. The participant-facing application database, by contrast, is recreated and reseeded on
every version change and uses no migrations. Keeping the admin store on migrations means admin
accounts and their password hashes survive deployments.

### Roles

`AdminSeeder` creates two roles:

- **`SystemAdmin`** — operates the whole platform (manages platforms, global AI settings).
- **`ProjectPlatformAdmin`** — manages the projects of one specific platform.

A `SystemAdmin` account is seeded from configuration (`SystemAdmin:Email` / `SystemAdmin:Password`),
and a `ProjectPlatformAdmin` is seeded per platform. After login, `AuthController.RedirectAfterLogin`
sends each role to its own landing area; an account with no recognised role is signed out
immediately to avoid a redirect loop.

### How admin routes are protected

The pipeline calls `UseAuthentication()` and `UseAuthorization()`, and controllers are guarded
with `[Authorize]` attributes that name the required role, for example:

- `ProjectController` → `[Authorize(Roles = "ProjectPlatformAdmin")]`
- `SettingsController` → `[Authorize(Roles = "ProjectPlatformAdmin")]`
- `PlatformController` → `[Authorize(Roles = "SystemAdmin")]`
- `HomeController` → `[Authorize]`

Only the login/logout actions in `AuthController` are marked `[AllowAnonymous]`. The login
action additionally validates any `returnUrl` with `Url.IsLocalUrl`, which prevents an
open-redirect attack (a crafted login link that bounces the admin to an external site after
sign-in).

## 4. The moderation token — HMAC-signed proof of moderation

This is the most security-critical piece of the participant flow, so it is worth explaining
carefully.

### Why it exists

Before an idea or comment becomes visible, it must pass content moderation (a regex scan and an
AI scan — see section 6). The moderation check happens on one API endpoint; the *save* happens
on a different one. Nothing stops a malicious client from skipping the first endpoint entirely
and calling the save endpoint directly with any text it likes.

The moderation token closes that gap. The moderation endpoint hands the client a small signed
token that says *"this exact text was moderated, here is the result"*. The save endpoint
refuses to trust a moderation result unless it arrives as a valid token. Because the token is
signed with a server-side secret, the client can hold it and send it back, but it can neither
forge one nor alter the text it covers.

### The flow

```
  ┌────────────┐     1. draft text      ┌──────────────────────┐
  │ Participant │ ─────────────────────▶ │ ModerationController  │
  │  (browser)  │                        │  /moderation/ai       │
  │             │ ◀───────────────────── │  → runs moderation    │
  │             │   2. result + TOKEN    │  → signs a token      │
  │             │                        └──────────────────────┘
  │             │
  │             │     3. submit idea     ┌──────────────────────┐
  │             │        text + TOKEN    │ IdeasController       │
  │             │ ─────────────────────▶ │  POST .../ideas       │
  │             │                        │  → verifies HMAC      │
  │             │ ◀───────────────────── │  → verifies text hash │
  │             │   4. created / error   │  → consumes token     │
  └────────────┘                        └──────────────────────┘
```

1. The participant's browser sends the draft text to `ModerationController` (`/moderation/ai`
   or `/moderation/regex`).
2. The controller moderates the text and calls `ModerationTokenService.GenerateToken`, which
   returns a signed token encoding the moderation result. The token goes back to the browser
   alongside the human-readable result.
3. When the participant submits the idea, the browser sends the idea text **and** the token to
   `IdeasController`.
4. `IdeasController` passes both to `ModerationResolver`, which calls
   `ModerationTokenService.ValidateToken`. If the token is valid, the cached moderation result
   is trusted and the token is consumed; if it is not valid, the resolver simply runs a fresh
   AI moderation check instead of trusting the client. (For uploaded *images*, an invalid token
   is rejected outright with **400 Bad Request**.)

### What is signed

`GenerateToken` builds a pipe-separated payload and appends an HMAC over it:

```csharp
public string GenerateToken(string text, ModerationStatus status, int projectId)
{
    var textHash  = ComputeTextHash(text);                       // base64( SHA-256(text) )
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var payload   = $"{projectId}|{textHash}|{(byte)status}|{timestamp}";
    var hmac      = ComputeHmac(payload);                        // HMAC-SHA256(payload)
    return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{payload}|{hmac}"));
}
```

The payload binds four facts together:

- **`projectId`** — so a token issued for one project cannot be reused on another (the service
  comments call this *Cross Project Moderation Forging* prevention).
- **`textHash`** — a SHA-256 hash of the moderated text, so the token only covers *that exact
  text*; changing one character invalidates it.
- **`status`** — the moderation outcome (e.g. *Unflagged*, *Flagged*).
- **`timestamp`** — when it was issued, used for expiry.

Verification (`ValidateToken`, abridged below — parsing and exception handling omitted) reruns
every check:

```csharp
// reject already-used tokens
if (_consumedTokens.ContainsKey(token)) return null;
// ... decode and split payload into projectId | textHash | status | timestamp | hmac ...
if (tokenProjectId != projectId) return null;                    // right project
if (!CryptographicOperations.FixedTimeEquals(                    // signature intact
        Encoding.UTF8.GetBytes(ComputeHmac(payload)),
        Encoding.UTF8.GetBytes(hmac)))
    return null;
if (ComputeTextHash(text) != textHash) return null;              // text unchanged
var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp;
if (age > (maxAge ?? DefaultMaxAge).TotalSeconds || age < 0) return null;  // not expired
return (ModerationStatus)statusByte;
```

Three details matter for security:

- **HMAC comparison uses `CryptographicOperations.FixedTimeEquals`**, a constant-time compare,
  so an attacker cannot learn the correct signature byte-by-byte through timing.
- **Tokens expire.** `DefaultMaxAge` is **10 minutes**. A token older than that (or with a
  future timestamp) is rejected.
- **Tokens are single-use.** After a successful submission the token is passed to
  `ConsumeToken`, which records it in an in-memory set; a replayed token is then rejected by the
  first line above.

A separate pair of methods, `GenerateImageToken` / `ValidateImageToken`, applies the same
construction to uploaded images, hashing the image bytes instead of text.

### The HMAC key

The HMAC is keyed with the **`ApiKey`** configuration value (see section 5). Because both the
front-ends and the API share that one secret, any instance of the API can verify a token signed
by any other instance — the design is stateless and needs no shared database for token storage.

## 5. API key management

### What the API key is

The `ApiKey` setting plays two roles:

1. **A shared secret protecting the whole backend.** `ApiKeyMiddleware` runs on every API
   request and checks the `X-Api-Key` header. The participant and admin front-ends attach this
   header to every call they make to the API; a request without the correct key is rejected
   with **401 Unauthorized**. (The `/health` endpoint is exempt so load balancers can probe it.)
   The comparison uses `CryptographicOperations.FixedTimeEquals` — again, a constant-time
   compare to avoid timing leaks.
2. **The HMAC signing secret** for the moderation tokens of section 4.

### Where it lives

- **Development:** `API/appsettings.Development.json` contains a throwaway value
  (`"dev-api-key-local"`). This is fine because it only ever protects a local machine.
- **Production:** the key is supplied through an environment variable (or .NET user secrets /
  a secret manager). It is deliberately **absent** from `appsettings.Production.json`. Both the
  API and the front-ends fail fast at startup with `InvalidOperationException` if no key is
  configured, so a misconfigured deployment cannot run *without* a key.

### Why it must never be committed

If the production `ApiKey` were committed to the repository, anyone with read access to the
source — now or in the Git history forever — could do two things. First, call the API directly,
bypassing the front-ends entirely. Second, and worse, **forge moderation tokens**: knowing the
HMAC key lets an attacker mint a token that claims any text was "Unflagged", which would let
unmoderated content through the submit endpoint. The key is therefore treated as a production
secret and injected only at deploy time. (The project's operational checklist explicitly lists
"set `ApiKey` via environment variable or user secrets" as a release step.)

## 6. Input validation and content security

Security at the data layer is layered: the same input is checked more than once, at different
levels.

### Model validation (controllers)

Request DTOs carry data annotations — `[Required]`, `[MaxLength]`, `[Range]`, `[EmailAddress]`
— and controllers reject malformed requests before any business logic runs. Both MVC
front-ends also register `AutoValidateAntiforgeryTokenAttribute` as a global filter, so every
state-changing form post must carry a valid anti-forgery token (see section 9).

### Business-layer validation (managers)

Validation is not left to the controllers alone. Managers in the business layer revalidate
through `EntityValidator` before persisting. For example, `ParticipantManager.SaveContactInfoAsync`
calls `EntityValidator.ValidateProperty` on the email *and* `AddParticipantAsync` validates the
whole `Participant` entity — so even a caller that reached the manager directly cannot store an
invalid entity.

### Content moderation as a security layer

Because the platform accepts free-text and image submissions from minors, every submission runs
through a **two-stage moderation pipeline**: an instant regex/banned-word scan, followed by a
full AI (LLM) scan. This is what the moderation token of section 4 protects. The pipeline is
described in detail in `Documentation/ai-pipeline.md`.

### File upload security

`UploadsController` constrains uploads tightly:

- **MIME whitelist.** Image uploads are restricted to `image/jpeg`, `image/png`, `image/webp`,
  and `image/gif`. Notably, **SVG is excluded** — an SVG file can carry embedded scripts and is
  a known XSS vector, so allowing it on a public upload endpoint would be unsafe.
- **Size limits.** Images are capped at 10 MB, videos at 25 MB, audio at 20 MB, enforced both
  by `[RequestSizeLimit]` attributes and by an explicit length check. Kestrel itself caps the
  request body at 30 MB.
- **AI image moderation.** Idea images are additionally screened by the AI moderation service,
  and the result is bound into an HMAC image token (section 4) so the submit endpoint can trust
  the verdict. Rejected images can still be stored, but the idea is forced to `Flagged` so an
  admin must review it before it is shown.

### Localised validation messages

Validation error messages are localised (`AddDataAnnotationsLocalization` is registered in both
front-ends), so a participant sees errors in their own language. The internationalisation
mechanism is documented in `Documentation/i18n.md`.

## 7. Host header injection prevention

ASP.NET Core's `AllowedHosts` setting controls which `Host` headers the application will serve.

In development, `UI-User/appsettings.json` sets `AllowedHosts` to `"*"`, which accepts any host
— convenient for local work and tunnels. The API's `appsettings.Production.json`, by contrast,
pins it to the real domains (`synapse-ip.xyz`, `api.synapse-ip.xyz`, and the Cloud Run
`*.run.app` hosts).

**The attack.** If an application echoes the incoming `Host` header into absolute URLs — in
password-reset links, redirects, or cached responses — an attacker can send a request with a
forged `Host` header and make the application generate links pointing at an attacker-controlled
domain (host-header injection / web-cache poisoning). Restricting `AllowedHosts` to the genuine
production domains makes the framework reject any request whose `Host` does not match.

For this reason, the production checklist requires that the participant front-end's
`AllowedHosts` be changed from `"*"` to the actual production domain(s) before go-live — the
`"*"` value is a development convenience, not a production setting.

## 8. Security in CI/CD

The GitLab pipeline (`.gitlab-ci.yml`) builds, scans, signs, and deploys the four container
images. Several security practices are visible in it.

### Secrets are CI/CD variables, never code

No secret is written into the pipeline file. The container-registry credentials
(`GCR_SERVICE_ACCOUNT_KEY`), the image-signing key and its password (`COSIGN_KEY_B64`,
`COSIGN_PASSWORD`), and the GCP project id (`PROJECT_ID`) are all referenced as GitLab CI/CD
variables — values stored in GitLab's protected settings and injected into the job environment
at run time.

### Workload Identity Federation for deployment

The deploy jobs do **not** authenticate to Google Cloud with a long-lived service-account key.
Instead they use **Workload Identity Federation**: GitLab issues a short-lived OIDC token
(`GCP_ID_TOKEN`, with an `aud` pointing at a `gitlab-pool` / `gitlab-provider` workload-identity
pool), and `gcloud auth login --cred-file=gcp-wif-test.json` / `gcp-wif-prod.json` exchanges it
for a temporary GCP credential. Short-lived federated tokens are safer than a static key file:
there is no long-lived credential to leak, and access is scoped to the pipeline.

### Container image scanning

Every image is scanned in a dedicated `scan` stage with **Trivy**. The scan
(`--severity HIGH,CRITICAL --exit-code 1 --ignore-unfixed`) fails the build on any fixable
high- or critical-severity vulnerability — `allow_failure: false`, so a vulnerable image cannot
progress to deployment. Each scan also emits an SBOM (`spdx-json`) as a build artifact.

### Image signing

In production, the `sign` stage uses **cosign** to cryptographically sign the API, UI, and
admin images after they are pushed. A signed image lets the deployment target verify that an
image really came from this pipeline and was not tampered with in the registry.

## 9. Known limitations and honest gaps

In the interest of an honest assessment, this section lists what is partial or absent.

- **Rate limiting is present but per-instance.** Both front-ends configure per-IP fixed-window
  rate limiters on the expensive AI endpoints and on participant creation (rejecting with
  **429 Too Many Requests**). However, the limiter state is held in process memory, so behind a
  multi-instance managed instance group each instance counts independently, and the effective
  limit scales with the instance count. The limiters also key on the client IP, which a
  determined attacker behind multiple addresses can spread across.
- **The single-use token store is per-instance.** `ModerationTokenService` tracks consumed
  tokens in an in-memory `ConcurrentDictionary`. Across multiple API instances, a token
  consumed on one instance is still unknown to the others, so within its 10-minute lifetime a
  token could in principle be replayed against a different instance. The HMAC, project binding,
  text-hash binding, and expiry still hold; only the *single-use* guarantee is best-effort.
- **CSRF protection is present** for the MVC front-ends — `AutoValidateAntiforgeryTokenAttribute`
  is a global filter in both `UI-User` and `UI-Admin`. The API itself does not use anti-forgery
  tokens, but it does not need to: it authenticates with the `X-Api-Key` and `X-Participant-Token`
  headers rather than ambient cookies, and a custom header cannot be set by a cross-site form.
- **No Content-Security-Policy header.** The applications do not emit a CSP response header. A
  CSP would add defence-in-depth against XSS on top of the existing HttpOnly cookies and the
  SVG-exclusion on uploads.
- **A static, shared API key.** `X-Api-Key` is a single secret shared by all callers, with no
  per-client keys and no documented rotation procedure. If it leaks, the only remedy is to
  rotate it everywhere at once.
- **Seeded credentials.** `AdminSeeder` creates each platform admin with a fixed default
  password (`Admin1234!`). This is acceptable for seeding a test environment but those accounts
  must have their passwords changed before any real use.
- **Registry credentials are still a long-lived key.** While *deployment* uses Workload Identity
  Federation, the container-registry login still uses a long-lived service-account key
  (`GCR_SERVICE_ACCOUNT_KEY`). Moving registry access onto federation as well would remove the
  last long-lived credential from the pipeline.

None of these are unaddressed holes in the core flow; they are the natural next steps for
hardening a platform that already has a coherent, layered security model.
