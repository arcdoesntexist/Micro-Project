# Task 4 implementation report

Checkout: `C:/Users/Arc/Unity/CPS-Project`, branch `codex/kitchen-catch`, base `3758dff4772db4c6989bb71041a108c52ab72444`. Work began 8 October and continued in place on 9 October after interruption; Tasks 1–3 were preserved.

Status: DONE. Commit: `78eaf9fc67d62baf43c1474fb44f618bc5aecb27` — `feat: add safe pooled falling objects`.

## Implementation and public API

- `DifficultyConfig`: ScriptableObject public fields `difficulty: DifficultyId`, `speed`, `interval`, `trashProbability`, `lanes`; `Validate()` throws named `ArgumentException` for invalid difficulty, nonfinite/nonpositive speed/interval, invalid probability, missing/empty or unreachable lanes, and nonzero Easy trash probability. Reachable lane centers use the current player field limit ±7.5.
- `FallingObjectDefinition`: ScriptableObject fields `kind`, `points`, `sprite`. Authored Apple/Bread/Cake have 10/15/20 points; Can/BananaPeel are Trash with zero points. Sprite fields await the production-art integration task.
- `SpawnChoice`: readonly `Kind`, `DefinitionIndex`, `X`, `ArrivalTime`. Definition indices are scoped to the corresponding three-food/two-trash arrays.
- `SpawnPolicy(DifficultyConfig config, int seed, float catchHalfHeight = 0.25f)` snapshots tuning and lanes. `TryChoose(float now, float playerSpeed, float catchHalfWidth, out SpawnChoice choice)` proposes at most sixteen candidates, returns false instead of forcing unsafe spawns, and uses seeded uniform definition indices. The catch-height addition was controller-approved; finite/nonnegative height is validated.
- `FallingObjectSpawner.Begin(config, Action<long,FallingObjectDefinition,ObjectOutcome> report)`, `Step(dt)`, `StopAndClear()`, read-only `ActiveCount`, and shared non-mutating `GetValidationError(config)`. The validator returns null when valid or a named message for missing player/prefab/config, malformed arrays/config or invalid catcher geometry; Begin uses the same validator and throws before changing the existing session when validation fails. Inspector fields: `player`, `prefab`, `foodDefinitions`, `trashDefinitions`, `seed`.
- `FallingObject.Spawn(id, definition, position, speed)`, `Step(dt, catchZone)`, `Despawn()`, `BindResolution(callback)`, `TryResolve(id,outcome)`, read-only `IsActive` and `SpawnId`. Spawn clears callbacks and resets position/rotation/scale, speed, sprite/color/flips and active state. Despawn clears definition, callback, speed and ID, and deactivates the object. Resolution closes before invoking the callback; stale IDs and repeated resolution fail.
- `GameObjectAssetSetup.CreateMissing()` is an explicit Unity Editor/menu entry point. It creates only absent Task4 targets and refuses to replace an existing asset or existing file. `CPS.Editor.asmdef` accompanies this first real Editor source, referencing CPS.Runtime on Editor only.

Authored difficulty assets match the approved tuning exactly: Easy 2 / 1.40 / 0 with lanes [-6,0,6], Normal 2.8 / 1.10 / .15 with lanes [-6,-3,0,3,6], Hard 3.5 / .85 / .25 with lanes [-6,-4,-2,0,2,4,6]. FallingObject prefab contains the real component and SpriteRenderer, with no physics callback or dummy MonoBehaviour. No packages or unrelated settings were changed for this task.

## Safety and simulation behavior

The policy protects a feasible centered path: hold the previous food center through its full vertical catcher window, travel linearly at no more than 7 units/s, then hold the next food center throughout its window. Each half-window is actual catcher half-height / fall speed. Trash proposals are checked against existing food segments and the stationary continuation after the last food; food proposals are checked against all still-relevant trash windows. Horizontal protection expands the entire path interval by catcher half-width. Completed plans are pruned while retaining the preceding food anchor; there is no planner framework or unbounded history.

Easy remains strictly cyclic. The right-to-left wrap is delayed until feasible; no lane is silently skipped. Trash probability governs proposals before safety rejection, so accepted trash frequency may differ from the nominal probability. Food and trash definition choice occurs uniformly only after a safe kind/lane is accepted.

The first focused stress run found that a later food following several trash proposals could imply movement beginning before the food proposal existed. Travel now starts at `max(previous arrival + half-window, proposal time)`, holding the previous center before departure. The independently checked stress path uses that causal departure time. This ensures past hazard avoidance is not retroactively replaced.

Spawner Step owns all movement. It advances objects between spawn deadlines so a large dt preserves each object's age, vertical position and outcome. A rejected/over-capacity proposal retries after 1/60 simulation second; safe accepted deadlines are reached exactly, including when the deadline is less than one frame away. The pool has a live cap of sixteen; IDs monotonically increase across Stop/Begin. StopAndClear is safe from an outcome callback: a generation guard prevents the old Step loop touching returned/restarted objects. There is no independent Update or trigger reporter.

FallingObject checks the swept downward segment against catcher width/vertical bounds before checking miss, so a coarse step crossing the entire catcher produces one catch. Spawner snapshots CatchZone for each caller Step. Task5 should reset the player to center before Begin, invoke player input and spawner only while Playing, pause both together, and keep gameplay catcher geometry/config stable during a round. Presentation changes can use the existing scale/facing-independent CatchZone. Per-object collision is point-versus-explicit-catcher geometry; sprite edges are not physical colliders.

## TDD evidence and exact commands

Read task-4-brief.md and constraints.md fully, PRODUCT.md, asset provenance, relevant object/input/field/pooling paragraphs of the approved specification, Task2/Task3 dependency reports, implementer template, test-driven-development and writing-good-tests, ponytail, verification-before-completion, and systematic-debugging guidance. No full implementation-plan reread, subagent or reviewer dispatch occurred. Resume reread the task brief and constraints.

Tests preceded implementation bodies. Initial compilable no-op skeletons let behavior assertions execute: TryChoose returned false, Validate accepted invalid values, and objects never produced outcomes. These are executable RED assertions, not missing-symbol/compile failures.

Unity executable: `C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe`. Every Unity invocation used require_escalated, hidden Start-Process / PassThru / WaitForExit / Refresh and inspected the actual ExitCode. Tests never used -quit. Final runs hard-check there is no Unity process before launching and are strictly serial.

Launch form:

```powershell
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Unity project is already in use.' }
$taskUnity = Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe' -ArgumentList '<exact arguments below>' -PassThru -WindowStyle Hidden
$taskUnity.WaitForExit()
$taskUnity.Refresh()
Write-Output "Unity exit=$($taskUnity.ExitCode)"
```

Exact test arguments (all result/log paths are under the approved reference workspace):

```text
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests.SpawnPolicyTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-red-edit.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-red-edit.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests.FallingObjectTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-red-play.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-red-play.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests.SpawnPolicyTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-green-edit-focused.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-green-edit-focused.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests.SpawnPolicyTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-green-edit-focused2.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-green-edit-focused2.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests.FallingObjectTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-red-cadence-play.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-red-cadence-play.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests.FallingObjectTests.ValidationNamesMissingReferencesAndInvalidCatchGeometryWithoutStartingPool -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-red-geometry-play.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-red-geometry-play.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests.FallingObjectTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-green-play-focused2.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-green-play-focused2.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests.FallingObjectTests.StepRatePreservesSpawnCadencePositionsAndSingleReports -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-red-rate-play.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-red-rate-play.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests.FallingObjectTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-green-play-focused3.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-green-play-focused3.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-final-green-edit-20261009.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-final-green-edit-20261009.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-final-green-play-20261009.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-final-green-play-20261009.log
```

| Run | Total | Passed | Failed | Unity exit |
| --- | ---: | ---: | ---: | ---: |
| Initial policy RED | 19 | 0 | 19 | 2 |
| Initial objects RED | 5 | 0 | 5 | 2 |
| First focused policy | 19 | 18 | 1 | 2 |
| Corrected policy focused GREEN | 19 | 19 | 0 | 0 |
| Cadence/geometry RED | 8 | 6 | 2 | 2 |
| Direct geometry assertion RED | 1 | 0 | 1 | 2 |
| Focused objects GREEN | 8 | 8 | 0 | 0 |
| 30/60/120 rate-position RED | 4 | 1 | 3 | 2 |
| Final focused objects GREEN | 11 | 11 | 0 | 0 |
| Full current EditMode 9 Oct | 105 | 105 | 0 | 0 |
| Full current PlayMode 9 Oct | 30 | 30 | 0 | 0 |

Observed RED excerpts:

```text
Initial policy: Expected: True / But was: False
Invalid config: Expected: <System.ArgumentException> / But was: null
Initial object: Missing: < Missed >; Expected: 1 / But was: 0
Stress delayed trajectory: Trash at -2/208.858 crosses food path 0->-6; Expected: True / But was: False
Coarse cadence: Expected: 8 / But was: 1
Direct geometry: Expected: not null / But was: null
Rate-position: Expected: 1.5f +/- 0.01f / But was: 1.53334641f (other rates 1.56667233f, 1.54995584f)
```

The first geometry check used `Does.Contain` on null and NUnit reported an assertion-constraint ArgumentException; this is recorded but is not the direct missing-validation RED proof. Added `Is.Not.Null` and reran before fixing, producing the expected null assertion failure. The rate test exposed clamping every near-deadline slice to at least 1/60: ordinary rates overshot spawn deadlines. Only rejected/not-yet-reschedulable proposals now use the retry floor; positive time-to-deadline is preserved exactly. The same coarse/30/60/120 cases pass and produce 8 total objects, 5 distinct outcomes and remaining Y positions [1.5,4.3,7.1] after 10 seconds.

The thousand-spawn stress cases cover all three difficulties using seed723, comparing a second real policy with identical seed and call sequence; they assert cyclic Easy lanes/no trash, reachable bounds, 7-unit arrival time, all food definitions used without a collapsed selection, both trash variants in Normal/Hard, food travel budgets and trash exclusion over the full protected time interval. These checks derive a real feasible centered path rather than asserting a policy helper's own output. Pool tests use real GameObjects/components, fresh IDs, old/new callbacks, swept catcher crossing, miss, clear/restart, active cap16, zero catcher-height propagation and StopAndClear during an outcome callback.

Final full-suite XMLs both report `result="Passed"`, no failures, no skipped/inconclusive cases; EditMode105 includes the existing86 tests plus19 Task4 tests, PlayMode30 includes existing19 plus11 Task4 tests. Both actual Unity process exits were0. Log searches `rg -n 'error CS|warning CS|Exception|Warning|warning|Curl error'` found no matches in either final test log; the shell tool therefore returned1 for the final grep despite Unity exit0 and passed XML. That grep no-match status is not a test failure. There were no new compiler warnings/errors or game exceptions. Source/assets remained frozen throughout both final Unity runs, and no runtime changes followed them.

## Asset authoring and procedural evidence

Authoring command uses the same hidden process form with:

```text
-batchmode -quit -projectPath C:/Users/Arc/Unity/CPS-Project -executeMethod CPS.KitchenCatch.Editor.GameObjectAssetSetup.CreateMissing -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-create-assets.log
-batchmode -quit -projectPath C:/Users/Arc/Unity/CPS-Project -executeMethod CPS.KitchenCatch.Editor.GameObjectAssetSetup.CreateMissing -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-preserve-assets.log
-batchmode -quit -projectPath C:/Users/Arc/Unity/CPS-Project -executeMethod CPS.KitchenCatch.Editor.GameObjectAssetSetup.CreateMissing -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-preserve-assets-final.log
```

Initial authoring exited0 and created only the nine missing object assets; its log contained no compiler warnings/errors or exceptions. The first 9 Oct SHA256 preservation comparison was inconclusive: while Unity ran, generated whitespace was trimmed in the prefab between before/after hashes. Unity itself exited0; the shell correctly rejected unequal bytes caused by that concurrent manual formatting. The isolated replacement check froze source/assets through process exit and confirmed all nine asset SHA256 hashes unchanged, Unity exit0. Its shutdown contained the already-known baseline `Curl error 42: Callback aborted`, not a game failure. Existing files are skipped without replacement; no edited asset was silently overwritten.

One earlier PlayMode launch (`task4-green-play-focused.log`) overlapped the still-running corrected EditMode session. Unity rejected the project lock, exit1, and produced no XML. This is a launch failure, not RED or GREEN. No process was killed. Subsequent launches hard-check competing Unity processes and wait for the preceding process to exit. Valid earlier RED/focused evidence remains available; no valid RED was rerun merely to regenerate it after interruption.

## Changed files, staging and self-review

Exactly 40 Task4 files (1,072 added lines), limited to:

- `Assets/CPS/Objects/{DifficultyConfig,FallingObjectDefinition,SpawnPolicy,FallingObjectSpawner,FallingObject}.cs` and their five metas.
- `Assets/CPS/Config/{Easy,Normal,Hard,Apple,Bread,Cake,Can,BananaPeel}.asset` and their eight metas.
- `Assets/CPS/Prefabs/FallingObject.prefab` and its meta.
- `Assets/CPS/Editor/{CPS.Editor.asmdef,GameObjectAssetSetup.cs}` and their two metas.
- `Assets/CPS/Tests/EditMode/SpawnPolicyTests.cs`, `Assets/CPS/Tests/PlayMode/FallingObjectTests.cs`, and their metas.
- Four folder metas: Config, Prefabs, Editor, Objects.

Reviewed the complete staged sources/tests and authored asset values/GUID references. Runtime files remain focused (largest112 lines); no extra architecture or unrelated refactor. Mutation checks: dropping seed/definition randomness breaks reproducibility/selection coverage; skipping Easy wrap changes cyclic assertions; ignoring travel or trash conflicts breaks the independently checked path; ignoring height breaks the zero-height live-cap fixture; wrong speed/cadence breaks literal remaining positions; catch-after-miss, missing resolved-state/ID guards or callback reset breaks object tests; missing generation guard breaks callback-driven clearing. Config validation is exercised by malformed speeds/intervals/probabilities/lanes and catcher geometry.

Only generated trailing whitespace was trimmed from owned metas and the prefab; no GUID or functional asset value changed. Git staging initially required escalation because restricted .git writes denied index.lock creation; the approved escalation staged the exact owned paths. Existing user changes in Assets/Editor, Assets/Settings/UniversalRP.asset, ProjectSettings and the deleted project-local design document remain unstaged, as do PackageManagerSettings.asset and SceneTemplateSettings.json. No `git add .`, reset/restore/clean, worktree, main commit, merge, push or PR occurred. This report lives outside the Unity project as required.

After both full suites exited, re-staged only the eleven already-cleaned metadata/prefab paths whose index snapshot predated whitespace trimming. `git diff --cached --check` passed; `git diff --name-only -- Assets/CPS` was empty, confirming the staged snapshot matched the tested working files. Commit contains exactly40 owned files /1072 additions. Post-commit index is empty, no Task4 working changes remain, and only the original unrelated modifications/untracked settings remain. No Unity process was left running.

## Remaining acceptance work

No blocker is known. Production sprite assignment/scale/sorting, live playtest and WebGL frame-time/input acceptance remain with subsequent artwork/integration/build tasks. This task did not import new art, choose fonts/audio, simulate Newton forces, add MQTT/network transport, or implement Task5's state/pause/UI flow. Task5 should use the shared validator for Start gating, set the five Inspector definition references and prefab/player references, reset PlayerController before Begin, and stop/clear at terminal state. Full-suite verification and the scoped commit are complete.

## Fix round 1 — configured player reachability (9 October)

Status: DONE. Fix base: `78eaf9fc67d62baf43c1474fb44f618bc5aecb27`. Fix commit: `455c6d4880541667df2755a60af22d7493b68a70` — `fix: validate lanes against configured player bounds`.

The independent review correctly identified that the original validator only enforced the default field limit, while PlayerController's Inspector mascot width changes the actual movement clamp. With width3, actual player centers stop at±5 and the default catcher edge stops at±5.75; authored lanes±6 were previously accepted and were impossible to catch. Reviewed task-4-review.md, current brief/constraints/report and the actual controller/spawner dependency before changing code, following receiving-code-review guidance. The controller explicitly approved the read-only producer contract and conservative centered-path rejection.

Changes, limited to four files:

- `Assets/CPS/Input/PlayerController.cs`: added read-only `Vector2 ReachableCatchCenterRange` (X=min, Y=max). Endpoints include the horizontal CatchZone offset. A private `MovementLimit` property is now the single producer of the existing `8 - Clamp(mascotHalfWidth,0,8)` calculation; Step and the range use the same value. Actual movement behavior is unchanged.
- `Assets/CPS/Objects/FallingObjectSpawner.cs`: the existing shared GetValidationError validates finite range endpoints, rejects unsupported non-centered horizontal catcher geometry with a named horizontal-offset error, and rejects every lane outside the configured reachable catcher-center endpoints with a named lane error. Begin already consumes this validator, so an invalid configuration cannot start a pool. NaN movement bounds receive their own named error.
- `Assets/CPS/Tests/PlayMode/FallingObjectTests.cs`: five new cases reproduce width3 through a real PlayerController Step, prove actual center5/catcher-edge5.75, then assert named validation failure and Begin rejection before any active object exists. Additional cases reject lane5.5 despite its partial catcher overlap, accept centered boundary lanes±5, reject offset±1, and reject NaN mascot-width movement bounds.
- `Assets/CPS/Tests/PlayMode/KeyboardPlayerTests.cs`: four new cases verify literal reachable-center endpoints for configured widths0.5/3/9/-1 with offset1, then drive real queued keyboard input to both extrema and independently assert the catcher's actual center. This protects the shared producer against width/clamp/offset drift.

The rejection is deliberately conservative: a lane must be reachable by the catcher **center**, rather than merely overlapping a catcher edge, because SpawnPolicy proves a centered path. Horizontal offsets are rejected rather than introducing unused offset-anchor planning. The read-only range remains accurate for offsets for other consumers, but the current centered spawn policy supports zero horizontal offset. DifficultyConfig/SpawnPolicy and their tests were not changed. The review's Minor food-weight assertion suggestion remains deferred in the controller ledger, outside this fix round.

### Executable RED and GREEN

Added tests first and a compilable range getter returning Vector2.zero solely for RED. Before implementing its actual body or validator branches, the affected component suites executed34 cases:25 existing passes and9 expected new failures. The width3 real component reproduced center5 / catcher-edge5.75, then failed `Expected: not null / But was: null` on validator acceptance. Lane5.5, NaN movement range and both offset cases produced the same direct missing-validation assertion; all four producer cases failed against the zero getter with literal expected endpoints, e.g. `Expected: (-4.00,6.00) / But was: (0.00,0.00)`. These were behavior assertions, not missing-symbol/compiler failures.

Implemented the actual producer and shared validation after that RED. The same affected suites passed34/34, Unity exit0. Tests and runtime inputs remained frozen throughout each strictly serial run. No competing Unity process was present; none was killed. Both runs used the established escalated hidden Start-Process/PassThru/WaitForExit/Refresh launch form above. Exact arguments:

```text
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests.FallingObjectTests;CPS.KitchenCatch.Tests.KeyboardPlayerTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-fix1-red-play.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-fix1-red-play.log
-batchmode -projectPath C:/Users/Arc/Unity/CPS-Project -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests.FallingObjectTests;CPS.KitchenCatch.Tests.KeyboardPlayerTests -testResults C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-fix1-green-play.xml -logFile C:/Users/Arc/Downloads/CPS/Unity_Project/verification/task4-fix1-green-play.log
```

| Fix run | Total | Passed | Failed | Unity exit |
| --- | ---: | ---: | ---: | ---: |
| Focused affected suites RED | 34 | 25 | 9 | 2 |
| Focused affected suites GREEN | 34 | 34 | 0 | 0 |

GREEN XML reports `result="Passed"`, no skipped/inconclusive tests. The focused filter includes all16 FallingObjectTests and all18 KeyboardPlayerTests, so original default geometry, stale IDs/pool clearing/cadence and keyboard clamp behavior were exercised along with the new cases. `rg -n 'error CS|warning CS|Exception|Warning|warning|Curl error'` found no matches in the GREEN log; its no-match exit1 is separate from Unity exit0 and passed XML. Per the controller's fix-scope instruction, unrelated profile/AppSession suites were not rerun; policy/config code was unchanged, so no redundant EditMode rerun was required. Earlier full-suite evidence remains above and is not presented as a full-suite run of this fix.

### Fix self-review and scope

Read the complete four-file diff. The runtime correction adds one read-only producer and8 validator lines; no movement semantics, safety planner, tuning asset, prefab, input DTO, metadata or settings change. Confirmed distinct named geometry/offset/lane errors and valid boundary acceptance. Removing configured range checks makes the width3/5.5 fixtures fail; widening to catcher-edge coverage makes the centered-path fixture fail; removing offset or finite guards makes their fixtures fail; wrong range/clamp/offset arithmetic breaks literal endpoint and real movement assertions. Existing default Begin fixtures remain accepted. No new comments or emojis were added.

Staged only the four named sources/tests, confirmed `git diff --cached --check` clean and `git diff --name-only -- Assets/CPS` empty, then committed73 additions/1 deletion. Source was not changed after GREEN. Post-commit index is empty; all original user settings/deletions and untracked settings remain untouched and unstaged. No assets were regenerated, no subagent/reviewer was spawned, and no merge/push/PR occurred. No blocking concern remains for this review finding; Task5 still owns the centered initial player reset and stable geometry during a round.
