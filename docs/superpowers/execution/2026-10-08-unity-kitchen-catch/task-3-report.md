# Task 3 implementation report

Task: local profiles and persistent AppSession. Checkout: `C:/Users/Arc/Unity/CPS-Project`, branch `codex/kitchen-catch`, base `43faf730df1887fc711c78e06f0e474e2bf0feb0`.

## Result and contract

Implemented serializable PlayerProfile, a read-only ProfileStore collection with injected read/write/flush delegates and PlayerPrefs defaults, and an AppSession singleton that survives scene changes. AppSession exposes Store, SelectedProfile, SelectedDifficulty and Initialize/ProfileStore injection. It loads profiles without scene navigation; Boot routing belongs to Task5.

Controller accepted these clarifications before implementation:

- Create and RecordResult save automatically; Save returns actual persistence success and supports retry. RecordResult's boolean means a new result was counted, not that disk persistence succeeded. Warning reports failed persistence while the in-memory profile remains usable.
- Invalid nicknames, unknown players/selections, invalid session IDs and difficulty values reject with argument exceptions; invalid selection keeps the previous selection.
- A missing PlayerPrefs key reads as null, distinct from an existing empty string, which is corrupt input.
- Completed session IDs are deduplicated across players within the same store runtime, retained across Load and scene transitions, and never persisted as history. PlayCount is persisted.
- Read failure prevents primary writes until a successful read. Invalid/future-version raw input is backed up and flushed before any primary replacement; backup write/flush failure keeps the original primary intact.

Names are trimmed and validated as 1–24 Unicode text elements using StringInfo, including Thai and combining marks. Markup-like names stay literal. New IDs are GUIDs; loaded IDs must be nonblank, trimmed, and unique. Negative counts, null entries, missing/wrong-type fields and unsupported wrapper versions are corrupt payloads.

## Markdown and workflow instructions read

Read task-3-brief.md and constraints.md completely, relevant profile/AppSession sections of the approved design, test-driven-development/SKILL.md and writing-good-tests.md, ponytail/SKILL.md, verification-before-completion/SKILL.md, and systematic-debugging/SKILL.md after discovering the parser failure. The using-superpowers skill explicitly excludes dispatched subagents. No unrelated task briefs or complete implementation plan were loaded. No reviewers/subagents were spawned.

## RED/GREEN executable evidence

All results/logs are under `C:/Users/Arc/Downloads/CPS/Unity_Project/verification`. Unity runs used the installed `6000.3.25f1` editor with required escalation. No `-quit`, no process termination, no package installation, and no user's PlayerPrefs/browser/registry cleanup were used.

| Run | Filter/platform | Actual XML total / passed / failed | Editor exit | Evidence files |
| --- | --- | --- | --- | --- |
| Initial RED discovery | ProfileStoreTests / EditMode | 32 / 0 / 32 | 2 | task3-red-edit.xml / .log |
| RED behavior assertions | ProfileStoreTests / EditMode | 32 / 2 / 30 | 2 | task3-red-edit-assertions.xml / .log |
| RED AppSession | AppSessionTests / PlayMode | 5 / 0 / 5 | 2 | task3-red-play.xml / .log |
| First focused profile implementation | ProfileStoreTests / EditMode | 32 / 31 / 1 | 2 | task3-green-edit-focused.xml / .log |
| Focused AppSession GREEN | AppSessionTests / PlayMode | 5 / 5 / 0 | 0 | task3-green-play-focused.xml / .log |
| JSON trust-boundary RED | ProfileStoreTests / EditMode | 37 / 31 / 6 | 2 | task3-red-json-boundary.xml / .log |
| Full current EditMode GREEN | CPS.KitchenCatch.Tests / EditMode | 85 / 85 / 0 | 0 | task3-green-edit.xml / .log |
| Full current PlayMode GREEN | CPS.KitchenCatch.Tests / PlayMode | 19 / 19 / 0 | 0 | task3-green-play.xml / .log |
| Date-shaped nickname RED reproduction | NamesAreTrimmedAndLiteral / EditMode | 4 / 3 / 1 | 2 | task3-red-date-name.xml / .log |
| Date-shaped nickname RED direct assertion | NamesAreTrimmedAndLiteral / EditMode | 4 / 3 / 1 | 2 | task3-red-date-name-assertions.xml / .log |
| Final full EditMode GREEN | CPS.KitchenCatch.Tests / EditMode | 86 / 86 / 0 | 0 | task3-final-green-edit.xml / .log |
| Final full PlayMode GREEN | CPS.KitchenCatch.Tests / PlayMode | 19 / 19 / 0 | 0 | task3-final-green-play.xml / .log |

The initial RED discovery failed in fixture setup because the skeleton Load threw NotImplementedException; it is recorded honestly and is not the behavioral RED evidence. Load was made an empty stub so real corruption/name assertions ran, then the second RED run provided executable failures before implementation.

Actual failure excerpts:

```text
InvalidNamesDoNotCreateProfiles:
Expected: <System.ArgumentException>
But was: <System.NotImplementedException ...>

ActivatedSessionBecomesPersistentSingleton:
Expected: same as <AppSession (CPS.KitchenCatch.AppSession)>
But was: null

InvalidPayloadIsBackedUpBeforeItCanBeReplaced("{\"schemaVersion\":1,\"profiles\":null}"):
Expected: not <empty>
But was: <string.Empty>
```

The third excerpt exposed JsonUtility's normalization/coercion: null/missing profile arrays, string schemaVersion, fractional PlayCount, numeric PlayerId and missing PlayCount were accepted as valid data. Added five fixtures and reran RED, observing all six independent failures before the correction. Controller approved the already-installed `com.unity.nuget.newtonsoft-json` 3.2.1 for strict JObject/JArray token validation before JsonUtility DTO conversion. Its existing DLLs auto-reference the runtime assembly, with separate installed Editor/AOT WebGL import settings. No package manifest, lockfile or asmdef changed. This avoids a custom JSON parser while preserving raw data for invalid tokens.

Final self-review then identified Newtonsoft's default date parsing: the legal 20-character nickname `2026-10-08T00:00:00Z` could become a Date token and be falsely treated as corrupt. Added that name to the literal-name reload test; a focused run reproduced one failure of four cases (indexing the incorrectly empty reloaded profile list). Added direct no-warning/count assertions and reran before fixing; actual failure was `Expected: <empty> But was: "Profiles could not be loaded; using an empty list. Invalid profile field types or play count."`. The installed library XML documentation confirmed JsonReader.DateParseHandling controls this conversion. Replaced JObject.Parse with JObject.Load using JsonTextReader configured with DateParseHandling.None, retaining strict field validation and checking for unexpected trailing content. This keeps every quoted nickname string literal without a serializer/type conversion workaround.

Latest full XML reports `result="Passed"`, total/passed 86 and failed 0 for EditMode, total/passed 19 and failed 0 for PlayMode after the date-name correction; both Editor exits are 0. Compiler/exception/warning searches of both final logs returned no matches. The earlier baseline Curl shutdown noise was not present in these final runs.

## Exact executed commands

Each Unity run first checked `Get-Process Unity -ErrorAction SilentlyContinue | Select-Object Id,Path`; no Unity editor lock/process was present before launches. Each used this PowerShell launch form (with the concrete argument rows below), under require_escalated:

```powershell
$taskUnity = Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe' -ArgumentList '<arguments below>' -PassThru -WindowStyle Hidden
$taskUnity.WaitForExit()
$taskUnity.Refresh()
Write-Output "Unity exit=$($taskUnity.ExitCode)"
```

```text
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests.ProfileStoreTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-red-edit.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-red-edit.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests.ProfileStoreTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-red-edit-assertions.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-red-edit-assertions.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests.AppSessionTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-red-play.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-red-play.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests.ProfileStoreTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-green-edit-focused.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-green-edit-focused.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests.AppSessionTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-green-play-focused.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-green-play-focused.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests.ProfileStoreTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-red-json-boundary.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-red-json-boundary.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-green-edit.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-green-edit.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-green-play.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-green-play.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests.ProfileStoreTests.NamesAreTrimmedAndLiteral -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-red-date-name.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-red-date-name.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests.ProfileStoreTests.NamesAreTrimmedAndLiteral -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-red-date-name-assertions.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-red-date-name-assertions.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-final-green-edit.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-final-green-edit.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-final-green-play.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task3-final-green-play.log
```

XML totals/failure excerpts were read with `[xml]$taskResults = Get-Content -Raw '<result path>'`, inspecting `$taskResults.'test-run'` and `SelectNodes('//test-case/failure/message')`. Final log inspection used `rg -n 'error CS|warning CS|Exception|Curl error|warning|Warning' '<log path>'`. `git diff --cached --check` is clean after trimming the generated folder meta's three trailing spaces; no source or functional change followed the final suite runs.

## Owned files and selective staging

- Assets/CPS/Profiles.meta
- Assets/CPS/Profiles/PlayerProfile.cs and .meta
- Assets/CPS/Profiles/ProfileStore.cs and .meta
- Assets/CPS/Profiles/AppSession.cs and .meta
- Assets/CPS/Tests/EditMode/ProfileStoreTests.cs and .meta
- Assets/CPS/Tests/PlayMode/AppSessionTests.cs and .meta

All 11 exact files were staged explicitly for the initial implementation commit (611 added lines). The follow-up staged only ProfileStore.cs and ProfileStoreTests.cs; staged diff inspected: 12 additions / 1 deletion. Final combined Task3 change from the original base is 11 files / 622 added lines. Existing user modifications/deletions in Assets/Editor, Assets/Settings, ProjectSettings and the removed design document remain unstaged, as do PackageManagerSettings.asset and Unity-generated SceneTemplateSettings.json. No main/merge/push/PR operation occurred. Report is outside the Unity checkout as required.

## Self-review and practical limits

- Storage fakes model pending SetString writes separately from flushed persisted values; corruption tests check both pending and persisted primary raw remains intact when backup writes/flushes fail. Valid-save failures retain created profiles/counts in memory, warn, return false, and succeed on explicit retry.
- Once-counting tests catch a missing dedup guard and cross-player double counting; reloading within the same store preserves completed IDs. Scene-change tests also ensure the same singleton/store/selection survives.
- Profile creation/reload tests catch lost GUID identity, name trimming, markup alteration, Thai/combining-mark rejection, and omitted persistence. Corruption tests catch bad schema/token types, null entries, duplicate IDs and negative counts.
- All AppSession tests use inactive GameObjects, initialize fake storage before activation/Awake, and destroy only their own created objects. They do not touch default PlayerPrefs. Duplicate OnDestroy cannot clear the original singleton.
- Loaded IDs are validated as unique nonblank trimmed strings; newly created IDs are GUIDs. This retains the specified string-ID DTO contract without silently rewriting loaded identity.
- Public serializable PlayerProfile fields remain mutable as requested; Save validates before writing. UI callers should use store operations for Create/RecordResult.
- Default PlayerPrefs production adapters and WebGL browser persistence were not manually exercised; tests intentionally isolate user storage. Actual browser persistence/rendering and rich-text disabling in UI belong to later integration/acceptance tasks. AppSession must be placed on a root GameObject in Boot for DontDestroyOnLoad.
- A backup uses one dedicated key. A later separate corrupt payload may replace that backup after a successful flush, matching the requested single raw backup contract.

Commits:

- `b1aeda4d435ec25d1701aaf4e6e5b1fbd9d36ad4` — `feat: add local profiles and app session`.
- `3758dff4772db4c6989bb71041a108c52ab72444` — `fix: preserve date-shaped profile names on reload`.

Verified post-commit staged file list is empty and only the original unrelated modifications/untracked settings remain. No Unity process remained after testing.
