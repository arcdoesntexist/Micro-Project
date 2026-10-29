## Task 5: เชื่อม playable session และ terminal flow

### Integration ownership and safe settings edit

Read `constraints.md` beside brief. Task1 reviewer cross-task contracts: cap all simulation/stats dt first, Resolve capped-step outcomes before CompleteStep, copy frozen result to subscribers/UI, only controller owns Ready/Results presentation delays. Do not make model event mutability or terminal VFX change stats. Task3 AppSession does not auto-navigate; wire Boot here. Reuse Task4 GameObjectAssetSetup.CreateMissing and existing Editor asmdef.

Frozen-result publication includes isolation between observers: LastResult returns a deep copy; each Finished subscriber receives an independent deep copy so a mutating listener cannot corrupt the result seen by the UI/next listener. Test observer mutation explicitly, including nested hand statistics, rather than only checking source DTO fields.

Task4 spawner exposes non-mutating configuration validation with named errors; use its actual API from task-4-report.md for Start preflight rather than duplicating validators. DifficultyConfig includes DifficultyId; spawner supplies actual CatchZone vertical half-height to SpawnPolicy. MainMenu/config selection and Gameplay must share the authored config assets.

Task4 geometry correction: PlayerController.ReachableCatchCenterRange is the read-only producer of actual reachable catcher-center endpoints from the same movement clamp. Shared spawner validation rejects lanes outside those endpoints, nonfinite movement bounds and nonzero horizontal catcher offsets. Keep authored horizontal catcher offset zero and do not duplicate a fixed field-bound check in flow. Validate the actual Gameplay player/spawner references, not a differently configured temporary player. This carries Ruling8's conservative centered-path contract into scene authoring and Start gating.

Ruling9 integration: one authored GameplayRig prefab is the configuration authority. Menu holds a prefab component reference and uses read-only validation without instantiation/Start/Awake/simulation; Gameplay uses an instance without relevant reference/geometry/config overrides and revalidates before Begin. Do not add an additive scene-loading preflight route. Test shared template identity and missing-dependency rejection in both paths. Later UI/artwork owners update the shared rig prefab, not scene-only overrides. Author the planned `UI/GameplayViewport.cs` production camera/Canvas viewport now rather than a throwaway camera helper; Task6 extends it and adds its required UI-size coverage. Tiny named prototype UI and Boot route glue are allowed for functional scenes and are replaced/removed by Task6 where appropriate. Temporary sprites/cloned definitions must retain the exact original kind/points and are replaced by Task7 production art; original Task4 definitions remain preserved until their artwork assignment task.

Task4 completed API: `GetValidationError(config)` returns null when valid; Inspector fields `player`, `prefab`, `foodDefinitions`, `trashDefinitions`, `seed`; `ActiveCount` is read-only. Config fields are `difficulty`, `speed`, `interval`, `trashProbability`, `lanes`; definitions are `kind`, `points`, `sprite`. Refer to actual source/report for authoring. Reset player to centre before Begin; keep catcher geometry/config stable within a round because each spawner Step snapshots catch bounds. Step handles chronology itself and StopAndClear tolerates an outcome callback. Authored definition sprites are intentionally null until Task7; provide visible simple prototype sprites for Task5's playable check without importing the concept board or claiming production artwork acceptance.

Inspector references + production initialization methods only, no test-only debug methods in Runtime. Tests drive real controller/input/spawner objects and scripted time. Provide read-only countdown state for HUD; Ready/resume3s exclude simulation and stats, explicit Resume after focus return. Pausing during Ready may restart its3s countdown on Resume rather than nesting countdowns. Terminal presentation stays distinct from active gameplay; do not leave Results blocked by old pause flag. Add full metadata to copied result exactly once and call ProfileStore.RecordResult once.

Storage safety remains binding during real scene tests: initialise a persistent injected fake AppSession before loading Boot/MainMenu; do not silently create demo profiles in the user's production PlayerPrefs to test scenes. Native Unity UI control is unavailable in this harness. Drive actual scenes/components through PlayMode checks and capture real Unity renders where feasible; clearly distinguish automated component/scene exercise from an actual keyboard/mouse manual play session. Browser interaction acceptance belongs Task8 when its module is available.

IMPORTANT existing `ProjectSettings/EditorBuildSettings.asset` contains USER edits. Snapshot of current user version at `D/verification/pre-flow-editor-build-settings.asset`. Change only enabled scene list through Editor APIs, preserve m_configObjects. Do NOT `git add ProjectSettings/EditorBuildSettings.asset` (would stage user modifications). Stage only your scene-list hunk with a scoped index patch (`git apply --cached`); compare snapshot→working diff and HEAD→index diff, preserving user config entries unstaged. If patch cannot isolate safely, keep setting change unstaged and ask controller; no restore/reset/stash. Test behavior still uses working scene list.

**Files:** Create `Assets/CPS/Flow/{GameSessionController,SceneNavigator}.cs`, `Assets/CPS/Editor/GameSceneSetup.cs`, `Assets/CPS/Scenes/{Boot,MainMenu,Gameplay}.unity`; Modify `ProjectSettings/EditorBuildSettings.asset` เฉพาะ scene list โดยรักษา m_configObjects; Test `Tests/PlayMode/GameSessionFlowTests.cs`

**Interfaces:** `GameSessionController.State`, `IsPaused`, `LastResult` returns copy; `event Action<SessionState> StateChanged`, `event Action<SessionResult> Finished`; `BeginRound()` / `Pause()` / `Resume()` / `Restart()` / `ReturnToMenu()`; `SceneNavigator.GoTo(string sceneName)` returns guarded transition; `GameSceneSetup.CreateOrUpdate()` explicit Editor menu/executeMethod ไม่ auto-run เมื่อ import
Consumes models Task1, snapshots/stats Task2, AppSession Task3, spawner Task4; terminal result metadata sessionId GUID/endedAtUtc ISO8601; stats DTO source and units included

- [ ] Tests countdown3→Playing, timeout→Triumph→Results1.2s, HP0→Dying→Results1.4s, simultaneous last damage wins, result frozen once, playCount1, 10restarts no state/object/event leak, pause during Ready/Playing/terminal and repeated scene request

```csharp
Assert.That(result.PlayedSeconds, Is.EqualTo(60f).Within(0.001));
Assert.That(result.EndReason, Is.EqualTo(EndReason.HpDepleted));
Assert.That(resultsEmitted, Is.EqualTo(1));
Assert.That(restarted.Health.CurrentHp, Is.EqualTo(3));
Assert.That(loadedAppSessions, Is.EqualTo(1));
```

- [ ] Run P fail
- [ ] Implement one explicit Update order: cap gameplay dt by remainingSeconds → input/player → objects/spawner resolution → stats → CompleteStep → terminal freeze/clear → presentation delay → Results; terminal transition rejects further queued reports; Ready3s, resume3s freezes all simulation/stats; focus loss pauses and resets input, return requires Resume button (ไม่ auto-resume)
- [ ] Author minimal functional scenes and references via Unity Editor APIs; Boot first, MainMenu second, Gameplay third; MainMenu มี temporary demo-profile/Start buttons เรียก AppSession.Select และ SceneNavigator โดยตรงก่อนแทนด้วย Task6; missing configs/references disable Start with named error; editor setup aborts if authored target scenes already exist rather than overwrite manual edits silently; preserve SampleScene
- [ ] Run E/P green; manual Play core using temporary simple sprites, verify 60second survival and early death, restart same selection; functional first, no claim production visuals yet
- [ ] Commit exact new Flow/scenes/setup/tests/metas และ reviewed scene-list diff: `feat: connect playable round and terminal flow`

