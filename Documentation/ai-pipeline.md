# AI Feature Pipeline

This document describes every AI-assisted feature in the SynapseEnqueteManagement
platform.

## 1. Overview

SynapseEnqueteManagement is a participation platform that collects input from young
people (the platform targets an age range of roughly 12–30) through questionnaires,
free-form idea submissions, and comments. AI is central to the platform for three
reasons. First, **moderation at scale**: a public submission box used by minors cannot
rely on manual review alone, so every piece of user-generated text and every uploaded
image passes an automated moderation pipeline before it becomes visible. Second,
**expression assistance**: many participants in the target age group find it hard to
phrase an idea or a comment clearly, so the platform offers AI writing help that
rewrites rough input without changing the participant's intent. Third,
**accessibility and reach**: speech-to-text lowers the barrier for participants who
struggle to type, image description generates alt-text, and post generation helps
organisers communicate results.

The platform contains seven AI subsystems, each documented in its own section below:

1. **Content moderation** — two-stage regex + LLM screening of text and images.
2. **Idea support** — chat-style AI writing assistance for idea submissions.
3. **Comment support** — the same writing assistance applied to comments.
4. **Image description** — AI-generated descriptions for uploaded images.
5. **Speech-to-text** — server-side audio transcription.
6. **Social media post generation** — admin tool that turns project results into
   shareable posts.
7. **Semantic embeddings** — vector representations of ideas for similarity-based
   sorting.

Two further AI services exist in the codebase and follow the exact same architecture
as the seven above (question support for questionnaire answers, and AI image
generation); they are not detailed separately here because they reuse the patterns
described in sections 2 and 10.

## 2. AI Configuration System

### The `AiConfig` / `AiConfigLoader` / `ResolvedPlatformAiConfig` pattern

All AI behaviour is driven by configuration rather than hard-coded values. Three types
work together:

- **`AiConfig`** (`BL/Ai/AiConfig.cs`) is the root configuration model. It is a plain
  object graph that maps one-to-one to a JSON file (`ai-config.json`). Each feature
  owns its own config class (`ModerationConfig`, `AiIdeaSupportConfig`,
  `AiSpeechToTextConfig`, `SocialMediaConfig`, …); `AiConfig` is purely the composition
  root that holds them all. A `PlatformConfig` block carries identity tokens — platform
  name, language, country, and age range.

- **`AiConfigLoader`** (`BL/Ai/AiConfigLoader.cs`) loads that JSON file once at
  startup. It searches a fixed list of candidate paths and uses the first file it
  finds: the path in the `AI_CONFIG_PATH` environment variable, then `ai-config.json`
  next to the assembly or in the working directory, then the committed fallback
  `ai-config.example.json` in the same two locations. If nothing is found it returns
  safe built-in defaults and logs a warning, so the application always starts.

- **`ResolvedPlatformAiConfig`** (`BL/Ai/ResolvedPlatformAiConfig.cs`) is the
  per-request, per-platform view that AI services actually receive. It is a record
  bundling the chat client to call, the selected model id, and the feature config
  blocks (moderation, idea support, comment support, question support).

### How per-platform AI settings are resolved

The static `AiConfig` is the same for the whole application instance. Per-platform
resolution happens at request time in `PlatformAiConfigService`
(`API/Ai/PlatformAiConfigService.cs`). When a controller needs AI config it calls
`ResolveAsync(platformId, ct)`. That service reads two tables from the UI-Admin
database — `SystemAiSettings` (which models are allowed) and `PlatformSettings`
(which model a specific platform selected) — picks the appropriate `IChatClient` from
a `ChatClientRegistry`, and returns a `ResolvedPlatformAiConfig`. Results are cached
for 30 seconds so that prompt or model changes made in the admin panel are picked up
quickly without a database hit on every request. If the admin database is unavailable
the service falls back to global defaults rather than failing.

The platform supports multiple AI providers. At startup, `Program.cs` registers chat
clients for Vertex AI (Google Gemini), OpenAI, Anthropic, and Grok, depending on which
API keys are configured. Every AI feature talks to the provider-neutral `IChatClient`
abstraction, so the choice of provider/model is a configuration concern, not a code
concern.

### Tunable prompts without code changes

Every prompt the platform sends to an LLM lives in the JSON config, not in C#. This
means the wording of moderation rules, the tone of the writing assistant, and the
structure of generated social posts can all be adjusted by editing the config file and
restarting the API — no recompilation. To keep the JSON readable while still dynamic,
`AiConfigLoader.ApplyPlatformSubstitutions` replaces placeholder tokens at load time:
`{platform_name}`, `{language}`, and `{country}` are substituted from the
`PlatformConfig`, and `{age_range}` is baked into the moderation prompts. Other tokens
(e.g. `{category_name}`, `{idea_text}`, `{draft}`, `{language}` for participant-facing
features) are substituted per request, because their values depend on the specific
participant, project, or category.

### The mock / real service pattern

Every AI service is defined as an interface (`IAiModerationService`,
`IAiIdeaSupportService`, `IEmbeddingService`, …) with **two** implementations: a real
one that calls a cloud LLM, and a mock one that returns scripted, deterministic
output. The DI container in `Program.cs` wires up exactly one of the two based on the
`moderation.use_ai` flag in `ai-config.json`. This is covered in detail in section 10.

## 3. Content Moderation Pipeline

Content moderation is the most elaborate AI feature in the platform, so it is
described here in full.

### Two-stage pipeline: regex pre-filter, then AI

Moderation runs in two stages. A piece of text is first screened by a deterministic
regex/banned-word filter; only if that filter passes does the text go to the LLM.

```
Participant types an idea / comment
            │
            ▼
┌─────────────────────────────┐
│  Stage 1: RegexModeration    │   no AI, no network, runs on every request
│  - structural regex patterns │
│  - fuzzy banned-word match   │   (leetspeak-normalised, Levenshtein-tolerant)
└──────────────┬───────────────┘
               │
       rejected│            passed
               ▼               │
      RejectedByRegex(reason)   ▼
                       ┌─────────────────────────────┐
                       │  Stage 2: AiModeration       │   one LLM call
                       │  - sends prompt + user text  │
                       │  - expects JSON verdict      │
                       │    { acceptable, reason }    │
                       └──────────────┬───────────────┘
                                      │
                       rejected       │        passed
                            ▼         │           ▼
                  RejectedByAi(reason) │     Acceptable()
                                      ▼
                       (AI call fails → ServiceUnavailable, fail closed)
               │
               ▼
   API issues an HMAC-signed moderation token describing the verdict
               │
               ▼
   Participant submits the idea/comment together with that token
               │
               ▼
   Submit endpoint verifies the token instead of re-running moderation
```

### Stage 1 — `RegexModerationService`

`RegexModerationService` (`BL/AiModeration/RegexModerationService.cs`) is deterministic
and has **no AI dependency**, which is why it runs first. It is cheap (no network
call), fast, and gives guaranteed catches for content the platform never wants to
allow regardless of what an LLM might say. It performs two kinds of checks:

- **Structural regex patterns** — compiled patterns from the `regex_patterns` array in
  the config (built by `RegexModerationPatterns`). Some patterns are idea-only; others
  apply to both ideas and comments.
- **Fuzzy banned-word matching** — the `banned_words` list. To resist obvious evasion,
  `ModerationUtility` normalises the input first: lower-casing, stripping accents,
  mapping leetspeak characters (`0→o`, `3→e`, `@→a`, …), and collapsing repeated
  characters. The check is then run against several normalised variants, and matching
  uses a consonant-skeleton comparison plus a length-bucketed Levenshtein distance, so
  small misspellings still trip the filter. The Levenshtein thresholds are themselves
  config values (`FuzzyConfig`).

If Stage 1 rejects the text, the pipeline stops immediately and no AI tokens are spent.

### Stage 2 — `AiModerationService`

If Stage 1 passes and `moderation.use_ai` is enabled, `AiModerationService`
(`BL/AiModeration/AiModerationService.cs`) sends the text to the LLM. The system
message is the configured moderation prompt (a separate `comment_moderation_prompt`
exists for comments), and the user message is the participant's text. The prompt
instructs the model to reply with a small JSON object — `{ "acceptable": bool,
"reason": string }`. The service strips any Markdown code fences from the reply,
deserialises that JSON, and converts it into a `ModerationResult`. The same class also
moderates **images** (`CheckImageWithAiAsync`, sending the image bytes plus a prompt)
and can produce an AI-rewritten, cleaned-up version of flagged text
(`SuggestRewriteAsync`).

Failure handling is deliberate. For text, if the AI call throws, the service "fails
closed" and returns `ServiceUnavailable()` so questionable content is not silently
accepted; if the AI returns an empty or non-JSON response it "fails open" and treats
the text as acceptable, to avoid blocking legitimate submissions on a malformed reply.
Image moderation fails closed in all error cases.

### `ModerationResult` and the static factory pattern

`ModerationResult` (`BL/AiModeration/ModerationResult.cs`) is the single result type
returned by both stages. It carries whether the content is acceptable, a
human-readable reason, and which layer flagged it. Rather than letting callers
construct it field-by-field, it exposes named static factory methods. Each factory
encodes one valid combination of fields, so an invalid result (e.g. "acceptable but
with a rejection reason") cannot be created by accident, and call sites read as
intent:

```csharp
public static ModerationResult Acceptable() =>
    new() { IsAcceptable = true };

public static ModerationResult RejectedByRegex(string reason) =>
    new() { IsAcceptable = false, Reason = reason, FlaggedBy = "Regex" };

public static ModerationResult RejectedByAi(string reason) =>
    new() { IsAcceptable = false, Reason = reason, FlaggedBy = "AI" };

public static ModerationResult ServiceUnavailable() =>
    new() { IsAcceptable = false, Reason = "AI moderation service unavailable", FlaggedBy = "Service" };
```

### `ModerationResolver` — composing the two stages

`ModerationResolver` (`BL/Implementation/ModerationResolver.cs`) is the entry point
used at submission time. It composes the AI moderation service with the token service
(see below): given submitted text and an optional token, it first tries to validate
the token, and only if no valid token is present does it re-run moderation. Note that
`AiModerationService` internally always runs the regex pre-filter before calling the
LLM, so the resolver gets the full two-stage check for free.

### The HMAC-signed moderation token

Moderation is split from submission, which raises a question: how does the submit
endpoint know that the content was actually checked, and that the participant did not
edit the text after the check passed? The answer is a signed token.

After a moderation check, `ModerationTokenService`
(`BL/Implementation/ModerationTokenService.cs`) issues a token. The token's payload is
`projectId | SHA-256(text) | status | timestamp`, and the whole payload is signed with
an HMAC-SHA256 using a server-side secret (the `ApiKey` setting). The token is then
Base64-encoded. Because it is signed, the client cannot forge a verdict; because it
embeds a hash of the exact text, the client cannot alter the text after the check;
because it embeds the `projectId` it cannot be replayed against another project; and
because it embeds a timestamp it expires (default 10 minutes). Used tokens are tracked
and rejected on reuse. There is a parallel scheme for image moderation tokens.

The idea submission endpoint (`API/Controllers/IdeasController.cs`, `CreateIdea`) never
trusts a raw verdict from the client. It calls `ModerationResolver.ResolveStatusAsync`,
which validates and then consumes the token:

```csharp
public async Task<ModerationStatus> ResolveStatusAsync(string text, string token,
    int projectId, ResolvedPlatformAiConfig platformConfig, CancellationToken ct = default,
    string respondInLanguage = null)
{
    if (!string.IsNullOrEmpty(token))
    {
        var cached = tokenService.ValidateToken(text, token, projectId);
        if (cached.HasValue)
        {
            tokenService.ConsumeToken(token);
            return cached.Value;
        }
    }
    var result = await moderationService.CheckWithAiAsync(text, platformConfig, projectId, ct,
        respondInLanguage: respondInLanguage);
    return result.IsAcceptable ? ModerationStatus.Unflagged : ModerationStatus.Flagged;
}
```

If the token is valid the cached verdict is used and no second LLM call is made; if it
is missing or invalid, moderation is re-run server-side. Either way the submitted
content cannot bypass moderation.

### Why moderation is a separate HTTP round-trip

Moderation is exposed as its own set of endpoints (`API/Controllers/ModerationController.cs`):
`POST .../moderation/regex` for an instant deterministic scan (cheap enough to call on
every debounced keystroke as the participant types), `POST .../moderation/ai` for the
full AI scan (called on submit or when typing pauses), and `POST .../moderation/suggest`
for an AI rewrite of flagged content. The participant flow is therefore: type the idea
→ moderate → receive a signed token → submit the idea together with that token. This
keeps interactive feedback fast and cheap, runs the expensive AI check at most once,
and still guarantees — via the token — that what gets stored is exactly what was
checked.

## 4. Idea and Comment Support (AI Writing Assistance)

### The problem it solves

The platform's audience includes participants who find it difficult to put an idea or
a comment into clear words. Idea support and comment support are optional AI assistants
that help a participant turn a rough thought into a well-phrased submission, without
the AI taking over: the participant always remains the author and always chooses what
to submit.

### The chat-style refinement flow

Idea support is a short, structured chat. `AiIdeaSupportService`
(`BL/AiIdeaSupport/AiIdeaSupportService.cs`) drives a conversation through phases
modelled by the `IdeaSupportPhase` enum — `Intro`, `Ideation`, `Draft`, `Confirm`,
`Error`. The participant types a rough idea, the assistant asks brief follow-up
questions, and eventually it produces a suggested rewritten draft. The AI's reply is a
JSON object `{ phase, message, draft }`: `message` is shown in the chat, and `draft`,
when present, is the polished version the participant can accept. The participant is
free to submit the AI's draft or their own original wording.

A separate, simpler `/refine` endpoint (`AiIdeaSupportService.RefineAsync`) takes an
existing draft and returns a single cleaned-up rewrite in one shot, without a
conversation.

### Conversation history model

The conversation is stateless on the server: the client sends the full history with
every turn. Each turn is an `AiSupportMessage` record (`BL/AiSupport/AiSupportMessage.cs`):

```csharp
public record AiSupportMessage(string Role, string Content);
```

`Role` is `"user"` or `"assistant"`; `Content` is the message text. The service maps
this history into provider chat messages, trims it to a configurable
`MaxHistoryTurns` so the prompt does not grow unbounded, and appends the latest user
message before sending. On the first turn it also injects a context prompt built from
the project name, project introduction, and category, so the assistant understands
what the participant is contributing to.

### Passing the participant's language

The participant's UI language is resolved by the controller from the request culture
(`CultureInfo.CurrentUICulture`) and passed into the request as a two-letter code. The
service substitutes it into the configured prompts and, importantly, also adds an
explicit system instruction that names the language in full (e.g. "You MUST respond in
Dutch for every message in this conversation"). Using the full language name removes
ambiguity regardless of the language the prompt template itself is written in.

### Comment support

Comment support (`BL/AiCommentSupport/`) is the same pattern applied to a different
context. Instead of helping write an idea, it helps a participant write a comment on an
existing idea, so its context prompt is built from the idea text and category. Its
reply shape is `{ message, suggestion, is_final }`. Both idea support and comment
support inherit the shared `ChatClientAiSupportBase`, which centralises the call to the
chat client, code-fence stripping, JSON extraction, usage logging, and a fallback reply
when the AI call fails.

## 5. Image Description

When a participant or an administrator uploads an image, the platform can generate a
textual description of it via `AiImageDescriptionService`
(`BL/AiImageDescription/AiImageDescriptionService.cs`). The image bytes and a prompt
(parameterised with the category name, category description, and target language) are
sent together to the multimodal chat model, and the model's text reply becomes the
description.

The endpoint is `POST /api/upload/describe-image` (`API/Controllers/UploadsController.cs`).
It is used in the idea-image upload flow: when an image is attached to an idea, the
generated description provides accessible alt-text and context that flows back to the
frontend as an `ImageDescriptionResultDto`. The same capability is available to
administrators uploading images. If the model returns nothing, the endpoint responds
with an "image could not be described" result rather than an error.

**Privacy consideration.** Image description sends the uploaded image to an external AI
provider (Vertex AI / the configured provider). Anyone deploying the platform should be
aware that uploaded images leave the platform's own infrastructure and are processed by
a third party. This is inherent to the feature.

## 6. Speech-to-Text

### Why it exists

Speech-to-text lets a participant dictate an idea or comment instead of typing it. This
directly serves participants with motor impairments, dyslexia, or simply the awkwardness
of typing a longer text on a phone — all common in the platform's audience.

### Server-side transcription, not the browser API

Transcription is **server-side**. The browser records audio and sends the raw bytes
(Base64-encoded) to `POST /api/projects/{projectId}/speechtotext/transcribe`
(`API/Controllers/SpeechToTextController.cs`). The server decodes the audio and
`AiSpeechToTextService` (`BL/AiSpeechToText/AiSpeechToTextService.cs`) sends it as a
binary `DataContent` part, together with a transcription prompt, to the multimodal chat
model. The browser's built-in `SpeechRecognition` API is **not** used; doing it
server-side gives consistent quality across browsers and keeps the model choice under
platform control.

### Language handling

The request carries the participant's language as a two-letter code (falling back to
the request UI culture). The service converts that code into the full English language
name via `CultureInfo` and substitutes it into the transcription prompt, so the model
knows which language to expect and produce.

### Documented failure modes

`SpeechToTextResult` returns a stable, machine-readable error code that the frontend
maps to a localised message. The handled failure modes (`SpeechToTextErrorCodes`) are:

- `unsupported_format` — the audio MIME type is not one of the accepted formats
  (webm, ogg, mp4, wav, flac, mpeg, aac).
- `empty_audio` — no audio data was received (e.g. no microphone, or nothing recorded).
- `too_long` — the recording exceeds the configured maximum size.
- `no_speech` — the model recognised no speech in the recording.
- `service_unavailable` — the transcription call failed.
- `invalid_audio_data` — the Base64 payload could not be decoded (returned by the
  controller).

Microphone-permission denial and "no microphone" surface to the server as empty or
absent audio and are reported through `empty_audio`/`invalid_audio_data`.

## 7. Social Media Post Generation

This is an **administrator** feature, not a participant feature. Once a project has run,
an organiser can ask the platform to generate a ready-to-publish social media post
summarising the results. The endpoint is
`POST /api/projects/{projectId}/social/generate` (`API/Controllers/SocialMediaController.cs`).

The supported platforms are defined by the `SocialPlatform` enum
(`BL/AiSocialMedia/SocialPlatform.cs`): **LinkedIn, Facebook, and Instagram**. Each has
its own system prompt, user-prompt template, and character limit in the config.

The input is project data: the project name, topic, introduction, organisation name,
and a `SocialPostStats` block (view count, completion count, completion rate, top idea
descriptions, and top category names) gathered by the controller. `AiSocialPostService`
(`BL/AiSocialMedia/AiSocialPostService.cs`) fills the platform-specific template with
these values and asks the model for a JSON reply `{ copy, hashtags }`. The output is a
`SocialPostReply` containing the post copy (truncated to the platform's character
limit), a list of suggested hashtags, and the final character count — platform-specific
copy ready for the organiser to review and publish.

## 8. Semantic Embeddings

### What is embedded and why

An embedding is a numeric vector that represents the meaning of a text. The platform
embeds the **description of each idea**. `AiEmbeddingService`
(`BL/Embeddings/AiEmbeddingService.cs`) wraps a provider embedding generator and turns
an idea's text into a vector. These vectors power the embedding-based idea sort
strategies (`SimilarSortStrategy`, `DifferentSortStrategy`, `DiverseSortStrategy`
registered in `Program.cs`), which let participants browse ideas by semantic similarity
or diversity rather than only by date — a lightweight form of deduplication and
similarity search over submissions.

### Storage: the `vector(3072)` column

The `Idea` entity (`Domain/Project/Entities/Idea.cs`) has an `Embedding` property of
type `float[]`. In PostgreSQL production this is mapped to a native
`vector(3072)` column (`DAL/SynapseEnqueteContext.cs`): 3072 is the dimensionality of
the production embedding model, `gemini-embedding-001`. In the SQLite development
database there is no vector type, so the same property is stored as JSON text and
sorting is done in memory.

### pgvector in production

Vector columns and similarity operators are provided by the **pgvector** PostgreSQL
extension. The application ensures it is present by running
`CREATE EXTENSION IF NOT EXISTS vector;` at startup before creating the schema, and the
EF Core model declares the extension via `HasPostgresExtension("vector")`. New ideas
are embedded on creation, and a startup backfill generates embeddings for any seeded
ideas that do not yet have one.

### `MockEmbeddingService` for development

`MockEmbeddingService` (`BL/Embeddings/MockEmbeddingService.cs`) is the
development/CI substitute used when AI is disabled, so no embedding API key or network
call is needed. It is deterministic and dependency-free: it hashes the words of the
text into a fixed-size, L2-normalised float vector. The same text always produces the
same vector, and texts sharing words produce vectors with higher cosine similarity, so
the embedding-based sort strategies still function — but the result is a bag-of-words
stand-in, not a true semantic embedding. Empty input yields a zero vector.

## 9. AI Usage Tracking and Cost Monitoring

Every billable AI call is recorded so that cost is visible and attributable.

### What is tracked

`AiUsageManager` (`BL/AiUsage/AiUsageManager.cs`) writes an `AiUsageEvent`
(`Domain/Ai/Entities/AiUsageEvent.cs`) row for each call. An event records the project
it belongs to, the service kind (`AiServiceKind` — moderation, idea support, comment
support, speech-to-text, image description, embedding, social post, …), the model id,
the input and output token counts, a request count, the computed cost in euros, and a
timestamp. Cost is computed by `AiPricing` from a per-model price table
(`AiPricingConfig`, bound from configuration) — input, output, and a separate
audio-input rate for speech-to-text are each priced per million tokens.

Logging is best-effort: every AI service wraps the `LogChatAsync` call in a try/catch
so that a failure to record usage never breaks the user-facing feature. Calls made with
a non-positive project id (system-level work such as the seed-time embedding backfill)
are treated as not attributable and are not billed.

### The admin cost dashboard

`AiUsageManager` exposes a set of read-only aggregation queries — totals per project,
per platform, and per service — which back the cost-monitoring views in the admin
panel. An administrator can see, for any date range, how much each platform and each AI
feature is costing.

### Why this matters

The platform is a multi-tenant SaaS product: several organisations run their projects
on the same instance. AI calls cost real money, and that money has to be attributed to
the right tenant. Per-project and per-platform usage tracking makes AI spend
transparent, supports billing or quota decisions, and lets operators spot a feature or
a platform whose AI usage is unexpectedly high.

## 10. Mock vs Real Services

### How the toggle works

The single switch is the `moderation.use_ai` boolean in `ai-config.json`. `Program.cs`
reads it into a `useAi` flag *before* building the DI container. When `useAi` is true,
the real provider clients are constructed (Vertex AI, and optionally OpenAI, Anthropic,
and Grok if keys are present) and the real AI service implementations are registered.
When it is false, no provider client is created and the mock implementation of every AI
service is registered instead. Switching the whole AI layer between real and mock is
therefore a one-line config change plus a restart.

### Why every AI service has a mock

- **Local development needs no API keys.** A developer can clone the repository, leave
  AI disabled, and run the full application — chat assistants, moderation, embeddings,
  image and audio features — without any cloud credentials or cost.
- **Tests are deterministic.** Mocks return scripted, repeatable output, so automated
  tests do not depend on a network call or on a non-deterministic model.
- **The frontend contract is exercised end to end.** Each mock returns data in exactly
  the same shape as the real service, so UI work against the mock is faithful to
  production behaviour.

### The mock implementations

The codebase contains a mock for every AI service:

- `MockModerationService`
- `MockIdeaSupportService`
- `MockCommentSupportService`
- `MockQuestionSupportService`
- `MockSpeechToTextService`
- `MockImageDescriptionService`
- `MockImageGenerationService`
- `MockSocialPostService`
- `MockEmbeddingService`

Each implements the same interface as its real counterpart, so no code outside the DI
registration in `Program.cs` is aware of which implementation is active.

## 11. Secrets and Credentials

The AI pipeline depends on several secrets — most importantly the HMAC signing key
used by the moderation token service and the optional API keys for third-party LLM
providers. How these reach the running application differs by environment, and the
indirection is deliberately kept out of the application code.

### The kinds of secrets in play

- **`ApiKey`** — a single shared secret with two roles. It is the HMAC key that signs
  every moderation token (`ModerationTokenService`), and it is also the value that
  must appear in the `X-Api-Key` header on every incoming request
  (`ApiKeyMiddleware`). The application refuses to start if `ApiKey` is missing.
- **`DB_HOST` / `DB_PASSWORD`** — PostgreSQL connection details for the production
  database (Cloud SQL).
- **`OpenAi:ApiKey`, `Anthropic:ApiKey`, `Grok:ApiKey`** — optional per-provider API
  keys. When present they unlock those providers in the `ChatClientRegistry`; when
  absent the provider is silently skipped and the app continues to work with whichever
  providers do have keys.
- **Vertex AI** — uses no secret string. Authentication is via Google **Application
  Default Credentials**, i.e. whichever service account is attached to the runtime
  environment (local `gcloud auth application-default login` for developers; the VM's
  attached service account in production).

### Local development

Secrets live in `API/appsettings.Development.json` (committed, with non-sensitive
placeholder values such as `"ApiKey": "REDACTED"`) and/or .NET user secrets
for anything a developer does not want to commit. Vertex AI uses the developer's
`gcloud` credentials. When `moderation.use_ai = false` in `ai-config.json`, no LLM
provider client is built and no provider keys are read at all — the mock services
take over, so a contributor can run the full application without any cloud
credentials.

### Cloud Run test environment

The temporary test deployment described in `cloudrun-test-api.yaml` passes
configuration as plain `env:` entries on the container spec. The values there
(`dev-api-key-synapse-2026`, the test database password, etc.) are intentionally
non-sensitive test credentials scoped to that ephemeral environment.

### Production — Google Secret Manager via the VM startup script

In production the application runs in a Docker container on a Google Compute Engine
VM. Secrets are stored in **Google Secret Manager** and pulled into the container by
the VM startup script `infra/startup-api.sh`. The .NET application itself does **not**
talk to Secret Manager: from its point of view it is reading ordinary environment
variables, and the entire Secret Manager indirection lives in the shell script.

The flow is:

1. **Authenticate using the VM's service account.** The script queries the GCE
   metadata server at `metadata.google.internal` to obtain a short-lived OAuth access
   token for the service account attached to the instance. No key file is mounted, no
   `gcloud auth` runs — the token is fetched on demand.

2. **Read the environment selector.** The script reads an `env` attribute (and the
   `project-id`) from instance metadata. That selector — `prod`, `test`, `staging`,
   etc. — both decides the image tag to deploy and namespaces the secret names:
   `prod-api-key`, `test-api-key`, and so on can coexist in the same Secret Manager
   project.

3. **Fetch each secret over the REST API.** A `get_secret` helper calls
   `https://secretmanager.googleapis.com/v1/projects/{PROJECT_ID}/secrets/{ENV}-{name}/versions/latest:access`
   with the metadata token, base64-decodes the payload and trims trailing `\r`. Five
   secrets are pulled this way: `db-host`, `db-password`, `api-key`, and `gcs-bucket`
   (each prefixed with the environment).

4. **Inject the secrets as container environment variables.** The script then runs
   `docker run` with `-e DB_HOST=...`, `-e DB_PASSWORD=REDACTED, `-e ApiKey=REDACTED,
   `-e GCS_BUCKET=...` populated from the fetched values, alongside the static
   settings (`ASPNETCORE_ENVIRONMENT=Production`, `AllowedHosts=*`, etc.). The .NET
   configuration system then reads those env vars exactly as it would in any other
   environment.

### Why the indirection lives in the startup script

Keeping Secret Manager out of the application code has two benefits. First, the .NET
code is portable: the same image runs locally with values from
`appsettings.Development.json`, on Cloud Run with inline env values, and on GCE with
values from Secret Manager, without any conditional configuration providers. Second,
secret rotation is an infrastructure concern: rotating a secret means publishing a new
version in Secret Manager and restarting the VM, with no code change or redeploy.
