# Task 2 report

Status: DONE. Commit: `43faf730df1887fc711c78e06f0e474e2bf0feb0` (`feat: add keyboard control and input statistics`) on `codex/kitchen-catch`, based on approved Task1 `5965e66d0d0ed8e36e6af9b7126e2341d9f4544b`.

## Files and behavior

18 committed files, limited to these sources and their Unity-generated metadata:

- `Assets/CPS/Input/InputSnapshot.cs`: readonly HandInput/InputSnapshot values; exact constructor arguments and read-only Pressed/Strength01/Valid/ForceN and Left/Right/Source properties.
- `Assets/CPS/Input/IPlayerInput.cs`: exact Read contract.
- `Assets/CPS/Input/KeyboardInput.cs`: installed Input System Keyboard; A/Left and D/Right aliases OR together; source Keyboard, pressed strength 1 and valid released strength 0, no force N. Missing Keyboard or application focus loss returns invalid non-pressed zero-strength snapshots with absent force.
- `Assets/CPS/Input/PlayerController.cs`: explicit Step/ResetPosition, fixed speed 7 units/s, width 16, serialized visible half-width (default 0.5), read-only MovementDirection, serialized catch-zone world offset/half extents and read-only Bounds independent of transform facing/scale/rotation. Reset centers X and preserves Y/Z. Caller controls when Step runs.
- `Assets/CPS/Input/ForceStatsCollector.cs`: duration-weighted strength and independently available force, valid release samples included, per-hand pressed and shared both-pressed durations, pressed-time balance, reset and fresh copied snapshots using Task1 DTOs. Double accumulators prevent finite force multiplication overflowing float weighted means; serialized duration output saturates at float maximum.
- `Assets/CPS/Tests/EditMode/InputStatsTests.cs`: 20 cases.
- `Assets/CPS/Tests/PlayMode/KeyboardPlayerTests.cs`: 14 cases using real queued KeyboardState events in installed InputTestFixture.
- `Assets/CPS/Tests/PlayMode/CPS.PlayModeTests.asmdef`: CPS.Runtime, Unity.InputSystem, Unity.InputSystem.TestFramework; optional TestAssemblies. Installed TestFramework has UNITY_TESTS_FRAMEWORK satisfied by installed com.unity.test-framework version define; no added package dependencies.
- Folder metadata: `Assets/CPS/Input.meta`, `Assets/CPS/Tests/PlayMode.meta`; accompanying source/asmdef metas. Removed only generated trailing whitespace from three new metadata files after verification; no GUID/import behavior changes.

Read task-2-brief.md, constraints.md, applicable spec input/statistics paragraphs, existing Game DTOs, package assembly definition and fixture API, TDD, writing-good-tests reference, ponytail and verification-before-completion guidance. Project had no current non-deleted Markdown files. Did not reread whole plan or unrelated tasks.

Controller-confirmed sample rules: HandInput.Valid=false excludes every hand channel. A valid hand with nonfinite strength excludes normalized statistics but permits independently valid nonnegative force and pressed duration. Clamp finite strength to [0,1]; reject negative or nonfinite force. Both-pressed requires both hands valid and pressed. Nonfinite/nonpositive dt contributes nothing. Balance is left pressed duration divided by combined pressed duration; unavailable if neither hand has pressed time. These decisions are covered by executable tests.

## RED and GREEN evidence

Editor executable: `C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe`.

Each invocation was an escalated PowerShell command after checking `Get-Process Unity` for competing editor processes, launched with `Start-Process -WindowStyle Hidden -PassThru`, followed by `WaitForExit()`, printed actual Unity ExitCode, then parsed result XML. No user process was stopped. No `-quit` was used.

Exact argument sets:

```text
-batchmode -projectPath "C:\Users\Arc\Unity\CPS-Project" -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests.InputStatsTests -testResults "C:\Users\Arc\Downloads\CPS\Unity_Project\verification\task2-red-edit.xml" -logFile "C:\Users\Arc\Downloads\CPS\Unity_Project\verification\task2-red-edit.log"
-batchmode -projectPath "C:\Users\Arc\Unity\CPS-Project" -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests.KeyboardPlayerTests -testResults "C:\Users\Arc\Downloads\CPS\Unity_Project\verification\task2-red-play.xml" -logFile "C:\Users\Arc\Downloads\CPS\Unity_Project\verification\task2-red-play.log"
-batchmode -projectPath "C:\Users\Arc\Unity\CPS-Project" -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests -testResults "C:\Users\Arc\Downloads\CPS\Unity_Project\verification\task2-green-edit.xml" -logFile "C:\Users\Arc\Downloads\CPS\Unity_Project\verification\task2-green-edit.log"
-batchmode -projectPath "C:\Users\Arc\Unity\CPS-Project" -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests -testResults "C:\Users\Arc\Downloads\CPS\Unity_Project\verification\task2-green-play.xml" -logFile "C:\Users\Arc\Downloads\CPS\Unity_Project\verification\task2-green-play.log"
```

| Run | Unity exit | XML total | Passed | Failed | Result |
|---|---:|---:|---:|---:|---|
| RED EditMode | 2 | 20 | 0 | 20 | Failed(Child) |
| RED PlayMode | 2 | 14 | 0 | 14 | Failed(Child) |
| GREEN full EditMode | 0 | 48 | 48 | 0 | Passed |
| GREEN full PlayMode | 0 | 14 | 14 | 0 | Passed |

Tests were written before bodies; compilable throwing skeletons allowed behavior tests to execute rather than counting missing symbols as RED. Both RED XMLs record the expected missing-implementation failure, for example:

```text
System.NotImplementedException : The method or operation is not implemented.
CPS.KitchenCatch.Tests.KeyboardPlayerTests.CatchZoneTracksPositionWithoutFacingOrScaleChanges ()
```

InputStatsTests also records the same deliberately unimplemented collector exception, including StrengthMeanIncludesReleaseAndUsesDuration and all malformed/weighted sample cases. After implementation, the exact same cases pass. Full GREEN EditMode includes all 28 approved Task1 cases plus 20 new cases; PlayMode is 14 new cases. There were no compiler warnings/errors or failed tests. Existing licensing access-token/404 chatter and FMOD audio-device fallback occur in both Task2 GREEN logs and Task1 baseline `task1-green-full-20261008-01.log`; they are environment messages, not new game failures. No fabricated zero-test success.

## Self-review and scope

Reviewed all new source and tests against requirements and realistic mutations: OR-to-AND breaks alias release; flipped direction/both-key handling breaks four state cases; speed or dt mistakes break 30/60/120-step distance; clamp removal breaks both boundary tests; force duration mixed with unavailable samples breaks weighted force; dropping release breaks strength mean 0.25; removing validity/nonfinite guards breaks sample cases; exposing mutable internals breaks copy case; facing-dependent Bounds breaks catch-zone test. Runtime does not add Update polling or gameplay state timing; Task5 remains responsible for invoking input/Step/stat collection only while Playing and implementing pause/resume/countdown.

`git diff --cached --check` passed after generated meta whitespace cleanup. Staged exact owned paths, checked staged names/statistics, then committed. No settings or unrelated deletions were staged. Pre-existing changes remain: Assets/Editor deletion, UniversalRP asset and project settings edits, deleted project-local spec, untracked PackageManagerSettings.asset. Unity additionally generated untracked `ProjectSettings/SceneTemplateSettings.json`; left unstaged and reported to controller.

No blocking concerns. Browser focus delivery, final artwork dimensions and WebGL performance have not been acceptance-tested in this task; later gameplay/build tasks own those checks. No dependency installation, UI/audio authoring, force simulation, MQTT support, gameplay pause flow, unrelated source edits or subagent dispatch.
