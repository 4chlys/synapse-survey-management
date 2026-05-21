# Survey Flow & Branching Engine

This document describes how the participant-facing survey works: how a questionnaire is
modelled, how branching decides which questions a participant sees, how the front-end
renders that path slide by slide, and how responses are validated, persisted and finally
submitted.

The audience is technical readers (professors, clients) who are not familiar with the
codebase. Everything below is derived from the actual source files; nothing is invented.

## 1. Overview

A *Project* contains exactly one *Questionnaire*. The questionnaire owns a flat list of
*Questions* (and optional *Sections* that group questions for the admin UI). A
*Participant* who fills in the survey produces one *Response* per answered question.
Questions can be nested under a parent choice, which is how conditional branching is
expressed: a child question only becomes visible when its parent was answered with the
matching choice.

A custom engine was built instead of a generic form tool because the platform needs:

- **Multi-language responses**, with the participant able to switch languages mid-survey
  (responses are re-translated, see `ResponseManager.RetranslateResponsesForLanguageAsync`).
- **AI-assisted authoring** for the admin (the `QuestionSupportController` chat is used
  while designing questions, not while answering them).
- **A specific UX for the target age group**: a full-screen "scroll form" with one
  question per slide and reels-style gestures (`UI-User/src/survey/gestures.ts`), plus
  a chat-bubble alternative (`Chatform.cshtml` + `chat.ts`) that the project owner can
  pick via `Project.FormType`.

## 2. Data model

### Entity hierarchy

```
Project
└── Questionnaire (1-to-1 with Project)
    ├── Section[]            ← grouping only; not used for branching
    ├── Question[]            ← flat list scoped to the questionnaire
    │   ├── ParentQuestion?   ← optional parent (self-reference)
    │   ├── ChoiceIndex?      ← which parent answer makes this child visible
    │   ├── EndingAnswer[]    ← answers that terminate the survey
    │   └── Translation[]     ← per-language text + answer choices
    └── Participant[]         ← people filling in this questionnaire
        └── Response[]        ← one row per answered question
```

Source: `Domain/Project/Entities/Questionnaire.cs`, `Section.cs`, `Question.cs`,
`Response.cs`, `EndingAnswer.cs`, `Participant.cs`.

### Question types

The four supported formats are defined in `Domain/Project/Enums/QuestionFormat.cs`:

| Value            | Numeric | UI widget                     |
| ---------------- | ------- | ----------------------------- |
| `Open`           | 1       | `<textarea>` (≤ 500 chars)    |
| `SingleChoice`   | 2       | Radio buttons                 |
| `MultipleChoice` | 3       | Check-boxes                   |
| `Likert`         | 4       | 1–10 slider (`<input type=range>`) |

Two other enums sit alongside:

- `QuestionMediaType` — `Image | Video | Audio`. Media is rendered above the question
  text and can be a hosted file or an embedded URL (`MediaIsEmbed`).
- `FormType` — `VerticalScroll | Chat`. Picks one of two front-end layouts.

### Ordering

Position is stored as an integer `Position` on `Question` and is **scoped per sibling
group**, not globally. Top-level questions share one ordering; the children that belong
to choice X of a given parent share their own ordering; etc. `QuestionnaireManager`
maintains this via `RepackScopeAsync`, which renumbers a sibling group to 1..N whenever
a question is added, moved, or deleted.

Sections have no `Order` field — they are only used to label and group questions in the
admin and survey UI; they do not influence the path.

### Branching fields on `Question`

```csharp
public Question  ParentQuestion { get; set; }   // optional parent question
public int?      ChoiceIndex    { get; set; }   // which parent choice activates this child
public ICollection<Question>     Children       { get; set; }
public ICollection<EndingAnswer> EndingAnswers  { get; set; }
```

A child question is only visible when:

1. its `ParentQuestion` has been answered, and
2. the selected answer corresponds to its `ChoiceIndex`.

For `SingleChoice` / `MultipleChoice`, `ChoiceIndex` is the 0-based index of the chosen
option in the parent's `AnswerChoices` list (see `QuestionnaireManager.ValidateParentChoiceIndex`).
For `Likert`, the same field stores the 0-based scale point (the UI shows 1..N, the engine
subtracts 1 when matching — `SurveyFlowManager.cs:122`).

`EndingAnswer` is the second branching primitive. Each `Question` can declare a list of
ending answers; if the participant selects any of them, traversal stops and the survey
is treated as complete.

### `Response`

```csharp
public class Response : IHasCreatedAt
{
    public int       ResponseId  { get; set; }
    public DateTime  CreatedAt   { get; set; }
    public string    Value       { get; set; }    // raw text, ≤ 500 chars
    public bool      IsCommitted { get; set; }    // false while draft, true after Submit
    public Question  Question    { get; set; }
    public Participant Participant { get; set; }
    public Language   Language   { get; set; }    // language at save time
}
```

`Value` stores the raw answer:

- `Open` — the free text.
- `SingleChoice` — the selected choice's **translated text**.
- `MultipleChoice` — the selected choices joined with `", "`.
- `Likert` — the numeric position as a string (`"1"` .. `"N"`).

`Language` records the participant's language at the moment the answer was saved.
That matters because the response value for choice/Likert questions is the translated
choice text — so a later language switch needs to remap the saved value to the new
language (handled by `ResponseManager.RetranslateResponsesForLanguageAsync`, and read
during branching by `SurveyFlowManager.ComputePathAsync`).

`IsCommitted` is `false` for every saved-but-not-final answer and is flipped to `true`
at submit time, but only for responses whose question is still on the participant's
visible path (so answers to questions that branching skipped are kept in the DB as
draft but never marked as final — see `ResponseManager.CommitResponsesAsync` and
`ResponseRepository.ReadCommittedByProjectWithQuestionReadOnlyAsync`).

## 3. Branching logic

### What branching means

The participant answers question A. Based on the answer, the engine decides whether to
show child question B (and any further nested children), or to skip them. If the answer
is in question A's `EndingAnswers` list, the whole survey terminates at A — the
participant is sent straight to the submit slide.

Crucially, there is **no separate "skip-to" rule table**. The branching graph is
expressed entirely by the parent-child tree on `Question` plus the `EndingAnswers`
list. This keeps branch authoring inside the same editor that creates the questions.

### Server-side: `SurveyFlowManager.ComputePathAsync`

The visible path is computed server-side from the participant's saved responses. The
algorithm walks the question tree depth-first, taking each subtree only if the parent's
saved answer activates it (`BL/Implementation/SurveyFlowManager.cs:78`).

Pseudo-code (one walked scope = "all questions whose parent is X and whose ChoiceIndex is i"):

```
WalkScope(parentId, triggerChoiceIndex):
  children = allQuestions
             .where(q.parent == parentId && q.choiceIndex == triggerChoiceIndex)
             .orderBy(q.Position)

  for child in children:
    if visited[child.id]: continue
    visited[child.id] = true
    path.append(child)

    response = responsesByQuestionId[child.id]
    if response is null:           # unanswered → stop descending here
        continue

    selected = SplitSelected(child.format, response.Value)   # list of strings

    if any selected ∈ child.EndingAnswers (matched in response.Language):
        return STOP_SURVEY

    for value in selected:
        if child.format == Likert:
            idx = int(value) - 1
        else:
            idx = child.translations[response.Language].AnswerChoices.indexOf(value)
            if idx < 0: continue
        if WalkScope(child.id, idx) == STOP_SURVEY:
            return STOP_SURVEY

WalkScope(parentId=null, triggerChoiceIndex=null)   # entry point: top-level questions
```

`SplitSelected` returns a single-element list for `Open` / `SingleChoice` / `Likert`,
and the comma-separated list of selected options for `MultipleChoice` (so a multi-select
can open *several* sibling subtrees at once).

Three convenience methods sit on top of the walk and are the only methods the front-end
calls:

- `GetNextQuestionAsync(currentQuestionId, participantId)` → walk, find the current
  question, return `path[idx + 1]`.
- `GetPreviousQuestionAsync(currentQuestionId, participantId)` → same, returns
  `path[idx - 1]`.
- `GetResumeQuestionAsync(projectId, participantId)` → return the first question on
  the path that has no saved response (or the last one if all are answered).
- `ComputeVisiblePathAsync(projectId, participantId)` → return every question id on
  the current path; used on first page load and at submit time.

### Client-side: `branching.ts` + `navigation.ts`

The browser does **not** re-implement the branching graph. Every slide carries
`data-question-id` and `data-format`; the actual decision of "what is the next slide"
is delegated to the server via `getNextQuestion` / `getPreviousQuestion`
(`UI-User/src/survey/api.ts:32`). `branching.ts` is mostly a set of DOM utilities:

- `getAnswerFromSlide(slide)` — reads the chosen radio / checkboxes / range / textarea
  and returns the same string shape the server expects.
- `questionIdToSlideIndex(slides, questionId)` — looks up the slide element for a
  question id returned by the server.
- `prefillAnswers(slides, responses)` — on first render, fills inputs with whatever
  the participant already saved before (so a resumed survey looks the way they left it).

`navigation.ts:navigateNext` ties it together:

1. Validate the current slide (`isSlideValid` from `validation.ts`); shake it if invalid.
2. Lock `state.isAnimating` so a second forward gesture during the round-trip is dropped.
3. On the first forward navigation, lazily call `startSession` to create the
   `Participant` (so anonymous page loads cost nothing).
4. POST the current answer (`saveResponse`) — except for the intro slide.
5. GET the next question id (`getNextQuestion`). If the server returns `null`, the
   participant is at the end and the target becomes the submit slide.
6. Trim `state.visibleIndices` to drop any slides that were on a previously projected
   branch (a participant who changes an earlier answer should not see stale slides
   from the old branch).
7. Animate to the target slide.

### Going back and changing an answer

`navigateNext` records the chosen forward target per slide in `state.lastForwardTargets`.
If the participant later returns and changes their answer such that the server now
returns a *different* next question, the entry for that slide changes, and
`state.maxProgressPct` is reset to 0 so the progress bar reflects the new (potentially
longer) branch instead of pretending the old one's progress still applies
(`navigation.ts:82`).

The previously saved response on the abandoned branch stays in the database with
`IsCommitted = false`; at submit time `ResponseManager.CommitResponsesAsync` only flips
the responses that lie on the freshly recomputed `visiblePath` to `IsCommitted = true`,
so abandoned answers never appear in the final dataset.

## 4. The survey rendering engine (frontend)

The vertical-scroll layout (`Views/Survey/Scrollform.cshtml`) renders the whole survey
as a stack of absolutely-positioned `<section class="survey-slide">` elements inside a
fixed-height container. Exactly one slide has `is-active` at a time; slides that have
already been visited get `is-above` so going back animates them downward from the top.

### Slide states

- default: translated below the viewport (the "next" position).
- `is-active`: in the viewport.
- `is-above`: translated above the viewport (the "previous" position).

`animation.ts:activateSlide` flips the classes when navigating, and pauses any
`<video>` / `<audio>` / YouTube / Vimeo embed on the slide being left so media doesn't
keep playing in the background.

### Initialisation (`init.ts`)

On page load:

1. Collect every `.survey-slide`, attach char counters to `Open` textareas, prefill
   any existing saved answers, mark the first slide active.
2. Wire next / previous buttons and gestures (mouse wheel, swipe, arrow keys).
3. If the participant already has a session (`data-has-session="true"`), call
   `getVisiblePath` and `getResumeQuestion` in parallel; rebuild `visibleIndices` from
   that path and jump straight to the resume slide.
4. Hook the form submit: prevent the default POST, call `submitSurvey` via the API,
   then reveal the `#thank-you-popup` partial.

### Navigation

`navigation.ts:navigateNext` and `navigatePrev` are the only entry points. Both lock
`state.isAnimating` for the duration of the CSS transition (`TRANSITION_MS = 580`) so
overlapping gestures cannot fire two transitions at once.

### Animations

`animation.ts` uses CSS class toggles only — no JS-driven keyframes for the slide
movement itself. The progress bar is updated by `updateProgressBar`, which never lets
the displayed percentage drop below the previously reached high-water mark *unless*
branching reset it (see §3 above on changed answers).

### Gestures (`gestures.ts`)

Three input methods, all routed to the same `navigateNext` / `navigatePrev`:

- **Mouse wheel / trackpad** — `wheel` with `passive: false`. Continuous trackpad
  momentum events are debounced by a fixed 900 ms cooldown measured from the last
  navigation (not a resetting timer, see the comment block in `gestures.ts:12`).
- **Touch swipe** — vertical swipes ≥ ~10 % of viewport height (`SWIPE_TRIGGER_PX`)
  fire navigation mid-drag. Horizontal swipes are ignored. Swipes that start on a
  scrollable inner element (long answer list) are allowed to scroll the inner element
  first.
- **Keyboard** — Arrow Down / Right = next, Arrow Up / Left = previous; suppressed
  while the focus is in a textarea or input.

### Client-side validation (`validation.ts`)

The format-specific validators check "is this slide answered" for slides marked
`data-mandatory="true"`. Open answers additionally reject text longer than 500 chars
(matching the server-side `[MaxLength(500)]` on `Response.Value`). On failure the slide
gets a red error message and is shaken (`shakeSlide`).

Likert is always considered answered (slider defaults to the midpoint).

## 5. Question widgets

The closed-question widgets are rendered server-side in
`UI-User/Views/Survey/Scrollform.cshtml`:

- **Single choice** — radio inputs wrapped in clickable label cards.
- **Multiple choice** — checkbox inputs wrapped in clickable label cards.
- **Likert** — a 1..10 native range slider with localised endpoint labels
  (`LikertDisagree` / `LikertAgree`).
- **Open** — a 500-char `<textarea>` with a live char counter and a
  `_MicButton` partial that toggles speech-to-text.

The slide also renders **supporting media** above the question text when
`Question.MediaUrl` is set: an `<img>` for `Image`, a native `<video>` or sandboxed
YouTube / Vimeo `<iframe>` for `Video` (depending on `MediaIsEmbed`), and an
`<audio>` for `Audio`. An optional rich-text caption (`MediaText`, rendered via
`MarkdownRenderer`) appears just below the media.

The chat layout (`Chatform.cshtml` + `chat.ts`) presents the same widgets in a
chat-bubble style: single/multiple choice become "chips", Likert becomes a slider
inside the chat input area, and Open becomes a textarea-input row at the bottom of
the conversation. The same answer formats are produced, so the back-end is identical.

## 6. The "chat" form layout

The prompt of this document referred to `chat.ts` as "AI question support". In the
current codebase `chat.ts` is **not** an AI module — it is the alternative
participant-facing survey layout used when `Project.FormType == Chat`. It renders the
exact same questions as the scroll form, only as a conversational stream of bubbles
with the input controls (chips, slider, textarea) appearing below the latest question.

`chat.ts` reuses every survey API call (`startSession`, `saveResponse`,
`getNextQuestion`, `getResumeQuestion`, `submitSurvey`, `removeAllResponses`), so
branching, validation and submission all run through exactly the same server pipeline
as the scroll form.

The actual AI-assisted feature in the codebase is `QuestionSupportController` +
`BL/AiQuestionSupport/AiQuestionSupportService.cs`. It is wired into the **admin**
side, where it helps the project owner brainstorm questions during questionnaire
authoring. The trigger and prominence are governed by `Project.AiAssistance`
(`AiAssistanceType.ProActive | AskFirst | Never`). It is not exposed to the
survey-filling participant.

## 7. Response submission flow

### When responses are sent

Responses are saved **progressively, one at a time**, on every forward navigation. The
client never batches answers and never POSTs the whole questionnaire at the end. See
`navigation.ts:navigateNext`:

```
saveResponse(projectId, questionId, answer)   →   POST /{projectId}/api/SurveyApi/responses
```

Each call is forwarded by `UI-User/Controllers/Api/SurveyApiController.cs` to the API,
which calls `ResponseManager.UpdateOrCreateResponseAsync`. If the participant already
answered this question, the existing row is updated (and reset to `IsCommitted = false`);
otherwise a new draft response is created.

### Server-side validation

`ResponseManager.UpdateOrCreateResponseAsync` runs every save through the format-specific
validator picked by `ResponseValidatorFactory` (constructor-injected list of
`IResponseValidator` implementations keyed by `QuestionFormat`):

| Format         | Validator (`BL/Implementation/ResponseValidation/`) | Checks |
| -------------- | --------------------------------------------------- | ------ |
| `Open`         | `OpenResponseValidator`                             | Accepts any text (length check is the `[MaxLength(500)]` data annotation on `Response.Value`). |
| `SingleChoice` | `SingleChoiceResponseValidator`                     | The value must be one of the question's translated choice strings (across all languages). |
| `MultipleChoice` | `MultipleChoiceResponseValidator`                 | Splits on `,` and verifies every part is a known translated choice. |
| `Likert`       | `LikertResponseValidator`                           | Must parse as integer in `[1, AnswerChoiceCount]` (defaults to 1..10 if not set). |

In addition, `EntityValidator.Validate(response)` enforces the data-annotation
constraints on the entity itself (the `[MaxLength(500)]` on `Value` is the relevant one).

### Final submission

When the participant presses **Submit** on the last slide, the front-end calls
`POST /{projectId}/api/SurveyApi/submit`, which is proxied to the API
(`ResponsesController.Submit`) and ultimately handled by
`BL/Implementation/SubmissionManager.cs:12`:

1. Load the questionnaire (read-only).
2. Recompute the visible path
   (`SurveyFlowManager.ComputeVisiblePathAsync(projectId, participantId)`).
3. Load every response the participant has saved.
4. `questionnaireManager.ValidateMandatoryAnswers(questionnaire, answeredPairs, visibleSet)` —
   for every question with `IsMandatory == true` that is **on the visible path**, ensure
   a non-whitespace answer exists. Throw `LocalizedValidationException` listing the missing
   ids otherwise.
5. `responseManager.CommitResponsesAsync(participantId, visibleSet)` — flip
   `IsCommitted = true` for responses on the visible path and `false` for any others
   (defensive: re-flips stale committed rows back to draft if branching has moved on).
6. `participantManager.MarkAsSubmittedAsync(participant.SessionToken)` — set
   `Participant.HasSubmitted = true` and `CompletedAt = UtcNow`.

After the request succeeds the UI reveals the localised `_ThankYouPopup` partial; there
is no separate "contact info prompt" step on the survey side (email collection on the
`Participant` entity exists, but it is set through the ideation flow, not survey
submission).

## 8. The "short form"

`ShortForm` is the **project's slug used as a subdomain**, not an alternative
questionnaire layout. It is a single field on `Project` (validated as lowercase
alphanumeric + hyphens, `Domain/Project/Entities/Project.cs:21`) and lets a project be
reached as `https://<short-form>.synapse-ip.xyz/` instead of using the numeric project
id in the URL.

The resolution happens in `UI-User/Middleware/ProjectShortFormResolverMiddleware.cs`:

1. Extract the subdomain from `Host`.
2. Ignore reserved labels (`admin`, `api`, `www`, …, configurable via
   `Subdomains:Reserved`).
3. Look the subdomain up via `IProjectApiService.GetByShortFormAsync` (cached in
   `IMemoryCache` for 60 s).
4. Rewrite the request path from `/<anything>` to `/<projectId>/<anything>` so the
   normal MVC routing takes over. Inactive projects are sent to
   `/Home/ShortFormInactive`; unknown subdomains to `/Home/ShortFormNotFound` (cached
   for 10 s with a sentinel).

The participant sees exactly the same survey afterwards. There is no condensed
questionnaire view in the rendering pipeline.

## 9. State, resume and idempotency

### Sessions

Identification is by a `SessionToken` (GUID) on the `Participant` entity, stored in a
cookie via `ISessionService`. There is **no participant login**; the cookie alone is
the proof of identity. `ParticipantControllerBase` resolves the participant from the
cookie on each request.

The participant row is created lazily on the first forward navigation
(`navigation.ts:62` → `startSession` → `POST /{projectId}/api/SurveyApi/participants`)
so anonymous page views do not pollute the database.

### Resuming

If the participant returns to the survey with a valid cookie:

1. `SurveyController.Index` (`UI-User/Controllers/SurveyController.cs:43`) loads any
   existing responses, attaches them to the view model, and sets `HasSession = true`.
   If `hasParticipantRespondedAsync` reports the participant has already submitted,
   they are redirected to the category / ideation page instead of being able to
   re-take the survey.
2. The scroll-form view embeds the saved responses as JSON in a `<script
   type="application/json" id="existing-responses">` tag.
3. `init.ts` reads that JSON, prefills every slide via `prefillAnswers`, and then
   asks the server for the visible path + resume question. It jumps directly to the
   resume slide and marks every prior slide `is-above` so back-swiping animates
   downward naturally.

### Idempotency of saves

`ResponseManager.UpdateOrCreateResponseAsync` does an explicit upsert: if a `Response`
already exists for `(QuestionId, ParticipantId)` it is updated in place and
`IsCommitted` is reset to `false`; otherwise a new row is inserted. Sending the same
answer twice therefore produces the same single row on the second call as on the first.
Sending a different value updates the existing row's `Value` (and may change which
sub-branch becomes visible the next time `ComputePathAsync` runs).

### Final submission idempotency

Submission flips `Participant.HasSubmitted = true`. A re-submission attempt by the same
participant is blocked at the entry point: `SurveyController.Index` redirects an
already-submitted participant away from the survey before they reach the submit slide
again. On the back end `CommitResponsesAsync` is itself idempotent — it only updates
rows whose `IsCommitted` flag does not already match the desired value, so re-running
it (e.g. on a retry) is a no-op.

### Restart

The chat layout exposes a "restart" button that calls `removeAllResponses`
(`DELETE /{projectId}/api/SurveyApi/responses`), which delegates to
`ResponseManager.RemoveAllAsync(participant)`. That call runs inside a transaction via
`IUnitOfWork.ExecuteInTransactionAsync`, deleting every draft response for the
participant in one go and reloading the page. The participant row itself is kept so
the session cookie remains valid.
