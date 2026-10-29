# Task 5 review

## Spec Compliance verdict

Needs fixes. The supplied change implements the playable round, terminal flow, persistent selection, shared rig authoring and guarded navigation. Two Important integration gaps remain: Start does not prove that the moved player is the spawner's catcher, and the production viewport handoff does not establish its promised Canvas matching behavior. Root confirmed that no prior ruling relaxes the brief's camera/Canvas requirement.

Missing: a shared player-identity check in preflight; production Canvas viewport ownership. Extra: no unsupported framework, package or production art work was found. The prototype UI, cloned definitions, temporary sprites and Boot glue are permitted by the brief. Misunderstood: treating Canvas matching as entirely Task6 work narrows the brief's stated Task5 handoff; Task6 was described as extending the production component and adding UI-size coverage.

⚠️ Scoped unresolved items:

- The first actual render visibly gives the player the food's red appearance. The two later supplied captures show the expected cream player, but neither establishes a cause or fix. This is unresolved render evidence, not a proven shader defect or a production-art pass.
- Scene survival/death checks script time and real object outcomes. A separate test queues real keyboard input. These do not establish human keyboard/mouse manual play acceptance; the report accurately says that acceptance was unavailable in this harness.
- Cannot verify from this diff: WebGL browser acceptance, real-build 60 FPS/frame time, final UI/font/audio/art/animation/IoTE appearance, future sensor integration, or correctness of unchanged model/statistics/storage implementations beyond their exercised Task5 integration.

## Strengths

- `Assets/CPS/Flow/GameSessionController.cs:103` caps dt before input/player, spawner resolution, statistics and CompleteStep. Ready/resume countdowns return before these operations. Terminal presentation has its own delay and ignores Pause, so old pause state does not block Results.
- `Assets/CPS/Flow/GameSessionController.cs:28` returns a copied LastResult. Publication at `GameSessionController.cs:140` copies separately for every Finished listener. The flow test mutates both top-level score and nested hand duration, then checks the following observer and stored result; it tests the required isolation rather than only source fields.
- `Assets/CPS/Flow/GameSessionController.cs:58` snapshots difficulty and profile identity, resets HP/position/statistics and clears spawned objects for each Begin. Metadata and profile recording are centralized in terminal freezing. Ten-restart assertions cover stale object rejection and one publication/record per completion.
- `Assets/CPS/Tests/EditMode/GameSceneAuthoringTests.cs:23` proves the menu template is the source of the authored gameplay instance, rejects relevant component overrides, checks original difficulty asset identities and preserves the active authoring scene. The scene diff itself contains only root transform/name overrides on the rig.
- `Assets/CPS/Editor/GameSceneSetup.cs:24` checks every target scene before authoring; explicit setup refuses existing scenes. `ProjectSettings/EditorBuildSettings.asset:8` adds only Boot/MainMenu/Gameplay, keeps SampleScene and leaves config objects unchanged in the supplied commit diff. The original user dt.app-ui entry is absent from the committed change.
- `Assets/CPS/Tests/PlayMode/GameSceneFlowTests.cs:18` installs a fake persistent AppSession before loading real scenes, so the tests do not create demo records in production PlayerPrefs. Runner preservation is test-only, and teardown unloads only the named fixture scenes.

## Critical

None found within Task5 scope.

## Important

1. **Preflight can approve a different catcher than the player the controller moves** — `Assets/CPS/Flow/GameSessionController.cs:44` and `:47`.

   Cause: the controller only verifies its player is non-null, then asks the spawner to validate its own private player. The existing spawner validator and Step both use that separate reference, with no identity comparison. If the shared rig is authored with controller.player = A and spawner.player = B, both paths can pass validation, input moves A, and objects spawn/resolve against B. This breaks the brief's actual-player/reference validation contract even though the current prefab happens to wire both to the same object.

   Impact: an invalid shared rig can enable Start and produce catches/damage unrelated to the visible controlled player; an invalid controller player geometry can also escape the geometry checks applied to B.

   Fix: expose a read-only spawner player reference or extend its existing validator to accept/check the expected player, and return a named mismatch error from shared preflight. Cover mismatched non-null references through menu validation and actual BeginRound, without instantiating the menu template.

2. **GameplayViewport does not implement the required production Canvas viewport handoff** — `Assets/CPS/UI/GameplayViewport.cs:18`.

   Cause: Apply updates only Camera.rect. The component has no Canvas/CanvasScaler reference or scale/matching behavior; the authored Canvas separately uses ScreenSpaceCamera and a fixed 0.5 width/height match. ScreenSpaceCamera does couple rendering to the camera, so this is not a claim that the Canvas ignores Camera.rect. It is the absence of the production camera/Canvas ownership promised by the brief, which says Task6 extends that component and adds size coverage. The implementation report explicitly defers Canvas matching itself to Task6.

   Impact: the Task5 production handoff does not establish reference-size UI matching across non-16:9 displays; current tests prove only camera rect fractions and a 16:9 capture. Exact native Canvas size/clipping at other ratios cannot be verified from the supplied diff and was not assumed.

   Fix: establish the camera/Canvas contract in the production viewport and authored scene wiring, including reference-size matching, with a focused native camera-plus-Canvas bounds/scale behavior check. If ScreenSpaceCamera supplies a required part automatically, prove and retain that behavior rather than duplicating it. Task6 can retain ownership of its broader final UI-size matrix. Root confirmed there is no approved camera-only relaxation; the report's deferred handoff does not supersede the brief.

## Minor

No additional concrete code defect found. The test-only reserved runner name and prototype pixel-color assertion are recorded handoff limits: review them when the test framework or production art changes. They do not justify a runtime framework or speculative compatibility layer now.

## Evidence and scope

Reviewed the supplied `455c6d4880541667df2755a60af22d7493b68a70` → `8e16ae0e83729ad2bbe2cc22219c622df37827ab` package, its single commit/stat/full diff, Task5 brief, adjacent constraints and implementation report. Read the package in bounded parts; recovered only the tool-truncated middle of the initial part. No Git rerun, changed-source reread, broad crawl, implementation edit, suite rerun or Unity launch was performed.

Independent artifact inspection confirmed final EditMode XML 106/106 and PlayMode XML 52/52, zero failures/skips. The final logs report exit 0 and contain no matched compiler errors/warnings or game exceptions. A Curl token occurs only in Unity's memory-label accounting, not as a Curl failure. Root independently confirmed actual process exits and pristine logs. Executable RED XML inspected: flow 8/8 failed and recovered scenes 3/3 failed. The aborted original scene run without XML and passing file named red-render were not counted as RED evidence.

Focused checks outside the diff, each tied to a concrete risk:

- **Risk: different controller/spawner players can pass actual-reference preflight.** Read only `Assets/CPS/Objects/FallingObjectSpawner.cs` through its validator/Step/cleanup. Confirmed it validates and simulates its own serialized player and exposes no identity gate. One initial guessed path did not exist; an exact-filename lookup located the file.
- **Risk: camera-only Apply does not establish Canvas reference-size matching.** Located the installed ugui CanvasScaler by exact filename and read HandleScaleWithScreenSize. It uses native Canvas.renderingDisplaySize and width/height matching; read the installed UnityEngine.UIModule XML descriptions of pixelRect/renderingDisplaySize. These prove no Task5 explicit Canvas handling, but do not resolve native mismatched-size layout dimensions; no clipping outcome was asserted.
- **Risk: later passing captures could conceal the earlier reported bad render.** Viewed exactly the three named supplied PNGs. The first player is red; the later two are cream. No source cause is established by those images.

Expected load assessed: one local demonstration round, bounded pooled objects and per-frame updates, desktop browser target. No evidence-supported new load problem was found. Models/statistics/storage remain shared dependencies; cross-task implementation requirements that need their broader review remain unverified here.

## Assessment

Task quality: Needs fixes. The round/controller code is compact, uses the existing domain types and validators, and has meaningful state, observer, keyboard, restart and scene tests. The two Important items are integration-contract gaps; passing suites do not exercise them. Resolve findings 1 and 2 before approving Task5. Finding 1 is missing validation, not behavior mandated incorrectly by the plan; finding 2 requires proving the promised Canvas handoff and does not assert an unobserved runtime clipping defect. Carry the unexplained render observation and unavailable manual acceptance forward without claiming that either has been fixed.
