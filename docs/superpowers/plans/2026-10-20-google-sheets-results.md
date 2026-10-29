# Google Sheets Result Upload Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Send one immutable Unity game result per completed Profile session to the formatted `CPS-DB` Google Sheet without blocking gameplay or recording Pause-to-menu exits.

**Architecture:** A container-bound Google Apps Script validates a fixed JSON contract, deduplicates by `sessionId`, and appends a flattened row under a script lock. A small Unity component subscribes to the existing `GameSessionController.Finished` event, maps `SessionResult` to that contract, and posts asynchronously with `UnityWebRequest`.

**Tech Stack:** Unity 6.3 LTS, C#, Unity Test Framework, `UnityWebRequest`, Google Sheets, Google Apps Script V8

**Spec:** `docs/superpowers/specs/2026-10-20-google-sheets-results-design.md`

## Global Constraints

- Use Sheet ID `1fi7VUnYyXRohA6jlrFwCaoA0w9RvS3AIjYzJb9nriWk` and tab name `Results`.
- Store one row per completed `SessionResult`; Pause → Main Menu creates no upload.
- Treat `sessionId` as the idempotency key and never append it twice.
- Post JSON as `text/plain` so WebGL does not require a CORS preflight.
- Missing force values from Keyboard input become JSON `null` and blank Sheet cells, never simulated Newton values.
- Upload is asynchronous, has no retry queue, never blocks Results/Restart/Main Menu, and only logs failures.
- Add no package or dependency.
- Deploy the Web App as the Sheet owner with access set to `Anyone`; request action-time confirmation immediately before changing deployment access.
- Keep the five local MP3 files ignored and preserve unrelated dirty Unity project settings.

## Review Focus

- Malformed, empty, unsupported-schema, or non-finite payloads must return `ok:false` without adding a row; covered by Task 1 contract self-tests.
- Concurrent duplicate POSTs must leave one row because duplicate lookup occurs inside the script lock; covered by Task 2 live duplicate test.
- `InputStatistics`, left hand, or right hand may be null and must serialize safely with blank optional cells; covered by Task 3 EditMode tests.
- Destroying or disabling the uploader during a request must not throw or change the frozen result; covered by Task 3 PlayMode tests.
- Redirect/CORS behavior can differ between Editor and WebGL; covered by Task 4 Editor and WebGL end-to-end checks.

---

### Task 1: Apps Script contract and Sheet formatter

**Files:**
- Create: `google-apps-script/Code.gs`
- Create: `google-apps-script/appsscript.json`
- Create: `google-apps-script/README.md`
- Test: `google-apps-script/Code.gs` (`runContractSelfTests`)

**Interfaces:**
- Consumes: flattened JSON fields listed in the approved spec.
- Produces: `setupResultsSheet(): void`, `doPost(e): TextOutput`, and JSON responses `{ok,status,message?}`.

- [ ] **Step 1: Add failing contract self-tests**

Add `runContractSelfTests()` assertions for a complete payload, missing `sessionId`, unsupported `schemaVersion`, non-finite numeric text, null hand statistics, enum strings, and exact 29-column row order. It must fail because validation and row mapping do not exist.

- [ ] **Step 2: Run the self-test and verify RED**

Paste only the test and empty stubs into the bound Apps Script editor, run `runContractSelfTests`, and confirm it throws on the first unimplemented assertion. Do not deploy yet.

- [ ] **Step 3: Implement the fixed server contract**

Implement these functions in `Code.gs`:

```javascript
function setupResultsSheet()
function doPost(e)
function parsePayload_(raw)
function validatePayload_(payload)
function rowFromPayload_(payload, receivedAtUtc)
function findSessionRow_(sheet, sessionId)
function jsonResponse_(body)
function runContractSelfTests()
```

Use `SpreadsheetApp.openById`, `LockService.getScriptLock()`, a `try/finally` lock release, and server-owned header order. `setupResultsSheet()` creates or repairs only the `Results` tab header/formatting: A–H subtle blue, I–Q subtle green, R–AC subtle orange, darker group headers, two pale alternating shades within each group, frozen row 1, filter, widths, UTC date/time, and numeric formats.

- [ ] **Step 4: Run the self-test and verify GREEN**

Run `runContractSelfTests` in Apps Script. Expected: completes without exception and does not add a Sheet row.

- [ ] **Step 5: Document installation and commit**

Document binding the script to `CPS-DB`, running setup, deployment ownership/access, redeployment after code changes, response examples, and the public-endpoint limitation.

```powershell
git add google-apps-script docs/superpowers/plans/2026-10-20-google-sheets-results.md
git commit -m "feat: add Google Sheets result receiver"
```

### Task 2: Install, format, and deploy the Google Web App

**Files:**
- Modify externally: Google Sheet `CPS-DB`
- Modify externally: its container-bound Apps Script project
- Modify: `google-apps-script/README.md`

**Interfaces:**
- Consumes: Task 1 `Code.gs` and `setupResultsSheet()`.
- Produces: a public `/exec` deployment URL returning the Task 1 response contract.

- [ ] **Step 1: Install source in the bound Apps Script project**

Open Extensions → Apps Script from `CPS-DB`, replace `Code.gs` with the reviewed local source, set the V8 manifest, save, run `runContractSelfTests`, then run `setupResultsSheet`. Approve Google authorization only when prompted.

- [ ] **Step 2: Verify Sheet formatting before deployment**

Confirm `Results!A1:AC1`, frozen row 1, one filter, group colors, alternating rows, widths, and number/date formats. Confirm no game-result row exists yet.

- [ ] **Step 3: Deploy with an action-time permission confirmation**

Immediately before clicking Deploy with access `Anyone`, ask the user to confirm that the endpoint will be public and unauthenticated. Deploy as the owner, copy the `/exec` URL, and record it in `google-apps-script/README.md`.

- [ ] **Step 4: Verify create, duplicate, and invalid requests**

POST a clearly labeled test session. Expected: `created` and one row. POST the identical payload. Expected: `duplicate` and unchanged row count. POST without `sessionId`. Expected: `ok:false` and unchanged row count.

- [ ] **Step 5: Remove only the labeled test row after confirmation and commit**

Ask for action-time confirmation before deleting the cloud test row. Verify the header and formatting remain, then commit only the deployment documentation update.

```powershell
git add google-apps-script/README.md
git commit -m "docs: record result endpoint deployment"
```

### Task 3: Unity result payload and asynchronous uploader

**Files:**
- Create: `Assets/CPS/Telemetry/ResultUploadPayload.cs`
- Create: `Assets/CPS/Telemetry/GoogleSheetsResultUploader.cs`
- Create: corresponding Unity `.meta` files/folder metadata
- Modify: `Assets/CPS/Editor/GameSceneSetup.cs`
- Modify: `Assets/CPS/Prefabs/GameplayRig.prefab`
- Test: `Assets/CPS/Tests/EditMode/ResultUploadPayloadTests.cs`
- Test: `Assets/CPS/Tests/EditMode/GameSceneAuthoringTests.cs`
- Test: `Assets/CPS/Tests/PlayMode/ResultUploaderTests.cs`

**Interfaces:**
- Consumes: `SessionResult`, `GameSessionController.Finished`, and Task 2 `/exec` URL.
- Produces: `ResultUploadPayload.From(SessionResult)`, `ResultUploadPayload.ToJson()`, and `GoogleSheetsResultUploader` attached to `GameplayRig`.

- [ ] **Step 1: Write failing payload tests**

Test exact mapping of all 29 server fields, enum names, decimals, Unicode `DisplayName`, and `null` output for unavailable force/balance statistics. Test null `SessionResult` rejection and nested-statistics null safety.

- [ ] **Step 2: Run focused EditMode tests and verify RED**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\Arc\Unity\CPS-Project' -runTests -testPlatform EditMode -testFilter 'CPS.KitchenCatch.Tests.ResultUploadPayloadTests' -testResults 'C:\Users\Arc\Downloads\CPS\Unity_Project\verification\result-payload-red.xml' -logFile 'C:\Users\Arc\Downloads\CPS\Unity_Project\verification\result-payload-red.log' -quit
```

Expected: compile/test failure because `ResultUploadPayload` does not exist.

- [ ] **Step 3: Implement the payload mapper**

Create a serializable DTO with nullable optional measurements. Implement:

```csharp
public static ResultUploadPayload From(SessionResult result)
public string ToJson()
```

Map enums to their names and serialize with the Newtonsoft JSON package already used by the runtime; do not add a dependency.

- [ ] **Step 4: Run focused EditMode tests and verify GREEN**

Run the Step 2 command with output names `result-payload-green.*`. Expected: all focused tests pass with zero failures.

- [ ] **Step 5: Write failing uploader lifecycle tests**

Test that one `Finished` event starts one submission, a repeated same-session submission is ignored, an empty endpoint logs/skips without throwing, Pause → Main Menu starts none, and disabling/destroying the component unsubscribes cleanly. Inject a submission delegate through `Initialize` rather than making live network calls.

- [ ] **Step 6: Implement the uploader component**

Implement serialized `GameSessionController controller`, serialized `string endpoint`, a per-instance `HashSet<string>` for started sessions, and:

```csharp
public void Initialize(GameSessionController controller, string endpoint)
public void Initialize(GameSessionController controller, string endpoint, Action<string> submitOverride)
private bool TryStartUpload(SessionResult result)
private IEnumerator Post(string json)
```

The two-argument overload uses the real request path; the three-argument overload is the minimal test seam and receives serialized JSON. Subscribe in `OnEnable`, unsubscribe in `OnDisable`, POST UTF-8 JSON as `text/plain`, set a finite timeout, accept `created` and `duplicate`, and log warnings only.

- [ ] **Step 7: Author and verify prefab wiring**

Update `GameSceneSetup.CreateGameplayTemplate()` so newly authored rigs include the uploader wired to the same controller and Task 2 endpoint. Patch the existing `GameplayRig.prefab` through Unity authoring, not manual YAML editing. Extend `GameSceneAuthoringTests` to assert exactly one uploader, matching controller, and a non-empty HTTPS `/exec` URL.

- [ ] **Step 8: Run focused and full Unity suites**

Run focused EditMode/PlayMode tests, then the complete suites. Expected: zero failures and zero skipped tests; existing Pause → Main Menu play-count tests remain green.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\Arc\Unity\CPS-Project' -runTests -testPlatform EditMode -testResults 'C:\Users\Arc\Downloads\CPS\Unity_Project\verification\sheets-edit.xml' -logFile 'C:\Users\Arc\Downloads\CPS\Unity_Project\verification\sheets-edit.log' -quit
& 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\Arc\Unity\CPS-Project' -runTests -testPlatform PlayMode -testResults 'C:\Users\Arc\Downloads\CPS\Unity_Project\verification\sheets-play.xml' -logFile 'C:\Users\Arc\Downloads\CPS\Unity_Project\verification\sheets-play.log' -quit
```

- [ ] **Step 9: Commit the Unity integration**

Stage only the telemetry source/meta files, authoring/test changes, and `GameplayRig.prefab`.

```powershell
git add Assets/CPS/Telemetry Assets/CPS/Editor/GameSceneSetup.cs Assets/CPS/Prefabs/GameplayRig.prefab Assets/CPS/Tests/EditMode/ResultUploadPayloadTests.cs Assets/CPS/Tests/EditMode/GameSceneAuthoringTests.cs Assets/CPS/Tests/PlayMode/ResultUploaderTests.cs
git commit -m "feat: upload completed game results to Sheets"
```

### Task 4: End-to-end Editor and WebGL verification

**Files:**
- Modify: `README.md`
- Modify: `docs/unity-playtest-checklist.md`
- Create: `verification/google-sheets-result-upload-report.md`

**Interfaces:**
- Consumes: deployed Task 2 endpoint and wired Task 3 Unity prefab.
- Produces: verified Editor/WebGL evidence and operator instructions.

- [ ] **Step 1: Verify a real Editor session**

Play from Boot, choose a clearly labeled test Profile, finish one round, and confirm Results appears without waiting. Confirm exactly one matching Sheet row with correct Profile, score, reason, counts, rank, input source, and blank Keyboard force cells.

- [ ] **Step 2: Verify non-terminal exit and network failure**

Pause → Back to Main Menu and confirm no new row. Temporarily use an invalid endpoint in a test instance, finish a round, and confirm Results/Restart/Main Menu still work with only a warning.

- [ ] **Step 3: Build and verify WebGL**

Run the existing WebGL build workflow, serve it locally, complete a round, and confirm one Sheet row. Verify the browser console has no CORS/preflight failure and gameplay remains responsive.

- [ ] **Step 4: Clean test data with confirmation**

Ask for action-time confirmation, remove only rows whose Profile/session IDs were created by Tasks 2 and 4, and verify production headers/formatting remain.

- [ ] **Step 5: Update operator docs and commit**

Document data collection, redeployment, public-endpoint risk, failure behavior, and how to disable uploads by clearing the prefab endpoint. Record test commands/results and any manual audible/visual checks not performed.

```powershell
git add README.md docs/unity-playtest-checklist.md verification/google-sheets-result-upload-report.md
git commit -m "docs: verify Google Sheets result upload"
```

- [ ] **Step 6: Request final review**

Use `superpowers:requesting-code-review` for the complete change, fix blockers, rerun affected tests, and then use `superpowers:finishing-a-development-branch` for handoff.
