# Task 5 implementation report

Status: DONE_WITH_CONCERNS. Commit: `8e16ae0e83729ad2bbe2cc22219c622df37827ab` (`feat: connect playable round and terminal flow`). Checkout `C:/Users/Arc/Unity/CPS-Project`, branch `codex/kitchen-catch`; starting HEAD `455c6d4880541667df2755a60af22d7493b68a70`. No merge, push, PR, worktree, independent helper or reviewer was created. Parent dispatches independent review.

## Delivered behavior and interfaces

`GameSessionController` owns presentation state, countdown, pause and round lifecycle. Inspector references are player, KeyboardInput, spawner, the original three DifficultyConfig assets and SceneNavigator. Production `Initialize(PlayerController, IPlayerInput, FallingObjectSpawner, DifficultyConfig[], AppSession)` supports normal dependency injection and rejects rewiring an active round; tests use this rather than runtime debug methods. Public `Step(float)` is the same path called by Update. `Model`, `State`, `IsPaused`, `CountdownSeconds`, `ValidationError`, `LastResult`, `StateChanged` and `Finished` supply the UI contract; LastResult and each Finished subscriber receive independent deep copies including nested hand statistics.

BeginRound validates actual references with Task4's shared spawner validator, copies profile identity, clones the selected tuning asset, clears objects/stats/result/pause, resets catcher to its actual centered movement position and HP3, then enters Ready3s. Ready and explicit Resume3s exclude input/player/object simulation, time and statistics; countdown overshoot does not become gameplay time. Focus loss pauses and resets movement; focus return never auto-resumes. The explicit active step order is capped dt → input/player → spawner resolution → statistics → CompleteStep → terminal freeze/clear. HP0 wins a simultaneous timeout. Triumph1.2s or Dying1.4s proceeds to Results; terminal Pause is ignored. GUID sessionId, UTC ISO8601 endedAtUtc, profile/difficulty/score/HP/time and existing InputStatistics DTO fields are recorded once before publication. Keyboard source and existing strength/duration/ForceN semantics are reused, with no fabricated N. Restart resets real pooled objects and stale object outcomes; ReturnToMenu clears active simulation and preserves persistent selection.

Ruling9 is implemented with one `GameplayRig.prefab`. MainMenu's prefab-component reference is inspectable read-only: focused authoring validation passed Easy/Normal/Hard without instantiating the prefab or invoking its lifecycle/simulation. Gameplay uses one prefab instance with no controller/player/spawner configuration overrides; BeginRound revalidates that instance. No additive scene preflight, duplicate serialized tuning or hidden playable instance exists. Missing references show named safe diagnostics and gate Start. Later geometry/reference/art changes must be applied to this shared prefab.

`SceneNavigator.GoTo(string)` returns a bool, rejects blank/unloadable/current/duplicate requests, and uses one guarded single-load path. `BootRoute` supplies the tiny Boot→MainMenu integration because AppSession itself does not navigate. `PrototypeMenu` provides explicit demo selection and difficulty/Start; `PrototypeGameplayUI` provides temporary HUD/countdown/pause/resume/results/restart/menu. These intentionally named prototype components are Task6 replacement scope, not the final UI framework.

`GameplayViewport.Apply(width,height)` is the planned production camera component, not a throwaway camera helper: centered16:9 rect with letterbox/pillarbox and Screen resize updates. Scenes use orthographic size4.5 and ScreenSpaceCamera Canvas at1920×1080 reference. Task6 extends the Canvas matching and required UI-size coverage. Six simple procedural sprites and five cloned definitions are visible prototype scaffolding only; cloned kinds/points remain Apple10/Bread15/Cake20 and original trash values. Original Task4 config/definition/prefab assets remain unchanged. Task7 replaces these prototype definitions/sprites/material assignments with production art, and must review sprite rendering and adapt the prototype-specific pixel assertion.

## Owned files

The selective commit contains60 paths:58 new files, one existing Editor asmdef modification, and only the scene-list index hunk in EditorBuildSettings. Every new asset/source has its generated `.meta`.

- `Assets/CPS/Flow.meta`; `Flow/{GameSessionController,SceneNavigator,BootRoute,PrototypeMenu,PrototypeGameplayUI}.cs` and their metas.
- `Assets/CPS/UI.meta`; `UI/GameplayViewport.cs` and meta.
- `Assets/CPS/Editor/GameSceneSetup.cs` and meta; existing `Editor/CPS.Editor.asmdef` adds only installed `UnityEngine.UI` and `Unity.InputSystem` references. No package/dependency added.
- `Assets/CPS/Prefabs/GameplayRig.prefab` and meta.
- `Assets/CPS/Prototype.meta`; `Prototype/{Apple,Bread,Cake,Can,BananaPeel}.asset`, the five corresponding PNGs, `Player.png`, `FallingObject.prefab`, `Unlit.mat`, and all metas. The material uses installed `Universal Render Pipeline/2D/Sprite-Unlit-Default`; no custom shader or global pipeline change.
- `Assets/CPS/Scenes.meta`; `Scenes/{Boot,MainMenu,Gameplay}.unity` and metas.
- `Assets/CPS/Tests/PlayMode/{GameSessionFlowTests,GameSceneFlowTests}.cs` and metas; `Tests/EditMode/GameSceneAuthoringTests.cs` and meta.
- `ProjectSettings/EditorBuildSettings.asset`: exactly nine added scene-list lines, Boot/MainMenu/Gameplay first, preserved SampleScene fourth.

## Executed commands and evidence

All paths below are absolute after substituting U=`C:/Users/Arc/Unity/CPS-Project` and V=`C:/Users/Arc/Downloads/CPS/Unity_Project/verification`. Executable was exactly `C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe`. Every run was serial, with an escalated process inventory immediately before launch; sources/assets were frozen through actual process exit. Normal sandbox inventory can hide elevated Unity processes, so it was not used as proof of an empty inventory. No user Unity process was killed.

The exact test argument form was `-batchmode -projectPath U -runTests -testPlatform PLATFORM -testFilter FILTER -testResults V/NAME.xml -logFile V/NAME.log`, without `-quit`. Hidden escalated PowerShell launch used `Start-Process -FilePath <exe> -ArgumentList <those arguments> -WindowStyle Hidden -PassThru`, then the returned process's `WaitForExit()`, `Refresh()` and actual ExitCode output (not shell `-Wait`). This table specifies every test invocation's exact variable values and actual XML totals, not a prediction:

| NAME | PLATFORM | FILTER | Total / passed / failed | Actual process exit |
|---|---|---|---|---|
| task5-red-flow | PlayMode | CPS.KitchenCatch.Tests.GameSessionFlowTests |8 /0 /8 |2 |
| task5-green-flow1 | PlayMode | CPS.KitchenCatch.Tests.GameSessionFlowTests |8 /6 /2 |2 |
| task5-red-scenes | PlayMode | CPS.KitchenCatch.Tests.GameSessionFlowTests;CPS.KitchenCatch.Tests.GameSceneFlowTests |No XML; aborted harness |−1 |
| task5-red-scenes-recovered | PlayMode | CPS.KitchenCatch.Tests.GameSceneFlowTests |3 /0 /3 |2 |
| task5-green-integration1 | PlayMode | CPS.KitchenCatch.Tests.GameSessionFlowTests;CPS.KitchenCatch.Tests.GameSceneFlowTests |13 /12 /1 |2 |
| task5-green-integration2 | PlayMode | CPS.KitchenCatch.Tests.GameSessionFlowTests;CPS.KitchenCatch.Tests.GameSceneFlowTests |13 /13 /0 |0 |
| task5-red-render | PlayMode | CPS.KitchenCatch.Tests.GameSceneFlowTests.AuthoredScenesCompleteAllDifficultiesAndReturnWithPersistentSelection |1 /1 /0 |0 |
| task5-final-edit | EditMode | CPS.KitchenCatch.Tests |106 /106 /0 |0 |
| task5-final-play | PlayMode | CPS.KitchenCatch.Tests |52 /52 /0 |0 |

All completed XML files report zero skipped. Final Edit suite includes105 existing tests plus one authoring test; final Play includes39 existing plus13 Task5 tests. Final log scans found no compiler errors, warnings, game exceptions or Curl matches. Full suites ran after the final owned text whitespace normalization; no code/assets changed afterward. Capture runs additionally set `CPS_TASK5_CAPTURE_DIR=V` before launching (integration2, render, final Play).

Executable RED came from a minimal compilable no-op controller/viewport API skeleton: eight flow behavior assertions failed (Begin/Pause/state/publication/validation/restarts), not missing symbols. One incomplete focus callback also emitted the expected missing receiver diagnostic. Initial GREEN failures were faulty fixture assumptions: a slow policy at10s had fewer than three real foods; ordinary DateTime.Parse converted UTC to local. Fixture time20s and DateTimeStyles.RoundtripKind corrected those assumptions, without runtime workaround. Recovered scene RED independently failed missing Boot/MainMenu and expected tall-screen rect.height0.75 versus1 before scene/viewport implementation.

The file named `task5-red-render` is **not RED evidence**: the new cream-player pixel assertion unexpectedly passed before any shader/material fix. Its misleading initial filename is retained for traceability. No fix was claimed from it.

Authoring commands used the same hidden serial process wrapper with `-batchmode -quit -projectPath U -executeMethod METHOD -logFile V/NAME.log`:

| NAME | METHOD | Actual exit / observed output |
|---|---|---|
| task5-author-template | CPS.KitchenCatch.Editor.GameSceneSetup.CreateGameplayTemplate |0; shared prefab read-only validation passed all3 difficulties without instantiation |
| task5-author-scenes | CPS.KitchenCatch.Editor.GameSceneSetup.CreateOrUpdate |0; three scenes authored and scene list updated |
| task5-preserve-scenes | CPS.KitchenCatch.Editor.GameSceneSetup.CreateOrUpdate |1, intentional refusal: `InvalidOperationException: Authored scene already exists: Assets/CPS/Scenes/Boot.unity` |

The preservation invocation was deliberately expected to refuse, not a failing suite: all150 sampled files (CPS source/assets, SampleScene and build settings) had unchanged SHA256 entries before/after. CreateOrUpdate checks all existing target scene paths before invoking template or scene authoring. It is explicit Editor menu/executeMethod only, never import-time initialization. Existing SampleScene remains unchanged. Authored scenes/serialization use Unity Editor APIs, not handwritten YAML.

## Harness failure diagnosis and bounded recovery

The original scene RED invocation (PID8180, start local2026-10-09 00:43:39) lost its runner after the first failed scene assertion. The old fixture cleanup unloaded the currently active framework InitTestScene. Installed CreateBootstrapSceneTask/PlaymodeTestsController source showed the nonpersistent `Code-based tests runner` owns the test coroutine and result publication. This was a test lifecycle defect, not a gameplay defect or valid RED result.

Following systematic-debugging Phase1, an escalated bounded ownership/CPU/log diagnostic verified exact U and `task5-red-scenes.log` command line. Samples01:01:40→01:01:50 showed CPU2699.25→2726.55, unchanged log5,712,861 bytes and last write00:53:14; direct import workers37060/23752 were nearly idle and their tails showed successful imports. The invocation differed from successful flow only in the scene filter/result filenames. Parent independently inspected framework source and authorized cancellation ONLY of that tracked invocation after re-verifying its exact identity. CloseMainWindow returned false; guarded Stop-Process of PID8180 produced actual wrapper exit−1, no XML. Direct owned workers exited automatically. No broad taskkill, user-process kill, package edit or runtime harness workaround occurred.

The fixture-only fix preserves the documented reserved code-based runner before any Single scene load, and teardown unloads only fixture-loaded Boot/MainMenu/Gameplay scenes, never bootstrap/unowned scenes. The immediate recovered focused run completed3 genuine expected failures with XML/exit2, supporting this one hypothesis. This dependency on the installed test runner's reserved object name is test-only and must be reviewed if the test framework changes.

PlayMode PrefabUtility source identity is unavailable (null) on loaded runtime instances. Integration1's sole failed metadata assertion was moved to Editor preview-scene validation, where serialization identity actually exists. The Editor test opens/closes only preview scenes, inspects their roots, proves shared template identity/no relevant overrides and original difficulty asset identity, and checks the user's active authoring scene is preserved. Runtime tests still validate actual menu/instance behavior and missing dependency rejection. No production API was added for editor metadata.

## Scene exercise, storage and render truthfulness

Scene tests inject a persistent AppSession with in-memory ProfileStore before Boot, so its real PlayerPrefs initialization never creates test/demo data. They exercise Boot→MainMenu→Gameplay, one singleton, guarded double Start, Easy/Normal/Hard60s survival, early death, Results delays, restart, menu return, retained selection and six recorded completions. Survival/death uses scripted time and real spawned FallingObject.TryResolve outcomes, not a human keyboard survival claim. Separate flow test queues genuine InputSystem keyboard states through KeyboardInput and PlayerController, including countdown/pause exclusion. Ten restarts check real pool inactivity, old spawn reports rejected, HP/score/result reset, singleton/selection and exactly10 publications/records. Observer mutation includes nested stats. No real PlayerPrefs writes occur in tests/editor validation.

Native Unity UI interaction is unavailable in this harness; no human mouse/keyboard manual acceptance, browser acceptance, production visuals, audio, chosen-font acceptance or WebGL60FPS measurement is claimed. Impeccable craft/context guidance informed a plain functional prototype and explicit final ownership; its HTML/CSS detector cannot assess native C# uGUI and was not claimed. The user-approved temporary font is builtin LegacyRuntime.ttf, not a downloaded substitute.

ScreenCapture.CaptureScreenshot initially produced no file after frame waits and therefore provides **no render evidence**. Supported installed URP `RenderPipeline.SubmitRenderRequest` with StandardRequest subsequently captured the authored active Gameplay camera and its ScreenSpaceCamera HUD at960×540 without changing Canvas mode/layout/art. The test-only method applies the normal production viewport API for that output size, writes the PNG and restores/releases render targets; there is no runtime screenshot framework.

- `V/task5-gameplay-prototype-20261008181726758.png` (20,329 bytes): actual first capture, player incorrectly red like the apple despite cream source sprite/prefab. Retained evidence; not a visual pass.
- `V/task5-gameplay-prototype-20261008182150797.png` (20,312 bytes): subsequent unchanged-material capture, correct cream player/red food; new player-center green-channel assertion passed.
- `V/task5-gameplay-prototype-20261008183105184.png` (20,312 bytes): final full-suite capture, inspected correct cream player/red food and functional HUD; same pixel assertion passed.

The first discrepancy is unresolved/intermittent. Source inspection suggested possible shared-texture/SRP-batching capture context, but this hypothesis was not proven and no shader/material/global URP change was made. The later passes do not erase the first observation. Independent review and Task7's production render acceptance must consider it; this is why status remains DONE_WITH_CONCERNS.

## Selective staging and preservation evidence

Before staging, index was empty. Snapshot `V/pre-flow-editor-build-settings.asset`→working diff contained only nine scene lines; user `m_configObjects` entries were unchanged. `V/task5-scene-list-index.patch` was created with apply_patch, verified by `git apply --cached --check`, then applied with `git apply --cached`; EditorBuildSettings was never passed to git add. Exact59 owned CPS paths were individually staged. `git diff --cached --check` passed, staged path count60, and `git diff --name-only -- Assets/CPS` was empty (index matches tested working content).

HEAD→index EditorBuildSettings diff was only Boot/MainMenu/Gameplay nine-line insertion. Index→working still contained solely the pre-existing user `com.unity.dt.app-ui` config-object addition (GUID1b1c20d82303e4b5781c3ef50ac1449f). Snapshot→working was rechecked and still only our scene insertion. Commit completed60 paths,5763 insertions and one deletion. Post-commit index is empty, no Unity process remains, no CPS source/asset changes remain, and EditorBuildSettings's sole remaining diff is that user config entry. Other existing deleted Editor/HubForceResolve files, modified UniversalRP/ProjectSettings/ShaderGraphSettings/URPProjectSettings, deleted local design Markdown and untracked PackageManagerSettings/SceneTemplateSettings remain unstaged. No restore/reset/stash/clean, broad add or user settings commit occurred. Git's LF→CRLF notices on new C# files are line-ending notices, not source/test warnings.

## Self-review and handoff

The implementation reuses existing controller/model/input/profile/spawner types rather than duplicating DTOs, bus/framework or dependency. Simulation exists only while active, configuration is snapshotted per round, terminal publication is isolated and cleanup is bounded. Shared prefab identity/config preservation, actual references, scene lifecycle, stale reports, real keyboard, result metadata and both terminal branches have executable coverage. Prototype UI safely diagnoses missing integration, and explicit authoring refuses overwrite.

Remaining concerns: unresolved first-capture discrepancy; final UI/art/font/audio/browser/performance acceptance belongs later tasks; prototype pixel-color assertion must evolve with Task7 art; test runner reserved-name compatibility is test-only installed-framework coupling. Task6 should replace PrototypeMenu/PrototypeGameplayUI and extend GameplayViewport Canvas matching rather than add a second camera helper; Task7 should change the shared rig prefab and cloned definition/sprite wiring without scene-only geometry/config overrides. No final visual acceptance is inferred from functional tests.

## Task 5 review fixes, round 1 (2026-10-09)

Status: both Important review findings fixed with native regression evidence in commit `22fc89b` (`fix: validate shared catcher and match gameplay canvas`). The original status and handoff above describe the first Task 5 commit; this section supersedes its statement that Canvas matching itself is deferred to Task 6. Task 6 still owns broader final UI-size coverage.

Root cause 1: `GameSessionController.GetValidationError` checked its own player for null but passed configuration validation to the spawner, which used a separate private player. The spawner now exposes its read-only `Player`, and the shared controller preflight returns a named mismatch error when both non-null references differ. Spawner's existing named missing-player error still applies when its reference is null. Menu validation reads the shared prefab without instantiation; `BeginRound` reuses the same preflight on the live rig. Tests perturb each path with a distinct valid `PlayerController`, observe Start/Begin rejection, and verify no scene transition or round activity.

Root cause 2: `GameplayViewport.Apply` previously set only `Camera.rect`; authored Canvas settings were separate. The viewport now owns a serialized Canvas reference and restores `ScreenSpaceCamera`, its world camera, `CanvasScaler.ScaleWithScreenSize`, 1920×1080 reference resolution and `Expand` mode on Apply. The installed CanvasScaler uses the native Canvas rendering display size and `Expand` chooses the smaller width/height scale. For the centered 16:9 camera crop, that scale matches the camera viewport without manually duplicating insets. The Unity Editor migration wired existing MainMenu and Gameplay scene references and changed each scaler's serialized mode. Future `GameSceneSetup.CreateCanvas` authors the same contract. The one-time migration method was removed after use; its command below records what ran.

Native PlayMode evidence from the authored Gameplay scene, with actual render-target dimensions logged in XML after `Canvas.ForceUpdateCanvases`:

| Render target | Camera pixelRect | Canvas pixelRect | Canvas scale | Canvas root rect |
|---|---|---|---:|---|
| 1024×768 | x0 y96 1024×576 | x0 y96 1024×576 | 0.5333334 | 1920×1080 |
| 2560×1080 | x320 y0 1920×1080 | x320 y0 1920×1080 | 0.9999999 | 1920×1080 |

The test also corrupts the Canvas camera/mode/scaler first, then calls the production viewport and checks restoration. The existing camera-only rect test remains. The target-size exercise verifies real native camera/Canvas bounds and scaling for two representative aspect ratios; it does not claim browser viewport sizing or Task 6's final UI-size matrix.

### Commands and results

All Unity invocations used `C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe` through hidden `Start-Process -PassThru`, `WaitForExit`, `Refresh`, and an escalated Unity process inventory immediately before launch. `U=C:/Users/Arc/Unity/CPS-Project`; XML/log artifacts are in `D/verification` with `D=C:/Users/Arc/Downloads/CPS/Unity_Project`. Tests used `-batchmode -projectPath U -runTests -testPlatform <mode> -testFilter <filter> -testResults D/verification/<name>.xml -logFile D/verification/<name>.log`, with no `-quit`. The fake injected `ProfileStore` remained in use; these tests did not write production PlayerPrefs.

| Name | Mode and filter | XML total/pass/fail/skip | Actual Unity exit | Interpretation |
|---|---|---|---:|---|
| `task5-fix-red-play` | PlayMode `CPS.KitchenCatch.Tests` | 55/52/3/0 | 2 | Canvas contract genuinely failed; the two new identity assertions initially raised NUnit argument errors on null and were corrected before counting identity RED. |
| `task5-fix-red-identity` | PlayMode `CPS.KitchenCatch.Tests.GameSessionFlowTests.DifferentSpawnerPlayerRejectsBeginBeforeRoundStarts;CPS.KitchenCatch.Tests.GameSceneFlowTests.MenuRejectsDifferentSpawnerPlayerOnSharedTemplate` | 2/0/2/0 | 2 | Both correctly failed `Expected: not null` because preflight returned null. |
| `task5-fix-native-canvas` | PlayMode `CPS.KitchenCatch.Tests.GameSceneFlowTests.ViewportKeepsCanvasOnCameraAtReferenceSizeAcrossAspectRatios` | 1/1/0/0 | 0 | Native camera/Canvas bounds and scale passed. |
| `task5-fix-green-edit` | EditMode `CPS.KitchenCatch.Tests` | 106/106/0/0 | 0 | Full EditMode suite. |
| `task5-fix-green-play` | PlayMode `CPS.KitchenCatch.Tests` | 55/55/0/0 | 0 | Full PlayMode suite and measured bounds output above. |
| `task5-fix-final-authoring` | EditMode `CPS.KitchenCatch.Tests.GameSceneAuthoringTests` | 1/1/0/0 | 0 | Final source after deleting the temporary migration entry point. |

The one-time scene migration ran `-batchmode -quit -projectPath U -executeMethod CPS.KitchenCatch.Editor.GameSceneSetup.WireExistingViewportCanvases -logFile D/verification/task5-fix-wire-canvas.log`, actual exit 0. Unity's initial save normalized empty YAML fields and one Text max-size value; those incidental changes were removed with `apply_patch`. Final scene diffs contain only a Canvas reference and `m_ScreenMatchMode: 0→1` in each scene. The Edit/Play/final-authoring logs had no matched `error CS`, `warning CS`, game `Exception:` or `Curl error` entries. `git diff --check` passed; Git's LF→CRLF notices are checkout line-ending notices.

Changed Task 5 paths: `Assets/CPS/Objects/FallingObjectSpawner.cs`, `Flow/GameSessionController.cs`, `UI/GameplayViewport.cs`, `Editor/GameSceneSetup.cs`, `Scenes/{MainMenu,Gameplay}.unity`, `Tests/PlayMode/{GameSessionFlowTests,GameSceneFlowTests}.cs`, and `Tests/EditMode/GameSceneAuthoringTests.cs`. No new runtime package or metadata files. Self-review checked shared validation order, absence of prefab instantiation in menu tests, scene reference ownership, no duplicate manual Canvas insets, the native measurement output, and the exact nine-path diff. Existing user edits outside `Assets/CPS` remain unstaged. The unexplained first red-player render and unavailable human/browser acceptance remain separate concerns; this fix does not claim to resolve them.
