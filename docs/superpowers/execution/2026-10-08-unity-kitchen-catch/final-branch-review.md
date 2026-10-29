# Final whole-branch review

What this change does: Adds the Unity Kitchen Catch game from profile selection through a timed round, frozen results, and restart. It includes keyboard statistics, pooled falling objects, production presentation, and validated release/development WebGL builds.

Reviewed base `58122dcf35ab98921f17a461907f7f52900b8556` through head `bbbf39c22d3f38fb71d393bb63aa42c0601c9c51`, using the supplied bounded branch package, approved spec/plan, execution ledger, task reviews/reports, connected source, and existing verification evidence. Expected load is one local player in a desktop browser. No Git diff was regenerated, and no Unity, browser, build, or test run was started. The only write is this report.

## Assessment

- Spec compliance: **Issues found**
- Code quality: **Needs fixes**
- Overall: **Needs fixes**
- Findings: **0 Critical, 1 Important, 1 Minor**

## Important

1. **A returning player cannot select the sole saved profile after reload.**

   Location: [MainMenuUI.cs:87](C:/Users/Arc/Unity/CPS-Project/Assets/CPS/UI/MainMenuUI.cs:87), with the default index at line 77 and Start gating at line 162.

   Concrete failure: Create one profile, then restart the application or reload the browser. `AppSession.Initialize` loads stored profiles but leaves `SelectedProfile` null ([AppSession.cs:13](C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Profiles/AppSession.cs:13)). `RefreshProfiles` displays index 0 with `SetValueWithoutNotify`, without calling `app.Select`. Validation consequently keeps Start disabled. Clicking that already displayed row cannot repair the selection: the installed uGUI dropdown's `OnSelectItem` assigns the same index, and `Set` returns before invoking the callback when `value == m_Value` ([Dropdown.cs:415](C:/Users/Arc/Unity/CPS-Project/Library/PackageCache/com.unity.ugui@f3ea87521141/Runtime/UGUI/UI/Core/Dropdown.cs:415), [Dropdown.cs:1148](C:/Users/Arc/Unity/CPS-Project/Library/PackageCache/com.unity.ugui@f3ea87521141/Runtime/UGUI/UI/Core/Dropdown.cs:1148)). Difficulty buttons also do not select an unselected profile. With one saved profile, the player must create another profile to get unstuck; with several profiles, selecting a different row first works around the problem. This breaks the required saved-profile selection flow. The preliminary finding is independently supported by the actual installed uGUI implementation, not just an assumption about events.

   Smallest fix: When profiles exist and the session has no selection, explicitly select the same profile that the dropdown displays, using the chosen difficulty. Preserve the existing selection when returning from Gameplay.

   Coverage gap: [GameUITests.cs:56](C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Tests/PlayMode/GameUITests.cs:56) checks duplicate option labels without checking selection or Start. The warning-selection test preselects a profile and changes to index 1. [GameSceneFlowTests.cs:611](C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Tests/PlayMode/GameSceneFlowTests.cs:611) creates and selects a new profile whenever selection is null. The persistence test loads data without mounting the menu. Add a fresh AppSession backed by exactly one persisted profile, mount the production menu, and assert the displayed identity is selected and Start is enabled without creating a profile or manually invoking `onValueChanged`. Also retain the existing selection on a normal scene return. Browser persistence evidence does not establish this post-reload Start interaction.

## Minor

2. **The food distribution assertion does not protect equal selection weights.**

   Location: [SpawnPolicyTests.cs:68](C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Tests/EditMode/SpawnPolicyTests.cs:68).

   Concrete coverage failure: For the Easy case's 1,000 food selections, counts of 500/250/250 satisfy every `count > 200` assertion even though one food is selected twice as often as the others. This was the deferred Task 4 test weakness. The current implementation uses `random.Next(3)` ([SpawnPolicy.cs:66](C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Objects/SpawnPolicy.cs:66)); no current distribution bug is alleged.

   Smallest fix: Check each food's share against one third of the actual food count with a suitably broad deterministic tolerance, preferably in a dedicated seeded selection check. Avoid using 1,000 total food-plus-trash objects as the food denominator. This is nonblocking test hardening; it does not require changing the runtime selection logic.

## Integrated checks and evidence

- Rules/time: The controller caps the simulation interval before input, movement, spawning, statistics, and `CompleteStep`. The model resolves damage before timeout, latches terminal state, and rejects later outcomes. Terminal cleanup does not count outstanding objects as misses. Targeted tests cover deadline damage, oversized intervals, once-only publication, and restart.
- Input/focus/pause: Alias keys are combined per hand; invalid or unfocused input is excluded. Focus loss pauses Ready/Playing, manual Resume starts a fresh countdown, and paused/countdown intervals bypass simulation and statistics. Existing tests cover these paths; foreground hands-on interaction remains a disclosed acceptance item.
- Frozen results/statistics: Statistics use valid elapsed time and independent force availability. `SessionResult.Copy` deep-copies both hands; the controller gives separate copies to listeners and callers. Results distinguish missing samples from valid released keyboard input and do not invent Newton values.
- Persistence/data safety: Read failure blocks writes; malformed raw storage must be backed up and flushed before primary replacement. Failed saves retain in-memory profiles/counts with warnings. Session IDs prevent duplicate count increments within the store runtime. The Important finding concerns menu/session selection, not loss of stored profile data.
- Pool/restart/identity: Spawn IDs remain fresh across reuse; callbacks and active objects clear on stop. The generation guard handles a clear during an outcome callback. Runtime validation checks controller/spawner player identity and reachable lanes. Gameplay uses the shared rig without relevant scene overrides; build validation now checks the effective Gameplay scene instance.
- UI/presentation: Camera and Canvas share the fixed 16:9 viewport. Presentation holds no scoring authority, uses separate unmirrored hat/body renderers, and clears burst/dissolve state on reset. Final Task 7 reviews accepted looping, production-prefab assertions, peel contour, text alignment, and rank treatment. This review did not repeat visual capture inspection or hands-on animation acceptance.
- Build/scope: Release uses `BuildOptions.None`; the development harness is entirely compilation-guarded, installs only in development builds, and uses a separate output folder. Build failure throws. The branch package limits the settings change to the required scene list and adds no sensor, transport, upload queue, or backend implementation.

Independently parsed existing XML: `verification/task8-fix2-final-edit.xml` is 125/125 passed, zero failed; `verification/task8-fix1-final-play.xml` is 71/71 passed, zero failed; `verification/task8-fix2-scene-fixture-green2.xml` is 1/1 passed. The last commit changes the Editor fixture only, so the earlier PlayMode/build evidence remains relevant rather than being represented as a fresh final-head run. Task 8 reports successful release/development rebuilds, clean release HTTP loading, and three naturally elapsed 60-second development rounds. Those passing checks do not cover finding 1.

## Remaining triage and acceptance boundaries

- The unique temporary-scene fixture's `LoadAssetAtPath<SceneAsset>` cleanup guard ([BuildValidationTests.cs:99](C:/Users/Arc/Unity/CPS-Project/Assets/CPS/Tests/EditMode/BuildValidationTests.cs:99)) could be made more defensive if copying succeeds but importing fails. No such failure or remaining asset is established; this is an optional hardening observation, not an additional finding. The prior destructive fixed-path/scene-replacement behavior is gone.
- The two opaque development-only `ERROR: Shader ` events remain disclosed. Source identity and cause are unknown, while release evidence is clean. They do not support a concrete release-code finding or speculative URP changes.
- Licensed font/Thai shaping and audio, foreground 60 FPS, a genuine 30/60 comparison, mouse-only/visible keyboard-focus acceptance, and hands-on difficulty feel remain explicitly open. The headless browser ignored requested frame caps; its approximately 143.7 updates/s is not user-visible 60 FPS acceptance. Physical input, MQTT, and cloud work remain outside this phase. None of these disclosed gaps is counted as a new code defect.

Verdict: Fix finding 1 before approval. Finding 2 is nonblocking coverage hardening.
